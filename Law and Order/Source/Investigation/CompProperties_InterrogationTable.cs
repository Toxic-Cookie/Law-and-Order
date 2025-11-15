using RimWorld;
using Verse;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Properties for the InterrogationTable ThingComp.
    /// Allows tables to be designated as interrogation tables.
    /// </summary>
    public class CompProperties_InterrogationTable : CompProperties
    {
        public CompProperties_InterrogationTable()
        {
            compClass = typeof(Comp_InterrogationTable);
        }
    }
}
