using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hearings;

namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Represents a single crime committed by a pawn
    /// </summary>
    public class Crime : IExposable
    {
        public CrimeType crimeType;
        public int tickCommitted;
        public Pawn victim;
        public Thing targetThing;
        public float damageDealt;
        public string additionalInfo;
        public bool wasVictimDowned; // Was the victim downed by this crime?
        public bool wasVictimKilled; // Was the victim killed by this crime?
        public float debtAmount; // Actual debt incurred by this crime (stored at time of crime)
        public string damageType; // Type of damage dealt (e.g., "Gunshot", "Stab", "Burn")
        public PenaltyBreakdown penaltyBreakdown; // Detailed breakdown of how the penalty was calculated

        public Crime()
        {
        }

        public Crime(CrimeType type, Pawn victim = null, Thing targetThing = null, float damageDealt = 0f, string additionalInfo = null, bool wasVictimDowned = false, bool wasVictimKilled = false, float debtAmount = 0f, string damageType = null, PenaltyBreakdown penaltyBreakdown = null)
        {
            this.crimeType = type;
            this.tickCommitted = Find.TickManager.TicksGame;
            this.victim = victim;
            this.targetThing = targetThing;
            this.damageDealt = damageDealt;
            this.additionalInfo = additionalInfo;
            this.wasVictimDowned = wasVictimDowned;
            this.wasVictimKilled = wasVictimKilled;
            this.debtAmount = debtAmount;
            this.damageType = damageType;
            this.penaltyBreakdown = penaltyBreakdown;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref crimeType, "crimeType", CrimeType.Unknown);
            Scribe_Values.Look(ref tickCommitted, "tickCommitted", 0);
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_Values.Look(ref damageDealt, "damageDealt", 0f);
            Scribe_Values.Look(ref additionalInfo, "additionalInfo");
            Scribe_Values.Look(ref wasVictimDowned, "wasVictimDowned", false);
            Scribe_Values.Look(ref wasVictimKilled, "wasVictimKilled", false);
            Scribe_Values.Look(ref debtAmount, "debtAmount", 0f);
            Scribe_Values.Look(ref damageType, "damageType");
            Scribe_Deep.Look(ref penaltyBreakdown, "penaltyBreakdown");
        }

        public int DaysAgo => (Find.TickManager.TicksGame - tickCommitted) / GenDate.TicksPerDay;

        /// <summary>
        /// Get a descriptive label for this crime based on all available information
        /// </summary>
        public string GetCrimeLabel()
        {
            // For assault crimes, show the specific damage type if available
            if (crimeType == CrimeType.Assault)
            {
                if (wasVictimKilled)
                {
                    return "LawAndOrder_Crime_Murder".Translate();
                }
                if (!string.IsNullOrEmpty(damageType))
                {
                    // Try to find a translation key for the damage type
                    string translationKey = $"LawAndOrder_Crime_{damageType.Replace(" ", "")}";
                    if (translationKey.CanTranslate())
                    {
                        return translationKey.Translate();
                    }
                    // Fall back to the damage type directly
                    return damageType;
                }
                if (wasVictimDowned)
                {
                    return "LawAndOrder_Crime_Assault_Downed".Translate();
                }
                return "LawAndOrder_Crime_Assault".Translate();
            }

            // For murder, add context
            if (crimeType == CrimeType.Murder)
            {
                if (!string.IsNullOrEmpty(damageType))
                {
                    return "LawAndOrder_Crime_Murder_Via".Translate(damageType);
                }
                return "LawAndOrder_Crime_Murder".Translate();
            }

            // For property crimes, show what was damaged
            if (crimeType == CrimeType.PropertyDestruction && targetThing != null)
            {
                return "LawAndOrder_Crime_PropertyDestruction_Target".Translate(targetThing.Label);
            }

            if (crimeType == CrimeType.Vandalism && targetThing != null)
            {
                return "LawAndOrder_Crime_Vandalism_Target".Translate(targetThing.Label);
            }

            if (crimeType == CrimeType.Arson)
            {
                if (targetThing != null)
                {
                    return "LawAndOrder_Crime_Arson_Target".Translate(targetThing.Label);
                }
                return "LawAndOrder_Crime_Arson".Translate();
            }

            // For theft, show what was stolen
            if (crimeType == CrimeType.Theft && targetThing != null)
            {
                return "LawAndOrder_Crime_Theft_Target".Translate(targetThing.Label);
            }

            // For animal abuse, show the animal
            if (crimeType == CrimeType.AnimalAbuse && victim != null)
            {
                return "LawAndOrder_Crime_AnimalAbuse_Target".Translate(victim.Label);
            }

            // Default: try to translate the crime type
            string defaultKey = $"LawAndOrder_Crime_{crimeType}";
            if (defaultKey.CanTranslate())
            {
                return defaultKey.Translate();
            }

            // Last resort: return the enum name
            return crimeType.ToString();
        }

        public override string ToString()
        {
            string result = $"{crimeType}";
            if (victim != null)
            {
                result += $" against {victim.NameShortColored}";
            }
            if (targetThing != null)
            {
                result += $" targeting {targetThing.Label}";
            }
            if (damageDealt > 0)
            {
                result += $" ({damageDealt:F1} damage)";
            }
            return result;
        }
    }

    /// <summary>
    /// Types of crimes that can be tracked
    /// </summary>
    public enum CrimeType
    {
        Unknown,
        Assault,            // Attacking a colonist
        Murder,             // Killing a colonist
        PropertyDestruction,// Destroying colony property
        Arson,              // Setting fires
        Theft,              // Stealing items
        AnimalAbuse,        // Harming colony animals
        Trespassing,        // Entering forbidden areas
        Kidnapping,         // Taking colonists prisoner
        Vandalism,          // Minor property damage
        ContrabandPossession // Possessing contraband items
    }

    /// <summary>
    /// Summary of archived crimes for efficient storage
    /// </summary>
    public class CrimeSummary : IExposable
    {
        public int archivedCount;           // Number of crimes archived
        public float totalDebt;             // Total debt from archived crimes
        public int mostRecentTick;          // Most recent crime tick in this archive
        public int oldestTick;              // Oldest crime tick in this archive
        public Dictionary<CrimeType, int> crimeTypeCounts; // Count by type

        public CrimeSummary()
        {
            crimeTypeCounts = new Dictionary<CrimeType, int>();
        }

        public CrimeSummary(List<Crime> crimesToArchive)
        {
            crimeTypeCounts = new Dictionary<CrimeType, int>();

            if (crimesToArchive == null || crimesToArchive.Count == 0)
                return;

            archivedCount = crimesToArchive.Count;
            totalDebt = crimesToArchive.Sum(c => c.debtAmount);
            mostRecentTick = crimesToArchive.Max(c => c.tickCommitted);
            oldestTick = crimesToArchive.Min(c => c.tickCommitted);

            // Count crimes by type
            foreach (var crime in crimesToArchive)
            {
                if (!crimeTypeCounts.ContainsKey(crime.crimeType))
                {
                    crimeTypeCounts[crime.crimeType] = 0;
                }
                crimeTypeCounts[crime.crimeType]++;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref archivedCount, "archivedCount", 0);
            Scribe_Values.Look(ref totalDebt, "totalDebt", 0f);
            Scribe_Values.Look(ref mostRecentTick, "mostRecentTick", 0);
            Scribe_Values.Look(ref oldestTick, "oldestTick", 0);
            Scribe_Collections.Look(ref crimeTypeCounts, "crimeTypeCounts", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (crimeTypeCounts == null)
                {
                    crimeTypeCounts = new Dictionary<CrimeType, int>();
                }
            }
        }

        public int DaysAgo => (Find.TickManager.TicksGame - mostRecentTick) / GenDate.TicksPerDay;
    }

    /// <summary>
    /// Hediff that tracks all crimes committed by a pawn
    /// This is efficient for save files as only pawns with crimes get this hediff
    /// </summary>
    public class Hediff_Crimes : HediffWithComps
    {
        // Crime archival constants
        private const int DEFAULT_ARCHIVE_AGE_DAYS = 60; // Archive crimes older than 60 days (1 quadrum)

        private List<Crime> crimes = new List<Crime>();
        private HearingRecord hearingRecord = new HearingRecord();
        private List<CrimeSummary> archivedCrimes = new List<CrimeSummary>();

        public IReadOnlyList<Crime> Crimes => crimes.AsReadOnly();
        public IReadOnlyList<CrimeSummary> ArchivedCrimes => archivedCrimes.AsReadOnly();

        public int TotalCrimeCount => crimes?.Count ?? 0;
        public int ArchivedCrimeCount => archivedCrimes?.Sum(a => a.archivedCount) ?? 0;
        public int TotalCrimeCountIncludingArchived => TotalCrimeCount + ArchivedCrimeCount;

        public HearingRecord Hearing => hearingRecord;

        /// <summary>
        /// Add a new crime to this pawn's record
        /// </summary>
        public void AddCrime(Crime crime)
        {
            if (crimes == null)
            {
                crimes = new List<Crime>();
            }
            crimes.Add(crime);
        }

        /// <summary>
        /// Add a new crime with parameters
        /// </summary>
        public void AddCrime(CrimeType type, Pawn victim = null, Thing targetThing = null, float damageDealt = 0f, string additionalInfo = null)
        {
            AddCrime(new Crime(type, victim, targetThing, damageDealt, additionalInfo));
        }

        /// <summary>
        /// Get crimes of a specific type
        /// </summary>
        public List<Crime> GetCrimesByType(CrimeType type)
        {
            return crimes?.Where(c => c.crimeType == type).ToList() ?? new List<Crime>();
        }

        /// <summary>
        /// Get crimes committed within the last X days
        /// </summary>
        public List<Crime> GetRecentCrimes(int days)
        {
            int ticksAgo = days * GenDate.TicksPerDay;
            int cutoffTick = Find.TickManager.TicksGame - ticksAgo;
            return crimes?.Where(c => c.tickCommitted >= cutoffTick).ToList() ?? new List<Crime>();
        }

        /// <summary>
        /// Get crimes committed against a specific victim
        /// </summary>
        public List<Crime> GetCrimesAgainstVictim(Pawn victim)
        {
            return crimes?.Where(c => c.victim == victim).ToList() ?? new List<Crime>();
        }

        /// <summary>
        /// Check if pawn has committed a specific type of crime
        /// </summary>
        public bool HasCommitted(CrimeType type)
        {
            return crimes?.Any(c => c.crimeType == type) ?? false;
        }

        /// <summary>
        /// Archive old crimes to save memory and reduce save file bloat
        /// Old crimes are summarized and removed from the detailed list
        /// </summary>
        public void ArchiveOldCrimes(int daysOld = DEFAULT_ARCHIVE_AGE_DAYS)
        {
            if (crimes == null || crimes.Count == 0)
                return;

            if (archivedCrimes == null)
            {
                archivedCrimes = new List<CrimeSummary>();
            }

            // Find crimes older than threshold
            int ticksAgo = daysOld * GenDate.TicksPerDay;
            int cutoffTick = Find.TickManager.TicksGame - ticksAgo;
            var oldCrimes = crimes.Where(c => c.tickCommitted < cutoffTick).ToList();

            if (oldCrimes.Count == 0)
                return;

            // Create summary of old crimes
            var summary = new CrimeSummary(oldCrimes);
            archivedCrimes.Add(summary);

            // Remove old crimes from active list
            crimes.RemoveAll(c => c.tickCommitted < cutoffTick);

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Archived {oldCrimes.Count} crimes for {pawn?.LabelShort ?? "unknown pawn"} (older than {daysOld} days). Remaining active crimes: {crimes.Count}");
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref crimes, "crimes", LookMode.Deep);
            Scribe_Deep.Look(ref hearingRecord, "hearingRecord");
            Scribe_Collections.Look(ref archivedCrimes, "archivedCrimes", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (crimes == null)
                {
                    crimes = new List<Crime>();
                }
                if (hearingRecord == null)
                {
                    hearingRecord = new HearingRecord();
                }
                if (archivedCrimes == null)
                {
                    archivedCrimes = new List<CrimeSummary>();
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                if ((crimes == null || crimes.Count == 0) && (archivedCrimes == null || archivedCrimes.Count == 0))
                {
                    return "No crimes recorded";
                }

                var stringBuilder = new System.Text.StringBuilder();

                stringBuilder.AppendLine($"Active crimes: {TotalCrimeCount}");
                stringBuilder.AppendLine($"Recent (7 days): {GetRecentCrimes(7).Count}");

                if (ArchivedCrimeCount > 0)
                {
                    stringBuilder.AppendLine($"Archived crimes: {ArchivedCrimeCount}");
                }

                stringBuilder.Append("(View Justice tab for more info)");

                return stringBuilder.ToString();
            }
        }

        public override bool ShouldRemove => false; // Keep crimes on record unless manually cleared
    }
}
