using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// ThingComp that allows chairs to be designated as interrogation chairs for prisoners.
    /// </summary>
    public class Comp_InterrogationChair : ThingComp
    {
        private bool isDesignated = false;

        public bool IsDesignated => isDesignated;

        public CompProperties_InterrogationChair Props => (CompProperties_InterrogationChair)props;

        /// <summary>
        /// Save/load the designation status
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isDesignated, "isDesignatedInterrogationChair", false);
        }

        /// <summary>
        /// Provides the gizmo (right-click button) to designate/undesignate as interrogation chair
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only show gizmo if the chair is not forbidden or destroyed
            if (parent.Spawned && !parent.IsForbidden(Faction.OfPlayer))
            {
                yield return new Command_Toggle
                {
                    defaultLabel = isDesignated
                        ? "LawAndOrder_UndesignateInterrogationChair".Translate()
                        : "LawAndOrder_DesignateInterrogationChair".Translate(),
                    defaultDesc = isDesignated
                        ? "LawAndOrder_UndesignateInterrogationChairDesc".Translate()
                        : "LawAndOrder_DesignateInterrogationChairDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true),
                    isActive = () => isDesignated,
                    toggleAction = delegate
                    {
                        ToggleDesignation();
                    },
                    hotKey = KeyBindingDefOf.Misc2
                };
            }
        }

        /// <summary>
        /// Toggle the designation status
        /// </summary>
        private void ToggleDesignation()
        {
            isDesignated = !isDesignated;

            if (isDesignated)
            {
                Messages.Message(
                    "LawAndOrder_InterrogationChairDesignated".Translate(parent.Label),
                    parent,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                Messages.Message(
                    "LawAndOrder_InterrogationChairUndesignated".Translate(parent.Label),
                    parent,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        /// <summary>
        /// Adds inspection string showing designation status
        /// </summary>
        public override string CompInspectStringExtra()
        {
            if (isDesignated)
            {
                return "LawAndOrder_InterrogationChairStatus".Translate();
            }
            return null;
        }

        /// <summary>
        /// Find all designated interrogation chairs on a map
        /// </summary>
        public static List<Thing> GetAllInterrogationChairs(Map map)
        {
            List<Thing> chairs = new List<Thing>();

            if (map == null)
            {
                return chairs;
            }

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                Comp_InterrogationChair comp = thing.TryGetComp<Comp_InterrogationChair>();
                if (comp != null && comp.IsDesignated)
                {
                    chairs.Add(thing);
                }
            }

            return chairs;
        }

        /// <summary>
        /// Find the nearest designated interrogation chair to a position
        /// </summary>
        public static Thing FindNearestInterrogationChair(IntVec3 position, Map map, Pawn searcher = null)
        {
            List<Thing> chairs = GetAllInterrogationChairs(map);

            if (chairs.Count == 0)
            {
                return null;
            }

            Thing nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Thing chair in chairs)
            {
                // Check if searcher can reach the chair (if searcher provided)
                if (searcher != null && !searcher.CanReach(chair, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }

                float dist = chair.Position.DistanceTo(position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = chair;
                }
            }

            return nearest;
        }
    }
}
