using Verse;
using RimWorld;

namespace Law_and_Order.Source.Hediffs
{
    /// <summary>
    /// Hediff that tracks a monetary fine owed by a pawn.
    /// The pawn experiences mood penalties until the fine is paid.
    /// Phase 4: Fine Punishment System
    /// </summary>
    public class Hediff_Fine : HediffWithComps
    {
        /// <summary>
        /// Total amount of silver owed.
        /// </summary>
        public int fineAmount = 0;

        /// <summary>
        /// Amount of silver paid so far.
        /// </summary>
        public int finePaid = 0;

        /// <summary>
        /// Remaining fine amount.
        /// </summary>
        public int RemainingFine => fineAmount - finePaid;

        /// <summary>
        /// Whether the fine is fully paid.
        /// </summary>
        public bool IsFullyPaid => finePaid >= fineAmount;

        /// <summary>
        /// Pay a portion of the fine.
        /// </summary>
        public void PayFine(int amount)
        {
            if (amount <= 0)
                return;

            finePaid += amount;

            if (finePaid > fineAmount)
                finePaid = fineAmount;

            Mod.Log?.Message($"{pawn?.NameShortColored} paid {amount} silver towards fine. Remaining: {RemainingFine} silver");

            // Remove hediff if fully paid
            if (IsFullyPaid)
            {
                pawn?.health?.RemoveHediff(this);
                Mod.Log?.Message($"{pawn?.NameShortColored} has fully paid their fine of {fineAmount} silver");
            }
        }

        /// <summary>
        /// Label shown in health tab.
        /// </summary>
        public override string LabelInBrackets => $"{RemainingFine}/{fineAmount} silver";

        /// <summary>
        /// Tooltip with details.
        /// </summary>
        public override string TipStringExtra
        {
            get
            {
                return $"Total Fine: {fineAmount} silver\n" +
                       $"Paid: {finePaid} silver\n" +
                       $"Remaining: {RemainingFine} silver\n\n" +
                       $"This pawn owes a fine for their crimes. They will experience mood penalties until the fine is fully paid.";
            }
        }

        /// <summary>
        /// Save/load support.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref fineAmount, "fineAmount", 0);
            Scribe_Values.Look(ref finePaid, "finePaid", 0);
        }
    }
}
