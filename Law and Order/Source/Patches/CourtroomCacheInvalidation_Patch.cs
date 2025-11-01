using HarmonyLib;
using Verse;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Harmony patches to automatically invalidate courtroom cache when room structure changes.
    /// This ensures the cache stays up-to-date when players build or destroy courtroom furniture.
    /// </summary>
    public static class CourtroomCacheInvalidation_Patch
    {
        /// <summary>
        /// Invalidate cache when a building is spawned (constructed/placed)
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
        public static class Thing_SpawnSetup_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Thing __instance, Map map)
            {
                try
                {
                    // Only invalidate for buildings (furniture, walls, etc.)
                    if (__instance is Building)
                    {
                        CourtroomUtils.InvalidateCache(map);
                        ModLog.Trace($"Courtroom cache invalidated for map {map?.uniqueID} due to building spawn: {__instance.def.defName}");
                    }
                }
                catch (System.Exception e)
                {
                    ModLog.Error($"Error in Thing_SpawnSetup_Patch: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Invalidate cache when a building is destroyed/deconstructed
        /// Uses Prefix to capture the map before the thing is despawned
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.DeSpawn))]
        public static class Thing_DeSpawn_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(Thing __instance)
            {
                try
                {
                    // Only invalidate for buildings
                    if (__instance is Building && __instance.Map != null)
                    {
                        Map map = __instance.Map;
                        CourtroomUtils.InvalidateCache(map);
                        ModLog.Trace($"Courtroom cache invalidated for map {map.uniqueID} due to building despawn: {__instance.def.defName}");
                    }
                }
                catch (System.Exception e)
                {
                    ModLog.Error($"Error in Thing_DeSpawn_Patch: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Invalidate all caches when loading a save game to ensure fresh data
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class Game_LoadGame_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                try
                {
                    CourtroomUtils.InvalidateAllCaches();
                    ModLog.Trace("All courtroom caches invalidated on game load");
                }
                catch (System.Exception e)
                {
                    ModLog.Error($"Error in Game_LoadGame_Patch: {e.Message}");
                }
            }
        }
    }
}
