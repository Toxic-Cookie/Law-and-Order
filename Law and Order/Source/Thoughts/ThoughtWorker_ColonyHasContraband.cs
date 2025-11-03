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
                // Check storage zones for player-owned contraband items
                var items = map.listerThings.ThingsOfDef(contrabandDef.thingDef);
                if (items.Any(t => t.Faction == Faction.OfPlayer))
                {
                    return ThoughtState.ActiveAtStage(0); // Found contraband in colony
                }

                // Check colonist inventories (including this pawn)
                foreach (Pawn colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist.inventory?.innerContainer?.Contains(contrabandDef.thingDef) == true)
                    {
                        return ThoughtState.ActiveAtStage(0); // Colonist carrying contraband
                    }
                }
            }

            return ThoughtState.Inactive;
        }
    }
}
