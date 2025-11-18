using HarmonyLib;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Patches to automatically reveal infiltrator identity when attacked
    /// </summary>
    [HarmonyPatch]
    public static class InfiltratorRevealPatches
    {
        /// <summary>
        /// Reveal infiltrator identity when they take damage from player pawns
        /// </summary>
        [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
        [HarmonyPostfix]
        public static void Pawn_PostApplyDamage_Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            try
            {
                if (__instance == null || totalDamageDealt <= 0f)
                    return;

                // Check if this pawn has a hidden identity
                var comp = __instance.GetComp<CompHiddenIdentity>();
                if (comp == null || comp.Hediff == null)
                    return;

                // Already revealed?
                if (comp.Hediff.identityRevealed)
                    return;

                // Check if damage came from player faction
                Pawn instigator = dinfo.Instigator as Pawn;
                if (instigator != null && instigator.Faction == Faction.OfPlayer)
                {
                    // Reveal identity when attacked by player
                    comp.Hediff.RevealIdentity();

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Infiltrator {__instance.LabelShort} revealed after taking damage from {instigator.LabelShort}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in Pawn_PostApplyDamage_Postfix: {ex}");
            }
        }
    }
}
