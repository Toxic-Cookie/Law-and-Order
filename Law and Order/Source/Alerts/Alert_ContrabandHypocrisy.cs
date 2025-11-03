using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using LawAndOrder;

namespace Law_and_Order.Source.Alerts
{
    /// <summary>
    /// Alert shown when the colony has items that are marked as contraband.
    /// Warns about hypocrisy: punishing prisoners for having items the colony keeps.
    /// </summary>
    public class Alert_ContrabandHypocrisy : Alert
    {
        private List<ThingDef> hypocriticalItemsResult = new List<ThingDef>();

        public Alert_ContrabandHypocrisy()
        {
            defaultLabel = "Contraband hypocrisy";
            defaultPriority = AlertPriority.Medium;
        }

        private List<ThingDef> HypocriticalItems
        {
            get
            {
                hypocriticalItemsResult.Clear();

                // Skip if game hasn't started yet (during world generation)
                if (Current.Game == null || Find.FactionManager == null)
                    return hypocriticalItemsResult;

                // Get player faction safely
                Faction playerFaction = Find.FactionManager.OfPlayer;
                if (playerFaction == null)
                    return hypocriticalItemsResult;

                // Get contraband manager
                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null)
                    return hypocriticalItemsResult;

                var contrabandDefinitions = contrabandManager.ContrabandDefinitions;
                if (contrabandDefinitions == null || contrabandDefinitions.Count == 0)
                    return hypocriticalItemsResult;

                // Check all player home maps for contraband items
                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome)
                        continue;

                    // Check each contraband definition
                    foreach (var contrabandDef in contrabandDefinitions)
                    {
                        // Check if colony has this item in storage (spawned, not forbidden)
                        var items = map.listerThings.ThingsOfDef(contrabandDef.thingDef);
                        if (items.Any(t => t.Spawned && !t.IsForbidden(playerFaction)))
                        {
                            if (!hypocriticalItemsResult.Contains(contrabandDef.thingDef))
                            {
                                hypocriticalItemsResult.Add(contrabandDef.thingDef);
                            }
                        }

                        // Check colonist inventories, equipment, and apparel
                        foreach (Pawn colonist in map.mapPawns.FreeColonists)
                        {
                            // Check inventory
                            if (colonist.inventory?.innerContainer?.Contains(contrabandDef.thingDef) == true)
                            {
                                if (!hypocriticalItemsResult.Contains(contrabandDef.thingDef))
                                {
                                    hypocriticalItemsResult.Add(contrabandDef.thingDef);
                                }
                            }

                            // Check equipment
                            if (colonist.equipment?.Primary?.def == contrabandDef.thingDef)
                            {
                                if (!hypocriticalItemsResult.Contains(contrabandDef.thingDef))
                                {
                                    hypocriticalItemsResult.Add(contrabandDef.thingDef);
                                }
                            }

                            // Check apparel
                            if (colonist.apparel?.WornApparel?.Any(a => a.def == contrabandDef.thingDef) == true)
                            {
                                if (!hypocriticalItemsResult.Contains(contrabandDef.thingDef))
                                {
                                    hypocriticalItemsResult.Add(contrabandDef.thingDef);
                                }
                            }
                        }
                    }
                }

                return hypocriticalItemsResult;
            }
        }

        public override AlertReport GetReport()
        {
            // Only show alert if colony is established
            if (!Find.PlaySettings.useWorkPriorities)
                return false;

            List<ThingDef> items = HypocriticalItems;
            if (items.Count == 0)
                return false;

            return AlertReport.Active;
        }

        public override TaggedString GetExplanation()
        {
            List<ThingDef> items = HypocriticalItems;

            string itemList = string.Join("\n", items.Select(def => $"  - {def.LabelCap}"));

            return $"The colony has items that are marked as contraband:\n\n{itemList}\n\n" +
                   "This is hypocritical - we punish prisoners for having items we keep ourselves.\n\n" +
                   "Colonists have a mood penalty (-3) for this double standard.\n\n" +
                   "Consider:\n" +
                   "  - Destroying these items (mood bonus)\n" +
                   "  - Trading/gifting them away\n" +
                   "  - Removing them from the contraband list";
        }
    }
}
