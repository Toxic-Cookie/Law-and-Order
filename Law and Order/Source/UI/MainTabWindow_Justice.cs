// TODO Phase 1: Rebuild this UI with Open Cases, Convictions, Settings tabs
// This is a minimal stub to allow the mod to compile
// The old debt/ritual/penalty UI has been removed

using Verse;
using RimWorld;

namespace Law_and_Order.Source.UI
{
    // Minimal stub - will be rebuilt in Phase 1
    public class MainTabWindow_Justice : MainTabWindow
    {
        public override void DoWindowContents(UnityEngine.Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(inRect, "Law and Order - Phase 0 Cleanup");
            Text.Font = GameFont.Small;

            UnityEngine.Rect textRect = inRect;
            textRect.y += 40f;
            Widgets.Label(textRect, "The Justice UI is being rebuilt.\n\nPhase 0: Cleanup complete\nPhase 1: New state-based system (in progress)\n\nThis tab will show Open Cases and Convictions once Phase 1 is complete.");
        }
    }
}
