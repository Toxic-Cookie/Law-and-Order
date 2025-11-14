using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Justice
{
    /// <summary>
    /// Represents an active criminal case for a suspected criminal.
    /// Cases are created when crimes transition from Hidden to Suspected.
    /// Player can investigate, convict, or dismiss cases through the Justice UI.
    /// </summary>
    public class CriminalCase : IExposable
    {
        // Core case data
        public int caseId;
        public Pawn accused;
        public CaseStatus status = CaseStatus.Open;
        public List<int> crimeTicksCommitted = new List<int>(); // Track crime ticks for identification

        // Case metadata
        public int tickOpened; // When the case was created
        public int tickClosed = -1; // When the case was closed (-1 = still open)

        // Investigation notes (optional, for future expansion)
        public string investigationNotes = "";

        public CriminalCase()
        {
            // Required for IExposable
        }

        public CriminalCase(int id, Pawn accused)
        {
            this.caseId = id;
            this.accused = accused;
            this.tickOpened = Find.TickManager.TicksGame;
            this.status = CaseStatus.Open;
        }

        /// <summary>
        /// Add a crime to this case.
        /// </summary>
        public void AddCrime(Crime crime)
        {
            if (!crimeTicksCommitted.Contains(crime.tickCommitted))
            {
                crimeTicksCommitted.Add(crime.tickCommitted);
                crime.caseId = this.caseId;

                Mod.Log?.Message($"Added crime {crime.crimeType} to case #{caseId}");
            }
        }

        /// <summary>
        /// Get all crimes associated with this case from the accused's criminal record.
        /// </summary>
        public List<Crime> GetAssociatedCrimes()
        {
            var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(accused);
            if (criminalRecord == null)
                return new List<Crime>();

            return criminalRecord.Crimes.Where(c => c.caseId == this.caseId).ToList();
        }

        /// <summary>
        /// Convict this case - transition all associated crimes to Convicted state.
        /// </summary>
        public void Convict()
        {
            if (status != CaseStatus.Open)
                return;

            status = CaseStatus.Convicted;
            tickClosed = Find.TickManager.TicksGame;

            // Transition all crimes to Convicted
            var crimes = GetAssociatedCrimes();
            foreach (var crime in crimes)
            {
                crime.TransitionToConvicted();
            }

            Mod.Log?.Message($"Case #{caseId} convicted - {crimes.Count} crimes");
        }

        /// <summary>
        /// Dismiss this case - no punishment assigned.
        /// Crimes remain Suspected but case is closed.
        /// </summary>
        public void Dismiss()
        {
            if (status != CaseStatus.Open)
                return;

            status = CaseStatus.Dismissed;
            tickClosed = Find.TickManager.TicksGame;

            Mod.Log?.Message($"Case #{caseId} dismissed");
        }

        /// <summary>
        /// Get the number of days this case has been open.
        /// </summary>
        public int DaysOpen
        {
            get
            {
                int endTick = tickClosed > 0 ? tickClosed : Find.TickManager.TicksGame;
                return (endTick - tickOpened) / GenDate.TicksPerDay;
            }
        }

        /// <summary>
        /// Get the most serious crime type in this case.
        /// </summary>
        public CrimeType GetMostSeriousCrime()
        {
            var crimes = GetAssociatedCrimes();
            if (crimes.Count == 0)
                return CrimeType.Unknown;

            // Order by severity (Murder > Assault > PropertyDestruction > etc.)
            var ordered = crimes.OrderByDescending(c => GetCrimeSeverity(c.crimeType));
            return ordered.First().crimeType;
        }

        /// <summary>
        /// Get numeric severity for crime type ordering.
        /// </summary>
        private int GetCrimeSeverity(CrimeType type)
        {
            return type switch
            {
                CrimeType.Murder => 100,
                CrimeType.Kidnapping => 90,
                CrimeType.Assault => 80,
                CrimeType.AnimalAbuse => 70,
                CrimeType.Arson => 60,
                CrimeType.PropertyDestruction => 50,
                CrimeType.Theft => 40,
                CrimeType.Vandalism => 30,
                CrimeType.Trespassing => 20,
                CrimeType.ContrabandPossession => 10,
                _ => 0
            };
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref caseId, "caseId", 0);
            Scribe_References.Look(ref accused, "accused");
            Scribe_Values.Look(ref status, "status", CaseStatus.Open);
            Scribe_Collections.Look(ref crimeTicksCommitted, "crimeTicksCommitted", LookMode.Value);
            Scribe_Values.Look(ref tickOpened, "tickOpened", 0);
            Scribe_Values.Look(ref tickClosed, "tickClosed", -1);
            Scribe_Values.Look(ref investigationNotes, "investigationNotes", "");

            // Ensure list is initialized after load
            if (Scribe.mode == LoadSaveMode.PostLoadInit && crimeTicksCommitted == null)
            {
                crimeTicksCommitted = new List<int>();
            }
        }

        public override string ToString()
        {
            return $"Case #{caseId}: {accused?.NameShortColored ?? "Unknown"} - {status} ({GetAssociatedCrimes().Count} crimes)";
        }
    }
}
