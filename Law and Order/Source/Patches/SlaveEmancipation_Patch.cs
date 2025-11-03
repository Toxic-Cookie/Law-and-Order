using HarmonyLib;
using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Components;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Detect when slaves are emancipated to trigger relation changes
    /// </summary>
    [HarmonyPatch(typeof(GenGuest), "SlaveRelease")]
    public static class SlaveEmancipation_Patch
    {
        static void Postfix(Pawn p)
        {
            if (p == null)
                return;

            try
            {
                var debtRecord = DebtUtils.TryGetDebtRecord(p);
                if (debtRecord != null && debtRecord.TicksSinceDebtPaid > 0)
                {
                    // This was a debtor being released
                    int daysOverdue = debtRecord.TicksSinceDebtPaid / GenDate.TicksPerDay;

                    var worldComp = Find.World.GetComponent<WorldComponent_DebtManager>();
                    worldComp?.OnDebtorEmancipated(p, daysOverdue);
                }
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in SlaveEmancipation_Patch: {e}");
            }
        }
    }
}
