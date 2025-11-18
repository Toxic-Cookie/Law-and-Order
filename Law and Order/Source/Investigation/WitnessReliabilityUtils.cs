using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Utility class for calculating witness reliability scores
    /// Phase 6.7: Witness Reliability & Social Impact
    /// </summary>
    public static class WitnessReliabilityUtils
    {
        // Distance constants
        private const float PERFECT_WITNESS_DISTANCE = 3f;  // Within 3 tiles = perfect observation
        private const float MAX_RELIABLE_DISTANCE = 20f;    // Beyond 20 tiles = very unreliable

        // Lighting penalties
        private const float DARKNESS_PENALTY = 0.6f;        // Dark conditions (< 30% light)
        private const float DIM_LIGHT_PENALTY = 0.75f;      // Dim conditions (30-60% light)

        // Relationship bias penalties
        private const float FRIEND_BIAS_PENALTY = 0.8f;     // Friends are biased (×0.8)
        private const float ENEMY_BIAS_PENALTY = 0.7f;      // Enemies are very biased (×0.7)

        // Opinion thresholds
        private const int FRIEND_OPINION_THRESHOLD = 20;
        private const int ENEMY_OPINION_THRESHOLD = -20;

        /// <summary>
        /// Calculate witness reliability for a specific witness to a crime
        /// </summary>
        public static WitnessReliability CalculateWitnessReliability(
            Pawn witness,
            Pawn accused,
            IntVec3 crimeLocation,
            Map map)
        {
            var reliability = new WitnessReliability(witness);

            if (witness == null || !witness.Spawned)
            {
                reliability.reliabilityScore = 0f;
                return reliability;
            }

            // Calculate individual factors
            reliability.distanceFactor = CalculateDistanceFactor(witness, crimeLocation);
            reliability.distanceInTiles = (witness.Position - crimeLocation).LengthHorizontal;

            reliability.lightingFactor = CalculateLightingFactor(crimeLocation, map);
            reliability.lightLevel = GetLightLevel(crimeLocation, map);

            reliability.sightCapacityFactor = CalculateSightCapacityFactor(witness);
            reliability.hasSightImpairment = HasSightImpairment(witness);

            if (accused != null)
            {
                reliability.relationshipBiasFactor = CalculateRelationshipBiasFactor(witness, accused);
                reliability.opinion = witness.relations?.OpinionOf(accused) ?? 0;
            }

            reliability.intelligenceFactor = CalculateIntelligenceFactor(witness);

            // Calculate overall score
            reliability.CalculateReliabilityScore();

            return reliability;
        }

        /// <summary>
        /// Calculate reliability factor based on distance from crime
        /// </summary>
        private static float CalculateDistanceFactor(Pawn witness, IntVec3 crimeLocation)
        {
            float distance = (witness.Position - crimeLocation).LengthHorizontal;

            if (distance <= PERFECT_WITNESS_DISTANCE)
            {
                // Perfect observation within 3 tiles
                return 1.0f;
            }

            if (distance >= MAX_RELIABLE_DISTANCE)
            {
                // Very poor observation beyond 20 tiles
                return 0.2f;
            }

            // Linear falloff between 3 and 20 tiles
            // At 3 tiles: 1.0
            // At 20 tiles: 0.2
            float t = (distance - PERFECT_WITNESS_DISTANCE) / (MAX_RELIABLE_DISTANCE - PERFECT_WITNESS_DISTANCE);
            return UnityEngine.Mathf.Lerp(1.0f, 0.2f, t);
        }

        /// <summary>
        /// Calculate reliability factor based on lighting conditions
        /// </summary>
        private static float CalculateLightingFactor(IntVec3 location, Map map)
        {
            if (map == null)
                return 1.0f;

            float lightLevel = GetLightLevel(location, map);

            if (lightLevel < 0.3f)
            {
                // Darkness - significant penalty
                return DARKNESS_PENALTY;
            }

            if (lightLevel < 0.6f)
            {
                // Dim light - moderate penalty
                return DIM_LIGHT_PENALTY;
            }

            // Good lighting - no penalty
            return 1.0f;
        }

        /// <summary>
        /// Get light level at a location (0.0-1.0)
        /// </summary>
        private static float GetLightLevel(IntVec3 location, Map map)
        {
            if (map == null)
                return 1.0f;

            var glowGrid = map.glowGrid;
            if (glowGrid == null)
                return 1.0f;

            float glow = glowGrid.GroundGlowAt(location, false, false);
            // Normalize to 0-1 range (glow is typically 0-1.4+)
            return UnityEngine.Mathf.Clamp01(glow / 1.4f);
        }

        /// <summary>
        /// Calculate reliability factor based on witness's sight capacity
        /// </summary>
        private static float CalculateSightCapacityFactor(Pawn witness)
        {
            if (witness?.health?.capacities == null)
                return 1.0f;

            float sightCapacity = witness.health.capacities.GetLevel(PawnCapacityDefOf.Sight);

            // Sight capacity is typically 0-1, with 1 being perfect
            // We want to penalize poor sight more severely
            if (sightCapacity < 0.3f)
            {
                // Severe sight impairment - very unreliable
                return 0.3f;
            }

            if (sightCapacity < 0.6f)
            {
                // Moderate sight impairment - somewhat unreliable
                return 0.6f;
            }

            // Good or perfect sight
            return sightCapacity;
        }

        /// <summary>
        /// Check if witness has sight impairment
        /// </summary>
        private static bool HasSightImpairment(Pawn witness)
        {
            if (witness?.health?.capacities == null)
                return false;

            float sightCapacity = witness.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
            return sightCapacity < 1.0f;
        }

        /// <summary>
        /// Calculate reliability factor based on relationship bias
        /// Friends and enemies are both less reliable due to bias!
        /// </summary>
        private static float CalculateRelationshipBiasFactor(Pawn witness, Pawn accused)
        {
            if (witness?.relations == null || accused == null)
                return 1.0f;

            int opinion = witness.relations.OpinionOf(accused);

            if (opinion >= FRIEND_OPINION_THRESHOLD)
            {
                // Friend - biased in favor (less reliable)
                return FRIEND_BIAS_PENALTY;
            }

            if (opinion <= ENEMY_OPINION_THRESHOLD)
            {
                // Enemy - biased against (even less reliable)
                return ENEMY_BIAS_PENALTY;
            }

            // Neutral relationship - no bias
            return 1.0f;
        }

        /// <summary>
        /// Calculate reliability factor based on witness intelligence/perception
        /// </summary>
        private static float CalculateIntelligenceFactor(Pawn witness)
        {
            if (witness?.skills == null)
                return 1.0f;

            // Use Intellectual skill as proxy for observation/memory quality
            int intellectualSkill = witness.skills.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;

            // Scale from 0.7 (unskilled) to 1.0 (max skill)
            // At skill 0: 0.7
            // At skill 20: 1.0
            float factor = 0.7f + (intellectualSkill / 20f) * 0.3f;

            return UnityEngine.Mathf.Clamp(factor, 0.7f, 1.0f);
        }

        /// <summary>
        /// Calculate reliability for multiple witnesses
        /// </summary>
        public static List<WitnessReliability> CalculateWitnessReliabilities(
            List<Pawn> witnesses,
            Pawn accused,
            IntVec3 crimeLocation,
            Map map)
        {
            var reliabilities = new List<WitnessReliability>();

            if (witnesses == null || witnesses.Count == 0)
                return reliabilities;

            foreach (var witness in witnesses)
            {
                if (witness != null)
                {
                    var reliability = CalculateWitnessReliability(witness, accused, crimeLocation, map);
                    reliabilities.Add(reliability);
                }
            }

            return reliabilities;
        }

        /// <summary>
        /// Get average reliability of all witnesses to a crime
        /// </summary>
        public static float GetAverageReliability(List<WitnessReliability> reliabilities)
        {
            if (reliabilities == null || reliabilities.Count == 0)
                return 0f;

            return reliabilities.Average(r => r.reliabilityScore);
        }

        /// <summary>
        /// Check if any witness is highly reliable (>= 0.8)
        /// </summary>
        public static bool HasHighlyReliableWitness(List<WitnessReliability> reliabilities)
        {
            if (reliabilities == null || reliabilities.Count == 0)
                return false;

            return reliabilities.Any(r => r.reliabilityScore >= 0.8f);
        }

        /// <summary>
        /// Get the most reliable witness
        /// </summary>
        public static WitnessReliability GetMostReliableWitness(List<WitnessReliability> reliabilities)
        {
            if (reliabilities == null || reliabilities.Count == 0)
                return null;

            return reliabilities.OrderByDescending(r => r.reliabilityScore).FirstOrDefault();
        }

        /// <summary>
        /// Adjust evidence strength based on witness reliability
        /// </summary>
        public static float AdjustEvidenceStrengthForReliability(
            float baseEvidenceStrength,
            List<WitnessReliability> reliabilities)
        {
            if (reliabilities == null || reliabilities.Count == 0)
                return baseEvidenceStrength;

            float avgReliability = GetAverageReliability(reliabilities);

            // Multiply base evidence by average reliability
            // This reduces evidence strength if witnesses are unreliable
            return baseEvidenceStrength * avgReliability;
        }

        /// <summary>
        /// Get reliability description for UI
        /// </summary>
        public static string GetReliabilityDescription(float reliabilityScore)
        {
            if (reliabilityScore >= 0.8f)
                return "LawAndOrder_ReliabilityCategory_HighlyReliable".Translate();
            if (reliabilityScore >= 0.6f)
                return "LawAndOrder_ReliabilityCategory_Reliable".Translate();
            if (reliabilityScore >= 0.4f)
                return "LawAndOrder_ReliabilityCategory_Questionable".Translate();
            if (reliabilityScore >= 0.2f)
                return "LawAndOrder_ReliabilityCategory_Unreliable".Translate();
            return "LawAndOrder_ReliabilityCategory_VeryUnreliable".Translate();
        }
    }
}
