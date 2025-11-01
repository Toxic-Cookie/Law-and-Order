using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Represents a debt entry - either added or paid
    /// </summary>
    public class DebtEntry : IExposable
    {
        public DebtChangeType changeType;
        public float silverAmount;
        public int tickRecorded;
        public string reason;

        public DebtEntry()
        {
        }

        public DebtEntry(DebtChangeType changeType, float silverAmount, string reason = null)
        {
            this.changeType = changeType;
            this.silverAmount = silverAmount;
            this.tickRecorded = Find.TickManager.TicksGame;
            this.reason = reason ?? "Unknown";
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref changeType, "changeType", DebtChangeType.Added);
            Scribe_Values.Look(ref silverAmount, "silverAmount", 0f);
            Scribe_Values.Look(ref tickRecorded, "tickRecorded", 0);
            Scribe_Values.Look(ref reason, "reason", "Unknown");
        }

        public int DaysAgo => (Find.TickManager.TicksGame - tickRecorded) / GenDate.TicksPerDay;

        public override string ToString()
        {
            string sign = changeType == DebtChangeType.Added ? "+" : "-";
            return $"{sign}{silverAmount:F0} silver - {reason}";
        }
    }

    /// <summary>
    /// Type of debt change
    /// </summary>
    public enum DebtChangeType
    {
        Added,      // Debt was added (crime committed)
        Paid        // Debt was paid off (labor, settlement, etc)
    }

    /// <summary>
    /// Hediff that tracks the financial debt owed by a prisoner
    /// This works in conjunction with Hediff_Crimes to track both the crimes and their financial consequences
    /// </summary>
    public class Hediff_Debt : HediffWithComps
    {
        private float totalDebtOwed = 0f;
        private float totalDebtPaid = 0f;
        private List<DebtEntry> debtHistory = new List<DebtEntry>();

        /// <summary>
        /// Current debt remaining (total owed - total paid)
        /// </summary>
        public float CurrentDebt => totalDebtOwed - totalDebtPaid;

        /// <summary>
        /// Total debt ever incurred
        /// </summary>
        public float TotalDebtOwed => totalDebtOwed;

        /// <summary>
        /// Total debt paid off
        /// </summary>
        public float TotalDebtPaid => totalDebtPaid;

        /// <summary>
        /// Full history of debt changes
        /// </summary>
        public IReadOnlyList<DebtEntry> DebtHistory => debtHistory.AsReadOnly();

        /// <summary>
        /// Add debt to this pawn's record
        /// </summary>
        public void AddDebt(float silverAmount, string reason = null)
        {
            if (silverAmount <= 0)
            {
                Mod.Log?.Warning($"Attempted to add non-positive debt: {silverAmount}");
                return;
            }

            if (debtHistory == null)
            {
                debtHistory = new List<DebtEntry>();
            }

            totalDebtOwed += silverAmount;
            debtHistory.Add(new DebtEntry(DebtChangeType.Added, silverAmount, reason));

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"{pawn?.NameShortColored} incurred {silverAmount:F0} silver debt: {reason}");
            }
        }

        /// <summary>
        /// Pay off some or all of the debt
        /// </summary>
        /// <returns>The amount actually paid (may be less than requested if debt is lower)</returns>
        public float PayDebt(float silverAmount, string reason = null)
        {
            if (silverAmount <= 0)
            {
                Mod.Log?.Warning($"Attempted to pay non-positive debt: {silverAmount}");
                return 0f;
            }

            if (debtHistory == null)
            {
                debtHistory = new List<DebtEntry>();
            }

            // Can't pay more than what's owed
            float amountToPay = Mathf.Min(silverAmount, CurrentDebt);

            if (amountToPay <= 0)
            {
                return 0f;
            }

            totalDebtPaid += amountToPay;
            debtHistory.Add(new DebtEntry(DebtChangeType.Paid, amountToPay, reason));

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"{pawn?.NameShortColored} paid {amountToPay:F0} silver debt: {reason}. Remaining: {CurrentDebt:F0}");
            }

            // If debt is fully paid, keep the hediff as a historical record
            // This allows us to track that a pawn had debt and paid it off
            if (CurrentDebt <= 0.01f) // Small epsilon for floating point errors
            {
                ModLog.Debug($"{pawn?.NameShortColored} has fully paid their debt. Hediff kept for historical record.");
                // Hediff is intentionally kept to maintain payment history
                // The ShouldRemove property returns false, so this hediff will persist
            }

            return amountToPay;
        }

        /// <summary>
        /// Get recent debt entries from the last X days
        /// </summary>
        public List<DebtEntry> GetRecentEntries(int days)
        {
            if (debtHistory == null) return new List<DebtEntry>();

            int ticksAgo = days * GenDate.TicksPerDay;
            int cutoffTick = Find.TickManager.TicksGame - ticksAgo;
            return debtHistory.Where(e => e.tickRecorded >= cutoffTick).ToList();
        }

        /// <summary>
        /// Calculate estimated days of labor needed to pay off debt
        /// Based on typical prisoner labor value
        /// </summary>
        public int EstimatedDaysOfLabor(float silverPerDay = 35f)
        {
            if (CurrentDebt <= 0) return 0;
            return Mathf.CeilToInt(CurrentDebt / silverPerDay);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalDebtOwed, "totalDebtOwed", 0f);
            Scribe_Values.Look(ref totalDebtPaid, "totalDebtPaid", 0f);
            Scribe_Collections.Look(ref debtHistory, "debtHistory", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (debtHistory == null)
                {
                    debtHistory = new List<DebtEntry>();
                }
            }
        }

        public override string TipStringExtra
        {
            get
            {
                if (CurrentDebt <= 0 && totalDebtOwed <= 0)
                {
                    return "No debt recorded";
                }

                var stringBuilder = new System.Text.StringBuilder();
                stringBuilder.AppendLine($"Current debt: {CurrentDebt:F0} silver");

                if (CurrentDebt > 0)
                {
                    int estimatedDays = EstimatedDaysOfLabor();
                    stringBuilder.AppendLine($"Est. labor: ~{estimatedDays} days");
                }

                if (totalDebtPaid > 0)
                {
                    float percentPaid = (totalDebtPaid / totalDebtOwed) * 100f;
                    stringBuilder.AppendLine($"Paid: {totalDebtPaid:F0} ({percentPaid:F0}%)");
                }

                stringBuilder.AppendLine("\n(View Justice tab for details)");

                return stringBuilder.ToString().TrimEnd();
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                if (CurrentDebt <= 0)
                {
                    return "Paid";
                }
                return $"{CurrentDebt:F0} silver";
            }
        }

        public override bool ShouldRemove => false; // Keep debt record for historical purposes
    }
}
