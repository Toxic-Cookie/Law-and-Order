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

            // Animals cannot be criminals - only humanlike pawns can commit crimes
            if (criminal.RaceProps?.Animal == true)
            {
#if DEBUG
                Mod.Log?.Message($"Skipping crime recording for animal: {criminal.LabelShort}");
#endif
                return;
            }

            try
            {
                // TODO Phase 1: Add witness detection using FogOfWarUtils here

                // Extract damage type from DamageInfo if available
                if (damageType == null && damageInfo.HasValue)
                {
                    damageType = damageInfo.Value.Def?.label;
                }

                // Create the crime (simplified for Phase 0)
                var crime = new Crime(crimeType, victim, targetThing, damageDealt, additionalInfo, wasVictimDowned, wasVictimKilled, damageType);

                var criminalRecord = GetOrCreateCriminalRecord(criminal);
                criminalRecord?.AddCrime(crime);

                // TODO Phase 1: Determine visibility state based on witnesses
                // TODO Phase 1: Create/update case if Suspected

                // Optional: Debug logging
                if (Prefs.DevMode)
                {
                    string victimInfo = victim != null ? $" against {victim.NameShortColored}" : "";
                    string damageInfoStr = damageDealt > 0 ? $" ({damageDealt:F1} damage)" : "";
                    string downedInfo = wasVictimDowned ? " [DOWNED]" : "";
                    string killedInfo = wasVictimKilled ? " [KILLED]" : "";
                    Mod.Log?.Message($"Recorded {crimeType} by {criminal.NameShortColored}{victimInfo}{damageInfoStr}{downedInfo}{killedInfo}");
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

        /// <summary>
        /// Get a penalty breakdown tooltip showing how the penalty was calculated
        /// </summary>
        // TODO Phase 1: Replace with evidence/witness tooltip
        public static string GetPenaltyBreakdownTooltip(Crime crime)
        {
            return null; // Penalty system removed in Phase 0 cleanup

            /* OLD CODE - REMOVED
            if (crime == null || crime.debtAmount <= 0)
            {
                return null;
            }

            // Check if we have a penalty breakdown stored
            if (crime.penaltyBreakdown == null)
            {
                // Fallback for old crimes without breakdown data
                return "LawAndOrder_PenaltyBreakdown_Simple".Translate(crime.debtAmount);
            }

            var breakdown = crime.penaltyBreakdown;
            System.Text.StringBuilder tooltip = new System.Text.StringBuilder();

            tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Header".Translate());
            tooltip.AppendLine();

            // Crime type
            tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Crime".Translate(breakdown.crimeDefName));

            // Different display for victim vs non-victim crimes
            if (breakdown.isNonVictimCrime)
            {
                // Non-victim crime (property, theft, etc.)
                tooltip.AppendLine();
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Calculation".Translate(breakdown.calculationMethod));

                if (breakdown.minPenalty > 0 || breakdown.maxPenalty > 0)
                {
                    tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_BaseRange".Translate(breakdown.minPenalty, breakdown.maxPenalty));
                }
            }
            else
            {
                // Victim crime (assault, murder, etc.)
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_BaseRange".Translate(breakdown.minPenalty, breakdown.maxPenalty));
                tooltip.AppendLine();

                // Severity calculation
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_SeverityFactors".Translate());
                tooltip.AppendLine("  • " + "LawAndOrder_PenaltyBreakdown_BodyPartImportance".Translate((breakdown.bodyPartImportance * 100).ToString("F0")));
                tooltip.AppendLine("  • " + "LawAndOrder_PenaltyBreakdown_DamageRatio".Translate((breakdown.damageRatio * 100).ToString("F0")));
                tooltip.AppendLine("  • " + "LawAndOrder_PenaltyBreakdown_PermanentEffect".Translate(breakdown.hasPermanentEffect ? "Yes" : "No"));
                tooltip.AppendLine("  • " + "LawAndOrder_PenaltyBreakdown_SeverityScore".Translate((breakdown.severityScore * 100).ToString("F0")));
                tooltip.AppendLine();

                // Base penalty (after severity interpolation)
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_BasePenalty".Translate(breakdown.basePenalty.ToString("F0")));
            }

            // Multipliers (if any)
            if (breakdown.appliedMultipliers != null && breakdown.appliedMultipliers.Count > 0)
            {
                tooltip.AppendLine();
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Multipliers".Translate());
                foreach (var mult in breakdown.appliedMultipliers)
                {
                    tooltip.AppendLine(string.Format("  • {0}: {1}x", mult.name, mult.value.ToString("F2")));
                }
            }

            // Global penalty scale
            if (breakdown.globalPenaltyScale != 1.0f)
            {
                tooltip.AppendLine();
                tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_GlobalScale".Translate((breakdown.globalPenaltyScale * 100).ToString("F0")));
            }

            // Final penalty
            tooltip.AppendLine();
            tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Final".Translate(breakdown.finalPenalty.ToString("F0")));

            return tooltip.ToString();
            */
        }
    }
}
