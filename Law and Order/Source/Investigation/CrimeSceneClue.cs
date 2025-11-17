using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Types of clues that can be found at crime scenes
    /// </summary>
    public enum ClueType
    {
        BloodStain,        // Blood at crime scene
        Footprint,         // Shoe prints
        ToolMark,          // Damage pattern
        DroppedItem,       // Item left behind
        FabricScrap,       // Torn clothing
        FingerprintTrace,  // Touch evidence
        WitnessReport      // Verbal clue from interrogation
    }

    /// <summary>
    /// Progressive revelation data for studying clues
    /// </summary>
    public class ClueRevelation : IExposable
    {
        public float threshold;         // Study progress needed (0-1)
        public string label;            // "Blood Type Analysis"
        public string description;      // "The blood matches [Pawn]..."
        public float accuracyChance;    // Can be wrong!
        public Pawn pointsTo;           // Who does this implicate?
        public bool revealed;           // Has this revelation been shown yet?

        public ClueRevelation()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref threshold, "threshold", 0f);
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref accuracyChance, "accuracyChance", 1f);
            Scribe_References.Look(ref pointsTo, "pointsTo");
            Scribe_Values.Look(ref revealed, "revealed", false);
        }
    }

    /// <summary>
    /// Physical evidence thing that can be discovered and studied at crime scenes
    /// Inspired by Anomaly DLC's Sightstealer mechanics
    /// </summary>
    public class CrimeSceneClue : Thing
    {
        // What crime does this clue relate to?
        public Crime linkedCrime;
        public Pawn linkedCriminal;     // Who left this clue?
        public CrimeType crimeType;

        // Clue properties
        public ClueType clueType;       // Blood, Footprint, Tool Mark, etc.
        public float clueQuality;       // 0-1, how revealing is this clue?
        public bool isPlanted;          // False evidence?
        public Pawn plantedBy;          // Who planted it?

        // Study/discovery system
        public float studyProgress;     // Progress toward full analysis (0-1)
        public bool analyzed;           // Has this been fully studied?
        public List<ClueRevelation> revelations; // What does this reveal?

        // Visual
        public bool discovered;         // Has player found it yet?
        public int ticksToDecay;        // Clues fade over time

        // Decay constants
        private const int DEFAULT_DECAY_TICKS = GenDate.TicksPerDay * 10; // 10 days

        public CrimeSceneClue()
        {
            revelations = new List<ClueRevelation>();
            ticksToDecay = DEFAULT_DECAY_TICKS;
            discovered = true; // Visible by default for now
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Crime links
            Scribe_Deep.Look(ref linkedCrime, "linkedCrime");
            Scribe_References.Look(ref linkedCriminal, "linkedCriminal");
            Scribe_Values.Look(ref crimeType, "crimeType", CrimeType.Unknown);

            // Clue properties
            Scribe_Values.Look(ref clueType, "clueType", ClueType.BloodStain);
            Scribe_Values.Look(ref clueQuality, "clueQuality", 0.5f);
            Scribe_Values.Look(ref isPlanted, "isPlanted", false);
            Scribe_References.Look(ref plantedBy, "plantedBy");

            // Study system
            Scribe_Values.Look(ref studyProgress, "studyProgress", 0f);
            Scribe_Values.Look(ref analyzed, "analyzed", false);
            Scribe_Collections.Look(ref revelations, "revelations", LookMode.Deep);

            // Visual
            Scribe_Values.Look(ref discovered, "discovered", true);
            Scribe_Values.Look(ref ticksToDecay, "ticksToDecay", DEFAULT_DECAY_TICKS);

            // Post-load init
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (revelations == null)
                    revelations = new List<ClueRevelation>();
            }
        }

        public override void TickRare()
        {
            // NOTE: Do NOT call base.TickRare() - Entity's implementation throws NotImplementedException
            // RimWorld pattern: override TickRare/TickLong without calling base (see Corpse, Blight, etc.)

            // Decay over time (TickRare is called every 250 ticks)
            if (ticksToDecay > 0)
            {
                ticksToDecay -= 250;

                if (ticksToDecay <= 0)
                {
                    // Clue has decayed, destroy it
                    this.Destroy(DestroyMode.Vanish);
                }
            }
        }

        public override string GetInspectString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            // Use Append (not AppendLine) for base to avoid empty lines if base returns empty string
            sb.Append(base.GetInspectString());

            sb.AppendInNewLine($"Clue Type: {clueType}");
            sb.AppendInNewLine($"Quality: {clueQuality:P0}");
            sb.AppendInNewLine($"Study Progress: {studyProgress:P0}");

            if (analyzed)
            {
                sb.AppendInNewLine("Fully analyzed");
            }
            else
            {
                sb.AppendInNewLine($"Analysis remaining: {(1f - studyProgress):P0}");
            }

            if (isPlanted && Prefs.DevMode)
            {
                sb.AppendInNewLine($"[DEV] Planted by: {plantedBy?.LabelShort ?? "Unknown"}");
            }

            int daysUntilDecay = ticksToDecay / GenDate.TicksPerDay;
            sb.AppendInNewLine($"Decays in: {daysUntilDecay} days");

            return sb.ToString().TrimEndNewlines();
        }

        /// <summary>
        /// Check for new revelations based on current study progress
        /// </summary>
        public List<ClueRevelation> CheckForNewRevelations()
        {
            if (revelations == null)
                return new List<ClueRevelation>();

            return revelations
                .Where(r => !r.revealed && studyProgress >= r.threshold)
                .ToList();
        }

        /// <summary>
        /// Mark a revelation as revealed
        /// </summary>
        public void MarkRevelationRevealed(ClueRevelation revelation)
        {
            if (revelation != null)
            {
                revelation.revealed = true;
            }
        }
    }

    /// <summary>
    /// Static utility class for generating clues
    /// </summary>
    public static class ClueGenerator
    {
        /// <summary>
        /// Generate clues for a crime
        /// </summary>
        public static void GenerateCluesForCrime(
            Crime crime,
            Pawn criminal,
            IntVec3 location,
            Map map)
        {
            if (crime == null || criminal == null || map == null)
                return;

            // Intelligence determines if criminal leaves clues
            float clueLikelihood = 1.0f;

            var hiddenIdentity = criminal.TryGetComp<Infiltration.CompHiddenIdentity>();
            if (hiddenIdentity != null && hiddenIdentity.HasHiddenIdentity)
            {
                // Smart criminals leave fewer clues
                clueLikelihood -= hiddenIdentity.Identity.intelligenceStat * 0.5f;
            }

            // Crime type affects clue generation
            int numClues = GetClueCountForCrime(crime.crimeType);

            for (int i = 0; i < numClues; i++)
            {
                if (Rand.Chance(clueLikelihood))
                {
                    SpawnClue(crime, criminal, location, map);
                }
            }
        }

        /// <summary>
        /// Determine how many clues a crime type should generate
        /// </summary>
        private static int GetClueCountForCrime(CrimeType crimeType)
        {
            // Use settings for min/max
            int min = LawAndOrderMod.Settings.cluesPerCrime_Min;
            int max = LawAndOrderMod.Settings.cluesPerCrime_Max;

            switch (crimeType)
            {
                case CrimeType.Murder:
                case CrimeType.Assault:
                    return Rand.RangeInclusive(min, max);

                case CrimeType.Theft:
                case CrimeType.Arson:
                case CrimeType.PropertyDestruction:
                    return Rand.RangeInclusive(min, Mathf.Max(min, max - 1));

                default:
                    return Rand.RangeInclusive(min, Mathf.Max(min, max - 1));
            }
        }

        /// <summary>
        /// Determine clue type based on crime type
        /// </summary>
        public static ClueType DetermineClueType(CrimeType crimeType)
        {
            switch (crimeType)
            {
                case CrimeType.Murder:
                case CrimeType.Assault:
                    return new[] { ClueType.BloodStain, ClueType.Footprint, ClueType.DroppedItem }.RandomElement();

                case CrimeType.Theft:
                    return new[] { ClueType.Footprint, ClueType.FingerprintTrace, ClueType.DroppedItem }.RandomElement();

                case CrimeType.Arson:
                    return new[] { ClueType.Footprint, ClueType.ToolMark, ClueType.FabricScrap }.RandomElement();

                case CrimeType.PropertyDestruction:
                case CrimeType.Vandalism:
                    return new[] { ClueType.ToolMark, ClueType.Footprint }.RandomElement();

                default:
                    return ClueType.Footprint;
            }
        }

        /// <summary>
        /// Spawn a clue at a location
        /// </summary>
        private static void SpawnClue(Crime crime, Pawn criminal, IntVec3 loc, Map map)
        {
            ClueType type = DetermineClueType(crime.crimeType);
            ThingDef clueDef = GetClueThingDef(type);

            if (clueDef == null)
            {
                ModLog.Warning($"No ThingDef found for clue type {type}");
                return;
            }

            // Create clue thing
            CrimeSceneClue clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);

            // Calculate clue quality based on circumstances
            float quality = CalculateClueQuality(crime, criminal, loc, map);

            // Link to crime
            clue.linkedCrime = crime;
            clue.linkedCriminal = criminal;
            clue.crimeType = crime.crimeType;
            clue.clueType = type;
            clue.clueQuality = quality;
            clue.isPlanted = false;
            clue.plantedBy = null;

            // Generate revelations
            clue.revelations = GenerateRevelations(clue, criminal);

            // Find spawn location
            IntVec3 spawnLoc = FindClueSpawnLocation(loc, map);

            // Spawn in world
            GenSpawn.Spawn(clue, spawnLoc, map);

            ModLog.Debug($"Spawned {type} clue (quality {quality:P0}) at {spawnLoc} for {crime.crimeType} by {criminal.LabelShort}");
        }

        /// <summary>
        /// Get the ThingDef for a clue type
        /// </summary>
        public static ThingDef GetClueThingDef(ClueType type)
        {
            // Will be defined in ThingDefs XML
            switch (type)
            {
                case ClueType.BloodStain:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_Blood");
                case ClueType.Footprint:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_Footprint");
                case ClueType.ToolMark:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_ToolMark");
                case ClueType.DroppedItem:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_DroppedItem");
                case ClueType.FabricScrap:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_FabricScrap");
                case ClueType.FingerprintTrace:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_Fingerprint");
                default:
                    return DefDatabase<ThingDef>.GetNamedSilentFail("CrimeSceneClue_Blood");
            }
        }

        /// <summary>
        /// Calculate clue quality based on crime circumstances
        /// </summary>
        private static float CalculateClueQuality(Crime crime, Pawn criminal, IntVec3 loc, Map map)
        {
            float quality = 0.5f; // Base

            // Lighting affects quality
            float light = map.glowGrid.GroundGlowAt(loc);
            quality += light * 0.2f;

            // Hasty crimes leave better clues (criminals who were witnessed likely rushed)
            if (crime.witnesses != null && crime.witnesses.Any())
                quality += 0.2f; // Criminal rushed

            // Intelligence reduces clue quality
            var hiddenId = criminal.TryGetComp<Infiltration.CompHiddenIdentity>();
            if (hiddenId != null && hiddenId.HasHiddenIdentity)
                quality -= hiddenId.Identity.intelligenceStat * 0.3f;

            return Mathf.Clamp01(quality);
        }

        /// <summary>
        /// Find a suitable location to spawn a clue near the crime location
        /// </summary>
        private static IntVec3 FindClueSpawnLocation(IntVec3 center, Map map)
        {
            // Try to find a nearby valid cell
            for (int i = 0; i < 8; i++)
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

        /// <summary>
        /// Generate revelations for a clue
        /// </summary>
        private static List<ClueRevelation> GenerateRevelations(CrimeSceneClue clue, Pawn criminal)
        {
            var revelations = new List<ClueRevelation>();

            // Basic revelation at 25% study
            revelations.Add(new ClueRevelation
            {
                threshold = 0.25f,
                label = $"Initial_{clue.clueType}_Analysis".Translate(),
                description = GetBasicDescription(clue.clueType),
                accuracyChance = 0.7f,
                pointsTo = null // General information
            });

            // Intermediate at 50%
            revelations.Add(new ClueRevelation
            {
                threshold = 0.50f,
                label = $"Detailed_{clue.clueType}_Examination".Translate(),
                description = GetDetailedDescription(clue.clueType, criminal),
                accuracyChance = 0.85f,
                pointsTo = criminal // May be wrong if planted!
            });

            // Complete at 100%
            revelations.Add(new ClueRevelation
            {
                threshold = 1.0f,
                label = $"Complete_{clue.clueType}_Profile".Translate(),
                description = GetCompleteDescription(clue.clueType, criminal),
                accuracyChance = clue.isPlanted ? 0.95f : 1.0f, // Still small chance of error
                pointsTo = clue.isPlanted ? clue.plantedBy : criminal
            });

            return revelations;
        }

        /// <summary>
        /// Get basic description for a clue type
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
        /// Get detailed description for a clue type
        /// </summary>
        private static string GetDetailedDescription(ClueType type, Pawn criminal)
        {
            switch (type)
            {
                case ClueType.BloodStain:
                    return "Clue_Blood_Detailed".Translate(criminal.LabelShort);
                case ClueType.Footprint:
                    return "Clue_Footprint_Detailed".Translate(criminal.LabelShort);
                case ClueType.ToolMark:
                    return "Clue_ToolMark_Detailed".Translate(criminal.LabelShort);
                case ClueType.DroppedItem:
                    return "Clue_DroppedItem_Detailed".Translate(criminal.LabelShort);
                case ClueType.FabricScrap:
                    return "Clue_FabricScrap_Detailed".Translate(criminal.LabelShort);
                case ClueType.FingerprintTrace:
                    return "Clue_Fingerprint_Detailed".Translate(criminal.LabelShort);
                default:
                    return $"Evidence points to {criminal.LabelShort}";
            }
        }

        /// <summary>
        /// Get complete description for a clue type
        /// </summary>
        private static string GetCompleteDescription(ClueType type, Pawn criminal)
        {
            switch (type)
            {
                case ClueType.BloodStain:
                    return "Clue_Blood_Complete".Translate(criminal.LabelShort);
                case ClueType.Footprint:
                    return "Clue_Footprint_Complete".Translate(criminal.LabelShort);
                case ClueType.ToolMark:
                    return "Clue_ToolMark_Complete".Translate(criminal.LabelShort);
                case ClueType.DroppedItem:
                    return "Clue_DroppedItem_Complete".Translate(criminal.LabelShort);
                case ClueType.FabricScrap:
                    return "Clue_FabricScrap_Complete".Translate(criminal.LabelShort);
                case ClueType.FingerprintTrace:
                    return "Clue_Fingerprint_Complete".Translate(criminal.LabelShort);
                default:
                    return $"Conclusive evidence: {criminal.LabelShort} is the perpetrator";
            }
        }
    }
}
