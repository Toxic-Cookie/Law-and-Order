using HarmonyLib;
using Verse;
using Law_and_Order.Source.Utils;
using System.Collections.Generic;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Harmony patches to automatically invalidate courtroom cache when room structure changes.
    /// This ensures the cache stays up-to-date when players build or destroy courtroom furniture.
    /// </summary>
    public static class CourtroomCacheInvalidation_Patch
    {
        // Debounce tracking: map -> tick of last invalidation
        private static Dictionary<Map, int> lastInvalidationTick = new Dictionary<Map, int>();
        private const int INVALIDATION_COOLDOWN_TICKS = 60; // 1 second at normal speed

        /// <summary>
        /// Check if a building is relevant to courtroom detection
        /// </summary>
        private static bool IsRelevantBuilding(Thing thing)
        {
            if (!(thing is Building building))
            {
                return false;
            }

            // Chairs and sittable furniture (potential courtroom seating)
            if (building.def.building?.isSittable == true)
            {
                return true;
            }

            // Walls, doors, and other room-defining structures
            // These can change room boundaries which affects which chairs are in which room
            if (building.def.passability == Traversability.Impassable ||
                building.def.fillPercent >= 0.99f ||
                building.def.building?.isEdifice == true)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Invalidate cache with debouncing to prevent spam
        /// </summary>
        private static void TryInvalidateCache(Map map, string reason)
        {
            if (map == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;

            // Check if we've invalidated recently
            if (lastInvalidationTick.TryGetValue(map, out int lastTick))
            {
                if (currentTick - lastTick < INVALIDATION_COOLDOWN_TICKS)
                {
                    // Too soon, skip this invalidation
                    return;
                }
            }

            // Invalidate and update timestamp
            CourtroomUtils.InvalidateCache(map);
            lastInvalidationTick[map] = currentTick;

            // Only log at trace level if debugging is needed
            // ModLog.Trace($"Courtroom cache invalidated for map {map.uniqueID}: {reason}");
        }

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
                    // Only invalidate for buildings that could affect courtrooms
                    if (IsRelevantBuilding(__instance))
                    {
                        TryInvalidateCache(map, $"building spawn: {__instance.def.defName}");
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
                    // Only invalidate for buildings that could affect courtrooms
                    if (__instance.Map != null && IsRelevantBuilding(__instance))
                    {
                        Map map = __instance.Map;
                        TryInvalidateCache(map, $"building despawn: {__instance.def.defName}");
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
