using RimWorld;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Colonists feel bad when the colony has items marked as contraband.
    /// Creates hypocrisy penalty: "We punish prisoners for having things we keep ourselves."
    /// </summary>
    public class ThoughtWorker_ColonyHasContraband : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Skip if game hasn't started yet (during world generation)
            if (Current.Game == null || Find.FactionManager == null)
                return ThoughtState.Inactive;

            // Get player faction safely
            Faction playerFaction = Find.FactionManager.OfPlayer;
            if (playerFaction == null)
                return ThoughtState.Inactive;

            // Only colonists care about hypocrisy
            if (!p.IsColonist)
                return ThoughtState.Inactive;

            // Get contraband manager
            var contrabandManager = WorldComponent_ContrabandManager.Instance;
            if (contrabandManager == null)
                return ThoughtState.Inactive;

            var contrabandDefinitions = contrabandManager.ContrabandDefinitions;
            if (contrabandDefinitions == null || contrabandDefinitions.Count == 0)
                return ThoughtState.Inactive;

            // Check if pawn is on a map
            Map map = p.Map;
            if (map == null)
                return ThoughtState.Inactive;

            // Check colony storage and colonist inventories for contraband items
            foreach (var contrabandDef in contrabandDefinitions)
            {
                // Check storage zones for spawned contraband items (not forbidden)
                // Items in storage don't have Faction set, so check if spawned and not forbidden
                var items = map.listerThings.ThingsOfDef(contrabandDef.thingDef);
                if (items.Any(t => t.Spawned && !t.IsForbidden(playerFaction)))
                {
                    return ThoughtState.ActiveAtStage(0); // Found contraband in colony storage
                }

                // Check colonist inventories, equipment, and apparel
                foreach (Pawn colonist in map.mapPawns.FreeColonists)
                {
                    // Check inventory
                    if (colonist.inventory?.innerContainer?.Contains(contrabandDef.thingDef) == true)
                    {
                        return ThoughtState.ActiveAtStage(0); // Colonist carrying contraband
                    }

                    // Check equipment (weapons, shield belts, etc.)
                    if (colonist.equipment?.Primary?.def == contrabandDef.thingDef)
                    {
                        return ThoughtState.ActiveAtStage(0); // Colonist equipped with contraband weapon
                    }

                    // Check apparel (clothing, armor)
                    if (colonist.apparel?.WornApparel?.Any(a => a.def == contrabandDef.thingDef) == true)
                    {
                        return ThoughtState.ActiveAtStage(0); // Colonist wearing contraband apparel
                    }
                }
            }

            return ThoughtState.Inactive;
        }
    }
}
