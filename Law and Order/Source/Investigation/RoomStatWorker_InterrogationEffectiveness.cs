using RimWorld;
using System.Linq;
using Verse;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// RoomStatWorker that calculates interrogation effectiveness based on room quality.
    /// Only shows for rooms containing a designated interrogation table.
    /// </summary>
    public class RoomStatWorker_InterrogationEffectiveness : RoomStatWorker
    {
        public override float GetScore(Room room)
        {
            // Only calculate if this room has a designated interrogation table
            if (!HasInterrogationTable(room))
            {
                return 0f;
            }

            // Base score starts at 50 (decent)
            float score = 50f;

            // Room impressiveness contributes (up to +30)
            float impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
            score += CalculateImpressivenessBonus(impressiveness);

            // Room cleanliness contributes (up to +15 or down to -20)
            float cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
            score += CalculateCleanlinessBonus(cleanliness);

            // Room space contributes (up to +10)
            float space = room.GetStat(RoomStatDefOf.Space);
            score += CalculateSpaceBonus(space);

            // Ensure score is within reasonable bounds (0-100)
            return UnityEngine.Mathf.Clamp(score, 0f, 100f);
        }

        /// <summary>
        /// Check if room contains a designated interrogation table
        /// </summary>
        private bool HasInterrogationTable(Room room)
        {
            if (room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }

            // Check all things in the room
            foreach (var thing in room.ContainedAndAdjacentThings)
            {
                var comp = thing.TryGetComp<Comp_InterrogationTable>();
                if (comp != null && comp.IsDesignated)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculate bonus from room impressiveness
        /// </summary>
        private float CalculateImpressivenessBonus(float impressiveness)
        {
            // Impressiveness ranges typically 0-200+
            // Map to -10 to +30 bonus
            if (impressiveness < 20)
                return -10f;
            else if (impressiveness < 40)
                return 0f;
            else if (impressiveness < 65)
                return 10f;
            else if (impressiveness < 100)
                return 20f;
            else
                return 30f;
        }

        /// <summary>
        /// Calculate bonus from room cleanliness
        /// </summary>
        private float CalculateCleanlinessBonus(float cleanliness)
        {
            // Cleanliness ranges typically -5 to +5
            // Map to -20 to +15 bonus
            if (cleanliness < -2.0f)
                return -20f;
            else if (cleanliness < -0.5f)
                return -10f;
            else if (cleanliness < 0f)
                return -5f;
            else if (cleanliness < 0.5f)
                return 5f;
            else
                return 15f;
        }

        /// <summary>
        /// Calculate bonus from room space
        /// </summary>
        private float CalculateSpaceBonus(float space)
        {
            // Space ranges typically 0-350
            // Map to 0 to +10 bonus
            if (space < 12.5f)
                return 0f;
            else if (space < 29f)
                return 3f;
            else if (space < 55f)
                return 5f;
            else if (space < 100f)
                return 8f;
            else
                return 10f;
        }
    }
}
