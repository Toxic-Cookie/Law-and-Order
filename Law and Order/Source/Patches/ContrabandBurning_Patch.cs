using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using LawAndOrder;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Detect when ingredients are consumed in burn contraband recipe and give mood buff.
    /// </summary>
    [HarmonyPatch(typeof(Bill_Production), "Notify_IterationCompleted")]
    public static class ContrabandBurnNotify_Patch
    {
        static void Postfix(Bill_Production __instance, Pawn billDoer, List<Thing> ingredients)
        {
            if (__instance?.recipe?.defName != "LawAndOrder_BurnContraband")
                return;

            if (billDoer == null || !billDoer.IsColonist)
                return;

            try
            {
                // Skip if game hasn't started yet
                if (Current.Game == null || Find.FactionManager == null)
                    return;

                Faction playerFaction = Find.FactionManager.OfPlayer;
                if (playerFaction == null)
                    return;

                // Give mood buff when burn contraband recipe completes
                ThoughtDef destroyedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_DestroyedContraband");
                if (destroyedThought == null)
                    return;

                // Get the map
                Map map = billDoer.Map;
                if (map == null)
                    return;

                // Give all colonists on the map a mood buff
                foreach (Pawn colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist.needs?.mood?.thoughts?.memories != null)
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(destroyedThought);
                    }
                }

                #if DEBUG
                string ingredientList = ingredients != null ? string.Join(", ", ingredients.Select(i => i.LabelCap)) : "unknown";
                Mod.Log?.Message($"Contraband burned via recipe by {billDoer.LabelShort}: {ingredientList} - colonists gained mood buff");
                #endif
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in ContrabandBurnNotify_Patch: {e}");
            }
        }
    }

    /// <summary>
    /// Auto-set ingredient filter for burn contraband bills to only allow contraband items.
    /// </summary>
    [HarmonyPatch(typeof(Bill_Production), "ExposeData")]
    public static class BurnContrabandFilter_Save_Patch
    {
        static void Postfix(Bill_Production __instance)
        {
            if (__instance?.recipe?.defName != "LawAndOrder_BurnContraband")
                return;

            try
            {
                // After loading, update the filter
                UpdateBurnContrabandFilter(__instance);
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in BurnContrabandFilter_Save_Patch: {e}");
            }
        }

        private static void UpdateBurnContrabandFilter(Bill_Production bill)
        {
            if (bill?.recipe?.defName != "LawAndOrder_BurnContraband")
                return;

            var contrabandManager = WorldComponent_ContrabandManager.Instance;
            if (contrabandManager == null)
                return;

            // Clear current filter and add only contraband items
            bill.ingredientFilter.SetDisallowAll();

            foreach (var contrabandDef in contrabandManager.ContrabandDefinitions)
            {
                bill.ingredientFilter.SetAllow(contrabandDef.thingDef, true);
            }
        }
    }

    /// <summary>
    /// Auto-configure ingredient filter when burn contraband bills are created.
    /// </summary>
    [HarmonyPatch(typeof(BillStack), "AddBill")]
    public static class BurnContrabandFilter_Create_Patch
    {
        static void Postfix(BillStack __instance, Bill bill)
        {
            if (!(bill is Bill_Production productionBill))
                return;

            if (productionBill?.recipe?.defName != "LawAndOrder_BurnContraband")
                return;

            try
            {
                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null || contrabandManager.ContrabandDefinitions.Count == 0)
                {
                    Messages.Message("No contraband items defined. Mark items as contraband in the Justice tab first.", MessageTypeDefOf.RejectInput);
                    return;
                }

                // Set filter to only allow contraband items
                productionBill.ingredientFilter.SetDisallowAll();

                foreach (var contrabandDef in contrabandManager.ContrabandDefinitions)
                {
                    productionBill.ingredientFilter.SetAllow(contrabandDef.thingDef, true);
                }

                #if DEBUG
                Mod.Log?.Message($"Burn contraband bill created with {contrabandManager.ContrabandDefinitions.Count} contraband items in filter");
                #endif
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in BurnContrabandFilter_Create_Patch: {e}");
            }
        }
    }

    /// <summary>
    /// Update burn contraband bill filters when contraband list changes.
    /// </summary>
    public static class BurnContrabandFilterUpdater
    {
        /// <summary>
        /// Call this whenever contraband definitions change to update all burn contraband bills.
        /// </summary>
        public static void UpdateAllBurnContrabandBills()
        {
            try
            {
                if (Current.Game == null)
                    return;

                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null)
                    return;

                int updatedCount = 0;

                // Find all burn contraband bills across all maps
                foreach (Map map in Find.Maps)
                {
                    foreach (Building building in map.listerBuildings.allBuildingsColonist)
                    {
                        if (building is IBillGiver billGiver)
                        {
                            foreach (Bill bill in billGiver.BillStack)
                            {
                                if (bill is Bill_Production productionBill &&
                                    productionBill.recipe?.defName == "LawAndOrder_BurnContraband")
                                {
                                    // Update filter
                                    productionBill.ingredientFilter.SetDisallowAll();

                                    foreach (var contrabandDef in contrabandManager.ContrabandDefinitions)
                                    {
                                        productionBill.ingredientFilter.SetAllow(contrabandDef.thingDef, true);
                                    }

                                    updatedCount++;
                                }
                            }
                        }
                    }
                }

                #if DEBUG
                if (updatedCount > 0)
                {
                    Mod.Log?.Message($"Updated {updatedCount} burn contraband bill(s) with new contraband filters");
                }
                #endif
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in BurnContrabandFilterUpdater: {e}");
            }
        }
    }
}
