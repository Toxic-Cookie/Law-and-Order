using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

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

        public Crime()
        {
        }

        public Crime(CrimeType type, Pawn victim = null, Thing targetThing = null, float damageDealt = 0f, string additionalInfo = null)
        {
            this.crimeType = type;
            this.tickCommitted = Find.TickManager.TicksGame;
            this.victim = victim;
            this.targetThing = targetThing;
            this.damageDealt = damageDealt;
            this.additionalInfo = additionalInfo;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref crimeType, "crimeType", CrimeType.Unknown);
            Scribe_Values.Look(ref tickCommitted, "tickCommitted", 0);
            Scribe_References.Look(ref victim, "victim");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_Values.Look(ref damageDealt, "damageDealt", 0f);
            Scribe_Values.Look(ref additionalInfo, "additionalInfo");
        }

        public int DaysAgo => (Find.TickManager.TicksGame - tickCommitted) / GenDate.TicksPerDay;

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
        Vandalism           // Minor property damage
    }

    /// <summary>
    /// Hediff that tracks all crimes committed by a pawn
    /// This is efficient for save files as only pawns with crimes get this hediff
    /// </summary>
    public class Hediff_Crimes : HediffWithComps
    {
        private List<Crime> crimes = new List<Crime>();

        public IReadOnlyList<Crime> Crimes => crimes.AsReadOnly();

        public int TotalCrimeCount => crimes?.Count ?? 0;

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
        /// Clear old crimes (optional - for performance if list gets too long)
        /// </summary>
        public void ClearCrimesOlderThan(int days)
        {
            if (crimes == null) return;

            int ticksAgo = days * GenDate.TicksPerDay;
            int cutoffTick = Find.TickManager.TicksGame - ticksAgo;
            crimes.RemoveAll(c => c.tickCommitted < cutoffTick);

            // If no crimes left, could optionally remove the hediff entirely
            if (crimes.Count == 0)
            {
                pawn.health.RemoveHediff(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref crimes, "crimes", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (crimes == null)
                {
                    crimes = new List<Crime>();
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                if (crimes == null || crimes.Count == 0)
                {
                    return "No crimes recorded";
                }

                var stringBuilder = new System.Text.StringBuilder();

                return stringBuilder.ToString() + $"\nTotal crimes: {crimes.Count}\n" + $"Recent (7 days): {GetRecentCrimes(7).Count}\n" + $"(View Justice tab for more info)";
            }
        }

        public override bool ShouldRemove => false; // Keep crimes on record unless manually cleared
    }
}
