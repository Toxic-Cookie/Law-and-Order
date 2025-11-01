using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Law_and_Order.Source.Hediffs;
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

        // Skill multipliers - skilled work is worth more
        private const float SKILL_MULTIPLIER_LOW = 0.7f;    // Average skill 0-4
        private const float SKILL_MULTIPLIER_MED = 1.0f;    // Average skill 5-9
        private const float SKILL_MULTIPLIER_HIGH = 1.3f;   // Average skill 10-14
        private const float SKILL_MULTIPLIER_EXPERT = 1.6f; // Average skill 15+

        private int tickCounter = 0;

        // Queue for prisoners that need to be enslaved (to handle timing issues with rituals)
        private class PendingEnslavement
        {
            public Pawn prisoner;
            public Pawn warden;
            public float debt;
            public int scheduledTick;
        }
        private List<PendingEnslavement> pendingEnslavements = new List<PendingEnslavement>();

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
                scheduledTick = Find.TickManager.TicksGame + 60 // Wait 1 second (60 ticks)
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
                if (pending.prisoner == null || pending.prisoner.Dead)
                {
                    toRemove.Add(pending);
                    continue;
                }

                // Only enslave if still a prisoner (not already enslaved)
                if (pending.prisoner.IsPrisonerOfColony && !pending.prisoner.IsSlave)
                {
                    // Check if prisoner is still in a ritual or being carried
                    if (pending.prisoner.GetLord() != null)
                    {
                        // Still in a Lord (ritual/event), delay longer
                        pending.scheduledTick = currentTick + 60;
#if DEBUG
                        Mod.Log?.Message($"{pending.prisoner.LabelShort} still in Lord, delaying enslavement");
#endif
                        continue;
                    }

                    // Check if prisoner is being carried or escorted
                    if (pending.prisoner.CurJob != null &&
                        (pending.prisoner.CurJob.def.defName.Contains("Escort") ||
                         pending.prisoner.CarriedBy != null))
                    {
                        // Still being moved, delay longer
                        pending.scheduledTick = currentTick + 60;
#if DEBUG
                        Mod.Log?.Message($"{pending.prisoner.LabelShort} still being moved (job: {pending.prisoner.CurJob?.def?.defName}), delaying enslavement");
#endif
                        continue;
                    }

                    // Check if in bed/cell (ideal state for enslavement)
                    bool inBed = pending.prisoner.CurrentBed() != null;
                    if (!inBed && pending.scheduledTick + 600 > currentTick) // Give up to 10 seconds to reach bed
                    {
                        // Not in bed yet, delay a bit more
                        pending.scheduledTick = currentTick + 60;
#if DEBUG
                        Mod.Log?.Message($"{pending.prisoner.LabelShort} not in bed yet, delaying enslavement");
#endif
                        continue;
                    }

                    // Get warden (use original or find a new one)
                    Pawn warden = pending.warden;
                    if (warden == null || warden.Dead || warden.Downed)
                    {
                        warden = pending.prisoner.Map?.mapPawns.FreeColonists.FirstOrDefault(p => p != null && !p.Dead && !p.Downed);
                    }

                    if (warden != null)
                    {
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
                            Mod.Log?.Warning($"Failed to enslave {pending.prisoner.LabelShort} - TryEnslavePrisoner returned false");
                        }
                    }
                    else
                    {
                        Mod.Log?.Warning($"Cannot enslave {pending.prisoner.LabelShort} - no valid warden found");
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
                    if (avgSkill < 5)
                    {
                        payment *= SKILL_MULTIPLIER_LOW;
                    }
                    else if (avgSkill < 10)
                    {
                        payment *= SKILL_MULTIPLIER_MED;
                    }
                    else if (avgSkill < 15)
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
                        payment *= 0.7f;
                    }
                    else if (degree > 0)
                    {
                        // Hard worker/Industrious - works more effectively
                        payment *= 1.3f;
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
                    // We want minimum 50% efficiency even at 0 suppression
                    float suppressionMultiplier = 0.5f + (suppression.CurLevel * 0.5f);
                    payment *= suppressionMultiplier;
                }
            }

            return payment;
        }

        /// <summary>
        /// Handle what happens when a slave fully pays off their debt
        /// </summary>
        private void HandleDebtFullyPaid(Pawn slave)
        {
            if (slave == null || !slave.IsSlaveOfColony)
            {
                return;
            }

            // Send notification to player
            Messages.Message(
                $"{slave.LabelShort} has fully paid off their debt through labor. They remain enslaved until you choose to free them.",
                slave,
                MessageTypeDefOf.PositiveEvent
            );

            // Log the event
            Mod.Log?.Message($"{slave.LabelShort} has completed debt repayment");

            // Optional: Apply a positive mood thought for completing debt
            slave.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.Catharsis);
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
