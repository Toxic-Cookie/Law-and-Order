using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Rituals;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Adds the courtroom role designation gizmo to chairs
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetGizmos))]
    public static class Thing_GetGizmos_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, ref IEnumerable<Gizmo> __result)
        {
            // Check if this is sittable furniture
            if (__instance.def?.building?.isSittable != true)
            {
                return;
            }

            // Only add if the player owns this furniture
            if (__instance.Faction != Faction.OfPlayer)
            {
                return;
            }

            // Try to get the comp
            var comp = __instance.TryGetComp<CompCourtroomChair>();

            if (comp == null)
            {
                // Debug: Log when a chair doesn't have the comp
                if (Prefs.DevMode && __instance is ThingWithComps thingWithComps)
                {
                    Mod.Log?.Warning($"Chair {__instance.def.defName} is missing CompCourtroomChair. " +
                                   $"Comps: {string.Join(", ", thingWithComps.AllComps.Select(c => c.GetType().Name))}");
                }
                return;
            }

            // Add our custom gizmo
            var gizmos = new List<Gizmo>(__result);
            gizmos.Add(new Gizmo_DesignateChairRole(__instance, comp));
            __result = gizmos;

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Added courtroom gizmo to {__instance.def.defName}");
            }
        }
    }
}
