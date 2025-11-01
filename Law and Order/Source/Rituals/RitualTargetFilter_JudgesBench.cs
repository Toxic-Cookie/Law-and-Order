using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Law_and_Order.Source.Buildings;

namespace Law_and_Order.Source.Rituals
{
    /// <summary>
    /// Ritual target filter that finds tables designated as Judge's Bench.
    /// Used by the court hearing ritual to find valid target locations.
    /// </summary>
    public class RitualTargetFilter_JudgesBench : RitualTargetFilter
    {
        public RitualTargetFilter_JudgesBench()
        {
        }

        public RitualTargetFilter_JudgesBench(RitualTargetFilterDef def) : base(def)
        {
        }

        public override bool CanStart(TargetInfo initiator, TargetInfo selectedTarget, out string rejectionReason)
        {
            TargetInfo bestTarget = BestTarget(initiator, selectedTarget);
            rejectionReason = "";

            if (!bestTarget.IsValid)
            {
                rejectionReason = "LawAndOrder_MustTargetJudgesBench".Translate();
                return false;
            }

            return true;
        }

        public override TargetInfo BestTarget(TargetInfo initiator, TargetInfo selectedTarget)
        {
            Pawn pawn = initiator.Thing as Pawn;
            if (pawn == null || pawn.Map == null)
            {
                return TargetInfo.Invalid;
            }

            // If selectedTarget is already valid, use it
            if (selectedTarget.HasThing && selectedTarget.Thing != null)
            {
                Comp_JudgesBench comp = selectedTarget.Thing.TryGetComp<Comp_JudgesBench>();
                if (comp != null && comp.IsDesignatedAsJudgesBench)
                {
                    return selectedTarget;
                }
            }

            // Otherwise, find the closest Judge's Bench
            Thing closestBench = null;
            float closestDistance = 99999f;

            // Search all buildings on the map
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                Comp_JudgesBench comp = building.TryGetComp<Comp_JudgesBench>();
                if (comp != null && comp.IsDesignatedAsJudgesBench)
                {
                    if (pawn.CanReach(building, PathEndMode.Touch, pawn.NormalMaxDanger(), false, false, TraverseMode.ByPawn))
                    {
                        int distanceSq = (pawn.Position - building.Position).LengthHorizontalSquared;
                        if (distanceSq < closestDistance)
                        {
                            closestBench = building;
                            closestDistance = distanceSq;
                        }
                    }
                }
            }

            if (closestBench != null)
            {
                return new TargetInfo(closestBench);
            }

            return TargetInfo.Invalid;
        }

        public override IEnumerable<string> GetTargetInfos(TargetInfo initiator)
        {
            yield return "LawAndOrder_RequiresJudgesBench".Translate();
        }
    }
}
