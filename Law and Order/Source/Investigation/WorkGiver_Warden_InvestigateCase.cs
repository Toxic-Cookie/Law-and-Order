using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Law_and_Order.Source.Components;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// WorkGiver for wardens to automatically interrogate prisoners with Hidden crimes.
    /// Integrates with the Warden work type.
    /// </summary>
    public class WorkGiver_Warden_InvestigateCase : WorkGiver_Warden
    {
        // Cooldown tracking to prevent job spam
        private static Dictionary<int, int> lastInterrogationTick = new Dictionary<int, int>();
        private const int INTERROGATION_COOLDOWN_TICKS = 60000; // 1 in-game day between interrogations

        public override Job NonScanJob(Pawn pawn)
        {
            // Don't run this every tick - only check occasionally
            if (Find.TickManager.TicksGame % 250 != 0)
            {
                return null;
            }

            // Get prisoners who can be interrogated
            List<Pawn> interrogatablePrisoners = InterrogationSystem.GetInterrogatablePrisoners(pawn.Map);

            if (interrogatablePrisoners.Count == 0)
            {
                return null;
            }

            // Find a designated interrogation table
            List<Thing> interrogationTables = Comp_InterrogationTable.GetAllInterrogationTables(pawn.Map);

            if (interrogationTables.Count == 0)
            {
                // No interrogation tables available
                return null;
            }

            // Find the best prisoner to interrogate (one with most Hidden crimes)
            Pawn bestPrisoner = FindBestPrisonerToInterrogate(interrogatablePrisoners);

            if (bestPrisoner == null)
            {
                return null;
            }

            // Check cooldown - don't interrogate same prisoner too frequently
            if (lastInterrogationTick.TryGetValue(bestPrisoner.thingIDNumber, out int lastTick))
            {
                if (Find.TickManager.TicksGame - lastTick < INTERROGATION_COOLDOWN_TICKS)
                {
                    return null; // Too soon since last interrogation
                }
            }

            // Find nearest interrogation table to the warden
            Thing table = Comp_InterrogationTable.FindNearestInterrogationTable(pawn, bestPrisoner);

            if (table == null)
            {
                return null;
            }

            // Check if warden can reach both prisoner and table
            if (!pawn.CanReach(bestPrisoner, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return null;
            }

            if (!pawn.CanReach(table, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return null;
            }

            // Mark this prisoner as being interrogated
            lastInterrogationTick[bestPrisoner.thingIDNumber] = Find.TickManager.TicksGame;

            // Create the interrogation job
            Job job = JobMaker.MakeJob(
                JobDefOf_LawAndOrder.LawAndOrder_InvestigateCase,
                bestPrisoner,
                table
            );

            return job;
        }

        /// <summary>
        /// Find the best prisoner to interrogate based on Hidden crime count
        /// </summary>
        private Pawn FindBestPrisonerToInterrogate(List<Pawn> prisoners)
        {
            if (prisoners.Count == 0) return null;

            Pawn best = null;
            int maxHiddenCrimes = 0;

            foreach (Pawn prisoner in prisoners)
            {
                // Get case for this prisoner
                WorldComponent_JusticeManager justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
                if (justiceManager == null) continue;

                var openCase = justiceManager.GetOpenCases().FirstOrDefault(c => c.accused == prisoner);
                if (openCase == null) continue;

                // Count hidden crimes
                int hiddenCrimeCount = openCase.GetAssociatedCrimes()
                    .Count(c => c.visibilityState == Hediffs.CrimeVisibilityState.Hidden);

                if (hiddenCrimeCount > maxHiddenCrimes)
                {
                    maxHiddenCrimes = hiddenCrimeCount;
                    best = prisoner;
                }
            }

            // If no one has hidden crimes in their case, just return the first prisoner
            // (they might have hidden crimes not yet in a case)
            return best ?? prisoners.FirstOrDefault();
        }

        /// <summary>
        /// Should skip this prisoner if they can't be interrogated
        /// </summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // Skip if no interrogation tables on map
            List<Thing> tables = Comp_InterrogationTable.GetAllInterrogationTables(pawn.Map);
            if (tables.Count == 0)
            {
                return true;
            }

            // Skip if no prisoners who can be interrogated
            List<Pawn> interrogatablePrisoners = InterrogationSystem.GetInterrogatablePrisoners(pawn.Map);
            return interrogatablePrisoners.Count == 0;
        }
    }

    /// <summary>
    /// Static class to hold custom JobDef references
    /// </summary>
    [DefOf]
    public static class JobDefOf_LawAndOrder
    {
        public static JobDef LawAndOrder_InvestigateCase;

        static JobDefOf_LawAndOrder()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf_LawAndOrder));
        }
    }
}
