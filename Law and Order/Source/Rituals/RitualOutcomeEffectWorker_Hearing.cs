using System.Collections.Generic;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace LawAndOrder
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
        }

        /// <summary>
        /// Apply the ritual outcome effects
        /// </summary>
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn judge = jobRitual.PawnWithRole("judge");
            Pawn defendant = jobRitual.PawnWithRole("defendant");

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
                // TODO: Add LawAndOrder_PresidedOverHearing thought in Phase 5
                // judge.needs.mood.thoughts.memories.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_PresidedOverHearing);
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

                // TODO: Add LawAndOrder_AttendedHearing thought in Phase 5
                // spectator.needs?.mood?.thoughts?.memories?.TryGainMemory(LawAndOrder_ThoughtDefOf.LawAndOrder_AttendedHearing);
            }
        }
    }
}
