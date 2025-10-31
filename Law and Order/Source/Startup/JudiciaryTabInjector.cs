using System.Linq;
using Verse;
using Law_and_Order.Source.UI;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Startup
{
    /// <summary>
    /// Injects the Judiciary ITab into all humanlike pawn defs at startup
    /// </summary>
    [StaticConstructorOnStartup]
    public static class JudiciaryTabInjector
    {
        static JudiciaryTabInjector()
        {
            // Get our ITab instance
            var judiciaryTab = InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Judiciary));

            // Add to all humanlike ThingDefs
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(d =>
                d.category == ThingCategory.Pawn &&
                d.race?.Humanlike == true))
            {
                if (def.inspectorTabsResolved == null)
                {
                    def.inspectorTabsResolved = new System.Collections.Generic.List<InspectTabBase>();
                }

                // Don't add if already present
                if (!def.inspectorTabsResolved.Any(t => t is ITab_Pawn_Judiciary))
                {
                    def.inspectorTabsResolved.Add(judiciaryTab);

                    if (Prefs.DevMode)
                    {
                        Mod.Log?.Message($"Added Judiciary tab to {def.defName}");
                    }
                }
            }

            Mod.Log?.Message("Judiciary ITab injected into all humanlike pawn defs");
        }
    }
}
