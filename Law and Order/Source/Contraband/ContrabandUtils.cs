using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hediffs;

namespace LawAndOrder
{
    /// <summary>
    /// Utilities for detecting and processing contraband items.
    /// </summary>
    public static class ContrabandUtils
    {
        /// <summary>
        /// Scans a pawn's inventory for contraband and applies penalties.
        /// Returns true if any contraband was found.
        /// </summary>
        public static bool ScanAndApplyContrabandPenalties(Pawn pawn)
        {
            if (pawn == null || pawn.inventory?.innerContainer == null)
                return false;

            var manager = WorldComponent_ContrabandManager.Instance;
            if (manager == null || manager.ContrabandDefinitions.Count == 0)
                return false;

            // Calculate contraband penalties
            int totalPenalty = manager.CalculateContrabandPenalty(pawn, out Dictionary<ThingDef, int> contrabandItems);

            if (totalPenalty > 0 && contrabandItems.Count > 0)
            {
                // Add debt for contraband
                DebtUtils.GetOrCreateDebtRecord(pawn);
                DebtUtils.AddDebtForContraband(pawn, totalPenalty, contrabandItems);

                // Record contraband crime
                RecordContrabandCrime(pawn, contrabandItems, totalPenalty);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Records a contraband crime for a pawn.
        /// </summary>
        private static void RecordContrabandCrime(Pawn pawn, Dictionary<ThingDef, int> contrabandItems, int totalPenalty)
        {
            var criminalRecord = CrimeUtils.GetOrCreateCriminalRecord(pawn);

            // Build description of contraband items
            string itemList = string.Join(", ", contrabandItems.Select(kvp => $"{kvp.Value}x {kvp.Key.LabelCap}"));

            var crime = new Crime
            {
                crimeType = CrimeType.ContrabandPossession,
                victim = null,
                damageDealt = 0f,
                tickCommitted = Find.TickManager.TicksGame,
                debtAmount = totalPenalty,
                additionalInfo = $"Items: {itemList}"
            };

            criminalRecord.AddCrime(crime);
        }

        /// <summary>
        /// Gets a list of contraband items in a pawn's inventory.
        /// </summary>
        public static List<Thing> GetContrabandItems(Pawn pawn)
        {
            var contrabandList = new List<Thing>();

            if (pawn?.inventory?.innerContainer == null)
                return contrabandList;

            var manager = WorldComponent_ContrabandManager.Instance;
            if (manager == null)
                return contrabandList;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (manager.IsContraband(thing.def))
                {
                    contrabandList.Add(thing);
                }
            }

            return contrabandList;
        }

        /// <summary>
        /// Confiscates contraband items from a pawn's inventory.
        /// </summary>
        public static void ConfiscateContraband(Pawn pawn)
        {
            var contrabandItems = GetContrabandItems(pawn);

            if (contrabandItems.Count == 0)
                return;

            // Drop contraband items on the ground or add to colony storage
            foreach (Thing contraband in contrabandItems.ToList())
            {
                if (pawn.inventory.innerContainer.Contains(contraband))
                {
                    pawn.inventory.innerContainer.TryDrop(contraband, pawn.Position, pawn.Map, ThingPlaceMode.Near, out Thing droppedThing);
                }
            }

            Messages.Message(
                $"Confiscated {contrabandItems.Count} contraband items from {pawn.LabelShort}",
                pawn,
                MessageTypeDefOf.NeutralEvent
            );
        }
    }
}
