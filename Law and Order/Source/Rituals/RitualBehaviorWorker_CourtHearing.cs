using RimWorld;
using Verse;
using Verse.AI.Group;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
{
    /// <summary>
    /// Ritual behavior worker for court hearings using Custom Ritual Framework.
    /// Integrates CRF outcomes with the Law and Order debt/criminal systems.
    /// </summary>
    public class RitualBehaviorWorker_CourtHearing : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_CourtHearing()
        {
        }

        public RitualBehaviorWorker_CourtHearing(RitualBehaviorDef def) : base(def)
        {
        }

        /// <summary>
        /// Called after ritual completes - integrate with debt/criminal systems
        /// </summary>
        public override void PostCleanup(LordJob_Ritual ritual)
        {
            base.PostCleanup(ritual);

            Pawn judge = ritual.PawnWithRole("judge");
            Pawn defendant = ritual.PawnWithRole("defendant");

            if (defendant == null)
            {
                Law_and_Order.Source.Mod.Log?.Warning("Court hearing completed but defendant is null");
                return;
            }

            // Determine outcome based on which CRF memory/thought was applied
            // CRF already applied the memories based on ritual quality
            PleaBargainOutcome pleaOutcome = DetermineOutcomeFromMemories(defendant);

            Law_and_Order.Source.Mod.Log?.Message($"Plea outcome: {pleaOutcome}");

            // Get current debt
            var debtRecord = DebtUtils.TryGetDebtRecord(defendant);
            float currentDebt = debtRecord?.CurrentDebt ?? 0f;

            // Apply plea bargain outcome (debt modifications and thoughts)
            if (currentDebt > 0)
            {
                HearingUtils.ApplyPleaBargainOutcome(defendant, judge, pleaOutcome, currentDebt);

                // Send message about the outcome
                string outcomeDesc = GetPleaOutcomeDescription(pleaOutcome);
                Messages.Message(
                    $"{defendant.LabelShort}'s plea bargain: {outcomeDesc}",
                    defendant,
                    MessageTypeDefOf.NeutralEvent
                );
            }

            // Mark hearing as completed in criminal record
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(defendant);
            if (criminalRecord != null)
            {
                var hearing = criminalRecord.Hearing;
                hearing.status = HearingStatus.Completed;
                hearing.completedTick = Find.TickManager.TicksGame;
                hearing.adjudicator = judge;
                hearing.courtroom = ritual.selectedTarget.Cell.GetRoom(ritual.Map);
                hearing.chargesRead = true;
                hearing.debtCalculated = true;
                hearing.sentenced = true;
                hearing.pleaBargainOutcome = pleaOutcome;
                hearing.debtBeforePlea = currentDebt;
                hearing.debtAfterPlea = debtRecord?.CurrentDebt ?? 0f;

                Law_and_Order.Source.Mod.Log?.Message($"Hearing marked as completed for {defendant.LabelShort}");
            }
        }

        /// <summary>
        /// Determine plea outcome based on which CRF memory was applied
        /// </summary>
        private PleaBargainOutcome DetermineOutcomeFromMemories(Pawn defendant)
        {
            if (defendant?.needs?.mood?.thoughts?.memories == null)
            {
                return PleaBargainOutcome.Failure; // Default to no change
            }

            var memories = defendant.needs.mood.thoughts.memories.Memories;

            // Check which CRF ritual memory was applied (in reverse order of severity)
            if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingExcellentPlea"))
            {
                return PleaBargainOutcome.CriticalSuccess; // Best outcome
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingStandardPlea"))
            {
                return PleaBargainOutcome.Success; // Good outcome
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingPartialDeal"))
            {
                return PleaBargainOutcome.Failure; // Neutral outcome
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingNoDeal"))
            {
                return PleaBargainOutcome.CriticalFailure; // Worst outcome
            }

            // Default to neutral if no memory found
            return PleaBargainOutcome.Failure;
        }

        /// <summary>
        /// Get human-readable description of plea outcome
        /// </summary>
        private string GetPleaOutcomeDescription(PleaBargainOutcome outcome)
        {
            switch (outcome)
            {
                case PleaBargainOutcome.CriticalSuccess:
                    return "Impressive plea, debt reduced 25%";
                case PleaBargainOutcome.Success:
                    return "Plea accepted, debt reduced 10%";
                case PleaBargainOutcome.Failure:
                    return "Plea rejected, no debt change";
                case PleaBargainOutcome.CriticalFailure:
                    return "Contempt of court, debt increased 15%";
                default:
                    return "Unknown outcome";
            }
        }
    }
}
