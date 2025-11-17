using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Job driver for colonists to analyze crime scene clues
    /// Inspired by Anomaly DLC's study mechanics
    /// </summary>
    public class JobDriver_AnalyzeClue : JobDriver
    {
        private const int StudyDuration = 2400; // 40 seconds base (2400 ticks = 1 in-game hour)
        private const float StudyProgressPerTick = 0.00025f; // Base progress rate (very slow, skill-dependent)

        private CrimeSceneClue Clue => (CrimeSceneClue)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail if clue despawned or destroyed
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Fail if clue already analyzed
            this.FailOn(() => Clue.analyzed);

            // Go to the clue
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Analyze the clue
            Toil analyze = new Toil();
            analyze.initAction = delegate()
            {
                analyze.actor.jobs.curDriver.ticksLeftThisToil = StudyDuration;
            };

            analyze.tickAction = delegate()
            {
                // Study progress based on intellectual skill
                int intellectSkill = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
                float progressRate = StudyProgressPerTick * (1f + (intellectSkill * 0.1f));

                // Apply progress
                float oldProgress = Clue.studyProgress;
                Clue.studyProgress += progressRate;

                // Check for new revelations
                CheckForRevelations(Clue, oldProgress);

                // Mark as analyzed if complete
                if (Clue.studyProgress >= 1.0f)
                {
                    Clue.studyProgress = 1.0f;
                    Clue.analyzed = true;

                    ModLog.Debug($"{pawn.LabelShort} completed analysis of {Clue.clueType} clue");

                    ReadyForNextToil();
                }
            };

            analyze.defaultCompleteMode = ToilCompleteMode.Delay;
            analyze.defaultDuration = StudyDuration;

            // Show progress bar
            analyze.WithProgressBar(TargetIndex.A, () => Clue.studyProgress);

            // Play sound effect
            analyze.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);

            // Add experience
            analyze.activeSkill = () => SkillDefOf.Intellectual;

            yield return analyze;

            // Final toil - apply revelations
            Toil finalize = new Toil();
            finalize.initAction = delegate()
            {
                // Final check for any remaining revelations
                CheckForRevelations(Clue, 0f, true);

                // Phase 6: Check if this clue reveals planted evidence (false accusation)
                TruthDiscoveryUtils.TruthDiscoveryResult truthResult =
                    TruthDiscoveryUtils.TryDiscoverTruthDuringAnalysis(pawn, Clue);

                if (truthResult != null && truthResult.truthRevealed)
                {
                    // Apply truth discovery
                    TruthDiscoveryUtils.ApplyTruthDiscovery(truthResult);

                    // Exonerate the falsely accused pawn
                    if (truthResult.falselyAccused != null)
                    {
                        ExonerationSystem.ExoneratePawn(
                            truthResult.falselyAccused,
                            truthResult.falseAccusationCrime,
                            truthResult.falseAccuser
                        );
                    }
                }

                // Message if fully analyzed
                if (Clue.analyzed)
                {
                    Messages.Message(
                        "LawAndOrder_ClueFullyAnalyzed".Translate(pawn.LabelShort.Named("PAWN"), Clue.clueType.Named("CLUETYPE")),
                        Clue,
                        MessageTypeDefOf.PositiveEvent
                    );
                }
            };
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return finalize;
        }

        /// <summary>
        /// Check if any new revelations should be unlocked
        /// </summary>
        private void CheckForRevelations(CrimeSceneClue clue, float oldProgress, bool forceCheck = false)
        {
            if (clue.revelations == null)
                return;

            var newRevelations = clue.CheckForNewRevelations();

            foreach (var revelation in newRevelations)
            {
                // Roll accuracy check
                bool accurate = Rand.Chance(revelation.accuracyChance);

                Pawn pointsTo = revelation.pointsTo;

                // If inaccurate, might point to wrong pawn (rare)
                if (!accurate && !clue.isPlanted)
                {
                    pointsTo = GetRandomInnocentPawn();
                }

                // Mark as revealed
                clue.MarkRevelationRevealed(revelation);

                // Create message
                Messages.Message(
                    "LawAndOrder_ClueRevelation".Translate(
                        pawn.LabelShort.Named("PAWN"),
                        revelation.label.Named("LABEL"),
                        clue.clueType.ToString().Named("CLUETYPE"),
                        revelation.description.Named("DESCRIPTION")
                    ),
                    clue,
                    MessageTypeDefOf.PositiveEvent
                );

                // Link clue to case if it points to someone
                if (pointsTo != null)
                {
                    LinkClueToCase(clue, pointsTo);
                }

                ModLog.Debug($"Revelation unlocked: {revelation.label} (points to {pointsTo?.LabelShort ?? "no one"})");
            }
        }

        /// <summary>
        /// Link a clue to a criminal case and create evidence
        /// </summary>
        private void LinkClueToCase(CrimeSceneClue clue, Pawn suspect)
        {
            ModLog.Debug($"Clue {clue.clueType} linked to suspect {suspect.LabelShort}");

            // Create Evidence object from analyzed clue
            if (clue.linkedCrime != null)
            {
                CreateEvidenceFromClue(clue, suspect);

                // If crime is Hidden, upgrade its visibility
                if (clue.linkedCrime.visibilityState == CrimeVisibilityState.Hidden)
                {
                    // Transition to Suspected
                    clue.linkedCrime.TransitionToSuspected(
                        new List<Pawn> { pawn }, // Analyst is the "witness"
                        clue.clueQuality
                    );

                    Messages.Message(
                        "LawAndOrder_ClueRevealsHiddenCrime".Translate(
                            pawn.LabelShort.Named("PAWN"),
                            clue.clueType.Named("CLUETYPE"),
                            suspect.LabelShort.Named("SUSPECT"),
                            clue.linkedCrime.crimeType.Named("CRIMETYPE")
                        ),
                        suspect,
                        MessageTypeDefOf.NegativeEvent
                    );
                }
            }
        }

        /// <summary>
        /// Create an Evidence object from an analyzed clue
        /// </summary>
        private void CreateEvidenceFromClue(CrimeSceneClue clue, Pawn suspect)
        {
            // Determine evidence type based on clue type
            EvidenceType evidenceType = DetermineEvidenceType(clue.clueType);

            // Calculate evidence reliability based on clue quality and analyst skill
            float reliability = CalculateEvidenceReliability(clue);

            // Generate description
            string description = GenerateEvidenceDescription(clue, suspect);

            // Create the evidence
            Evidence evidence = new Evidence(
                evidenceType,
                description,
                reliability,
                pawn, // Analyst who collected it
                clue.Position,
                clue.linkedCrime
            );

            // Add type-specific data
            if (evidenceType == EvidenceType.Physical && clue is Thing thing)
            {
                evidence.physicalItem = thing;
            }

            // Link to planted evidence if applicable
            if (clue.isPlanted)
            {
                evidence.isPlanted = true;
                evidence.plantedBy = clue.plantedBy;
            }

            // Add to evidence manager
            var evidenceManager = Components.MapComponent_EvidenceManager.GetFor(pawn.Map);
            if (evidenceManager != null)
            {
                evidenceManager.AddEvidence(evidence);
                ModLog.Debug($"Evidence created: {description} (reliability: {reliability:F2})");
            }
            else
            {
                Mod.Log?.Warning("EvidenceManager not found on map!");
            }
        }

        /// <summary>
        /// Determine evidence type from clue type
        /// </summary>
        private EvidenceType DetermineEvidenceType(ClueType clueType)
        {
            switch (clueType)
            {
                case ClueType.BloodStain:
                case ClueType.FingerprintTrace:
                case ClueType.DroppedItem:
                case ClueType.Footprint:
                case ClueType.FabricScrap:
                case ClueType.ToolMark:
                    return EvidenceType.Physical;

                case ClueType.WitnessReport:
                    return EvidenceType.Testimonial;

                default:
                    return EvidenceType.Circumstantial;
            }
        }

        /// <summary>
        /// Calculate evidence reliability from clue quality and analyst skill
        /// </summary>
        private float CalculateEvidenceReliability(CrimeSceneClue clue)
        {
            // Base reliability from clue quality
            float baseReliability = clue.clueQuality;

            // Analyst's intellectual skill affects reliability
            int intellectSkill = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
            float skillBonus = intellectSkill * 0.01f; // Up to +20% for max skill

            // Freshness factor - old clues are less reliable
            int daysOld = clue.linkedCrime?.DaysAgo ?? 0;
            float freshnessFactor = 1.0f;
            if (daysOld > 5)
            {
                freshnessFactor = 0.8f; // 20% penalty for old evidence
            }

            float reliability = (baseReliability + skillBonus) * freshnessFactor;

            return UnityEngine.Mathf.Clamp01(reliability);
        }

        /// <summary>
        /// Generate a description for the evidence
        /// </summary>
        private string GenerateEvidenceDescription(CrimeSceneClue clue, Pawn suspect)
        {
            string crimeType = clue.linkedCrime?.GetCrimeLabel() ?? "unknown crime";
            return $"{clue.clueType} evidence linking {suspect.LabelShort} to {crimeType}";
        }

        /// <summary>
        /// Get a random innocent pawn for inaccurate revelations
        /// </summary>
        private Pawn GetRandomInnocentPawn()
        {
            var colonists = pawn.Map?.mapPawns?.FreeColonists;
            if (colonists == null || colonists.Count == 0)
                return null;

            return colonists.RandomElement();
        }

        public override string GetReport()
        {
            return "JobReport_AnalyzingClue".Translate(Clue?.clueType.ToString() ?? "clue");
        }
    }
}
