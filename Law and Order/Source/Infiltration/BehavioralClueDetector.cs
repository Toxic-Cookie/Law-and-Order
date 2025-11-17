using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Detects suspicious behavioral patterns that reveal hidden identities
    /// Monitors pawns for signs like night activity, avoiding food, no socializing
    /// </summary>
    public class BehavioralClueDetector
    {
        // Track behavioral observations per pawn
        private class BehaviorTracking
        {
            public int daysObserved = 0;
            public int nightActivityCount = 0;
            public int normalFoodEatingCount = 0;
            public int socialInteractionCount = 0;
            public int relationshipsFormed = 0;
            public int lastCheckTick = 0;
        }

        private static Dictionary<Pawn, BehaviorTracking> trackingData = new Dictionary<Pawn, BehaviorTracking>();

        // Detection thresholds
        private const int MIN_OBSERVATION_DAYS = 5;
        private const int CLUE_CHECK_INTERVAL_TICKS = 60000; // 1 in-game day
        private const float NIGHT_ACTIVITY_THRESHOLD = 0.7f; // 70% of activity at night
        private const float NO_FOOD_THRESHOLD = 0.9f; // Never seen eating normal food
        private const float ANTISOCIAL_THRESHOLD = 0.1f; // Very few social interactions
        private const int NO_RELATIONSHIPS_DAYS = 30; // 30 days without forming bonds

        /// <summary>
        /// Check all pawns with hidden identities for behavioral clues
        /// Called periodically by WorldComponent
        /// </summary>
        public static void CheckBehavioralClues(Map map)
        {
            if (map == null)
                return;

            var pawnsToCheck = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.TryGetComp<CompHiddenIdentity>()?.HasHiddenIdentity ?? false)
                .ToList();

            foreach (var pawn in pawnsToCheck)
            {
                CheckPawnBehavior(pawn);
            }

            // Clean up tracking for despawned pawns
            var keysToRemove = trackingData.Keys.Where(p => p.Destroyed || !p.Spawned).ToList();
            foreach (var key in keysToRemove)
            {
                trackingData.Remove(key);
            }
        }

        /// <summary>
        /// Check a specific pawn's behavior for suspicious patterns
        /// </summary>
        private static void CheckPawnBehavior(Pawn pawn)
        {
            if (pawn == null)
                return;

            var comp = pawn.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // Get or create tracking data
            if (!trackingData.ContainsKey(pawn))
            {
                trackingData[pawn] = new BehaviorTracking();
            }

            var tracking = trackingData[pawn];

            // Only check once per day
            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - tracking.lastCheckTick < CLUE_CHECK_INTERVAL_TICKS)
                return;

            tracking.lastCheckTick = currentTick;
            tracking.daysObserved++;

            // Record current behavior
            RecordCurrentBehavior(pawn, tracking);

            // Need minimum observation period before checking for clues
            if (tracking.daysObserved < MIN_OBSERVATION_DAYS)
                return;

            // Check for suspicious patterns
            CheckNightActivityPattern(pawn, comp, tracking);
            CheckFoodAvoidancePattern(pawn, comp, tracking);
            CheckSocialAvoidancePattern(pawn, comp, tracking);
            CheckRelationshipPattern(pawn, comp, tracking);
        }

        /// <summary>
        /// Record the pawn's current behavior for pattern detection
        /// </summary>
        private static void RecordCurrentBehavior(Pawn pawn, BehaviorTracking tracking)
        {
            // Check if currently active at night
            if (pawn.Map != null)
            {
                bool isNight = GenLocalDate.HourInteger(pawn) >= 20 || GenLocalDate.HourInteger(pawn) <= 5;
                bool isAwake = pawn.Awake();

                if (isNight && isAwake && !pawn.CurJob.playerForced)
                {
                    tracking.nightActivityCount++;
                }
            }

            // Check social interactions (approximate by checking relationship count)
            // If pawn is forming relationships, they're likely socializing
            int currentRelationships = pawn.relations?.DirectRelations?.Count ?? 0;
            if (currentRelationships > tracking.relationshipsFormed)
            {
                // New relationship formed, so they must have socialized
                tracking.socialInteractionCount += 5; // Boost count since they formed a bond
            }
            else if (currentRelationships > 0)
            {
                // Has relationships, likely maintaining them
                tracking.socialInteractionCount++;
            }

            // Count relationships
            if (pawn.relations != null)
            {
                tracking.relationshipsFormed = pawn.relations.DirectRelations
                    .Count(r => r.otherPawn != null && !r.otherPawn.Dead);
            }

            // Check if eating normal food (hard to track directly, so we check needs)
            // Sanguophages don't need normal food
            if (pawn.needs?.food != null)
            {
                // If food need is increasing, they're eating
                // This is approximate - we can't easily detect exact eating events
                if (pawn.needs.food.CurLevelPercentage > 0.3f)
                {
                    tracking.normalFoodEatingCount++;
                }
            }
        }

        /// <summary>
        /// Check if pawn shows suspicious night activity pattern
        /// Discovery: 5-10% progress
        /// </summary>
        private static void CheckNightActivityPattern(Pawn pawn, CompHiddenIdentity comp, BehaviorTracking tracking)
        {
            if (tracking.daysObserved < 7)
                return;

            // Calculate night activity ratio
            float nightRatio = (float)tracking.nightActivityCount / tracking.daysObserved;

            if (nightRatio >= NIGHT_ACTIVITY_THRESHOLD)
            {
                // Suspicious! Always working at night
                if (!comp.Identity.discoveredClues.Contains("NightActivity"))
                {
                    float discoveryAmount = Rand.Range(5f, 10f);
                    comp.ProgressDiscovery(discoveryAmount, "NightActivity");

                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            $"CLUE: {pawn.LabelShort} is always active at night ({nightRatio:P0})",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Check if pawn avoids eating normal food (sanguophage indicator)
        /// Discovery: 10-15% progress
        /// </summary>
        private static void CheckFoodAvoidancePattern(Pawn pawn, CompHiddenIdentity comp, BehaviorTracking tracking)
        {
            if (tracking.daysObserved < 10)
                return;

            // Check if they have the Bloodfeeder gene (should be masked)
            if (ModsConfig.BiotechActive && pawn.genes?.HasActiveGene(GeneDefOf.Bloodfeeder) == true)
            {
                // Calculate food eating ratio
                float foodRatio = (float)tracking.normalFoodEatingCount / tracking.daysObserved;

                if (foodRatio <= NO_FOOD_THRESHOLD - 0.8f) // Very rarely eating normal food
                {
                    // Suspicious! Never seen eating
                    if (!comp.Identity.discoveredClues.Contains("NoFoodEating"))
                    {
                        float discoveryAmount = Rand.Range(10f, 15f);
                        comp.ProgressDiscovery(discoveryAmount, "NoFoodEating");

                        if (Prefs.DevMode)
                        {
                            Messages.Message(
                                $"CLUE: {pawn.LabelShort} never eats normal food",
                                MessageTypeDefOf.NeutralEvent
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if pawn avoids social interactions
        /// Discovery: 5-8% progress
        /// </summary>
        private static void CheckSocialAvoidancePattern(Pawn pawn, CompHiddenIdentity comp, BehaviorTracking tracking)
        {
            if (tracking.daysObserved < 10)
                return;

            // Calculate social interaction ratio
            float socialRatio = (float)tracking.socialInteractionCount / tracking.daysObserved;

            if (socialRatio <= ANTISOCIAL_THRESHOLD)
            {
                // Suspicious! Very antisocial
                if (!comp.Identity.discoveredClues.Contains("Antisocial"))
                {
                    float discoveryAmount = Rand.Range(5f, 8f);
                    comp.ProgressDiscovery(discoveryAmount, "Antisocial");

                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            $"CLUE: {pawn.LabelShort} avoids social interactions ({socialRatio:P0})",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Check if pawn has formed no relationships after extended time
        /// Discovery: 8-12% progress
        /// </summary>
        private static void CheckRelationshipPattern(Pawn pawn, CompHiddenIdentity comp, BehaviorTracking tracking)
        {
            if (tracking.daysObserved < NO_RELATIONSHIPS_DAYS)
                return;

            if (tracking.relationshipsFormed == 0)
            {
                // Suspicious! No bonds formed after 30+ days
                if (!comp.Identity.discoveredClues.Contains("NoRelationships"))
                {
                    float discoveryAmount = Rand.Range(8f, 12f);
                    comp.ProgressDiscovery(discoveryAmount, "NoRelationships");

                    if (Prefs.DevMode)
                    {
                        Messages.Message(
                            $"CLUE: {pawn.LabelShort} has formed no relationships after {tracking.daysObserved} days",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Manually add discovery progress from external events
        /// Used for interrogation, investigation, etc.
        /// </summary>
        public static void AddInvestigationClue(Pawn pawn, string clueType, float amount)
        {
            if (pawn == null)
                return;

            var comp = pawn.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity)
                return;

            comp.ProgressDiscovery(amount, clueType);

            if (Prefs.DevMode)
            {
                ModLog.Debug($"Investigation clue added for {pawn.LabelShort}: {clueType} (+{amount}%)");
            }
        }

        /// <summary>
        /// Reset tracking data (for testing or when identity revealed)
        /// </summary>
        public static void ResetTracking(Pawn pawn)
        {
            if (trackingData.ContainsKey(pawn))
            {
                trackingData.Remove(pawn);
            }
        }

        /// <summary>
        /// Clear all tracking data
        /// </summary>
        public static void ClearAllTracking()
        {
            trackingData.Clear();
        }
    }
}
