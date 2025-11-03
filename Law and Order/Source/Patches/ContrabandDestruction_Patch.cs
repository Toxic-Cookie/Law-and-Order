using HarmonyLib;
using RimWorld;
using Verse;
using LawAndOrder;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Detect when contraband items are destroyed to give colonists a mood buff.
    /// Rewards players for properly disposing of illegal items.
    /// </summary>
    [HarmonyPatch(typeof(Thing), "Destroy")]
    public static class ContrabandDestruction_Patch
    {
        static void Prefix(Thing __instance, DestroyMode mode)
        {
            if (__instance == null)
                return;

            try
            {
                // Only care about player faction items being destroyed
                if (__instance.Faction != Faction.OfPlayer)
                    return;

                // Check if this is contraband
                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null || !contrabandManager.IsContraband(__instance.def))
                    return;

                // Only give buff for intentional destruction (burning, deconstruct, kill finalize)
                // Not for things like deterioration or being consumed
                if (mode != DestroyMode.KillFinalize &&
                    mode != DestroyMode.Deconstruct &&
                    mode != DestroyMode.Vanish)
                    return;

                // Get the map
                Map map = __instance.Map;
                if (map == null)
                    return;

                // Get the thought def
                ThoughtDef destroyedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_DestroyedContraband");
                if (destroyedThought == null)
                    return;

                // Give all colonists on the map a mood buff
                foreach (Pawn colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist.needs?.mood?.thoughts?.memories != null)
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(destroyedThought);
                    }
                }

                #if DEBUG
                Mod.Log?.Message($"Contraband destroyed: {__instance.LabelCap} - colonists gained mood buff");
                #endif
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in ContrabandDestruction_Patch: {e}");
            }
        }
    }
}
