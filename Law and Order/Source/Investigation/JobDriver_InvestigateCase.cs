using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// JobDriver for wardens to interrogate prisoners.
    /// Escorts the prisoner to a designated interrogation table, performs interrogation,
    /// then returns them to their bed.
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
            this.FailOnDestroyedOrNull(TargetIndex.A); // Prisoner despawned
            this.FailOnDestroyedOrNull(TargetIndex.B); // Table despawned
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A); // Prisoner mental break
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.PrisonerIsSecure);

            // Check if table is still designated
            this.FailOn(() =>
            {
                var comp = InterrogationTable.TryGetComp<Comp_InterrogationTable>();
                return comp == null || !comp.IsDesignated;
            });

            // Handle cleanup if job ends prematurely
            this.AddFinishAction(delegate
            {
                if (pawn.carryTracker.CarriedThing != null)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out Thing _, null);
                }
            });

            // Step 1: Go to prisoner
            Toil goToPrisoner = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !InterrogationSystem.CanInterrogate(Prisoner))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            // Skip to returning if already carrying
            yield return Toils_Jump.JumpIf(goToPrisoner, () => pawn.IsCarryingPawn(Prisoner));

            yield return goToPrisoner;

            // Step 2: Pick up the prisoner
            Toil ensureCount = new Toil();
            ensureCount.initAction = delegate
            {
                // Ensure count is set (defensive programming)
                if (job.count <= 0)
                {
                    job.count = 1;
                }
            };
            ensureCount.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return ensureCount;

            Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false, true, false);
            yield return startCarrying;

            // Step 3: Carry prisoner to interrogation table
            Toil goToTable = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOn(() => !pawn.IsCarryingPawn(Prisoner));
            yield return goToTable;

            // Step 4: Put prisoner down near the table
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.B);

            // Step 5: Warm up - warden prepares for interrogation
            Toil warmup = new Toil();
            warmup.initAction = delegate
            {
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.Standing;
            };
            warmup.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Prisoner);
                if (Prisoner.Spawned)
                {
                    Prisoner.rotationTracker.FaceTarget(pawn);
                }
            };
            warmup.defaultCompleteMode = ToilCompleteMode.Delay;
            warmup.defaultDuration = WARMUP_TICKS;
            warmup.WithProgressBarToilDelay(TargetIndex.A);
            warmup.socialMode = RandomSocialMode.Off;
            yield return warmup;

            // Step 6: Perform interrogation
            yield return PerformInterrogation();

            // Step 7: Set last interaction time
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);

            // Step 8: Return prisoner to their bed
            yield return ReturnPrisonerToBed();
        }

        /// <summary>
        /// Toil to return the prisoner to their bed after interrogation
        /// </summary>
        private Toil ReturnPrisonerToBed()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Use the vanilla warden system to return them to bed
                WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(Prisoner, pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
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
