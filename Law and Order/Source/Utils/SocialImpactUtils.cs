using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for applying social impact (mood effects, opinions) when punishments are assigned.
    /// Phase 4: Social Impact System
    /// </summary>
    public static class SocialImpactUtils
    {
        /// <summary>
        /// Apply all social impact effects when a punishment is assigned.
        /// </summary>
        public static void ApplyPunishmentImpact(Punishment punishment)
        {
            if (punishment == null || punishment.criminal == null)
                return;

            // Get the crimes this punishment is for
            var justiceManager = Components.WorldComponent_JusticeManager.Instance;
            var criminalCase = justiceManager?.GetCaseById(punishment.caseId);

            if (criminalCase == null)
                return;

            var crimes = criminalCase.GetAssociatedCrimes()
                .Where(c => c.visibilityState == CrimeVisibilityState.Convicted)
                .ToList();

            // Apply impact based on punishment type
            switch (punishment.type)
            {
                case PunishmentType.Imprisonment:
                    ApplyImprisonmentImpact(punishment, crimes);
                    break;

                case PunishmentType.Beating:
                    ApplyBeatingImpact(punishment, crimes);
                    break;

                case PunishmentType.Execution:
                    ApplyExecutionImpact(punishment, crimes);
                    break;

                case PunishmentType.Fine:
                    ApplyFineImpact(punishment, crimes);
                    break;

                case PunishmentType.Exile:
                    ApplyExileImpact(punishment, crimes);
                    break;
            }

            // Apply victim satisfaction
            ApplyVictimSatisfaction(punishment, crimes);

            // Apply colony-wide mood (law and order)
            ApplyColonyMood(punishment);
        }

        private static void ApplyImprisonmentImpact(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Criminal gets negative thought
            if (punishment.criminal.needs?.mood != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_ImprisonedForCrime", false);
                if (thought != null)
                {
                    punishment.criminal.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }
        }

        private static void ApplyBeatingImpact(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Criminal gets severe negative thought
            if (punishment.criminal.needs?.mood != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_BeatenForCrime", false);
                if (thought != null)
                {
                    punishment.criminal.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }

            // Witnesses get negative thought (harsh punishment)
            ApplyWitnessThoughts(punishment.criminal, "LawAndOrder_WitnessedHarshPunishment");
        }

        private static void ApplyExecutionImpact(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Witnesses get execution thought
            ApplyWitnessThoughts(punishment.criminal, "LawAndOrder_WitnessedExecution");
        }

        private static void ApplyFineImpact(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Fine hediff already provides mood penalty
            // No additional thoughts needed
        }

        private static void ApplyExileImpact(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Criminal gets exile thought
            if (punishment.criminal.needs?.mood != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_Exiled", false);
                if (thought != null)
                {
                    punishment.criminal.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }
        }

        private static void ApplyVictimSatisfaction(Punishment punishment, System.Collections.Generic.List<Crime> crimes)
        {
            // Find all victims of the crimes
            var victims = crimes
                .Where(c => c.victim != null && !c.victim.Dead)
                .Select(c => c.victim)
                .Distinct()
                .ToList();

            foreach (var victim in victims)
            {
                if (victim.needs?.mood == null)
                    continue;

                // Determine severity of punishment
                bool isHarsh = punishment.type == PunishmentType.Execution ||
                               punishment.type == PunishmentType.Beating;

                ThoughtDef satisfactionThought;

                if (isHarsh)
                {
                    satisfactionThought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_JusticeServed_Victim_Major", false);
                }
                else
                {
                    satisfactionThought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_JusticeServed_Victim", false);
                }

                if (satisfactionThought != null)
                {
                    victim.needs.mood.thoughts.memories.TryGainMemory(satisfactionThought);
                }
            }
        }

        private static void ApplyWitnessThoughts(Pawn criminal, string thoughtDefName)
        {
            if (criminal?.Map == null)
                return;

            // Find colonists near the criminal who can see
            var witnesses = criminal.Map.mapPawns.FreeColonistsSpawned
                .Where(p => p != criminal &&
                            p.Position.DistanceTo(criminal.Position) <= 15f &&
                            p.needs?.mood != null)
                .ToList();

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed(thoughtDefName, false);
            if (thought == null)
                return;

            foreach (var witness in witnesses)
            {
                witness.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }

        private static void ApplyColonyMood(Punishment punishment)
        {
            if (punishment.criminal?.Map == null)
                return;

            // Apply positive "law and order" thought to all colonists
            var colonists = punishment.criminal.Map.mapPawns.FreeColonistsSpawned
                .Where(p => p != punishment.criminal && p.needs?.mood != null);

            ThoughtDef lawAndOrder = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_LawAndOrder", false);
            if (lawAndOrder == null)
                return;

            foreach (var colonist in colonists)
            {
                // Small chance to get the thought (don't spam)
                if (Rand.Chance(0.3f))
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(lawAndOrder);
                }
            }
        }

        /// <summary>
        /// Apply social opinion modifiers based on relationships.
        /// Friends of the criminal will be upset if punishment is harsh.
        /// Enemies will be pleased.
        /// </summary>
        public static void ApplyRelationshipImpact(Punishment punishment)
        {
            if (punishment.criminal?.Map == null || punishment.criminal.relations == null)
                return;

            bool isHarsh = punishment.type == PunishmentType.Execution ||
                           punishment.type == PunishmentType.Beating ||
                           (punishment.type == PunishmentType.Imprisonment && punishment.durationDays > 30);

            var colonists = punishment.criminal.Map.mapPawns.FreeColonistsSpawned
                .Where(p => p != punishment.criminal && p.needs?.mood != null);

            foreach (var colonist in colonists)
            {
                if (colonist.relations == null)
                    continue;

                int opinion = colonist.relations.OpinionOf(punishment.criminal);

                // Friends (positive opinion) upset by harsh punishment
                if (opinion >= 20 && isHarsh)
                {
                    ThoughtDef friendThought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_FriendPunishedHarshly", false);
                    if (friendThought != null)
                    {
                        Thought_MemorySocial memory = (Thought_MemorySocial)ThoughtMaker.MakeThought(friendThought);
                        memory.opinionOffset = -10;
                        colonist.needs.mood.thoughts.memories.TryGainMemory(memory, punishment.criminal);
                    }
                }
                // Enemies (negative opinion) pleased by punishment
                else if (opinion <= -20)
                {
                    ThoughtDef enemyThought = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_EnemyPunished", false);
                    if (enemyThought != null)
                    {
                        Thought_MemorySocial memory = (Thought_MemorySocial)ThoughtMaker.MakeThought(enemyThought);
                        memory.opinionOffset = 5;
                        colonist.needs.mood.thoughts.memories.TryGainMemory(memory, punishment.criminal);
                    }
                }
            }
        }

        /// <summary>
        /// Apply negative thoughts when a criminal is not punished (case dismissed).
        /// </summary>
        public static void ApplyUnpunishedImpact(CriminalCase dismissedCase)
        {
            if (dismissedCase?.accused == null)
                return;

            // Get victims who won't get justice
            var crimes = dismissedCase.GetAssociatedCrimes();
            var victims = crimes
                .Where(c => c.victim != null && !c.victim.Dead)
                .Select(c => c.victim)
                .Distinct();

            ThoughtDef noJustice = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_NoJustice_Victim", false);
            if (noJustice != null)
            {
                foreach (var victim in victims)
                {
                    if (victim.needs?.mood != null)
                    {
                        victim.needs.mood.thoughts.memories.TryGainMemory(noJustice);
                    }
                }
            }

            // Colony-wide thought about unpunished criminal
            if (dismissedCase.accused.Map != null)
            {
                var colonists = dismissedCase.accused.Map.mapPawns.FreeColonistsSpawned
                    .Where(p => p.needs?.mood != null);

                ThoughtDef unpunished = DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_CriminalUnpunished", false);
                if (unpunished != null)
                {
                    foreach (var colonist in colonists)
                    {
                        if (Rand.Chance(0.4f)) // 40% chance
                        {
                            colonist.needs.mood.thoughts.memories.TryGainMemory(unpunished);
                        }
                    }
                }
            }
        }
    }
}
