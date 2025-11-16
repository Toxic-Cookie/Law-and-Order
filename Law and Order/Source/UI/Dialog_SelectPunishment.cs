using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// Dialog for selecting and customizing punishment for a convicted criminal.
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    public class Dialog_SelectPunishment : Window
    {
        private CriminalCase criminalCase;
        private Pawn criminal;
        private List<Crime> crimes;
        private Action onPunishmentAssigned;

        private PunishmentDef selectedPunishment = null;
        private int customDurationDays = 15;
        private int customFineAmount = 500;

        private Vector2 scrollPosition = Vector2.zero;
        private const float PADDING = 10f;
        private const float BUTTON_HEIGHT = 35f;

        public override Vector2 InitialSize => new Vector2(600f, 650f);

        public Dialog_SelectPunishment(CriminalCase criminalCase, List<Crime> convictedCrimes, Action onPunishmentAssigned = null)
        {
            this.criminalCase = criminalCase;
            this.criminal = criminalCase.accused;
            this.crimes = convictedCrimes;
            this.onPunishmentAssigned = onPunishmentAssigned;

            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, 40f);
            Widgets.Label(titleRect, $"Select Punishment: {criminal?.NameFullColored ?? "Unknown"}");
            Text.Font = GameFont.Small;

            // Crime summary
            Rect crimeRect = new Rect(0f, 45f, inRect.width, 60f);
            DrawCrimeSummary(crimeRect);

            // Punishment list
            Rect punishmentRect = new Rect(0f, 110f, inRect.width, 350f);
            DrawPunishmentList(punishmentRect);

            // Punishment customization (if applicable)
            Rect customizeRect = new Rect(0f, 465f, inRect.width, 100f);
            DrawPunishmentCustomization(customizeRect);

            // Buttons
            Rect buttonRect = new Rect(0f, inRect.height - BUTTON_HEIGHT - PADDING, inRect.width, BUTTON_HEIGHT);
            DrawButtons(buttonRect);
        }

        private void DrawCrimeSummary(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            string crimeText = $"Convicted of {crimes.Count} crime(s): ";
            var crimeTypes = crimes.Select(c => c.crimeType.ToString()).Distinct();
            crimeText += string.Join(", ", crimeTypes);

            // Get total severity
            int totalSeverity = crimes.Sum(c => GetCrimeSeverity(c.crimeType));
            crimeText += $"\n\nTotal Severity: {totalSeverity}";

            Text.Font = GameFont.Small;
            Widgets.Label(innerRect, crimeText);
        }

        private void DrawPunishmentList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(PADDING);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 25f), "Available Punishments:");

            Rect scrollRect = new Rect(innerRect.x, innerRect.y + 30f, innerRect.width, innerRect.height - 30f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, GetPunishmentDefs().Count() * 70f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float yPos = 0f;
            foreach (var punishmentDef in GetPunishmentDefs())
            {
                Rect entryRect = new Rect(0f, yPos, viewRect.width, 65f);
                DrawPunishmentEntry(entryRect, punishmentDef);
                yPos += 70f;
            }

            Widgets.EndScrollView();
        }

        private void DrawPunishmentEntry(Rect rect, PunishmentDef def)
        {
            bool isSelected = selectedPunishment == def;
            bool isAvailable = IsPunishmentAvailable(def);

            // Background
            if (isSelected)
            {
                Widgets.DrawHighlight(rect);
            }

            // Make unselectable if not available
            if (!isAvailable)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
            }

            Rect clickRect = rect.ContractedBy(2f);

            if (isAvailable && Widgets.ButtonInvisible(clickRect))
            {
                selectedPunishment = def;
                customDurationDays = def.defaultDurationDays;
                customFineAmount = def.defaultFineAmount;
            }

            // Content
            Rect labelRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;

            string label = def.punishmentLabel;
            if (IsRecommended(def))
            {
                label += " (Recommended)";
                GUI.color = new Color(0.8f, 1f, 0.8f);
            }

            Widgets.Label(labelRect, label);

            // Description
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect descRect = new Rect(rect.x + 5f, rect.y + 28f, rect.width - 10f, 32f);
            Widgets.Label(descRect, def.description);

            // Severity indicator
            Rect severityRect = new Rect(rect.x + rect.width - 60f, rect.y + 5f, 55f, 20f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(severityRect, $"Severity: {def.severityRating}/10");

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Tooltip for why unavailable
            if (!isAvailable)
            {
                TooltipHandler.TipRegion(rect, GetUnavailableReason(def));
            }
        }

        private void DrawPunishmentCustomization(Rect rect)
        {
            if (selectedPunishment == null)
                return;

            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(PADDING);

            Text.Font = GameFont.Small;

            switch (selectedPunishment.punishmentType)
            {
                case PunishmentType.Imprisonment:
                    Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 25f),
                        $"Imprisonment Duration: {customDurationDays} days");

                    Rect sliderRect = new Rect(innerRect.x, innerRect.y + 30f, innerRect.width, 30f);
                    customDurationDays = (int)Widgets.HorizontalSlider(sliderRect, customDurationDays, 1f, 120f, true, null, "1 day", "120 days");
                    break;

                case PunishmentType.Fine:
                    Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 25f),
                        $"Fine Amount: {customFineAmount} silver");

                    sliderRect = new Rect(innerRect.x, innerRect.y + 30f, innerRect.width, 30f);
                    customFineAmount = (int)Widgets.HorizontalSlider(sliderRect, customFineAmount, 50f, 5000f, true, null, "50", "5000");
                    break;

                case PunishmentType.Beating:
                case PunishmentType.Execution:
                case PunishmentType.Exile:
                    Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 50f),
                        $"This punishment will be carried out immediately upon confirmation.\n{selectedPunishment.description}");
                    break;
            }
        }

        private void DrawButtons(Rect rect)
        {
            float buttonWidth = (rect.width - PADDING) / 2f;

            // Cancel button
            Rect cancelRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(cancelRect, "Cancel"))
            {
                Close();
            }

            // Confirm button
            Rect confirmRect = new Rect(rect.x + buttonWidth + PADDING, rect.y, buttonWidth, rect.height);

            if (selectedPunishment == null)
            {
                GUI.enabled = false;
            }

            if (Widgets.ButtonText(confirmRect, "Assign Punishment"))
            {
                AssignPunishment();
                Close();
            }

            GUI.enabled = true;
        }

        private void AssignPunishment()
        {
            if (selectedPunishment == null || criminal == null)
                return;

            // Create punishment
            Punishment punishment = new Punishment(selectedPunishment.punishmentType, criminal, criminalCase.caseId);

            // Apply customization
            switch (selectedPunishment.punishmentType)
            {
                case PunishmentType.Imprisonment:
                    punishment.durationDays = customDurationDays;
                    break;

                case PunishmentType.Fine:
                    punishment.fineAmount = customFineAmount;
                    break;
            }

            // Add to punishment manager
            var punishmentManager = WorldComponent_PunishmentManager.Instance;
            if (punishmentManager != null)
            {
                punishmentManager.AddPunishment(punishment);

                Messages.Message(
                    $"Punishment assigned: {selectedPunishment.punishmentLabel} for {criminal.NameShortColored}",
                    criminal,
                    MessageTypeDefOf.TaskCompletion
                );

                // Call the callback to finalize conviction
                onPunishmentAssigned?.Invoke();
            }
            else
            {
                Mod.Log?.Error("PunishmentManager not found!");
            }
        }

        private IEnumerable<PunishmentDef> GetPunishmentDefs()
        {
            return DefDatabase<PunishmentDef>.AllDefs.OrderBy(def => def.severityRating);
        }

        private bool IsPunishmentAvailable(PunishmentDef def)
        {
            if (criminal == null)
                return false;

            if (def.requiresAlive && criminal.Dead)
                return false;

            if (def.requiresOnMap && !criminal.Spawned)
                return false;

            if (def.requiresImprisoned && !criminal.IsPrisoner)
                return false;

            // Check minimum crime severity
            int totalSeverity = crimes.Sum(c => GetCrimeSeverity(c.crimeType));
            if (totalSeverity < def.minimumCrimeSeverity)
                return false;

            return true;
        }

        private string GetUnavailableReason(PunishmentDef def)
        {
            if (criminal == null)
                return "No criminal found.";

            if (def.requiresAlive && criminal.Dead)
                return "Criminal is dead.";

            if (def.requiresOnMap && !criminal.Spawned)
                return "Criminal must be on the map.";

            if (def.requiresImprisoned && !criminal.IsPrisoner)
                return "Criminal must be imprisoned first.";

            int totalSeverity = crimes.Sum(c => GetCrimeSeverity(c.crimeType));
            if (totalSeverity < def.minimumCrimeSeverity)
                return $"Crime severity too low (need {def.minimumCrimeSeverity}, have {totalSeverity}).";

            return "Not available.";
        }

        private bool IsRecommended(PunishmentDef def)
        {
            // Recommend punishment based on total crime severity
            int totalSeverity = crimes.Sum(c => GetCrimeSeverity(c.crimeType));
            int recommendedSeverity = totalSeverity / 10; // Rough conversion

            return Math.Abs(def.severityRating - recommendedSeverity) <= 2;
        }

        private int GetCrimeSeverity(CrimeType type)
        {
            return type switch
            {
                CrimeType.Murder => 100,
                CrimeType.Kidnapping => 90,
                CrimeType.Assault => 80,
                CrimeType.AnimalAbuse => 70,
                CrimeType.Arson => 60,
                CrimeType.PropertyDestruction => 50,
                CrimeType.Theft => 40,
                CrimeType.Vandalism => 30,
                CrimeType.Trespassing => 20,
                CrimeType.ContrabandPossession => 10,
                _ => 0
            };
        }
    }
}
