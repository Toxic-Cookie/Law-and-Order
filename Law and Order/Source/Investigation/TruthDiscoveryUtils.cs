using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Settings;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Utilities for discovering the truth behind false accusations
    /// Integrates with interrogation and clue analysis systems
    /// </summary>
    public static class TruthDiscoveryUtils
    {
        /// <summary>
        /// Result of a truth discovery investigation
        /// </summary>
        public class TruthDiscoveryResult
        {
            public bool truthRevealed;
            public Crime falseAccusationCrime;
            public Pawn actualPerpetrator;      // Who really did it (null if no real crime)
            public Pawn falseAccuser;           // Who made the false accusation
            public Pawn falselyAccused;         // Who was wrongly accused
            public string discoveryMethod;      // "interrogation", "evidence_analysis", "witness_contradiction"
        }

        /// <summary>
        /// Try to discover truth during interrogation
        /// Called from JobDriver_InvestigateCase
        /// </summary>
        public static TruthDiscoveryResult TryDiscoverTruthDuringInterrogation(Pawn interrogator, Pawn suspect)
        {
            if (interrogator == null || suspect == null)
                return null;

            var result = new TruthDiscoveryResult();

            // Get all suspected crimes for this pawn
            var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(suspect);
            if (criminalRecord == null)
                return null;

            var suspectedCrimes = criminalRecord.GetSuspectedCrimes();
            var falseCrimes = suspectedCrimes.Where(c => c.isFalseAccusation).ToList();

            if (!falseCrimes.Any())
                return null;

            // Base chance to discover truth
            float baseChance = LawAndOrderSettings.TruthDiscoveryChance?.Value ?? 0.15f;

            // Social skill bonus
            float socialSkill = interrogator.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0f;
            float skillBonus = socialSkill * 0.01f; // +1% per social level

            // Intelligence penalty (smart suspects hide truth better)
            float suspectIntel = suspect.GetStatValue(StatDefOf.PsychicSensitivity);
            float intelPenalty = suspectIntel * 0.1f;

            float finalChance = baseChance + skillBonus - intelPenalty;

            if (!Rand.Chance(finalChance))
                return null;

            // Truth discovered! Pick a random false crime
            var discoveredCrime = falseCrimes.RandomElement();

            result.truthRevealed = true;
            result.falseAccusationCrime = discoveredCrime;
            result.actualPerpetrator = discoveredCrime.actualPerpetrator;
            result.falseAccuser = discoveredCrime.witnesses?.FirstOrDefault(); // First witness is usually the accuser
            result.falselyAccused = suspect;
            result.discoveryMethod = "interrogation";

            Mod.Log?.Message($"[Truth Discovery] {interrogator.LabelShort} discovered {suspect.LabelShort} was falsely accused of {discoveredCrime.crimeType}");

            return result;
        }

        /// <summary>
        /// Try to discover truth during clue analysis
        /// Called from JobDriver_AnalyzeClue
        /// </summary>
        public static TruthDiscoveryResult TryDiscoverTruthDuringAnalysis(Pawn researcher, CrimeSceneClue clue)
        {
            if (researcher == null || clue == null || !clue.isPlanted)
                return null;

            var result = new TruthDiscoveryResult();

            // Base chance to detect planted evidence
            float baseChance = LawAndOrderSettings.TruthDiscoveryChance?.Value ?? 0.15f;

            // Intellectual skill bonus
            float intellectSkill = researcher.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0f;
            float skillBonus = intellectSkill * 0.02f; // +2% per intellectual level

            // Clue quality penalty (high quality fakes are harder to detect)
            float qualityPenalty = clue.clueQuality * 0.15f;

            float finalChance = baseChance + skillBonus - qualityPenalty;

            if (!Rand.Chance(finalChance))
                return null;

            // Planted evidence detected!
            result.truthRevealed = true;
            result.falseAccusationCrime = clue.linkedCrime;
            result.actualPerpetrator = clue.plantedBy; // Who planted the evidence
            result.falseAccuser = clue.plantedBy;
            result.falselyAccused = clue.linkedCriminal; // Who the evidence points to
            result.discoveryMethod = "evidence_analysis";

            Mod.Log?.Message($"[Truth Discovery] {researcher.LabelShort} detected planted evidence in {clue.clueType}");

            return result;
        }

        /// <summary>
        /// Check for witness contradictions that reveal false accusations
        /// Called periodically by WorldComponent
        /// </summary>
        public static TruthDiscoveryResult TryDiscoverContradictions(Crime crime)
        {
            if (crime == null || !crime.isFalseAccusation)
                return null;

            // Check if there are multiple witnesses with contradicting stories
            if (crime.witnesses == null || crime.witnesses.Count < 2)
                return null;

            var result = new TruthDiscoveryResult();

            // Low chance for contradiction to be noticed
            if (!Rand.Chance(0.05f)) // 5% per check
                return null;

            result.truthRevealed = true;
            result.falseAccusationCrime = crime;
            result.actualPerpetrator = crime.actualPerpetrator;
            result.falseAccuser = crime.witnesses.FirstOrDefault();
            result.falselyAccused = crime.falselyAccused;
            result.discoveryMethod = "witness_contradiction";

            Mod.Log?.Message($"[Truth Discovery] Witness contradictions revealed false accusation in {crime.crimeType}");

            return result;
        }

        /// <summary>
        /// Apply the truth discovery results
        /// </summary>
        public static void ApplyTruthDiscovery(TruthDiscoveryResult result)
        {
            if (result == null || !result.truthRevealed)
                return;

            var crime = result.falseAccusationCrime;
            if (crime == null)
                return;

            // Mark the crime as discovered
            crime.additionalInfo += " [Truth Discovered]";

            // Notify player with appropriate message based on discovery method
            string messageKey = result.discoveryMethod switch
            {
                "interrogation" => "LawAndOrder_TruthDiscovered_Interrogation",
                "evidence_analysis" => "LawAndOrder_TruthDiscovered_Evidence",
                "witness_contradiction" => "LawAndOrder_TruthDiscovered_Contradiction",
                _ => "LawAndOrder_TruthDiscovered_Generic"
            };

            Messages.Message(
                messageKey.Translate(
                    result.falselyAccused?.LabelShort ?? "Unknown",
                    crime.crimeType.ToString(),
                    result.falseAccuser?.LabelShort ?? "Unknown"
                ),
                new LookTargets(new Pawn[] { result.falselyAccused, result.falseAccuser }.Where(p => p != null).ToArray()),
                MessageTypeDefOf.NeutralEvent
            );

            // Add letter for important discoveries
            if (crime.visibilityState == CrimeVisibilityState.Suspected)
            {
                Find.LetterStack.ReceiveLetter(
                    "LawAndOrder_TruthDiscovered_Letter_Title".Translate(),
                    "LawAndOrder_TruthDiscovered_Letter_Text".Translate(
                        result.falselyAccused?.LabelShort ?? "Unknown",
                        crime.GetCrimeLabel(),
                        result.falseAccuser?.LabelShort ?? "Unknown",
                        result.discoveryMethod.Replace("_", " ")
                    ),
                    LetterDefOf.NeutralEvent,
                    new LookTargets(new Pawn[] { result.falselyAccused, result.falseAccuser }.Where(p => p != null).ToArray())
                );
            }
        }

        /// <summary>
        /// Get inconsistency score for a crime (higher = more suspicious)
        /// Used by UI to show "disputed" status
        /// </summary>
        public static float GetInconsistencyScore(Crime crime)
        {
            if (crime == null)
                return 0f;

            float score = 0f;

            // Low evidence strength is suspicious
            if (crime.evidenceStrength < 0.3f)
                score += 0.3f;

            // Single witness is less reliable
            if (crime.witnesses != null && crime.witnesses.Count == 1)
                score += 0.2f;

            // Check if witness has grudge against accused
            if (crime.witnesses != null && crime.falselyAccused != null)
            {
                var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(crime.falselyAccused);
                if (criminalRecord != null)
                {
                    foreach (var witness in crime.witnesses)
                    {
                        if (witness?.relations != null)
                        {
                            int opinion = witness.relations.OpinionOf(crime.falselyAccused);
                            if (opinion < -20)
                            {
                                score += 0.3f; // Grudge witness
                                break;
                            }
                        }
                    }
                }
            }

            // Check for planted evidence
            // Note: We need to get the map from somewhere else since IntVec3 doesn't have GetMap()
            // For now, skip this check if we don't have direct map access
            Map crimeMap = Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
            if (crimeMap != null)
            {
                var evidenceManager = crimeMap.GetComponent<Components.MapComponent_EvidenceManager>();
                if (evidenceManager != null)
                {
                    var evidence = evidenceManager.GetEvidenceForCrime(crime);
                    if (evidence != null && evidence.Any(e => e.isPlanted))
                    {
                        score += 0.4f;
                    }
                }
            }

            return Verse.GenMath.LerpDoubleClamped(0f, 1.2f, 0f, 1f, score);
        }

        /// <summary>
        /// Check if a crime should be marked as "disputed" in UI
        /// </summary>
        public static bool IsDisputed(Crime crime)
        {
            return GetInconsistencyScore(crime) > 0.5f;
        }

        /// <summary>
        /// Get tooltip explaining why a crime is disputed
        /// </summary>
        public static string GetDisputedTooltip(Crime crime)
        {
            if (crime == null || !IsDisputed(crime))
                return "";

            var reasons = new List<string>();

            if (crime.evidenceStrength < 0.3f)
                reasons.Add("- Weak evidence");

            if (crime.witnesses != null && crime.witnesses.Count == 1)
                reasons.Add("- Single witness testimony");

            if (crime.witnesses != null && crime.falselyAccused != null)
            {
                foreach (var witness in crime.witnesses)
                {
                    if (witness?.relations != null && witness.relations.OpinionOf(crime.falselyAccused) < -20)
                    {
                        reasons.Add($"- {witness.LabelShort} has grudge against accused");
                        break;
                    }
                }
            }

            Map crimeMap2 = Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
            if (crimeMap2 != null)
            {
                var evidenceManager = crimeMap2.GetComponent<Components.MapComponent_EvidenceManager>();
                if (evidenceManager != null)
                {
                    var evidence = evidenceManager.GetEvidenceForCrime(crime);
                    if (evidence != null && evidence.Any(e => e.isPlanted))
                    {
                        reasons.Add("- Suspicious evidence detected");
                    }
                }
            }

            return string.Join("\n", reasons);
        }
    }
}
