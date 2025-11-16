using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Result of an interrogation attempt
    /// </summary>
    public class InterrogationResult
    {
        public Pawn suspect;
        public Pawn interrogator;
        public bool wasSuccessful;
        public bool falseConfession;
        public List<Crime> crimesConfessed = new List<Crime>();
        public float successChance;
        public string failureReason;

        public InterrogationResult(Pawn suspect, Pawn interrogator)
        {
            this.suspect = suspect;
            this.interrogator = interrogator;
        }
    }

    /// <summary>
    /// Core interrogation system for revealing Hidden crimes through confession.
    /// Uses social skill checks, relationship modifiers, and room quality.
    /// </summary>
    public static class InterrogationSystem
    {
        // Base confession chance before modifiers
        private const float BASE_CONFESSION_CHANCE = 0.3f;

        // Skill-based modifiers
        private const float SOCIAL_SKILL_FACTOR = 0.05f; // +5% per social skill level
        private const float MAX_SKILL_BONUS = 0.5f; // Cap at +50%

        // Relationship modifiers
        private const float FRIEND_PENALTY = -0.15f; // -15% if friends
        private const float RIVAL_BONUS = 0.1f; // +10% if rivals/enemies

        // Room quality modifiers
        private const float ROOM_QUALITY_FACTOR = 0.02f; // +2% per room impressiveness level

        // False confession chance
        private const float FALSE_CONFESSION_BASE_CHANCE = 0.05f; // 5% base chance

        // Minimum confession chance
        private const float MIN_CONFESSION_CHANCE = 0.1f; // At least 10%

        /// <summary>
        /// Perform an interrogation of a suspect to attempt to reveal Hidden crimes.
        /// </summary>
        /// <param name="interrogator">The warden performing the interrogation</param>
        /// <param name="suspect">The prisoner being interrogated</param>
        /// <param name="interrogationTable">The table being used (affects quality bonus)</param>
        /// <returns>InterrogationResult containing outcome and crimes confessed</returns>
        public static InterrogationResult PerformInterrogation(Pawn interrogator, Pawn suspect, Thing interrogationTable = null)
        {
            var result = new InterrogationResult(suspect, interrogator);

            // Calculate base success chance
            result.successChance = CalculateConfessionChance(interrogator, suspect, interrogationTable);

            // Roll for success
            result.wasSuccessful = Rand.Chance(result.successChance);

            if (!result.wasSuccessful)
            {
                // Interrogation failed
                result.failureReason = GenerateFailureReason(interrogator, suspect);
                return result;
            }

            // Check for false confession (low chance)
            float falseConfessionChance = CalculateFalseConfessionChance(interrogator, suspect);
            if (Rand.Chance(falseConfessionChance))
            {
                result.falseConfession = true;
                // Generate a false confession for a crime they didn't commit
                GenerateFalseConfession(result);
                return result;
            }

            // Success! Reveal Hidden crimes
            var hiddenCrimes = GetHiddenCrimes(suspect);

            if (hiddenCrimes.Count == 0)
            {
                // No hidden crimes to confess
                result.wasSuccessful = false;
                result.failureReason = "LawAndOrder_Interrogation_NoHiddenCrimes";
                return result;
            }

            // Randomly select 1-3 crimes to confess based on success margin
            int crimesToConfess = CalculateCrimesToConfess(result.successChance, hiddenCrimes.Count);

            for (int i = 0; i < crimesToConfess && i < hiddenCrimes.Count; i++)
            {
                Crime crime = hiddenCrimes.RandomElement();
                RevealCrime(crime, suspect, interrogator);
                result.crimesConfessed.Add(crime);
                hiddenCrimes.Remove(crime);
            }

            return result;
        }

        /// <summary>
        /// Calculate the chance of a successful confession
        /// </summary>
        private static float CalculateConfessionChance(Pawn interrogator, Pawn suspect, Thing interrogationTable)
        {
            float chance = BASE_CONFESSION_CHANCE;

            // Social skill bonus
            int socialSkill = interrogator.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float skillBonus = Mathf.Min(socialSkill * SOCIAL_SKILL_FACTOR, MAX_SKILL_BONUS);
            chance += skillBonus;

            // Relationship modifier
            if (suspect.relations != null && interrogator.relations != null)
            {
                int opinion = suspect.relations.OpinionOf(interrogator);

                if (opinion >= 20)
                {
                    // Friends are harder to confess
                    chance += FRIEND_PENALTY;
                }
                else if (opinion <= -20)
                {
                    // Rivals/enemies more likely to confess
                    chance += RIVAL_BONUS;
                }
            }

            // Room quality bonus (interrogation table room quality)
            if (interrogationTable != null && interrogationTable.Spawned)
            {
                Room tableRoom = interrogationTable.GetRoom();

                // Apply room quality bonus - interrogating in a high-quality room is more effective
                if (tableRoom != null && !tableRoom.PsychologicallyOutdoors)
                {
                    float impressiveness = tableRoom.GetStat(RoomStatDefOf.Impressiveness);
                    chance += impressiveness * ROOM_QUALITY_FACTOR;
                }
            }

            // Suspect health/consciousness penalty
            if (suspect.health != null)
            {
                float consciousness = suspect.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                if (consciousness < 1f)
                {
                    chance *= consciousness; // Reduced chance if suspect is impaired
                }
            }

            // Ensure minimum chance
            chance = Mathf.Max(chance, MIN_CONFESSION_CHANCE);

            // Cap at 95%
            chance = Mathf.Min(chance, 0.95f);

            return chance;
        }

        /// <summary>
        /// Calculate chance of false confession
        /// </summary>
        private static float CalculateFalseConfessionChance(Pawn interrogator, Pawn suspect)
        {
            float chance = FALSE_CONFESSION_BASE_CHANCE;

            // Low social skill increases false confession chance
            int socialSkill = interrogator.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            if (socialSkill < 5)
            {
                chance += 0.05f; // +5% if unskilled
            }

            // Suspect's will/mental state affects false confession
            if (suspect.mindState != null && suspect.mindState.mentalBreaker != null)
            {
                float breakLevel = suspect.mindState.mentalBreaker.CurMood;
                if (breakLevel < 0.3f)
                {
                    chance += 0.1f; // +10% if close to mental break
                }
            }

            return Mathf.Min(chance, 0.2f); // Cap at 20%
        }

        /// <summary>
        /// Get all Hidden crimes for a suspect
        /// </summary>
        private static List<Crime> GetHiddenCrimes(Pawn suspect)
        {
            Hediff_Crimes crimeRecord = suspect.health?.hediffSet?.GetFirstHediffOfDef(Hediffs.HediffDefOf.LawAndOrder_CriminalRecord) as Hediff_Crimes;

            if (crimeRecord == null || crimeRecord.Crimes == null)
            {
                return new List<Crime>();
            }

            return crimeRecord.Crimes
                .Where(c => c.visibilityState == CrimeVisibilityState.Hidden)
                .ToList();
        }

        /// <summary>
        /// Calculate how many crimes to confess based on success margin
        /// </summary>
        private static int CalculateCrimesToConfess(float successChance, int totalHiddenCrimes)
        {
            if (totalHiddenCrimes == 0) return 0;

            // Higher success chance = more crimes confessed
            if (successChance >= 0.8f)
            {
                return Mathf.Min(3, totalHiddenCrimes); // Confess up to 3 crimes
            }
            else if (successChance >= 0.5f)
            {
                return Mathf.Min(2, totalHiddenCrimes); // Confess up to 2 crimes
            }
            else
            {
                return 1; // Confess 1 crime
            }
        }

        /// <summary>
        /// Reveal a Hidden crime by transitioning it to Suspected
        /// </summary>
        private static void RevealCrime(Crime crime, Pawn suspect, Pawn interrogator)
        {
            // Transition from Hidden to Suspected
            crime.visibilityState = CrimeVisibilityState.Suspected;

            // Set evidence strength to 70-90% (confession is strong evidence)
            crime.evidenceStrength = Rand.Range(0.7f, 0.9f);

            // Add interrogator as a "witness" (they heard the confession)
            if (!crime.witnesses.Contains(interrogator))
            {
                crime.witnesses.Add(interrogator);
            }

            // Mark that this was confessed (we'll add this field later)
            crime.additionalInfo = (crime.additionalInfo ?? "") + " [Confessed during interrogation]";

            // Get or create case for this suspect
            WorldComponent_JusticeManager justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            if (justiceManager != null)
            {
                CriminalCase existingCase = justiceManager.GetOrCreateCase(suspect);
                existingCase.AddCrime(crime);
                crime.caseId = existingCase.caseId;
            }
        }

        /// <summary>
        /// Generate a false confession (low chance)
        /// </summary>
        private static void GenerateFalseConfession(InterrogationResult result)
        {
            // For now, just mark it as false confession
            // In Phase 5.6, we'll implement full false accusation mechanics
            result.failureReason = "LawAndOrder_Interrogation_FalseConfession";
        }

        /// <summary>
        /// Generate a failure reason message
        /// </summary>
        private static string GenerateFailureReason(Pawn interrogator, Pawn suspect)
        {
            // Randomly select a failure reason for flavor
            List<string> reasons = new List<string>
            {
                "LawAndOrder_Interrogation_Refused",
                "LawAndOrder_Interrogation_Uncooperative",
                "LawAndOrder_Interrogation_Silent",
                "LawAndOrder_Interrogation_Evasive"
            };

            return reasons.RandomElement();
        }

        /// <summary>
        /// Check if a pawn can be interrogated
        /// </summary>
        public static bool CanInterrogate(Pawn suspect)
        {
            if (suspect == null || suspect.Dead)
            {
                return false;
            }

            // Must be prisoner
            if (!suspect.IsPrisoner)
            {
                return false;
            }

            // Must not be downed
            if (suspect.Downed)
            {
                return false;
            }

            // Must not be in bed (sleeping, resting, or recovering)
            if (suspect.InBed())
            {
                return false;
            }

            // Must be conscious
            if (suspect.health == null ||
                suspect.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) < 0.5f)
            {
                return false;
            }

            // Must be able to move
            if (!suspect.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
            {
                return false;
            }

            // Must have Hidden crimes
            var hiddenCrimes = GetHiddenCrimes(suspect);
            if (hiddenCrimes.Count == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all prisoners who can be interrogated
        /// </summary>
        public static List<Pawn> GetInterrogatablePrisoners(Map map)
        {
            return map.mapPawns.PrisonersOfColony
                .Where(p => CanInterrogate(p))
                .ToList();
        }
    }
}
