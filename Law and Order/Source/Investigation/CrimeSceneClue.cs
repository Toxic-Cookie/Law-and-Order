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

        public override void TickLong()
        {
            base.TickLong();

            // Decay over time (TickLong is called every 2000 ticks)
            if (ticksToDecay > 0)
            {
                ticksToDecay -= 2000;

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
            sb.AppendLine(base.GetInspectString());

            sb.AppendLine($"Clue Type: {clueType}");
            sb.AppendLine($"Quality: {clueQuality:P0}");
            sb.AppendLine($"Study Progress: {studyProgress:P0}");

            if (analyzed)
            {
                sb.AppendLine("Fully analyzed");
            }
            else
            {
                sb.AppendLine($"Analysis remaining: {(1f - studyProgress):P0}");
            }

            if (isPlanted && Prefs.DevMode)
            {
                sb.AppendLine($"[DEV] Planted by: {plantedBy?.LabelShort ?? "Unknown"}");
            }

            int daysUntilDecay = ticksToDecay / GenDate.TicksPerDay;
            sb.AppendLine($"Decays in: {daysUntilDecay} days");

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
        /// Spawn a clue at a location (placeholder for now - ThingDefs will be added in XML)
        /// </summary>
        private static void SpawnClue(Crime crime, Pawn criminal, IntVec3 loc, Map map)
        {
            // Note: This will be fully implemented once we create the ThingDefs in XML
            // For now, just log that we would spawn a clue

            ClueType type = DetermineClueType(crime.crimeType);

            ModLog.Debug($"Would spawn {type} clue at {loc} for {crime.crimeType} by {criminal.LabelShort}");

            // TODO Phase 6.2: Actually spawn CrimeSceneClue Thing once ThingDefs are created
        }
    }
}
