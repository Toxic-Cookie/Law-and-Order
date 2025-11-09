using System;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual outcome component that provides a quality bonus based on the number of pawns assigned to a specific role.
    /// </summary>
    public class RitualOutcomeComp_RoleCount : RitualOutcomeComp_Quality
    {
        /// <summary>
        /// The role ID to count (e.g., "jury", "victim", "judge")
        /// </summary>
        public string roleId;

        public override bool DataRequired
        {
            get { return false; }
        }

        public override bool Applies(LordJob_Ritual ritual)
        {
            return !string.IsNullOrEmpty(roleId);
        }

        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            if (ritual == null || ritual.assignments == null)
                return 0f;

            // Count pawns assigned to this specific role
            int count = ritual.assignments.Participants.Count(p =>
            {
                var role = ritual.assignments.RoleForPawn(p, includeForced: true);
                return role != null && role.id == roleId;
            });

            return (float)count;
        }

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            // Count pawns assigned to this role
            int count = assignments.Participants.Count(p =>
            {
                var role = assignments.RoleForPawn(p, includeForced: true);
                return role != null && role.id == roleId;
            });

            float quality = (curve != null) ? curve.Evaluate((float)count) : 0f;
            int maxCount = (curve != null) ? (int)MaxValue : 0;

            return new QualityFactor
            {
                label = label.CapitalizeFirst(),
                count = count.ToString() + " / " + Math.Max(maxCount, count).ToString(),
                qualityChange = ExpectedOffsetDesc(true, quality),
                quality = quality,
                positive = true,
                priority = 4f
            };
        }
    }
}
