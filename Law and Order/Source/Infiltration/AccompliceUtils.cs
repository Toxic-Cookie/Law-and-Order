using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Utility class for accomplice recruitment and management
    /// </summary>
    public static class AccompliceUtils
    {
        /// <summary>
        /// Calculate how vulnerable a colonist is to being recruited as an accomplice
        /// Returns 0.0-1.0 chance of recruitment success
        /// </summary>
        public static float CalculateVulnerability(Pawn colonist, Pawn infiltrator)
        {
            if (colonist == null || colonist.Dead || !colonist.IsColonist)
                return 0f;

            // Already an accomplice
            var existingComp = colonist.TryGetComp<CompAccomplice>();
            if (existingComp != null && existingComp.IsRecruited)
                return 0f;

            float vulnerability = 0f;

            // 1. MOOD - Low mood makes pawns vulnerable
            if (colonist.needs?.mood != null)
            {
                float moodLevel = colonist.needs.mood.CurLevelPercentage;

                if (moodLevel < 0.3f)
                {
                    vulnerability += 0.20f; // +20% if very unhappy
                }
                else if (moodLevel < 0.5f)
                {
                    vulnerability += 0.10f; // +10% if somewhat unhappy
                }
            }

            // 2. TRAITS - Certain traits make pawns more susceptible
            if (colonist.story?.traits != null)
            {
                // Greedy trait
                var greedyTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Greedy");
                if (greedyTrait != null && colonist.story.traits.HasTrait(greedyTrait))
                {
                    vulnerability += 0.15f; // +15% for greedy
                }

                // Jealous trait
                var jealousTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Jealous");
                if (jealousTrait != null && colonist.story.traits.HasTrait(jealousTrait))
                {
                    vulnerability += 0.12f; // +12% for jealous
                }

                // Depressive trait (from vanilla NaturalMood trait)
                var naturalMoodTrait = DefDatabase<TraitDef>.GetNamedSilentFail("NaturalMood");
                if (naturalMoodTrait != null && colonist.story.traits.HasTrait(naturalMoodTrait) &&
                    colonist.story.traits.DegreeOfTrait(naturalMoodTrait) == -2)
                {
                    vulnerability += 0.10f; // +10% for depressive
                }

                // Psychopath (can be swayed if mood is low)
                if (colonist.story.traits.HasTrait(TraitDefOf.Psychopath))
                {
                    vulnerability += 0.08f; // +8% for psychopath
                }

                // Abrasive (dislikes others, easier to turn)
                var abrasiveTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Abrasive");
                if (abrasiveTrait != null && colonist.story.traits.HasTrait(abrasiveTrait))
                {
                    vulnerability += 0.10f; // +10% for abrasive
                }
            }

            // 3. BACKSTORY - Criminal past or rebellious background
            if (colonist.story != null)
            {
                string childhoodId = colonist.story.Childhood?.identifier?.ToLower() ?? "";
                string adulthoodId = colonist.story.Adulthood?.identifier?.ToLower() ?? "";

                // Check for criminal, rebel, or outcast backgrounds
                if (childhoodId.Contains("criminal") || adulthoodId.Contains("criminal") ||
                    childhoodId.Contains("rebel") || adulthoodId.Contains("rebel") ||
                    childhoodId.Contains("outcast") || adulthoodId.Contains("outcast") ||
                    childhoodId.Contains("pirate") || adulthoodId.Contains("pirate"))
                {
                    vulnerability += 0.10f; // +10% for criminal/rebel past
                }
            }

            // 4. RELATIONSHIP - Friend of infiltrator
            if (infiltrator != null && colonist.relations != null)
            {
                float opinion = colonist.relations.OpinionOf(infiltrator);

                if (opinion >= 20)
                {
                    vulnerability += 0.15f; // +15% if they like the infiltrator
                }
                else if (opinion >= 0)
                {
                    vulnerability += 0.05f; // +5% if neutral/slightly positive
                }
            }

            // 5. SOCIAL ISOLATION - No friends/lovers makes them vulnerable
            if (colonist.relations != null)
            {
                int friendCount = colonist.relations.DirectRelations
                    .Count(r => r.def == PawnRelationDefOf.Lover || r.def == PawnRelationDefOf.Fiance);

                if (friendCount == 0)
                {
                    vulnerability += 0.08f; // +8% if no close relationships
                }
            }

            // Clamp to 0-1 range
            return UnityEngine.Mathf.Clamp01(vulnerability);
        }

        /// <summary>
        /// Attempt to recruit a colonist as an accomplice
        /// </summary>
        public static bool TryRecruitAccomplice(Pawn infiltrator, Pawn target, out CompAccomplice accompliceComp)
        {
            accompliceComp = null;

            if (infiltrator == null || target == null || target.Dead)
                return false;

            // Check if infiltrator is actually an infiltrator
            var infiltratorComp = infiltrator.TryGetComp<CompHiddenIdentity>();
            if (infiltratorComp == null || !infiltratorComp.HasHiddenIdentity)
            {
                ModLog.Warning($"TryRecruitAccomplice: {infiltrator.LabelShort} is not an infiltrator!");
                return false;
            }

            // Check max accomplices setting
            int maxAccomplices = LawAndOrderMod.Settings.maxAccomplicesPerInfiltrator;
            int currentAccomplices = CountAccomplicesForInfiltrator(infiltrator);

            if (currentAccomplices >= maxAccomplices)
            {
                ModLog.Debug($"Infiltrator {infiltrator.LabelShort} already has max accomplices ({maxAccomplices})");
                return false;
            }

            // Calculate vulnerability
            float vulnerability = CalculateVulnerability(target, infiltrator);

            if (vulnerability < 0.1f)
            {
                // Not vulnerable enough
                return false;
            }

            // Roll for recruitment success
            if (!Rand.Chance(vulnerability))
            {
                ModLog.Debug($"Failed to recruit {target.LabelShort} (vulnerability: {vulnerability:P0})");
                return false;
            }

            // SUCCESS! Recruit as accomplice
            accompliceComp = target.TryGetComp<CompAccomplice>();
            if (accompliceComp == null)
            {
                // Add the comp dynamically if it doesn't exist
                accompliceComp = new CompAccomplice();
                accompliceComp.parent = target;
                target.AllComps.Add(accompliceComp);
                accompliceComp.Initialize(new CompProperties_Accomplice());
            }

            // Initial loyalty based on vulnerability
            float initialLoyalty = vulnerability;
            accompliceComp.RecruitAsAccomplice(infiltrator, initialLoyalty);

            ModLog.Info($"Successfully recruited {target.LabelShort} as accomplice for {infiltrator.LabelShort} (vulnerability: {vulnerability:P0})");

            return true;
        }

        /// <summary>
        /// Count how many accomplices an infiltrator currently has
        /// </summary>
        public static int CountAccomplicesForInfiltrator(Pawn infiltrator)
        {
            if (infiltrator?.Map == null)
                return 0;

            int count = 0;

            foreach (var pawn in infiltrator.Map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited && comp.Recruiter == infiltrator)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Get all accomplices for a specific infiltrator
        /// </summary>
        public static List<Pawn> GetAccomplicesForInfiltrator(Pawn infiltrator)
        {
            var accomplices = new List<Pawn>();

            if (infiltrator?.Map == null)
                return accomplices;

            foreach (var pawn in infiltrator.Map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited && comp.Recruiter == infiltrator)
                {
                    accomplices.Add(pawn);
                }
            }

            return accomplices;
        }

        /// <summary>
        /// Apply betrayal thoughts when an accomplice is discovered
        /// </summary>
        public static void ApplyBetrayalThoughts(Pawn accomplice, Pawn recruiter)
        {
            if (accomplice == null || accomplice.Map == null)
                return;

            // Apply thought to the accomplice
            if (accomplice.needs?.mood != null)
            {
                // Caught accomplice feels guilty/scared
                var caughtThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_CaughtAsAccomplice");
                if (caughtThought != null)
                {
                    accomplice.needs.mood.thoughts.memories.TryGainMemory(caughtThought);
                }
            }

            // All other colonists are shocked and lose trust
            foreach (var colonist in accomplice.Map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist == accomplice || colonist.Dead)
                    continue;

                // Apply betrayal thought
                if (colonist.needs?.mood != null)
                {
                    var betrayalThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_ColonistBetrayed");
                    if (betrayalThought != null)
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(betrayalThought);
                    }
                }

                // Opinion penalty toward the accomplice
                if (colonist.relations != null)
                {
                    var opinionThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_OpinionBetrayer");
                    if (opinionThought != null)
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(opinionThought, accomplice);
                    }
                }
            }

            // If recruiter is known, apply thoughts about them too
            if (recruiter != null && !recruiter.Dead)
            {
                foreach (var colonist in accomplice.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist == recruiter || colonist.Dead)
                        continue;

                    if (colonist.relations != null)
                    {
                        var recruiterOpinionThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_OpinionInfiltrator");
                        if (recruiterOpinionThought != null)
                        {
                            colonist.needs.mood.thoughts.memories.TryGainMemory(recruiterOpinionThought, recruiter);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Periodic check to attempt accomplice recruitment for all infiltrators
        /// Should be called from WorldComponent
        /// </summary>
        public static void TryRecruitAccomplices_Periodic(Map map)
        {
            if (map == null)
                return;

            // Find all infiltrators on the map
            var infiltrators = new List<Pawn>();
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompHiddenIdentity>();
                if (comp != null && comp.HasHiddenIdentity && !comp.Hediff.identityRevealed)
                {
                    infiltrators.Add(pawn);
                }
            }

            if (infiltrators.Count == 0)
                return;

            // Each infiltrator attempts to recruit one vulnerable colonist
            foreach (var infiltrator in infiltrators)
            {
                // Check if they've hit max accomplices
                int maxAccomplices = LawAndOrderMod.Settings.maxAccomplicesPerInfiltrator;
                int currentAccomplices = CountAccomplicesForInfiltrator(infiltrator);

                if (currentAccomplices >= maxAccomplices)
                    continue; // Already at max

                // Find vulnerable targets
                var vulnerableTargets = map.mapPawns.FreeColonistsSpawned
                    .Where(p => p != infiltrator && !p.Dead)
                    .Select(p => new { Pawn = p, Vulnerability = CalculateVulnerability(p, infiltrator) })
                    .Where(t => t.Vulnerability >= 0.15f) // Must be at least 15% vulnerable
                    .OrderByDescending(t => t.Vulnerability)
                    .ToList();

                if (vulnerableTargets.Count == 0)
                    continue;

                // Try to recruit the most vulnerable target
                var target = vulnerableTargets.First();
                TryRecruitAccomplice(infiltrator, target.Pawn, out _);
            }
        }

        /// <summary>
        /// Update loyalty for all accomplices on a map
        /// Should be called periodically from WorldComponent
        /// </summary>
        public static void UpdateAllAccompliceLoyalty(Map map)
        {
            if (map == null)
                return;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited)
                {
                    comp.UpdateLoyalty();
                }
            }
        }
    }
}
