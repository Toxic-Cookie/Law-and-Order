using RimWorld;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Defines an item as contraband with an associated silver penalty.
    /// </summary>
    public class ContrabandDefinition : IExposable
    {
        public ThingDef thingDef;
        public int silverPenaltyPerItem;

        public ContrabandDefinition()
        {
        }

        public ContrabandDefinition(ThingDef thingDef, int silverPenaltyPerItem)
        {
            this.thingDef = thingDef;
            this.silverPenaltyPerItem = silverPenaltyPerItem;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref silverPenaltyPerItem, "silverPenaltyPerItem", 0);
        }
    }
}
