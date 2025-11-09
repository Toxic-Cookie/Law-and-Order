using Verse;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for working with criminal records
    /// </summary>
    public static class CrimeUtils
    {
        public static HediffDef CriminalRecordDef => LawAndOrder_HediffDefOf.LawAndOrder_CriminalRecord;

        /// <summary>
        /// Get or create the criminal record hediff for a pawn
        /// </summary>
        public static Hediff_Crimes GetOrCreateCriminalRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            // Try to find existing criminal record
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(CriminalRecordDef) as Hediff_Crimes;

            if (existingHediff != null)
            {
                return existingHediff;
            }

            // Create new criminal record
            var newHediff = (Hediff_Crimes)HediffMaker.MakeHediff(CriminalRecordDef, pawn);
            pawn.health.AddHediff(newHediff);
            return newHediff;
        }

        /// <summary>
        /// Try to get existing criminal record (returns null if pawn has no crimes)
        /// </summary>
        public static Hediff_Crimes TryGetCriminalRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetFirstHediffOfDef(CriminalRecordDef) as Hediff_Crimes;
        }

        /// <summary>
        /// Record a crime for a pawn
        /// </summary>
        public static void RecordCrime(Pawn criminal, CrimeType crimeType, Pawn victim = null, Thing targetThing = null, float damageDealt = 0f, string additionalInfo = null, bool wasVictimDowned = false, bool wasVictimKilled = false, DamageInfo? damageInfo = null, string damageType = null)
        {
            if (criminal == null)
            {
                Mod.Log?.Warning("Attempted to record crime with null criminal");
                return;
            }

            try
            {
                float debtAmount = 0f;

                // Use new penalty management system if DamageInfo is provided
                if (damageInfo.HasValue && victim != null)
                {
                    debtAmount = DebtUtils.CalculateDebtForCrimeNew(
                        criminal: criminal,
                        victim: victim,
                        damageInfo: damageInfo,
                        crimeDefName: null  // Auto-detect based on damage
                    );
                }
                else
                {
                    // No DamageInfo provided - cannot calculate penalty accurately
                    // Log warning and use minimal penalty
                    Mod.Log?.Warning($"Crime recorded without DamageInfo for {criminal.NameShortColored}. Cannot calculate accurate penalty.");
                    debtAmount = 100f; // Minimal penalty as fallback
                }

                // Extract damage type from DamageInfo if available
                if (damageType == null && damageInfo.HasValue)
                {
                    damageType = damageInfo.Value.Def?.label;
                }

                // Create the crime with the calculated debt amount stored
                var crime = new Crime(crimeType, victim, targetThing, damageDealt, additionalInfo, wasVictimDowned, wasVictimKilled, debtAmount, damageType);

                var criminalRecord = GetOrCreateCriminalRecord(criminal);
                criminalRecord?.AddCrime(crime);

                // Automatically add debt for this crime
                DebtUtils.AddDebtForCrime(criminal, crime);

                // Optional: Debug logging
                if (Prefs.DevMode)
                {
                    string victimInfo = victim != null ? $" against {victim.NameShortColored}" : "";
                    string damageInfoStr = damageDealt > 0 ? $" ({damageDealt:F1} damage)" : "";
                    string downedInfo = wasVictimDowned ? " [DOWNED]" : "";
                    string killedInfo = wasVictimKilled ? " [KILLED]" : "";
                    string systemUsed = damageInfo.HasValue ? "[NEW]" : "[LEGACY]";
                    Mod.Log?.Message($"{systemUsed} Recorded {crimeType} by {criminal.NameShortColored}{victimInfo}{damageInfoStr}{downedInfo}{killedInfo} - Debt: {debtAmount:F0} silver");
                }
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Failed to record crime for {criminal.NameShortColored}: {e.Message}");
            }
        }

        /// <summary>
        /// Check if a pawn has committed any crimes
        /// </summary>
        public static bool HasCriminalRecord(Pawn pawn)
        {
            var record = TryGetCriminalRecord(pawn);
            return record != null && record.TotalCrimeCount > 0;
        }

        /// <summary>
        /// Get total crime count for a pawn
        /// </summary>
        public static int GetCrimeCount(Pawn pawn)
        {
            var record = TryGetCriminalRecord(pawn);
            return record?.TotalCrimeCount ?? 0;
        }

        /// <summary>
        /// Check if pawn has committed a specific type of crime
        /// </summary>
        public static bool HasCommittedCrime(Pawn pawn, CrimeType crimeType)
        {
            var record = TryGetCriminalRecord(pawn);
            return record?.HasCommitted(crimeType) ?? false;
        }
    }
}
