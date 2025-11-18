using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Job driver for manually cleaning up fully analyzed clues
    /// </summary>
    public class JobDriver_CleanupClue : JobDriver
    {
        private const int CleanupDuration = 120; // 2 seconds (120 ticks)

        private CrimeSceneClue Clue => (CrimeSceneClue)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail if clue despawned or destroyed
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Fail if clue not fully analyzed
            this.FailOn(() => !Clue.analyzed);

            // Go to the clue
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Clean up the clue
            Toil cleanup = new Toil();
            cleanup.initAction = delegate()
            {
                cleanup.actor.jobs.curDriver.ticksLeftThisToil = CleanupDuration;
            };

            cleanup.defaultCompleteMode = ToilCompleteMode.Delay;
            cleanup.defaultDuration = CleanupDuration;

            // Show progress bar
            cleanup.WithProgressBarToilDelay(TargetIndex.A);

            // Play sound effect (use cleaning filth sound)
            cleanup.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);

            yield return cleanup;

            // Final toil - destroy the clue
            Toil finalize = new Toil();
            finalize.initAction = delegate()
            {
                ModLog.Debug($"{pawn.LabelShort} cleaned up {Clue.clueType} clue");

                // Destroy the clue
                Clue.Destroy(DestroyMode.Vanish);

                // Optional: Show message
                Messages.Message(
                    "LawAndOrder_ClueCleanedUp".Translate(pawn.LabelShort.Named("PAWN"), Clue.clueType.Named("CLUETYPE")),
                    pawn,
                    MessageTypeDefOf.TaskCompletion
                );
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return finalize;
        }

        public override string GetReport()
        {
            return "JobReport_CleaningUpClue".Translate(Clue?.clueType.ToString() ?? "clue");
        }
    }
}
