using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
{
    /// <summary>
    /// Slaves feel bad when their debt is paid but they're still enslaved
    /// </summary>
    public class ThoughtWorker_DebtPaidButEnslaved : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.IsSlave)
                return false;

            var debtRecord = DebtUtils.TryGetDebtRecord(p);
            if (debtRecord != null && debtRecord.IsOverdueForRelease)
            {
                return true;
            }

            return false;
        }
    }
}
