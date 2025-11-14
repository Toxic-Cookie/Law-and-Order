using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Settings
{
    /// <summary>
    /// Custom settings window for Law and Order mod settings
    /// Provides organized sections and a recalculate debt button
    /// </summary>
    public class LawAndOrderSettingsWindow : Window
    {
        private Vector2 scrollPosition;
        private const float RowHeight = 40f;
        private const float LabelWidth = 400f;
        private const float InputWidth = 100f;
        private const float Padding = 10f;

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public LawAndOrderSettingsWindow()
        {
            doCloseButton = true;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();

            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
            Widgets.Label(titleRect, "Law and Order - Debt Settings");
            Text.Font = GameFont.Small;

            // Main content area (with scrolling)
            Rect scrollRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width, inRect.height - titleRect.height - 120f);
            Rect scrollViewRect = new Rect(0f, 0f, scrollRect.width - 20f, 900f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, scrollViewRect);
            listing.Begin(scrollViewRect);

            // Crime Penalties Section - Now configured in Crimes Tab
            DrawSectionHeader(listing, "Crime Penalties");

            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 1f, 0.7f); // Light yellow for info
            listing.Label("Crime penalties are now configured in the Justice Menu → Crimes Tab.");
            listing.Label("Press 'J' or click Justice on the main toolbar to access the Crimes Tab.");
            GUI.color = Color.white;
            listing.Gap(12f);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("The Crimes Tab provides fine-grained control over individual crime penalties,");
            listing.Label("penalty ranges for contextual variation, and configurable multipliers.");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            listing.Gap(20f);

            // Labor Settings Section
            DrawSectionHeader(listing, "Labor Settings");
            DrawSetting(listing, "Default Silver per Day (labor)", LawAndOrderSettings.DefaultSilverPerDay, "silver/day");

            listing.Gap(20f);

            // Debug Settings Section
            DrawSectionHeader(listing, "Debug Settings");
            if (listing.ButtonTextLabeled("Log Level", LawAndOrderSettings.LogLevel.Value.ToString()))
            {
                // Cycle through log levels
                var values = System.Enum.GetValues(typeof(Law_and_Order.Source.Utils.LogLevel));
                int currentIndex = System.Array.IndexOf(values, LawAndOrderSettings.LogLevel.Value);
                int nextIndex = (currentIndex + 1) % values.Length;
                LawAndOrderSettings.LogLevel.Value = (Law_and_Order.Source.Utils.LogLevel)values.GetValue(nextIndex);
            }
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("Controls verbosity of mod logging. Use 'Debug' or 'Trace' for troubleshooting.");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            listing.End();
            Widgets.EndScrollView();

            // Bottom buttons
            Rect buttonAreaRect = new Rect(inRect.x, scrollRect.yMax + 10f, inRect.width, 100f);
            DrawBottomButtons(buttonAreaRect);
        }

        private void DrawSectionHeader(Listing_Standard listing, string title)
        {
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.8f, 1f);
            listing.Label(title);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.GapLine(6f);
        }

        private void DrawSetting(Listing_Standard listing, string label, HugsLib.Settings.SettingHandle<float> setting, string suffix)
        {
            Rect lineRect = listing.GetRect(RowHeight);

            // Label
            Rect labelRect = new Rect(lineRect.x, lineRect.y, LabelWidth, lineRect.height);
            Widgets.Label(labelRect, label);

            // Current value display
            Rect valueRect = new Rect(labelRect.xMax + 10f, lineRect.y, 150f, lineRect.height);
            string valueText = $"{setting.Value:F0} {suffix}";
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = new Color(0.7f, 1f, 0.7f);
            Widgets.Label(valueRect, valueText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // Slider
            Rect sliderRect = new Rect(valueRect.xMax + 10f, lineRect.y + 10f, lineRect.width - labelRect.width - valueRect.width - 20f, 20f);
            float newValue = setting.Value;

            // Determine slider range based on setting type
            float min = 0f;
            float max = 10000f;

            if (label.Contains("Multiplier") || label.Contains("Modifier"))
            {
                min = 1f;
                max = 5f;
            }
            else if (label.Contains("Murder"))
            {
                max = 50000f;
            }
            else if (label.Contains("per Day"))
            {
                min = 1f;
                max = 500f;
            }

            newValue = Widgets.HorizontalSlider(sliderRect, newValue, min, max, true);

            if (newValue != setting.Value)
            {
                setting.Value = newValue;
            }
        }

        private void DrawBottomButtons(Rect rect)
        {
            float buttonWidth = 200f;
            float buttonHeight = 40f;
            float spacing = 10f;

            // Reset to Defaults button
            Rect resetButtonRect = new Rect(rect.x, rect.y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(resetButtonRect, "Reset to Defaults"))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "Are you sure you want to reset all debt values to their defaults?",
                    delegate
                    {
                        LawAndOrderSettings.ResetToDefaults();
                        Messages.Message("Debt settings reset to defaults", MessageTypeDefOf.TaskCompletion);
                    },
                    destructive: false
                ));
            }

            // Recalculate All Debt button
            Rect recalcButtonRect = new Rect(resetButtonRect.xMax + spacing, rect.y, buttonWidth + 50f, buttonHeight);
            if (Widgets.ButtonText(recalcButtonRect, "Recalculate All Debt"))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "This will recalculate debt for all criminals based on the current settings. This cannot be undone. Continue?",
                    delegate
                    {
                        RecalculateAllDebt();
                    },
                    destructive: false
                ));
            }

            // Info text
            Rect infoRect = new Rect(rect.x, resetButtonRect.yMax + 10f, rect.width, 40f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(infoRect, "Note: Use 'Recalculate All Debt' after changing settings to update existing criminals' debt.");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void RecalculateAllDebt()
        {
            int count = 0;
            int totalCriminals = 0;

            // Get all pawns with criminal records
            var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive;

            foreach (var pawn in allPawns)
            {
                var criminalRecord = CrimeUtils.TryGetCriminalRecord(pawn);
                if (criminalRecord != null && criminalRecord.TotalCrimeCount > 0)
                {
                    totalCriminals++;

                    // TODO Phase 1: Replace with state/case recalculation
                    // Old debt system removed - will be replaced with case-based punishment system
                    count++;
                }
            }

            Messages.Message(
                $"Reapplied debt for {count} criminals (out of {totalCriminals} total criminals)",
                MessageTypeDefOf.TaskCompletion
            );

            Mod.Log?.Message($"Debt reapplied for {count} pawns");
        }
    }
}
