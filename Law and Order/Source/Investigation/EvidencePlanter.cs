using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hediffs;
using System.Collections.Generic;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// System for infiltrators to plant false evidence and frame innocent pawns
    /// </summary>
    public static class EvidencePlanter
    {
        /// <summary>
        /// Plant false evidence to frame an innocent pawn for a crime
        /// </summary>
        public static void PlantFalseEvidence(
            Pawn infiltrator,
            Crime crime,
            Pawn scapegoat,
            IntVec3 location,
            Map map)
        {
            if (infiltrator == null || crime == null || scapegoat == null || map == null)
                return;

            // Only intelligent criminals attempt evidence planting
            var hiddenId = infiltrator.TryGetComp<Infiltration.CompHiddenIdentity>();
            if (hiddenId == null || !hiddenId.HasHiddenIdentity)
                return;

            if (hiddenId.Hediff.intelligenceStat < 0.6f)
                return; // Not smart enough to plant evidence

            // Generate fake clue
            ClueType type = ClueGenerator.DetermineClueType(crime.crimeType);
            ThingDef clueDef = ClueGenerator.GetClueThingDef(type);

            if (clueDef == null)
            {
                ModLog.Warning($"Cannot plant evidence: No ThingDef for {type}");
                return;
            }

            // Create fake clue thing
            CrimeSceneClue fakeClue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);

            // Point to scapegoat (WRONG!)
            fakeClue.linkedCrime = crime;
            fakeClue.linkedCriminal = scapegoat; // Frame the innocent
            fakeClue.crimeType = crime.crimeType;
            fakeClue.clueType = type;
            fakeClue.isPlanted = true;
            fakeClue.plantedBy = infiltrator;

            // High quality fake (smart criminal makes it convincing)
            float intelligence = hiddenId.Hediff.intelligenceStat;
            fakeClue.clueQuality = 0.8f + (intelligence * 0.2f);

            // Generate revelations that point to scapegoat
            fakeClue.revelations = GenerateFalseRevelations(type, scapegoat, intelligence);

            // Find spawn location
            IntVec3 spawnLoc = FindPlantingLocation(location, map);

            // Spawn fake evidence
            GenSpawn.Spawn(fakeClue, spawnLoc, map);

            ModLog.Debug($"Infiltrator {infiltrator.LabelShort} planted false {type} at {spawnLoc} implicating {scapegoat.LabelShort}");
        }

        /// <summary>
        /// Generate false revelations that implicate the scapegoat
        /// </summary>
        private static List<ClueRevelation> GenerateFalseRevelations(ClueType type, Pawn scapegoat, float intelligence)
        {
            var revelations = new List<ClueRevelation>();

            // Basic revelation - generic info
            revelations.Add(new ClueRevelation
            {
                threshold = 0.25f,
                label = $"Initial_{type}_Analysis".Translate(),
                description = GetBasicDescription(type),
                accuracyChance = 0.7f,
                pointsTo = null
            });

            // Intermediate - points to scapegoat
            revelations.Add(new ClueRevelation
            {
                threshold = 0.50f,
                label = $"Detailed_{type}_Examination".Translate(),
                description = $"Evidence matches {scapegoat.LabelShort}",
                accuracyChance = 0.9f, // Very convincing!
                pointsTo = scapegoat
            });

            // Complete - conclusive (but false)
            revelations.Add(new ClueRevelation
            {
                threshold = 1.0f,
                label = $"Complete_{type}_Profile".Translate(),
                description = $"Conclusive evidence: {scapegoat.LabelShort} is the perpetrator",
                accuracyChance = 0.95f + (intelligence * 0.05f), // Smart criminals make it very convincing
                pointsTo = scapegoat
            });

            return revelations;
        }

        /// <summary>
        /// Get basic description for a clue type (reused from ClueGenerator)
        /// </summary>
        private static string GetBasicDescription(ClueType type)
        {
            switch (type)
            {
                case ClueType.BloodStain:
                    return "Clue_Blood_Basic".Translate();
                case ClueType.Footprint:
                    return "Clue_Footprint_Basic".Translate();
                case ClueType.ToolMark:
                    return "Clue_ToolMark_Basic".Translate();
                case ClueType.DroppedItem:
                    return "Clue_DroppedItem_Basic".Translate();
                case ClueType.FabricScrap:
                    return "Clue_FabricScrap_Basic".Translate();
                case ClueType.FingerprintTrace:
                    return "Clue_Fingerprint_Basic".Translate();
                default:
                    return "Generic clue analysis";
            }
        }

        /// <summary>
        /// Find a good location to plant evidence
        /// </summary>
        private static IntVec3 FindPlantingLocation(IntVec3 center, Map map)
        {
            // Try to find a nearby valid cell
            for (int i = 0; i < 12; i++)
            {
                IntVec3 candidate = center + GenRadial.RadialPattern[i];
                if (candidate.InBounds(map) && candidate.Standable(map))
                {
                    return candidate;
                }
            }

            // Fallback to center
            return center;
        }
    }
}
