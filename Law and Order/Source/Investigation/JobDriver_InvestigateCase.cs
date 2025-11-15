using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// JobDriver for wardens to interrogate prisoners at interrogation tables.
    /// Requires a designated interrogation table.
    /// </summary>
    public class JobDriver_InvestigateCase : JobDriver
    {
        private const int INTERROGATION_DURATION_TICKS = 1200; // ~20 seconds base
        private const int WARMUP_TICKS = 120; // ~2 seconds to get started

        // Job targets
        protected Pawn Prisoner => (Pawn)job.targetA.Thing;
        protected Thing InterrogationTable => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the prisoner
            if (!pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            // Reserve the interrogation table
            if (!pawn.Reserve(InterrogationTable, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions
            this.FailOnDespawnedOrNull(TargetIndex.A); // Prisoner despawned
            this.FailOnDespawnedOrNull(TargetIndex.B); // Table despawned
            this.FailOnMentalState(TargetIndex.A); // Prisoner mental break
            this.FailOnNotAwake(TargetIndex.A); // Prisoner asleep
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.PrisonerIsSecure);
            this.FailOn(() => !InterrogationSystem.CanInterrogate(Prisoner));

            // Check if table is still designated
            this.FailOn(() =>
            {
                var comp = InterrogationTable.TryGetComp<Comp_InterrogationTable>();
                return comp == null || !comp.IsDesignated;
            });

            // Step 1: Go to interrogation table (simpler - prisoner stays in cell)
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // Step 2: Go to prisoner
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // Step 3: Warm up - warden prepares for interrogation
            Toil warmup = new Toil();
            warmup.initAction = delegate
            {
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.Standing;
            };
            warmup.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Prisoner);
            };
            warmup.defaultCompleteMode = ToilCompleteMode.Delay;
            warmup.defaultDuration = WARMUP_TICKS;
            warmup.WithProgressBarToilDelay(TargetIndex.A);
            warmup.socialMode = RandomSocialMode.Off;
            yield return warmup;

            // Step 4: Perform interrogation
            yield return PerformInterrogation();

            // Step 5: Return prisoner to cell (or leave them at table for warden AI to handle)
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
        }

        /// <summary>
        /// Toil to escort the prisoner to the interrogation table
        /// </summary>
        private Toil EscortPrisonerToTable()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Make prisoner follow warden
                if (Prisoner.CurJob != null && Prisoner.CurJob.def == JobDefOf.Wait_Downed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                // Take control of prisoner
                Job followJob = JobMaker.MakeJob(JobDefOf.FollowClose, pawn);
                Prisoner.jobs.StartJob(followJob, JobCondition.InterruptForced);
            };

            toil.AddFinishAction(delegate
            {
                // Stop prisoner following
                if (Prisoner.CurJob != null && Prisoner.CurJob.def == JobDefOf.FollowClose)
                {
                    Prisoner.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            });

            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.B);

            // Go to interaction cell near table
            toil.initAction += delegate
            {
                pawn.pather.StartPath(InterrogationTable.InteractionCell, PathEndMode.OnCell);
            };

            return toil;
        }

        /// <summary>
        /// Toil to perform the actual interrogation
        /// </summary>
        private Toil PerformInterrogation()
        {
            Toil toil = new Toil();

            toil.initAction = delegate
            {
                // Position both pawns
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.Standing;

                // Calculate duration based on social skill
                int socialSkill = pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
                int duration = INTERROGATION_DURATION_TICKS - (socialSkill * 30); // -30 ticks per skill level
                duration = Mathf.Max(duration, 600); // Minimum 10 seconds

                toil.defaultDuration = duration;
            };

            toil.tickAction = delegate
            {
                // Face each other during interrogation
                pawn.rotationTracker.FaceTarget(Prisoner);
                Prisoner.rotationTracker.FaceTarget(pawn);

                // Play interaction occasionally
                if (Find.TickManager.TicksGame % 180 == 0)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            };

            toil.AddFinishAction(delegate
            {
                // Perform the interrogation when toil completes
                InterrogationResult result = InterrogationSystem.PerformInterrogation(
                    pawn,
                    Prisoner,
                    InterrogationTable
                );

                // Show result message
                ShowInterrogationResult(result);

                // Grant social XP to warden
                pawn.skills?.Learn(SkillDefOf.Social, 150f);
            });

            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.socialMode = RandomSocialMode.Off;
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.B);

            return toil;
        }

        /// <summary>
        /// Display the interrogation result to the player
        /// </summary>
        private void ShowInterrogationResult(InterrogationResult result)
        {
            if (result.wasSuccessful && result.crimesConfessed.Count > 0)
            {
                // Success - crimes confessed
                string crimeList = string.Join(", ", result.crimesConfessed.ConvertAll(c => c.GetCrimeLabel()));

                Messages.Message(
                    "LawAndOrder_Interrogation_Success".Translate(
                        pawn.LabelShort,
                        Prisoner.LabelShort,
                        result.crimesConfessed.Count,
                        crimeList
                    ),
                    Prisoner,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else if (result.falseConfession)
            {
                // False confession
                Messages.Message(
                    "LawAndOrder_Interrogation_FalseConfession".Translate(
                        pawn.LabelShort,
                        Prisoner.LabelShort
                    ),
                    Prisoner,
                    MessageTypeDefOf.NeutralEvent
                );
            }
            else
            {
                // Failure
                Messages.Message(
                    result.failureReason.Translate(pawn.LabelShort, Prisoner.LabelShort),
                    Prisoner,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }
    }
}
