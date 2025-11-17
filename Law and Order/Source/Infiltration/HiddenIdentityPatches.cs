using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Harmony patches to mask genes, names, and backstories for pawns with hidden identities
    /// </summary>
    [HarmonyPatch]
    public static class HiddenIdentityPatches
    {
        /// <summary>
        /// Patch: Hide sanguophage gene from inspection panel
        /// Patches Pawn_GeneTracker.GenesListForReading getter
        /// </summary>
        [HarmonyPatch(typeof(Pawn_GeneTracker), "GenesListForReading", MethodType.Getter)]
        [HarmonyPostfix]
        public static void HideSanguophageGene_Postfix(Pawn_GeneTracker __instance, ref List<Gene> __result, Pawn ___pawn)
        {
            if (!ModsConfig.BiotechActive)
                return;

            if (___pawn == null || __result == null)
                return;

            var comp = ___pawn.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // If masking sanguophage status, remove bloodfeeder gene from display
            if (comp.Identity.maskSanguophageStatus)
            {
                var originalCount = __result.Count;
                __result = __result.Where(g => g.def != GeneDefOf.Bloodfeeder).ToList();

                if (Prefs.DevMode && __result.Count < originalCount)
                {
                    ModLog.Debug($"Masked bloodfeeder gene for {___pawn.LabelShort}");
                }
            }
        }

        /// <summary>
        /// Patch: Show fake name in pawn LabelShort (most commonly used label)
        /// Patches Pawn.LabelShort property getter
        /// </summary>
        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("LabelShort", MethodType.Getter)]
        [HarmonyPostfix]
        public static void UseFakeName_LabelShort_Postfix(Pawn __instance, ref string __result)
        {
            if (__instance == null || __result == null)
                return;

            var comp = __instance.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // Use display name instead of real name
            string displayName = comp.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                __result = displayName;

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Showing fake name '{displayName}' for {comp.Identity.realName}");
                }
            }
        }

        /// <summary>
        /// Patch: Show fake name in pawn LabelNoCount
        /// Patches Pawn.LabelNoCount property getter
        /// This is used in many UI contexts
        /// </summary>
        [HarmonyPatch(typeof(Pawn))]
        [HarmonyPatch("LabelNoCount", MethodType.Getter)]
        [HarmonyPostfix]
        public static void UseFakeName_LabelNoCount_Postfix(Pawn __instance, ref string __result)
        {
            if (__instance == null || __result == null)
                return;

            var comp = __instance.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // Use display name instead of real name
            string displayName = comp.GetDisplayName();
            if (!string.IsNullOrEmpty(displayName))
            {
                __result = displayName;
            }
        }

        /// <summary>
        /// Patch: Intercept backstory retrieval to return fake backstory
        /// Patches Pawn_StoryTracker.GetBackstory method
        /// Creates a temporary BackstoryDef with fake text on the fly
        /// </summary>
        [HarmonyPatch(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.GetBackstory))]
        [HarmonyPostfix]
        public static void GetFakeBackstory_Postfix(Pawn_StoryTracker __instance, BackstorySlot slot, ref BackstoryDef __result, Pawn ___pawn)
        {
            if (___pawn == null || __result == null)
                return;

            var comp = ___pawn.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // Create a fake BackstoryDef with our fabricated backstory text
            // We modify the result to show our fake description
            // Note: This is a bit of a hack but necessary to show fake backstory in the UI

            string fakeBackstory = comp.Identity.GetDisplayBackstory();
            if (!string.IsNullOrEmpty(fakeBackstory))
            {
                // Create a modified copy of the backstory with our fake description
                // We can't easily create a new BackstoryDef, so we'll use the CharacterCardUtility hook instead
                // This method is called when displaying backstories, but the actual text comes from
                // BackstoryDef.TitleCapFor and BackstoryDef.FullDescriptionFor

                // For now, we'll skip modifying the BackstoryDef and instead patch the display methods
                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Backstory retrieval for {___pawn.LabelShort}: {__result?.defName ?? "null"}");
                }
            }
        }

        /// <summary>
        /// Patch: Show fake backstory description in tooltip
        /// Patches BackstoryDef.FullDescriptionFor to return fake backstory text
        /// </summary>
        [HarmonyPatch(typeof(BackstoryDef), nameof(BackstoryDef.FullDescriptionFor))]
        [HarmonyPostfix]
        public static void FakeBackstoryDescription_Postfix(BackstoryDef __instance, Pawn p, ref TaggedString __result)
        {
            if (p == null)
                return;

            var comp = p.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity || comp.Identity.identityRevealed)
                return;

            // Replace the backstory description with our fake one
            string fakeBackstory = comp.Identity.GetDisplayBackstory();
            if (!string.IsNullOrEmpty(fakeBackstory))
            {
                __result = fakeBackstory;

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Showing fake backstory description for {p.LabelShort}");
                }
            }
        }

        // Note: We cannot patch BackstoryDef.TitleCapFor because it doesn't have access to the pawn
        // The backstory title will show the real backstory name, but the description will be fake
        // This is acceptable since the description is more important for hiding identity
    }
}
