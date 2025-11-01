using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
{
    /// <summary>
    /// Ritual behavior worker for court hearings.
    /// Manages the hearing ritual flow and integrates with the plea bargain system.
    /// </summary>
    public class RitualBehaviorWorker_Hearing : RitualBehaviorWorker
    {
        private PleaBargainOutcome? pleaBargainOutcome = null;
        private int pleaRoll = 0;
        private bool pleaBargainAttempted = false;

        public RitualBehaviorWorker_Hearing()
        {
        }

        public RitualBehaviorWorker_Hearing(RitualBehaviorDef def) : base(def)
        {
        }

        /// <summary>
        /// Additional validation for starting a hearing ritual
        /// </summary>
        public override string CanStartRitualNow(TargetInfo target, Precept_Ritual ritual, Pawn selectedPawn = null, Dictionary<string, Pawn> forcedForRole = null)
        {
            string baseCheck = base.CanStartRitualNow(target, ritual, selectedPawn, forcedForRole);
            if (baseCheck != null)
            {
                return baseCheck;
            }

            // Validate courtroom exists and has required seating
            if (target.HasThing)
            {
                return "LawAndOrder_HearingRequiresRoom".Translate();
            }

            Room room = target.Cell.GetRoom(target.Map);
            if (room == null || room.OutdoorsForWork)
            {
                return "LawAndOrder_HearingRequiresIndoorRoom".Translate();
            }

            // Check for minimum required seating
            int judgeSeats = CourtroomUtils.GetSeatCountForRole(room, Law_and_Order.Source.Rituals.CourtroomChairRole.Judge);
            int defendantSeats = CourtroomUtils.GetSeatCountForRole(room, Law_and_Order.Source.Rituals.CourtroomChairRole.Defendant);

            if (judgeSeats < 1)
            {
                return "LawAndOrder_HearingRequiresJudgeSeat".Translate();
            }

            if (defendantSeats < 1)
            {
                return "LawAndOrder_HearingRequiresDefendantSeat".Translate();
            }

            return null;
        }

        /// <summary>
        /// Called when ritual is cleaning up (before PostCleanup)
        /// </summary>
        public override void Cleanup(LordJob_Ritual ritual)
        {
            // Make sure prisoner waits instead of trying to escape
            Pawn defendant = ritual.PawnWithRole("defendant");
            if (defendant != null && defendant.IsPrisonerOfColony)
            {
                defendant.guest?.WaitInsteadOfEscapingFor(2500);
            }
        }

        /// <summary>
        /// Called after ritual completes - apply plea bargain results and return prisoner to cell
        /// </summary>
        public override void PostCleanup(LordJob_Ritual ritual)
        {
            Pawn judge = ritual.PawnWithRole("judge");
            Pawn defendant = ritual.PawnWithRole("defendant");

            if (defendant == null)
            {
                Law_and_Order.Source.Mod.Log?.Warning("Hearing ritual completed but defendant is null");
                return;
            }

            // Apply plea bargain outcome if it occurred during the ritual
            if (pleaBargainAttempted && pleaBargainOutcome.HasValue)
            {
                var debtRecord = DebtUtils.TryGetDebtRecord(defendant);
                float currentDebt = debtRecord?.CurrentDebt ?? 0f;

                HearingUtils.ApplyPleaBargainOutcome(defendant, judge, pleaBargainOutcome.Value, currentDebt);

                // Send message about the outcome
                string outcomeDesc = HearingUtils.GetPleaOutcomeDescription(pleaBargainOutcome.Value);
                Messages.Message(
                    $"{defendant.LabelShort}'s plea bargain: {outcomeDesc}",
                    defendant,
                    MessageTypeDefOf.NeutralEvent
                );
            }

            // Mark hearing as completed
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
            }

            // Return prisoner to cell if they're still a prisoner
            if (defendant.IsPrisonerOfColony && judge != null)
            {
                WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(defendant, judge);
                defendant.guest?.WaitInsteadOfEscapingFor(1250);
            }

            // Reset plea bargain state for next ritual
            pleaBargainAttempted = false;
            pleaBargainOutcome = null;
            pleaRoll = 0;
        }

        /// <summary>
        /// Called periodically during the ritual
        /// Can be used to trigger plea bargain during the appropriate stage
        /// </summary>
        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);

            // The plea bargain will be handled by a specific ritual stage
            // This tick method is here for future enhancements
        }

        /// <summary>
        /// Attempt a plea bargain during the ritual (called by ritual stage)
        /// </summary>
        public void AttemptPleaBargainDuringRitual(LordJob_Ritual ritual)
        {
            if (pleaBargainAttempted)
            {
                return; // Already attempted
            }

            Pawn judge = ritual.PawnWithRole("judge");
            Pawn defendant = ritual.PawnWithRole("defendant");

            if (judge == null || defendant == null)
            {
                return;
            }

            pleaBargainOutcome = HearingUtils.AttemptPleaBargain(defendant, judge, out pleaRoll);
            pleaBargainAttempted = true;

            if (Prefs.DevMode)
            {
                Law_and_Order.Source.Mod.Log?.Message($"Plea bargain attempted: Roll={pleaRoll}, Outcome={pleaBargainOutcome}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pleaBargainAttempted, "pleaBargainAttempted", false);
            Scribe_Values.Look(ref pleaRoll, "pleaRoll", 0);

            // Save/load nullable enum
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                int outcomeInt = pleaBargainOutcome.HasValue ? (int)pleaBargainOutcome.Value : -1;
                Scribe_Values.Look(ref outcomeInt, "pleaBargainOutcome", -1);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                int outcomeInt = -1;
                Scribe_Values.Look(ref outcomeInt, "pleaBargainOutcome", -1);
                pleaBargainOutcome = outcomeInt >= 0 ? (PleaBargainOutcome)outcomeInt : (PleaBargainOutcome?)null;
            }
        }
    }
}
