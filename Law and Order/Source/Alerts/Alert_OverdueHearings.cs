using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Alerts
{
    /// <summary>
    /// Alert shown when prisoners with criminal records have not received a hearing.
    /// Shows as a reminder during grace period, warning after grace period expires.
    /// </summary>
    public class Alert_OverdueHearings : Alert
    {
        private const int GRACE_PERIOD_DAYS = 7;

        // Cache to avoid checking every tick
        private const int CACHE_DURATION_TICKS = 250; // ~4 seconds at normal speed
        private List<Pawn> prisonersAwaitingHearingResult = new List<Pawn>();
        private int lastUpdateTick = -999999;

        public Alert_OverdueHearings()
        {
            defaultLabel = "Prisoners awaiting hearing";
            defaultPriority = AlertPriority.Medium;
        }

        private List<Pawn> PrisonersAwaitingHearing
        {
            get
            {
                int currentTick = Find.TickManager.TicksGame;

                // Use cached result if still valid
                if (currentTick - lastUpdateTick < CACHE_DURATION_TICKS)
                {
                    return prisonersAwaitingHearingResult;
                }

                // Cache expired, recalculate
                prisonersAwaitingHearingResult.Clear();

                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome)
                        continue;

                    var prisoners = map.mapPawns.PrisonersOfColonySpawned;

                    foreach (Pawn prisoner in prisoners)
                    {
                        var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);

                        // Skip if no criminal record or no crimes
                        if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
                            continue;

                        // Skip if hearing already completed
                        if (criminalRecord.Hearing.status == HearingStatus.Completed)
                            continue;

                        // Add all prisoners awaiting hearings (regardless of grace period)
                        prisonersAwaitingHearingResult.Add(prisoner);
                    }
                }

                lastUpdateTick = currentTick;
                return prisonersAwaitingHearingResult;
            }
        }

        public override AlertReport GetReport()
        {
            // Only show alert if game has started properly
            if (Current.Game == null || Find.CurrentMap == null)
            {
                return false;
            }

            List<Pawn> pawns = PrisonersAwaitingHearing;
            return AlertReport.CulpritsAre(pawns);
        }

        public override TaggedString GetExplanation()
        {
            List<Pawn> pawns = PrisonersAwaitingHearing;

            var overduePawns = new List<Pawn>();
            var gracePeriodPawns = new List<Pawn>();

            foreach (var p in pawns)
            {
                var criminalRecord = CrimeUtils.TryGetCriminalRecord(p);
                var oldestCrime = criminalRecord?.Crimes.OrderBy(c => c.tickCommitted).FirstOrDefault();
                if (oldestCrime != null)
                {
                    int daysWaiting = (Find.TickManager.TicksGame - oldestCrime.tickCommitted) / GenDate.TicksPerDay;
                    if (daysWaiting >= GRACE_PERIOD_DAYS)
                    {
                        overduePawns.Add(p);
                    }
                    else
                    {
                        gracePeriodPawns.Add(p);
                    }
                }
            }

            string explanation = "";

            if (overduePawns.Count > 0)
            {
                string overdueList = string.Join("\n", overduePawns.Select(p =>
                {
                    var criminalRecord = CrimeUtils.TryGetCriminalRecord(p);
                    var oldestCrime = criminalRecord?.Crimes.OrderBy(c => c.tickCommitted).FirstOrDefault();
                    int daysWaiting = oldestCrime != null
                        ? (Find.TickManager.TicksGame - oldestCrime.tickCommitted) / GenDate.TicksPerDay
                        : 0;
                    return $"  - {p.LabelShort} (waiting {daysWaiting} days - OVERDUE)";
                }));

                explanation += $"These prisoners have been waiting longer than the {GRACE_PERIOD_DAYS}-day grace period:\n\n{overdueList}\n\n" +
                              "Holding prisoners without due process is now causing:\n" +
                              "  - Colonist mood penalty (-3)\n" +
                              "  - Harm to faction relations\n" +
                              "  - Increased rebellion risk\n\n";
            }

            if (gracePeriodPawns.Count > 0)
            {
                string gracePeriodList = string.Join("\n", gracePeriodPawns.Select(p =>
                {
                    var criminalRecord = CrimeUtils.TryGetCriminalRecord(p);
                    var oldestCrime = criminalRecord?.Crimes.OrderBy(c => c.tickCommitted).FirstOrDefault();
                    int daysWaiting = oldestCrime != null
                        ? (Find.TickManager.TicksGame - oldestCrime.tickCommitted) / GenDate.TicksPerDay
                        : 0;
                    int daysRemaining = GRACE_PERIOD_DAYS - daysWaiting;
                    return $"  - {p.LabelShort} ({daysRemaining} days remaining)";
                }));

                explanation += $"These prisoners are within the grace period:\n\n{gracePeriodList}\n\n";
            }

            explanation += "Conduct hearings through the Justice tab.";

            return explanation;
        }
    }
}
