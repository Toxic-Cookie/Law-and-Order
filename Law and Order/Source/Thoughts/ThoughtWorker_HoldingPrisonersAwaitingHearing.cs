using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
{
    /// <summary>
    /// Colonists feel bad when the colony holds prisoners awaiting hearings for too long
    /// </summary>
    public class ThoughtWorker_HoldingPrisonersAwaitingHearing : ThoughtWorker
    {
        private const int GRACE_PERIOD_DAYS = 7;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.IsColonist || p.IsPrisoner)
                return false;

            // Check if colony has any prisoners overdue for hearings
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                    continue;

                foreach (Pawn prisoner in map.mapPawns.PrisonersOfColonySpawned)
                {
                    var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);

                    // Skip if no criminal record or no crimes
                    if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
                        continue;

                    // Skip if hearing already completed
                    if (criminalRecord.Hearing.status == HearingStatus.Completed)
                        continue;

                    // Check if first crime is older than grace period
                    var oldestCrime = criminalRecord.Crimes.OrderBy(c => c.tickCommitted).FirstOrDefault();
                    if (oldestCrime == null)
                        continue;

                    int daysSinceFirstCrime = (Find.TickManager.TicksGame - oldestCrime.tickCommitted) / GenDate.TicksPerDay;

                    if (daysSinceFirstCrime >= GRACE_PERIOD_DAYS)
                    {
                        return true; // Found at least one
                    }
                }
            }

            return false;
        }
    }
}
