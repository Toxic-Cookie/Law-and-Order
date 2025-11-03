using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Colonists feel good when contraband enforcement aligns with their ideology.
    /// Rewards players for creating ideology-consistent contraband rules.
    /// </summary>
    public class ThoughtWorker_EnforcingBeliefs : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Only colonists have this thought
            if (!p.IsColonist)
                return ThoughtState.Inactive;

            // Need an ideology system (RimWorld 1.3+)
            Ideo ideo = p.Ideo;
            if (ideo == null)
                return ThoughtState.Inactive;

            // Get contraband manager
            var contrabandManager = WorldComponent_ContrabandManager.Instance;
            if (contrabandManager == null)
                return ThoughtState.Inactive;

            // Check if any contraband items align with ideology
            var contrabandDefinitions = contrabandManager.ContrabandDefinitions;
            if (contrabandDefinitions == null || contrabandDefinitions.Count == 0)
                return ThoughtState.Inactive;

            // Look for at least one aligned item
            foreach (var contrabandDef in contrabandDefinitions)
            {
                if (IdeologyContrabandMapper.ItemAlignsWithIdeology(contrabandDef.thingDef, ideo))
                {
                    // Found at least one item that aligns with ideology
                    return ThoughtState.ActiveAtStage(0);
                }
            }

            return ThoughtState.Inactive;
        }
    }
}
