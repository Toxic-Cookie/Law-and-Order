using RimWorld;
using Verse;

namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Static references to custom HediffDefs
    /// </summary>
    [DefOf]
    public static class HediffDefOf
    {
        public static HediffDef LawAndOrder_CriminalRecord;

        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }
    }
}
