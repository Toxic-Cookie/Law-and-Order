using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// WorkGiver for researchers/wardens to automatically analyze crime scene clues
    /// </summary>
    public class WorkGiver_AnalyzeClue : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        // Use ForUndefined since we provide our own PotentialWorkThingsGlobal
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

        /// <summary>
        /// Get all potential clues that can be analyzed
        /// </summary>
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (pawn?.Map == null)
                yield break;

            // Find all CrimeSceneClue things in the map
            var allThings = pawn.Map.listerThings.AllThings;

            foreach (var thing in allThings)
            {
                if (thing is CrimeSceneClue clue)
                {
                    // Only consider discovered, unanalyzed clues
                    if (clue.discovered && !clue.analyzed)
                    {
                        yield return clue;
                    }
                }
            }
        }

        /// <summary>
        /// Check if pawn has the job for this clue
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is CrimeSceneClue clue))
                return false;

            // Already analyzed
            if (clue.analyzed)
                return false;

            // Not discovered yet
            if (!clue.discovered)
                return false;

            // Check if pawn can reach and reserve
            if (!pawn.CanReserve(t, 1, -1, null, forced))
                return false;

            if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                return false;

            return true;
        }

        /// <summary>
        /// Create the analyze job
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is CrimeSceneClue clue))
                return null;

            if (!HasJobOnThing(pawn, t, forced))
                return null;

            // Create analyze job
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("AnalyzeCrimeClue"), clue);
            return job;
        }
    }
}
