using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Settings;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for managing prisoner debt
    /// </summary>
    public static class DebtUtils
    {
        // Crime debt multipliers
        private const float KIDNAPPING_DEBT_MULTIPLIER = 1.5f; // Kidnapping is 150% of downing debt

        public static HediffDef DebtDef => LawAndOrder_HediffDefOf.LawAndOrder_Debt;

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
                    debt = LawAndOrderSettings.ArmedTrespassing.Value;
                    reason = "Armed Trespassing";
                    break;

                case CrimeType.Arson:
                    debt = LawAndOrderSettings.ArsonBase.Value;
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
                        debt = crime.targetThing.MarketValue * LawAndOrderSettings.TheftMultiplier.Value;
                        reason = $"Theft of {crime.targetThing.Label}";
                    }
                    break;

                case CrimeType.Assault:
                    // Check if target was downed (use stored flag, not current state)
                    if (crime.wasVictimDowned)
                    {
                        debt = LawAndOrderSettings.DownedColonist.Value;
                        reason = $"Downed {crime.victim?.LabelShort ?? "colonist"}";
                    }
                    else if (crime.victim?.RaceProps?.Animal ?? false)
                    {
                        debt = LawAndOrderSettings.AssaultAnimal.Value;
                        reason = $"Assault on {crime.victim.LabelShort}";
                    }
                    else
                    {
                        debt = LawAndOrderSettings.Assault.Value;
                        reason = crime.victim != null ? $"Assault on {crime.victim.LabelShort}" : "Assault";
                    }
                    break;

                case CrimeType.Murder:
                    debt = LawAndOrderSettings.Murder.Value;
                    reason = crime.victim != null ? $"Murder of {crime.victim.LabelShort}" : "Murder";
                    break;

                case CrimeType.AnimalAbuse:
                    // Check if animal was killed (use stored flag, not current state)
                    if (crime.wasVictimKilled)
                    {
                        // Killed animal
                        float baseValue = crime.victim?.MarketValue ?? 0f;

                        // Check if bonded
                        bool isBonded = crime.victim?.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond, x => x.IsColonist) != null;

                        if (isBonded)
                        {
                            debt = (baseValue * LawAndOrderSettings.KillBondedAnimalMultiplier.Value) + LawAndOrderSettings.KillBondedAnimalBonus.Value;
                            reason = $"Killed bonded animal ({crime.victim?.LabelShort ?? "animal"})";
                        }
                        else
                        {
                            debt = baseValue * LawAndOrderSettings.KillAnimalMultiplier.Value;
                            reason = $"Killed {crime.victim?.LabelShort ?? "animal"}";
                        }
                    }
                    else
                    {
                        debt = LawAndOrderSettings.AssaultAnimal.Value;
                        reason = crime.victim != null ? $"Harmed {crime.victim.LabelShort}" : "Animal Abuse";
                    }
                    break;

                case CrimeType.PropertyDestruction:
                case CrimeType.Vandalism:
                    if (crime.targetThing != null)
                    {
                        debt = crime.targetThing.MarketValue * LawAndOrderSettings.PropertyDestructionMultiplier.Value;
                        reason = $"Destroyed {crime.targetThing.Label}";
                    }
                    break;

                case CrimeType.Kidnapping:
                    // This is a serious crime, treated similarly to assault/downed
                    debt = LawAndOrderSettings.DownedColonist.Value * KIDNAPPING_DEBT_MULTIPLIER;
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
                totalDebt *= LawAndOrderSettings.BannedWeaponModifier.Value;
                int percent = (int)((LawAndOrderSettings.BannedWeaponModifier.Value - 1f) * 100f);
                modifiers += $" [Banned Weapon +{percent}%]";
            }

            if (isRepeatOffender)
            {
                totalDebt *= LawAndOrderSettings.RepeatOffenderModifier.Value;
                int percent = (int)((LawAndOrderSettings.RepeatOffenderModifier.Value - 1f) * 100f);
                modifiers += $" [Repeat Offender +{percent}%]";
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
                totalDebt *= LawAndOrderSettings.BannedWeaponModifier.Value;
            }

            if (isRepeatOffender)
            {
                totalDebt *= LawAndOrderSettings.RepeatOffenderModifier.Value;
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

        /// <summary>
        /// Add debt for contraband items found on a pawn.
        /// </summary>
        public static void AddDebtForContraband(Pawn criminal, float totalPenalty, System.Collections.Generic.Dictionary<ThingDef, int> contrabandItems)
        {
            if (criminal == null || totalPenalty <= 0)
            {
                return;
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord == null)
            {
                return;
            }

            // Build a description of the contraband
            string itemList = string.Join(", ", System.Linq.Enumerable.Select(contrabandItems, kvp => $"{kvp.Value}x {kvp.Key.LabelCap}"));
            string reason = $"Contraband: {itemList}";

            debtRecord.AddDebt(totalPenalty, reason);

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Applied {totalPenalty:F0} silver contraband debt to {criminal.LabelShort}");
            }
        }
    }
}
