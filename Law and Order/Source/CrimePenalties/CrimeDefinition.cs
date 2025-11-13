using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Defines a crime type with its associated base penalty and multiplier.
    /// </summary>
    public class CrimeDefinition : IExposable
    {
        public string defName;
        public string label;
        public string description;
        public CrimeSeverity severity;
        public int basePenalty; // Base penalty value (typically average of old min/max range)
        public float penaltyMultiplier; // Player-configurable multiplier (0.5x to 3.0x)
        public CrimeCategory category;

        // Indicates whether this crime uses the base penalty for calculation
        // False for value-based crimes (Theft, Vandalism, etc.) that calculate penalties from item values
        public bool usesRangeCalculation = true;

        // Track pending changes (staged values before commit)
        public float pendingPenaltyMultiplier = -1f;
        public bool hasPendingChanges => pendingPenaltyMultiplier >= 0f;

        public CrimeDefinition()
        {
        }

        /// <summary>
        /// Main constructor for crime definitions with base penalty and multiplier.
        /// </summary>
        public CrimeDefinition(string defName, string label, string description, CrimeSeverity severity,
            int basePenalty, float penaltyMultiplier, CrimeCategory category, bool usesRangeCalculation = true)
        {
            this.defName = defName;
            this.label = label;
            this.description = description;
            this.severity = severity;
            this.basePenalty = basePenalty;
            this.penaltyMultiplier = penaltyMultiplier;
            this.category = category;
            this.usesRangeCalculation = usesRangeCalculation;
            this.pendingPenaltyMultiplier = -1f;
        }

        /// <summary>
        /// Convenience constructor that accepts min/max and converts to base penalty (average) with 1.0x multiplier.
        /// This allows for easier initialization of crimes with a range.
        /// </summary>
        public CrimeDefinition(string defName, string label, string description, CrimeSeverity severity,
            int minPenalty, int maxPenalty, CrimeCategory category, bool usesRangeCalculation = true)
            : this(defName, label, description, severity, (minPenalty + maxPenalty) / 2, 1.0f, category, usesRangeCalculation)
        {
        }

        /// <summary>
        /// Gets the effective penalty multiplier (pending if staged, otherwise current).
        /// </summary>
        public float GetEffectiveMultiplier()
        {
            return hasPendingChanges && pendingPenaltyMultiplier >= 0 ? pendingPenaltyMultiplier : penaltyMultiplier;
        }

        /// <summary>
        /// Stages a multiplier change (doesn't apply until commit).
        /// </summary>
        public void StageMultiplierChange(float newMultiplier)
        {
            pendingPenaltyMultiplier = newMultiplier;
        }

        /// <summary>
        /// Commits the pending multiplier change.
        /// </summary>
        public void CommitPendingChanges()
        {
            if (hasPendingChanges)
            {
                if (pendingPenaltyMultiplier >= 0)
                    penaltyMultiplier = pendingPenaltyMultiplier;

                ClearPendingChanges();
            }
        }

        /// <summary>
        /// Cancels the pending multiplier change.
        /// </summary>
        public void ClearPendingChanges()
        {
            pendingPenaltyMultiplier = -1f;
        }

        /// <summary>
        /// Gets the effective penalty range based on base penalty, multiplier, and severity variation.
        /// This provides min/max values for display purposes.
        /// Severity causes ±33% variation around the base value.
        /// </summary>
        public void GetEffectivePenaltyRange(out int minPenalty, out int maxPenalty)
        {
            float effectiveMultiplier = GetEffectiveMultiplier();
            float baseValue = basePenalty * effectiveMultiplier;

            // Severity causes variation: 0.67x to 1.33x of base value
            minPenalty = UnityEngine.Mathf.RoundToInt(baseValue * 0.67f);
            maxPenalty = UnityEngine.Mathf.RoundToInt(baseValue * 1.33f);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref severity, "severity");
            Scribe_Values.Look(ref category, "category");
            Scribe_Values.Look(ref usesRangeCalculation, "usesRangeCalculation", true);
            Scribe_Values.Look(ref basePenalty, "basePenalty", 0);
            Scribe_Values.Look(ref penaltyMultiplier, "penaltyMultiplier", 1.0f);
            Scribe_Values.Look(ref pendingPenaltyMultiplier, "pendingPenaltyMultiplier", -1f);
        }
    }

    /// <summary>
    /// Crime severity levels.
    /// </summary>
    public enum CrimeSeverity
    {
        Low,
        Moderate,
        High,
        Critical
    }

    /// <summary>
    /// Crime categories for organization.
    /// </summary>
    public enum CrimeCategory
    {
        Lethal,
        SevereInjury,
        ModerateInjury,
        MinorInjury,
        Environmental,
        Disease,
        PropertyDestruction,
        Theft,
        Social,
        Anomaly,
        Biotech
    }
}
