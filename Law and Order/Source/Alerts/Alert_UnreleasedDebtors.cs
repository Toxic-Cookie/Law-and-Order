using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Alerts
{
    /// <summary>
    /// Alert shown when enslaved pawns have paid their debt and should be released
    /// </summary>
    public class Alert_UnreleasedDebtors : Alert
    {
        private const int GRACE_PERIOD_DAYS = 10;

        private List<Pawn> unreleasedDebtorsResult = new List<Pawn>();

        public Alert_UnreleasedDebtors()
        {
            defaultLabel = "Unreleased debtors";
            defaultPriority = AlertPriority.Medium;
        }

        private List<Pawn> UnreleasedDebtors
        {
            get
            {
                unreleasedDebtorsResult.Clear();

                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome)
                        continue;

                    foreach (Pawn pawn in map.mapPawns.SlavesOfColonySpawned)
                    {
                        var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
                        if (debtRecord != null && debtRecord.IsOverdueForRelease)
                        {
                            unreleasedDebtorsResult.Add(pawn);
                        }
                    }
                }

                return unreleasedDebtorsResult;
            }
        }

        public override AlertReport GetReport()
        {
            if (!Find.PlaySettings.useWorkPriorities) // Only show if colony is established
                return false;

            List<Pawn> pawns = UnreleasedDebtors;
            return AlertReport.CulpritsAre(pawns);
        }

        public override TaggedString GetExplanation()
        {
            List<Pawn> pawns = UnreleasedDebtors;

            string pawnList = string.Join("\n", pawns.Select(p =>
                $"  - {p.LabelShort} (debt paid {p.GetDebtDaysOverdue()} days ago)"));

            return $"These enslaved pawns have fully paid their debt and should be released:\n\n{pawnList}\n\n" +
                   $"The grace period of {GRACE_PERIOD_DAYS} days has expired. Continuing to hold them enslaved will:\n" +
                   "  - Lower colonist mood\n" +
                   "  - Harm faction relations\n" +
                   "  - Increase rebellion risk\n\n" +
                   "Consider emancipating them soon.";
        }
    }
}
