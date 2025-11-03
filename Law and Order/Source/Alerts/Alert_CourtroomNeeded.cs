using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Alerts
{
    /// <summary>
    /// Alert shown when prisoners with criminal records are awaiting hearing but no courtroom exists.
    /// </summary>
    public class Alert_CourtroomNeeded : Alert
    {
        // Cache to avoid checking every tick
        private const int CACHE_DURATION_TICKS = 250; // ~4 seconds at normal speed
        private bool hasCourtroom = false;
        private List<Pawn> prisonersAwaitingHearingResult = new List<Pawn>();
        private int lastUpdateTick = -999999;

        public Alert_CourtroomNeeded()
        {
            defaultLabel = "LawAndOrder_Alert_CourtroomNeeded".Translate();
            defaultExplanation = "LawAndOrder_Alert_CourtroomNeededDesc".Translate();
            defaultPriority = AlertPriority.High;
        }

        private void UpdateCache()
        {
            int currentTick = Find.TickManager.TicksGame;

            // Use cached result if still valid
            if (currentTick - lastUpdateTick < CACHE_DURATION_TICKS)
            {
                return;
            }

            // Cache expired, recalculate
            prisonersAwaitingHearingResult.Clear();
            hasCourtroom = false;

            // Check all player home maps
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                    continue;

                // Check if this map has a courtroom
                if (CourtroomUtils.HasCourtroom(map))
                {
                    hasCourtroom = true;
                }

                // Find prisoners awaiting hearings
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

                    // Add prisoners awaiting hearings
                    prisonersAwaitingHearingResult.Add(prisoner);
                }
            }

            lastUpdateTick = currentTick;
        }

        public override AlertReport GetReport()
        {
            // Only show alert if game has started properly
            if (Current.Game == null || Find.CurrentMap == null)
            {
                return false;
            }

            UpdateCache();

            // Only show alert if:
            // 1. There are prisoners awaiting hearing
            // 2. No courtroom exists
            if (prisonersAwaitingHearingResult.Count > 0 && !hasCourtroom)
            {
                return AlertReport.Active;
            }

            return false;
        }

        public override TaggedString GetExplanation()
        {
            UpdateCache();

            string explanation = defaultExplanation;

            if (prisonersAwaitingHearingResult.Count > 0)
            {
                string prisonerList = string.Join("\n", prisonersAwaitingHearingResult.Select(p =>
                {
                    var criminalRecord = CrimeUtils.TryGetCriminalRecord(p);
                    int crimeCount = criminalRecord != null ? criminalRecord.TotalCrimeCount : 0;
                    return $"  - {p.LabelShort} ({crimeCount} crime{(crimeCount != 1 ? "s" : "")})";
                }));

                explanation += $"\n\nPrisoners awaiting hearing:\n\n{prisonerList}";
            }

            return explanation;
        }
    }
}
