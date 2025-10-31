using System.Linq;
using Verse;
using Law_and_Order.Source.Rituals;

namespace Law_and_Order.Source.Startup
{
    /// <summary>
    /// Debug helper to verify chair comp patching
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CourtroomChairDebug
    {
        static CourtroomChairDebug()
        {
            if (!Prefs.DevMode)
            {
                return;
            }

            // Count how many sittable things have the comp
            var allSittable = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.building?.isSittable == true)
                .ToList();

            var withComp = allSittable
                .Where(d => d.comps?.Any(c => c is CompProperties_CourtroomChair) == true)
                .ToList();

            Mod.Log?.Message($"[Courtroom Debug] Found {allSittable.Count} sittable furniture defs");
            Mod.Log?.Message($"[Courtroom Debug] {withComp.Count} have CompCourtroomChair");

            if (withComp.Count == 0)
            {
                Mod.Log?.Error("[Courtroom Debug] NO CHAIRS HAVE THE COMP! XML patch may have failed.");
                Mod.Log?.Error("[Courtroom Debug] Check if Patches/CourtroomChairComp_Patch.xml is being loaded.");
            }
            else
            {
                Mod.Log?.Message($"[Courtroom Debug] Successfully patched chairs: {string.Join(", ", withComp.Take(5).Select(d => d.defName))}...");
            }
        }
    }
}
