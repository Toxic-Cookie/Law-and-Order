using RimWorld;
using Verse;

namespace Law_and_Order.Source.Buildings
{
    /// <summary>
    /// Component properties for marking a building as a Judge's Bench.
    /// Can be attached to any table to allow it to be used in court hearings.
    /// </summary>
    public class CompProperties_JudgesBench : CompProperties
    {
        public CompProperties_JudgesBench()
        {
            this.compClass = typeof(Comp_JudgesBench);
        }
    }
}
