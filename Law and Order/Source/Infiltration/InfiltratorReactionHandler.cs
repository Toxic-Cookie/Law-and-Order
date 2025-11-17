using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Handles infiltrator reactions when their identity is revealed
    /// Reactions vary by intelligence stat
    /// </summary>
    public static class InfiltratorReactionHandler
    {
        /// <summary>
        /// Execute the infiltrator's reaction to being exposed
        /// </summary>
        public static void ExecuteReaction(Pawn infiltrator, InfiltratorReaction reaction)
        {
            if (infiltrator == null || infiltrator.Dead)
                return;

            ModLog.Info($"Infiltrator {infiltrator.LabelShort} executing reaction: {reaction}");

            switch (reaction)
            {
                case InfiltratorReaction.FleeMap:
                    ReactFleeMap(infiltrator);
                    break;

                case InfiltratorReaction.FightToTheEnd:
                    ReactFightToEnd(infiltrator);
                    break;

                case InfiltratorReaction.FakeInnocence:
                    ReactFakeInnocence(infiltrator);
                    break;

                case InfiltratorReaction.AttemptBribery:
                    ReactAttemptBribery(infiltrator);
                    break;

                case InfiltratorReaction.ActivateAccomplices:
                    ReactActivateAccomplices(infiltrator);
                    break;

                case InfiltratorReaction.TransmitIntelNow:
                    ReactTransmitIntel(infiltrator);
                    break;

                default:
                    ModLog.Warning($"Unknown infiltrator reaction: {reaction}");
                    break;
            }
        }

        /// <summary>
        /// Reaction: Try to flee the map immediately
        /// </summary>
        private static void ReactFleeMap(Pawn infiltrator)
        {
            if (infiltrator?.Map == null)
                return;

            // Make them hostile
            if (infiltrator.Faction == Faction.OfPlayer)
            {
                infiltrator.SetFaction(null);
            }

            // Force them to flee off the map
            IntVec3 exitSpot;
            if (RCellFinder.TryFindBestExitSpot(infiltrator, out exitSpot))
            {
                Job fleeJob = JobMaker.MakeJob(JobDefOf.Goto, exitSpot);
                fleeJob.exitMapOnArrival = true;
                fleeJob.locomotionUrgency = LocomotionUrgency.Sprint;

                infiltrator.jobs.StartJob(fleeJob, JobCondition.InterruptForced);

                Messages.Message(
                    "LawAndOrder_InfiltratorFleeing".Translate(infiltrator.LabelShort),
                    infiltrator,
                    MessageTypeDefOf.ThreatBig
                );

                ModLog.Debug($"{infiltrator.LabelShort} is fleeing to {exitSpot}");
            }
            else
            {
                ModLog.Warning($"Could not find exit spot for fleeing infiltrator {infiltrator.LabelShort}");
                // Fallback to fighting
                ReactFightToEnd(infiltrator);
            }
        }

        /// <summary>
        /// Reaction: Go berserk and fight to the death
        /// </summary>
        private static void ReactFightToEnd(Pawn infiltrator)
        {
            if (infiltrator == null)
                return;

            // Make them hostile
            if (infiltrator.Faction == Faction.OfPlayer)
            {
                infiltrator.SetFaction(null);
            }

            // Make them go berserk
            if (infiltrator.mindState != null)
            {
                infiltrator.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Berserk,
                    reason: "Exposed as infiltrator",
                    forceWake: true
                );

                Messages.Message(
                    "LawAndOrder_InfiltratorBerserk".Translate(infiltrator.LabelShort),
                    infiltrator,
                    MessageTypeDefOf.ThreatBig
                );

                ModLog.Debug($"{infiltrator.LabelShort} has gone berserk");
            }
        }

        /// <summary>
        /// Reaction: Claim it's all a misunderstanding
        /// High social skill can convince some colonists
        /// </summary>
        private static void ReactFakeInnocence(Pawn infiltrator)
        {
            if (infiltrator?.Map == null)
                return;

            // Try to convince colonists it's a mistake
            int socialSkill = infiltrator.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float convinceChance = 0.1f + (socialSkill * 0.02f); // 10% base + 2% per social level

            bool convincedAnyone = false;

            var colonists = infiltrator.Map.mapPawns.FreeColonistsSpawned
                .Where(p => p != infiltrator && !p.Dead)
                .ToList();

            foreach (var colonist in colonists.InRandomOrder().Take(3)) // Try to convince up to 3 colonists
            {
                if (Rand.Chance(convinceChance))
                {
                    // Successfully convinced this colonist
                    // Add positive opinion (they believe the infiltrator)
                    if (colonist.relations != null && infiltrator != null)
                    {
                        colonist.relations.AddDirectRelation(PawnRelationDefOf.Bond, infiltrator);
                    }

                    convincedAnyone = true;

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"{infiltrator.LabelShort} convinced {colonist.LabelShort} of innocence");
                    }
                }
            }

            if (convincedAnyone)
            {
                Messages.Message(
                    "LawAndOrder_InfiltratorFakedInnocence".Translate(infiltrator.LabelShort),
                    infiltrator,
                    MessageTypeDefOf.NeutralEvent
                );
            }
            else
            {
                // Failed to convince anyone, try something else
                Messages.Message(
                    "LawAndOrder_InfiltratorFakedInnocenceFailed".Translate(infiltrator.LabelShort),
                    infiltrator,
                    MessageTypeDefOf.NeutralEvent
                );

                // Fallback to fleeing
                ReactFleeMap(infiltrator);
            }
        }

        /// <summary>
        /// Reaction: Offer information in exchange for freedom
        /// Reveals intelligence gathered (Phase 6.5 stub)
        /// </summary>
        private static void ReactAttemptBribery(Pawn infiltrator)
        {
            if (infiltrator == null)
                return;

            // For now, just offer some silver or reveal a secret
            // In Phase 6.5, this would reveal actual intelligence data

            var comp = infiltrator.TryGetComp<CompHiddenIdentity>();
            string intel = "information about an upcoming raid"; // Placeholder

            // Send letter offering the trade
            Find.LetterStack.ReceiveLetter(
                "LawAndOrder_LetterBriberyAttemptTitle".Translate(),
                "LawAndOrder_LetterBriberyAttemptDesc".Translate(
                    infiltrator.LabelShort,
                    intel
                ),
                LetterDefOf.NeutralEvent,
                infiltrator
            );

            ModLog.Debug($"{infiltrator.LabelShort} is attempting bribery with intel: {intel}");

            // TODO Phase 6.5: Actually reveal gathered intelligence
        }

        /// <summary>
        /// Reaction: Activate recruited accomplices immediately
        /// Triggers sabotage (Phase 6.6 stub)
        /// </summary>
        private static void ReactActivateAccomplices(Pawn infiltrator)
        {
            if (infiltrator == null)
                return;

            // Placeholder for Phase 6.6 accomplice system
            Messages.Message(
                "LawAndOrder_InfiltratorActivatedAccomplices".Translate(infiltrator.LabelShort),
                infiltrator,
                MessageTypeDefOf.ThreatBig
            );

            ModLog.Debug($"{infiltrator.LabelShort} is attempting to activate accomplices");

            // TODO Phase 6.6: Trigger accomplice sabotage actions

            // After attempting activation, flee
            ReactFleeMap(infiltrator);
        }

        /// <summary>
        /// Reaction: Emergency intelligence transmission
        /// Sends gathered intel to faction immediately (Phase 6.5 stub)
        /// </summary>
        private static void ReactTransmitIntel(Pawn infiltrator)
        {
            if (infiltrator == null)
                return;

            // Placeholder for Phase 6.5 intelligence system
            Messages.Message(
                "LawAndOrder_InfiltratorTransmittingIntel".Translate(infiltrator.LabelShort),
                infiltrator,
                MessageTypeDefOf.ThreatBig
            );

            ModLog.Debug($"{infiltrator.LabelShort} is transmitting intelligence");

            // TODO Phase 6.5: Actually transmit gathered intelligence to hostile faction

            // After transmission, try to flee
            ReactFleeMap(infiltrator);
        }

        /// <summary>
        /// Choose reaction based on infiltrator's intelligence stat
        /// Smarter infiltrators have more options
        /// </summary>
        public static InfiltratorReaction ChooseReaction(Pawn infiltrator, HiddenIdentity identity)
        {
            if (identity == null)
                return InfiltratorReaction.FleeMap;

            // Build list of available reactions based on intelligence
            var availableReactions = new List<InfiltratorReaction>
            {
                InfiltratorReaction.FleeMap,        // Always available
                InfiltratorReaction.FightToTheEnd   // Always available
            };

            if (identity.intelligenceStat > 0.6f)
            {
                availableReactions.Add(InfiltratorReaction.FakeInnocence);
                availableReactions.Add(InfiltratorReaction.AttemptBribery);
            }

            if (identity.intelligenceStat > 0.8f)
            {
                availableReactions.Add(InfiltratorReaction.ActivateAccomplices);
                availableReactions.Add(InfiltratorReaction.TransmitIntelNow);
            }

            // Weight reactions by intelligence
            // Smarter infiltrators prefer strategic options
            var weights = new Dictionary<InfiltratorReaction, float>();

            foreach (var reaction in availableReactions)
            {
                float weight = 1.0f;

                switch (reaction)
                {
                    case InfiltratorReaction.FleeMap:
                        weight = 2.0f; // Always a solid option
                        break;

                    case InfiltratorReaction.FightToTheEnd:
                        weight = 1.0f - identity.intelligenceStat; // Dumber = more likely to fight
                        break;

                    case InfiltratorReaction.FakeInnocence:
                        weight = identity.intelligenceStat * 1.5f; // Smart move if high intelligence
                        break;

                    case InfiltratorReaction.AttemptBribery:
                        weight = identity.intelligenceStat * 1.2f;
                        break;

                    case InfiltratorReaction.ActivateAccomplices:
                        weight = identity.intelligenceStat * 2.0f; // Very smart move
                        break;

                    case InfiltratorReaction.TransmitIntelNow:
                        weight = identity.intelligenceStat * 2.0f; // Very smart move
                        break;
                }

                weights[reaction] = weight;
            }

            // Choose weighted random reaction
            return availableReactions.RandomElementByWeight(r => weights[r]);
        }
    }
}
