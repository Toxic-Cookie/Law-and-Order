using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Harmony patches for hidden identity system
    ///
    /// NOTE: Name concealment is now handled by directly setting pawn.Name to the fake name,
    /// using RimWorld's native rename system (like in god mode). This eliminates the need
    /// for extensive patching of Label methods.
    ///
    /// We only patch:
    /// 1. Gene display (to hide sanguophage gene)
    /// 2. Backstory display (to show fake backstory)
    /// </summary>
    [HarmonyPatch]
    public static class HiddenIdentityPatches
    {
        /// <summary>
        /// Helper method to get the hidden identity hediff from a pawn
        /// </summary>
        private static Hediff_HiddenIdentity GetHiddenIdentity(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
                return null;

            return pawn.health.hediffSet.GetFirstHediffOfDef(
                LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity;
        }

        /// <summary>
        /// Patch: Hide sanguophage gene from inspection panel
        /// Patches Pawn_GeneTracker.GenesListForReading getter
        /// </summary>
        [HarmonyPatch(typeof(Pawn_GeneTracker), "GenesListForReading", MethodType.Getter)]
        [HarmonyPostfix]
        public static void HideSanguophageGene_Postfix(Pawn_GeneTracker __instance, ref System.Collections.Generic.List<Gene> __result, Pawn ___pawn)
        {
            if (!ModsConfig.BiotechActive)
                return;

            if (___pawn == null || __result == null)
                return;

            var hediff = GetHiddenIdentity(___pawn);
            if (hediff == null || hediff.identityRevealed)
                return;

            // If masking sanguophage status, remove bloodfeeder gene from display
            if (hediff.maskSanguophageStatus)
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
        /// Patch: Show fake backstory description in tooltip
        /// Patches BackstoryDef.FullDescriptionFor to return fake backstory text
        /// </summary>
        [HarmonyPatch(typeof(BackstoryDef), nameof(BackstoryDef.FullDescriptionFor))]
        [HarmonyPostfix]
        public static void FakeBackstoryDescription_Postfix(BackstoryDef __instance, Pawn p, ref TaggedString __result)
        {
            if (p == null)
                return;

            var hediff = GetHiddenIdentity(p);
            if (hediff == null || hediff.identityRevealed)
                return;

            // Replace the backstory description with our fake one
            string fakeBackstory = hediff.fakeBackstory;
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
