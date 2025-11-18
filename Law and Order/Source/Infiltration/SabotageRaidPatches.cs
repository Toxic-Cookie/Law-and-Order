using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Harmony patches to trigger accomplice sabotage during raids
    /// </summary>
    [HarmonyPatch]
    public static class SabotageRaidPatches
    {
        /// <summary>
        /// Patch IncidentWorker_RaidEnemy.TryExecuteWorker to trigger sabotage when raids start
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
        [HarmonyPostfix]
        public static void TriggerSabotageOnRaid(IncidentWorker_RaidEnemy __instance, IncidentParms parms, bool __result)
        {
            // Only proceed if the raid was successful
            if (!__result)
                return;

            Map map = (Map)parms.target;
            if (map == null)
                return;

            ModLog.Debug($"Raid detected on map {map.uniqueID}, checking for accomplice sabotage");

            // Trigger sabotage from all active accomplices
            SabotageExecutor.AssignSabotageForRaid(map);
        }
    }

    /// <summary>
    /// Patch to clean up accomplice comps when pawns are destroyed
    /// </summary>
    [HarmonyPatch]
    public static class AccompliceCleanupPatches
    {
        /// <summary>
        /// When a pawn is destroyed, clean up accomplice references
        /// </summary>
        [HarmonyPatch(typeof(Pawn), "Destroy")]
        [HarmonyPrefix]
        public static void CleanupAccompliceReferences(Pawn __instance)
        {
            if (__instance == null)
                return;

            var comp = __instance.TryGetComp<CompAccomplice>();
            if (comp != null && comp.IsRecruited)
            {
                ModLog.Debug($"Cleaning up accomplice {__instance.LabelShort} (destroyed)");
                comp.ClearPendingSabotage();
            }
        }

        /// <summary>
        /// When an infiltrator dies, notify their accomplices
        /// </summary>
        [HarmonyPatch(typeof(Pawn), "Kill")]
        [HarmonyPostfix]
        public static void NotifyAccomplicesOfRecruiterDeath(Pawn __instance)
        {
            if (__instance == null || __instance.Map == null)
                return;

            // Check if this was an infiltrator with accomplices
            var infiltratorComp = __instance.TryGetComp<CompHiddenIdentity>();
            if (infiltratorComp == null || !infiltratorComp.HasHiddenIdentity)
                return;

            // Find all accomplices recruited by this infiltrator
            var accomplices = AccompliceUtils.GetAccomplicesForInfiltrator(__instance);
            if (accomplices.Count == 0)
                return;

            ModLog.Info($"Infiltrator {__instance.LabelShort} died with {accomplices.Count} accomplices");

            // Accomplices are freed when their recruiter dies
            foreach (var accomplice in accomplices)
            {
                var comp = accomplice.TryGetComp<CompAccomplice>();
                if (comp != null)
                {
                    // Clear recruitment but don't trigger discovery
                    // They're just quietly freed
                    comp.ClearPendingSabotage();

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Accomplice {accomplice.LabelShort} freed (recruiter died)");
                    }
                }
            }
        }
    }
}
