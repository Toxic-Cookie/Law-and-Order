using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Defines a multiplier that can be applied to crime penalties based on context.
    /// </summary>
    public class CrimePenaltyMultiplier : IExposable
    {
        public MultiplierType multiplierType;
        public float multiplierValue;
        public string description;
        public bool enabled;

        // Track pending changes (staged values before commit)
        public float pendingMultiplierValue = -1f;
        public bool pendingEnabled = false;
        public bool hasPendingEnabledChange = false;
        public bool hasPendingChanges => pendingMultiplierValue >= 0f || hasPendingEnabledChange;

        public CrimePenaltyMultiplier()
        {
        }

        public CrimePenaltyMultiplier(MultiplierType multiplierType, float multiplierValue, string description, bool enabled = true)
        {
            this.multiplierType = multiplierType;
            this.multiplierValue = multiplierValue;
            this.description = description;
            this.enabled = enabled;
            this.pendingMultiplierValue = -1f;
            this.pendingEnabled = false;
            this.hasPendingEnabledChange = false;
        }

        /// <summary>
        /// Gets the effective multiplier value (pending if staged, otherwise current).
        /// </summary>
        public float GetEffectiveMultiplierValue()
        {
            return pendingMultiplierValue >= 0f ? pendingMultiplierValue : multiplierValue;
        }

        /// <summary>
        /// Gets the effective enabled state (pending if staged, otherwise current).
        /// </summary>
        public bool GetEffectiveEnabled()
        {
            return hasPendingEnabledChange ? pendingEnabled : enabled;
        }

        /// <summary>
        /// Stages a multiplier change (doesn't apply until commit).
        /// </summary>
        public void StageMultiplierChange(float newMultiplierValue)
        {
            pendingMultiplierValue = newMultiplierValue;
        }

        /// <summary>
        /// Stages an enabled state change (doesn't apply until commit).
        /// </summary>
        public void StageEnabledChange(bool newEnabled)
        {
            pendingEnabled = newEnabled;
            hasPendingEnabledChange = true;
        }

        /// <summary>
        /// Commits the pending changes.
        /// </summary>
        public void CommitPendingChanges()
        {
            if (pendingMultiplierValue >= 0f)
            {
                multiplierValue = pendingMultiplierValue;
            }

            if (hasPendingEnabledChange)
            {
                enabled = pendingEnabled;
            }

            ClearPendingChanges();
        }

        /// <summary>
        /// Cancels the pending changes.
        /// </summary>
        public void ClearPendingChanges()
        {
            pendingMultiplierValue = -1f;
            pendingEnabled = false;
            hasPendingEnabledChange = false;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref multiplierType, "multiplierType");
            Scribe_Values.Look(ref multiplierValue, "multiplierValue", 1.0f);
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref pendingMultiplierValue, "pendingMultiplierValue", -1f);
            Scribe_Values.Look(ref pendingEnabled, "pendingEnabled", false);
            Scribe_Values.Look(ref hasPendingEnabledChange, "hasPendingEnabledChange", false);
        }
    }

    /// <summary>
    /// Types of penalty multipliers that can be applied to crimes.
    /// </summary>
    public enum MultiplierType
    {
        RepeatOffender,
        VictimNobility,
        VictimAge,
        Premeditated,
        VictimRelationship,
        RaiderWealth,
        ColonyWealth,
        DifficultySetting
    }
}
