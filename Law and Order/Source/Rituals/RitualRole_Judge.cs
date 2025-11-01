using RimWorld;
using Verse;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual role for the judge presiding over a hearing.
    /// Requires a colonist with social skill.
    /// </summary>
    public class RitualRole_Judge : RitualRole
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

        protected override int PawnDesirability(Pawn pawn)
        {
            // Prefer pawns with higher social skill
            return pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
        }
    }
}
