using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// ThingComp that allows tables to be designated as interrogation tables.
    /// Similar to the Judge's Bench concept - any table can be designated for interrogations.
    /// </summary>
    public class Comp_InterrogationTable : ThingComp
    {
        private bool isDesignated = false;

        public bool IsDesignated => isDesignated;

        public CompProperties_InterrogationTable Props => (CompProperties_InterrogationTable)props;

        /// <summary>
        /// Save/load the designation status
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isDesignated, "isDesignatedInterrogationTable", false);
        }

        /// <summary>
        /// Provides the gizmo (right-click button) to designate/undesignate as interrogation table
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only show gizmo if the table is not forbidden or destroyed
            if (parent.Spawned && !parent.IsForbidden(Faction.OfPlayer))
            {
                yield return new Command_Toggle
                {
                    defaultLabel = isDesignated
                        ? "LawAndOrder_UndesignateInterrogationTable".Translate()
                        : "LawAndOrder_DesignateInterrogationTable".Translate(),
                    defaultDesc = isDesignated
                        ? "LawAndOrder_UndesignateInterrogationTableDesc".Translate()
                        : "LawAndOrder_DesignateInterrogationTableDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true), // Use vanilla icon
                    isActive = () => isDesignated,
                    toggleAction = delegate
                    {
                        ToggleDesignation();
                    },
                    hotKey = KeyBindingDefOf.Misc1
                };
            }
        }

        /// <summary>
        /// Toggle the designation status
        /// </summary>
        private void ToggleDesignation()
        {
            isDesignated = !isDesignated;

            Log.Message($"[Law & Order] Table {parent.Label} (ID: {parent.thingIDNumber}) designated status changed to: {isDesignated}");

            if (isDesignated)
            {
                Messages.Message(
                    "LawAndOrder_InterrogationTableDesignated".Translate(parent.Label),
                    parent,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                Messages.Message(
                    "LawAndOrder_InterrogationTableUndesignated".Translate(parent.Label),
                    parent,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        /// <summary>
        /// Adds inspection string showing designation status and effectiveness breakdown
        /// </summary>
        public override string CompInspectStringExtra()
        {
            if (!isDesignated)
            {
                return null;
            }

            // Start with designation status
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("LawAndOrder_InterrogationTableStatus".Translate());

            // Add room effectiveness breakdown
            Room room = parent.GetRoom();
            if (room != null && !room.PsychologicallyOutdoors)
            {
                sb.Append("\n");
                sb.Append("LawAndOrder_InterrogationEffectivenessFactors".Translate());

                // Impressiveness factor
                float impressiveness = room.GetStat(RoomStatDefOf.Impressiveness);
                string impressivenessImpact = GetImpressivenessImpact(impressiveness);
                sb.Append($"\n  {"LawAndOrder_Factor_Impressiveness".Translate()}: {impressiveness:F0} ({impressivenessImpact})");

                // Cleanliness factor
                float cleanliness = room.GetStat(RoomStatDefOf.Cleanliness);
                string cleanlinessImpact = GetCleanlinessImpact(cleanliness);
                sb.Append($"\n  {"LawAndOrder_Factor_Cleanliness".Translate()}: {cleanliness:F1} ({cleanlinessImpact})");

                // Space factor
                float space = room.GetStat(RoomStatDefOf.Space);
                string spaceImpact = GetSpaceImpact(space);
                sb.Append($"\n  {"LawAndOrder_Factor_Space".Translate()}: {space:F0} ({spaceImpact})");
            }
            else
            {
                sb.Append("\n");
                sb.Append("LawAndOrder_InterrogationTableOutdoors".Translate());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get impact description for impressiveness
        /// </summary>
        private string GetImpressivenessImpact(float impressiveness)
        {
            if (impressiveness < 20)
                return "LawAndOrder_Impact_Negative".Translate();
            else if (impressiveness < 40)
                return "LawAndOrder_Impact_Neutral".Translate();
            else if (impressiveness < 65)
                return "LawAndOrder_Impact_Positive".Translate();
            else if (impressiveness < 100)
                return "LawAndOrder_Impact_VeryPositive".Translate();
            else
                return "LawAndOrder_Impact_Excellent".Translate();
        }

        /// <summary>
        /// Get impact description for cleanliness
        /// </summary>
        private string GetCleanlinessImpact(float cleanliness)
        {
            if (cleanliness < -2.0f)
                return "LawAndOrder_Impact_VeryNegative".Translate();
            else if (cleanliness < -0.5f)
                return "LawAndOrder_Impact_Negative".Translate();
            else if (cleanliness < 0f)
                return "LawAndOrder_Impact_SlightlyNegative".Translate();
            else if (cleanliness < 0.5f)
                return "LawAndOrder_Impact_Positive".Translate();
            else
                return "LawAndOrder_Impact_VeryPositive".Translate();
        }

        /// <summary>
        /// Get impact description for space
        /// </summary>
        private string GetSpaceImpact(float space)
        {
            if (space < 12.5f)
                return "LawAndOrder_Impact_Neutral".Translate();
            else if (space < 29f)
                return "LawAndOrder_Impact_SlightlyPositive".Translate();
            else if (space < 55f)
                return "LawAndOrder_Impact_Positive".Translate();
            else if (space < 100f)
                return "LawAndOrder_Impact_VeryPositive".Translate();
            else
                return "LawAndOrder_Impact_Excellent".Translate();
        }

        /// <summary>
        /// Helper method to find all designated interrogation tables on a map
        /// </summary>
        public static List<Thing> GetAllInterrogationTables(Map map)
        {
            List<Thing> tables = new List<Thing>();

            if (map == null)
            {
                Log.Warning("[Law & Order] GetAllInterrogationTables called with null map");
                return tables;
            }

            // Check all buildings
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                Comp_InterrogationTable comp = thing.TryGetComp<Comp_InterrogationTable>();
                if (comp != null && comp.IsDesignated)
                {
                    tables.Add(thing);
                }
            }

            if (tables.Count == 0)
            {
                Log.Message($"[Law & Order] No designated interrogation tables found on map. Total buildings checked: {map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).Count}");
            }
            else
            {
                Log.Message($"[Law & Order] Found {tables.Count} designated interrogation tables");
            }

            return tables;
        }

        /// <summary>
        /// Find the nearest designated interrogation table to a position
        /// </summary>
        public static Thing FindNearestInterrogationTable(Pawn pawn, Pawn prisoner)
        {
            List<Thing> tables = GetAllInterrogationTables(pawn.Map);

            if (tables.Count == 0)
            {
                return null;
            }

            // Find nearest table that's accessible
            Thing nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Thing table in tables)
            {
                // Check if pawn can reach the table
                if (!pawn.CanReach(table, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }

                float dist = table.Position.DistanceTo(pawn.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = table;
                }
            }

            return nearest;
        }
    }
}
