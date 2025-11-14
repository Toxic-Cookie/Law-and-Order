// TODO Phase 1: Rebuild this inspector tab to show crime states and evidence
// This is a minimal stub to allow the mod to compile

using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.UI
{
    // Minimal stub - will be rebuilt in Phase 1
    public class ITab_Pawn_Judiciary : ITab
    {
        public ITab_Pawn_Judiciary()
        {
            this.size = new UnityEngine.Vector2(500f, 400f);
            this.labelKey = "LawAndOrder_TabJudiciary";
        }

        protected override void FillTab()
        {
            var hediff = CrimeUtils.TryGetCriminalRecord(SelPawn);

            UnityEngine.Rect rect = new UnityEngine.Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            if (hediff == null || hediff.TotalCrimeCount == 0)
            {
                Widgets.Label(rect, "No crimes on record.");
            }
            else
            {
                Widgets.Label(rect, $"Total crimes: {hediff.TotalCrimeCount}\n\n(Full UI will be added in Phase 1)");
            }
        }
    }
}
