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
        /// Link a clue to a criminal case
        /// </summary>
        private void LinkClueToCase(CrimeSceneClue clue, Pawn suspect)
        {
            // TODO: Link to CriminalCase system
            // For now, just log
            ModLog.Debug($"Clue {clue.clueType} linked to suspect {suspect.LabelShort}");

            // If there's a linked crime, upgrade its visibility
            if (clue.linkedCrime != null && clue.linkedCrime.visibilityState == CrimeVisibilityState.Hidden)
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
