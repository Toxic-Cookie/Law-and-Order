using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Stores reliability information for a single witness to a crime
    /// </summary>
    public class WitnessReliability : IExposable
    {
        public Pawn witness;                    // The witness pawn
        public float reliabilityScore = 1.0f;   // Overall reliability (0.0-1.0)

        // Individual reliability factors
        public float distanceFactor = 1.0f;     // Distance to crime (0.0-1.0)
        public float lightingFactor = 1.0f;     // Lighting conditions (0.0-1.0)
        public float sightCapacityFactor = 1.0f;// Sight ability (0.0-1.0)
        public float relationshipBiasFactor = 1.0f; // Relationship bias (0.0-1.0)
        public float intelligenceFactor = 1.0f; // Intelligence/perception (0.0-1.0)

        // Metadata
        public float distanceInTiles = 0f;      // Actual distance from crime
        public float lightLevel = 1.0f;         // Light level at crime scene (0.0-1.0)
        public int opinion = 0;                 // Witness's opinion of the accused
        public bool hasSightImpairment = false; // Does witness have vision problems?

        public WitnessReliability()
        {
        }

        public WitnessReliability(Pawn witness)
        {
            this.witness = witness;
        }

        /// <summary>
        /// Calculate overall reliability score from individual factors
        /// </summary>
        public void CalculateReliabilityScore()
        {
            // Multiplicative combination of all factors
            reliabilityScore = distanceFactor * lightingFactor * sightCapacityFactor
                             * relationshipBiasFactor * intelligenceFactor;

            // Clamp to valid range
            reliabilityScore = UnityEngine.Mathf.Clamp01(reliabilityScore);
        }

        /// <summary>
        /// Get reliability category for UI display
        /// </summary>
        public ReliabilityCategory GetCategory()
        {
            if (reliabilityScore >= 0.8f) return ReliabilityCategory.HighlyReliable;
            if (reliabilityScore >= 0.6f) return ReliabilityCategory.Reliable;
            if (reliabilityScore >= 0.4f) return ReliabilityCategory.Questionable;
            if (reliabilityScore >= 0.2f) return ReliabilityCategory.Unreliable;
            return ReliabilityCategory.VeryUnreliable;
        }

        /// <summary>
        /// Get color for UI display based on reliability
        /// </summary>
        public UnityEngine.Color GetColor()
        {
            switch (GetCategory())
            {
                case ReliabilityCategory.HighlyReliable:
                    return UnityEngine.Color.green;
                case ReliabilityCategory.Reliable:
                    return new UnityEngine.Color(0.7f, 0.9f, 0.3f); // Yellow-green
                case ReliabilityCategory.Questionable:
                    return UnityEngine.Color.yellow;
                case ReliabilityCategory.Unreliable:
                    return new UnityEngine.Color(1.0f, 0.65f, 0.0f); // Orange
                case ReliabilityCategory.VeryUnreliable:
                    return UnityEngine.Color.red;
                default:
                    return UnityEngine.Color.white;
            }
        }

        /// <summary>
        /// Get detailed tooltip explaining reliability factors
        /// </summary>
        public string GetTooltip()
        {
            var tooltip = new System.Text.StringBuilder();

            tooltip.AppendLine($"{"LawAndOrder_WitnessReliability_Overall".Translate()}: {(reliabilityScore * 100f):F0}%");
            tooltip.AppendLine($"{"LawAndOrder_WitnessReliability_Category".Translate()}: {($"LawAndOrder_ReliabilityCategory_{GetCategory()}").Translate()}");
            tooltip.AppendLine();

            // Distance factor
            tooltip.AppendLine($"{"LawAndOrder_ReliabilityFactor_Distance".Translate()}: {(distanceFactor * 100f):F0}%");
            tooltip.AppendLine($"  ({distanceInTiles:F1} {"LawAndOrder_Tiles".Translate()})");

            // Lighting factor
            tooltip.AppendLine($"{"LawAndOrder_ReliabilityFactor_Lighting".Translate()}: {(lightingFactor * 100f):F0}%");
            tooltip.AppendLine($"  ({(lightLevel * 100f):F0}% {"LawAndOrder_LightLevel".Translate()})");

            // Sight capacity factor
            tooltip.AppendLine($"{"LawAndOrder_ReliabilityFactor_SightCapacity".Translate()}: {(sightCapacityFactor * 100f):F0}%");
            if (hasSightImpairment)
            {
                tooltip.AppendLine($"  ({"LawAndOrder_HasSightImpairment".Translate()})");
            }

            // Relationship bias factor
            tooltip.AppendLine($"{"LawAndOrder_ReliabilityFactor_RelationshipBias".Translate()}: {(relationshipBiasFactor * 100f):F0}%");
            if (opinion < -20)
            {
                tooltip.AppendLine($"  ({"LawAndOrder_NegativeOpinion".Translate()}: {opinion})");
            }
            else if (opinion > 20)
            {
                tooltip.AppendLine($"  ({"LawAndOrder_PositiveOpinion".Translate()}: {opinion})");
            }

            // Intelligence factor
            tooltip.AppendLine($"{"LawAndOrder_ReliabilityFactor_Intelligence".Translate()}: {(intelligenceFactor * 100f):F0}%");

            return tooltip.ToString();
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref witness, "witness");
            Scribe_Values.Look(ref reliabilityScore, "reliabilityScore", 1.0f);
            Scribe_Values.Look(ref distanceFactor, "distanceFactor", 1.0f);
            Scribe_Values.Look(ref lightingFactor, "lightingFactor", 1.0f);
            Scribe_Values.Look(ref sightCapacityFactor, "sightCapacityFactor", 1.0f);
            Scribe_Values.Look(ref relationshipBiasFactor, "relationshipBiasFactor", 1.0f);
            Scribe_Values.Look(ref intelligenceFactor, "intelligenceFactor", 1.0f);
            Scribe_Values.Look(ref distanceInTiles, "distanceInTiles", 0f);
            Scribe_Values.Look(ref lightLevel, "lightLevel", 1.0f);
            Scribe_Values.Look(ref opinion, "opinion", 0);
            Scribe_Values.Look(ref hasSightImpairment, "hasSightImpairment", false);
        }
    }

    /// <summary>
    /// Reliability categories for UI display
    /// </summary>
    public enum ReliabilityCategory
    {
        HighlyReliable,  // 0.8+  (green)
        Reliable,        // 0.6-0.8 (yellow-green)
        Questionable,    // 0.4-0.6 (yellow)
        Unreliable,      // 0.2-0.4 (orange)
        VeryUnreliable   // < 0.2 (red)
    }
}
