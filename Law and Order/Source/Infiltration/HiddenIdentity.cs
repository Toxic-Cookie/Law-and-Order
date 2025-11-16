using System.Collections.Generic;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Stores hidden identity information for infiltrators and sanguophages
    /// Allows pawns to conceal their true nature from the player
    /// </summary>
    public class HiddenIdentity : IExposable
    {
        // Display identity (what player sees)
        public string displayName;              // Fake name shown to player
        public string displayBackstory;         // Fabricated background
        public List<string> maskedTraits;       // Traits hidden from inspection
        public bool maskSanguophageStatus;      // Hide bloodsucker gene

        // True identity (hidden until discovered)
        public string realName;                 // Actual name
        public string realBackstory;            // True background
        public Faction realFaction;             // Hostile faction affiliation
        public PawnKindDef realKind;            // Actual pawn type

        // Discovery tracking
        public bool identityRevealed;           // Has player discovered the truth?
        public float discoveryProgress;         // 0-100, investigation uncovers truth
        public List<string> discoveredClues;    // Which clues have been found

        // Behavior intelligence
        public float intelligenceStat;          // 0.0-1.0, how smart this infiltrator is

        public HiddenIdentity()
        {
            maskedTraits = new List<string>();
            discoveredClues = new List<string>();
        }

        public void ExposeData()
        {
            // Display identity
            Scribe_Values.Look(ref displayName, "displayName");
            Scribe_Values.Look(ref displayBackstory, "displayBackstory");
            Scribe_Collections.Look(ref maskedTraits, "maskedTraits", LookMode.Value);
            Scribe_Values.Look(ref maskSanguophageStatus, "maskSanguophageStatus", false);

            // True identity
            Scribe_Values.Look(ref realName, "realName");
            Scribe_Values.Look(ref realBackstory, "realBackstory");
            Scribe_References.Look(ref realFaction, "realFaction");
            Scribe_Defs.Look(ref realKind, "realKind");

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
        /// Get the name that should be displayed to the player
        /// </summary>
        public string GetDisplayName()
        {
            return identityRevealed ? realName : displayName;
        }

        /// <summary>
        /// Get the backstory that should be displayed to the player
        /// </summary>
        public string GetDisplayBackstory()
        {
            return identityRevealed ? realBackstory : displayBackstory;
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
                ModLog.Debug($"Identity discovery progress: {discoveryProgress}% (clue: {clueType})");
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
        /// Calculate intelligence stat based on pawn's skills and traits
        /// </summary>
        public static float CalculateIntelligence(Pawn pawn)
        {
            if (pawn == null)
                return 0.5f;

            float intel = 0.5f; // Base

            // Social skill affects deception
            if (pawn.skills != null)
            {
                intel += pawn.skills.GetSkill(SkillDefOf.Social).Level * 0.02f;

                // Intellectual skill affects planning
                intel += pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.015f;
            }

            // Traits modify intelligence
            if (pawn.story?.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
                    intel += 0.1f; // Cold, calculating

                // Check for fast/slow learner traits
                var fastLearnerTrait = DefDatabase<TraitDef>.GetNamedSilentFail("FastLearner");
                var slowLearnerTrait = DefDatabase<TraitDef>.GetNamedSilentFail("SlowLearner");

                if (fastLearnerTrait != null && pawn.story.traits.HasTrait(fastLearnerTrait))
                    intel += 0.1f; // Fast learner

                if (slowLearnerTrait != null && pawn.story.traits.HasTrait(slowLearnerTrait))
                    intel -= 0.2f; // Slow learner
            }

            return UnityEngine.Mathf.Clamp01(intel);
        }
    }
}
