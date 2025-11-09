using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.CrimeDetection
{
    /// <summary>
    /// Crime detection patches that automatically track crimes committed by pawns
    /// </summary>
    public static class CrimeDetectionPatches
    {
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

                    // Only track if:
                    // 1. Both pawns exist
                    // 2. Victim is a colonist
                    // 3. Attacker is hostile
                    if (victim == null || attacker == null)
                        return;

                    if (!victim.IsColonist)
                        return;

                    if (!attacker.HostileTo(victim.Faction))
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
        /// </summary>
        [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
        public static class TrackPropertyDamage_Patch
        {
            static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
            {
                try
                {
                    // Only track if the thing is owned by the player
                    if (__instance.Faction != Faction.OfPlayer)
                        return;

                    // Only track buildings and valuable items (not plants, filth, etc.)
                    if (!(__instance is Building || __instance is MinifiedThing ||
                          (__instance.def.category == ThingCategory.Item && __instance.MarketValue > 50)))
                        return;

                    // Get the attacker
                    Pawn attacker = dinfo.Instigator as Pawn;
                    if (attacker == null)
                        return;

                    // Only track if attacker is hostile to player
                    if (!attacker.HostileTo(Faction.OfPlayer))
                        return;

                    // Determine crime type based on damage type
                    CrimeType crimeType = CrimeType.PropertyDestruction;

                    // Check if it's arson (flame damage)
                    if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn)
                    {
                        crimeType = CrimeType.Arson;
                    }

                    // Record the crime
                    CrimeUtils.RecordCrime(
                        criminal: attacker,
                        crimeType: crimeType,
                        targetThing: __instance,
                        damageDealt: __result.totalDamageDealt,
                        additionalInfo: $"Damaged {__instance.Label} with {dinfo.Def.label}"
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

                    // Basic validation checks
                    if (victim == null || attacker == null)
                        return;

                    // Only track if attacker is hostile to player
                    if (!attacker.HostileTo(Faction.OfPlayer))
                        return;

                    // Check if victim is a colony animal (not wild, not hostile)
                    if (!victim.RaceProps.Animal)
                        return;

                    if (victim.Faction != Faction.OfPlayer)
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
                    if (pawn == null)
                        return;

                    // Only track if the pawn is hostile to player
                    if (!pawn.HostileTo(Faction.OfPlayer))
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
                    if (victim == null)
                        return;

                    // Only track if kidnapper is hostile to player
                    if (!pawn.HostileTo(Faction.OfPlayer))
                        return;

                    // Only track if victim is a colonist or player-owned pawn
                    if (!victim.IsColonist && victim.Faction != Faction.OfPlayer)
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
