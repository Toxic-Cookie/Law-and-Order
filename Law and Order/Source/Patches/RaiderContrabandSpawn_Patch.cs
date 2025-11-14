using HarmonyLib;
using RimWorld;
using Verse;
using LawAndOrder;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Check for contraband when raiders/hostiles spawn on the map.
    /// This allows pre-emptive debt assignment before capture.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    public static class RaiderContrabandSpawn_Patch
    {
        static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            // Skip if this is a load from save
            if (respawningAfterLoad)
                return;

            // Skip if not a pawn or no map
            if (__instance == null || map == null)
                return;

            try
            {
                // Skip if game hasn't started yet
                if (Current.Game == null || Find.FactionManager == null)
                    return;

                Faction playerFaction = Find.FactionManager.OfPlayer;
                if (playerFaction == null)
                    return;

                // Only check hostile pawns (potential raiders)
                if (__instance.Faction == null || !__instance.Faction.HostileTo(playerFaction))
                    return;

                // Skip if not humanlike (animals, mechanoids don't carry contraband)
                if (!__instance.RaceProps.Humanlike)
                    return;

                // Check for contraband in inventory, equipment, and apparel
                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null)
                    return;

                int totalContrabandValue = 0;
                var contrabandItems = new System.Collections.Generic.Dictionary<ThingDef, int>();

                // Check inventory
                if (__instance.inventory?.innerContainer != null)
                {
                    foreach (Thing thing in __instance.inventory.innerContainer)
                    {
                        var contrabandDef = contrabandManager.GetContrabandDefinition(thing.def);
                        if (contrabandDef != null)
                        {
                            int value = contrabandDef.silverPenaltyPerItem * thing.stackCount;
                            totalContrabandValue += value;

                            // Track count of this item type
                            if (contrabandItems.ContainsKey(thing.def))
                                contrabandItems[thing.def] += thing.stackCount;
                            else
                                contrabandItems[thing.def] = thing.stackCount;
                        }
                    }
                }

                // Check equipment
                if (__instance.equipment?.Primary != null)
                {
                    var contrabandDef = contrabandManager.GetContrabandDefinition(__instance.equipment.Primary.def);
                    if (contrabandDef != null)
                    {
                        totalContrabandValue += contrabandDef.silverPenaltyPerItem;

                        if (contrabandItems.ContainsKey(__instance.equipment.Primary.def))
                            contrabandItems[__instance.equipment.Primary.def]++;
                        else
                            contrabandItems[__instance.equipment.Primary.def] = 1;
                    }
                }

                // Check apparel
                if (__instance.apparel?.WornApparel != null)
                {
                    foreach (var apparel in __instance.apparel.WornApparel)
                    {
                        var contrabandDef = contrabandManager.GetContrabandDefinition(apparel.def);
                        if (contrabandDef != null)
                        {
                            totalContrabandValue += contrabandDef.silverPenaltyPerItem;

                            if (contrabandItems.ContainsKey(apparel.def))
                                contrabandItems[apparel.def]++;
                            else
                                contrabandItems[apparel.def] = 1;
                        }
                    }
                }

                // If contraband found, record crime and add debt
                if (totalContrabandValue > 0)
                {
                    // Build description of contraband items
                    string itemList = string.Join(", ", System.Linq.Enumerable.Select(contrabandItems, kvp => $"{kvp.Value}x {kvp.Key.LabelCap}"));
                    string additionalInfo = $"Detected on arrival: {itemList}";

                    // Record the crime (this creates the criminal record if needed)
                    CrimeUtils.RecordCrime(
                        criminal: __instance,
                        crimeType: Law_and_Order.Source.Hediffs.CrimeType.ContrabandPossession,
                        victim: null,
                        targetThing: null,
                        damageDealt: 0f,
                        additionalInfo: additionalInfo,
                        wasVictimDowned: false,
                        wasVictimKilled: false
                    );

                    // TODO Phase 1: Instead of auto-debt, create crime with Suspected state
                    // DebtUtils.AddDebtForContraband(__instance, totalContrabandValue, contrabandItems);

                    #if DEBUG
                    Mod.Log?.Message($"Raider {__instance.LabelShort} spawned with {totalContrabandValue} silver worth of contraband: {itemList}");
                    #endif
                }
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in RaiderContrabandSpawn_Patch: {e}");
            }
        }
    }
}
