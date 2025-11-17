using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Hediff that stores a pawn's real identity while they operate under a false one.
    /// Hidden from the health tab - this is just a data container.
    /// </summary>
    public class Hediff_HiddenIdentity : HediffWithComps
    {
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

        public Hediff_HiddenIdentity()
        {
            maskedTraits = new List<string>();
            discoveredClues = new List<string>();
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

            // Post-load init
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (maskedTraits == null)
                    maskedTraits = new List<string>();
                if (discoveredClues == null)
                    discoveredClues = new List<string>();
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
    }
}
