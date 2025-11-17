using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// ThingComp for pawns with hidden identities (infiltrators, sanguophages)
    /// This comp manages the Hediff_HiddenIdentity and handles name swapping
    /// </summary>
    public class CompHiddenIdentity : ThingComp
    {
        private Hediff_HiddenIdentity cachedHediff;

        /// <summary>
        /// Get the hidden identity hediff for this pawn
        /// </summary>
        public Hediff_HiddenIdentity Hediff
        {
            get
            {
                if (cachedHediff == null || cachedHediff.pawn != parent)
                {
                    Pawn pawn = parent as Pawn;
                    if (pawn?.health?.hediffSet != null)
                    {
                        cachedHediff = pawn.health.hediffSet.GetFirstHediffOfDef(
                            LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity;
                    }
                }
                return cachedHediff;
            }
        }

        public bool HasHiddenIdentity => Hediff != null;

        public CompProperties_HiddenIdentity Props => (CompProperties_HiddenIdentity)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // Generate fake identity when pawn spawns (if not loading from save)
            if (!respawningAfterLoad && Hediff == null && ShouldHaveHiddenIdentity(parent as Pawn))
            {
                InitializeHiddenIdentity(parent as Pawn);
            }
        }

        /// <summary>
        /// Initialize hidden identity for this pawn
        /// Stores real identity in hediff and changes pawn.Name to fake name
        /// </summary>
        public void InitializeHiddenIdentity(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return;

            // Create the hediff
            var hediff = (Hediff_HiddenIdentity)HediffMaker.MakeHediff(
                LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity, pawn);

            // Store REAL identity in hediff
            hediff.realName = pawn.Name;
            hediff.realFaction = pawn.Faction;
            hediff.realKind = pawn.kindDef;

            // Store real backstories
            if (pawn.story != null)
            {
                hediff.realBackstoryChildhood = pawn.story.GetBackstory(BackstorySlot.Childhood);
                hediff.realBackstoryAdulthood = pawn.story.GetBackstory(BackstorySlot.Adulthood);
            }

            // Generate FAKE identity
            hediff.fakeName = GenerateFakeName(pawn);
            hediff.fakeBackstory = BackstoryFabricator.GenerateCoverStory(pawn);

            // Mask sanguophage gene if present
            if (pawn.genes != null && ModsConfig.BiotechActive)
            {
                hediff.maskSanguophageStatus = pawn.genes.HasActiveGene(GeneDefOf.Bloodfeeder);
            }

            // Calculate intelligence
            hediff.intelligenceStat = CalculateIntelligence(pawn);

            // Add hediff to pawn
            pawn.health.AddHediff(hediff);
            cachedHediff = hediff;

            // CRITICAL: Set pawn.Name to fake name (RimWorld's native name system)
            // This makes the fake name appear everywhere automatically!
            pawn.Name = hediff.fakeName;

            ModLog.Debug($"Initialized hidden identity for {hediff.realName.ToStringShort} -> fake name: {hediff.fakeName.ToStringShort} (intel: {hediff.intelligenceStat:F2})");
        }

        /// <summary>
        /// Progress the discovery of this pawn's hidden identity
        /// </summary>
        public void ProgressDiscovery(float amount, string clueType)
        {
            var hediff = Hediff;
            if (hediff == null)
                return;

            hediff.ProgressDiscovery(amount, clueType);

            // Check if we should reveal identity
            if (hediff.ShouldReveal())
            {
                RevealIdentity();
            }
        }

        /// <summary>
        /// Reveal this pawn's true identity to the player
        /// </summary>
        public void RevealIdentity()
        {
            var hediff = Hediff;
            if (hediff == null || hediff.identityRevealed)
                return;

            Pawn pawn = parent as Pawn;
            if (pawn == null)
                return;

            string fakeName = pawn.Name.ToStringShort; // Currently displayed fake name

            // Mark as revealed
            hediff.identityRevealed = true;

            // CRITICAL: Restore real name (RimWorld's native name system)
            pawn.Name = hediff.realName;

            // Dramatic reveal event!
            Find.LetterStack.ReceiveLetter(
                "LawAndOrder_LetterLabelInfiltratorRevealed".Translate(),
                "LawAndOrder_LetterInfiltratorRevealed".Translate(
                    fakeName,
                    hediff.realName.ToStringShort,
                    pawn.Named("INFILTRATOR")
                ),
                LetterDefOf.ThreatBig,
                pawn
            );

            ModLog.Info($"Identity revealed: {fakeName} is actually {hediff.realName.ToStringShort}");

            // Choose reaction based on intelligence
            ChooseReaction(pawn);
        }

        /// <summary>
        /// Choose how the infiltrator reacts to being exposed
        /// </summary>
        private void ChooseReaction(Pawn infiltrator)
        {
            var hediff = Hediff;
            if (hediff == null || infiltrator == null)
                return;

            // Choose reaction based on intelligence
            var reaction = InfiltratorReactionHandler.ChooseReaction(infiltrator, hediff);

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
        /// Generate a fake name for the infiltrator
        /// </summary>
        private Name GenerateFakeName(Pawn pawn)
        {
            // Use RimWorld's name generator to create a realistic fake name
            NameStyle style = NameStyle.Full;
            string forcedLastName = null;
            bool forceNoNick = false;

            Name fakeName = PawnBioAndNameGenerator.GeneratePawnName(
                pawn,
                style,
                forcedLastName,
                forceNoNick,
                pawn.genes?.Xenotype
            );

            return fakeName;
        }

        /// <summary>
        /// Calculate intelligence stat based on pawn's skills and traits
        /// </summary>
        private static float CalculateIntelligence(Pawn pawn)
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
