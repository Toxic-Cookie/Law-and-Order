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
        /// Reveal infiltrator identity and skip goodwill penalty for cover faction
        /// Returns false to prevent original method from running if infiltrator is revealed
        /// </summary>
        [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
        [HarmonyPrefix]
        public static bool Faction_Notify_MemberTookDamage_Prefix(Pawn member, DamageInfo dinfo)
        {
            try
            {
                if (member == null || dinfo.Instigator == null)
                    return true; // Run original method

                // Only care if damage came from player
                if (dinfo.Instigator.Faction != Faction.OfPlayer)
                    return true; // Run original method

                // Check if this pawn has a hidden identity
                var comp = member.GetComp<CompHiddenIdentity>();
                if (comp == null || comp.Hediff == null)
                    return true; // Run original method (not an infiltrator)

                // Already revealed?
                if (comp.Hediff.identityRevealed)
                    return true; // Run original method (already revealed, let normal hostile faction relations apply)

                // Reveal identity and skip the goodwill penalty for the cover faction
                comp.Hediff.RevealIdentity();

                if (Prefs.DevMode)
                {
                    ModLog.Debug($"Infiltrator {member.LabelShort} revealed, skipping goodwill penalty for cover faction");
                }

                // Return false to skip the original Notify_MemberTookDamage method
                // This prevents the friendly cover faction from losing goodwill
                return false;
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in Faction_Notify_MemberTookDamage_Prefix: {ex}");
                return true; // Run original method on error
            }
        }
    }
}
