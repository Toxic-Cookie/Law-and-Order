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

        private List<Pawn> prisonersAwaitingHearingResult = new List<Pawn>();

        public Alert_OverdueHearings()
        {
            defaultLabel = "Prisoners awaiting hearing";
            defaultPriority = AlertPriority.Medium;
        }

        private List<Pawn> PrisonersAwaitingHearing
        {
            get
            {
                prisonersAwaitingHearingResult.Clear();

                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome)
                        continue;

                    var prisoners = map.mapPawns.PrisonersOfColonySpawned;

                    if (Prefs.DevMode && prisoners.Count > 0)
                    {
                        Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] Checking {prisoners.Count} prisoners");
                    }

                    foreach (Pawn prisoner in prisoners)
                    {
                        var criminalRecord = CrimeUtils.TryGetCriminalRecord(prisoner);

                        if (Prefs.DevMode)
                        {
                            if (criminalRecord == null)
                            {
                                Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] {prisoner.LabelShort}: No criminal record");
                            }
                            else
                            {
                                Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] {prisoner.LabelShort}: {criminalRecord.TotalCrimeCount} crimes, hearing status: {criminalRecord.Hearing.status}");
                            }
                        }

                        // Skip if no criminal record or no crimes
                        if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
                            continue;

                        // Skip if hearing already completed
                        if (criminalRecord.Hearing.status == HearingStatus.Completed)
                            continue;

                        // Add all prisoners awaiting hearings (regardless of grace period)
                        prisonersAwaitingHearingResult.Add(prisoner);

                        if (Prefs.DevMode)
                        {
                            Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] Added {prisoner.LabelShort} to alert list");
                        }
                    }
                }

                if (Prefs.DevMode)
                {
                    Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] Total prisoners awaiting hearing: {prisonersAwaitingHearingResult.Count}");
                }

                return prisonersAwaitingHearingResult;
            }
        }

        public override AlertReport GetReport()
        {
            // Only show alert if game has started properly
            if (Current.Game == null || Find.CurrentMap == null)
            {
                if (Prefs.DevMode)
                {
                    Law_and_Order.Source.Mod.Log?.Message("[Alert_OverdueHearings] Game not initialized, skipping alert");
                }
                return false;
            }

            List<Pawn> pawns = PrisonersAwaitingHearing;

            if (Prefs.DevMode)
            {
                Law_and_Order.Source.Mod.Log?.Message($"[Alert_OverdueHearings] GetReport returning {pawns.Count} pawns");
            }

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
