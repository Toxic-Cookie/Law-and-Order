using System.Collections.Generic;
using Verse;

namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Stores detailed information about how a crime penalty was calculated.
    /// This allows the UI to display the complete calculation breakdown to the player.
    /// </summary>
    public class PenaltyBreakdown : IExposable
    {
        // Crime definition
        public string crimeDefName;
        public int minPenalty;
        public int maxPenalty;

        // Severity calculation (for victim crimes)
        public float bodyPartImportance = 0f;
        public float damageRatio = 0f;
        public bool hasPermanentEffect = false;
        public float severityScore = 0.5f;

        // Base penalty (after interpolation)
        public float basePenalty;

        // Multipliers applied
        public List<MultiplierInfo> appliedMultipliers = new List<MultiplierInfo>();

        // Final penalty calculation
        public float globalPenaltyScale = 1.0f;
        public float finalPenalty;

        // Non-victim crime info
        public bool isNonVictimCrime = false;
        public string calculationMethod; // e.g., "Item Value x 1.5", "Fixed Range Average"

        public PenaltyBreakdown()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref crimeDefName, "crimeDefName");
            Scribe_Values.Look(ref minPenalty, "minPenalty", 0);
            Scribe_Values.Look(ref maxPenalty, "maxPenalty", 0);
            Scribe_Values.Look(ref bodyPartImportance, "bodyPartImportance", 0f);
            Scribe_Values.Look(ref damageRatio, "damageRatio", 0f);
            Scribe_Values.Look(ref hasPermanentEffect, "hasPermanentEffect", false);
            Scribe_Values.Look(ref severityScore, "severityScore", 0.5f);
            Scribe_Values.Look(ref basePenalty, "basePenalty", 0f);
            Scribe_Collections.Look(ref appliedMultipliers, "appliedMultipliers", LookMode.Deep);
            Scribe_Values.Look(ref globalPenaltyScale, "globalPenaltyScale", 1.0f);
            Scribe_Values.Look(ref finalPenalty, "finalPenalty", 0f);
            Scribe_Values.Look(ref isNonVictimCrime, "isNonVictimCrime", false);
            Scribe_Values.Look(ref calculationMethod, "calculationMethod");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (appliedMultipliers == null)
                {
                    appliedMultipliers = new List<MultiplierInfo>();
                }
            }
        }
    }

    /// <summary>
    /// Information about a single multiplier that was applied to a penalty
    /// </summary>
    public class MultiplierInfo : IExposable
    {
        public string name;
        public float value;

        public MultiplierInfo()
        {
        }

        public MultiplierInfo(string name, float value)
        {
            this.name = name;
            this.value = value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref value, "value", 1.0f);
        }
    }
}
