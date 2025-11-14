using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldRealFoW;
using Verse;
using Law_and_Order.Source.Utils;

namespace LawAndOrder.Utils
{
    /// <summary>
    /// Utility methods for integrating with Real Fog of War mod
    /// Provides witness detection and visibility checking for crime system
    /// </summary>
    public static class FogOfWarUtils
    {
        /// <summary>
        /// Gets the MapComponentSeenFog for a given map
        /// Returns null if FoW mod is not active or component not found
        /// </summary>
        public static MapComponentSeenFog GetFoWComponent(Map map)
        {
            if (map == null)
                return null;

            return map.GetComponent<MapComponentSeenFog>();
        }

        /// <summary>
        /// Checks if a location is visible to the player's faction (colonists)
        /// Returns true if visible, false if in fog of war
        /// Falls back to true if FoW not available
        /// </summary>
        public static bool IsLocationVisible(IntVec3 location, Map map)
        {
            if (map == null || !location.InBounds(map))
                return false;

            MapComponentSeenFog fowComponent = GetFoWComponent(map);

            // Fallback: if FoW not available, assume everything is visible
            if (fowComponent == null)
            {
                ModLog.Warning("FoW component not found - assuming location is visible (fallback behavior)");
                return true;
            }

            // Check if location is visible to player faction
            Faction playerFaction = Faction.OfPlayer;
            return fowComponent.IsShown(playerFaction, location);
        }

        /// <summary>
        /// Gets all colonists who have line of sight to a specific location
        /// These are potential witnesses to crimes at this location
        /// </summary>
        /// <param name="location">The location to check</param>
        /// <param name="map">The map</param>
        /// <param name="includeAnimals">Whether to include colony animals as witnesses</param>
        /// <returns>List of pawns who can see the location</returns>
        public static List<Pawn> GetWitnessesAtLocation(IntVec3 location, Map map, bool includeAnimals = false)
        {
            List<Pawn> witnesses = new List<Pawn>();

            if (map == null || !location.InBounds(map))
                return witnesses;

            // First check if location is visible to player faction at all
            if (!IsLocationVisible(location, map))
            {
                // Location is in fog of war, no colonist witnesses
                return witnesses;
            }

            // Get all colonists on the map
            IEnumerable<Pawn> potentialWitnesses = map.mapPawns.FreeColonistsSpawned;

            if (includeAnimals)
            {
                potentialWitnesses = potentialWitnesses.Concat(
                    map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                        .Where(p => p.RaceProps.Animal)
                );
            }

            foreach (Pawn pawn in potentialWitnesses)
            {
                // Check if pawn can see the location
                if (CanPawnSeeLocation(pawn, location, map))
                {
                    witnesses.Add(pawn);
                }
            }

            return witnesses;
        }

        /// <summary>
        /// Checks if a specific pawn has line of sight to a location
        /// Uses FoW visibility if available, falls back to distance check
        /// </summary>
        public static bool CanPawnSeeLocation(Pawn pawn, IntVec3 location, Map map)
        {
            if (pawn == null || map == null || !pawn.Spawned || pawn.Map != map)
                return false;

            // Check if pawn is conscious and can see
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                return false;

            if (pawn.Dead || pawn.Downed)
                return false;

            // FoW handles line-of-sight and visibility
            // If the location is visible to the player faction, and the pawn is awake/conscious,
            // we need to check if they're close enough to reasonably witness

            float distance = pawn.Position.DistanceTo(location);
            float sightRange = GetPawnSightRange(pawn, map);

            // Simple distance check - could be enhanced with FoW's vision calculations
            return distance <= sightRange;
        }

