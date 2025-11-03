using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hediffs;

namespace LawAndOrder
{
    /// <summary>
    /// Calculates debt stress thought stage based on current debt
    /// -1 mood per 250 silver
    /// </summary>
    public class ThoughtWorker_DebtStress : ThoughtWorker
    {
        private const float DEBT_PER_STAGE = 250f;

        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Only prisoners and slaves feel debt stress
            if (!p.IsPrisonerOfColony && !p.IsSlaveOfColony)
                return ThoughtState.Inactive;

            var debtRecord = DebtUtils.TryGetDebtRecord(p);
            if (debtRecord == null || debtRecord.CurrentDebt <= 0)
                return ThoughtState.Inactive;

            // Calculate stage based on debt
            // 0-249 = stage 0 (invisible)
            // 250-499 = stage 1 (-1)
            // 500-749 = stage 2 (-2)
            // etc.
            int stage = (int)(debtRecord.CurrentDebt / DEBT_PER_STAGE);

            // Cap at max stage (7 = 1750+ silver)
            if (stage > 7)
                stage = 7;

            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
