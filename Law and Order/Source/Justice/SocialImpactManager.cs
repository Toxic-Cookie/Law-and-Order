using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Justice
{
    /// <summary>
    /// Manages social relationship impacts from crimes, convictions, and false accusations
    /// Phase 6.7: Social Impact & Relationship Cascades
    /// </summary>
    public static class SocialImpactManager
    {
        // Thought def names (matching XML)
        private static ThoughtDef VictimOfCrime => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_VictimOfCrime");
        private static ThoughtDef WitnessedCrime => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_WitnessedCrime");
        private static ThoughtDef FriendVictimized => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_FriendVictimized");
        private static ThoughtDef FriendAccused => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_FriendAccused");
        private static ThoughtDef UnjustConviction => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_UnjustConviction");
        private static ThoughtDef InnocentlyAccused => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_InnocentlyAccused");
        private static ThoughtDef ExecutedInnocent => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_ExecutedInnocent");
        private static ThoughtDef FramedInnocent => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_FramedInnocent");
        private static ThoughtDef WitnessedFalseAccusation => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_WitnessedFalseAccusation");
        private static ThoughtDef JusticeRestored => DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_JusticeRestored");

        /// <summary>
        /// Apply social impact when a crime is witnessed
        /// Called when crime transitions to Suspected state
        /// </summary>
        public static void OnCrimeWitnessed(Crime crime, Pawn criminal, Map map)
        {
            if (crime == null || criminal == null)
                return;

            // 1. Victim hates criminal (if victim exists and is colonist)
            if (crime.victim != null && crime.victim.IsColonist && crime.victim != criminal)
            {
                TryAddThought(crime.victim, criminal, VictimOfCrime);
            }

            // 2. Witnesses hate criminal
            if (crime.witnesses != null)
            {
                foreach (var witness in crime.witnesses)
                {
                    if (witness != null && witness.IsColonist && witness != criminal)
                    {
                        TryAddThought(witness, criminal, WitnessedCrime);
                    }
                }
            }

            // 3. Friends of victim hate criminal
            if (crime.victim != null && crime.victim.IsColonist && crime.victim.relations != null)
            {
                var victimFriends = GetFriends(crime.victim, map);
                foreach (var friend in victimFriends)
                {
                    if (friend != criminal)
                    {
                        TryAddThought(friend, criminal, FriendVictimized);
                    }
                }
            }
        }

        /// <summary>
        /// Apply social impact when someone is accused (whether true or false)
        /// </summary>
        public static void OnPawnAccused(Pawn accused, List<Pawn> accusers, Map map)
        {
            if (accused == null || !accused.IsColonist)
                return;

            // Friends of accused dislike the accusers
            var accusedFriends = GetFriends(accused, map);
            foreach (var friend in accusedFriends)
            {
                foreach (var accuser in accusers ?? new List<Pawn>())
                {
                    if (accuser != null && accuser.IsColonist)
                    {
                        TryAddThought(friend, accuser, FriendAccused);
                    }
                }
            }
        }

        /// <summary>
        /// Apply social impact when someone is convicted
        /// </summary>
        public static void OnPawnConvicted(Pawn convicted, Crime crime, Map map, bool isJust = true)
        {
            if (convicted == null || !convicted.IsColonist)
                return;

            if (!isJust)
            {
                // Unjust conviction - colony-wide mood penalty
                ApplyColonyWideThought(map, UnjustConviction);
                Mod.Log?.Warning($"Unjust conviction applied to colony for {convicted.LabelShort}");
            }
        }

        /// <summary>
        /// Apply social impact when an innocent person is executed
        /// This is the most devastating social event
        /// </summary>
        public static void OnInnocentExecuted(Pawn innocent, Crime falseAccusation, Map map)
        {
            if (innocent == null)
                return;

            // Devastating colony-wide mood penalty
            ApplyColonyWideThought(map, ExecutedInnocent);

            // If we know who the real criminal is, apply intense hatred
            if (falseAccusation?.actualPerpetrator != null && falseAccusation.actualPerpetrator.IsColonist)
            {
                var allColonists = GetAllColonists(map);
                foreach (var colonist in allColonists)
                {
                    if (colonist != falseAccusation.actualPerpetrator)
                    {
                        TryAddThought(colonist, falseAccusation.actualPerpetrator, FramedInnocent);
                    }
                }
            }

            Mod.Log?.Warning($"Innocent executed social impact applied: {innocent.LabelShort}");
        }

        /// <summary>
        /// Apply social impact when truth is discovered and innocent is exonerated
        /// </summary>
        public static void OnTruthRevealed(Pawn innocent, Crime falseAccusation, Map map)
        {
            if (innocent == null || !innocent.IsColonist)
                return;

            // Colony sympathizes with the innocent person
            var allColonists = GetAllColonists(map);
            foreach (var colonist in allColonists)
            {
                if (colonist != innocent)
                {
                    TryAddThought(colonist, innocent, InnocentlyAccused);
                }
            }

            // Colony-wide thought about false accusation being revealed
            ApplyColonyWideThought(map, WitnessedFalseAccusation);

            // Positive thought - justice restored
            ApplyColonyWideThought(map, JusticeRestored);

            // If we know who the real criminal is, apply hatred toward them
            if (falseAccusation?.actualPerpetrator != null && falseAccusation.actualPerpetrator.IsColonist)
            {
                foreach (var colonist in allColonists)
                {
                    if (colonist != falseAccusation.actualPerpetrator)
                    {
                        TryAddThought(colonist, falseAccusation.actualPerpetrator, FramedInnocent);
                    }
                }
            }

            Mod.Log?.Message($"Truth revealed social impact applied for {innocent.LabelShort}");
        }

        /// <summary>
        /// Apply social impact when a false accusation is made
        /// </summary>
        public static void OnFalseAccusationMade(Pawn falselyAccused, Pawn accuser, Map map)
        {
            if (falselyAccused == null || !falselyAccused.IsColonist)
                return;

            // Friends of the accused defend them
            if (accuser != null && accuser.IsColonist)
            {
                var friends = GetFriends(falselyAccused, map);
                foreach (var friend in friends)
                {
                    TryAddThought(friend, accuser, FriendAccused);
                }
            }
        }

        /// <summary>
        /// Get all friends of a pawn (opinion >= 20)
        /// </summary>
        private static List<Pawn> GetFriends(Pawn pawn, Map map)
        {
            var friends = new List<Pawn>();

            if (pawn?.relations == null || map == null)
                return friends;

            var allColonists = GetAllColonists(map);
            foreach (var colonist in allColonists)
            {
                if (colonist != pawn && pawn.relations.OpinionOf(colonist) >= 20)
                {
                    friends.Add(colonist);
                }
            }

            return friends;
        }

        /// <summary>
        /// Get all colonists on the map
        /// </summary>
        private static List<Pawn> GetAllColonists(Map map)
        {
            if (map == null)
                return new List<Pawn>();

            return map.mapPawns.FreeColonistsSpawned.ToList();
        }

        /// <summary>
        /// Try to add a social thought between two pawns
        /// </summary>
        private static void TryAddThought(Pawn thinker, Pawn otherPawn, ThoughtDef thoughtDef)
        {
            if (thinker == null || otherPawn == null || thoughtDef == null)
                return;

            if (thinker.Dead || !thinker.IsColonist)
                return;

            if (thinker.needs?.mood?.thoughts?.memories == null)
                return;

            try
            {
                thinker.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, otherPawn);
            }
            catch (Exception ex)
            {
                Mod.Log?.Error($"Failed to add social thought {thoughtDef?.defName} between {thinker?.LabelShort} and {otherPawn?.LabelShort}: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply a thought to all colonists on the map
        /// </summary>
        private static void ApplyColonyWideThought(Map map, ThoughtDef thoughtDef)
        {
            if (map == null || thoughtDef == null)
                return;

            var colonists = GetAllColonists(map);
            foreach (var colonist in colonists)
            {
                if (colonist?.needs?.mood?.thoughts?.memories != null)
                {
                    try
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                    }
                    catch (Exception ex)
                    {
                        Mod.Log?.Error($"Failed to add colony-wide thought {thoughtDef?.defName} to {colonist?.LabelShort}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Calculate total social impact score for a crime
        /// Used to determine how much the colony cares about this crime
        /// </summary>
        public static int CalculateSocialImpactScore(Crime crime, Pawn criminal, Map map)
        {
            if (crime == null || criminal == null || map == null)
                return 0;

            int score = 0;

            // Victim impact (if colonist)
            if (crime.victim != null && crime.victim.IsColonist)
            {
                score += 20;

                // Higher impact if victim has friends
                var victimFriends = GetFriends(crime.victim, map);
                score += victimFriends.Count * 5;
            }

            // Witness impact
            if (crime.witnesses != null)
            {
                score += crime.witnesses.Count * 3;
            }

            // Criminal's social connections (if they have friends, it's more divisive)
            if (criminal.IsColonist)
            {
                var criminalFriends = GetFriends(criminal, map);
                score += criminalFriends.Count * 2;
            }

            return score;
        }

        /// <summary>
        /// Check if a conviction would be socially divisive
        /// Returns true if the convicted pawn has many friends who will be upset
        /// </summary>
        public static bool WouldConvictionBeDivisive(Pawn convicted, Map map)
        {
            if (convicted == null || !convicted.IsColonist || map == null)
                return false;

            var friends = GetFriends(convicted, map);
            return friends.Count >= 3; // 3+ friends = divisive
        }
    }
}