        /// <summary>
        /// Gets the effective sight range for a pawn
        /// Factors in light level, weather, and pawn sight capacity
        /// </summary>
        public static float GetPawnSightRange(Pawn pawn, Map map)
        {
            if (pawn == null || map == null)
                return 0f;

            // Base sight range (adjust these values during balance phase)
            float baseSightRange = 20f;

            // Factor in sight capacity
            float sightCapacity = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
            float range = baseSightRange * sightCapacity;

            // Factor in light level at pawn's position
            float lightLevel = map.glowGrid.GroundGlowAt(pawn.Position);
            if (lightLevel < 0.3f) // Dark
            {
                range *= 0.5f; // Halve range in darkness
            }
            else if (lightLevel < 0.6f) // Dim
            {
                range *= 0.75f;
            }

            // Factor in weather
            if (map.weatherManager.CurWeatherPerceived != null)
            {
                float visibility = map.weatherManager.CurWeatherPerceived.rainRate;
                if (visibility > 0.5f) // Heavy rain/snow
                {
                    range *= 0.6f;
                }
                else if (visibility > 0.2f) // Light rain/snow
                {
                    range *= 0.8f;
                }
            }

            return range;
        }

        /// <summary>
        /// Calculates evidence strength based on witnesses and circumstances
        /// Returns a value from 0.0 (no evidence) to 1.0 (caught red-handed)
        /// </summary>
        public static float CalculateEvidenceStrength(IntVec3 location, Map map, List<Pawn> witnesses)
        {
            if (witnesses == null || witnesses.Count == 0)
                return 0f; // No witnesses = no evidence

            float evidenceStrength = 0f;

            // Base evidence from witness count (diminishing returns)
            float witnessBonus = 0.3f + (0.15f * witnesses.Count);
            witnessBonus = UnityEngine.Mathf.Min(witnessBonus, 0.8f); // Cap at 0.8

            evidenceStrength += witnessBonus;

            // Bonus for close witnesses (caught red-handed)
            int closeWitnesses = witnesses.Count(w => w.Position.DistanceTo(location) <= 5f);
            if (closeWitnesses > 0)
            {
                evidenceStrength += 0.2f; // Strong evidence if caught nearby
            }

            // Factor in light level
            float lightLevel = map.glowGrid.GroundGlowAt(location);
            if (lightLevel < 0.3f) // Darkness
            {
                evidenceStrength *= 0.6f; // Reduce evidence in darkness
            }

            // Factor in multiple independent witnesses
            if (witnesses.Count >= 2)
            {
                evidenceStrength += 0.1f; // Bonus for corroboration
            }

            // Clamp to 0.0 - 1.0 range
            evidenceStrength = UnityEngine.Mathf.Clamp01(evidenceStrength);

            return evidenceStrength;
        }

        /// <summary>
        /// Checks if FoW mod is active and available
        /// </summary>
        public static bool IsFoWActive()
        {
            // Check if we can find the FoW component on current map
            if (Current.Game == null || Find.CurrentMap == null)
                return false;

            return GetFoWComponent(Find.CurrentMap) != null;
        }

        /// <summary>
        /// Debug method to test FoW integration
        /// Call this from dev mode to verify APIs are accessible
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void TestFoWIntegration()
        {
            if (Find.CurrentMap == null)
            {
                Log.Error("Law and Order: Cannot test FoW - no map loaded");
                return;
            }

            MapComponentSeenFog fowComponent = GetFoWComponent(Find.CurrentMap);

            if (fowComponent == null)
            {
                Log.Warning("Law and Order: FoW component not found on map - mod may not be loaded");
                return;
            }

            Log.Message("Law and Order: FoW integration test PASSED - component accessible");

            // Test visibility check at a random location
            IntVec3 testLocation = Find.CurrentMap.AllCells.RandomElement();
            bool isVisible = IsLocationVisible(testLocation, Find.CurrentMap);
            Log.Message($"Law and Order: Test location {testLocation} visibility: {isVisible}");

            // Test witness detection
            List<Pawn> witnesses = GetWitnessesAtLocation(testLocation, Find.CurrentMap);
            Log.Message($"Law and Order: Found {witnesses.Count} potential witnesses at test location");
        }
    }
}
