using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Defines a crime type with its associated penalty range.
    /// </summary>
    public class CrimeDefinition : IExposable
    {
        public string defName;
        public string label;
        public string description;
        public CrimeSeverity severity;
        public int minPenalty;
        public int maxPenalty;
        public CrimeCategory category;

        // Indicates whether this crime uses the min/max range for penalty calculation
        // False for value-based crimes (Theft, Vandalism, etc.) that calculate penalties from item values
        public bool usesRangeCalculation = true;

        // Track pending changes (staged values before commit)
        public int pendingMinPenalty = -1;
        public int pendingMaxPenalty = -1;
        public bool hasPendingChanges => pendingMinPenalty >= 0 || pendingMaxPenalty >= 0;

        public CrimeDefinition()
        {
        }

        public CrimeDefinition(string defName, string label, string description, CrimeSeverity severity,
            int minPenalty, int maxPenalty, CrimeCategory category, bool usesRangeCalculation = true)
        {
            this.defName = defName;
            this.label = label;
            this.description = description;
            this.severity = severity;
            this.minPenalty = minPenalty;
            this.maxPenalty = maxPenalty;
            this.category = category;
            this.usesRangeCalculation = usesRangeCalculation;
            this.pendingMinPenalty = -1;
            this.pendingMaxPenalty = -1;
        }

        /// <summary>
        /// Gets the effective penalty (pending if staged, otherwise current).
        /// </summary>
        public int GetEffectiveMinPenalty()
        {
            return hasPendingChanges && pendingMinPenalty >= 0 ? pendingMinPenalty : minPenalty;
        }

        /// <summary>
        /// Gets the effective penalty (pending if staged, otherwise current).
        /// </summary>
        public int GetEffectiveMaxPenalty()
        {
            return hasPendingChanges && pendingMaxPenalty >= 0 ? pendingMaxPenalty : maxPenalty;
        }

        /// <summary>
        /// Stages a penalty change (doesn't apply until commit).
        /// </summary>
        public void StagePenaltyChange(int newMinPenalty, int newMaxPenalty)
        {
            pendingMinPenalty = newMinPenalty;
            pendingMaxPenalty = newMaxPenalty;
        }

        /// <summary>
        /// Commits the pending penalty change.
        /// </summary>
        public void CommitPendingChanges()
        {
            if (hasPendingChanges)
            {
                if (pendingMinPenalty >= 0)
                    minPenalty = pendingMinPenalty;
                if (pendingMaxPenalty >= 0)
                    maxPenalty = pendingMaxPenalty;

                ClearPendingChanges();
            }
        }

        /// <summary>
        /// Cancels the pending penalty change.
        /// </summary>
        public void ClearPendingChanges()
        {
            pendingMinPenalty = -1;
            pendingMaxPenalty = -1;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref severity, "severity");
            Scribe_Values.Look(ref minPenalty, "minPenalty", 0);
            Scribe_Values.Look(ref maxPenalty, "maxPenalty", 0);
            Scribe_Values.Look(ref category, "category");
            Scribe_Values.Look(ref usesRangeCalculation, "usesRangeCalculation", true);
            Scribe_Values.Look(ref pendingMinPenalty, "pendingMinPenalty", -1);
            Scribe_Values.Look(ref pendingMaxPenalty, "pendingMaxPenalty", -1);
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
