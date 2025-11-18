using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Harmony patches to detect when infiltrators leave the map and transmit intelligence
    /// </summary>
    [HarmonyPatch]
    public static class IntelTransmissionPatches
    {
        /// <summary>
        /// Patch Pawn.DeSpawn to detect when infiltrators leave the map
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
        [HarmonyPostfix]
        public static void Pawn_DeSpawn_Postfix(Pawn __instance, DestroyMode mode)
        {
            try
            {
                // Only care about pawns that left the map (not dead/destroyed)
                if (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize)
                    return;

                // Check if this pawn has a hidden identity
                if (__instance?.health?.hediffSet == null)
                    return;

                var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(
                    LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity;

                if (hediff == null)
                    return;

                // Don't transmit if identity was already revealed (they're exposed)
                if (hediff.identityRevealed)
                {
                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Exposed infiltrator {__instance.LabelShort} left map - no intel transmitted");
                    }
                    return;
                }

                // Get intelligence network
                var network = WorldComponent_IntelligenceNetwork.Instance;
                if (network == null)
                {
                    ModLog.Warning("IntelligenceNetwork component not found!");
                    return;
                }

                // Transmit intelligence
                network.TransmitIntelligence(__instance, hediff);
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in Pawn_DeSpawn_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Patch Pawn.Destroy to detect when infiltrators are removed from the game
        /// This handles cases where pawns are destroyed without going through DeSpawn
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
        [HarmonyPrefix]
        public static void Pawn_Destroy_Prefix(Pawn __instance, DestroyMode mode)
        {
            try
            {
                // Only transmit if pawn is leaving voluntarily (not killed)
                if (mode == DestroyMode.KillFinalize)
                    return;

                // Check if this pawn has a hidden identity
                if (__instance?.health?.hediffSet == null)
                    return;

                var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(
                    LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity;

                if (hediff == null || hediff.identityRevealed)
                    return;

                // Get intelligence network
                var network = WorldComponent_IntelligenceNetwork.Instance;
                if (network == null)
                    return;

                // Transmit intelligence (if not already transmitted in DeSpawn)
                var gatheredIntel = hediff.GetAllGatheredIntelligence();
                if (gatheredIntel.Count > 0 && !gatheredIntel[0].transmitted)
                {
                    network.TransmitIntelligence(__instance, hediff);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in Pawn_Destroy_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Patch TransportPodsArrivalAction_LandInSpecificCell to detect when pawns leave via transport pod
        /// This handles the "TransmitIntelNow" infiltrator reaction
        /// </summary>
        [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExitMap))]
        [HarmonyPrefix]
        public static void Pawn_ExitMap_Prefix(Pawn __instance, bool allowedToJoinOrCreateCaravan)
        {
            try
            {
                // Check if this pawn has a hidden identity
                if (__instance?.health?.hediffSet == null)
                    return;

                var hediff = __instance.health.hediffSet.GetFirstHediffOfDef(
                    LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity;

                if (hediff == null || hediff.identityRevealed)
                    return;

                // Get intelligence network
                var network = WorldComponent_IntelligenceNetwork.Instance;
                if (network == null)
                    return;

                // Transmit intelligence when exiting map
                network.TransmitIntelligence(__instance, hediff);

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Infiltrator {__instance.LabelShort} exiting map - transmitting intel");
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in Pawn_ExitMap_Prefix: {ex}");
            }
        }
    }
}
