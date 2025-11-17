using System.Collections.Generic;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// ThingComp for pawns with hidden identities (infiltrators, sanguophages)
    /// </summary>
    public class CompHiddenIdentity : ThingComp
    {
        private HiddenIdentity identity;

        public HiddenIdentity Identity => identity;
        public bool HasHiddenIdentity => identity != null;

        public CompProperties_HiddenIdentity Props => (CompProperties_HiddenIdentity)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Generate fake identity when pawn spawns (if not loading from save)
            if (!respawningAfterLoad && identity == null && ShouldHaveHiddenIdentity(parent as Pawn))
            {
                identity = GenerateFakeIdentity(parent as Pawn);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref identity, "identity");
        }

        /// <summary>
        /// Get the name that should be displayed to the player
        /// </summary>
        public string GetDisplayName()
        {
            if (identity == null || identity.identityRevealed)
                return (parent as Pawn)?.Name?.ToStringShort ?? "Unknown";

            return identity.displayName;
        }

        /// <summary>
        /// Progress the discovery of this pawn's hidden identity
        /// </summary>
        public void ProgressDiscovery(float amount, string clueType)
        {
            if (identity == null)
                return;

            identity.ProgressDiscovery(amount, clueType);

            // Check if we should reveal identity
            if (identity.ShouldReveal())
            {
                RevealIdentity();
            }
        }

        /// <summary>
        /// Reveal this pawn's true identity to the player
        /// </summary>
        private void RevealIdentity()
        {
            if (identity == null || identity.identityRevealed)
                return;

            identity.identityRevealed = true;

            Pawn pawn = parent as Pawn;
            if (pawn == null)
                return;

            // Dramatic reveal event!
            Find.LetterStack.ReceiveLetter(
                "LawAndOrder_LetterLabelInfiltratorRevealed".Translate(),
                "LawAndOrder_LetterInfiltratorRevealed".Translate(
                    identity.displayName,
                    identity.realName,
                    pawn.Named("INFILTRATOR")
                ),
                LetterDefOf.ThreatBig,
                pawn
            );

            ModLog.Info($"Identity revealed: {identity.displayName} is actually {identity.realName}");

            // Choose reaction based on intelligence
            ChooseReaction(pawn);
        }

        /// <summary>
        /// Choose how the infiltrator reacts to being exposed
        /// </summary>
        private void ChooseReaction(Pawn infiltrator)
        {
            if (identity == null || infiltrator == null)
                return;

            // Choose reaction based on intelligence
            var reaction = InfiltratorReactionHandler.ChooseReaction(infiltrator, identity);

            ModLog.Debug($"Infiltrator {infiltrator.LabelShort} reaction: {reaction}");

            // Execute the reaction
            InfiltratorReactionHandler.ExecuteReaction(infiltrator, reaction);
        }

        /// <summary>
        /// Determine if a pawn should have a hidden identity
        /// </summary>
        private bool ShouldHaveHiddenIdentity(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Check for sanguophage gene
            if (pawn.genes != null && ModsConfig.BiotechActive)
            {
                if (pawn.genes.HasActiveGene(GeneDefOf.Bloodfeeder))
                    return true;
            }

            // Check for hostile faction affiliation
            if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                // Potential infiltrator
                if (Rand.Chance(LawAndOrderMod.Settings.infiltratorSpawnChance))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Generate a fake identity for this infiltrator
        /// </summary>
        private HiddenIdentity GenerateFakeIdentity(Pawn pawn)
        {
            if (pawn == null)
                return null;

            var newIdentity = new HiddenIdentity();

            // Store real identity
            newIdentity.realName = pawn.Name?.ToStringShort ?? "Unknown";
            newIdentity.realFaction = pawn.Faction;
            newIdentity.realKind = pawn.kindDef;

            // Generate fake name (simple for now - could be enhanced)
            newIdentity.displayName = GenerateFakeName(pawn);

            // Create fake backstory
            newIdentity.displayBackstory = GenerateFakeBackstory(pawn);

            // Mask sanguophage gene if present
            if (pawn.genes != null && ModsConfig.BiotechActive)
            {
                newIdentity.maskSanguophageStatus = pawn.genes.HasActiveGene(GeneDefOf.Bloodfeeder);
            }

            // Hide suspicious traits
            newIdentity.maskedTraits = new List<string>();
            if (pawn.story?.traits != null)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
                    newIdentity.maskedTraits.Add("Psychopath");

                if (pawn.story.traits.HasTrait(TraitDefOf.Bloodlust))
                    newIdentity.maskedTraits.Add("Bloodlust");
            }

            // Calculate intelligence
            newIdentity.intelligenceStat = HiddenIdentity.CalculateIntelligence(pawn);

            ModLog.Debug($"Generated fake identity for {pawn.LabelShort}: {newIdentity.displayName} (intel: {newIdentity.intelligenceStat:F2})");

            return newIdentity;
        }

        /// <summary>
        /// Generate a fake name for the infiltrator
        /// </summary>
        private string GenerateFakeName(Pawn pawn)
        {
            return BackstoryFabricator.GenerateFakeName(pawn);
        }

        /// <summary>
        /// Generate a fake backstory for the infiltrator
        /// </summary>
        private string GenerateFakeBackstory(Pawn pawn)
        {
            return BackstoryFabricator.GenerateCoverStory(pawn);
        }
    }

    /// <summary>
    /// Properties for CompHiddenIdentity
    /// </summary>
    public class CompProperties_HiddenIdentity : CompProperties
    {
        public CompProperties_HiddenIdentity()
        {
            compClass = typeof(CompHiddenIdentity);
        }
    }

    /// <summary>
    /// Possible reactions when an infiltrator's identity is revealed
    /// </summary>
    public enum InfiltratorReaction
    {
        FleeMap,              // Try to escape
        FightToTheEnd,        // Go down fighting
        FakeInnocence,        // "This is a misunderstanding!"
        AttemptBribery,       // Offer information
        ActivateAccomplices,  // Trigger accomplice sabotage
        TransmitIntelNow      // Emergency intel transmission
    }
}
