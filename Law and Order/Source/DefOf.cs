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
        public static HediffDef LawAndOrder_Debt;

        static LawAndOrder_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_HediffDefOf));
        }
    }

    /// <summary>
    /// DefOf class for Law and Order mod - Thoughts
    /// </summary>
    [DefOf]
    public static class LawAndOrder_ThoughtDefOf
    {
        public static ThoughtDef LawAndOrder_ImpressedByPlea;
        public static ThoughtDef LawAndOrder_HumiliatedInCourt;
        public static ThoughtDef LawAndOrder_PleaAccepted;
        public static ThoughtDef LawAndOrder_PleaImpressive;
        public static ThoughtDef LawAndOrder_PleaRejected;

        static LawAndOrder_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_ThoughtDefOf));
        }
    }

    /// <summary>
    /// DefOf class for Law and Order mod - Rituals
    /// </summary>
    [DefOf]
    public static class LawAndOrder_RitualDefOf
    {
        public static RitualBehaviorDef LawAndOrder_Hearing;
        public static PreceptDef LawAndOrder_Hearing_Precept;
        public static RitualPatternDef LawAndOrder_Hearing_Pattern;
        public static RitualOutcomeEffectDef LawAndOrder_HearingOutcome;

        static LawAndOrder_RitualDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_RitualDefOf));
        }
    }
}
