using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Rituals;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for courtroom-related functionality
    /// </summary>
    public static class CourtroomUtils
    {
        // Cache for courtroom lists to avoid repeated scanning
        private static Dictionary<Map, List<Room>> cachedCourtrooms = new Dictionary<Map, List<Room>>();
        private static Dictionary<Map, int> lastCacheUpdateTick = new Dictionary<Map, int>();

        // Cache duration in ticks (~1 minute at normal speed)
        private const int CACHE_DURATION_TICKS = 2500;

        /// <summary>
        /// Invalidate the courtroom cache for a specific map.
        /// Call this when furniture is built/destroyed or room structure changes.
        /// </summary>
        public static void InvalidateCache(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (cachedCourtrooms.ContainsKey(map))
            {
                cachedCourtrooms.Remove(map);
            }

            if (lastCacheUpdateTick.ContainsKey(map))
            {
                lastCacheUpdateTick.Remove(map);
            }
        }

        /// <summary>
        /// Invalidate the courtroom cache for all maps.
        /// </summary>
        public static void InvalidateAllCaches()
        {
            cachedCourtrooms.Clear();
            lastCacheUpdateTick.Clear();
        }

        /// <summary>
        /// Get all chairs in a room with their role assignments
        /// </summary>
        public static Dictionary<CourtroomChairRole, List<Thing>> GetCourtroomChairs(Room room)
        {
            var result = new Dictionary<CourtroomChairRole, List<Thing>>();

            if (room == null)
            {
                return result;
            }

            // Get all things in the room that are sittable and have the courtroom comp
            var chairs = room.ContainedAndAdjacentThings
                .Where(t => t.def.building?.isSittable == true)
                .Where(t => t.TryGetComp<CompCourtroomChair>() != null)
                .ToList();

            // Group by role
            foreach (var chair in chairs)
            {
                var comp = chair.TryGetComp<CompCourtroomChair>();
                if (comp != null)
                {
                    if (!result.ContainsKey(comp.Role))
                    {
                        result[comp.Role] = new List<Thing>();
                    }
                    result[comp.Role].Add(chair);
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a room has the minimum required seating for a hearing
        /// </summary>
        public static bool HasMinimumSeating(Room room)
        {
            var chairs = GetCourtroomChairs(room);

            // Must have at least: 1 judge, 1 defendant, 1 spectator
            bool hasJudge = chairs.ContainsKey(CourtroomChairRole.Judge) && chairs[CourtroomChairRole.Judge].Count >= 1;
            bool hasDefendant = chairs.ContainsKey(CourtroomChairRole.Defendant) && chairs[CourtroomChairRole.Defendant].Count >= 1;

            return hasJudge && hasDefendant;
        }

        /// <summary>
        /// Get the number of designated seats for a specific role
        /// </summary>
        public static int GetSeatCountForRole(Room room, CourtroomChairRole role)
        {
            var chairs = GetCourtroomChairs(room);
            return chairs.ContainsKey(role) ? chairs[role].Count : 0;
        }

        /// <summary>
        /// Find all potential courtrooms (rooms with courtroom chairs)
        /// Uses caching to avoid repeated scans of all rooms on the map.
        /// Cache is automatically refreshed after CACHE_DURATION_TICKS.
        /// </summary>
        public static List<Room> GetPotentialCourtroomsWithSeating(Map map = null)
        {
            if (map == null)
            {
                map = Find.CurrentMap;
            }

            if (map == null)
            {
                return new List<Room>();
            }

            int currentTick = Find.TickManager.TicksGame;

            // Check if we have a valid cache
            bool hasValidCache = cachedCourtrooms.ContainsKey(map) &&
                                lastCacheUpdateTick.ContainsKey(map) &&
                                (currentTick - lastCacheUpdateTick[map]) < CACHE_DURATION_TICKS;

            if (hasValidCache)
            {
                ModLog.Trace($"Using cached courtroom list for map {map.uniqueID} (age: {currentTick - lastCacheUpdateTick[map]} ticks)");
                return cachedCourtrooms[map];
            }

            // Cache miss or expired - scan for courtrooms
            ModLog.Trace($"Scanning map {map.uniqueID} for courtrooms (cache miss or expired)");
            var courtrooms = new List<Room>();

            foreach (var room in map.regionGrid.AllRooms)
            {
                if (HasMinimumSeating(room))
                {
                    courtrooms.Add(room);
                }
            }

            // Update cache
            cachedCourtrooms[map] = courtrooms;
            lastCacheUpdateTick[map] = currentTick;

            ModLog.Trace($"Found {courtrooms.Count} courtrooms on map {map.uniqueID}");

            return courtrooms;
        }
        /// <summary>
        /// Check if the colony has a valid courtroom
        /// A courtroom must have minimum required seating (Judge, Defendant)
        /// </summary>
        public static bool HasCourtroom(Map map = null)
        {
            var courtrooms = GetPotentialCourtroomsWithSeating(map);
            return courtrooms.Count > 0;
        }

        /// <summary>
        /// Get all potential courtrooms on the map
        /// </summary>
        public static List<Room> GetPotentialCourtrooms(Map map = null)
        {
            return GetPotentialCourtroomsWithSeating(map);
        }

        /// <summary>
        /// Check if a specific room qualifies as a courtroom
        /// </summary>
        public static bool IsValidCourtroom(Room room)
        {
            if (room == null || room.OutdoorsForWork)
            {
                return false;
            }

            // Must have minimum required seating
            return HasMinimumSeating(room);
        }

        /// <summary>
        /// Get the impressiveness score for a courtroom
        /// Higher scores = better courtroom
        /// </summary>
        public static float GetCourtroomQuality(Room room)
        {
            if (room == null || !IsValidCourtroom(room))
            {
                return 0f;
            }

            // Use room impressiveness as a proxy for courtroom quality
            return room.GetStat(RoomStatDefOf.Impressiveness);
        }

        /// <summary>
        /// Find the best courtroom on the map
        /// </summary>
        public static Room GetBestCourtroom(Map map = null)
        {
            var courtrooms = GetPotentialCourtrooms(map);

            if (!courtrooms.Any())
            {
                return null;
            }

            // Return the most impressive room
            return courtrooms.OrderByDescending(r => GetCourtroomQuality(r)).First();
        }

        /// <summary>
        /// Get a human-readable description of why a room is not a valid courtroom
        /// </summary>
        public static string GetCourtroomValidationMessage(Room room)
        {
            if (room == null)
            {
                return "No room selected";
            }

            if (room.OutdoorsForWork)
            {
                return "Room must be indoors";
            }

            var chairs = GetCourtroomChairs(room);

            int judgeSeats = GetSeatCountForRole(room, CourtroomChairRole.Judge);
            int defendantSeats = GetSeatCountForRole(room, CourtroomChairRole.Defendant);

            if (judgeSeats == 0 && defendantSeats == 0)
            {
                return "No designated courtroom seating. Select chairs and designate them for courtroom roles.";
            }

            if (judgeSeats == 0)
            {
                return "Missing Judge seat. Designate a chair for the judge.";
            }

            if (defendantSeats == 0)
            {
                return "Missing Defendant seat. Designate a chair for the defendant.";
            }

            return $"Valid courtroom (Judge: {judgeSeats}, Defendant: {defendantSeats}, " +
                   $"Jury: {GetSeatCountForRole(room, CourtroomChairRole.Jury)}, " +
                   $"Victim: {GetSeatCountForRole(room, CourtroomChairRole.Victim)}, " +
                   $"Spectator: {GetSeatCountForRole(room, CourtroomChairRole.Spectator)})";
        }
    }
}
