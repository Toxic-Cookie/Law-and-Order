using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Hearings
{
    /// <summary>
    /// Utility class for conducting hearings and plea bargains
    /// </summary>
    public static class HearingUtils
    {
        // Plea bargain chance constants
        private const float BASE_PLEA_CHANCE = 0.40f; // 40% base chance
        private const float SKILL_MODIFIER = 2.5f;     // Each point of skill difference = 2.5%
        private const float MIN_CHANCE = 0.05f;        // 5% minimum
        private const float MAX_CHANCE = 0.95f;        // 95% maximum

        // Plea bargain debt modifiers
        // High quality = harsh sentence, low quality = lenient
        private const float EXCELLENT_PLEA_MODIFIER = 0.10f;     // +10% debt (harsh sentence)
        private const float STANDARD_PLEA_MODIFIER = 0f;         // No change (standard)
        private const float PARTIAL_PLEA_MODIFIER = -0.15f;      // -15% debt (lenient)
        private const float NO_DEAL_MODIFIER = -0.25f;           // -25% debt (very lenient)

        // Dice roll constants (d100: 1-100)
        private const int DICE_MIN = 1;
        private const int DICE_MAX = 101; // Exclusive upper bound
        private const int CRITICAL_FAILURE_THRESHOLD = 5;
        private const int CRITICAL_SUCCESS_THRESHOLD = 96;

        // Trait modifier constants for social skill
        private const int PSYCHOPATH_PENALTY = -4;
        private const int ABRASIVE_PENALTY = -3;
        private const int ANNOYING_VOICE_PENALTY = -2;
        private const int CREEPY_BREATHING_PENALTY = -2;
        private const int KIND_BONUS = 2;
        private const int BEAUTY_BONUS = 2;

        // Health condition modifiers
        private const float SEVERE_PAIN_THRESHOLD = 0.5f;
        private const int SEVERE_PAIN_PENALTY = -3;
        private const float DRUNK_THRESHOLD = 0.5f;
        private const int DRUNK_PENALTY = -5;

        /// <summary>
        /// Calculate the effective social skill for plea bargaining
        /// Includes modifiers from traits and health conditions
        /// </summary>
        public static int GetEffectiveSocialSkill(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            int baseSocialSkill = pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            int modifier = 0;

            // Trait modifiers
            if (pawn.story?.traits != null)
            {
                // Negative modifiers
                if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    modifier += PSYCHOPATH_PENALTY;
                }
                if (pawn.story.traits.HasTrait(TraitDefOf.Abrasive))
                {
                    modifier += ABRASIVE_PENALTY;
                }
                if (pawn.story.traits.HasTrait(TraitDefOf.AnnoyingVoice))
                {
                    modifier += ANNOYING_VOICE_PENALTY;
                }
                if (pawn.story.traits.HasTrait(TraitDefOf.CreepyBreathing))
                {
                    modifier += CREEPY_BREATHING_PENALTY;
                }

                // Positive modifiers
                if (pawn.story.traits.HasTrait(TraitDefOf.Kind))
                {
                    modifier += KIND_BONUS;
                }

                // Check for beauty trait by name since it's not in TraitDefOf
                var beautyTrait = pawn.story?.traits?.allTraits?.FirstOrDefault(t => t.def.defName == "Beauty");
                if (beautyTrait != null && beautyTrait.Degree >= 1) // Pretty or Beautiful
                {
                    modifier += BEAUTY_BONUS;
                }
            }

            // Health condition modifiers
            if (pawn.health?.hediffSet != null)
            {
                // Check for severe pain
                float painLevel = pawn.health.hediffSet.PainTotal;
                if (painLevel >= SEVERE_PAIN_THRESHOLD)
                {
                    modifier += SEVERE_PAIN_PENALTY;
                }

                // Check for intoxication (alcohol/drugs)
                if (pawn.health.hediffSet.HasHediff(HediffDefOf.AlcoholHigh))
                {
                    var alcoholHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AlcoholHigh);
                    if (alcoholHediff.Severity >= DRUNK_THRESHOLD)
                    {
                        modifier += DRUNK_PENALTY;
                    }
                }
            }

            return Mathf.Max(0, baseSocialSkill + modifier);
        }

        /// <summary>
        /// Calculate the chance of a successful plea bargain
        /// </summary>
        public static float CalculatePleaChance(Pawn prisoner, Pawn adjudicator)
        {
            int prisonerSkill = GetEffectiveSocialSkill(prisoner);
            int adjudicatorSkill = GetEffectiveSocialSkill(adjudicator);

            float chance = BASE_PLEA_CHANCE + ((prisonerSkill - adjudicatorSkill) * SKILL_MODIFIER / 100f);

            return Mathf.Clamp(chance, MIN_CHANCE, MAX_CHANCE);
        }

        /// <summary>
        /// Attempt a plea bargain and return the outcome
        /// </summary>
        public static PleaBargainOutcome AttemptPleaBargain(Pawn prisoner, Pawn adjudicator, out int roll)
        {
            float successChance = CalculatePleaChance(prisoner, adjudicator);
            roll = Rand.Range(DICE_MIN, DICE_MAX); // d100: 1-100

            // Poor outcome (1-5) - Incompetent hearing
            if (roll <= CRITICAL_FAILURE_THRESHOLD)
            {
                return PleaBargainOutcome.Poor;
            }

            // Excellent outcome (96-100) - Professional hearing
            if (roll >= CRITICAL_SUCCESS_THRESHOLD)
            {
                return PleaBargainOutcome.Excellent;
            }

            // Standard/Partial outcome based on calculated chance
            // Convert chance to percentage threshold (e.g., 45% = roll must be 56 or higher to succeed)
            int successThreshold = Mathf.RoundToInt((1f - successChance) * 100f);

            if (roll > successThreshold)
            {
                return PleaBargainOutcome.Standard;
            }
            else
            {
                return PleaBargainOutcome.Partial;
            }
        }

        /// <summary>
        /// Apply plea bargain outcome to the prisoner's debt and mood
        /// </summary>
        public static void ApplyPleaBargainOutcome(Pawn prisoner, Pawn adjudicator, PleaBargainOutcome outcome, float currentDebt)
        {
            var debtRecord = DebtUtils.TryGetDebtRecord(prisoner);
            if (debtRecord == null || currentDebt <= 0)
            {
                return;
            }

            float modifier = 0f;
            string reason = "";

            switch (outcome)
            {
                case PleaBargainOutcome.Excellent:
                    modifier = EXCELLENT_PLEA_MODIFIER;
                    reason = "Professional Hearing";
                    // Apply mood to prisoner
                    prisoner.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_PleaImpressive);
                    // Adjudicator is impressed
                    adjudicator?.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_ImpressedByPlea);
                    break;

                case PleaBargainOutcome.Standard:
                    modifier = STANDARD_PLEA_MODIFIER;
                    reason = "Fair Hearing";
                    // Apply mood to prisoner
                    prisoner.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_PleaAccepted);
                    break;

                case PleaBargainOutcome.Partial:
                    modifier = PARTIAL_PLEA_MODIFIER;
                    reason = "Sloppy Hearing";
                    // Apply mood to prisoner
                    prisoner.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_PleaRejected);
                    break;

                case PleaBargainOutcome.Poor:
                    modifier = NO_DEAL_MODIFIER;
                    reason = "Incompetent Hearing";
                    // Apply mood to prisoner
                    prisoner.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_HumiliatedInCourt);
                    break;
            }

            if (modifier < 0)
            {
                // Reduce debt
                float reductionAmount = currentDebt * Mathf.Abs(modifier);
                debtRecord.PayDebt(reductionAmount, reason);
            }
            else if (modifier > 0)
            {
                // Increase debt
                float increaseAmount = currentDebt * modifier;
                debtRecord.AddDebt(increaseAmount, reason);
            }
        }

        /// <summary>
        /// Get the best available adjudicator (colonist with highest social skill)
        /// </summary>
        public static Pawn GetBestAdjudicator(Map map = null)
        {
            if (map == null)
            {
                map = Find.CurrentMap;
            }

            if (map == null)
            {
                return null;
            }

            var colonists = map.mapPawns.FreeColonistsSpawned.Where(p =>
                !p.Dead &&
                !p.Downed &&
                p.skills?.GetSkill(SkillDefOf.Social) != null
            );

            if (!colonists.Any())
            {
                return null;
            }

            return colonists.OrderByDescending(p => GetEffectiveSocialSkill(p)).First();
        }

        /// <summary>
        /// Conduct a formal hearing for a prisoner
        /// </summary>
        public static void ConductHearing(Pawn prisoner, Pawn adjudicator, Room courtroom = null)
        {
            if (prisoner == null || adjudicator == null)
            {
                Mod.Log?.Error("Cannot conduct hearing: prisoner or adjudicator is null");
                return;
            }

            var criminalRecord = CrimeUtils.GetOrCreateCriminalRecord(prisoner);
            if (criminalRecord == null)
            {
                Mod.Log?.Error($"Cannot conduct hearing for {prisoner.NameShortColored}: no criminal record");
                return;
            }

            var hearing = criminalRecord.Hearing;

            // Update hearing status
            hearing.status = HearingStatus.Completed;
            hearing.completedTick = Find.TickManager.TicksGame;
            hearing.adjudicator = adjudicator;
            hearing.courtroom = courtroom;
            hearing.chargesRead = true;
            hearing.debtCalculated = true;
            hearing.sentenced = true;

            // Generate formal charge reading message
            string chargeMessage = GenerateChargeReading(prisoner, criminalRecord, adjudicator);

            // Send message
            Messages.Message(
                chargeMessage,
                prisoner,
                MessageTypeDefOf.NeutralEvent
            );

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Conducted hearing for {prisoner.NameShortColored} with adjudicator {adjudicator.NameShortColored}");
            }
        }

        /// <summary>
        /// Generate a formal reading of charges for the hearing
        /// </summary>
        public static string GenerateChargeReading(Pawn prisoner, Hediff_Crimes criminalRecord, Pawn adjudicator)
        {
            var debtRecord = DebtUtils.TryGetDebtRecord(prisoner);
            float totalDebt = debtRecord?.CurrentDebt ?? 0f;

            List<string> charges = new List<string>();

            // Group crimes by type
            var crimesByType = criminalRecord.Crimes.GroupBy(c => c.crimeType);

            foreach (var group in crimesByType)
            {
                int count = group.Count();
                string crimeName = group.Key.ToString();

                if (count == 1)
                {
                    var crime = group.First();
                    if (crime.victim != null)
                    {
                        charges.Add($"{crimeName} against {crime.victim.LabelShort}");
                    }
                    else
                    {
                        charges.Add(crimeName);
                    }
                }
                else
                {
                    charges.Add($"{crimeName} (x{count})");
                }
            }

            string chargeList = string.Join(", ", charges);

            return $"{adjudicator.LabelShort} reads the charges against {prisoner.LabelShort}: {chargeList}. " +
                   $"Total debt owed: {totalDebt:F0} silver.";
        }

        /// <summary>
        /// Schedule a hearing for a prisoner
        /// </summary>
        public static bool ScheduleHearing(Pawn prisoner, Pawn adjudicator = null, Room courtroom = null)
        {
            if (prisoner == null)
            {
                return false;
            }

            var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);
            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                Messages.Message(
                    $"{prisoner.LabelShort} has no crimes on record.",
                    MessageTypeDefOf.RejectInput
                );
                return false;
            }

            // Auto-select best adjudicator if not provided
            if (adjudicator == null)
            {
                adjudicator = GetBestAdjudicator();
                if (adjudicator == null)
                {
                    Messages.Message(
                        "No suitable colonist available to serve as adjudicator.",
                        MessageTypeDefOf.RejectInput
                    );
                    return false;
                }
            }

            // Auto-select best courtroom if not provided
            if (courtroom == null)
            {
                courtroom = CourtroomUtils.GetBestCourtroom();
            }

            var hearing = criminalRecord.Hearing;
            hearing.status = HearingStatus.Scheduled;
            hearing.scheduledTick = Find.TickManager.TicksGame;
            hearing.adjudicator = adjudicator;
            hearing.courtroom = courtroom;

            Messages.Message(
                $"Hearing scheduled for {prisoner.LabelShort}. Adjudicator: {adjudicator.LabelShort}",
                prisoner,
                MessageTypeDefOf.PositiveEvent
            );

            return true;
        }

        /// <summary>
        /// Get a human-readable description of the hearing quality outcome
        /// </summary>
        public static string GetPleaOutcomeDescription(PleaBargainOutcome outcome)
        {
            switch (outcome)
            {
                case PleaBargainOutcome.Excellent:
                    return "The hearing was conducted with professionalism and efficiency. The judge imposed a full, proper sentence, and the defendant understands they can work it off quickly through motivated labor.";

                case PleaBargainOutcome.Standard:
                    return "The hearing proceeded fairly. The judge handed down a standard sentence that the defendant can accept as just.";

                case PleaBargainOutcome.Partial:
                    return "The hearing was somewhat disorganized. The judge went easy on the sentence, but the whole proceeding left the defendant feeling unmotivated.";

                case PleaBargainOutcome.Poor:
                    return "The hearing was a complete mess. The confused judge handed down a very lenient sentence, but the defendant feels demoralized by the unprofessional proceedings.";

                default:
                    return "No hearing conducted.";
            }
        }

        /// <summary>
        /// Start a hearing ritual for the given prisoner
        /// </summary>
        public static void StartHearingRitual(Pawn prisoner, Room courtroom = null, Pawn judge = null)
        {
            if (prisoner == null)
            {
                Law_and_Order.Source.Mod.Log?.Error("Cannot start hearing ritual: prisoner is null");
                return;
            }

            // Validate prisoner has crimes
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);
            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                Messages.Message(
                    $"{prisoner.LabelShort} has no crimes on record.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            // Auto-select best courtroom if not provided
            if (courtroom == null)
            {
                courtroom = CourtroomUtils.GetBestCourtroom();
                if (courtroom == null)
                {
                    Messages.Message(
                        "No suitable courtroom available. Designate judge and defendant seats in a room.",
                        MessageTypeDefOf.RejectInput
                    );
                    return;
                }
            }

            // Find the Judge's Bench in the courtroom
            Building judgesBench = null;
            foreach (IntVec3 cell in courtroom.Cells)
            {
                foreach (Thing thing in cell.GetThingList(prisoner.Map))
                {
                    Building building = thing as Building;
                    if (building != null)
                    {
                        var comp = building.TryGetComp<Law_and_Order.Source.Buildings.Comp_JudgesBench>();
                        if (comp != null && comp.IsDesignatedAsJudgesBench)
                        {
                            judgesBench = building;
                            break;
                        }
                    }
                }
                if (judgesBench != null)
                {
                    break;
                }
            }

            // If no Judge's Bench found, inform the user
            if (judgesBench == null)
            {
                Messages.Message(
                    "No Judge's Bench found in courtroom. Designate a table as Judge's Bench by right-clicking it.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            // Use the Judge's Bench as the ritual target
            TargetInfo target = new TargetInfo(judgesBench);

            // Prepare forced role assignments
            Dictionary<string, Pawn> forcedRoles = new Dictionary<string, Pawn>();
            forcedRoles["defendant"] = prisoner;

            if (judge != null)
            {
                forcedRoles["judge"] = judge;
            }

            // Get or create the hearing ritual
            Precept_Ritual ritual = GetOrCreateHearingRitual();
            if (ritual == null)
            {
                Messages.Message(
                    "Unable to initialize hearing ritual. Check mod installation.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            // Get the outcome effect def
            var outcomeEffectDef = ritual?.outcomeEffect?.def ?? Law_and_Order.Source.LawAndOrder_RitualDefOf.LawAndOrder_CourtHearingOutcome;

            // Create the action callback that actually starts the ritual
            Dialog_BeginRitual.ActionCallback actionCallback = delegate(RitualRoleAssignments assignments)
            {
                Law_and_Order.Source.Mod.Log?.Message($"Starting hearing ritual for {prisoner.LabelShort} with {assignments.Participants.Count()} participants");

                // Check if ritual can start
                string canStartError = ritual.behavior.CanStartRitualNow(target, ritual, null, assignments.ForcedRolesForReading);
                if (canStartError != null)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Cannot start ritual: {canStartError}");
                    Messages.Message($"Cannot start hearing: {canStartError}", MessageTypeDefOf.RejectInput);
                    return false;
                }

                // Start the ritual
                ritual.behavior.TryExecuteOn(target, judge, ritual, null, assignments, true);

                Law_and_Order.Source.Mod.Log?.Message($"Ritual started for {prisoner.LabelShort}");

                return true;
            };

            // Start the ritual using Dialog_BeginRitual
            Law_and_Order.Source.Mod.Log?.Message($"Opening ritual dialog for {prisoner.LabelShort}");

            Find.WindowStack.Add(new Dialog_BeginRitual(
                "Court Hearing",           // ritualLabel
                ritual,                    // ritual
                target,                    // target
                prisoner.Map,              // map
                actionCallback,            // action callback
                judge,                     // organizer
                null,                      // obligation
                null,                      // filter
                "Begin".Translate(),       // okButtonText
                null,                      // requiredPawns
                forcedRoles,               // forcedForRole
                outcomeEffectDef,          // outcome
                null,                      // extraInfoText
                null                       // selectedPawn
            ));
        }

        /// <summary>
        /// Get or create a hearing ritual
        /// </summary>
        private static Precept_Ritual GetOrCreateHearingRitual()
        {
            // Check if we have an ideology system
            if (Find.IdeoManager == null || Faction.OfPlayer?.ideos == null)
            {
                return null;
            }

            var primaryIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
            if (primaryIdeo == null)
            {
                return null;
            }

            // Look for existing hearing ritual in the primary ideology
            foreach (var precept in primaryIdeo.PreceptsListForReading)
            {
                if (precept is Precept_Ritual ritual &&
                    ritual.def != null &&
                    ritual.def == Law_and_Order.Source.LawAndOrder_RitualDefOf.LawAndOrder_CourtHearing)
                {
                    return ritual;
                }
            }

            // Get the precept def
            var preceptDef = Law_and_Order.Source.LawAndOrder_RitualDefOf.LawAndOrder_CourtHearing;
            if (preceptDef == null)
            {
                return null;
            }

            // Create a new ritual using PreceptMaker
            Precept_Ritual newRitual = (Precept_Ritual)PreceptMaker.MakePrecept(preceptDef);
            newRitual.Init(primaryIdeo, null);

            // Manually initialize behavior and outcomeEffect from the pattern
            var pattern = Law_and_Order.Source.LawAndOrder_RitualDefOf.LawAndOrder_CourtHearingPattern;
            if (pattern != null)
            {
                if (pattern.ritualBehavior != null)
                {
                    newRitual.behavior = pattern.ritualBehavior.GetInstance();
                }

                if (pattern.ritualOutcomeEffect != null)
                {
                    newRitual.outcomeEffect = pattern.ritualOutcomeEffect.GetInstance();
                }

                newRitual.sourcePattern = pattern;
            }

            primaryIdeo.AddPrecept(newRitual, false);

            return newRitual;
        }

        /// <summary>
        /// Check if a hearing ritual can be started for the given prisoner
        /// </summary>
        public static bool CanStartHearingRitual(Pawn prisoner, out string reason)
        {
            reason = null;

            if (prisoner == null)
            {
                reason = "Prisoner is null";
                return false;
            }

            if (!prisoner.IsPrisonerOfColony)
            {
                reason = $"{prisoner.LabelShort} is not a prisoner";
                return false;
            }

            var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);
            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                reason = $"{prisoner.LabelShort} has no crimes on record";
                return false;
            }

            var courtroom = CourtroomUtils.GetBestCourtroom();
            if (courtroom == null)
            {
                reason = "No suitable courtroom available";
                return false;
            }

            return true;
        }
    }
}
