using HarmonyLib;
using RimWorld;
using Verse;
using Law_and_Order.Source;

namespace LawAndOrder
{
    /// <summary>
    /// Harmony patches for detecting contraband on raiders and enemies.
    /// </summary>
    [HarmonyPatch]
    public static class ContrabandDetection_Patch
    {
        /// <summary>
        /// Patch: When a pawn is made a prisoner, scan for contraband.
        /// This catches raiders when they're captured.
        /// </summary>
        [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
        [HarmonyPostfix]
        public static void SetGuestStatus_Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus, Pawn ___pawn)
        {
            try
            {
                // Only process when becoming a prisoner of the player's colony
                if (guestStatus != GuestStatus.Prisoner)
                    return;

                if (newHost == null || !newHost.IsPlayer)
                    return;

                // ___pawn is injected by Harmony from the private field
                if (___pawn == null)
                    return;

                // Scan for contraband
                if (ContrabandUtils.ScanAndApplyContrabandPenalties(___pawn))
                {
                    // Notify the player
                    Messages.Message(
                        $"Contraband detected on {___pawn.LabelShort}. Penalties applied.",
                        ___pawn,
                        MessageTypeDefOf.NeutralEvent
                    );

                    if (Prefs.DevMode)
                    {
                        Law_and_Order.Source.Mod.Log?.Message($"Contraband scan completed for {___pawn.LabelShort}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Law_and_Order.Source.Mod.Log?.Error($"Error in SetGuestStatus_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Patch: When a hostile pawn spawns during a raid, mark them for contraband scanning.
        /// We scan them when they die or are captured.
        /// </summary>
        [HarmonyPatch(typeof(Pawn), "Kill")]
        [HarmonyPrefix]
        public static void Kill_Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                // Only process hostile pawns
                if (__instance == null)
                    return;

                if (__instance.Faction == null || !__instance.Faction.HostileTo(Faction.OfPlayer))
                    return;

                // Only scan if they have an inventory
                if (__instance.inventory?.innerContainer == null || __instance.inventory.innerContainer.Count == 0)
                    return;

                // Scan for contraband before they die (so we can still access their inventory)
                if (ContrabandUtils.ScanAndApplyContrabandPenalties(__instance))
                {
                    if (Prefs.DevMode)
                    {
                        Law_and_Order.Source.Mod.Log?.Message($"Contraband detected on dying hostile: {__instance.LabelShort}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Law_and_Order.Source.Mod.Log?.Error($"Error in Kill_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Patch: When hostile pawns spawn on the map during a raid, we could scan them immediately.
        /// However, this is disabled by default as it may feel "unfair" to penalize before combat.
        /// Enable this if you want instant contraband detection.
        /// </summary>
        /*
        [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
        [HarmonyPostfix]
        public static void SpawnSetup_Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            try
            {
                // Don't scan on load
                if (respawningAfterLoad)
                    return;

                // Only process hostile pawns
                if (__instance == null)
                    return;

                if (__instance.Faction == null || !__instance.Faction.HostileTo(Faction.OfPlayer))
                    return;

                // Only scan if they have an inventory
                if (__instance.inventory?.innerContainer == null || __instance.inventory.innerContainer.Count == 0)
                    return;

                // Scan for contraband when they spawn
                ContrabandUtils.ScanAndApplyContrabandPenalties(__instance);
            }
            catch (System.Exception ex)
            {
                Mod.Log?.Error($"Error in SpawnSetup_Postfix: {ex}");
            }
        }
        */
    }
}
