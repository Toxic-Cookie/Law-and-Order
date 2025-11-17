using RimWorld;
using Verse;
using Law_and_Order.Source.Justice;

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

    /// <summary>
    /// DefOf class for Law and Order mod - Punishments
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    [DefOf]
    public static class LawAndOrder_PunishmentDefOf
    {
        public static PunishmentDef LawAndOrder_Punishment_Imprisonment;
        public static PunishmentDef LawAndOrder_Punishment_Fine;
        public static PunishmentDef LawAndOrder_Punishment_Beating;
        public static PunishmentDef LawAndOrder_Punishment_Execution;
        public static PunishmentDef LawAndOrder_Punishment_Exile;

        static LawAndOrder_PunishmentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_PunishmentDefOf));
        }
    }

    /// <summary>
    /// DefOf class for Law and Order mod - Things
    /// Phase 6: Evidence & Clues
    /// </summary>
    [DefOf]
    public static class ThingDefOf
    {
        public static ThingDef CrimeSceneClue;
        public static ThingDef InterrogationTable;
        public static ThingDef InterrogationChair;

        static ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
        }
    }

    /// <summary>
    /// DefOf class for Law and Order mod - Main Tabs
    /// Phase 3: UI
    /// </summary>
    [DefOf]
    public static class MainTabDefOf
    {
        public static MainButtonDef Justice;

        static MainTabDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MainTabDefOf));
        }
    }
}
