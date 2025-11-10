using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// Manages debt repayment for enslaved prisoners working off their debt
    /// Ticks daily to calculate and apply debt payments based on slave labor
    /// </summary>
    public class WorldComponent_DebtManager : WorldComponent
    {
        // Configuration constants
        private const int TICK_INTERVAL = GenDate.TicksPerDay; // Check once per day
        private const float BASE_DAILY_DEBT_PAYMENT = 35f; // Base silver value earned per day of slave labor

        // Timing constants for enslavement queue
        private const int ENSLAVEMENT_DELAY_TICKS = 60; // 1 second (60 ticks)
        private const int MAX_BED_WAIT_TICKS = 600; // 10 seconds to reach bed

        // Skill level thresholds for labor value
        private const int SKILL_THRESHOLD_LOW = 5;   // Below this = low skill
        private const int SKILL_THRESHOLD_MED = 10;  // Below this = medium skill
        private const int SKILL_THRESHOLD_HIGH = 15; // Below this = high skill, above = expert

        // Skill multipliers - skilled work is worth more
        private const float SKILL_MULTIPLIER_LOW = 0.7f;    // Average skill 0-4
        private const float SKILL_MULTIPLIER_MED = 1.0f;    // Average skill 5-9
        private const float SKILL_MULTIPLIER_HIGH = 1.3f;   // Average skill 10-14
        private const float SKILL_MULTIPLIER_EXPERT = 1.6f; // Average skill 15+

        // Trait work multipliers
        private const float LAZY_WORKER_MULTIPLIER = 0.7f;  // Lazy trait penalty
        private const float HARD_WORKER_MULTIPLIER = 1.3f;  // Industrious trait bonus

        // Suppression efficiency constants
        private const float MIN_SUPPRESSION_EFFICIENCY = 0.5f; // Minimum efficiency at 0 suppression
        private const float SUPPRESSION_EFFICIENCY_RANGE = 0.5f; // Range from min to max (0.5-1.0)

        // Ritual quality affects how efficiently slaves work off debt
        // High quality ritual = motivated worker, low quality = demoralized worker
        private const float REPAYMENT_MULTIPLIER_EXCELLENT = 1.4f;  // Excellent ritual = 40% faster
        private const float REPAYMENT_MULTIPLIER_STANDARD = 1.15f;  // Standard ritual = 15% faster
        private const float REPAYMENT_MULTIPLIER_PARTIAL = 1.0f;    // Partial = normal speed
        private const float REPAYMENT_MULTIPLIER_NODEAL = 0.6f;     // No deal = 40% slower (demoralized)

        private int tickCounter = 0;

        // Queue for prisoners that need to be enslaved (to handle timing issues with rituals)
        private class PendingEnslavement
        {
            public Pawn prisoner;
            public Pawn warden;
            public float debt;
            public int scheduledTick;
            public int retryCount = 0; // Track retry attempts
            public string lastFailureReason = null; // Track why it failed
        }
        private List<PendingEnslavement> pendingEnslavements = new List<PendingEnslavement>();

        // Maximum retry attempts before giving up
        private const int MAX_ENSLAVEMENT_RETRIES = 20; // ~20 seconds of retries

        public WorldComponent_DebtManager(World world) : base(world)
        {
        }

        /// <summary>
        /// Queue a prisoner to be enslaved on the next tick
        /// This avoids timing issues with ritual cleanup
        /// </summary>
        public void QueuePrisonerForEnslavement(Pawn prisoner, Pawn warden, float debt)
        {
            if (prisoner == null) return;

            pendingEnslavements.Add(new PendingEnslavement
            {
                prisoner = prisoner,
                warden = warden,
                debt = debt,
                scheduledTick = Find.TickManager.TicksGame + ENSLAVEMENT_DELAY_TICKS
            });
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            // Process pending enslavements
            ProcessPendingEnslavements();

            tickCounter++;

            // Process debt payments once per day
            if (tickCounter >= TICK_INTERVAL)
            {
                tickCounter = 0;
                ProcessDailyDebtPayments();
            }
        }

        /// <summary>
        /// Process any prisoners queued for enslavement
        /// </summary>
        private void ProcessPendingEnslavements()
        {
            if (pendingEnslavements == null || pendingEnslavements.Count == 0)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            List<PendingEnslavement> toRemove = new List<PendingEnslavement>();

            foreach (var pending in pendingEnslavements)
            {
                // Skip if not yet time
                if (currentTick < pending.scheduledTick)
                {
                    continue;
                }

                // Validate the prisoner is still valid
                if (pending.prisoner == null)
                {
                    pending.lastFailureReason = "Prisoner reference is null";
                    toRemove.Add(pending);
                    Mod.Log?.Warning($"Enslavement cancelled: {pending.lastFailureReason}");
                    continue;
                }

                if (pending.prisoner.Dead)
                {
                    pending.lastFailureReason = $"{pending.prisoner.LabelShort} died before enslavement";
                    toRemove.Add(pending);
                    Mod.Log?.Message($"Enslavement cancelled: {pending.lastFailureReason}");
                    continue;
                }

                // Only enslave if still a prisoner (not already enslaved)
                if (pending.prisoner.IsPrisonerOfColony && !pending.prisoner.IsSlave)
                {
                    // Check retry limit (but allow one final attempt)
                    bool isFinalAttempt = pending.retryCount >= MAX_ENSLAVEMENT_RETRIES;

                    if (isFinalAttempt)
                    {
                        // This is the final attempt, skip all checks and try to enslave directly
                        Mod.Log?.Message($"Making final enslavement attempt for {pending.prisoner.LabelShort} (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
                    }
                    else
                    {
                        // Still have retries left, perform normal checks

                        // Check if prisoner is still in a ritual or being carried
                        if (pending.prisoner.GetLord() != null)
                        {
                            // Still in a Lord (ritual/event), delay longer
                            pending.lastFailureReason = "Still in ritual/event";
                            pending.retryCount++;
                            pending.scheduledTick = currentTick + ENSLAVEMENT_DELAY_TICKS;
#if DEBUG
                            Mod.Log?.Message($"{pending.prisoner.LabelShort} still in Lord, delaying enslavement (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
#endif
                            continue;
                        }

                        // Check if prisoner is being carried or escorted
                        if (pending.prisoner.CurJob != null &&
                            (pending.prisoner.CurJob.def.defName.Contains("Escort") ||
                             pending.prisoner.CarriedBy != null))
                        {
                            // Still being moved, delay longer
                            pending.lastFailureReason = $"Being moved (job: {pending.prisoner.CurJob?.def?.defName})";
                            pending.retryCount++;
                            pending.scheduledTick = currentTick + ENSLAVEMENT_DELAY_TICKS;
#if DEBUG
                            Mod.Log?.Message($"{pending.prisoner.LabelShort} still being moved, delaying enslavement (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
#endif
                            continue;
                        }

                        // Check if in bed/cell (ideal state for enslavement)
                        bool inBed = pending.prisoner.CurrentBed() != null;
                        if (!inBed && pending.scheduledTick + MAX_BED_WAIT_TICKS > currentTick)
                        {
                            // Not in bed yet, delay a bit more
                            pending.lastFailureReason = "Not in bed yet";
                            pending.retryCount++;
                            pending.scheduledTick = currentTick + ENSLAVEMENT_DELAY_TICKS;
#if DEBUG
                            Mod.Log?.Message($"{pending.prisoner.LabelShort} not in bed yet, delaying enslavement (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
#endif
                            continue;
                        }
                    } // End of normal checks

                    // Get warden (use original or find a new one)
                    Pawn warden = pending.warden;
                    if (warden == null || warden.Dead || warden.Downed)
                    {
                        warden = pending.prisoner.Map?.mapPawns.FreeColonists.FirstOrDefault(p => p != null && !p.Dead && !p.Downed);
                    }

                    if (warden == null)
                    {
                        // No warden available
                        if (isFinalAttempt)
                        {
                            // Final attempt and no warden - give up
                            string failureMsg = $"Failed to enslave {pending.prisoner.LabelShort} after {pending.retryCount + 1} attempts. Final reason: No valid warden available";
                            Mod.Log?.Warning(failureMsg);
                            Messages.Message(
                                $"Unable to enslave {pending.prisoner.LabelShort}. They remain a prisoner with {pending.debt:F0} silver debt.",
                                pending.prisoner,
                                MessageTypeDefOf.NegativeEvent
                            );
                            toRemove.Add(pending);
                            continue;
                        }
                        else
                        {
                            // This is a transient failure, retry
                            pending.lastFailureReason = "No valid warden available";
                            pending.retryCount++;
                            pending.scheduledTick = currentTick + ENSLAVEMENT_DELAY_TICKS;
                            Mod.Log?.Warning($"Cannot enslave {pending.prisoner.LabelShort} - {pending.lastFailureReason} (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
                            continue;
                        }
                    }

                    // Attempt enslavement
                    bool enslaved = GenGuest.TryEnslavePrisoner(warden, pending.prisoner);

                    if (enslaved)
                    {
                        var debtRecord = DebtUtils.TryGetDebtRecord(pending.prisoner);
                        int estimatedDays = debtRecord?.EstimatedDaysOfLabor() ?? 0;

                        Messages.Message(
                            $"{pending.prisoner.LabelShort} has been enslaved to work off their debt of {pending.debt:F0} silver (est. {estimatedDays} days of labor).",
                            pending.prisoner,
                            MessageTypeDefOf.NeutralEvent
                        );

#if DEBUG
                        Mod.Log?.Message($"Successfully enslaved {pending.prisoner.LabelShort} to pay off debt of {pending.debt:F0} silver");
#endif
                    }
                    else
                    {
                        // Enslavement failed
                        if (isFinalAttempt)
                        {
                            // This was the final attempt and it failed - give up
                            string failureMsg = $"Failed to enslave {pending.prisoner.LabelShort} after {pending.retryCount + 1} attempts. Final reason: TryEnslavePrisoner returned false";
                            Mod.Log?.Warning(failureMsg);
                            Messages.Message(
                                $"Unable to enslave {pending.prisoner.LabelShort}. They remain a prisoner with {pending.debt:F0} silver debt.",
                                pending.prisoner,
                                MessageTypeDefOf.NegativeEvent
                            );
                            toRemove.Add(pending);
                            continue;
                        }
                        else
                        {
                            // Not final attempt yet, this could be due to game state, retry
                            pending.lastFailureReason = "TryEnslavePrisoner returned false (game state issue)";
                            pending.retryCount++;
                            pending.scheduledTick = currentTick + ENSLAVEMENT_DELAY_TICKS;
                            Mod.Log?.Warning($"Failed to enslave {pending.prisoner.LabelShort} - {pending.lastFailureReason} (retry {pending.retryCount}/{MAX_ENSLAVEMENT_RETRIES})");
                            continue;
                        }
                    }
                }
                else if (pending.prisoner.IsSlave)
                {
#if DEBUG
                    Mod.Log?.Message($"{pending.prisoner.LabelShort} is already a slave");
#endif
                }

                toRemove.Add(pending);
            }

            // Remove processed items
            foreach (var item in toRemove)
            {
                pendingEnslavements.Remove(item);
            }
        }

        /// <summary>
        /// Process debt payments for all enslaved pawns with debt across all colonies
        /// </summary>
        private void ProcessDailyDebtPayments()
        {
            // Find all player colonies
            List<Map> playerMaps = Find.Maps.Where(m => m.IsPlayerHome).ToList();

            foreach (Map map in playerMaps)
            {
                // Find all slaves on this map
                List<Pawn> slaves = map.mapPawns.SlavesOfColonySpawned.ToList();

                foreach (Pawn slave in slaves)
                {
                    ProcessSlaveDebtPayment(slave);
                }
            }

            // Also process crime archival for all pawns with criminal records
            ProcessCrimeArchival(playerMaps);
        }

        /// <summary>
        /// Archive old crimes to reduce save file bloat
        /// Called once per day as part of daily processing
        /// </summary>
        private void ProcessCrimeArchival(List<Map> playerMaps)
        {
            foreach (Map map in playerMaps)
            {
                // Get all pawns on the map (colonists, prisoners, slaves, etc.)
                List<Pawn> allPawns = map.mapPawns.AllPawns.ToList();

                foreach (Pawn pawn in allPawns)
                {
                    var criminalRecord = CrimeUtils.TryGetCriminalRecord(pawn);
                    if (criminalRecord != null && criminalRecord.TotalCrimeCount > 0)
                    {
                        // Archive crimes older than 60 days (default)
                        criminalRecord.ArchiveOldCrimes();
                    }
                }
            }
        }

        /// <summary>
        /// Process debt payment for a single slave
        /// </summary>
        private void ProcessSlaveDebtPayment(Pawn slave)
        {
            if (slave == null || slave.Dead || !slave.IsSlaveOfColony)
            {
                return;
            }

            // Check if slave has debt
            var debtRecord = DebtUtils.TryGetDebtRecord(slave);
            if (debtRecord == null || debtRecord.CurrentDebt <= 0)
            {
                return;
            }

            // Calculate daily payment based on slave's capabilities
            float dailyPayment = CalculateDailyDebtPayment(slave);

            if (dailyPayment <= 0)
            {
                return;
            }

            // Pay down the debt
            float actualPayment = debtRecord.PayDebt(dailyPayment, "Slave Labor");

            if (actualPayment > 0)
            {
                if (Prefs.DevMode)
                {
                    Mod.Log?.Message($"{slave.LabelShort} paid {actualPayment:F0} silver through labor. Remaining debt: {debtRecord.CurrentDebt:F0}");
                }

                // Check if debt is fully paid
                if (debtRecord.CurrentDebt <= 0)
                {
                    HandleDebtFullyPaid(slave);
                }
            }
        }

        /// <summary>
        /// Calculate the daily debt payment for a slave based on their work capability
        /// Factors in: health, skills, traits
        /// </summary>
        private float CalculateDailyDebtPayment(Pawn slave)
        {
            float payment = BASE_DAILY_DEBT_PAYMENT;

            // Factor 1: Health condition (can't work well if sick/injured)
            if (slave.health != null)
            {
                float healthEfficiency = 1f;

                // Consciousness affects all work
                if (slave.health.capacities != null)
                {
                    healthEfficiency *= slave.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                }

                // Can't work if downed
                if (slave.Downed)
                {
                    return 0f;
                }

                // Reduce payment based on health
                payment *= healthEfficiency;
            }

            // Factor 2: Skill level (more skilled = more valuable work)
            if (slave.skills != null)
            {
                // Calculate average skill level across all non-disabled skills
                List<SkillDef> workSkills = new List<SkillDef>
                {
                    SkillDefOf.Construction,
                    SkillDefOf.Plants,
                    SkillDefOf.Mining,
                    SkillDefOf.Cooking,
                    SkillDefOf.Crafting,
                    SkillDefOf.Animals
                };

                float totalSkill = 0f;
                int skillCount = 0;

                foreach (SkillDef skillDef in workSkills)
                {
                    SkillRecord skill = slave.skills.GetSkill(skillDef);
                    if (skill != null && !skill.TotallyDisabled)
                    {
                        totalSkill += skill.Level;
                        skillCount++;
                    }
                }

                if (skillCount > 0)
                {
                    float avgSkill = totalSkill / skillCount;

                    // Apply skill multiplier
                    if (avgSkill < SKILL_THRESHOLD_LOW)
                    {
                        payment *= SKILL_MULTIPLIER_LOW;
                    }
                    else if (avgSkill < SKILL_THRESHOLD_MED)
                    {
                        payment *= SKILL_MULTIPLIER_MED;
                    }
                    else if (avgSkill < SKILL_THRESHOLD_HIGH)
                    {
                        payment *= SKILL_MULTIPLIER_HIGH;
                    }
                    else
                    {
                        payment *= SKILL_MULTIPLIER_EXPERT;
                    }
                }
            }

            // Factor 3: Traits that affect work
            if (slave.story?.traits != null)
            {
                // Check for Industriousness trait (has degrees: -2 = Lazy, 0 = Normal, 2 = Hard worker)
                if (slave.story.traits.HasTrait(TraitDefOf.Industriousness))
                {
                    int degree = slave.story.traits.DegreeOfTrait(TraitDefOf.Industriousness);
                    if (degree < 0)
                    {
                        // Lazy/Slothful - works less effectively
                        payment *= LAZY_WORKER_MULTIPLIER;
                    }
                    else if (degree > 0)
                    {
                        // Hard worker/Industrious - works more effectively
                        payment *= HARD_WORKER_MULTIPLIER;
                    }
                }
            }

            // Factor 4: Suppression level (slaves with low suppression work less effectively)
            if (slave.needs != null)
            {
                Need_Suppression suppression = slave.needs.TryGetNeed<Need_Suppression>();
                if (suppression != null)
                {
                    // Low suppression = risk of rebellion, less effective work
                    // Suppression ranges from 0 (rebellious) to 1 (fully suppressed)
                    // We want minimum efficiency even at 0 suppression
                    float suppressionMultiplier = MIN_SUPPRESSION_EFFICIENCY + (suppression.CurLevel * SUPPRESSION_EFFICIENCY_RANGE);
                    payment *= suppressionMultiplier;
                }
            }

            // Factor 5: Ritual quality affects motivation/efficiency
            // Check the hearing record to see what outcome they got
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(slave);
            if (criminalRecord?.Hearing != null)
            {
                float ritualMultiplier = 1.0f;

                switch (criminalRecord.Hearing.pleaBargainOutcome)
                {
                    case PleaBargainOutcome.Excellent:
                        ritualMultiplier = REPAYMENT_MULTIPLIER_EXCELLENT;
                        break;
                    case PleaBargainOutcome.Standard:
                        ritualMultiplier = REPAYMENT_MULTIPLIER_STANDARD;
                        break;
                    case PleaBargainOutcome.Partial:
                        ritualMultiplier = REPAYMENT_MULTIPLIER_PARTIAL;
                        break;
                    case PleaBargainOutcome.Poor:
                        ritualMultiplier = REPAYMENT_MULTIPLIER_NODEAL;
                        break;
                }

                payment *= ritualMultiplier;

                #if DEBUG
                if (ritualMultiplier != 1.0f)
                {
                    Mod.Log?.Message($"{slave.LabelShort}: Ritual quality multiplier = {ritualMultiplier:F2}x");
                }
                #endif
            }

            return payment;
        }

        /// <summary>
        /// Handle what happens when a slave fully pays off their debt
        /// Can be called externally (e.g., when pardoning)
        /// </summary>
        public void HandleDebtFullyPaid(Pawn slave)
        {
            if (slave == null || !slave.IsSlaveOfColony)
            {
                return;
            }

            var debtRecord = DebtUtils.TryGetDebtRecord(slave);
            if (debtRecord == null)
            {
                return;
            }

            // Send notification to player
            Messages.Message(
                $"{slave.LabelShort} has fully paid off their debt through labor. They will be released in 10 days unless you intervene.",
                slave,
                MessageTypeDefOf.PositiveEvent
            );

            // Log the event
            Mod.Log?.Message($"{slave.LabelShort} has completed debt repayment");

            // Apply positive mood thought for completing debt
            slave.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.Catharsis);

            // Auto-queue emancipation (one-time only)
            if (!debtRecord.EmancipationQueued)
            {
                AutoQueueEmancipation(slave, debtRecord);
            }
        }

        /// <summary>
        /// Automatically sets the slave to be emancipated or recruited based on preference
        /// </summary>
        private void AutoQueueEmancipation(Pawn slave, Hediff_Debt debtRecord)
        {
            if (slave?.guest == null)
                return;

            debtRecord.EmancipationQueued = true;

            // Check what action to take
            if (debtRecord.PostDebtAction == Hediffs.PostDebtAction.Recruit)
            {
                // Convert slave to prisoner and set to recruit mode
                slave.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                slave.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.AttemptRecruit);

                Messages.Message(
                    $"{slave.LabelShort} has been converted to a prisoner and set to recruitment. Wardens will attempt to recruit them.",
                    slave,
                    MessageTypeDefOf.NeutralEvent
                );

                #if DEBUG
                Mod.Log?.Message($"Auto-converted {slave.LabelShort} to prisoner for recruitment");
                #endif
            }
            else
            {
                // Set emancipate flag (default)
                slave.guest.slaveInteractionMode = SlaveInteractionModeDefOf.Emancipate;

                Messages.Message(
                    $"{slave.LabelShort} has been queued for emancipation. They will be freed once a warden is available.",
                    slave,
                    MessageTypeDefOf.NeutralEvent
                );

                #if DEBUG
                Mod.Log?.Message($"Auto-queued {slave.LabelShort} for emancipation");
                #endif
            }
        }

        /// <summary>
        /// Called when a debt-free slave is finally emancipated
        /// Improves faction relations based on how timely the release was
        /// </summary>
        public void OnDebtorEmancipated(Pawn freedPawn, int daysOverdue)
        {
            if (freedPawn?.Faction == null)
                return;

            Faction faction = freedPawn.Faction;

            if (faction.IsPlayer || faction.defeated)
                return;

            int relationChange = 0;

            if (daysOverdue <= 2)
            {
                // Released promptly
                relationChange = 15;
            }
            else if (daysOverdue <= 10)
            {
                // Released within grace period
                relationChange = 10;
            }
            else if (daysOverdue <= 20)
            {
                // Released late
                relationChange = 5;
            }
            else
            {
                // Held for a very long time
                relationChange = -5;
            }

            if (relationChange != 0)
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, relationChange,
                    canSendMessage: true, canSendHostilityLetter: false,
                    reason: null); // Pass null for HistoryEventDef

                #if DEBUG
                Mod.Log?.Message($"Faction {faction.Name} relation changed by {relationChange} for releasing {freedPawn.LabelShort} ({daysOverdue} days overdue)");
                #endif
            }

            // Give colonists mood buff for doing the right thing (if released reasonably on time)
            if (daysOverdue <= 10)
            {
                foreach (Pawn colonist in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
                {
                    if (colonist.needs?.mood?.thoughts?.memories != null)
                    {
                        ThoughtDef releasedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_ReleasedDebtor");
                        if (releasedThought != null)
                        {
                            colonist.needs.mood.thoughts.memories.TryGainMemory(releasedThought);
                        }
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
            // Note: pendingEnslavements is intentionally not saved - it's a temporary queue
            // If game is saved/loaded mid-ritual, the enslavement just won't happen
            // This is acceptable as it's an edge case
        }
    }
}
