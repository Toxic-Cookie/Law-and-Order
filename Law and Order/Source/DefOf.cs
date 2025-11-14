using RimWorld;
using Verse;

namespace Law_and_Order.Source
{
    /// <summary>
    /// DefOf class for Law and Order mod - Hediffs
    /// RimWorld automatically initializes these fields at startup
    /// </summary>
    [DefOf]
    public static class LawAndOrder_HediffDefOf
    {
        public static HediffDef LawAndOrder_CriminalRecord;

        static LawAndOrder_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_HediffDefOf));
        }
    }

    // TODO Phase 2+: Add new DefOf classes for state-based system
    // - CaseStatusDef (if we create custom defs for case statuses)
    // - PunishmentDef (for Phase 4 punishment system)
}
