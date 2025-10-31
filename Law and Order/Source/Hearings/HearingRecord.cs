using Verse;
using RimWorld;

namespace Law_and_Order.Source.Hearings
{
    /// <summary>
    /// Represents the outcome of a plea bargain attempt
    /// </summary>
    public enum PleaBargainOutcome
    {
        NotAttempted,       // Prisoner has not tried to plea bargain
        CriticalSuccess,    // Roll 96-100: -25% debt
        Success,            // Roll within success range: -10% debt
        Failure,            // Roll within failure range: No change
        CriticalFailure     // Roll 1-5: +15% debt (Contempt of Court)
    }

    /// <summary>
    /// Represents the status of a hearing
    /// </summary>
    public enum HearingStatus
    {
        NotScheduled,       // No hearing has been scheduled
        Scheduled,          // Hearing is scheduled but not yet conducted
        InProgress,         // Hearing is currently being conducted
        Completed,          // Hearing has been completed
        Cancelled           // Hearing was cancelled
    }

    /// <summary>
    /// Tracks hearing information for a criminal
    /// </summary>
    public class HearingRecord : IExposable
    {
        public HearingStatus status = HearingStatus.NotScheduled;
        public int scheduledTick = 0;
        public int completedTick = 0;
        public Pawn adjudicator;           // The colonist acting as judge/warden
        // Note: Room is not saved/loaded since it's not ILoadReferenceable
        // It's just used transiently during hearings
        public Room courtroom;             // The room where hearing was/will be conducted
        public PleaBargainOutcome pleaBargainOutcome = PleaBargainOutcome.NotAttempted;
        public int pleaBargainRoll = 0;    // The actual d100 roll
        public float debtBeforePlea = 0f;  // Debt amount before plea bargain
        public float debtAfterPlea = 0f;   // Debt amount after plea bargain
        public bool chargesRead = false;   // Has the adjudicator read the charges?
        public bool debtCalculated = false; // Has the debt been formally calculated?
        public bool sentenced = false;     // Has the sentence been delivered?
        public string sentenceNotes = "";  // Notes about the sentence

        public HearingRecord()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref status, "status", HearingStatus.NotScheduled);
            Scribe_Values.Look(ref scheduledTick, "scheduledTick", 0);
            Scribe_Values.Look(ref completedTick, "completedTick", 0);
            Scribe_References.Look(ref adjudicator, "adjudicator");
            // Don't save/load courtroom since Room is not ILoadReferenceable
            Scribe_Values.Look(ref pleaBargainOutcome, "pleaBargainOutcome", PleaBargainOutcome.NotAttempted);
            Scribe_Values.Look(ref pleaBargainRoll, "pleaBargainRoll", 0);
            Scribe_Values.Look(ref debtBeforePlea, "debtBeforePlea", 0f);
            Scribe_Values.Look(ref debtAfterPlea, "debtAfterPlea", 0f);
            Scribe_Values.Look(ref chargesRead, "chargesRead", false);
            Scribe_Values.Look(ref debtCalculated, "debtCalculated", false);
            Scribe_Values.Look(ref sentenced, "sentenced", false);
            Scribe_Values.Look(ref sentenceNotes, "sentenceNotes", "");
        }

        public bool CanAttemptPleaBargain => pleaBargainOutcome == PleaBargainOutcome.NotAttempted;

        public int DaysSinceScheduled => (Find.TickManager.TicksGame - scheduledTick) / GenDate.TicksPerDay;
        public int DaysSinceCompleted => (Find.TickManager.TicksGame - completedTick) / GenDate.TicksPerDay;

        public override string ToString()
        {
            if (status == HearingStatus.NotScheduled)
            {
                return "No hearing scheduled";
            }

            if (status == HearingStatus.Completed)
            {
                string result = $"Hearing completed {DaysSinceCompleted} days ago";
                if (adjudicator != null)
                {
                    result += $" by {adjudicator.LabelShort}";
                }
                return result;
            }

            if (status == HearingStatus.Scheduled)
            {
                return $"Hearing scheduled ({DaysSinceScheduled} days ago)";
            }

            return status.ToString();
        }
    }
}
