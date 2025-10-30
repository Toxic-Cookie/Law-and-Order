using RimWorld;
using Verse;

namespace Law_and_Order.Source
{
    /// <summary>
    /// DefOf class for Law and Order mod
    /// RimWorld automatically initializes these fields at startup
    /// </summary>
    [DefOf]
    public static class LawAndOrder_HediffDefOf
    {
        public static HediffDef LawAndOrder_CriminalRecord;
        public static HediffDef LawAndOrder_Debt;

        static LawAndOrder_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_HediffDefOf));
        }
    }
}
