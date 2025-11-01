using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.CrimeDetection
{
    /// <summary>
    /// Debt system integration and example usage
    /// This system tracks silver debts owed by prisoners based on crimes committed
    /// </summary>
    public static class DebtSystemPatches
    {
        /// <summary>
        /// Example: Add debt when a raider is captured
        /// This shows how to automatically calculate and apply debt for all crimes
        /// </summary>
        public static void Example_ProcessCapturedRaider(Pawn raider)
        {
            // When a raider is captured, automatically calculate debt for all crimes
            DebtUtils.ApplyDebtForAllCrimes(raider);

            // Now the raider has both a criminal record and a debt record
            var debtRecord = DebtUtils.TryGetDebtRecord(raider);
            if (debtRecord != null)
            {
                Log.Message($"{raider.Name} owes {debtRecord.CurrentDebt} silver to the colony");
                Log.Message($"Estimated sentence: {debtRecord.EstimatedDaysOfLabor()} days of labor");
            }
        }

        /// <summary>
        /// Example: Manually add debt for a specific crime
        /// Use this when you want more control over debt calculation
        /// </summary>
        public static void Example_ManualDebtAddition(Pawn criminal)
        {
            // Record a crime
            var colonists = Find.AnyPlayerHomeMap?.mapPawns?.FreeColonists;
            Pawn victim = colonists != null && colonists.Count > 0 ? colonists[0] : null;

            var crime = new Crime(
                CrimeType.Assault,
                victim: victim,
                damageDealt: 25f
            );

            // Get or create criminal record
            var criminalRecord = CrimeUtils.GetOrCreateCriminalRecord(criminal);
            criminalRecord.AddCrime(crime);

            // Add debt for that specific crime
            DebtUtils.AddDebtForCrime(
                criminal,
                crime,
                isRepeatOffender: false,  // +50% if true
                usedBannedWeapon: false   // +25% if true
            );
        }

        /// <summary>
        /// Example: Pay off debt through labor
        /// Call this periodically (e.g., once per day) for working prisoners
        /// </summary>
        public static void Example_PayOffDebtThroughLabor(Pawn prisoner, float workValue)
        {
            // When a prisoner does work, pay down their debt
            // workValue is the market value of work done (e.g., 35 silver/day for basic labor)

            if (!DebtUtils.HasDebt(prisoner))
            {
                return; // No debt to pay
            }

            float paidAmount = DebtUtils.PayDebt(
                prisoner,
                workValue,
                reason: "Prison Labor"
            );

            if (paidAmount > 0)
            {
                Log.Message($"{prisoner.Name} worked off {paidAmount} silver debt");

                // Check if debt is fully paid
                if (!DebtUtils.HasDebt(prisoner))
                {
                    Messages.Message(
                        $"{prisoner.Name} has fully repaid their debt to the colony!",
                        prisoner,
                        MessageTypeDefOf.PositiveEvent
                    );
                }
            }
        }

        /// <summary>
        /// Example: Query debt information
        /// </summary>
        public static void Example_QueryDebtInfo(Pawn pawn)
        {
            var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
            if (debtRecord == null)
            {
                Log.Message($"{pawn.Name} has no debt record");
                return;
            }

            Log.Message($"=== Debt Report for {pawn.Name} ===");
            Log.Message($"Current Debt: {debtRecord.CurrentDebt} silver");
            Log.Message($"Total Incurred: {debtRecord.TotalDebtOwed} silver");
            Log.Message($"Total Paid: {debtRecord.TotalDebtPaid} silver");
            Log.Message($"Estimated Labor: {debtRecord.EstimatedDaysOfLabor()} days");

            // View recent debt changes
            var recentEntries = debtRecord.GetRecentEntries(7); // Last 7 days
            Log.Message($"Recent changes ({recentEntries.Count} in last 7 days):");
            foreach (var entry in recentEntries)
            {
                Log.Message($"  {entry}");
            }
        }

        /// <summary>
        /// Example: Calculate debt for a specific crime type
        /// This shows the debt values for each crime
        /// </summary>
        public static void Example_ViewDebtValues()
        {
            // These are the base values (before modifiers)
            // See DebtUtils.cs for the actual calculation logic

            Log.Message("=== Crime Debt Values ===");
            Log.Message("Armed Trespassing: 150 silver");
            Log.Message("Arson (base): 250 silver + value of items destroyed");
            Log.Message("Theft: Item value × 1.5");
            Log.Message("Assault: 350 silver");
            Log.Message("Downed Colonist: 750 silver");
            Log.Message("Assault on Animal: 100 silver");
            Log.Message("Killed Animal: Market value × 2");
            Log.Message("Killed Bonded Animal: (Market value × 3) + 500 silver");
            Log.Message("Murder: 5,000 silver");
            Log.Message("Property Destruction: Item value × 1.2");
            Log.Message("");
            Log.Message("Modifiers:");
            Log.Message("  Banned Weapon: +25% to total");
            Log.Message("  Repeat Offender: +50% to total");
        }

        /// <summary>
        /// Example: Settlement payment
        /// Allow prisoners to pay off debt with silver instead of labor
        /// </summary>
        public static void Example_SettleDebtWithSilver(Pawn prisoner)
        {
            float debtAmount = DebtUtils.GetCurrentDebt(prisoner);

            if (debtAmount <= 0)
            {
                Messages.Message("This prisoner has no debt", MessageTypeDefOf.RejectInput);
                return;
            }

            // Check if colony wants to accept a silver payment
            // (You would show a dialog here in real implementation)

            // If accepted, take silver from prisoner's inventory and pay debt
            Thing silver = null;
            foreach (Thing thing in prisoner.inventory.innerContainer)
            {
                if (thing.def == ThingDefOf.Silver)
                {
                    silver = thing;
                    break;
                }
            }

            if (silver != null)
            {
                float paymentAmount = Mathf.Min(silver.stackCount, debtAmount);
                silver.stackCount -= (int)paymentAmount;

                if (silver.stackCount <= 0)
                {
                    silver.Destroy();
                }

                DebtUtils.PayDebt(prisoner, paymentAmount, reason: "Silver Payment");

                Messages.Message(
                    $"{prisoner.Name} paid {paymentAmount} silver toward their debt",
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        /// <summary>
        /// Example: Integration with Prison Labor or similar mods
        /// This could be called from a tick-based system or work completion handler
        /// </summary>
        public static void Example_IntegrationWithPrisonLabor()
        {
            // Pseudo-code for how you might integrate with Prison Labor mod:

            /*
            // In your work completion handler:
            void OnPrisonerCompletedWork(Pawn prisoner, WorkTypeDef workType, float ticks)
            {
                // Calculate work value based on skill, work type, and time
                float baseValuePerHour = 2.0f; // Adjust based on work type
                float skillMultiplier = 1f + (prisoner.skills.GetSkill(workType.relevantSkills[0]).Level * 0.05f);
                float hoursWorked = ticks / GenDate.TicksPerHour;

                float workValue = baseValuePerHour * skillMultiplier * hoursWorked;

                // Pay down debt
                DebtUtils.PayDebt(prisoner, workValue, $"Work: {workType.label}");
            }
            */

            // For a simpler daily system:
            /*
            void OnDayPassed()
            {
                foreach (Pawn prisoner in PawnsFinder.AllMaps_PrisonersOfColonySpawned)
                {
                    if (!prisoner.Downed && prisoner.Awake())
                    {
                        // Assume average daily labor value
                        float dailyValue = 35f; // Configurable

                        // Adjust based on factors (health, skills, etc.)
                        if (prisoner.health.summaryHealth.SummaryHealthPercent < 0.8f)
                        {
                            dailyValue *= prisoner.health.summaryHealth.SummaryHealthPercent;
                        }

                        DebtUtils.PayDebt(prisoner, dailyValue, "Daily Labor");
                    }
                }
            }
            */
        }
    }
}
