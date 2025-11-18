using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Utils;
using LawAndOrder;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Harmony patches to modify raid behavior based on gathered intelligence
    /// </summary>
    [HarmonyPatch]
    public static class RaidModificationPatches
    {
        /// <summary>
        /// Patch IncidentWorker_RaidEnemy to modify raid strategy based on intelligence
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
        [HarmonyPrefix]
        public static void RaidEnemy_TryExecuteWorker_Prefix(IncidentWorker_RaidEnemy __instance, IncidentParms parms)
        {
            try
            {
                if (parms == null || parms.faction == null || parms.target == null)
                    return;

                // Only modify raids against player colonies
                if (parms.faction == Faction.OfPlayer)
                    return;

                // Get intelligence network
                var network = WorldComponent_IntelligenceNetwork.Instance;
                if (network == null)
                    return;

                // Get map
                Map targetMap = parms.target as Map;
                if (targetMap == null)
                    return;

                // Get intelligence for this faction and map
                List<IntelligenceData> intel = network.GetMapIntelligence(parms.faction, targetMap);

                if (intel == null || intel.Count == 0)
                    return; // No intelligence, raid proceeds normally

                // Modify raid based on available intelligence
                ModifyRaidBasedOnIntel(parms, intel, targetMap);
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in RaidEnemy_TryExecuteWorker_Prefix: {ex}");
            }
        }

        /// <summary>
        /// Modify raid parameters based on gathered intelligence
        /// </summary>
        private static void ModifyRaidBasedOnIntel(IncidentParms parms, List<IntelligenceData> intel, Map map)
        {
            if (parms == null || intel == null || intel.Count == 0)
                return;

            // Apply raid point modifier based on intelligence quality
            float pointModifier = IntelligenceUtils.GetRaidPointModifier(intel);
            parms.points *= pointModifier;

            if (Prefs.DevMode)
            {
                ModLog.Debug($"Raid points modified by {pointModifier:F2}x due to intelligence (new points: {parms.points:F0})");
            }

            // Check what intelligence categories we have
            bool hasDefenseIntel = intel.Any(i => i.category == IntelCategory.Defense && i.completeness > 0.3f);
            bool hasWealthIntel = intel.Any(i => i.category == IntelCategory.Wealth && i.completeness > 0.3f);
            bool hasScheduleIntel = intel.Any(i => i.category == IntelCategory.Schedule && i.completeness > 0.3f);
            bool hasLayoutIntel = intel.Any(i => i.category == IntelCategory.Layout && i.completeness > 0.3f);

            // Apply strategy modifications based on intelligence categories
            if (hasDefenseIntel)
            {
                ApplyDefenseIntelModifications(parms, intel, map);
            }

            if (hasWealthIntel)
            {
                ApplyWealthIntelModifications(parms, intel, map);
            }

            if (hasScheduleIntel)
            {
                ApplyScheduleIntelModifications(parms, intel, map);
            }

            if (hasLayoutIntel)
            {
                ApplyLayoutIntelModifications(parms, intel, map);
            }
        }

        /// <summary>
        /// Defense intel: Use sappers to bypass killboxes
        /// </summary>
        private static void ApplyDefenseIntelModifications(IncidentParms parms, List<IntelligenceData> intel, Map map)
        {
            var defenseIntel = intel.FirstOrDefault(i => i.category == IntelCategory.Defense);
            if (defenseIntel == null)
                return;

            // Check if colony has a killbox
            bool hasKillbox = defenseIntel.GetDetail<bool>("hasKillbox", false);

            if (hasKillbox && defenseIntel.completeness > 0.5f)
            {
                // Force sapper strategy to bypass killbox
                var sapperStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail("ImmediateAttackSappers");
                if (sapperStrategy != null)
                {
                    parms.raidStrategy = sapperStrategy;

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug("Defense intel: Using sappers to bypass killbox");
                    }

                    Messages.Message(
                        "LawAndOrder.RaidModified.Sappers".Translate(parms.faction.Name),
                        MessageTypeDefOf.ThreatBig
                    );
                }
            }

            // Scale raid size based on defensive strength
            float defenseStrength = defenseIntel.GetDetail<float>("defenseStrength", 0f);
            if (defenseStrength > 20f && defenseIntel.completeness > 0.6f)
            {
                // They know we're well-defended, send more raiders
                parms.points *= 1.2f;

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Defense intel: Increased raid size due to high defense strength ({defenseStrength:F0})");
                }
            }
        }

        /// <summary>
        /// Wealth intel: Use drop pod strikes on storage areas
        /// </summary>
        private static void ApplyWealthIntelModifications(IncidentParms parms, List<IntelligenceData> intel, Map map)
        {
            var wealthIntel = intel.FirstOrDefault(i => i.category == IntelCategory.Wealth);
            if (wealthIntel == null)
                return;

            bool hasValuables = wealthIntel.GetDetail<bool>("hasValuables", false);

            if (hasValuables && wealthIntel.completeness > 0.5f)
            {
                // Use drop pod raid to strike storage areas
                // 50% chance to force drop pod strategy
                if (Rand.Chance(0.5f))
                {
                    var smartStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail("ImmediateAttackSmart");
                    if (smartStrategy != null)
                    {
                        parms.raidStrategy = smartStrategy;
                        parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;

                        if (Prefs.DevMode)
                        {
                            ModLog.Debug("Wealth intel: Using drop pods to strike storage areas");
                        }

                        Messages.Message(
                            "LawAndOrder.RaidModified.DropPods".Translate(parms.faction.Name),
                            MessageTypeDefOf.ThreatBig
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Schedule intel: Night raids during low-guard periods
        /// </summary>
        private static void ApplyScheduleIntelModifications(IncidentParms parms, List<IntelligenceData> intel, Map map)
        {
            var scheduleIntel = intel.FirstOrDefault(i => i.category == IntelCategory.Schedule);
            if (scheduleIntel == null)
                return;

            bool vulnerableAtNight = scheduleIntel.GetDetail<bool>("vulnerableAtNight", false);

            if (vulnerableAtNight && scheduleIntel.completeness > 0.5f)
            {
                // Force raid to occur at night (if not already)
                // This is tricky to implement perfectly, but we can try to adjust timing
                int currentHour = GenLocalDate.HourOfDay(map);

                // If it's daytime, show a message that they're waiting for night
                if (currentHour >= 6 && currentHour < 20)
                {
                    if (Prefs.DevMode)
                    {
                        ModLog.Debug("Schedule intel: Raiders would prefer night attack, but raid is during day");
                    }
                }
                else
                {
                    // It's already night, increase raid effectiveness
                    parms.points *= 1.15f;

                    Messages.Message(
                        "LawAndOrder.RaidModified.NightRaid".Translate(parms.faction.Name),
                        MessageTypeDefOf.ThreatBig
                    );

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug("Schedule intel: Night raid with +15% effectiveness");
                    }
                }
            }
        }

        /// <summary>
        /// Layout intel: Multi-pronged coordinated assaults
        /// </summary>
        private static void ApplyLayoutIntelModifications(IncidentParms parms, List<IntelligenceData> intel, Map map)
        {
            var layoutIntel = intel.FirstOrDefault(i => i.category == IntelCategory.Layout);
            if (layoutIntel == null)
                return;

            int entryPoints = layoutIntel.GetDetail<int>("entryPoints", 0);

            if (entryPoints >= 2 && layoutIntel.completeness > 0.6f)
            {
                // Multi-pronged assault: Increase raid size and use edge drop
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                parms.points *= 1.1f;

                Messages.Message(
                    "LawAndOrder.RaidModified.MultiPronged".Translate(parms.faction.Name),
                    MessageTypeDefOf.ThreatBig
                );

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Layout intel: Multi-pronged assault through {entryPoints} entry points");
                }
            }
        }
    }

    /// <summary>
    /// Additional patches for more specific raid modifications
    /// </summary>
    [HarmonyPatch]
    public static class RaidStrategySelectionPatches
    {
        /// <summary>
        /// Patch to influence raid strategy selection based on intelligence
        /// </summary>
        [HarmonyPatch(typeof(RaidStrategyWorker), "MinimumPoints")]
        [HarmonyPostfix]
        public static void RaidStrategyWorker_MinimumPoints_Postfix(ref float __result, RaidStrategyWorker __instance, Faction faction, PawnGroupKindDef groupKind)
        {
            try
            {
                if (faction == null || faction == Faction.OfPlayer)
                    return;

                // Get intelligence network
                var network = WorldComponent_IntelligenceNetwork.Instance;
                if (network == null)
                    return;

                // Get intelligence for this faction
                List<IntelligenceData> intel = network.GetFactionIntelligence(faction);

                if (intel == null || intel.Count == 0)
                    return;

                // If faction has high-quality intelligence, reduce minimum points needed for advanced strategies
                // This makes sapper/drop pod raids more likely even at lower raid points
                float avgQuality = intel.Average(i => (i.completeness + i.accuracy) / 2f);

                if (avgQuality > 0.6f)
                {
                    __result *= 0.8f; // Reduce minimum points by 20%

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Intel allows {__instance.GetType().Name} at lower points ({__result:F0})");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in RaidStrategyWorker_MinimumPoints_Postfix: {ex}");
            }
        }
    }
}
