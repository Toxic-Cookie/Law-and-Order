using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for courtroom-related functionality
    /// </summary>
    public static class CourtroomUtils
    {
        /// <summary>
        /// Check if the colony has a valid courtroom
        /// A courtroom is defined as a room with specific furniture/requirements
        /// TODO: Define actual courtroom requirements (throne, chairs, etc.)
        /// </summary>
        public static bool HasCourtroom(Map map = null)
        {
            if (map == null)
            {
                map = Find.CurrentMap;
            }

            if (map == null)
            {
                return false;
            }

            // Placeholder implementation
            // TODO: Implement actual courtroom detection logic
            // Could check for:
            // - A room with throne/chair
            // - Minimum room size
            // - Specific courtroom marker building
            // - Room role/designation

            // For now, just check if there's any room that could be used
            var rooms = map.regionGrid.AllRooms;
            if (rooms == null || !rooms.Any())
            {
                return false;
            }

            // Example: Check for a room with at least 10 cells and some furniture
            foreach (var room in rooms)
            {
                if (room.CellCount >= 10 && room.ContainedAndAdjacentThings.Any(t => t.def.building?.isSittable ?? false))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all potential courtrooms on the map
        /// </summary>
        public static List<Room> GetPotentialCourtrooms(Map map = null)
        {
            if (map == null)
            {
                map = Find.CurrentMap;
            }

            if (map == null)
            {
                return new List<Room>();
            }

            var courtrooms = new List<Room>();

            foreach (var room in map.regionGrid.AllRooms)
            {
                // Basic requirements for a courtroom
                // TODO: Make these requirements configurable or more sophisticated
                if (room.CellCount >= 10 &&
                    room.ContainedAndAdjacentThings.Any(t => t.def.building?.isSittable ?? false))
                {
                    courtrooms.Add(room);
                }
            }

            return courtrooms;
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

            // TODO: Add specific courtroom requirements
            // For now, basic checks:
            // - Must be indoors
            // - Must have minimum size
            // - Must have seating

            if (room.CellCount < 10)
            {
                return false;
            }

            // Check for seating
            bool hasSeating = room.ContainedAndAdjacentThings.Any(t => t.def.building?.isSittable ?? false);

            return hasSeating;
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

            if (room.CellCount < 10)
            {
                return "Room is too small (minimum 10 cells)";
            }

            bool hasSeating = room.ContainedAndAdjacentThings.Any(t => t.def.building?.isSittable ?? false);
            if (!hasSeating)
            {
                return "Room needs seating (chairs, stools, thrones, etc.)";
            }

            return "Valid courtroom";
        }
    }
}
