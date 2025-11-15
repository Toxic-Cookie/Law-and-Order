using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Types of evidence that can be collected
    /// </summary>
    public enum EvidenceType
    {
        /// <summary>
        /// Physical evidence: blood, items, damage traces
        /// </summary>
        Physical,

        /// <summary>
        /// Circumstantial evidence: presence at scene, motive, opportunity
        /// </summary>
        Circumstantial,

        /// <summary>
        /// Testimonial evidence: witness statements, confessions
        /// </summary>
        Testimonial
    }

    /// <summary>
    /// Represents a piece of evidence linked to a crime
    /// </summary>
    public class Evidence : IExposable
    {
        // Core evidence data
        public EvidenceType evidenceType;
        public string description;
        public float reliability; // 0.0-1.0 how reliable this evidence is
        public int tickCollected;
        public Pawn collectedBy; // Investigator who found/recorded this
        public IntVec3 location; // Where evidence was found

        // Link to crime
        public Crime linkedCrime; // Crime this evidence supports

        // Type-specific data
        public Thing physicalItem; // For Physical evidence
        public Pawn witness; // For Testimonial evidence
        public string additionalInfo; // Any extra context

        public Evidence()
        {
        }

        public Evidence(
            EvidenceType type,
            string description,
            float reliability,
            Pawn collectedBy,
            IntVec3 location,
            Crime linkedCrime = null)
        {
            this.evidenceType = type;
            this.description = description;
            this.reliability = Mathf.Clamp01(reliability);
            this.tickCollected = Find.TickManager.TicksGame;
            this.collectedBy = collectedBy;
            this.location = location;
            this.linkedCrime = linkedCrime;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref evidenceType, "evidenceType", EvidenceType.Physical);
            Scribe_Values.Look(ref description, "description");
            Scribe_Values.Look(ref reliability, "reliability", 0f);
            Scribe_Values.Look(ref tickCollected, "tickCollected", 0);
            Scribe_References.Look(ref collectedBy, "collectedBy");
            Scribe_Values.Look(ref location, "location");
            Scribe_Deep.Look(ref linkedCrime, "linkedCrime");
            Scribe_References.Look(ref physicalItem, "physicalItem");
            Scribe_References.Look(ref witness, "witness");
            Scribe_Values.Look(ref additionalInfo, "additionalInfo");
        }

        public int DaysAgo => (Find.TickManager.TicksGame - tickCollected) / GenDate.TicksPerDay;

        /// <summary>
        /// Get a translated label for this evidence type
        /// </summary>
        public string GetTypeLabel()
        {
            switch (evidenceType)
            {
                case EvidenceType.Physical:
                    return "LawAndOrder_Evidence_Physical".Translate();
                case EvidenceType.Circumstantial:
                    return "LawAndOrder_Evidence_Circumstantial".Translate();
                case EvidenceType.Testimonial:
                    return "LawAndOrder_Evidence_Testimonial".Translate();
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Get a detailed description of this evidence
        /// </summary>
        public string GetDetailedDescription()
        {
            string detail = description;

            if (collectedBy != null)
            {
                detail += $"\nCollected by: {collectedBy.LabelShort}";
            }

            detail += $"\nReliability: {reliability:P0}";
            detail += $"\nDays ago: {DaysAgo}";

            if (witness != null)
            {
                detail += $"\nWitness: {witness.LabelShort}";
            }

            if (physicalItem != null)
            {
                detail += $"\nItem: {physicalItem.Label}";
            }

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                detail += $"\n{additionalInfo}";
            }

            return detail;
        }
    }

    /// <summary>
    /// Static utility class for evidence collection and management
    /// </summary>
    public static class EvidenceUtils
    {
        /// <summary>
        /// Create physical evidence from a crime scene
        /// </summary>
        public static Evidence CreatePhysicalEvidence(
            Crime crime,
            Pawn investigator,
            IntVec3 location,
            Thing item = null)
        {
            string description = GeneratePhysicalEvidenceDescription(crime, item);

            Evidence evidence = new Evidence(
                EvidenceType.Physical,
                description,
                CalculatePhysicalEvidenceReliability(crime, item),
                investigator,
                location,
                crime
            )
            {
                physicalItem = item
            };

            return evidence;
        }

        /// <summary>
        /// Create testimonial evidence from a witness statement
        /// </summary>
        public static Evidence CreateTestimonialEvidence(
            Crime crime,
            Pawn witness,
            Pawn investigator)
        {
            string description = $"{witness.LabelShort} witnessed {crime.GetCrimeLabel()}";

            Evidence evidence = new Evidence(
                EvidenceType.Testimonial,
                description,
                CalculateTestimonialEvidenceReliability(witness, crime),
                investigator,
                witness.Position,
                crime
            )
            {
                witness = witness
            };

            return evidence;
        }

        /// <summary>
        /// Create circumstantial evidence
        /// </summary>
        public static Evidence CreateCircumstantialEvidence(
            Crime crime,
            Pawn suspect,
            Pawn investigator,
            string reason)
        {
            string description = $"{suspect.LabelShort}: {reason}";

            Evidence evidence = new Evidence(
                EvidenceType.Circumstantial,
                description,
                0.4f, // Circumstantial evidence is less reliable
                investigator,
                suspect.Position,
                crime
            );

            return evidence;
        }

        /// <summary>
        /// Generate a description for physical evidence
        /// </summary>
        private static string GeneratePhysicalEvidenceDescription(Crime crime, Thing item)
        {
            if (item != null)
            {
                return $"Physical evidence: {item.Label} related to {crime.GetCrimeLabel()}";
            }

            switch (crime.crimeType)
            {
                case CrimeType.Assault:
                case CrimeType.Murder:
                    return "Blood traces at crime scene";
                case CrimeType.Theft:
                    return "Stolen goods recovered";
                case CrimeType.Arson:
                    return "Accelerant traces found";
                case CrimeType.PropertyDestruction:
                case CrimeType.Vandalism:
                    return "Damage patterns analyzed";
                default:
                    return "Physical evidence recovered";
            }
        }

        /// <summary>
        /// Calculate reliability of physical evidence
        /// </summary>
        private static float CalculatePhysicalEvidenceReliability(Crime crime, Thing item)
        {
            float baseReliability = 0.7f; // Physical evidence is generally reliable

            // Fresh evidence is more reliable
            int daysOld = crime.DaysAgo;
            if (daysOld > 5)
            {
                baseReliability *= 0.8f; // 20% penalty for old evidence
            }

            // Specific item evidence is more reliable
            if (item != null)
            {
                baseReliability += 0.1f;
            }

            return Mathf.Clamp01(baseReliability);
        }

        /// <summary>
        /// Calculate reliability of testimonial evidence based on witness
        /// </summary>
        private static float CalculateTestimonialEvidenceReliability(Pawn witness, Crime crime)
        {
            float baseReliability = 0.6f;

            // Conscious witnesses are more reliable
            if (witness.health != null)
            {
                float consciousness = witness.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                baseReliability *= consciousness;
            }

            // Fresh testimony is more reliable
            int daysOld = crime.DaysAgo;
            if (daysOld > 3)
            {
                baseReliability *= 0.9f;
            }

            // Social skill of witness affects reliability (can they articulate well?)
            if (witness.skills != null)
            {
                int socialSkill = witness.skills.GetSkill(SkillDefOf.Social).Level;
                baseReliability += (socialSkill / 100f); // Up to +20% for max skill
            }

            return Mathf.Clamp01(baseReliability);
        }

        /// <summary>
        /// Link evidence to a crime and update crime's evidence strength
        /// </summary>
        public static void LinkEvidenceToCrime(Evidence evidence, Crime crime)
        {
            evidence.linkedCrime = crime;

            // Update crime's evidence strength based on new evidence
            // Multiple pieces of evidence increase overall strength
            float currentStrength = crime.evidenceStrength;
            float evidenceContribution = evidence.reliability * 0.3f; // Each piece adds up to 30%

            crime.evidenceStrength = Mathf.Min(currentStrength + evidenceContribution, 1.0f);
        }

        /// <summary>
        /// Get all evidence linked to a specific crime
        /// </summary>
        public static List<Evidence> GetEvidenceForCrime(Crime crime, Map map)
        {
            // For now, evidence is stored in the Crime's additional info
            // In a full implementation, we'd have a WorldComponent to track all evidence

            List<Evidence> evidenceList = new List<Evidence>();

            // Generate evidence from crime's witnesses (testimonial)
            if (crime.witnesses != null && crime.witnesses.Count > 0)
            {
                foreach (Pawn witness in crime.witnesses)
                {
                    if (witness != null && !witness.Dead)
                    {
                        Evidence testimonial = CreateTestimonialEvidence(
                            crime,
                            witness,
                            witness // Self-reported for now
                        );
                        evidenceList.Add(testimonial);
                    }
                }
            }

            return evidenceList;
        }
    }
}
