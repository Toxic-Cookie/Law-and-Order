using HarmonyLib;
using RimWorld;
using Verse;
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

                    // Record the crime
                    CrimeUtils.RecordCrime(
                        criminal: attacker,
                        crimeType: crimeType,
                        victim: victim,
                        damageDealt: totalDamageDealt,
                        additionalInfo: $"Weapon: {dinfo.Weapon?.label ?? "Unknown"}"
                    );
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in assault tracking patch: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Tracks property destruction when things are destroyed
        /// </summary>
        [HarmonyPatch(typeof(Thing), "Destroy")]
        public static class TrackPropertyDestruction_Patch
        {
            static void Prefix(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
            {
                try
                {
                    // Only track if it's being destroyed (not deconstructed/killed normally)
                    if (mode != DestroyMode.KillFinalize)
                        return;

                    // Only track colony buildings/items
                    if (__instance.Faction != Faction.OfPlayer)
                        return;

                    // Try to find who caused the destruction
                    // This is tricky - you might need to track recent attackers separately
                    // For now, this is just a placeholder showing the structure

                    // Example: if there's a recent attacker map (you'd need to implement this)
                    // Pawn attacker = SomeSystemToTrackRecentAttacker(__instance);
                    // if (attacker != null)
                    // {
                    //     CrimeUtils.RecordCrime(
                    //         criminal: attacker,
                    //         crimeType: CrimeType.PropertyDestruction,
                    //         targetThing: __instance
                    //     );
                    // }
                }
                catch (System.Exception e)
                {
                    Law_and_Order.Source.Mod.Log?.Error($"Error in property destruction tracking patch: {e.Message}\n{e.StackTrace}");
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
