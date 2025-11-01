using System.Collections.Generic;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual outcome effect worker for court hearings.
    /// Applies post-ritual effects like mood thoughts to participants.
    /// The actual plea bargain logic is handled by RitualBehaviorWorker_Hearing.PostCleanup()
    /// </summary>
    public class RitualOutcomeEffectWorker_Hearing : RitualOutcomeEffectWorker
    {
        public RitualOutcomeEffectWorker_Hearing()
        {
        }

        public RitualOutcomeEffectWorker_Hearing(RitualOutcomeEffectDef def) : base(def)
        {
            // Initialize compDatas even if comps is empty to prevent NullReferenceException
            // Base constructor calls FillCompData() which doesn't initialize if comps is empty
            if (this.compDatas == null)
            {
                this.compDatas = new List<RitualOutcomeComp_Data>();
            }
        }

        /// <summary>
        /// Apply the ritual outcome effects
        /// </summary>
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn judge = jobRitual.PawnWithRole("judge");
            Pawn defendant = jobRitual.PawnWithRole("defendant");
            Pawn victim = jobRitual.PawnWithRole("victim");

            if (defendant == null)
            {
                Law_and_Order.Source.Mod.Log?.Warning("Hearing ritual completed but defendant is null");
                return;
            }

            // The plea bargain behavior is handled by RitualBehaviorWorker_Hearing.PostCleanup()

            // Get criminal and debt records
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(defendant);
            var debtRecord = DebtUtils.TryGetDebtRecord(defendant);
            float totalDebt = debtRecord?.CurrentDebt ?? 0f;

            // Build the outcome message
            string message = $"{defendant.LabelShort} has been tried in court.\n\n";

            if (criminalRecord != null)
            {
                message += $"Crimes on record: {criminalRecord.TotalCrimeCount}\n";
            }

            message += $"Remaining debt: {totalDebt:F0} silver";

            // Apply thoughts to participants
            ApplyThoughtsToParticipants(jobRitual, judge, defendant, totalPresence);

            // Log social interactions that occurred during the hearing
            LogCourtInteractions(judge, defendant, victim);

            // Send completion letter
            Find.LetterStack.ReceiveLetter(
                "Hearing Complete",
                message,
                LetterDefOf.NeutralEvent,
                defendant,
                null,
                null,
                null,
                null,
                0,
                true
            );
        }

        /// <summary>
        /// Apply appropriate thoughts to all participants
        /// </summary>
        private void ApplyThoughtsToParticipants(LordJob_Ritual jobRitual, Pawn judge, Pawn defendant, Dictionary<Pawn, int> totalPresence)
        {
            // Judge gets a positive thought for performing their duty
            if (judge?.needs?.mood?.thoughts?.memories != null)
            {
                judge.needs.mood.thoughts.memories.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_PresidedOverHearing);
            }

            // Defendant already gets thoughts from the plea bargain system
            // So we don't add additional thoughts here

            // Spectators get a small positive thought
            foreach (var presence in totalPresence)
            {
                Pawn spectator = presence.Key;

                // Skip the judge and defendant
                if (spectator == judge || spectator == defendant)
                {
                    continue;
                }

                // Skip if not a colonist
                if (!spectator.IsColonist)
                {
                    continue;
                }

                spectator.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_AttendedHearing);
            }
        }

        /// <summary>
        /// Log social interactions that occurred during the court hearing
        /// </summary>
        private void LogCourtInteractions(Pawn judge, Pawn defendant, Pawn victim)
        {
            if (judge == null || defendant == null)
            {
                return;
            }

            // Get interaction defs
            InteractionDef judgeQuestionsInteraction = DefDatabase<InteractionDef>.GetNamedSilentFail("LawAndOrder_JudgeQuestions");
            InteractionDef defendantPleadsInteraction = DefDatabase<InteractionDef>.GetNamedSilentFail("LawAndOrder_DefendantPleads");
            InteractionDef judgeSentencesInteraction = DefDatabase<InteractionDef>.GetNamedSilentFail("LawAndOrder_JudgeSentences");
            InteractionDef victimConfrontsInteraction = DefDatabase<InteractionDef>.GetNamedSilentFail("LawAndOrder_VictimConfronts");

            // 1. Judge questions defendant
            if (judgeQuestionsInteraction != null)
            {
                PlayLogEntry_Interaction questioningEntry = new PlayLogEntry_Interaction(
                    judgeQuestionsInteraction,
                    judge,
                    defendant,
                    null
                );
                Find.PlayLog.Add(questioningEntry);
            }

            // 2. Defendant pleads to judge
            if (defendantPleadsInteraction != null)
            {
                PlayLogEntry_Interaction pleaEntry = new PlayLogEntry_Interaction(
                    defendantPleadsInteraction,
                    defendant,
                    judge,
                    null
                );
                Find.PlayLog.Add(pleaEntry);
            }

            // 3. Victim confronts defendant (if victim is present)
            if (victim != null && victimConfrontsInteraction != null)
            {
                PlayLogEntry_Interaction victimEntry = new PlayLogEntry_Interaction(
                    victimConfrontsInteraction,
                    victim,
                    defendant,
                    null
                );
                Find.PlayLog.Add(victimEntry);
            }

            // 4. Judge sentences defendant
            if (judgeSentencesInteraction != null)
            {
                PlayLogEntry_Interaction sentenceEntry = new PlayLogEntry_Interaction(
                    judgeSentencesInteraction,
                    judge,
                    defendant,
                    null
                );
                Find.PlayLog.Add(sentenceEntry);
            }
        }
    }
}
