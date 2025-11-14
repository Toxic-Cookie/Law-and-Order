using Verse;
using System.Collections.Generic;

namespace Law_and_Order.Source.Justice
{
    /// <summary>
    /// Definition for a punishment type.
    /// Loaded from XML in Defs/PunishmentDefs/
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    public class PunishmentDef : Def
    {
        /// <summary>
        /// The punishment type this def represents.
        /// </summary>
        public PunishmentType punishmentType;

        /// <summary>
        /// User-friendly label for this punishment (e.g., "Imprisonment").
        /// </summary>
        public string punishmentLabel;

        /// <summary>
        /// Detailed description shown in punishment selection dialog.
        /// </summary>
        public new string description;

        /// <summary>
        /// Severity rating (1-10) for recommended punishment matching.
        /// 1 = Very lenient (short imprisonment, small fine)
        /// 5 = Moderate (medium imprisonment, beating)
        /// 10 = Extreme (execution)
        /// </summary>
        public int severityRating;

        /// <summary>
        /// Whether this punishment requires the criminal to be alive.
        /// </summary>
        public bool requiresAlive = true;

        /// <summary>
        /// Whether this punishment requires the criminal to be on the map.
        /// </summary>
        public bool requiresOnMap = true;

        /// <summary>
        /// Whether this punishment requires the criminal to be imprisoned.
        /// </summary>
        public bool requiresImprisoned = false;

        /// <summary>
        /// Minimum crime severity to allow this punishment.
        /// Prevents using execution for minor crimes, etc.
        /// </summary>
        public int minimumCrimeSeverity = 0;

        /// <summary>
        /// Default duration in days (for imprisonment).
        /// </summary>
        public int defaultDurationDays = 0;

        /// <summary>
        /// Default fine amount in silver (for fines).
        /// </summary>
        public int defaultFineAmount = 0;

        /// <summary>
        /// Mood impact on victim when this punishment is applied.
        /// Positive = victim satisfied, negative = victim unhappy.
        /// </summary>
        public int victimMoodImpact = 0;

        /// <summary>
        /// Mood impact on the criminal when this punishment is applied.
        /// Usually negative.
        /// </summary>
        public int criminalMoodImpact = 0;

        /// <summary>
        /// Mood impact on colonists who witness the punishment.
        /// Can be positive (justice served) or negative (too harsh).
        /// </summary>
        public int witnessMoodImpact = 0;

        /// <summary>
        /// Whether this punishment can be applied multiple times to the same pawn.
        /// </summary>
        public bool allowMultiple = true;
    }
}
