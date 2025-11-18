using System.Collections.Generic;
using Verse;
using RimWorld;
using LawAndOrder;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Hediff that stores a pawn's real identity while they operate under a false one.
    /// Hidden from the health tab - this is just a data container.
    /// </summary>
    public class Hediff_HiddenIdentity : HediffWithComps
    {
        /// <summary>
        /// Hide this hediff from all UI displays
        /// </summary>
        public override bool Visible => false;
        // Real identity (what we're hiding)
        public Name realName;                   // Actual name - restored when revealed
        public BackstoryDef realBackstoryChildhood;
        public BackstoryDef realBackstoryAdulthood;
        public Faction realFaction;             // Hostile faction affiliation
        public PawnKindDef realKind;            // Actual pawn type

        // Fake identity (currently active)
        public Name fakeName;                   // What pawn.Name is set to
        public string fakeBackstory;            // Fabricated background
        public List<string> maskedTraits;       // Traits hidden from inspection
        public bool maskSanguophageStatus;      // Hide bloodsucker gene

        // Discovery tracking
        public bool identityRevealed;           // Has player discovered the truth?
        public float discoveryProgress;         // 0-100, investigation uncovers truth
        public List<string> discoveredClues;    // Which clues have been found

        // Behavior intelligence
        public float intelligenceStat;          // 0.0-1.0, how smart this infiltrator is

        // Intelligence gathering (Phase 6.5)
        public Dictionary<IntelCategory, IntelligenceData> gatheredIntelligence; // Intel being gathered
        public int lastIntelGatherTick;         // Last time intel was gathered (for rate limiting)

        public Hediff_HiddenIdentity()
        {
            maskedTraits = new List<string>();
            discoveredClues = new List<string>();
            gatheredIntelligence = new Dictionary<IntelCategory, IntelligenceData>();
            lastIntelGatherTick = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Real identity
            Scribe_Deep.Look(ref realName, "realName");
            Scribe_Defs.Look(ref realBackstoryChildhood, "realBackstoryChildhood");
            Scribe_Defs.Look(ref realBackstoryAdulthood, "realBackstoryAdulthood");
            Scribe_References.Look(ref realFaction, "realFaction");
            Scribe_Defs.Look(ref realKind, "realKind");

            // Fake identity
            Scribe_Deep.Look(ref fakeName, "fakeName");
            Scribe_Values.Look(ref fakeBackstory, "fakeBackstory");
            Scribe_Collections.Look(ref maskedTraits, "maskedTraits", LookMode.Value);
            Scribe_Values.Look(ref maskSanguophageStatus, "maskSanguophageStatus", false);

            // Discovery tracking
            Scribe_Values.Look(ref identityRevealed, "identityRevealed", false);
            Scribe_Values.Look(ref discoveryProgress, "discoveryProgress", 0f);
            Scribe_Collections.Look(ref discoveredClues, "discoveredClues", LookMode.Value);

            // Behavior intelligence
            Scribe_Values.Look(ref intelligenceStat, "intelligenceStat", 0.5f);

            // Intelligence gathering (Phase 6.5)
            Scribe_Collections.Look(ref gatheredIntelligence, "gatheredIntelligence", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref lastIntelGatherTick, "lastIntelGatherTick", 0);

            // Post-load init
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (maskedTraits == null)
                    maskedTraits = new List<string>();
                if (discoveredClues == null)
                    discoveredClues = new List<string>();
                if (gatheredIntelligence == null)
                    gatheredIntelligence = new Dictionary<IntelCategory, IntelligenceData>();
            }
        }

        /// <summary>
        /// Check if a specific trait should be masked
        /// </summary>
        public bool ShouldMaskTrait(string traitDefName)
        {
            if (identityRevealed)
                return false;

            return maskedTraits != null && maskedTraits.Contains(traitDefName);
        }

        /// <summary>
        /// Progress the discovery of this hidden identity
        /// </summary>
        public void ProgressDiscovery(float amount, string clueType)
        {
            if (identityRevealed)
                return;

            discoveryProgress += amount;

            if (!string.IsNullOrEmpty(clueType) && !discoveredClues.Contains(clueType))
            {
                discoveredClues.Add(clueType);
            }

            // Clamp progress
            discoveryProgress = UnityEngine.Mathf.Clamp(discoveryProgress, 0f, 100f);

            if (Prefs.DevMode)
            {
                Utils.ModLog.Debug($"Identity discovery progress: {discoveryProgress}% (clue: {clueType})");
            }
        }

        /// <summary>
        /// Check if identity should be revealed based on discovery progress
        /// </summary>
        public bool ShouldReveal()
        {
            return !identityRevealed && discoveryProgress >= 100f;
        }

        /// <summary>
        /// Reveal the infiltrator's true identity and change to real faction
        /// </summary>
        public void RevealIdentity()
        {
            if (identityRevealed || pawn == null)
                return;

            identityRevealed = true;

            // Restore real name
            if (realName != null)
            {
                pawn.Name = realName;
            }

            // Change to real hostile faction
            if (realFaction != null)
            {
                Faction oldFaction = pawn.Faction;
                pawn.SetFaction(realFaction, null);

                // Send player letter about revealed infiltrator
                TaggedString letterText = "LawAndOrder.InfiltratorRevealed".Translate(
                    pawn.NameShortColored,
                    realFaction.Name,
                    oldFaction?.Name ?? "Unknown"
                );
                TaggedString letterLabel = "LawAndOrder.InfiltratorRevealedLabel".Translate();

                Find.LetterStack.ReceiveLetter(
                    letterLabel,
                    letterText,
                    LetterDefOf.ThreatBig,
                    pawn
                );

                if (Prefs.DevMode)
                {
                    Utils.ModLog.Debug($"Revealed infiltrator {pawn.LabelShort}: {oldFaction?.Name ?? "None"} -> {realFaction.Name}");
                }
            }
        }

        /// <summary>
        /// Get or create intelligence data for a specific category
        /// </summary>
        public IntelligenceData GetOrCreateIntelligence(IntelCategory category, Map map)
        {
            if (gatheredIntelligence == null)
                gatheredIntelligence = new Dictionary<IntelCategory, IntelligenceData>();

            if (!gatheredIntelligence.ContainsKey(category))
            {
                gatheredIntelligence[category] = new IntelligenceData(category, map.uniqueID, pawn);
            }

            return gatheredIntelligence[category];
        }

        /// <summary>
        /// Check if this infiltrator can gather more intelligence (rate limiting)
        /// </summary>
        public bool CanGatherIntelNow()
        {
            if (identityRevealed)
                return false; // Can't gather intel if exposed

            int ticksSinceLastGather = Find.TickManager.TicksGame - lastIntelGatherTick;
            int gatherCooldown = GenDate.TicksPerHour * 2; // 2 hour cooldown between gathering

            return ticksSinceLastGather >= gatherCooldown;
        }

        /// <summary>
        /// Gather intelligence for a random category
        /// </summary>
        public void GatherIntelligence(Map map)
        {
            if (!CanGatherIntelNow())
                return;

            // Choose a random category to focus on
            IntelCategory category = (IntelCategory)Rand.RangeInclusive(0, 3);

            // Get or create intel data
            IntelligenceData intel = GetOrCreateIntelligence(category, map);

            // Calculate progress amount
            float progress = IntelligenceUtils.GatherIntelligence(pawn, map, category);

            // Improve completeness
            intel.ImproveCompleteness(progress);

            // Populate details based on current progress
            IntelligenceUtils.PopulateIntelDetails(intel, map);

            // Update last gather time
            lastIntelGatherTick = Find.TickManager.TicksGame;

            if (Prefs.DevMode)
            {
                Utils.ModLog.Debug($"Infiltrator {pawn.LabelShort} gathered {category} intel: {intel.completeness * 100f:F1}% complete");
            }
        }

        /// <summary>
        /// Get all intelligence that has been gathered
        /// </summary>
        public List<IntelligenceData> GetAllGatheredIntelligence()
        {
            if (gatheredIntelligence == null)
                return new List<IntelligenceData>();

            return new List<IntelligenceData>(gatheredIntelligence.Values);
        }

        /// <summary>
        /// Mark all intelligence as transmitted (when infiltrator leaves map)
        /// </summary>
        public void TransmitAllIntelligence()
        {
            if (gatheredIntelligence == null)
                return;

            foreach (var intel in gatheredIntelligence.Values)
            {
                if (!intel.transmitted)
                {
                    intel.MarkTransmitted();
                }
            }
        }
    }
}
