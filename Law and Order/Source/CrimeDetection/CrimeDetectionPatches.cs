using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using System.Collections.Generic;

namespace Law_and_Order.Source.CrimeDetection
{
    /// <summary>
    /// Crime detection patches that automatically track crimes committed by pawns
    /// </summary>
    public static class CrimeDetectionPatches
    {
        // Track which items have been damaged to prevent duplicate crime records
        // Key: Thing ID, Value: (Pawn, CrimeType) tuple
        private static Dictionary<int, (Pawn attacker, CrimeType crimeType)> damagedItemsTracker = new Dictionary<int, (Pawn, CrimeType)>();

        // Clear the tracker periodically to prevent memory leaks
        private static int clearTrackerTick = 0;

        public static void ClearDamagedItemsTracker()
        {
            clearTrackerTick++;
            if (clearTrackerTick > 60000) // Clear every in-game day
            {
                damagedItemsTracker.Clear();
                clearTrackerTick = 0;
            }
        }

        /// <summary>
        /// Helper method to check if a pawn is a valid criminal
        /// (exists, is not an animal, and is hostile to player)
        /// </summary>
        private static bool IsValidCriminal(Pawn pawn)
        {
            if (pawn == null)
                return false;

            // Animals cannot commit crimes
            if (pawn.RaceProps.Animal)
                return false;

            // Must be hostile to player
            if (!pawn.HostileTo(Faction.OfPlayer))
                return false;

            return true;
        }

        /// <summary>
        /// Helper method to check if a pawn is a valid colonist victim
        /// </summary>
        private static bool IsValidColonistVictim(Pawn pawn)
        {
            if (pawn == null)
                return false;

            return pawn.IsColonist;
        }

