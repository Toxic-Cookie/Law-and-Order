using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual role for the defendant (prisoner being tried).
    /// Requires the specific prisoner with pending crimes.
    /// </summary>
    public class RitualRole_Defendant : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (!base.AppliesIfChild(p, out reason, skipReason))
            {
                return false;
            }

            // Must be a prisoner
            if (!p.IsPrisonerOfColony)
            {
                if (!skipReason)
                {
                    reason = "LawAndOrder_RoleMustBePrisoner".Translate(base.LabelCap);
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

            // Must have crimes on record
            var crimeRecord = CrimeUtils.TryGetCriminalRecord(p);
            if (crimeRecord == null || crimeRecord.TotalCrimeCount == 0)
            {
                if (!skipReason)
                {
                    reason = "LawAndOrder_RoleNoCrimes".Translate(p);
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
