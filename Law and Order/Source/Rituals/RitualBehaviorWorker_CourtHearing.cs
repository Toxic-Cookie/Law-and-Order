using RimWorld;
using Verse;
using Verse.AI.Group;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual behavior worker for court hearings using Custom Ritual Framework.
    /// Integrates CRF outcomes with the Law and Order debt/criminal systems.
    /// </summary>
    public class RitualBehaviorWorker_CourtHearing : RitualBehaviorWorker
    {
        // Track which interactions have been triggered during this ritual
        private bool hasQuestionedDefendant = false;
        private bool hasDefendantPleaded = false;
        private bool hasVictimConfronted = false;

        public RitualBehaviorWorker_CourtHearing()
        {
        }

        public RitualBehaviorWorker_CourtHearing(RitualBehaviorDef def) : base(def)
        {
        }

        /// <summary>
        /// Tick during ritual to trigger interactions at appropriate times
        /// </summary>
        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);

            // Only trigger interactions during stage 1 (the actual hearing)
            if (ritual.StageIndex != 1)
            {
                return;
            }

            float progress = ritual.Progress;
            Pawn judge = ritual.PawnWithRole("judge");
            Pawn defendant = ritual.PawnWithRole("defendant");
            Pawn victim = ritual.PawnWithRole("victim");

            if (judge == null || defendant == null)
            {
                return;
            }

            // 25% progress: Judge questions defendant
            if (progress >= 0.25f && !hasQuestionedDefendant)
            {
                LogInteraction("LawAndOrder_JudgeQuestions", judge, defendant);
                hasQuestionedDefendant = true;
            }

            // 50% progress: Defendant pleads to judge
            if (progress >= 0.50f && !hasDefendantPleaded)
            {
                LogInteraction("LawAndOrder_DefendantPleads", defendant, judge);
                hasDefendantPleaded = true;
            }

            // 75% progress: Victim confronts defendant (if present)
            if (progress >= 0.75f && !hasVictimConfronted && victim != null)
            {
                LogInteraction("LawAndOrder_VictimConfronts", victim, defendant);
                hasVictimConfronted = true;
            }
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

            // Final interaction: Judge sentences defendant
            LogInteraction("LawAndOrder_JudgeSentences", judge, defendant);

            // Determine outcome based on which CRF memory/thought was applied
            // CRF already applied the memories based on ritual quality
            PleaBargainOutcome pleaOutcome = DetermineOutcomeFromMemories(defendant);

#if DEBUG
            Law_and_Order.Source.Mod.Log?.Message($"Plea outcome: {pleaOutcome}");
#endif

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

#if DEBUG
                Law_and_Order.Source.Mod.Log?.Message($"Hearing marked as completed for {defendant.LabelShort}");
#endif
            }

            // Queue prisoner for enslavement if they have remaining debt
            // Use the WorldComponent to handle this on the next tick to avoid timing issues
            float finalDebt = debtRecord?.CurrentDebt ?? 0f;
            if (finalDebt > 0 && defendant.IsPrisonerOfColony && !defendant.IsSlave)
            {
                var worldComp = Find.World.GetComponent<Law_and_Order.Source.Components.WorldComponent_DebtManager>();
                if (worldComp != null)
                {
                    worldComp.QueuePrisonerForEnslavement(defendant, judge, finalDebt);
#if DEBUG
                    Law_and_Order.Source.Mod.Log?.Message($"Queued {defendant.LabelShort} for enslavement with debt of {finalDebt:F0} silver");
#endif
                }
            }
        }

        /// <summary>
        /// Determine hearing quality outcome based on which CRF memory was applied
        /// </summary>
        private PleaBargainOutcome DetermineOutcomeFromMemories(Pawn defendant)
        {
            if (defendant?.needs?.mood?.thoughts?.memories == null)
            {
                return PleaBargainOutcome.Partial; // Default to partial
            }

            var memories = defendant.needs.mood.thoughts.memories.Memories;

            // Check which CRF ritual memory was applied (in reverse order of quality)
            if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingExcellentPlea"))
            {
                return PleaBargainOutcome.Excellent; // Best quality
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingStandardPlea"))
            {
                return PleaBargainOutcome.Standard; // Good quality
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingPartialDeal"))
            {
                return PleaBargainOutcome.Partial; // Partial quality
            }
            else if (memories.Any(m => m.def.defName == "LawAndOrder_CourtHearingNoDeal"))
            {
                return PleaBargainOutcome.Poor; // Poor quality
            }

            // Default to partial if no memory found
            return PleaBargainOutcome.Partial;
        }

        /// <summary>
        /// Get human-readable description of hearing quality outcome
        /// </summary>
        private string GetPleaOutcomeDescription(PleaBargainOutcome outcome)
        {
            switch (outcome)
            {
                case PleaBargainOutcome.Excellent:
                    return "Professional hearing, full sentence imposed (+10% debt, fast repayment)";
                case PleaBargainOutcome.Standard:
                    return "Fair hearing, standard sentence (no debt change, normal repayment)";
                case PleaBargainOutcome.Partial:
                    return "Sloppy hearing, lenient sentence (-15% debt, slow repayment)";
                case PleaBargainOutcome.Poor:
                    return "Incompetent hearing, very lenient sentence (-25% debt, very slow repayment)";
                default:
                    return "Unknown outcome";
            }
        }

        /// <summary>
        /// Helper method to log a single social interaction
        /// </summary>
        private void LogInteraction(string interactionDefName, Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null)
            {
                return;
            }

            InteractionDef interactionDef = DefDatabase<InteractionDef>.GetNamedSilentFail(interactionDefName);
            if (interactionDef == null)
            {
#if DEBUG
                Law_and_Order.Source.Mod.Log?.Warning($"Could not find interaction def: {interactionDefName}");
#endif
                return;
            }

            PlayLogEntry_Interaction entry = new PlayLogEntry_Interaction(
                interactionDef,
                initiator,
                recipient,
                null
            );
            Find.PlayLog.Add(entry);

#if DEBUG
            Law_and_Order.Source.Mod.Log?.Message($"Logged interaction: {initiator.LabelShort} -> {recipient.LabelShort} ({interactionDefName})");
#endif
        }
    }
}
