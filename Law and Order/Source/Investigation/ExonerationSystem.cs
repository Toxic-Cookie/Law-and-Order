using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Justice;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Handles exoneration of falsely accused pawns
    /// Clears criminal records, restores social standing, and punishes false accusers
    /// </summary>
    public static class ExonerationSystem
    {
        /// <summary>
        /// Exonerate a falsely accused pawn
        /// </summary>
        public static void ExoneratePawn(Pawn exonerated, Crime falseAccusationCrime, Pawn falseAccuser)
        {
            if (exonerated == null || falseAccusationCrime == null)
            {
                Mod.Log?.Warning("Cannot exonerate: null parameters");
                return;
            }

            Mod.Log?.Message($"[Exoneration] Exonerating {exonerated.LabelShort} for false accusation of {falseAccusationCrime.crimeType}");

            // Step 1: Remove the false crime from their record
            RemoveFalseCrime(exonerated, falseAccusationCrime);

            // Step 2: Remove or update the case
            UpdateCriminalCase(exonerated, falseAccusationCrime);

            // Step 3: Cancel any active punishment
            CancelFalsePunishment(exonerated, falseAccusationCrime);

            // Step 4: Apply social effects (restore reputation)
            ApplySocialRestoration(exonerated, falseAccusationCrime);

            // Step 5: Punish the false accuser
            if (falseAccuser != null)
            {
                PunishFalseAccuser(falseAccuser, exonerated, falseAccusationCrime);
            }

            // Step 6: Notify player
            NotifyExoneration(exonerated, falseAccusationCrime, falseAccuser);
        }

        /// <summary>
        /// Remove the false crime from the pawn's criminal record
        /// </summary>
        private static void RemoveFalseCrime(Pawn exonerated, Crime falseAccusationCrime)
        {
            var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(exonerated);
            if (criminalRecord == null)
                return;

            // Mark crime as dismissed/removed
            falseAccusationCrime.visibilityState = CrimeVisibilityState.Hidden;
            falseAccusationCrime.additionalInfo += " [EXONERATED]";

            // Note: We can't easily remove from the list due to IExposable structure
            // Instead, we mark it as exonerated and filter it in UI
        }

        /// <summary>
        /// Update or dismiss the criminal case
        /// </summary>
        private static void UpdateCriminalCase(Pawn exonerated, Crime falseAccusationCrime)
        {
            var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            if (justiceManager == null)
                return;

            var activeCase = justiceManager.GetOpenCases().FirstOrDefault(c => c.accused == exonerated);
            if (activeCase == null)
                return;

            // Remove the false crime from the case
            var associatedCrimes = activeCase.GetAssociatedCrimes();
            // Note: We can't directly remove from the case, but we've marked it as exonerated

            // If no visible crimes remain, dismiss the case
            var visibleCrimes = associatedCrimes.Where(c => c.IsVisibleInUI()).ToList();
            if (visibleCrimes.Count == 0)
            {
                activeCase.Dismiss();
                Mod.Log?.Message($"[Exoneration] Case #{activeCase.caseId} dismissed (no remaining crimes)");
            }
        }

        /// <summary>
        /// Cancel any active punishment for the false crime
        /// </summary>
        private static void CancelFalsePunishment(Pawn exonerated, Crime falseAccusationCrime)
        {
            var punishmentManager = Find.World.GetComponent<WorldComponent_PunishmentManager>();
            if (punishmentManager == null)
                return;

            // Find active punishment for this crime's case
            var activePunishments = punishmentManager.GetActivePunishments();
            var falsePunishment = activePunishments.FirstOrDefault(p =>
                p.criminal == exonerated &&
                p.caseId == falseAccusationCrime.caseId
            );

            if (falsePunishment != null)
            {
                // Cancel the punishment
                falsePunishment.status = Justice.PunishmentStatus.Failed;
                falsePunishment.notes = "Exonerated - False Accusation";

                Mod.Log?.Message($"[Exoneration] Cancelled {falsePunishment.type} punishment for {exonerated.LabelShort}");

                // Release from prison if imprisoned
                if (falsePunishment.type == Justice.PunishmentType.Imprisonment && exonerated.IsPrisoner)
                {
                    if (exonerated.guest != null)
                    {
                        exonerated.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
                        Mod.Log?.Message($"[Exoneration] Released {exonerated.LabelShort} from prison");
                    }
                }
            }
        }

        /// <summary>
        /// Apply social effects to restore the exonerated pawn's reputation
        /// </summary>
        private static void ApplySocialRestoration(Pawn exonerated, Crime falseAccusationCrime)
        {
            if (exonerated?.needs?.mood?.thoughts?.memories == null)
                return;

            // Add positive thought: JusticeRestored
            var justiceRestoredThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_JusticeRestored");
            if (justiceRestoredThought != null)
            {
                exonerated.needs.mood.thoughts.memories.TryGainMemory(justiceRestoredThought);
            }

            // Remove negative thoughts from false accusation
            var falselyAccusedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_FalselyAccused");
            if (falselyAccusedThought != null)
            {
                exonerated.needs.mood.thoughts.memories.RemoveMemoriesOfDef(falselyAccusedThought);
            }

            // Restore opinion with other colonists
            if (exonerated.Map != null)
            {
                foreach (var colonist in exonerated.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist == exonerated || colonist.relations == null)
                        continue;

                    // Small opinion boost for being proven innocent
                    if (colonist.needs?.mood?.thoughts?.memories != null)
                    {
                        // Use a generic positive social thought
                        colonist.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.RescuedMe, exonerated);
                    }
                }
            }

            Mod.Log?.Message($"[Exoneration] Social restoration applied for {exonerated.LabelShort}");
        }

        /// <summary>
        /// Punish the false accuser with a new crime: False Accusation
        /// </summary>
        private static void PunishFalseAccuser(Pawn falseAccuser, Pawn victim, Crime originalCrime)
        {
            if (falseAccuser == null || victim == null)
                return;

            // Create a new crime: False Accusation
            var falseAccusationCrime = new Crime
            {
                crimeType = CrimeType.FalseAccusation,
                tickCommitted = Find.TickManager.TicksGame,
                victim = victim,
                visibilityState = CrimeVisibilityState.Suspected,
                evidenceStrength = 1.0f, // Overwhelming evidence (truth discovered)
                witnesses = new List<Pawn>(), // Investigation revealed it
                location = falseAccuser.Position,
                additionalInfo = $"False accusation of {originalCrime.crimeType}"
            };

            // Add to false accuser's criminal record
            var accuserRecord = Utils.CrimeUtils.GetOrCreateCriminalRecord(falseAccuser);
            accuserRecord?.AddCrime(falseAccusationCrime);

            // Create or amend case
            var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            if (justiceManager != null)
            {
                var accuserCase = justiceManager.GetOrCreateCaseForPawn(falseAccuser);
                accuserCase.AddCrime(falseAccusationCrime);
            }

            // Apply social penalty
            ApplyFalseAccuserPenalty(falseAccuser, victim);

            Mod.Log?.Message($"[Exoneration] {falseAccuser.LabelShort} charged with False Accusation");
        }

        /// <summary>
        /// Apply social penalties to the exposed false accuser
        /// </summary>
        private static void ApplyFalseAccuserPenalty(Pawn falseAccuser, Pawn victim)
        {
            if (falseAccuser?.needs?.mood?.thoughts?.memories == null)
                return;

            // Add negative thought: ExposedAsLiar
            var exposedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_ExposedAsLiar");
            if (exposedThought != null)
            {
                falseAccuser.needs.mood.thoughts.memories.TryGainMemory(exposedThought);
            }

            // Colony-wide opinion penalty
            if (falseAccuser.Map != null)
            {
                foreach (var colonist in falseAccuser.Map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist == falseAccuser || colonist.relations == null)
                        continue;

                    // Significant opinion penalty
                    if (colonist.needs?.mood?.thoughts?.memories != null)
                    {
                        colonist.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.KnowPrisonerDiedInnocent, falseAccuser);
                    }
                }
            }

            // Victim gets satisfaction
            if (victim?.needs?.mood?.thoughts?.memories != null)
            {
                var justiceServedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_JusticeServed_Victim");
                if (justiceServedThought != null)
                {
                    victim.needs.mood.thoughts.memories.TryGainMemory(justiceServedThought);
                }
            }
        }

        /// <summary>
        /// Notify the player of the exoneration
        /// </summary>
        private static void NotifyExoneration(Pawn exonerated, Crime falseAccusationCrime, Pawn falseAccuser)
        {
            if (exonerated == null)
                return;

            // Send letter
            Find.LetterStack.ReceiveLetter(
                "LawAndOrder_Exoneration_Letter_Title".Translate(),
                "LawAndOrder_Exoneration_Letter_Text".Translate(
                    exonerated.LabelShort,
                    falseAccusationCrime.GetCrimeLabel(),
                    falseAccuser?.LabelShort ?? "Unknown"
                ),
                LetterDefOf.PositiveEvent,
                new LookTargets(new Pawn[] { exonerated, falseAccuser }.Where(p => p != null).ToArray())
            );

            // Message
            Messages.Message(
                "LawAndOrder_Exoneration_Message".Translate(
                    exonerated.LabelShort,
                    falseAccusationCrime.crimeType.ToString()
                ),
                new LookTargets(exonerated),
                MessageTypeDefOf.PositiveEvent
            );
        }

        /// <summary>
        /// Batch exonerate multiple crimes for a pawn
        /// </summary>
        public static void ExoneratePawnMultipleCrimes(Pawn exonerated, List<Crime> falseCrimes, Pawn falseAccuser)
        {
            if (exonerated == null || falseCrimes == null || !falseCrimes.Any())
                return;

            foreach (var crime in falseCrimes)
            {
                ExoneratePawn(exonerated, crime, falseAccuser);
            }

            Mod.Log?.Message($"[Exoneration] Batch exonerated {exonerated.LabelShort} for {falseCrimes.Count} false accusations");
        }

        /// <summary>
        /// Check if a pawn has any false accusations that should be exonerated
        /// Called from UI
        /// </summary>
        public static List<Crime> GetExonerableCrimes(Pawn pawn)
        {
            if (pawn == null)
                return new List<Crime>();

            var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(pawn);
            if (criminalRecord == null)
                return new List<Crime>();

            // Get all crimes marked as false accusations where truth was discovered
            return criminalRecord.Crimes
                .Where(c => c.isFalseAccusation &&
                           c.additionalInfo != null &&
                           c.additionalInfo.Contains("[Truth Discovered]"))
                .ToList();
        }

        /// <summary>
        /// Check if any exoneration actions are available for UI
        /// </summary>
        public static bool HasExonerableCrimes(Pawn pawn)
        {
            return GetExonerableCrimes(pawn).Any();
        }
    }
}
