using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// Main tab window for the Law and Order justice system.
    /// Shows open cases, convicted cases, and settings.
    /// Phase 3: Basic UI Implementation
    /// </summary>
    public class MainTabWindow_Justice : MainTabWindow
    {
        private enum JusticeTab
        {
            OpenCases,
            Convictions,
            Settings
        }

        // UI State
        private JusticeTab currentTab = JusticeTab.OpenCases;
        private CriminalCase selectedCase = null;
        private HashSet<Crime> selectedCrimes = new HashSet<Crime>();
        private Vector2 caseListScrollPos = Vector2.zero;
        private Vector2 crimeListScrollPos = Vector2.zero;
        private Vector2 convictionsScrollPos = Vector2.zero;

        // Settings state (god mode only)
        private bool showHiddenCrimes = false;
        private bool autoConvictRedHanded = false;
        private bool enableFalseAccusations = false;
        private int statuteOfLimitationsDays = 60;

        // UI Constants
        private const float CASE_LIST_WIDTH = 300f;
        private const float PADDING = 10f;
        private const float ROW_HEIGHT = 40f;
        private const float CRIME_ROW_HEIGHT = 50f;
        private const float BUTTON_HEIGHT = 35f;

        public override Vector2 InitialSize => new Vector2(1000f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            // Reserve space at the top for tabs (TabDrawer.DrawTabs draws ABOVE the rect you pass it)
            // Following RimWorld's pattern from MainTabWindow_Quests
            Rect mainRect = inRect;
            mainRect.yMin += 32f; // Reserve space for tabs

            // Draw tabs - TabDrawer will draw them above mainRect
            DrawTabs(mainRect);

            // Content area - starts after the tabs
            Rect contentRect = new Rect(0f, mainRect.yMin + PADDING, inRect.width, inRect.height - mainRect.yMin - PADDING);

            switch (currentTab)
            {
                case JusticeTab.OpenCases:
                    DrawOpenCasesTab(contentRect);
                    break;
                case JusticeTab.Convictions:
                    DrawConvictionsTab(contentRect);
                    break;
                case JusticeTab.Settings:
                    DrawSettingsTab(contentRect);
                    break;
            }
        }

        private void DrawTabs(Rect rect)
        {
            float tabWidth = rect.width / 3f;

            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("LawAndOrder_Tab_OpenCases".Translate(),
                    () => { currentTab = JusticeTab.OpenCases; selectedCase = null; selectedCrimes.Clear(); },
                    currentTab == JusticeTab.OpenCases),
                new TabRecord("LawAndOrder_Tab_Convictions".Translate(),
                    () => { currentTab = JusticeTab.Convictions; selectedCase = null; selectedCrimes.Clear(); },
                    currentTab == JusticeTab.Convictions),
                new TabRecord("LawAndOrder_Tab_Settings".Translate(),
                    () => { currentTab = JusticeTab.Settings; },
                    currentTab == JusticeTab.Settings)
            };

            TabDrawer.DrawTabs(rect, tabs);
        }

        #region Open Cases Tab

        private void DrawOpenCasesTab(Rect rect)
        {
            var justiceManager = WorldComponent_JusticeManager.Instance;
            if (justiceManager == null)
            {
                Widgets.Label(rect, "Error: Justice Manager not found");
                return;
            }

            var openCases = justiceManager.GetOpenCases();

            // Sort by severity (most serious crime first)
            openCases = openCases.OrderByDescending(c => GetCaseSeverity(c)).ToList();

            // Split into case list (left) and details (right)
            Rect caseListRect = new Rect(rect.x, rect.y, CASE_LIST_WIDTH, rect.height);
            Rect detailsRect = new Rect(rect.x + CASE_LIST_WIDTH + PADDING, rect.y,
                rect.width - CASE_LIST_WIDTH - PADDING, rect.height);

            // Draw case list
            DrawCaseList(caseListRect, openCases);

            // Draw case details
            if (selectedCase != null)
            {
                DrawCaseDetails(detailsRect, selectedCase);
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(detailsRect, "LawAndOrder_SelectCase".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawCaseList(Rect rect, List<CriminalCase> cases)
        {
            // Header
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 30f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect.ContractedBy(5f), $"{"LawAndOrder_OpenCases".Translate()} ({cases.Count})");
            Text.Anchor = TextAnchor.UpperLeft;

            // Case list
            Rect listRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, cases.Count * ROW_HEIGHT);

            Widgets.BeginScrollView(listRect, ref caseListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var caseItem in cases)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, ROW_HEIGHT);
                DrawCaseListEntry(rowRect, caseItem);
                yPos += ROW_HEIGHT;
            }

            Widgets.EndScrollView();
        }

        private void DrawCaseListEntry(Rect rect, CriminalCase caseItem)
        {
            bool isSelected = selectedCase == caseItem;

            if (isSelected)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.5f, 0.5f, 0.2f, 0.3f));
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                selectedCase = caseItem;
                selectedCrimes.Clear();
            }

            Rect contentRect = rect.ContractedBy(5f);

            // Save text state
            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;

            // Case number and accused name at top
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            string label = $"#{caseItem.caseId} - {caseItem.accused?.NameShortColored ?? "Unknown"}";
            Rect nameRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 18f);
            Widgets.Label(nameRect, label.Truncate(nameRect.width));

            // Crime count and days open at bottom
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            var crimes = caseItem.GetAssociatedCrimes();
            string info = $"{crimes.Count} {"LawAndOrder_Crimes".Translate()} - {caseItem.DaysOpen}d";
            Rect infoRect = new Rect(contentRect.x, contentRect.y + 18f, contentRect.width, 15f);
            Widgets.Label(infoRect, info.Truncate(infoRect.width));

            // Restore text state
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        private void DrawCaseDetails(Rect rect, CriminalCase caseItem)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(PADDING);

            float yPos = 0f;

            // Accused info section
            yPos = DrawAccusedInfo(innerRect, caseItem, yPos);

            yPos += PADDING;

            // Crimes section header - position relative to innerRect
            Rect crimesHeaderRect = new Rect(innerRect.x, innerRect.y + yPos, innerRect.width, 25f);
            Text.Font = GameFont.Small;
            var crimes = caseItem.GetAssociatedCrimes();
            Widgets.Label(crimesHeaderRect, $"{"LawAndOrder_CrimesCommitted".Translate()} ({crimes.Count})");
            yPos += 25f;

            // Crime list with checkboxes - position relative to innerRect
            Rect crimeListRect = new Rect(innerRect.x, innerRect.y + yPos, innerRect.width, innerRect.height - yPos - BUTTON_HEIGHT - PADDING * 2);
            DrawCrimeList(crimeListRect, crimes);
            yPos += crimeListRect.height + PADDING;

            // Action buttons - position relative to innerRect
            DrawActionButtons(new Rect(innerRect.x, innerRect.y + yPos, innerRect.width, BUTTON_HEIGHT));
        }

        private float DrawAccusedInfo(Rect rect, CriminalCase caseItem, float yPos)
        {
            Pawn accused = caseItem.accused;
            if (accused == null)
            {
                Widgets.Label(new Rect(rect.x, rect.y + yPos, rect.width, 25f), "Unknown Accused");
                return yPos + 25f;
            }

            // Portrait (left) - position relative to rect
            Rect portraitRect = new Rect(rect.x, rect.y + yPos, 80f, 80f);
            Widgets.ThingIcon(portraitRect, accused);

            // Info (right of portrait) - position relative to rect
            Rect infoRect = new Rect(rect.x + 90f, rect.y + yPos, rect.width - 90f, 80f);

            // Save text state
            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect nameRect = new Rect(infoRect.x, infoRect.y, infoRect.width, 30f);
            Widgets.Label(nameRect, accused.NameFullColored.Truncate(nameRect.width));

            Text.Font = GameFont.Small;
            Rect statusRect = new Rect(infoRect.x, infoRect.y + 30f, infoRect.width, 20f);
            string status = GetPawnStatusLabel(accused);
            Widgets.Label(statusRect, status.Truncate(statusRect.width));

            Rect caseInfoRect = new Rect(infoRect.x, infoRect.y + 50f, infoRect.width, 20f);
            string caseInfo = "LawAndOrder_CaseOpenedDaysAgo".Translate(caseItem.DaysOpen);
            Widgets.Label(caseInfoRect, caseInfo.Truncate(caseInfoRect.width));

            // Restore text state
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;

            return yPos + 80f;
        }

        private void DrawCrimeList(Rect rect, List<Crime> crimes)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, crimes.Count * CRIME_ROW_HEIGHT);

            Widgets.BeginScrollView(rect, ref crimeListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var crime in crimes)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, CRIME_ROW_HEIGHT);
                DrawCrimeEntry(rowRect, crime);
                yPos += CRIME_ROW_HEIGHT;
            }

            Widgets.EndScrollView();
        }

        private void DrawCrimeEntry(Rect rect, Crime crime)
        {
            bool isSelected = selectedCrimes.Contains(crime);

            // Background
            if (isSelected)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.4f, 0.6f, 0.3f));
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Widgets.DrawBox(rect);

            Rect contentRect = rect.ContractedBy(5f);

            // Checkbox (left)
            Rect checkboxRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - 24f) / 2f, 24f, 24f);
            bool wasSelected = isSelected;
            Widgets.Checkbox(checkboxRect.position, ref isSelected, 24f);

            if (isSelected != wasSelected)
            {
                if (isSelected)
                    selectedCrimes.Add(crime);
                else
                    selectedCrimes.Remove(crime);
            }

            // State label
            Rect stateLabelRect = new Rect(contentRect.x + 30f, contentRect.y, 100f, 20f);
            DrawStateLabel(stateLabelRect, crime.visibilityState);

            // Crime type and details - ensure proper bounds
            float crimeInfoX = contentRect.x + 140f;
            float crimeInfoWidth = Mathf.Max(0f, contentRect.width - 140f);
            Rect crimeInfoRect = new Rect(crimeInfoX, contentRect.y, crimeInfoWidth, contentRect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            string crimeLabel = crime.GetCrimeLabel();
            Rect crimeLabelRect = new Rect(crimeInfoRect.x, crimeInfoRect.y, crimeInfoRect.width, 20f);
            Widgets.Label(crimeLabelRect, crimeLabel.Truncate(crimeLabelRect.width));

            Text.Font = GameFont.Tiny;
            string details = GetCrimeDetailsString(crime);
            Rect detailsRect = new Rect(crimeInfoRect.x, crimeInfoRect.y + 20f, crimeInfoRect.width, 20f);
            Widgets.Label(detailsRect, details.Truncate(detailsRect.width));

            // Evidence strength bar
            if (crime.evidenceStrength > 0f)
            {
                Rect evidenceRect = new Rect(contentRect.x + 30f, contentRect.y + 25f, 100f, 10f);
                Widgets.FillableBar(evidenceRect, crime.evidenceStrength, Texture2D.linearGrayTexture, null, false);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(evidenceRect, $"{crime.evidenceStrength:P0}");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawStateLabel(Rect rect, CrimeVisibilityState state)
        {
            Color labelColor = state switch
            {
                CrimeVisibilityState.Hidden => Color.gray,
                CrimeVisibilityState.Suspected => new Color(1f, 0.8f, 0f), // Yellow/orange
                CrimeVisibilityState.Convicted => new Color(1f, 0.3f, 0.3f), // Red
                _ => Color.white
            };

            string label = state switch
            {
                CrimeVisibilityState.Hidden => "LawAndOrder_State_Hidden".Translate(),
                CrimeVisibilityState.Suspected => "LawAndOrder_State_Suspected".Translate(),
                CrimeVisibilityState.Convicted => "LawAndOrder_State_Convicted".Translate(),
                _ => "Unknown"
            };

            // Save current state
            var oldColor = GUI.color;
            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;

            GUI.color = labelColor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, $"[{label.Truncate(rect.width)}]");

            // Restore state
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private void DrawActionButtons(Rect rect)
        {
            float buttonWidth = (rect.width - PADDING * 2) / 3f;

            // Convict Selected button
            Rect convictRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            bool canConvict = selectedCrimes.Any(c => c.visibilityState == CrimeVisibilityState.Suspected);

            if (!canConvict)
            {
                GUI.enabled = false;
            }

            if (Widgets.ButtonText(convictRect, "LawAndOrder_ConvictSelected".Translate()))
            {
                ConvictSelectedCrimes();
            }

            GUI.enabled = true;

            // Dismiss Selected button
            Rect dismissRect = new Rect(rect.x + buttonWidth + PADDING, rect.y, buttonWidth, rect.height);
            bool canDismiss = selectedCrimes.Any(c => c.visibilityState == CrimeVisibilityState.Suspected);

            if (!canDismiss)
            {
                GUI.enabled = false;
            }

            if (Widgets.ButtonText(dismissRect, "LawAndOrder_DismissSelected".Translate()))
            {
                DismissSelectedCrimes();
            }

            GUI.enabled = true;

            // Investigate button - starts interrogation of selected prisoner
            Rect investigateRect = new Rect(rect.x + (buttonWidth + PADDING) * 2, rect.y, buttonWidth, rect.height);

            // Enable only if we have a selected case with an interrogatable prisoner
            bool canInvestigate = selectedCase != null &&
                                  selectedCase.accused != null &&
                                  Investigation.InterrogationSystem.CanInterrogate(selectedCase.accused);

            GUI.enabled = canInvestigate;
            if (Widgets.ButtonText(investigateRect, "LawAndOrder_Investigate".Translate()))
            {
                StartInvestigation();
            }
            GUI.enabled = true;
        }

        #endregion

        #region Convictions Tab

        private void DrawConvictionsTab(Rect rect)
        {
            var justiceManager = WorldComponent_JusticeManager.Instance;
            if (justiceManager == null)
            {
                Widgets.Label(rect, "Error: Justice Manager not found");
                return;
            }

            var convictedCases = justiceManager.GetConvictedCases();

            // Sort by conviction date (most recent first)
            convictedCases = convictedCases.OrderByDescending(c => c.tickClosed).ToList();

            if (convictedCases.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_NoConvictions".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Scrollable list of convicted cases
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, convictedCases.Count * 100f);

            Widgets.BeginScrollView(rect, ref convictionsScrollPos, viewRect);

            float yPos = 0f;
            foreach (var caseItem in convictedCases)
            {
                Rect entryRect = new Rect(0f, yPos, viewRect.width, 95f);
                DrawConvictedCaseEntry(entryRect, caseItem);
                yPos += 100f;
            }

            Widgets.EndScrollView();
        }

        private void DrawConvictedCaseEntry(Rect rect, CriminalCase caseItem)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Accused name
            Text.Font = GameFont.Small;
            Rect nameRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Widgets.Label(nameRect, $"#{caseItem.caseId} - {caseItem.accused?.NameFullColored ?? "Unknown"}");

            // Crime count
            var crimes = caseItem.GetAssociatedCrimes();
            Rect crimeCountRect = new Rect(innerRect.x, innerRect.y + 25f, innerRect.width / 2f, 20f);
            Widgets.Label(crimeCountRect, $"{crimes.Count} {"LawAndOrder_CrimesConvicted".Translate()}");

            // Conviction date
            Rect dateRect = new Rect(innerRect.x, innerRect.y + 45f, innerRect.width / 2f, 20f);
            int daysAgo = (Find.TickManager.TicksGame - caseItem.tickClosed) / GenDate.TicksPerDay;
            Widgets.Label(dateRect, $"{"LawAndOrder_ConvictedDaysAgo".Translate(daysAgo)}");

            // Punishment status (Phase 4)
            Text.Font = GameFont.Tiny;
            Rect punishmentRect = new Rect(innerRect.x + innerRect.width / 2f, innerRect.y + 25f, innerRect.width / 2f, 40f);

            // Get punishments for this case
            var punishmentManager = Components.WorldComponent_PunishmentManager.Instance;
            if (punishmentManager != null)
            {
                var punishments = punishmentManager.GetPunishmentsForCase(caseItem.caseId);

                if (punishments.Count > 0)
                {
                    string punishmentText = "";

                    foreach (var punishment in punishments.Take(3))
                    {
                        string statusStr = punishment.status switch
                        {
                            Justice.PunishmentStatus.Pending => "LawAndOrder_Status_Pending".Translate(),
                            Justice.PunishmentStatus.Active => "LawAndOrder_Status_Active".Translate(),
                            Justice.PunishmentStatus.Completed => "LawAndOrder_Status_Completed".Translate(),
                            Justice.PunishmentStatus.Failed => "LawAndOrder_Status_Failed".Translate(),
                            _ => "Unknown"
                        };

                        string typeStr = punishment.type.ToString();

                        // Add additional info for some punishment types
                        string additionalInfo = "";
                        if (punishment.type == Justice.PunishmentType.Imprisonment && punishment.status == Justice.PunishmentStatus.Active)
                        {
                            int remainingDays = punishment.GetRemainingDays();
                            additionalInfo = $" ({remainingDays}d left)";
                        }
                        else if (punishment.type == Justice.PunishmentType.Fine && punishment.status == Justice.PunishmentStatus.Active)
                        {
                            int remainingFine = punishment.GetRemainingFine();
                            additionalInfo = $" ({remainingFine}$ left)";
                        }

                        punishmentText += $"{typeStr}: {statusStr}{additionalInfo}\n";
                    }

                    if (punishments.Count > 3)
                    {
                        punishmentText += $"+{punishments.Count - 3} more";
                    }

                    Widgets.Label(punishmentRect, punishmentText);
                }
                else
                {
                    Widgets.Label(punishmentRect, "LawAndOrder_PunishmentPending".Translate());
                }
            }
            else
            {
                Widgets.Label(punishmentRect, "LawAndOrder_PunishmentPending".Translate());
            }

            Text.Font = GameFont.Small;
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab(Rect rect)
        {
            // Only show in god mode
            if (!DebugSettings.godMode)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_SettingsGodModeOnly".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(PADDING);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(innerRect);

            // Header
            Text.Font = GameFont.Medium;
            listing.Label("LawAndOrder_DebugSettings".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();

            // Show hidden crimes
            listing.CheckboxLabeled("LawAndOrder_Setting_ShowHiddenCrimes".Translate(), ref showHiddenCrimes,
                "LawAndOrder_Setting_ShowHiddenCrimes_Desc".Translate());
            listing.Gap();

            // Auto-convict caught red-handed
            bool prevAutoConvict = autoConvictRedHanded;
            listing.CheckboxLabeled("LawAndOrder_Setting_AutoConvictRedHanded".Translate(), ref autoConvictRedHanded,
                "LawAndOrder_Setting_AutoConvictRedHanded_Desc".Translate());

            // Sync with JusticeManager when changed
            if (prevAutoConvict != autoConvictRedHanded)
            {
                Components.WorldComponent_JusticeManager.SetAutoConvictSetting(autoConvictRedHanded);
            }

            listing.Gap();

            // Enable false accusations
            listing.CheckboxLabeled("LawAndOrder_Setting_EnableFalseAccusations".Translate(), ref enableFalseAccusations,
                "LawAndOrder_Setting_EnableFalseAccusations_Desc".Translate());
            listing.Gap();

            // Statute of limitations
            listing.Label($"{"LawAndOrder_Setting_StatuteOfLimitations".Translate()}: {statuteOfLimitationsDays} {"LawAndOrder_Days".Translate()}");
            statuteOfLimitationsDays = (int)listing.Slider(statuteOfLimitationsDays, 0, 120);
            Text.Font = GameFont.Tiny;
            listing.Label("LawAndOrder_Setting_StatuteOfLimitations_Desc".Translate());
            Text.Font = GameFont.Small;

            listing.End();
        }

        #endregion

        #region Action Methods

        private void ConvictSelectedCrimes()
        {
            if (selectedCase == null || selectedCrimes.Count == 0)
                return;

            // Get crimes to convict
            var crimesToConvict = selectedCrimes.Where(c => c.visibilityState == CrimeVisibilityState.Suspected).ToList();

            if (crimesToConvict.Count == 0)
                return;

            // Open punishment selection dialog with callback
            // Only mark crimes as convicted if punishment is actually assigned
            Dialog_SelectPunishment dialog = new Dialog_SelectPunishment(selectedCase, crimesToConvict, () =>
            {
                // This callback is called when punishment is successfully assigned
                // Mark crimes as convicted
                foreach (var crime in crimesToConvict)
                {
                    crime.TransitionToConvicted();
                }

                // If all crimes in case are convicted, mark case as convicted
                var allCrimes = selectedCase.GetAssociatedCrimes();
                if (allCrimes.All(c => c.visibilityState == CrimeVisibilityState.Convicted))
                {
                    selectedCase.Convict();
                }
            });

            Find.WindowStack.Add(dialog);

            selectedCrimes.Clear();
        }

        private void DismissSelectedCrimes()
        {
            if (selectedCase == null || selectedCrimes.Count == 0)
                return;

            int dismissedCount = 0;
            foreach (var crime in selectedCrimes.ToList())
            {
                if (crime.visibilityState == CrimeVisibilityState.Suspected)
                {
                    // Remove crime from case (but keep on record)
                    selectedCase.crimeTicksCommitted.Remove(crime.tickCommitted);
                    crime.caseId = -1;
                    dismissedCount++;
                }
            }

            // If all crimes dismissed, dismiss the case
            var remainingCrimes = selectedCase.GetAssociatedCrimes();
            if (remainingCrimes.Count == 0)
            {
                // Apply social impact for unpunished criminal
                Utils.SocialImpactUtils.ApplyUnpunishedImpact(selectedCase);

                selectedCase.Dismiss();
                selectedCase = null;
            }

            selectedCrimes.Clear();

            Messages.Message($"{"LawAndOrder_CrimesDismissedMessage".Translate(dismissedCount)}", MessageTypeDefOf.TaskCompletion);
        }

        /// <summary>
        /// Start an investigation of the selected case by interrogating the prisoner
        /// </summary>
        private void StartInvestigation()
        {
            if (selectedCase == null || selectedCase.accused == null)
            {
                return;
            }

            Pawn prisoner = selectedCase.accused;

            // Check if prisoner can be interrogated
            if (!Investigation.InterrogationSystem.CanInterrogate(prisoner))
            {
                Messages.Message("LawAndOrder_CannotInterrogate".Translate(prisoner.LabelShort),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Find a warden to do the interrogation (need warden first to check accessibility)
            Pawn warden = FindAvailableWarden(prisoner.Map);

            if (warden == null)
            {
                Messages.Message("LawAndOrder_NoWardenAvailable".Translate(),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Find interrogation tables on the map
            var tables = Investigation.Comp_InterrogationTable.GetAllInterrogationTables(prisoner.Map);

            if (tables.Count == 0)
            {
                Messages.Message("LawAndOrder_NoInterrogationTable".Translate(),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Find nearest table that the WARDEN can reach (not prisoner)
            Thing table = Investigation.Comp_InterrogationTable.FindNearestInterrogationTable(warden, prisoner);

            if (table == null)
            {
                Messages.Message("LawAndOrder_NoAccessibleTable".Translate(),
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Create and assign interrogation job to warden
            Job interrogationJob = JobMaker.MakeJob(
                Investigation.JobDefOf_LawAndOrder.LawAndOrder_InvestigateCase,
                prisoner,
                table
            );

            warden.jobs.TryTakeOrderedJob(interrogationJob, JobTag.Misc);

            Messages.Message("LawAndOrder_InterrogationStarted".Translate(warden.LabelShort, prisoner.LabelShort),
                prisoner,
                MessageTypeDefOf.TaskCompletion);

            // Close the justice window (player can reopen to see results)
            this.Close();
        }

        /// <summary>
        /// Find an available warden on the map who can perform interrogation
        /// </summary>
        private Pawn FindAvailableWarden(Map map)
        {
            // Find colonists with Warden work enabled
            return map.mapPawns.FreeColonists
                .Where(p => p.workSettings != null &&
                           p.workSettings.WorkIsActive(WorkTypeDefOf.Warden) &&
                           !p.Downed &&
                           !p.Dead &&
                           p.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                           p.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Social).Level)
                .FirstOrDefault();
        }

        #endregion

        #region Helper Methods

        private int GetCaseSeverity(CriminalCase caseItem)
        {
            var mostSerious = caseItem.GetMostSeriousCrime();
            return mostSerious switch
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

        private string GetPawnStatusLabel(Pawn pawn)
        {
            if (pawn.Dead)
                return "LawAndOrder_StatusDead".Translate();
            if (pawn.IsPrisonerOfColony)
                return "LawAndOrder_StatusImprisoned".Translate();
            if (pawn.IsColonist)
                return "LawAndOrder_StatusColonist".Translate();
            if (pawn.Faction != null)
                return "LawAndOrder_StatusFaction".Translate(pawn.Faction.Name);
            return "LawAndOrder_StatusUnknown".Translate();
        }

        private string GetCrimeDetailsString(Crime crime)
        {
            List<string> parts = new List<string>();

            if (crime.victim != null)
            {
                parts.Add($"{"LawAndOrder_Victim".Translate()}: {crime.victim.NameShortColored}");
            }

            if (crime.witnesses.Count > 0)
            {
                parts.Add($"{crime.witnesses.Count} {"LawAndOrder_Witnesses".Translate()}");
            }

            parts.Add($"{crime.DaysAgo} {"LawAndOrder_DaysAgo".Translate()}");

            return string.Join(" • ", parts);
        }

        #endregion
    }
}
