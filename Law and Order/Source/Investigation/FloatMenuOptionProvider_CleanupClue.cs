using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Provides right-click float menu option to manually clean up fully analyzed clues
    /// Similar to how you can right-click a weapon to equip it
    /// </summary>
    public class FloatMenuOptionProvider_CleanupClue : FloatMenuOptionProvider
    {
        // Allow both drafted and undrafted pawns to clean up clues
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;

        /// <summary>
        /// Check if this provider applies to the current context
        /// </summary>
        protected override bool AppliesInt(FloatMenuContext context)
        {
            return true; // Always apply, we'll filter in GetOptionsFor
        }

        /// <summary>
        /// Get float menu options for the clicked thing (if it's a fully analyzed clue)
        /// </summary>
        public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
        {
            // Check if the clicked thing is a CrimeSceneClue
            if (!(clickedThing is CrimeSceneClue clue))
            {
                yield break;
            }

            // Only show option for fully analyzed clues
            if (!clue.analyzed)
            {
                yield break;
            }

            Pawn pawn = context.FirstSelectedPawn;

            // Check if pawn can reach the clue
            if (!pawn.CanReach(clue, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                yield return new FloatMenuOption(
                    "LawAndOrder_CannotCleanupClue".Translate(clue.clueType.ToString()) + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    0f,
                    null,
                    null,
                    true,
                    0
                );
                yield break;
            }

            // Check if pawn has manipulation capability
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                yield return new FloatMenuOption(
                    "LawAndOrder_CannotCleanupClue".Translate(clue.clueType.ToString()) + ": " + "Incapable".Translate(),
                    null,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    0f,
                    null,
                    null,
                    true,
                    0
                );
                yield break;
            }

            // Create the cleanup option
            FloatMenuOption cleanupOption = new FloatMenuOption(
                "LawAndOrder_CleanupClue".Translate(clue.clueType.ToString()),
                delegate()
                {
                    // Create and queue the cleanup job
                    Job cleanupJob = JobMaker.MakeJob(
                        DefDatabase<JobDef>.GetNamed("LawAndOrder_CleanupClue"),
                        clue
                    );

                    pawn.jobs.TryTakeOrderedJob(cleanupJob, new JobTag?(JobTag.Misc), false);

                    ModLog.Debug($"{pawn.LabelShort} ordered to clean up {clue.clueType} clue");
                },
                MenuOptionPriority.Default,
                null,
                null,
                0f,
                null,
                null,
                true,
                0
            );

            // Decorate with reservation check (similar to PickUpItem)
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                cleanupOption,
                pawn,
                clue,
                "ReservedBy",
                null
            );
        }
    }
}
