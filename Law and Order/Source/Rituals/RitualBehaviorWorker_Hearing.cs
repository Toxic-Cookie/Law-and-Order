using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
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

        private Area prisonerOriginalArea = null;

        /// <summary>
        /// Override to handle prisoner participation in rituals
        /// </summary>
        public override void TryExecuteOn(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced = false)
        {
            Law_and_Order.Source.Mod.Log?.Message($"=== TryExecuteOn called ===");
            Law_and_Order.Source.Mod.Log?.Message($"Target: {target}, Organizer: {organizer?.LabelShort}, PlayerForced: {playerForced}");

            // Get the defendant (prisoner)
            Pawn defendant = assignments.AssignedPawns("defendant").FirstOrDefault();
            Law_and_Order.Source.Mod.Log?.Message($"Defendant: {defendant?.LabelShort}, IsPrisoner: {defendant?.IsPrisonerOfColony}");

            if (defendant != null && defendant.IsPrisonerOfColony)
            {
                Law_and_Order.Source.Mod.Log?.Message($"Defendant current job: {defendant.CurJobDef?.defName}");
                Law_and_Order.Source.Mod.Log?.Message($"Defendant position: {defendant.Position}, Target: {target.Cell}");
                Law_and_Order.Source.Mod.Log?.Message($"Defendant has lord: {defendant.GetLord() != null}");

                // Store and clear area restrictions so prisoner can reach the courtroom
                prisonerOriginalArea = defendant.playerSettings?.AreaRestrictionInPawnCurrentMap;
                Law_and_Order.Source.Mod.Log?.Message($"Original area: {prisonerOriginalArea?.Label}");

                if (defendant.playerSettings != null)
                {
                    defendant.playerSettings.AreaRestrictionInPawnCurrentMap = null;
                }

                // Force prisoner out of bed/current job so they can be escorted
                // This is critical - prisoners laying in bed can't be reserved
                if (defendant.CurJob != null)
                {
                    Law_and_Order.Source.Mod.Log?.Message($"Ending defendant's current job: {defendant.CurJobDef?.defName}");
                    defendant.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                }

                Law_and_Order.Source.Mod.Log?.Message($"Prisoner {defendant.LabelShort} prepared for escort");
            }

            // Log all participants
            Law_and_Order.Source.Mod.Log?.Message($"Total participants: {assignments.Participants.Count()}");
            foreach (var p in assignments.Participants)
            {
                var role = assignments.RoleForPawn(p, true);
                Law_and_Order.Source.Mod.Log?.Message($"  - {p.LabelShort}: {role?.id ?? "no role"}, Position: {p.Position}");
            }

            // Call base implementation to actually start the ritual
            // Note: We don't need to call WaitInsteadOfEscapingFor() here because:
            // 1. LordJob_Ritual.PrisonerSecure() returns true, marking prisoner as secure
            // 2. Calling WaitInsteadOfEscapingFor() assigns a "Wait" job that conflicts with ritual duties
            // 3. We only need to call it AFTER the ritual ends (in PostCleanup)
            Law_and_Order.Source.Mod.Log?.Message($"Calling base.TryExecuteOn...");
            base.TryExecuteOn(target, organizer, ritual, obligation, assignments, playerForced);
            Law_and_Order.Source.Mod.Log?.Message($"=== TryExecuteOn completed ===");
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
            Law_and_Order.Source.Mod.Log?.Message($"=== Cleanup called ===");

            // Make sure prisoner waits instead of trying to escape
            Pawn defendant = ritual.PawnWithRole("defendant");
            if (defendant != null && defendant.IsPrisonerOfColony)
            {
                defendant.guest?.WaitInsteadOfEscapingFor(2500);
                Law_and_Order.Source.Mod.Log?.Message($"Defendant told to wait during cleanup");
            }
        }

        /// <summary>
        /// Called after ritual completes - apply plea bargain results and return prisoner to cell
        /// </summary>
        public override void PostCleanup(LordJob_Ritual ritual)
        {
            Law_and_Order.Source.Mod.Log?.Message($"=== PostCleanup called ===");

            Pawn judge = ritual.PawnWithRole("judge");
            Pawn defendant = ritual.PawnWithRole("defendant");

            Law_and_Order.Source.Mod.Log?.Message($"Judge: {judge?.LabelShort}, Defendant: {defendant?.LabelShort}");

            if (defendant == null)
            {
                Law_and_Order.Source.Mod.Log?.Warning("Hearing ritual completed but defendant is null");
                return;
            }

            Law_and_Order.Source.Mod.Log?.Message($"Plea bargain attempted: {pleaBargainAttempted}, Outcome: {pleaBargainOutcome}");

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

            // Restore prisoner area restrictions
            if (defendant.IsPrisonerOfColony && prisonerOriginalArea != null && defendant.playerSettings != null)
            {
                defendant.playerSettings.AreaRestrictionInPawnCurrentMap = prisonerOriginalArea;
                Law_and_Order.Source.Mod.Log?.Message($"Restored area restrictions for {defendant.LabelShort}");
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
            prisonerOriginalArea = null;
        }

        private int lastLoggedTick = 0;

        /// <summary>
        /// Called periodically during the ritual
        /// Can be used to trigger plea bargain during the appropriate stage
        /// </summary>
        public override void Tick(LordJob_Ritual ritual)
        {
            base.Tick(ritual);

            // Log ritual progress every 250 ticks (about 4 seconds)
            if (Find.TickManager.TicksGame - lastLoggedTick > 250)
            {
                lastLoggedTick = Find.TickManager.TicksGame;

                Pawn defendant = ritual.PawnWithRole("defendant");
                Pawn judge = ritual.PawnWithRole("judge");

                Law_and_Order.Source.Mod.Log?.Message($"=== Ritual Tick ===");
                Law_and_Order.Source.Mod.Log?.Message($"Current stage: {ritual.StageIndex}/{ritual.Ritual.behavior.def.stages.Count - 1}");
                Law_and_Order.Source.Mod.Log?.Message($"Current toil: {ritual.lord.CurLordToil?.GetType().Name}");
                Law_and_Order.Source.Mod.Log?.Message($"Defendant: {defendant?.LabelShort}, Position: {defendant?.Position}, Job: {defendant?.CurJobDef?.defName}");
                Law_and_Order.Source.Mod.Log?.Message($"Judge: {judge?.LabelShort}, Position: {judge?.Position}, Job: {judge?.CurJobDef?.defName}");

                // Log judge's duty information in detail
                if (judge?.mindState?.duty != null)
                {
                    var duty = judge.mindState.duty;
                    Law_and_Order.Source.Mod.Log?.Message($"Judge duty: {duty.def?.defName}");
                    Law_and_Order.Source.Mod.Log?.Message($"  - focus: {duty.focus} (Thing: {duty.focus.Thing?.LabelShort}, Cell: {duty.focus.Cell})");
                    Law_and_Order.Source.Mod.Log?.Message($"  - focusSecond: {duty.focusSecond} (Thing: {duty.focusSecond.Thing?.LabelShort}, Cell: {duty.focusSecond.Cell})");
                    Law_and_Order.Source.Mod.Log?.Message($"  - focusThird: {duty.focusThird} (Thing: {duty.focusThird.Thing?.LabelShort}, Cell: {duty.focusThird.Cell})");
                }
                else
                {
                    Law_and_Order.Source.Mod.Log?.Message($"Judge has no duty!");
                }

                // Log defendant's duty information
                if (defendant?.mindState?.duty != null)
                {
                    var duty = defendant.mindState.duty;
                    Law_and_Order.Source.Mod.Log?.Message($"Defendant duty: {duty.def?.defName}");
                    Law_and_Order.Source.Mod.Log?.Message($"  - focus: {duty.focus} (Thing: {duty.focus.Thing?.LabelShort}, Cell: {duty.focus.Cell})");
                }
                else
                {
                    Law_and_Order.Source.Mod.Log?.Message($"Defendant has no duty!");
                }

                // Log reservation information
                if (judge != null && defendant != null)
                {
                    bool canReserveDefendant = judge.CanReserve(defendant, 1, -1, null, false);
                    Law_and_Order.Source.Mod.Log?.Message($"Judge can reserve defendant: {canReserveDefendant}");

                    if (!canReserveDefendant)
                    {
                        // Try to find out why
                        var reservedBy = ritual.Map.reservationManager.FirstRespectedReserver(defendant, judge);
                        Law_and_Order.Source.Mod.Log?.Message($"  - Defendant reserved by: {reservedBy?.LabelShort ?? "none"}");
                        Law_and_Order.Source.Mod.Log?.Message($"  - Defendant faction: {defendant.Faction?.Name}");
                        Law_and_Order.Source.Mod.Log?.Message($"  - Defendant is prisoner: {defendant.IsPrisonerOfColony}");
                        Law_and_Order.Source.Mod.Log?.Message($"  - Defendant is downed: {defendant.Downed}");
                        Law_and_Order.Source.Mod.Log?.Message($"  - Defendant can be arrested: {defendant.guest?.CanBeBroughtFood ?? false}");
                        Law_and_Order.Source.Mod.Log?.Message($"  - Judge is warden: {judge.workSettings?.WorkIsActive(WorkTypeDefOf.Warden) ?? false}");
                    }
                }

                Law_and_Order.Source.Mod.Log?.Message($"Pawns in lord: {ritual.lord.ownedPawns.Count}");

                foreach (var pawn in ritual.lord.ownedPawns)
                {
                    Law_and_Order.Source.Mod.Log?.Message($"  - {pawn.LabelShort}: {pawn.Position}, Job: {pawn.CurJobDef?.defName}");
                }
            }

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
