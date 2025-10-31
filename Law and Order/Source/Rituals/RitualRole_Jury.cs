using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Ritual role for jury members (optional).
    /// Allows any colonist not already participating in the hearing.
    /// </summary>
    public class RitualRole_Jury : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!base.AppliesIfChild(p, out reason, skipReason))
            {
                return false;
            }

            // Must be a colonist
            if (!p.Faction.IsPlayerSafe())
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeColonist".Translate(base.Label);
                }
                return false;
            }

            // Must be humanlike
            if (!p.RaceProps.Humanlike)
            {
                if (!skipReason)
                {
                    reason = "MessageRitualRoleMustBeHumanlike".Translate(base.LabelCap);
                }
                return false;
            }

            // Must not be incapacitated
            if (p.Downed || p.Dead || p.InMentalState)
            {
                if (!skipReason)
                {
                    reason = "PawnUnavailable".Translate(p);
                }
                return false;
            }

            return true;
        }

        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn p = null, bool skipReason = false)
        {
            reason = null;
            return false;
        }
    }
}
