using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Alerts
{
    /// <summary>
    /// Alert shown when enslaved pawns have paid their debt.
    /// Shows as a reminder during grace period, warning after grace period expires.
    /// </summary>
    public class Alert_UnreleasedDebtors : Alert
    {
        private const int GRACE_PERIOD_DAYS = 10;

        private List<Pawn> unreleasedDebtorsResult = new List<Pawn>();

        public Alert_UnreleasedDebtors()
        {
            defaultLabel = "Debt-free slaves";
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
                        // Show all debt-free slaves (both in grace period and overdue)
                        if (debtRecord != null && (debtRecord.IsInGracePeriod || debtRecord.IsOverdueForRelease))
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
            // Only show alert if game has started properly
            if (Current.Game == null || Find.CurrentMap == null)
                return false;

            List<Pawn> pawns = UnreleasedDebtors;
            return AlertReport.CulpritsAre(pawns);
        }

        public override TaggedString GetExplanation()
        {
            List<Pawn> pawns = UnreleasedDebtors;

            var overduePawns = new List<Pawn>();
            var gracePeriodPawns = new List<Pawn>();

            foreach (var p in pawns)
            {
                var debtRecord = DebtUtils.TryGetDebtRecord(p);
                if (debtRecord != null)
                {
                    if (debtRecord.IsOverdueForRelease)
                    {
                        overduePawns.Add(p);
                    }
                    else if (debtRecord.IsInGracePeriod)
                    {
                        gracePeriodPawns.Add(p);
                    }
                }
            }

            string explanation = "";

            if (overduePawns.Count > 0)
            {
                string overdueList = string.Join("\n", overduePawns.Select(p =>
                    $"  - {p.LabelShort} (debt paid {p.GetDebtDaysOverdue()} days ago - OVERDUE)"));

                explanation += $"These slaves have been debt-free longer than the {GRACE_PERIOD_DAYS}-day grace period:\n\n{overdueList}\n\n" +
                              "Holding them enslaved is now causing:\n" +
                              "  - Colonist mood penalty (-3)\n" +
                              "  - Slave mood penalty (-6)\n" +
                              "  - Harm to faction relations\n" +
                              "  - Increased rebellion risk\n\n";
            }

            if (gracePeriodPawns.Count > 0)
            {
                string gracePeriodList = string.Join("\n", gracePeriodPawns.Select(p =>
                {
                    int daysSincePaid = p.GetDebtDaysOverdue();
                    int daysRemaining = GRACE_PERIOD_DAYS - daysSincePaid;
                    return $"  - {p.LabelShort} ({daysRemaining} days remaining)";
                }));

                explanation += $"These slaves are within the grace period:\n\n{gracePeriodList}\n\n";
            }

            explanation += "Emancipate them through the slave tab or they will be automatically freed.";

            return explanation;
        }
    }
}
