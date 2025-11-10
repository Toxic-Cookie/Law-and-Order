using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
{
    /// <summary>
    /// Colonists feel bad when the colony holds debt-free slaves
    /// </summary>
    public class ThoughtWorker_HoldingDebtFreeSlave : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!p.IsColonist || p.IsSlave)
                return false;

            // Check if colony has any debt-free slaves (excluding those marked for recruitment)
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                    continue;

                foreach (Pawn slave in map.mapPawns.SlavesOfColonySpawned)
                {
                    var debtRecord = DebtUtils.TryGetDebtRecord(slave);
                    // Skip pawns marked for recruitment - they're intentionally being kept to recruit
                    if (debtRecord != null &&
                        debtRecord.IsOverdueForRelease &&
                        debtRecord.PostDebtAction != Law_and_Order.Source.Hediffs.PostDebtAction.Recruit)
                    {
                        return true; // Found at least one
                    }
                }
            }

            return false;
        }
    }
}
