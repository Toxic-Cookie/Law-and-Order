using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hearings;
using Law_and_Order.Source.Components;
using LawAndOrder;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// Tracks pending changes to contraband settings
    /// </summary>
    public class PendingContrabandChange
    {
        public ThingDef thingDef;
        public int newPenalty;
        public bool isRemoval;

        public PendingContrabandChange(ThingDef thingDef, int newPenalty, bool isRemoval = false)
        {
            this.thingDef = thingDef;
            this.newPenalty = newPenalty;
            this.isRemoval = isRemoval;
        }
    }

    /// <summary>
    /// Main tab window for the Law and Order justice system
    /// Displays criminals, their crimes, and allows scheduling hearings
    /// </summary>
    public class MainTabWindow_Justice : MainTabWindow
    {
        private enum JusticeTab
        {
            ActiveCriminals,    // Criminals currently on the map
            Imprisoned,         // Criminals in prison
            Contraband,         // Contraband item configuration
            Crimes              // Crime penalty configuration
        }

        private JusticeTab curTab = JusticeTab.ActiveCriminals;
        private List<TabRecord> tabs = new List<TabRecord>();
        private Pawn selectedCriminal;
        private Vector2 criminalListScrollPos;
        private Vector2 selectedCriminalScrollPos;
        private string searchQuery = "";
        private CrimeCategoryUIHelper categoryHelper = new CrimeCategoryUIHelper();

        // Contraband tab fields
        private ThingDef selectedContrabandItem;
        private ContrabandCategoryNode selectedContrabandCategory;
        private Vector2 contrabandListScrollPos;
        private string contrabandSearchQuery = "";
        private string contrabandPenaltyInput = "";
        private List<ContrabandCategoryNode> contrabandCategoryTree;
        private List<ContrabandCategoryNode> contrabandFlattenedList;
        private bool contrabandTreeNeedsRebuild = true;

        // Pending changes tracking
        private Dictionary<ThingDef, PendingContrabandChange> pendingContrabandChanges = new Dictionary<ThingDef, PendingContrabandChange>();

        // Crimes tab fields
        private CrimeDefinition selectedCrime;
        private CrimeCategoryNode selectedCrimeCategory;
        private CrimePenaltyMultiplier selectedMultiplier;
        private Vector2 crimeListScrollPos;
        private Vector2 multiplierListScrollPos;
        private string crimeSearchQuery = "";
        private string crimeMinPenaltyInput = "";
        private string crimeMaxPenaltyInput = "";
        private string multiplierValueInput = "";
        private List<CrimeCategoryNode> crimeCategoryTree;
        private List<CrimeCategoryNode> crimeFlattenedList;
        private bool crimeTreeNeedsRebuild = true;
        private bool showMultipliers = false;

        private const float LeftPanelWidth = 0.35f;
        private const float PanelGap = 17f;

        public override Vector2 RequestedTabSize => new Vector2(1010f, 720f);

        public override void PreOpen()
        {
            base.PreOpen();

            // Initialize tabs
            tabs.Clear();
            tabs.Add(new TabRecord(
                "LawAndOrder_ActiveCriminals".Translate(),
                () => { curTab = JusticeTab.ActiveCriminals; selectedCriminal = null; },
                () => curTab == JusticeTab.ActiveCriminals
            ));
            tabs.Add(new TabRecord(
                "LawAndOrder_Imprisoned".Translate(),
                () => { curTab = JusticeTab.Imprisoned; selectedCriminal = null; },
                () => curTab == JusticeTab.Imprisoned
            ));
            tabs.Add(new TabRecord(
                "LawAndOrder_Contraband".Translate(),
                () => { curTab = JusticeTab.Contraband; selectedCriminal = null; },
                () => curTab == JusticeTab.Contraband
            ));
            tabs.Add(new TabRecord(
                "LawAndOrder_CrimesTab".Translate(),
                () => { curTab = JusticeTab.Crimes; selectedCriminal = null; },
                () => curTab == JusticeTab.Crimes
            ));
        }

        /// <summary>
        /// Selects a criminal and switches to the appropriate tab (Imprisoned if prisoner, ActiveCriminals otherwise)
        /// </summary>
        public void SelectCriminal(Pawn criminal)
        {
            if (criminal == null)
            {
                return;
            }

            selectedCriminal = criminal;

            // Switch to appropriate tab based on prisoner status
            if (criminal.IsPrisonerOfColony)
            {
                curTab = JusticeTab.Imprisoned;
            }
            else
            {
                curTab = JusticeTab.ActiveCriminals;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Adjust rect for tabs and draw them
            // TabDrawer will draw the tabs ABOVE this rect
            inRect.yMin += 45f;
            TabDrawer.DrawTabs<TabRecord>(inRect, tabs, 200f);

            // Handle Contraband and Crimes tabs differently
            if (curTab == JusticeTab.Contraband)
            {
                DrawContrabandUI(inRect);
            }
            else if (curTab == JusticeTab.Crimes)
            {
                DrawCrimesUI(inRect);
            }
            else
            {
                // Split into left panel (criminal list) and right panel (details)
                Rect leftPanel = inRect;
                leftPanel.width = inRect.width * LeftPanelWidth;

                Rect rightPanel = inRect;
                rightPanel.xMin = leftPanel.xMax + PanelGap;

                // Draw the criminal list
                DrawCriminalList(leftPanel);

                // Draw the selected criminal details
                DrawSelectedCriminalDetails(rightPanel);
            }
        }

        private void DrawCriminalList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Search bar at the top
            Rect searchRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            searchQuery = Widgets.TextField(searchRect, searchQuery);
            innerRect.yMin += 35f;

            // Get criminals based on current tab
            List<Pawn> criminals = GetCriminalsForTab();

            // Filter by search query
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                criminals = criminals.Where(p =>
                    p.Name.ToStringFull.ToLower().Contains(searchQuery.ToLower())
                ).ToList();
            }

            // Draw the list
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, criminals.Count * 50f);
            Widgets.BeginScrollView(innerRect, ref criminalListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var criminal in criminals)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, 48f);
                DrawCriminalListEntry(rowRect, criminal);
                yPos += 50f;
            }

            Widgets.EndScrollView();

            // Display count at bottom
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect countRect = new Rect(rect.x + 10f, rect.yMax - 25f, rect.width - 20f, 20f);
            Widgets.Label(countRect, $"{"LawAndOrder_CriminalsCount".Translate()}: {criminals.Count}");
            Text.Font = GameFont.Small;
        }

        private void DrawCriminalListEntry(Rect rect, Pawn criminal)
        {
            bool isSelected = selectedCriminal == criminal;

            if (isSelected)
            {
                Widgets.DrawHighlight(rect);
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawLightHighlight(rect);
            }

            // Handle double-click to pan camera to criminal
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Event.current.clickCount == 2 && Mouse.IsOver(rect))
            {
                Event.current.Use();
                CameraJumper.TryJump(criminal, CameraJumper.MovementMode.Pan);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                selectedCriminal = criminal;
                SoundDefOf.Click.PlayOneShotOnCamera(null);
            }

            // Draw pawn portrait
            Rect portraitRect = new Rect(rect.x + 4f, rect.y + 4f, 40f, 40f);
            Widgets.ThingIcon(portraitRect, criminal);

            // Draw name and basic info
            Rect textRect = new Rect(portraitRect.xMax + 8f, rect.y + 4f, rect.width - portraitRect.width - 12f, rect.height - 8f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(textRect, criminal.NameShortColored.Resolve());

            // Crime count
            var record = CrimeUtils.TryGetCriminalRecord(criminal);
            int crimeCount = record?.TotalCrimeCount ?? 0;

            Text.Font = GameFont.Tiny;
            Rect crimeCountRect = textRect;
            crimeCountRect.y += 18f;
            string crimeText = $"{crimeCount} {"LawAndOrder_Crimes".Translate()}";

            if (criminal.IsPrisonerOfColony)
            {
                crimeText += " â€¢ " + "LawAndOrder_Imprisoned".Translate();
            }

            Widgets.Label(crimeCountRect, crimeText);

            // Debt display
            var debtRecord = DebtUtils.TryGetDebtRecord(criminal);
            if (debtRecord != null && debtRecord.CurrentDebt > 0)
            {
                Rect debtRect = crimeCountRect;
                debtRect.y += 12f;
                string debtText = $"Debt: {debtRecord.CurrentDebt:F0} silver";
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                Widgets.Label(debtRect, debtText);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawSelectedCriminalDetails(Rect rect)
        {
            if (selectedCriminal == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_SelectCriminal".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with criminal name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 32f);
            Widgets.Label(headerRect, selectedCriminal.NameFullColored.Resolve());
            Text.Font = GameFont.Small;

            innerRect.yMin += 40f;

            // Status section
            Rect statusRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            DrawStatusSection(statusRect);

            innerRect.yMin += 65f;

            // Crime list section
            Rect crimesHeaderRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(crimesHeaderRect, "LawAndOrder_CrimesCommitted".Translate());
            Text.Font = GameFont.Small;

            innerRect.yMin += 30f;

            // Crimes list
            Rect crimesListRect = new Rect(innerRect.x, innerRect.y, innerRect.width, innerRect.height - 100f);
            DrawCrimesList(crimesListRect);

            // Action buttons at bottom
            Rect buttonsRect = new Rect(innerRect.x, rect.yMax - 100f, innerRect.width, 90f);
            DrawActionButtons(buttonsRect);
        }

        private void DrawStatusSection(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect textRect = rect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            string status = "";

            if (selectedCriminal.Dead)
            {
                status = "LawAndOrder_StatusDead".Translate();
            }
            else if (selectedCriminal.IsPrisonerOfColony)
            {
                status = "LawAndOrder_StatusImprisoned".Translate();
            }
            else if (selectedCriminal.IsColonist)
            {
                status = "LawAndOrder_StatusColonist".Translate();
            }
            else if (selectedCriminal.Faction != null)
            {
                status = "LawAndOrder_StatusFaction".Translate(selectedCriminal.Faction.Name);
            }
            else
            {
                status = "LawAndOrder_StatusUnknown".Translate();
            }

            Widgets.Label(textRect, $"{"LawAndOrder_Status".Translate()}: {status}");

            // Hearing status
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(selectedCriminal);
            if (criminalRecord != null)
            {
                textRect.y += 20f;
                string hearingStatus = criminalRecord.Hearing.status.ToString();

                if (criminalRecord.Hearing.status == HearingStatus.Completed)
                {
                    GUI.color = new Color(0.2f, 0.8f, 0.2f);
                    hearingStatus = $"Completed ({criminalRecord.Hearing.DaysSinceCompleted}d ago)";
                }
                else if (criminalRecord.Hearing.status == HearingStatus.Scheduled)
                {
                    GUI.color = new Color(0.8f, 0.8f, 0.2f);
                    hearingStatus = "Scheduled";
                }

                Widgets.Label(textRect, $"{"LawAndOrder_TrialStatus".Translate()}: {hearingStatus}");
                GUI.color = Color.white;
            }

            // Debt information
            var debtRecord = DebtUtils.TryGetDebtRecord(selectedCriminal);
            if (debtRecord != null)
            {
                textRect.y += 20f;
                if (debtRecord.CurrentDebt > 0)
                {
                    GUI.color = new Color(0.9f, 0.6f, 0.2f);
                    int estimatedDays = debtRecord.EstimatedDaysOfLabor();
                    Widgets.Label(textRect, $"Debt: {debtRecord.CurrentDebt:F0} silver (~{estimatedDays} days labor)");
                    GUI.color = Color.white;
                }
                else if (debtRecord.TotalDebtOwed > 0)
                {
                    GUI.color = new Color(0.2f, 0.8f, 0.2f);
                    Widgets.Label(textRect, $"Debt: Fully Paid ({debtRecord.TotalDebtOwed:F0} silver)");
                    GUI.color = Color.white;
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawCrimesList(Rect rect)
        {
            var record = CrimeUtils.TryGetCriminalRecord(selectedCriminal);

            if (record == null || record.TotalCrimeCount == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_NoCrimes".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var crimes = record.Crimes.ToList();
            crimes.Reverse(); // Show most recent first

            // Group crimes by category
            var groupedCrimes = categoryHelper.GroupCrimesByCategory(crimes);

            // Calculate total height needed
            const float crimeEntryHeight = 100f;
            float totalHeight = categoryHelper.CalculateTotalHeight(groupedCrimes, crimeEntryHeight);

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref selectedCriminalScrollPos, viewRect);

            float yPos = 0f;

            foreach (var categoryGroup in groupedCrimes)
            {
                CrimeType category = categoryGroup.Key;
                List<Crime> categoryCrimes = categoryGroup.Value;

                // Draw category header
                Rect categoryHeaderRect = new Rect(0f, yPos, viewRect.width, categoryHelper.GetCategoryHeaderHeight());
                bool isExpanded = categoryHelper.DrawCategoryHeader(categoryHeaderRect, category, categoryCrimes.Count);
                yPos += categoryHelper.GetCategoryHeaderHeight() + 5f;

                // Draw crimes if category is expanded
                if (isExpanded)
                {
                    foreach (var crime in categoryCrimes)
                    {
                        Rect crimeRect = new Rect(10f, yPos, viewRect.width - 10f, 95f);
                        DrawCrimeEntry(crimeRect, crime);
                        yPos += crimeEntryHeight;
                    }
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawCrimeEntry(Rect rect, Crime crime)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Widgets.DrawBox(rect);

            Rect innerRect = rect.ContractedBy(5f);

            // Crime type and debt
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 20f);
            Widgets.Label(titleRect, crime.GetCrimeLabel());

            // Use pre-calculated debt amount
            float crimeDebt = crime.debtAmount;

            if (crimeDebt > 0)
            {
                Rect debtRect = new Rect(innerRect.x + innerRect.width * 0.7f, innerRect.y, innerRect.width * 0.3f, 20f);
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                Widgets.Label(debtRect, $"{crimeDebt:F0} silver");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Details
            Text.Font = GameFont.Tiny;
            Rect detailsRect = new Rect(innerRect.x, innerRect.y + 22f, innerRect.width, innerRect.height - 22f);

            string details = "";
            if (crime.victim != null)
            {
                details += $"{"LawAndOrder_Victim".Translate()}: {crime.victim.NameShortColored.ToString()}\n";
            }
            if (crime.damageDealt > 0)
            {
                details += $"{"LawAndOrder_Damage".Translate()}: {crime.damageDealt:F1}\n";
            }
            details += $"{"LawAndOrder_DaysAgo".Translate()}: {crime.DaysAgo}";

            if (!string.IsNullOrEmpty(crime.additionalInfo))
            {
                details += $"\n{crime.additionalInfo}";
            }

            Widgets.Label(detailsRect, details);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawActionButtons(Rect rect)
        {
            float buttonWidth = (rect.width - 10f) / 2f;
            float buttonHeight = 35f;

            // Begin Hearing button
            Rect hearingButtonRect = new Rect(rect.x, rect.y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(hearingButtonRect, "LawAndOrder_BeginHearing".Translate()))
            {
                ScheduleHearing();
            }

            // Pardon button
            Rect pardonButtonRect = new Rect(rect.x + buttonWidth + 10f, rect.y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(pardonButtonRect, "LawAndOrder_Pardon".Translate()))
            {
                PardonCriminal();
            }

            // Post-debt action radio buttons
            float radioYOffset = buttonHeight + 10f;
            DrawPostDebtActionRadios(new Rect(rect.x, rect.y + radioYOffset, rect.width, 45f));
        }

        private void DrawPostDebtActionRadios(Rect rect)
        {
            if (selectedCriminal == null)
                return;

            var debtRecord = DebtUtils.TryGetDebtRecord(selectedCriminal);
            if (debtRecord == null)
                return;

            // Label
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, 20f);
            Widgets.Label(labelRect, "LawAndOrder_PostDebtAction".Translate());

            // Radio buttons
            float radioYPos = rect.y + 22f;
            float radioWidth = rect.width / 2f;
            float spacing = 30f; // Gap between radio buttons

            // Release option
            Rect releaseRect = new Rect(rect.x + 10f, radioYPos, radioWidth - 25f, 20f);
            if (Widgets.RadioButtonLabeled(releaseRect, "LawAndOrder_PostDebtAction_Release".Translate(),
                debtRecord.PostDebtAction == Hediffs.PostDebtAction.Release))
            {
                debtRecord.PostDebtAction = Hediffs.PostDebtAction.Release;
            }

            // Recruit option
            Rect recruitRect = new Rect(rect.x + radioWidth + spacing, radioYPos, radioWidth - spacing - 10f, 20f);
            if (Widgets.RadioButtonLabeled(recruitRect, "LawAndOrder_PostDebtAction_Recruit".Translate(),
                debtRecord.PostDebtAction == Hediffs.PostDebtAction.Recruit))
            {
                debtRecord.PostDebtAction = Hediffs.PostDebtAction.Recruit;
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void ScheduleHearing()
        {
            if (selectedCriminal == null)
            {
                return;
            }

            // Check if we can start a hearing ritual
            if (!HearingUtils.CanStartHearingRitual(selectedCriminal, out string reason))
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput);
                return;
            }

            // Get criminal record
            var criminalRecord = CrimeUtils.TryGetCriminalRecord(selectedCriminal);

            // Check if hearing already completed
            if (criminalRecord != null && criminalRecord.Hearing.status == HearingStatus.Completed)
            {
                Messages.Message(
                    $"{selectedCriminal.LabelShort} has already had their hearing.",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            // Start the hearing ritual
            StartHearingRitual();
        }

        private void StartHearingRitual()
        {
            // Get best adjudicator
            var adjudicator = HearingUtils.GetBestAdjudicator();

            // Get best courtroom
            var courtroom = CourtroomUtils.GetBestCourtroom();

            // Start the ritual
            HearingUtils.StartHearingRitual(selectedCriminal, courtroom, adjudicator);
        }

        private void PardonCriminal()
        {
            var record = CrimeUtils.TryGetCriminalRecord(selectedCriminal);
            if (record != null)
            {
                // Pay off any remaining debt (this triggers grace period and auto-emancipation)
                var debtRecord = DebtUtils.TryGetDebtRecord(selectedCriminal);
                if (debtRecord != null && debtRecord.CurrentDebt > 0)
                {
                    debtRecord.PayDebt(debtRecord.CurrentDebt, "Pardoned by colony");

                    // If enslaved, immediately trigger grace period and auto-emancipation
                    if (selectedCriminal.IsSlaveOfColony)
                    {
                        var debtManager = Find.World.GetComponent<WorldComponent_DebtManager>();
                        debtManager?.HandleDebtFullyPaid(selectedCriminal);
                    }
                }

                // Remove criminal record
                selectedCriminal.health.RemoveHediff(record);

                Messages.Message(
                    "LawAndOrder_Pardoned".Translate(selectedCriminal.NameShortColored),
                    selectedCriminal,
                    MessageTypeDefOf.PositiveEvent
                );
                selectedCriminal = null;
            }
        }

        private List<Pawn> GetCriminalsForTab()
        {
            var allPawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                .Concat(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_PrisonersOfColony)
                .Concat(Find.CurrentMap?.mapPawns.AllPawnsSpawned.Where(p => !p.IsColonist && !p.IsPrisonerOfColony) ?? Enumerable.Empty<Pawn>());

            var criminalsWithRecords = allPawns.Where(p => CrimeUtils.HasCriminalRecord(p)).ToList();

            switch (curTab)
            {
                case JusticeTab.ActiveCriminals:
                    return criminalsWithRecords.Where(p => !p.IsPrisonerOfColony && !p.Dead).ToList();

                case JusticeTab.Imprisoned:
                    return criminalsWithRecords.Where(p => p.IsPrisonerOfColony).ToList();

                default:
                    return criminalsWithRecords;
            }
        }

        private void DrawContrabandUI(Rect inRect)
        {
            // Reserve space for commit/cancel buttons at the bottom
            float bottomButtonHeight = 90f;
            Rect bottomButtonRect = new Rect(inRect.x, inRect.yMax - bottomButtonHeight, inRect.width, bottomButtonHeight);
            inRect.height -= bottomButtonHeight + 10f; // 10f spacing

            // Split into left panel (item list) and right panel (configuration)
            Rect leftPanel = inRect;
            leftPanel.width = inRect.width * LeftPanelWidth;

            Rect rightPanel = inRect;
            rightPanel.xMin = leftPanel.xMax + PanelGap;

            // Draw the item list
            DrawContrabandItemList(leftPanel);

            // Draw the configuration panel
            DrawContrabandConfiguration(rightPanel);

            // Draw commit/cancel buttons at the bottom
            DrawContrabandCommitButtons(bottomButtonRect);
        }

        private void DrawContrabandItemList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Search bar at the top
            Rect searchRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            string previousSearch = contrabandSearchQuery;
            contrabandSearchQuery = Widgets.TextField(searchRect, contrabandSearchQuery);

            // Rebuild tree if search changed
            if (previousSearch != contrabandSearchQuery)
            {
                contrabandTreeNeedsRebuild = true;
            }

            innerRect.yMin += 35f;

            // Build or rebuild tree if needed
            if (contrabandTreeNeedsRebuild || contrabandCategoryTree == null)
            {
                contrabandCategoryTree = ContrabandCategoryTreeBuilder.BuildCategoryTree();
                contrabandTreeNeedsRebuild = false;
            }

            // Get flattened list based on search
            if (!string.IsNullOrWhiteSpace(contrabandSearchQuery))
            {
                contrabandFlattenedList = ContrabandCategoryTreeBuilder.SearchTree(contrabandCategoryTree, contrabandSearchQuery);
            }
            else
            {
                contrabandFlattenedList = ContrabandCategoryTreeBuilder.FlattenTree(contrabandCategoryTree);
            }

            // Draw the tree
            float rowHeight = 30f;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, contrabandFlattenedList.Count * rowHeight);
            Widgets.BeginScrollView(innerRect, ref contrabandListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var node in contrabandFlattenedList)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, rowHeight - 2f);
                DrawContrabandCategoryNode(rowRect, node);
                yPos += rowHeight;
            }

            Widgets.EndScrollView();

            // Display count at bottom
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect countRect = new Rect(rect.x + 10f, rect.yMax - 25f, rect.width - 20f, 20f);

            int contrabandCount = WorldComponent_ContrabandManager.Instance.ContrabandDefinitions.Count;
            int totalItems = contrabandCategoryTree.Sum(n => n.GetTotalItemCount());
            Widgets.Label(countRect, $"{"LawAndOrder_ContrabandCount".Translate()}: {contrabandCount} / {totalItems}");
            Text.Font = GameFont.Small;
        }

        private void DrawContrabandCategoryNode(Rect rect, ContrabandCategoryNode node)
        {
            const float IndentWidth = 18f;
            const float ArrowWidth = 15f;

            float indent = node.depth * IndentWidth;
            Rect workRect = new Rect(rect.x + indent, rect.y, rect.width - indent, rect.height);

            if (node.IsCategory)
            {
                // Draw category
                bool isSelected = selectedContrabandCategory == node;

                if (isSelected)
                {
                    Widgets.DrawHighlight(workRect);
                }

                if (Mouse.IsOver(workRect))
                {
                    Widgets.DrawLightHighlight(workRect);
                }

                // Draw expand/collapse arrow
                Rect arrowRect = new Rect(workRect.x, workRect.y, ArrowWidth, workRect.height);
                if (Widgets.ButtonImage(arrowRect, node.isExpanded ? TexButton.Collapse : TexButton.Reveal))
                {
                    node.isExpanded = !node.isExpanded;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                // Draw category name with counts (clickable to select)
                Rect labelRect = new Rect(arrowRect.xMax + 4f, workRect.y, workRect.width - ArrowWidth - 4f, workRect.height);

                if (Widgets.ButtonInvisible(labelRect))
                {
                    selectedContrabandCategory = node;
                    selectedContrabandItem = null; // Clear item selection
                    contrabandPenaltyInput = "50"; // Default penalty
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                string categoryLabel = node.categoryDef != null ? node.categoryDef.LabelCap : "Uncategorized";
                int contrabandCount = node.GetContrabandCount();
                int totalCount = node.GetTotalItemCount();

                string label = $"{categoryLabel} ({contrabandCount}/{totalCount})";

                if (contrabandCount > 0)
                {
                    GUI.color = new Color(0.9f, 0.6f, 0.2f);
                }

                Widgets.Label(labelRect, label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else if (node.IsItem)
            {
                // Draw item
                ThingDef item = node.thingDef;
                bool isSelected = selectedContrabandItem == item;
                bool isContraband = WorldComponent_ContrabandManager.Instance.IsContraband(item);
                bool hasPendingChange = pendingContrabandChanges.ContainsKey(item);

                // Highlight pending changes with a special color
                if (hasPendingChange)
                {
                    Widgets.DrawBoxSolid(workRect, new Color(1f, 0.8f, 0.2f, 0.2f));
                }

                if (isSelected)
                {
                    Widgets.DrawHighlight(workRect);
                }

                if (Mouse.IsOver(workRect))
                {
                    Widgets.DrawLightHighlight(workRect);
                }

                if (Widgets.ButtonInvisible(workRect))
                {
                    selectedContrabandItem = item;
                    selectedContrabandCategory = null; // Clear category selection

                    // Check if there's a pending change first
                    if (hasPendingChange)
                    {
                        var pendingChange = pendingContrabandChanges[item];
                        if (!pendingChange.isRemoval)
                        {
                            contrabandPenaltyInput = pendingChange.newPenalty.ToString();
                        }
                        else
                        {
                            contrabandPenaltyInput = "50"; // Default penalty
                        }
                    }
                    // Otherwise load penalty if already set as contraband
                    else
                    {
                        var existingDef = WorldComponent_ContrabandManager.Instance.GetContrabandDefinition(item);
                        if (existingDef != null)
                        {
                            contrabandPenaltyInput = existingDef.silverPenaltyPerItem.ToString();
                        }
                        else
                        {
                            contrabandPenaltyInput = "50"; // Default penalty
                        }
                    }

                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                // Draw item icon
                Rect iconRect = new Rect(workRect.x + 4f, workRect.y + (workRect.height - 24f) / 2f, 24f, 24f);
                Widgets.ThingIcon(iconRect, item);

                // Draw name
                Rect textRect = new Rect(iconRect.xMax + 6f, workRect.y, workRect.width - iconRect.width - 10f, workRect.height);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                // Determine what to display based on pending changes
                string labelText = "";
                Color labelColor = Color.white;

                if (hasPendingChange)
                {
                    var pendingChange = pendingContrabandChanges[item];
                    if (pendingChange.isRemoval)
                    {
                        labelColor = new Color(1f, 0.5f, 0.2f);
                        var contrabandDef = WorldComponent_ContrabandManager.Instance.GetContrabandDefinition(item);
                        labelText = $"{item.LabelCap} ({contrabandDef.silverPenaltyPerItem} silver) [PENDING REMOVAL]";
                    }
                    else
                    {
                        labelColor = new Color(1f, 0.8f, 0.2f);
                        labelText = $"{item.LabelCap} ({pendingChange.newPenalty} silver) [PENDING]";
                    }
                }
                else if (isContraband)
                {
                    labelColor = new Color(0.9f, 0.2f, 0.2f);
                    var contrabandDef = WorldComponent_ContrabandManager.Instance.GetContrabandDefinition(item);
                    labelText = $"{item.LabelCap} ({contrabandDef.silverPenaltyPerItem} silver)";
                }
                else
                {
                    labelText = item.LabelCap;
                }

                GUI.color = labelColor;
                Widgets.Label(textRect, labelText);
                GUI.color = Color.white;

                Text.Anchor = TextAnchor.UpperLeft;
            }

            Text.Font = GameFont.Small;
        }

        private void DrawContrabandConfiguration(Rect rect)
        {
            if (selectedContrabandItem == null && selectedContrabandCategory == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_SelectItem".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Show category bulk operations if category is selected
            if (selectedContrabandCategory != null)
            {
                DrawContrabandCategoryConfiguration(rect);
                return;
            }

            // Otherwise show individual item configuration
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with item name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 32f);
            Widgets.Label(headerRect, selectedContrabandItem.LabelCap);
            Text.Font = GameFont.Small;

            innerRect.yMin += 40f;

            // Item description
            if (!string.IsNullOrEmpty(selectedContrabandItem.description))
            {
                Rect descRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, selectedContrabandItem.description);
                Text.Font = GameFont.Small;
                innerRect.yMin += 70f;
            }

            // Item stats box
            Rect statsRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 80f);
            Widgets.DrawBoxSolid(statsRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect statsTextRect = statsRect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Calculate maximum penalty (3x market value, minimum 10 for worthless items)
            float marketValue = selectedContrabandItem.BaseMarketValue;
            int maxPenalty = Mathf.RoundToInt(marketValue * 3.0f);
            if (maxPenalty < 10)
            {
                maxPenalty = 10;
            }

            string stats = $"{"LawAndOrder_Category".Translate()}: {selectedContrabandItem.thingCategories?.FirstOrDefault()?.LabelCap ?? "None"}\n";
            stats += $"{"LawAndOrder_MarketValue".Translate()}: {marketValue:F0} silver\n";

            // Show appropriate max penalty message based on market value
            if (marketValue <= 0)
            {
                stats += $"Maximum Penalty: {maxPenalty} silver (min for worthless items)";
            }
            else
            {
                stats += $"Maximum Penalty: {maxPenalty} silver (3x market value)";
            }

            Widgets.Label(statsTextRect, stats);
            Text.Anchor = TextAnchor.UpperLeft;

            innerRect.yMin += 90f;

            // Contraband configuration section
            bool isCurrentlyContraband = WorldComponent_ContrabandManager.Instance.IsContraband(selectedContrabandItem);
            bool hasPendingChange = pendingContrabandChanges.ContainsKey(selectedContrabandItem);

            Rect configHeaderRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(configHeaderRect, "LawAndOrder_ContrabandSettings".Translate());
            Text.Font = GameFont.Small;

            innerRect.yMin += 30f;

            // Show pending change indicator if there is one
            if (hasPendingChange)
            {
                Rect pendingIndicatorRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
                Widgets.DrawBoxSolid(pendingIndicatorRect, new Color(1f, 0.8f, 0.2f, 0.3f));
                Rect pendingTextRect = pendingIndicatorRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(1f, 0.8f, 0.2f);

                var pendingChange = pendingContrabandChanges[selectedContrabandItem];
                string pendingText = pendingChange.isRemoval
                    ? "Pending: Remove from contraband"
                    : $"Pending: Set penalty to {pendingChange.newPenalty} silver";

                Widgets.Label(pendingTextRect, pendingText);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                innerRect.yMin += 35f;
            }

            // Penalty input
            Rect penaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.6f, 24f);
            Widgets.Label(penaltyLabelRect, "LawAndOrder_SilverPenaltyPerItem".Translate());

            Rect penaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.65f, innerRect.y, innerRect.width * 0.35f, 24f);
            contrabandPenaltyInput = Widgets.TextField(penaltyInputRect, contrabandPenaltyInput);

            innerRect.yMin += 30f;

            // Buttons
            float buttonWidth = (innerRect.width - 10f) / 2f;
            float buttonHeight = 35f;

            if (isCurrentlyContraband)
            {
                // Show "Update" and "Remove" buttons
                Rect updateButtonRect = new Rect(innerRect.x, innerRect.y, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(updateButtonRect, "LawAndOrder_UpdateContraband".Translate()))
                {
                    if (int.TryParse(contrabandPenaltyInput, out int penalty) && penalty > 0)
                    {
                        // Stage the change instead of applying immediately
                        pendingContrabandChanges[selectedContrabandItem] = new PendingContrabandChange(selectedContrabandItem, penalty, false);
                        Messages.Message(
                            $"{selectedContrabandItem.LabelCap} update staged (not yet committed)",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                    else
                    {
                        Messages.Message(
                            "LawAndOrder_InvalidPenalty".Translate(),
                            MessageTypeDefOf.RejectInput
                        );
                    }
                }

                Rect removeButtonRect = new Rect(innerRect.x + buttonWidth + 10f, innerRect.y, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(removeButtonRect, "LawAndOrder_RemoveContraband".Translate()))
                {
                    // Stage the removal instead of applying immediately
                    pendingContrabandChanges[selectedContrabandItem] = new PendingContrabandChange(selectedContrabandItem, 0, true);
                    Messages.Message(
                        $"{selectedContrabandItem.LabelCap} removal staged (not yet committed)",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            }
            else
            {
                // Show "Mark as Contraband" button
                Rect addButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
                if (Widgets.ButtonText(addButtonRect, "LawAndOrder_MarkAsContraband".Translate()))
                {
                    if (int.TryParse(contrabandPenaltyInput, out int penalty) && penalty > 0)
                    {
                        // Stage the change instead of applying immediately
                        pendingContrabandChanges[selectedContrabandItem] = new PendingContrabandChange(selectedContrabandItem, penalty, false);
                        Messages.Message(
                            $"{selectedContrabandItem.LabelCap} addition staged (not yet committed)",
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                    else
                    {
                        Messages.Message(
                            "LawAndOrder_InvalidPenalty".Translate(),
                            MessageTypeDefOf.RejectInput
                        );
                    }
                }
            }
        }

        private void DrawContrabandCategoryConfiguration(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with category name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 32f);
            string categoryLabel = selectedContrabandCategory.categoryDef != null
                ? selectedContrabandCategory.categoryDef.LabelCap
                : "Uncategorized";
            Widgets.Label(headerRect, categoryLabel);
            Text.Font = GameFont.Small;

            innerRect.yMin += 40f;

            // Category stats box
            Rect statsRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 80f);
            Widgets.DrawBoxSolid(statsRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect statsTextRect = statsRect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            int totalItems = selectedContrabandCategory.GetTotalItemCount();
            int contrabandItems = selectedContrabandCategory.GetContrabandCount();

            string stats = $"{"LawAndOrder_TotalItems".Translate()}: {totalItems}\n";
            stats += $"{"LawAndOrder_ContrabandItems".Translate()}: {contrabandItems}\n";
            stats += $"{"LawAndOrder_NonContrabandItems".Translate()}: {totalItems - contrabandItems}";

            Widgets.Label(statsTextRect, stats);
            Text.Anchor = TextAnchor.UpperLeft;

            innerRect.yMin += 90f;

            // Bulk operations section
            Rect configHeaderRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(configHeaderRect, "LawAndOrder_BulkOperations".Translate());
            Text.Font = GameFont.Small;

            innerRect.yMin += 30f;

            // Penalty input
            Rect penaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.6f, 24f);
            Widgets.Label(penaltyLabelRect, "LawAndOrder_SilverPenaltyPerItem".Translate());

            Rect penaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.65f, innerRect.y, innerRect.width * 0.35f, 24f);
            contrabandPenaltyInput = Widgets.TextField(penaltyInputRect, contrabandPenaltyInput);

            innerRect.yMin += 30f;

            // Bulk operation buttons
            float buttonHeight = 35f;
            float buttonSpacing = 5f;

            // Mark All button
            Rect markAllButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
            if (Widgets.ButtonText(markAllButtonRect, "LawAndOrder_MarkAllAsContraband".Translate()))
            {
                if (int.TryParse(contrabandPenaltyInput, out int penalty) && penalty > 0)
                {
                    var items = selectedContrabandCategory.GetAllItems();
                    int count = 0;
                    foreach (var item in items)
                    {
                        pendingContrabandChanges[item] = new PendingContrabandChange(item, penalty, false);
                        count++;
                    }
                    Messages.Message(
                        $"{count} items in {categoryLabel} staged as contraband (not yet committed)",
                        MessageTypeDefOf.PositiveEvent
                    );
                }
                else
                {
                    Messages.Message(
                        "LawAndOrder_InvalidPenalty".Translate(),
                        MessageTypeDefOf.RejectInput
                    );
                }
            }

            innerRect.yMin += buttonHeight + buttonSpacing;

            // Update All button (only update existing contraband)
            Rect updateAllButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
            if (Widgets.ButtonText(updateAllButtonRect, "LawAndOrder_UpdateAllContraband".Translate()))
            {
                if (int.TryParse(contrabandPenaltyInput, out int penalty) && penalty > 0)
                {
                    var items = selectedContrabandCategory.GetAllItems();
                    int count = 0;
                    foreach (var item in items)
                    {
                        if (WorldComponent_ContrabandManager.Instance.IsContraband(item))
                        {
                            pendingContrabandChanges[item] = new PendingContrabandChange(item, penalty, false);
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        Messages.Message(
                            $"{count} items in {categoryLabel} staged for update (not yet committed)",
                            MessageTypeDefOf.PositiveEvent
                        );
                    }
                    else
                    {
                        Messages.Message(
                            "LawAndOrder_NoContrabandToUpdate".Translate(),
                            MessageTypeDefOf.RejectInput
                        );
                    }
                }
                else
                {
                    Messages.Message(
                        "LawAndOrder_InvalidPenalty".Translate(),
                        MessageTypeDefOf.RejectInput
                    );
                }
            }

            innerRect.yMin += buttonHeight + buttonSpacing;

            // Remove All button
            Rect removeAllButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
            if (Widgets.ButtonText(removeAllButtonRect, "LawAndOrder_RemoveAllContraband".Translate()))
            {
                var items = selectedContrabandCategory.GetAllItems();
                int count = 0;
                foreach (var item in items)
                {
                    if (WorldComponent_ContrabandManager.Instance.IsContraband(item))
                    {
                        pendingContrabandChanges[item] = new PendingContrabandChange(item, 0, true);
                        count++;
                    }
                }
                if (count > 0)
                {
                    Messages.Message(
                        $"{count} items in {categoryLabel} staged for removal (not yet committed)",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
                else
                {
                    Messages.Message(
                        "LawAndOrder_NoContrabandToRemove".Translate(),
                        MessageTypeDefOf.RejectInput
                    );
                }
            }
        }

        private void DrawContrabandCommitButtons(Rect rect)
        {
            var contrabandManager = WorldComponent_ContrabandManager.Instance;
            bool isGodMode = DebugSettings.godMode;
            bool wouldBeInLockout = false;
            float daysRemaining = 0f;

            // Check if we would be in lockout (without god mode bypass)
            if (contrabandManager.lastCommitTick >= 0)
            {
                int ticksSinceCommit = Find.TickManager.TicksGame - contrabandManager.lastCommitTick;
                wouldBeInLockout = ticksSinceCommit < 15 * 60000; // LOCKOUT_DURATION_TICKS
                if (wouldBeInLockout)
                {
                    int ticksRemaining = (15 * 60000) - ticksSinceCommit;
                    daysRemaining = ticksRemaining / 60000f;
                }
            }

            bool isInLockout = contrabandManager.IsInLockout();

            // Show god mode bypass indicator if god mode is active and would be locked
            if (isGodMode && wouldBeInLockout)
            {
                Rect godModeRect = new Rect(rect.x, rect.y, rect.width, rect.height / 2f);
                Widgets.DrawBoxSolid(godModeRect, new Color(0.2f, 0.8f, 0.2f, 0.3f));

                Rect godModeTextRect = godModeRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.3f, 1f, 0.3f);
                Widgets.Label(godModeTextRect, $"GOD MODE: LOCKOUT BYPASSED");

                Text.Font = GameFont.Tiny;
                godModeTextRect.y += 16f;
                Widgets.Label(godModeTextRect, $"(Would be locked for {daysRemaining:F1} more day(s))");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                rect.y += rect.height / 2f + 5f;
                rect.height = rect.height / 2f - 5f;
            }
            // Show lockout status prominently if not bypassed
            else if (isInLockout)
            {
                Rect lockoutRect = new Rect(rect.x, rect.y, rect.width, rect.height / 2f);
                Widgets.DrawBoxSolid(lockoutRect, new Color(0.8f, 0.2f, 0.2f, 0.3f));

                Rect lockoutTextRect = lockoutRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = new Color(1f, 0.3f, 0.3f);
                Widgets.Label(lockoutTextRect, $"CONTRABAND POLICY LOCKED");

                Text.Font = GameFont.Tiny;
                lockoutTextRect.y += 16f;
                Widgets.Label(lockoutTextRect, $"Cannot commit changes for {daysRemaining:F1} more day(s)");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                rect.y += rect.height / 2f + 5f;
                rect.height = rect.height / 2f - 5f;
            }

            // Only show buttons if there are pending changes or show status message
            if (pendingContrabandChanges.Count == 0 && !isInLockout && !isGodMode)
            {
                // Show a message indicating no pending changes
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(rect, "No pending changes");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            else if (pendingContrabandChanges.Count == 0 && !wouldBeInLockout)
            {
                // Show a message indicating no pending changes (god mode active but no lockout to show)
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(rect, "No pending changes");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float buttonWidth = 200f;
            float buttonHeight = 40f;
            float spacing = 15f;
            float totalWidth = buttonWidth * 2 + spacing;
            float startX = rect.x + (rect.width - totalWidth) / 2f;

            // Pending changes indicator (only show if there are pending changes)
            if (pendingContrabandChanges.Count > 0)
            {
                Rect pendingLabelRect = new Rect(rect.x, rect.y, rect.width / 3f, buttonHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(1f, 0.8f, 0.2f);
                Widgets.Label(pendingLabelRect, $"{pendingContrabandChanges.Count} pending change(s)");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // Commit button (green when available, grey when locked)
                Rect commitButtonRect = new Rect(startX, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);

                if (isInLockout)
                {
                    // Disabled button with tooltip
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    Widgets.ButtonText(commitButtonRect, "Commit Changes (Locked)");
                    GUI.color = Color.white;

                    if (Mouse.IsOver(commitButtonRect))
                    {
                        TooltipHandler.TipRegion(commitButtonRect, $"Contraband policy is locked for {daysRemaining:F1} more day(s)");
                    }
                }
                else
                {
                    // Active button
                    GUI.color = new Color(0.3f, 0.8f, 0.3f);
                    if (Widgets.ButtonText(commitButtonRect, "Commit Changes"))
                    {
                        CommitContrabandChanges();
                    }
                    GUI.color = Color.white;
                }

                // Cancel button (red) - always enabled
                Rect cancelButtonRect = new Rect(startX + buttonWidth + spacing, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
                GUI.color = new Color(0.8f, 0.3f, 0.3f);
                if (Widgets.ButtonText(cancelButtonRect, "Cancel Changes"))
                {
                    CancelContrabandChanges();
                }
                GUI.color = Color.white;
            }
        }

        private void CommitContrabandChanges()
        {
            if (pendingContrabandChanges.Count == 0)
            {
                return;
            }

            // Check lockout
            var contrabandManager = WorldComponent_ContrabandManager.Instance;
            bool isGodMode = DebugSettings.godMode;

            if (contrabandManager.IsInLockout())
            {
                float daysRemaining = contrabandManager.GetLockoutDaysRemaining();
                Messages.Message(
                    $"Cannot commit contraband changes. Policy is locked for {daysRemaining:F1} more day(s).",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            int addedCount = 0;
            int updatedCount = 0;
            int removedCount = 0;

            foreach (var change in pendingContrabandChanges.Values)
            {
                if (change.isRemoval)
                {
                    contrabandManager.RemoveContraband(change.thingDef);
                    removedCount++;
                }
                else
                {
                    bool wasContraband = contrabandManager.IsContraband(change.thingDef);
                    contrabandManager.SetContraband(change.thingDef, change.newPenalty);
                    if (wasContraband)
                    {
                        updatedCount++;
                    }
                    else
                    {
                        addedCount++;
                    }
                }
            }

            // Record the commit to start lockout period
            contrabandManager.RecordCommit();

            // Clear pending changes
            pendingContrabandChanges.Clear();

            // Rebuild tree to show updated contraband counts
            contrabandTreeNeedsRebuild = true;

            // Show success message
            string message = "Contraband changes committed: ";
            List<string> parts = new List<string>();
            if (addedCount > 0) parts.Add($"{addedCount} added");
            if (updatedCount > 0) parts.Add($"{updatedCount} updated");
            if (removedCount > 0) parts.Add($"{removedCount} removed");
            message += string.Join(", ", parts);

            // Different message based on god mode
            if (isGodMode)
            {
                message += "\n(God mode: Lockout bypassed)";
            }
            else
            {
                message += "\nContraband policy locked for 15 days.";
            }

            Messages.Message(message, MessageTypeDefOf.TaskCompletion);
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
        }

        private void CancelContrabandChanges()
        {
            if (pendingContrabandChanges.Count == 0)
            {
                return;
            }

            int count = pendingContrabandChanges.Count;
            pendingContrabandChanges.Clear();

            Messages.Message($"Cancelled {count} pending contraband change(s)", MessageTypeDefOf.NeutralEvent);
            SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
        }

        /// <summary>
        /// Gets a user-friendly label for a multiplier type.
        /// </summary>
        private string GetMultiplierLabel(MultiplierType type)
        {
            switch (type)
            {
                case MultiplierType.RepeatOffender:
                    return "LawAndOrder_Multiplier_RepeatOffender".Translate();
                case MultiplierType.VictimNobility:
                    return "LawAndOrder_Multiplier_VictimNobility".Translate();
                case MultiplierType.VictimAge:
                    return "LawAndOrder_Multiplier_VictimAge".Translate();
                case MultiplierType.Wartime:
                    return "LawAndOrder_Multiplier_Wartime".Translate();
                case MultiplierType.Premeditated:
                    return "LawAndOrder_Multiplier_Premeditated".Translate();
                case MultiplierType.VictimRelationship:
                    return "LawAndOrder_Multiplier_VictimRelationship".Translate();
                case MultiplierType.RaiderWealth:
                    return "LawAndOrder_Multiplier_RaiderWealth".Translate();
                case MultiplierType.ColonyWealth:
                    return "LawAndOrder_Multiplier_ColonyWealth".Translate();
                case MultiplierType.DifficultySetting:
                    return "LawAndOrder_Multiplier_DifficultySetting".Translate();
                case MultiplierType.FactionRelations:
                    return "LawAndOrder_Multiplier_FactionRelations".Translate();
                default:
                    return type.ToString();
            }
        }

        private void DrawCrimesUI(Rect inRect)
        {
            // Reserve space for commit/cancel buttons at the bottom
            float bottomButtonHeight = 90f;
            Rect bottomButtonRect = new Rect(inRect.x, inRect.yMax - bottomButtonHeight, inRect.width, bottomButtonHeight);
            inRect.height -= bottomButtonHeight + 10f; // 10f spacing

            // Split into left panel (crime list) and right panel (configuration)
            Rect leftPanel = inRect;
            leftPanel.width = inRect.width * LeftPanelWidth;

            Rect rightPanel = inRect;
            rightPanel.xMin = leftPanel.xMax + PanelGap;

            // Draw the crime list
            DrawCrimeList(leftPanel);

            // Draw the configuration panel
            DrawCrimeConfiguration(rightPanel);

            // Draw commit/cancel buttons at the bottom
            DrawCrimeCommitButtons(bottomButtonRect);
        }

        private void DrawCrimeList(Rect rect)
        {
            var crimeManager = WorldComponent_CrimePenaltyManager.Instance;
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Search bar at the top
            Rect searchRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            string previousSearch = crimeSearchQuery;
            crimeSearchQuery = Widgets.TextField(searchRect, crimeSearchQuery);

            // Rebuild tree if search changed
            if (previousSearch != crimeSearchQuery)
            {
                crimeTreeNeedsRebuild = true;
            }

            innerRect.yMin += 35f;

            // Build or rebuild tree if needed
            if (crimeTreeNeedsRebuild || crimeCategoryTree == null)
            {
                // Filter out crimes that don't use range calculation (not configurable)
                var configurableCrimes = crimeManager.CrimeDefinitions.Where(c => c.usesRangeCalculation).ToList();
                crimeCategoryTree = CrimeCategoryTreeBuilder.BuildCategoryTree(configurableCrimes);
                crimeTreeNeedsRebuild = false;
            }

            // Get flattened list based on search
            if (!string.IsNullOrWhiteSpace(crimeSearchQuery))
            {
                crimeFlattenedList = CrimeCategoryTreeBuilder.SearchTree(crimeCategoryTree, crimeSearchQuery);
            }
            else
            {
                crimeFlattenedList = CrimeCategoryTreeBuilder.FlattenTree(crimeCategoryTree);
            }

            // Draw the tree
            float rowHeight = 30f;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, crimeFlattenedList.Count * rowHeight);
            Widgets.BeginScrollView(innerRect, ref crimeListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var node in crimeFlattenedList)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, rowHeight - 2f);
                DrawCrimeCategoryNode(rowRect, node);
                yPos += rowHeight;
            }

            Widgets.EndScrollView();

            // Display count at bottom
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect countRect = new Rect(rect.x + 10f, rect.yMax - 25f, rect.width - 20f, 20f);

            int totalCrimes = crimeManager.CrimeDefinitions.Count();
            Widgets.Label(countRect, $"{"LawAndOrder_CrimesCount".Translate()}: {totalCrimes}");
            Text.Font = GameFont.Small;
        }

        private void DrawCrimeCategoryNode(Rect rect, CrimeCategoryNode node)
        {
            const float IndentWidth = 18f;
            const float ArrowWidth = 15f;

            float indent = node.depth * IndentWidth;
            Rect workRect = new Rect(rect.x + indent, rect.y, rect.width - indent, rect.height);

            if (node.IsCategory)
            {
                // Draw category
                bool isSelected = selectedCrimeCategory == node;

                if (isSelected)
                {
                    Widgets.DrawHighlight(workRect);
                }

                if (Mouse.IsOver(workRect))
                {
                    Widgets.DrawLightHighlight(workRect);
                }

                // Draw expand/collapse arrow
                Rect arrowRect = new Rect(workRect.x, workRect.y, ArrowWidth, workRect.height);
                if (Widgets.ButtonImage(arrowRect, node.isExpanded ? TexButton.Collapse : TexButton.Reveal))
                {
                    node.isExpanded = !node.isExpanded;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                // Draw category name with counts (clickable to select)
                Rect labelRect = new Rect(arrowRect.xMax + 4f, workRect.y, workRect.width - ArrowWidth - 4f, workRect.height);

                if (Widgets.ButtonInvisible(labelRect))
                {
                    selectedCrimeCategory = node;
                    selectedCrime = null; // Clear crime selection
                    selectedMultiplier = null; // Clear multiplier selection
                    showMultipliers = false; // Hide multipliers section
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                string categoryLabel = node.GetLabel();
                int crimeCount = node.GetTotalCrimeCount();

                string label = $"{categoryLabel} ({crimeCount})";

                if (crimeCount > 0)
                {
                    GUI.color = new Color(0.9f, 0.6f, 0.2f);
                }

                Widgets.Label(labelRect, label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else if (node.IsCrime)
            {
                // Draw crime
                CrimeDefinition crime = node.crimeDefinition;
                bool isSelected = selectedCrime == crime;
                bool hasPendingChange = crime.hasPendingChanges;

                // Highlight pending changes with a special color
                if (hasPendingChange)
                {
                    Widgets.DrawBoxSolid(workRect, new Color(1f, 0.8f, 0.2f, 0.2f));
                }

                if (isSelected)
                {
                    Widgets.DrawHighlight(workRect);
                }

                if (Mouse.IsOver(workRect))
                {
                    Widgets.DrawLightHighlight(workRect);
                }

                if (Widgets.ButtonInvisible(workRect))
                {
                    selectedCrime = crime;
                    selectedCrimeCategory = null; // Clear category selection
                    selectedMultiplier = null; // Clear multiplier selection
                    showMultipliers = false; // Hide multipliers section

                    // Load penalty values
                    if (hasPendingChange)
                    {
                        crimeMinPenaltyInput = crime.pendingMinPenalty.ToString();
                        crimeMaxPenaltyInput = crime.pendingMaxPenalty.ToString();
                    }
                    else
                    {
                        crimeMinPenaltyInput = crime.minPenalty.ToString();
                        crimeMaxPenaltyInput = crime.maxPenalty.ToString();
                    }

                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                // Draw name with penalty range
                Rect textRect = new Rect(workRect.x + 4f, workRect.y, workRect.width - 8f, workRect.height);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;

                string labelText = "";
                Color labelColor = Color.white;

                if (hasPendingChange)
                {
                    labelColor = new Color(1f, 0.8f, 0.2f);
                    labelText = $"{crime.label} ({crime.pendingMinPenalty}-{crime.pendingMaxPenalty} silver) [PENDING]";
                }
                else
                {
                    labelText = $"{crime.label} ({crime.minPenalty}-{crime.maxPenalty} silver)";
                }

                GUI.color = labelColor;
                Widgets.Label(textRect, labelText);
                GUI.color = Color.white;

                Text.Anchor = TextAnchor.UpperLeft;
            }

            Text.Font = GameFont.Small;
        }

        private void DrawCrimeConfiguration(Rect rect)
        {
            if (selectedCrime == null && selectedCrimeCategory == null && selectedMultiplier == null && !showMultipliers)
            {
                Widgets.DrawMenuSection(rect);
                Rect innerRect = rect.ContractedBy(10f);

                // Show button to view multipliers
                Rect multiplierButtonRect = new Rect(innerRect.x, innerRect.y + innerRect.height / 2f - 20f, innerRect.width, 40f);
                if (Widgets.ButtonText(multiplierButtonRect, "LawAndOrder_ViewMultipliers".Translate()))
                {
                    showMultipliers = true;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Rect instructionRect = new Rect(innerRect.x, innerRect.y + innerRect.height / 2f - 80f, innerRect.width, 40f);
                Widgets.Label(instructionRect, "LawAndOrder_SelectCrimeOrCategory".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Show multipliers section if flag is set
            if (showMultipliers)
            {
                DrawMultipliersConfiguration(rect);
                return;
            }

            // Show category bulk operations if category is selected
            if (selectedCrimeCategory != null)
            {
                DrawCrimeCategoryConfiguration(rect);
                return;
            }

            // Show individual multiplier configuration if selected
            if (selectedMultiplier != null)
            {
                DrawIndividualMultiplierConfiguration(rect);
                return;
            }

            // Otherwise show individual crime configuration
            DrawIndividualCrimeConfiguration(rect);
        }

        private void DrawIndividualCrimeConfiguration(Rect rect)
        {
            if (selectedCrime == null) return;

            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with crime name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 180f, 32f);
            Widgets.Label(headerRect, selectedCrime.label);
            Text.Font = GameFont.Small;

            // View Multipliers button (top right)
            Rect multipliersButtonRect = new Rect(innerRect.xMax - 170f, innerRect.y, 170f, 32f);
            if (Widgets.ButtonText(multipliersButtonRect, "LawAndOrder_ViewMultipliers".Translate()))
            {
                showMultipliers = true;
                selectedCrime = null;
                selectedCrimeCategory = null;
                SoundDefOf.Click.PlayOneShotOnCamera(null);
                return; // Don't continue drawing this panel
            }

            innerRect.yMin += 40f;

            // Crime description
            if (!string.IsNullOrEmpty(selectedCrime.description))
            {
                Rect descRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 80f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, selectedCrime.description);
                Text.Font = GameFont.Small;
                innerRect.yMin += 90f;
            }

            // Crime stats box
            Rect statsRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 80f);
            Widgets.DrawBoxSolid(statsRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect statsTextRect = statsRect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            string stats = $"{"LawAndOrder_Category".Translate()}: {selectedCrime.category}\n";
            stats += $"{"LawAndOrder_Severity".Translate()}: {selectedCrime.severity}\n";
            stats += $"Current Range: {selectedCrime.minPenalty}-{selectedCrime.maxPenalty} silver";

            Widgets.Label(statsTextRect, stats);
            Text.Anchor = TextAnchor.UpperLeft;

            innerRect.yMin += 90f;

            // Show pending change indicator if there is one
            if (selectedCrime.hasPendingChanges)
            {
                Rect pendingIndicatorRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
                Widgets.DrawBoxSolid(pendingIndicatorRect, new Color(1f, 0.8f, 0.2f, 0.3f));
                Rect pendingTextRect = pendingIndicatorRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(1f, 0.8f, 0.2f);

                string pendingText = $"Pending: {selectedCrime.pendingMinPenalty}-{selectedCrime.pendingMaxPenalty} silver";

                Widgets.Label(pendingTextRect, pendingText);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                innerRect.yMin += 35f;
            }

            // Penalty inputs (only show for crimes that use range calculation)
            if (selectedCrime.usesRangeCalculation)
            {
                Rect minPenaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 24f);
                Widgets.Label(minPenaltyLabelRect, "LawAndOrder_MinPenalty".Translate());

                Rect minPenaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.55f, innerRect.y, innerRect.width * 0.45f, 24f);
                crimeMinPenaltyInput = Widgets.TextField(minPenaltyInputRect, crimeMinPenaltyInput);

                innerRect.yMin += 30f;

                Rect maxPenaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 24f);
                Widgets.Label(maxPenaltyLabelRect, "LawAndOrder_MaxPenalty".Translate());

                Rect maxPenaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.55f, innerRect.y, innerRect.width * 0.45f, 24f);
                crimeMaxPenaltyInput = Widgets.TextField(maxPenaltyInputRect, crimeMaxPenaltyInput);

                innerRect.yMin += 35f;

                // Update button
                float buttonWidth = innerRect.width;
                float buttonHeight = 35f;

                Rect updateButtonRect = new Rect(innerRect.x, innerRect.y, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(updateButtonRect, "LawAndOrder_UpdateCrime".Translate()))
                {
                    if (int.TryParse(crimeMinPenaltyInput, out int minPenalty) &&
                        int.TryParse(crimeMaxPenaltyInput, out int maxPenalty) &&
                        minPenalty > 0 && maxPenalty >= minPenalty)
                    {
                        // Stage the change
                        selectedCrime.StagePenaltyChange(minPenalty, maxPenalty);
                        Messages.Message(
                            $"{selectedCrime.label} update staged (not yet committed)",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                    else
                    {
                        Messages.Message(
                            "LawAndOrder_InvalidPenaltyRange".Translate(),
                            MessageTypeDefOf.RejectInput
                        );
                    }
                }

                innerRect.yMin += buttonHeight + 10f;

                // Reset button (cancel pending changes for this crime)
                if (selectedCrime.hasPendingChanges)
                {
                    Rect resetButtonRect = new Rect(innerRect.x, innerRect.y, buttonWidth, buttonHeight);
                    if (Widgets.ButtonText(resetButtonRect, "LawAndOrder_ResetCrime".Translate()))
                    {
                        selectedCrime.ClearPendingChanges();
                        crimeMinPenaltyInput = selectedCrime.minPenalty.ToString();
                        crimeMaxPenaltyInput = selectedCrime.maxPenalty.ToString();
                        Messages.Message(
                            $"{selectedCrime.label} reset to current values",
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
            else
            {
                // For crimes that don't use range calculation, show an info message
                Rect infoBoxRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
                Widgets.DrawBoxSolid(infoBoxRect, new Color(0.2f, 0.3f, 0.4f, 0.5f));
                Rect infoTextRect = infoBoxRect.ContractedBy(5f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.9f, 1f);
                Widgets.Label(infoTextRect, "LawAndOrder_CrimeUsesValueCalculation".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                innerRect.yMin += 65f;
            }
        }

        private void DrawCrimeCategoryConfiguration(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with category name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 180f, 32f);
            Widgets.Label(headerRect, selectedCrimeCategory.GetLabel());
            Text.Font = GameFont.Small;

            // View Multipliers button (top right)
            Rect multipliersButtonRect = new Rect(innerRect.xMax - 170f, innerRect.y, 170f, 32f);
            if (Widgets.ButtonText(multipliersButtonRect, "LawAndOrder_ViewMultipliers".Translate()))
            {
                showMultipliers = true;
                selectedCrime = null;
                selectedCrimeCategory = null;
                SoundDefOf.Click.PlayOneShotOnCamera(null);
                return; // Don't continue drawing this panel
            }

            innerRect.yMin += 40f;

            // Category stats box
            Rect statsRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            Widgets.DrawBoxSolid(statsRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect statsTextRect = statsRect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            int totalCrimes = selectedCrimeCategory.GetTotalCrimeCount();

            string stats = $"{"LawAndOrder_TotalCrimes".Translate()}: {totalCrimes}";

            Widgets.Label(statsTextRect, stats);
            Text.Anchor = TextAnchor.UpperLeft;

            innerRect.yMin += 70f;

            // Bulk operations section
            Rect configHeaderRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(configHeaderRect, "LawAndOrder_BulkOperations".Translate());
            Text.Font = GameFont.Small;

            innerRect.yMin += 30f;

            // Penalty inputs
            Rect minPenaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 24f);
            Widgets.Label(minPenaltyLabelRect, "LawAndOrder_MinPenalty".Translate());

            Rect minPenaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.55f, innerRect.y, innerRect.width * 0.45f, 24f);
            crimeMinPenaltyInput = Widgets.TextField(minPenaltyInputRect, crimeMinPenaltyInput);

            innerRect.yMin += 30f;

            Rect maxPenaltyLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 24f);
            Widgets.Label(maxPenaltyLabelRect, "LawAndOrder_MaxPenalty".Translate());

            Rect maxPenaltyInputRect = new Rect(innerRect.x + innerRect.width * 0.55f, innerRect.y, innerRect.width * 0.45f, 24f);
            crimeMaxPenaltyInput = Widgets.TextField(maxPenaltyInputRect, crimeMaxPenaltyInput);

            innerRect.yMin += 35f;

            // Bulk update button
            float buttonHeight = 35f;

            Rect updateAllButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
            if (Widgets.ButtonText(updateAllButtonRect, "LawAndOrder_UpdateAllCrimes".Translate()))
            {
                if (int.TryParse(crimeMinPenaltyInput, out int minPenalty) &&
                    int.TryParse(crimeMaxPenaltyInput, out int maxPenalty) &&
                    minPenalty > 0 && maxPenalty >= minPenalty)
                {
                    var crimes = selectedCrimeCategory.GetAllCrimes();
                    int count = 0;
                    foreach (var crime in crimes)
                    {
                        crime.StagePenaltyChange(minPenalty, maxPenalty);
                        count++;
                    }
                    Messages.Message(
                        $"{count} crimes in {selectedCrimeCategory.GetLabel()} staged for update (not yet committed)",
                        MessageTypeDefOf.PositiveEvent
                    );
                }
                else
                {
                    Messages.Message(
                        "LawAndOrder_InvalidPenaltyRange".Translate(),
                        MessageTypeDefOf.RejectInput
                    );
                }
            }
        }

        private void DrawMultipliersConfiguration(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 100f, 32f);
            Widgets.Label(headerRect, "LawAndOrder_PenaltyMultipliers".Translate());
            Text.Font = GameFont.Small;

            // Back button
            Rect backButtonRect = new Rect(innerRect.xMax - 90f, innerRect.y, 90f, 32f);
            if (Widgets.ButtonText(backButtonRect, "LawAndOrder_Back".Translate()))
            {
                showMultipliers = false;
                selectedMultiplier = null;
                SoundDefOf.Click.PlayOneShotOnCamera(null);
            }

            innerRect.yMin += 40f;

            // Get all multipliers
            var crimeManager = WorldComponent_CrimePenaltyManager.Instance;
            var multipliers = crimeManager.PenaltyMultipliers.ToList();

            // Draw multipliers list
            float rowHeight = 50f;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, multipliers.Count * rowHeight);
            Widgets.BeginScrollView(innerRect, ref multiplierListScrollPos, viewRect);

            float yPos = 0f;
            foreach (var multiplier in multipliers)
            {
                Rect rowRect = new Rect(0f, yPos, viewRect.width, rowHeight - 2f);
                DrawMultiplierRow(rowRect, multiplier);
                yPos += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private void DrawMultiplierRow(Rect rect, CrimePenaltyMultiplier multiplier)
        {
            bool isSelected = selectedMultiplier == multiplier;
            bool hasPendingChange = multiplier.hasPendingChanges;

            // Highlight pending changes with a special color
            if (hasPendingChange)
            {
                Widgets.DrawBoxSolid(rect, new Color(1f, 0.8f, 0.2f, 0.2f));
            }

            if (isSelected)
            {
                Widgets.DrawHighlight(rect);
            }

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawLightHighlight(rect);
            }

            Widgets.DrawBox(rect);

            Rect innerRect = rect.ContractedBy(5f);

            // Multiplier name
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect nameRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 20f);

            string labelText = GetMultiplierLabel(multiplier.multiplierType);
            if (hasPendingChange)
            {
                GUI.color = new Color(1f, 0.8f, 0.2f);
                labelText += " [PENDING]";
            }

            Widgets.Label(nameRect, labelText);
            GUI.color = Color.white;

            // Current value
            Rect valueRect = new Rect(nameRect.x, nameRect.yMax + 2f, innerRect.width * 0.5f, 18f);
            Text.Font = GameFont.Tiny;

            float displayValue = hasPendingChange ? multiplier.pendingMultiplierValue : multiplier.multiplierValue;
            string valueText = multiplier.enabled ? $"Value: {displayValue:F2}x" : "Disabled";

            GUI.color = multiplier.enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(valueRect, valueText);
            GUI.color = Color.white;

            // Edit button
            Rect editButtonRect = new Rect(innerRect.xMax - 80f, innerRect.y + (innerRect.height - 30f) / 2f, 80f, 30f);
            if (Widgets.ButtonText(editButtonRect, "LawAndOrder_Edit".Translate()))
            {
                selectedMultiplier = multiplier;
                selectedCrime = null;
                selectedCrimeCategory = null;
                showMultipliers = false;

                // Load current value
                multiplierValueInput = displayValue.ToString("F2");

                SoundDefOf.Click.PlayOneShotOnCamera(null);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawIndividualMultiplierConfiguration(Rect rect)
        {
            if (selectedMultiplier == null) return;

            Widgets.DrawMenuSection(rect);

            Rect innerRect = rect.ContractedBy(10f);

            // Header with multiplier name
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width - 100f, 32f);
            Widgets.Label(headerRect, GetMultiplierLabel(selectedMultiplier.multiplierType));
            Text.Font = GameFont.Small;

            // Back button
            Rect backButtonRect = new Rect(innerRect.xMax - 90f, innerRect.y, 90f, 32f);
            if (Widgets.ButtonText(backButtonRect, "LawAndOrder_Back".Translate()))
            {
                selectedMultiplier = null;
                showMultipliers = true;
                SoundDefOf.Click.PlayOneShotOnCamera(null);
                return; // Don't continue drawing this panel
            }

            innerRect.yMin += 40f;

            // Multiplier description
            if (!string.IsNullOrEmpty(selectedMultiplier.description))
            {
                Rect descRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 80f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, selectedMultiplier.description);
                Text.Font = GameFont.Small;
                innerRect.yMin += 90f;
            }

            // Multiplier stats box
            Rect statsRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 60f);
            Widgets.DrawBoxSolid(statsRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect statsTextRect = statsRect.ContractedBy(5f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            string stats = $"Type: {selectedMultiplier.multiplierType}\n";
            stats += $"Current Value: {selectedMultiplier.multiplierValue:F2}x\n";
            stats += $"Status: {(selectedMultiplier.enabled ? "Enabled" : "Disabled")}";

            Widgets.Label(statsTextRect, stats);
            Text.Anchor = TextAnchor.UpperLeft;

            innerRect.yMin += 70f;

            // Show pending change indicator if there is one
            if (selectedMultiplier.hasPendingChanges)
            {
                Rect pendingIndicatorRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
                Widgets.DrawBoxSolid(pendingIndicatorRect, new Color(1f, 0.8f, 0.2f, 0.3f));
                Rect pendingTextRect = pendingIndicatorRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(1f, 0.8f, 0.2f);

                string pendingText = $"Pending: {selectedMultiplier.pendingMultiplierValue:F2}x";
                if (!selectedMultiplier.GetEffectiveEnabled())
                {
                    pendingText += " (Will be disabled)";
                }

                Widgets.Label(pendingTextRect, pendingText);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                innerRect.yMin += 35f;
            }

            // Enabled checkbox
            Rect checkboxRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 24f);
            bool currentEnabled = selectedMultiplier.hasPendingChanges ? selectedMultiplier.GetEffectiveEnabled() : selectedMultiplier.enabled;
            bool newEnabled = currentEnabled;
            Widgets.CheckboxLabeled(checkboxRect, "LawAndOrder_Enabled".Translate(), ref newEnabled);

            innerRect.yMin += 30f;

            // Multiplier value input
            Rect valueLabelRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.5f, 24f);
            Widgets.Label(valueLabelRect, "LawAndOrder_MultiplierValue".Translate());

            Rect valueInputRect = new Rect(innerRect.x + innerRect.width * 0.55f, innerRect.y, innerRect.width * 0.45f, 24f);
            multiplierValueInput = Widgets.TextField(valueInputRect, multiplierValueInput);

            innerRect.yMin += 35f;

            // Update button
            float buttonHeight = 35f;

            Rect updateButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
            if (Widgets.ButtonText(updateButtonRect, "LawAndOrder_UpdateMultiplier".Translate()))
            {
                if (float.TryParse(multiplierValueInput, out float value) && value >= 0)
                {
                    // Stage the change
                    selectedMultiplier.StageMultiplierChange(value); selectedMultiplier.StageEnabledChange(newEnabled);
                    Messages.Message(
                        $"{GetMultiplierLabel(selectedMultiplier.multiplierType)} update staged (not yet committed)",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
                else
                {
                    Messages.Message(
                        "LawAndOrder_InvalidMultiplierValue".Translate(),
                        MessageTypeDefOf.RejectInput
                    );
                }
            }

            innerRect.yMin += buttonHeight + 10f;

            // Reset button (cancel pending changes for this multiplier)
            if (selectedMultiplier.hasPendingChanges)
            {
                Rect resetButtonRect = new Rect(innerRect.x, innerRect.y, innerRect.width, buttonHeight);
                if (Widgets.ButtonText(resetButtonRect, "LawAndOrder_ResetMultiplier".Translate()))
                {
                    selectedMultiplier.ClearPendingChanges();
                    multiplierValueInput = selectedMultiplier.multiplierValue.ToString("F2");
                    Messages.Message(
                        $"{GetMultiplierLabel(selectedMultiplier.multiplierType)} reset to current value",
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            }
        }

        private void DrawCrimeCommitButtons(Rect rect)
        {
            var crimeManager = WorldComponent_CrimePenaltyManager.Instance;
            bool isGodMode = DebugSettings.godMode;
            bool wouldBeInLockout = false;
            float daysRemaining = 0f;

            // Check if we would be in lockout (without god mode bypass)
            if (crimeManager.lastCommitTick >= 0)
            {
                int ticksSinceCommit = Find.TickManager.TicksGame - crimeManager.lastCommitTick;
                wouldBeInLockout = ticksSinceCommit < 15 * 60000; // LOCKOUT_DURATION_TICKS
                if (wouldBeInLockout)
                {
                    int ticksRemaining = (15 * 60000) - ticksSinceCommit;
                    daysRemaining = ticksRemaining / 60000f;
                }
            }

            bool isInLockout = crimeManager.IsInLockout();

            // Show god mode bypass indicator if god mode is active and would be locked
            if (isGodMode && wouldBeInLockout)
            {
                Rect godModeRect = new Rect(rect.x, rect.y, rect.width, rect.height / 2f);
                Widgets.DrawBoxSolid(godModeRect, new Color(0.2f, 0.8f, 0.2f, 0.3f));

                Rect godModeTextRect = godModeRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = new Color(0.3f, 1f, 0.3f);
                Widgets.Label(godModeTextRect, $"GOD MODE: LOCKOUT BYPASSED");

                Text.Font = GameFont.Tiny;
                godModeTextRect.y += 16f;
                Widgets.Label(godModeTextRect, $"(Would be locked for {daysRemaining:F1} more day(s))");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                rect.y += rect.height / 2f + 5f;
                rect.height = rect.height / 2f - 5f;
            }
            // Show lockout status prominently if not bypassed
            else if (isInLockout)
            {
                Rect lockoutRect = new Rect(rect.x, rect.y, rect.width, rect.height / 2f);
                Widgets.DrawBoxSolid(lockoutRect, new Color(0.8f, 0.2f, 0.2f, 0.3f));

                Rect lockoutTextRect = lockoutRect.ContractedBy(5f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = new Color(1f, 0.3f, 0.3f);
                Widgets.Label(lockoutTextRect, $"CRIME PENALTY POLICY LOCKED");

                Text.Font = GameFont.Tiny;
                lockoutTextRect.y += 16f;
                Widgets.Label(lockoutTextRect, $"Cannot commit changes for {daysRemaining:F1} more day(s)");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                rect.y += rect.height / 2f + 5f;
                rect.height = rect.height / 2f - 5f;
            }

            // Count pending changes
            int pendingCount = crimeManager.CrimeDefinitions.Count(c => c.hasPendingChanges) +
                               crimeManager.PenaltyMultipliers.Count(m => m.hasPendingChanges);

            // Only show buttons if there are pending changes or show status message
            if (pendingCount == 0 && !isInLockout && !isGodMode)
            {
                // Show a message indicating no pending changes
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(rect, "No pending changes");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
            else if (pendingCount == 0 && !wouldBeInLockout)
            {
                // Show a message indicating no pending changes (god mode active but no lockout to show)
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(rect, "No pending changes");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float buttonWidth = 200f;
            float buttonHeight = 40f;
            float spacing = 15f;
            float totalWidth = buttonWidth * 2 + spacing;
            float startX = rect.x + (rect.width - totalWidth) / 2f;

            // Pending changes indicator (only show if there are pending changes)
            if (pendingCount > 0)
            {
                Rect pendingLabelRect = new Rect(rect.x, rect.y, rect.width / 3f, buttonHeight);
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(1f, 0.8f, 0.2f);
                Widgets.Label(pendingLabelRect, $"{pendingCount} pending change(s)");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // Commit button (green when available, grey when locked)
                Rect commitButtonRect = new Rect(startX, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);

                if (isInLockout)
                {
                    // Disabled button with tooltip
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    Widgets.ButtonText(commitButtonRect, "Commit Changes (Locked)");
                    GUI.color = Color.white;

                    if (Mouse.IsOver(commitButtonRect))
                    {
                        TooltipHandler.TipRegion(commitButtonRect, $"Crime penalty policy is locked for {daysRemaining:F1} more day(s)");
                    }
                }
                else
                {
                    // Active button
                    GUI.color = new Color(0.3f, 0.8f, 0.3f);
                    if (Widgets.ButtonText(commitButtonRect, "Commit Changes"))
                    {
                        CommitCrimeChanges();
                    }
                    GUI.color = Color.white;
                }

                // Cancel button (red) - always enabled
                Rect cancelButtonRect = new Rect(startX + buttonWidth + spacing, rect.y + (rect.height - buttonHeight) / 2f, buttonWidth, buttonHeight);
                GUI.color = new Color(0.8f, 0.3f, 0.3f);
                if (Widgets.ButtonText(cancelButtonRect, "Cancel Changes"))
                {
                    CancelCrimeChanges();
                }
                GUI.color = Color.white;
            }
        }

        private void CommitCrimeChanges()
        {
            var crimeManager = WorldComponent_CrimePenaltyManager.Instance;
            bool isGodMode = DebugSettings.godMode;

            if (crimeManager.IsInLockout())
            {
                float daysRemaining = crimeManager.GetLockoutDaysRemaining();
                Messages.Message(
                    $"Cannot commit crime penalty changes. Policy is locked for {daysRemaining:F1} more day(s).",
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            int crimeCount = 0;
            int multiplierCount = 0;

            // Commit all pending crime changes
            foreach (var crime in crimeManager.CrimeDefinitions)
            {
                if (crime.hasPendingChanges)
                {
                    crime.CommitPendingChanges();
                    crimeCount++;
                }
            }

            // Commit all pending multiplier changes
            foreach (var multiplier in crimeManager.PenaltyMultipliers)
            {
                if (multiplier.hasPendingChanges)
                {
                    multiplier.CommitPendingChanges();
                    multiplierCount++;
                }
            }

            // Record the commit to start lockout period
            crimeManager.CommitAllPendingChanges();

            // Rebuild tree to show updated values
            crimeTreeNeedsRebuild = true;

            // Show success message
            string message = "Crime penalty changes committed: ";
            List<string> parts = new List<string>();
            if (crimeCount > 0) parts.Add($"{crimeCount} crimes updated");
            if (multiplierCount > 0) parts.Add($"{multiplierCount} multipliers updated");
            message += string.Join(", ", parts);

            // Different message based on god mode
            if (isGodMode)
            {
                message += "\n(God mode: Lockout bypassed)";
            }
            else
            {
                message += "\nCrime penalty policy locked for 15 days.";
            }

            Messages.Message(message, MessageTypeDefOf.TaskCompletion);
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
        }

        private void CancelCrimeChanges()
        {
            var crimeManager = WorldComponent_CrimePenaltyManager.Instance;

            int count = 0;

            // Cancel all pending crime changes
            foreach (var crime in crimeManager.CrimeDefinitions)
            {
                if (crime.hasPendingChanges)
                {
                    crime.ClearPendingChanges();
                    count++;
                }
            }

            // Cancel all pending multiplier changes
            foreach (var multiplier in crimeManager.PenaltyMultipliers)
            {
                if (multiplier.hasPendingChanges)
                {
                    multiplier.ClearPendingChanges();
                    count++;
                }
            }

            Messages.Message($"Cancelled {count} pending crime penalty change(s)", MessageTypeDefOf.NeutralEvent);
            SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
        }
    }
}



