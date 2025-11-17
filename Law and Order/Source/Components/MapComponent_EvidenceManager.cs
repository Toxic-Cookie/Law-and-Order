using System.Collections.Generic;
using System.Linq;
using Verse;
using Law_and_Order.Source.Investigation;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// Manages all evidence collected on this map.
    /// Evidence is linked to crimes and criminal cases.
    /// </summary>
    public class MapComponent_EvidenceManager : MapComponent
    {
        // All evidence collected on this map
        private List<Evidence> allEvidence = new List<Evidence>();

        // Evidence grouped by case ID for quick lookup
        private Dictionary<int, List<Evidence>> evidenceByCaseId = new Dictionary<int, List<Evidence>>();

        // Evidence grouped by crime for quick lookup
        private Dictionary<Crime, List<Evidence>> evidenceByCrime = new Dictionary<Crime, List<Evidence>>();

        public MapComponent_EvidenceManager(Map map) : base(map)
        {
        }

        /// <summary>
        /// Get the EvidenceManager instance for a map
        /// </summary>
        public static MapComponent_EvidenceManager GetFor(Map map)
        {
            return map?.GetComponent<MapComponent_EvidenceManager>();
        }

        /// <summary>
        /// Add evidence to the manager and link it to a crime
        /// </summary>
        public void AddEvidence(Evidence evidence)
        {
            if (evidence == null)
                return;

            allEvidence.Add(evidence);

            // Link to crime if present
            if (evidence.linkedCrime != null)
            {
                if (!evidenceByCrime.ContainsKey(evidence.linkedCrime))
                {
                    evidenceByCrime[evidence.linkedCrime] = new List<Evidence>();
                }
                evidenceByCrime[evidence.linkedCrime].Add(evidence);

                // Update crime's evidence strength
                UpdateCrimeEvidenceStrength(evidence.linkedCrime);

                // Link to case if crime has a case ID
                if (evidence.linkedCrime.caseId >= 0)
                {
                    if (!evidenceByCaseId.ContainsKey(evidence.linkedCrime.caseId))
                    {
                        evidenceByCaseId[evidence.linkedCrime.caseId] = new List<Evidence>();
                    }
                    evidenceByCaseId[evidence.linkedCrime.caseId].Add(evidence);
                }
            }

            Mod.Log?.Message($"Evidence added: {evidence.description} (reliability: {evidence.reliability:F2})");
        }

        /// <summary>
        /// Get all evidence for a specific case
        /// </summary>
        public List<Evidence> GetEvidenceForCase(int caseId)
        {
            if (evidenceByCaseId.TryGetValue(caseId, out var evidence))
            {
                return evidence;
            }
            return new List<Evidence>();
        }

        /// <summary>
        /// Get all evidence for a specific crime
        /// </summary>
        public List<Evidence> GetEvidenceForCrime(Crime crime)
        {
            if (evidenceByCrime.TryGetValue(crime, out var evidence))
            {
                return evidence;
            }
            return new List<Evidence>();
        }

        /// <summary>
        /// Calculate the overall evidence strength for a case using quality-based formula.
        /// Quality > Quantity: Best evidence contributes most, additional evidence has diminishing returns.
        /// </summary>
        public float CalculateCaseEvidenceStrength(int caseId)
        {
            var evidence = GetEvidenceForCase(caseId);
            if (evidence == null || evidence.Count == 0)
                return 0f;

            // Sort by reliability (quality) descending
            var sortedEvidence = evidence.OrderByDescending(e => e.reliability).ToList();

            // Best evidence contributes fully
            float maxQuality = sortedEvidence[0].reliability;

            // Additional evidence has diminishing returns (30% effectiveness)
            float diminishingFactor = 0.3f;
            float sumOfRemaining = 0f;

            for (int i = 1; i < sortedEvidence.Count; i++)
            {
                sumOfRemaining += sortedEvidence[i].reliability;
            }

            float caseStrength = maxQuality + (sumOfRemaining * diminishingFactor);

            // Cap at 1.0
            return UnityEngine.Mathf.Min(caseStrength, 1.0f);
        }

        /// <summary>
        /// Get the quality tier description for an evidence strength value
        /// </summary>
        public static string GetEvidenceQualityTier(float strength)
        {
            if (strength >= 0.9f) return "LawAndOrder_Evidence_Quality_Conclusive".Translate();
            if (strength >= 0.7f) return "LawAndOrder_Evidence_Quality_Strong".Translate();
            if (strength >= 0.3f) return "LawAndOrder_Evidence_Quality_Moderate".Translate();
            return "LawAndOrder_Evidence_Quality_Unreliable".Translate();
        }

        /// <summary>
        /// Update a crime's evidence strength based on all linked evidence
        /// </summary>
        private void UpdateCrimeEvidenceStrength(Crime crime)
        {
            var evidence = GetEvidenceForCrime(crime);
            if (evidence == null || evidence.Count == 0)
            {
                crime.evidenceStrength = 0f;
                return;
            }

            // Use the same quality-based formula
            var sortedEvidence = evidence.OrderByDescending(e => e.reliability).ToList();
            float maxQuality = sortedEvidence[0].reliability;
            float sumOfRemaining = 0f;

            for (int i = 1; i < sortedEvidence.Count; i++)
            {
                sumOfRemaining += sortedEvidence[i].reliability;
            }

            crime.evidenceStrength = UnityEngine.Mathf.Min(maxQuality + (sumOfRemaining * 0.3f), 1.0f);
        }

        /// <summary>
        /// Get the highest quality evidence for a case
        /// </summary>
        public Evidence GetBestEvidence(int caseId)
        {
            var evidence = GetEvidenceForCase(caseId);
            if (evidence == null || evidence.Count == 0)
                return null;

            return evidence.OrderByDescending(e => e.reliability).First();
        }

        /// <summary>
        /// Remove evidence (for cleanup, phase 6 false accusations, etc.)
        /// </summary>
        public void RemoveEvidence(Evidence evidence)
        {
            if (evidence == null)
                return;

            allEvidence.Remove(evidence);

            // Remove from crime lookup
            if (evidence.linkedCrime != null && evidenceByCrime.ContainsKey(evidence.linkedCrime))
            {
                evidenceByCrime[evidence.linkedCrime].Remove(evidence);
                UpdateCrimeEvidenceStrength(evidence.linkedCrime);
            }

            // Remove from case lookup
            if (evidence.linkedCrime != null && evidence.linkedCrime.caseId >= 0)
            {
                if (evidenceByCaseId.ContainsKey(evidence.linkedCrime.caseId))
                {
                    evidenceByCaseId[evidence.linkedCrime.caseId].Remove(evidence);
                }
            }
        }

        /// <summary>
        /// Periodic cleanup of old/invalid evidence references
        /// </summary>
        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Cleanup every 5000 ticks (roughly every 2 hours in-game)
            if (Find.TickManager.TicksGame % 5000 == 0)
            {
                CleanupInvalidEvidence();
            }
        }

        /// <summary>
        /// Remove evidence with null/invalid references
        /// </summary>
        private void CleanupInvalidEvidence()
        {
            if (allEvidence == null || allEvidence.Count == 0)
                return;

            var toRemove = allEvidence.Where(e =>
                e.collectedBy?.Destroyed == true ||
                e.witness?.Destroyed == true ||
                e.physicalItem?.Destroyed == true
            ).ToList();

            foreach (var evidence in toRemove)
            {
                RemoveEvidence(evidence);
            }

            if (toRemove.Count > 0)
            {
                Mod.Log?.Message($"Cleaned up {toRemove.Count} invalid evidence entries");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref allEvidence, "allEvidence", LookMode.Deep);

            // Rebuild lookups after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (allEvidence == null)
                {
                    allEvidence = new List<Evidence>();
                }

                RebuildLookups();
            }
        }

        /// <summary>
        /// Rebuild the evidence lookup dictionaries after loading from save
        /// </summary>
        private void RebuildLookups()
        {
            evidenceByCaseId = new Dictionary<int, List<Evidence>>();
            evidenceByCrime = new Dictionary<Crime, List<Evidence>>();

            foreach (var evidence in allEvidence)
            {
                if (evidence.linkedCrime != null)
                {
                    // Rebuild crime lookup
                    if (!evidenceByCrime.ContainsKey(evidence.linkedCrime))
                    {
                        evidenceByCrime[evidence.linkedCrime] = new List<Evidence>();
                    }
                    evidenceByCrime[evidence.linkedCrime].Add(evidence);

                    // Rebuild case lookup
                    if (evidence.linkedCrime.caseId >= 0)
                    {
                        if (!evidenceByCaseId.ContainsKey(evidence.linkedCrime.caseId))
                        {
                            evidenceByCaseId[evidence.linkedCrime.caseId] = new List<Evidence>();
                        }
                        evidenceByCaseId[evidence.linkedCrime.caseId].Add(evidence);
                    }
                }
            }

            Mod.Log?.Message($"EvidenceManager rebuilt: {allEvidence.Count} evidence entries, {evidenceByCaseId.Count} cases, {evidenceByCrime.Count} crimes");
        }
    }
}
