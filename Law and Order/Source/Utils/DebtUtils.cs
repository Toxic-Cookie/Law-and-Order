using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for managing prisoner debt
    /// </summary>
    public static class DebtUtils
    {
        public static HediffDef DebtDef => LawAndOrder_HediffDefOf.LawAndOrder_Debt;

        // Base debt values for different crime types
        private const float ARMED_TRESPASSING = 150f;
        private const float ARSON_BASE = 250f;
        private const float THEFT_MULTIPLIER = 1.5f;
        private const float CONTRABAND_PER_DRUG = 50f;
        private const float ASSAULT = 350f;
        private const float DOWNED_COLONIST = 750f;
        private const float ASSAULT_ANIMAL = 100f;
        private const float KILL_ANIMAL_MULTIPLIER = 2f;
        private const float KILL_BONDED_ANIMAL_MULTIPLIER = 3f;
        private const float KILL_BONDED_ANIMAL_BONUS = 500f;
        private const float MURDER = 5000f;
        private const float PROPERTY_DESTRUCTION_MULTIPLIER = 1.2f;

        // Sentencing modifiers
        private const float BANNED_WEAPON_MODIFIER = 1.25f;
        private const float REPEAT_OFFENDER_MODIFIER = 1.5f;

        /// <summary>
        /// Get or create the debt hediff for a pawn
        /// </summary>
        public static Hediff_Debt GetOrCreateDebtRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            // Try to find existing debt record
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DebtDef) as Hediff_Debt;

            if (existingHediff != null)
            {
                return existingHediff;
            }

            // Create new debt record
            var newHediff = (Hediff_Debt)HediffMaker.MakeHediff(DebtDef, pawn);
            pawn.health.AddHediff(newHediff);
            return newHediff;
        }

        /// <summary>
        /// Try to get existing debt record (returns null if pawn has no debt)
        /// </summary>
        public static Hediff_Debt TryGetDebtRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetFirstHediffOfDef(DebtDef) as Hediff_Debt;
        }

        /// <summary>
        /// Calculate the debt amount for a specific crime
        /// </summary>
        public static float CalculateDebtForCrime(Crime crime)
        {
            if (crime == null) return 0f;

            float debt = 0f;
            string reason = crime.crimeType.ToString();

            switch (crime.crimeType)
            {
                case CrimeType.Trespassing:
                    debt = ARMED_TRESPASSING;
                    reason = "Armed Trespassing";
                    break;

                case CrimeType.Arson:
                    debt = ARSON_BASE;
                    // Additional cost for destroyed items could be added here if tracked
                    if (crime.targetThing != null && crime.targetThing.Destroyed)
                    {
                        debt += crime.targetThing.MarketValue;
                        reason = $"Arson (destroyed {crime.targetThing.Label})";
                    }
                    else
                    {
                        reason = "Arson";
                    }
                    break;

                case CrimeType.Theft:
                    if (crime.targetThing != null)
                    {
                        debt = crime.targetThing.MarketValue * THEFT_MULTIPLIER;
                        reason = $"Theft of {crime.targetThing.Label}";
                    }
                    break;

                case CrimeType.Assault:
                    // Check if target was downed
                    if (crime.victim != null && crime.victim.Downed)
                    {
                        debt = DOWNED_COLONIST;
                        reason = $"Downed {crime.victim.LabelShort}";
                    }
                    else if (crime.victim?.RaceProps?.Animal ?? false)
                    {
                        debt = ASSAULT_ANIMAL;
                        reason = $"Assault on {crime.victim.LabelShort}";
                    }
                    else
                    {
                        debt = ASSAULT;
                        reason = crime.victim != null ? $"Assault on {crime.victim.LabelShort}" : "Assault";
                    }
                    break;

                case CrimeType.Murder:
                    debt = MURDER;
                    reason = crime.victim != null ? $"Murder of {crime.victim.LabelShort}" : "Murder";
                    break;

                case CrimeType.AnimalAbuse:
                    if (crime.victim != null && crime.victim.Dead)
                    {
                        // Killed animal
                        float baseValue = crime.victim.MarketValue;

                        // Check if bonded
                        bool isBonded = crime.victim.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond, x => x.IsColonist) != null;

                        if (isBonded)
                        {
                            debt = (baseValue * KILL_BONDED_ANIMAL_MULTIPLIER) + KILL_BONDED_ANIMAL_BONUS;
                            reason = $"Killed bonded animal ({crime.victim.LabelShort})";
                        }
                        else
                        {
                            debt = baseValue * KILL_ANIMAL_MULTIPLIER;
                            reason = $"Killed {crime.victim.LabelShort}";
                        }
                    }
                    else
                    {
                        debt = ASSAULT_ANIMAL;
                        reason = crime.victim != null ? $"Harmed {crime.victim.LabelShort}" : "Animal Abuse";
                    }
                    break;

                case CrimeType.PropertyDestruction:
                case CrimeType.Vandalism:
                    if (crime.targetThing != null)
                    {
                        debt = crime.targetThing.MarketValue * PROPERTY_DESTRUCTION_MULTIPLIER;
                        reason = $"Destroyed {crime.targetThing.Label}";
                    }
                    break;

                case CrimeType.Kidnapping:
                    // This is a serious crime, treated similarly to assault/downed
                    debt = DOWNED_COLONIST * 1.5f;
                    reason = crime.victim != null ? $"Kidnapped {crime.victim.LabelShort}" : "Kidnapping";
                    break;
            }

            return debt;
        }

        /// <summary>
        /// Add debt for a crime and automatically add it to the pawn's debt record
        /// </summary>
        public static void AddDebtForCrime(Pawn criminal, Crime crime, bool isRepeatOffender = false, bool usedBannedWeapon = false)
        {
            if (criminal == null || crime == null)
            {
                return;
            }

            float baseDebt = CalculateDebtForCrime(crime);

            if (baseDebt <= 0)
            {
                return;
            }

            // Apply modifiers
            float totalDebt = baseDebt;
            string modifiers = "";

            if (usedBannedWeapon)
            {
                totalDebt *= BANNED_WEAPON_MODIFIER;
                modifiers += " [Banned Weapon +25%]";
            }

            if (isRepeatOffender)
            {
                totalDebt *= REPEAT_OFFENDER_MODIFIER;
                modifiers += " [Repeat Offender +50%]";
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord != null)
            {
                string reason = $"{crime.crimeType}";
                if (crime.victim != null)
                {
                    reason += $" vs {crime.victim.LabelShort}";
                }
                reason += modifiers;

                debtRecord.AddDebt(totalDebt, reason);
            }
        }

        /// <summary>
        /// Check if a pawn has any debt
        /// </summary>
        public static bool HasDebt(Pawn pawn)
        {
            var record = TryGetDebtRecord(pawn);
            return record != null && record.CurrentDebt > 0;
        }

        /// <summary>
        /// Get current debt amount for a pawn
        /// </summary>
        public static float GetCurrentDebt(Pawn pawn)
        {
            var record = TryGetDebtRecord(pawn);
            return record?.CurrentDebt ?? 0f;
        }

        /// <summary>
        /// Pay off some of a pawn's debt
        /// </summary>
        public static float PayDebt(Pawn pawn, float amount, string reason = null)
        {
            var record = TryGetDebtRecord(pawn);
            if (record == null)
            {
                return 0f;
            }

            return record.PayDebt(amount, reason ?? "Labor");
        }

        /// <summary>
        /// Calculate debt for all crimes in a criminal record
        /// Useful for applying debt retroactively or on first capture
        /// </summary>
        public static float CalculateTotalDebtForCrimes(Hediff_Crimes criminalRecord, bool isRepeatOffender = false, bool usedBannedWeapon = false)
        {
            if (criminalRecord == null || criminalRecord.Crimes == null)
            {
                return 0f;
            }

            float totalDebt = 0f;

            foreach (var crime in criminalRecord.Crimes)
            {
                float crimeDebt = CalculateDebtForCrime(crime);
                totalDebt += crimeDebt;
            }

            // Apply modifiers to total
            if (usedBannedWeapon)
            {
                totalDebt *= BANNED_WEAPON_MODIFIER;
            }

            if (isRepeatOffender)
            {
                totalDebt *= REPEAT_OFFENDER_MODIFIER;
            }

            return totalDebt;
        }

        /// <summary>
        /// Apply debt for all crimes in a criminal record
        /// This is useful when a raider is first captured
        /// </summary>
        public static void ApplyDebtForAllCrimes(Pawn criminal)
        {
            if (criminal == null)
            {
                return;
            }

            var criminalRecord = CrimeUtils.TryGetCriminalRecord(criminal);
            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                return;
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord == null)
            {
                return;
            }

            // Process each crime
            foreach (var crime in criminalRecord.Crimes)
            {
                float crimeDebt = CalculateDebtForCrime(crime);
                if (crimeDebt > 0)
                {
                    string reason = crime.crimeType.ToString();
                    if (crime.victim != null)
                    {
                        reason += $" vs {crime.victim.LabelShort}";
                    }
                    else if (crime.targetThing != null)
                    {
                        reason += $" ({crime.targetThing.Label})";
                    }

                    debtRecord.AddDebt(crimeDebt, reason);
                }
            }

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Applied {debtRecord.CurrentDebt:F0} silver debt to {criminal.NameShortColored} for {criminalRecord.TotalCrimeCount} crimes");
            }
        }
    }
}