        /// <summary>
        /// Helper method to check if a pawn is a valid player-owned animal victim
        /// </summary>
        private static bool IsValidPlayerAnimalVictim(Pawn pawn)
        {
            if (pawn == null)
                return false;

            if (!pawn.RaceProps.Animal)
                return false;

            return pawn.Faction == Faction.OfPlayer;
        }
        /// <summary>
        /// Tracks when a raider damages a colonist by patching the PostApplyDamage method
        /// </summary>
        [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
        public static class TrackAssault_Patch
        {
            static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
            {
                try
                {
                    // Get the victim pawn (the pawn who owns this health tracker)
                    Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                    // Get the attacker
                    Pawn attacker = dinfo.Instigator as Pawn;

                    // Validate criminal and victim
                    if (!IsValidCriminal(attacker))
                        return;

                    if (!IsValidColonistVictim(victim))
                        return;

                    // Determine crime type based on damage
                    CrimeType crimeType = victim.Dead ? CrimeType.Murder : CrimeType.Assault;

                    // Record the crime with DamageInfo for new penalty calculation system
                    CrimeUtils.RecordCrime(
                        criminal: attacker,
                        crimeType: crimeType,
                        victim: victim,
                        damageDealt: totalDamageDealt,
                        additionalInfo: $"Weapon: {dinfo.Weapon?.label ?? "Unknown"}",
                        wasVictimDowned: victim.Downed,
                        wasVictimKilled: victim.Dead,
                        damageInfo: dinfo  // Pass DamageInfo to use new penalty system
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in assault tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks property damage when buildings/things take damage
        /// Distinguishes between Vandalism (minor damage) and PropertyDestruction (major damage)
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        public static class TrackPropertyDamage_Patch
        {
            static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
            {
                try
                {
                    // Clear tracker periodically to prevent memory leaks
                    ClearDamagedItemsTracker();

                    // Only track buildings and valuable items (not plants, filth, etc.)
                    bool isBuilding = __instance is Building || __instance is MinifiedThing;
                    bool isValuableItem = __instance.def.category == ThingCategory.Item && __instance.MarketValue > 50;

                    if (!(isBuilding || isValuableItem))
                        return;

                    // Check ownership differently for buildings vs items
                    bool isPlayerOwned = false;

                    if (isBuilding)
                    {
                        // Buildings have faction set
                        isPlayerOwned = __instance.Faction == Faction.OfPlayer;
                    }
                    else if (isValuableItem)
                    {
                        // Items typically don't have faction, check if they're in player home area or storage
                        Map map = __instance.Map;
                        if (map != null)
                        {
                            // Check if item is in home area or in any zone
                            IntVec3 pos = __instance.Position;
                            isPlayerOwned = map.areaManager.Home[pos] ||
                                          map.zoneManager.ZoneAt(pos) != null;
                        }
                    }

                    if (!isPlayerOwned)
                        return;

                    // Get the attacker
                    Pawn attacker = dinfo.Instigator as Pawn;

                    // Validate criminal
                    if (!IsValidCriminal(attacker))
                        return;

                    // Calculate HP percentage after damage
                    float hpPercentage = __instance.HitPoints / (float)__instance.MaxHitPoints;

                    // Determine crime type based on damage severity and type
                    CrimeType crimeType;

                    // Check if it's arson (flame damage) - always considered more severe
                    if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn)
                    {
                        crimeType = CrimeType.Arson;
                    }
                    // Check if the item is destroyed or severely damaged
                    else if (__instance.Destroyed || hpPercentage < 0.5f)
                    {
                        // Severe damage or destruction = PropertyDestruction
                        crimeType = CrimeType.PropertyDestruction;
                    }
                    else
                    {
                        // Minor damage = Vandalism
                        crimeType = CrimeType.Vandalism;
                    }

                    // Check if we've already recorded a crime for this item by this attacker
                    int itemKey = __instance.GetHashCode();
                    if (damagedItemsTracker.TryGetValue(itemKey, out var existingRecord))
                    {
                        // If the existing crime is less severe than the current one, upgrade it
                        if (existingRecord.crimeType == CrimeType.Vandalism &&
                            (crimeType == CrimeType.PropertyDestruction || crimeType == CrimeType.Arson))
                        {
                            // Upgrade to more severe crime type
                            damagedItemsTracker[itemKey] = (attacker, crimeType);
                        }
                        else
                        {
                            // Already recorded a crime of equal or greater severity, don't spam
                            return;
                        }
                    }
                    else
                    {
                        // First time recording damage to this item
                        damagedItemsTracker[itemKey] = (attacker, crimeType);
                    }

                    // Record the crime
                    CrimeUtils.RecordCrime(
                        criminal: attacker,
                        crimeType: crimeType,
                        targetThing: __instance,
                        damageDealt: __result.totalDamageDealt,
                        additionalInfo: $"Damaged {__instance.Label} with {dinfo.Def.label} ({hpPercentage:P0} HP remaining)"
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in property damage tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks when hostile pawns damage or kill colony animals
        /// </summary>
        [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
        public static class TrackAnimalAbuse_Patch
        {
            static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
            {
                try
                {
                    // Get the victim pawn (the pawn who owns this health tracker)
                    Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                    // Get the attacker
                    Pawn attacker = dinfo.Instigator as Pawn;

                    // Validate criminal and victim
                    if (!IsValidCriminal(attacker))
                        return;

                    if (!IsValidPlayerAnimalVictim(victim))
                        return;

                    // Record the animal abuse crime with DamageInfo for penalty calculation
                    CrimeUtils.RecordCrime(
                        criminal: attacker,
                        crimeType: CrimeType.AnimalAbuse,
                        victim: victim,
                        damageDealt: totalDamageDealt,
                        additionalInfo: $"Attacked {victim.Label} with {dinfo.Weapon?.label ?? "Unknown"}",
                        wasVictimDowned: victim.Downed,
                        wasVictimKilled: victim.Dead,
                        damageInfo: dinfo  // Pass DamageInfo to use new penalty system
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in animal abuse tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks theft when hostile pawns pick up player items
        /// </summary>
        [HarmonyPatch(typeof(Pawn_CarryTracker), "TryStartCarry", new System.Type[] { typeof(Thing) })]
        public static class TrackTheft_Patch
        {
            static void Postfix(Pawn_CarryTracker __instance, Thing item, bool __result)
            {
                try
                {
                    // Only track if the carry action succeeded
                    if (!__result)
                        return;

                    // Get the pawn who owns this carry tracker
                    Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

                    // Validate criminal
                    if (!IsValidCriminal(pawn))
                        return;

                    // Only track if the item belongs to player or is in a player storage zone
                    bool isPlayerItem = item.Faction == Faction.OfPlayer;
                    bool isInPlayerZone = false;

                    if (item.Spawned && item.Map != null)
                    {
                        var zone = item.Map.zoneManager.ZoneAt(item.Position);
                        isInPlayerZone = zone is Zone_Stockpile;
                    }

                    if (!isPlayerItem && !isInPlayerZone)
                        return;

                    // Only track valuable items
                    if (item.MarketValue < 10)
                        return;

                    // Record the theft
                    CrimeUtils.RecordCrime(
                        criminal: pawn,
                        crimeType: CrimeType.Theft,
                        targetThing: item,
                        additionalInfo: $"Stole {item.Label} (worth {item.MarketValue:F0} silver)"
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in theft tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks kidnapping when hostile pawns are given a kidnap job
        /// </summary>
        [HarmonyPatch(typeof(RimWorld.JobGiver_Kidnap), "TryGiveJob")]
        public static class TrackKidnapping_Patch
        {
            static void Postfix(Pawn pawn, Job __result)
            {
                try
                {
                    // Only track if a kidnap job was successfully created
                    if (__result == null)
                        return;

                    // Get the victim from the job target
                    Pawn victim = __result.targetA.Thing as Pawn;

                    // Validate criminal
                    if (!IsValidCriminal(pawn))
                        return;

                    // Only track if victim is a colonist or player-owned pawn
                    if (victim == null || (!victim.IsColonist && victim.Faction != Faction.OfPlayer))
                        return;

                    // Only track if victim is downed (this should always be true for kidnapping, but double-check)
                    if (!victim.Downed)
                        return;

                    // Record the kidnapping crime
                    CrimeUtils.RecordCrime(
                        criminal: pawn,
                        crimeType: CrimeType.Kidnapping,
                        victim: victim,
                        additionalInfo: $"Attempted to kidnap {victim.NameShortColored}"
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in kidnapping tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks when explosions create toxic gas clouds
        /// This allows us to track who is responsible for the gas
        /// </summary>
        [HarmonyPatch(typeof(Verse.Explosion), "StartExplosion")]
        public static class TrackToxicGasCreation_Patch
        {
            static void Postfix(Verse.Explosion __instance)
            {
                try
                {
                    // Check if this explosion will create toxic gas
                    var gasType = Traverse.Create(__instance).Field("postExplosionGasType").GetValue<GasType?>();
                    if (gasType.HasValue && gasType.Value == GasType.ToxGas)
                    {
                        // Get the instigator (the pawn who threw the grenade)
                        var instigator = Traverse.Create(__instance).Field("instigator").GetValue<Thing>();
                        Pawn instigatorPawn = instigator as Pawn;

                        if (instigatorPawn != null && __instance.Map != null)
                        {
                            // Get the gas tracker component
                            var tracker = __instance.Map.GetComponent<Law_and_Order.Source.Components.MapComponent_ToxicGasTracker>();
                            if (tracker != null)
                            {
                                // Register this gas cloud with its instigator
                                float radius = Traverse.Create(__instance).Field("radius").GetValue<float>();
                                tracker.RegisterGasInstigator(__instance.Position, radius, instigatorPawn);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in toxic gas tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks when toxic buildup is added to colonists from toxic gas exposure
        /// Links the buildup back to the raider who created the gas
        /// </summary>
        [HarmonyPatch(typeof(Verse.HealthUtility), "AdjustSeverity")]
        public static class TrackToxicBuildup_Patch
        {
            // Track which victim-instigator pairs have already had crimes recorded
            // Key: (victim pawn ID, instigator pawn ID), Value: tick when crime was recorded
            private static Dictionary<(int, int), int> recordedToxicCrimes = new Dictionary<(int, int), int>();
            private const int CRIME_COOLDOWN_TICKS = GenDate.TicksPerHour; // Only record once per hour per victim-instigator pair

            static void Postfix(Pawn pawn, HediffDef hdDef, float sevOffset)
            {
                try
                {
                    // Only track ToxicBuildup on colonists
                    if (hdDef != RimWorld.HediffDefOf.ToxicBuildup || !pawn.IsColonist || sevOffset <= 0f)
                        return;

                    // Check if the pawn is standing in toxic gas
                    if (!pawn.Spawned || pawn.Map == null)
                        return;

                    byte gasDensity = pawn.Position.GasDensity(pawn.Map, GasType.ToxGas);
                    if (gasDensity == 0)
                        return; // Not in toxic gas

                    // Get the gas tracker to find who created this gas
                    var tracker = pawn.Map.GetComponent<Law_and_Order.Source.Components.MapComponent_ToxicGasTracker>();
                    if (tracker == null)
                        return;

                    Pawn instigator = tracker.GetGasInstigator(pawn.Position);

                    // Validate criminal
                    if (!IsValidCriminal(instigator))
                        return;

                    // Check if we've already recorded a crime for this victim-instigator pair recently
                    var crimeKey = (pawn.thingIDNumber, instigator.thingIDNumber);
                    int currentTick = Find.TickManager.TicksGame;

                    if (recordedToxicCrimes.TryGetValue(crimeKey, out int lastRecordedTick))
                    {
                        // If less than cooldown period has passed, don't record another crime
                        if (currentTick - lastRecordedTick < CRIME_COOLDOWN_TICKS)
                        {
                            return; // Skip - already recorded recently
                        }
                    }

                    // Update the last recorded time for this pair
                    recordedToxicCrimes[crimeKey] = currentTick;

                    // Clean up old entries periodically (every hour)
                    if (currentTick % GenDate.TicksPerHour == 0)
                    {
                        var keysToRemove = new List<(int, int)>();
                        foreach (var kvp in recordedToxicCrimes)
                        {
                            if (currentTick - kvp.Value > CRIME_COOLDOWN_TICKS * 2) // Remove entries older than 2x cooldown
                            {
                                keysToRemove.Add(kvp.Key);
                            }
                        }
                        foreach (var key in keysToRemove)
                        {
                            recordedToxicCrimes.Remove(key);
                        }
                    }

                    // Record the assault crime
                    // We create a fake DamageInfo to pass to the crime recording system
                    DamageInfo fakeInfo = new DamageInfo(
                        def: DamageDefOf.ToxGas,
                        amount: sevOffset * 100f, // Convert severity to approximate damage value
                        instigator: instigator,
                        angle: 0f,
                        intendedTarget: pawn
                    );

                    CrimeUtils.RecordCrime(
                        criminal: instigator,
                        crimeType: pawn.Dead ? CrimeType.Murder : CrimeType.Assault,
                        victim: pawn,
                        damageDealt: sevOffset * 100f,
                        additionalInfo: "Toxic gas exposure",
                        wasVictimDowned: pawn.Downed,
                        wasVictimKilled: pawn.Dead,
                        damageInfo: fakeInfo,
                        damageType: "toxic gas"
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in toxic buildup tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Example usage demonstrating the crime detection API
        /// </summary>
        public static void ExampleUsage()
        {
            // Example: Record a theft
            Pawn thief = null; // Your thief pawn
            Thing stolenItem = null; // The stolen item

            if (thief != null && stolenItem != null)
            {
                CrimeUtils.RecordCrime(
                    criminal: thief,
                    crimeType: CrimeType.Theft,
                    targetThing: stolenItem,
                    additionalInfo: "Stole from stockpile"
                );
            }

            // Example: Check if a pawn is a murderer
            Pawn suspect = null; // Your suspect
            if (suspect != null)
            {
                bool isMurderer = CrimeUtils.HasCommittedCrime(suspect, CrimeType.Murder);
                if (isMurderer)
                {
                    Log.Message($"{suspect.Name} is a murderer!");
                }
            }

            // Example: Get all crimes by a pawn
            Pawn criminal = null; // Your criminal
            if (criminal != null)
            {
                var record = CrimeUtils.TryGetCriminalRecord(criminal);
                if (record != null)
                {
                    Log.Message($"{criminal.Name} has committed {record.TotalCrimeCount} crimes");

                    // Get recent crimes (last 7 days)
                    var recentCrimes = record.GetRecentCrimes(7);
                    foreach (var crime in recentCrimes)
                    {
                        Log.Message($"  - {crime}");
                    }
                }
            }

            // Example: Get all murders
            Pawn prisoner = null; // A captured raider
            if (prisoner != null)
            {
                var record = CrimeUtils.TryGetCriminalRecord(prisoner);
                if (record != null)
                {
                    var murders = record.GetCrimesByType(CrimeType.Murder);
                    Log.Message($"{prisoner.Name} has committed {murders.Count} murders");
                }
            }
        }
    }
}
