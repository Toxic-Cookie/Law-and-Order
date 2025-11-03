using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hearings;
using LawAndOrder;

namespace Law_and_Order.Source.UI
{
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
            Historical,         // Past criminals (dead, released, etc)
            Contraband          // Contraband item configuration
        }

        private JusticeTab curTab = JusticeTab.ActiveCriminals;
        private List<TabRecord> tabs = new List<TabRecord>();
        private Pawn selectedCriminal;
        private Vector2 criminalListScrollPos;
        private Vector2 selectedCriminalScrollPos;
        private string searchQuery = "";

        // Contraband tab fields
        private ThingDef selectedContrabandItem;
        private ContrabandCategoryNode selectedContrabandCategory;
        private Vector2 contrabandListScrollPos;
        private string contrabandSearchQuery = "";
        private string contrabandPenaltyInput = "";
        private List<ContrabandCategoryNode> contrabandCategoryTree;
        private List<ContrabandCategoryNode> contrabandFlattenedList;
        private bool contrabandTreeNeedsRebuild = true;

        private const float LeftPanelWidth = 0.35f;
        private const float PanelGap = 17f;

        public override Vector2 RequestedTabSize => new Vector2(1010f, 640f);

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
                "LawAndOrder_Historical".Translate(),
                () => { curTab = JusticeTab.Historical; selectedCriminal = null; },
                () => curTab == JusticeTab.Historical
            ));
            tabs.Add(new TabRecord(
                "LawAndOrder_Contraband".Translate(),
                () => { curTab = JusticeTab.Contraband; selectedCriminal = null; },
                () => curTab == JusticeTab.Contraband
            ));
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Adjust rect for tabs and draw them
            // TabDrawer will draw the tabs ABOVE this rect
            inRect.yMin += 45f;
            TabDrawer.DrawTabs<TabRecord>(inRect, tabs, 200f);

            // Handle Contraband tab differently
            if (curTab == JusticeTab.Contraband)
            {
                DrawContrabandUI(inRect);
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
                crimeText += " • " + "LawAndOrder_Imprisoned".Translate();
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

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, crimes.Count * 100f);
            Widgets.BeginScrollView(rect, ref selectedCriminalScrollPos, viewRect);

            float yPos = 0f;
            foreach (var crime in crimes)
            {
                Rect crimeRect = new Rect(0f, yPos, viewRect.width, 95f);
                DrawCrimeEntry(crimeRect, crime);
                yPos += 100f;
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
            Widgets.Label(titleRect, crime.crimeType.ToString());

            // Display the stored debt amount (not recalculated)
            float crimeDebt = crime.debtAmount;

            // Fallback: if debtAmount is 0 (old saves), calculate it
            if (crimeDebt <= 0)
            {
                crimeDebt = DebtUtils.CalculateDebtForCrime(crime);
            }

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

                case JusticeTab.Historical:
                    return criminalsWithRecords.Where(p => p.Dead).ToList();

                default:
                    return criminalsWithRecords;
            }
        }

        private void DrawContrabandUI(Rect inRect)
        {
            // Split into left panel (item list) and right panel (configuration)
            Rect leftPanel = inRect;
            leftPanel.width = inRect.width * LeftPanelWidth;

            Rect rightPanel = inRect;
            rightPanel.xMin = leftPanel.xMax + PanelGap;

            // Draw the item list
            DrawContrabandItemList(leftPanel);

            // Draw the configuration panel
            DrawContrabandConfiguration(rightPanel);
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

                    // Load penalty if already set as contraband
                    var existingDef = WorldComponent_ContrabandManager.Instance.GetContrabandDefinition(item);
                    if (existingDef != null)
                    {
                        contrabandPenaltyInput = existingDef.silverPenaltyPerItem.ToString();
                    }
                    else
                    {
                        contrabandPenaltyInput = "50"; // Default penalty
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

                if (isContraband)
                {
                    GUI.color = new Color(0.9f, 0.2f, 0.2f);
                    var contrabandDef = WorldComponent_ContrabandManager.Instance.GetContrabandDefinition(item);
                    Widgets.Label(textRect, $"{item.LabelCap} ({contrabandDef.silverPenaltyPerItem} silver)");
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.Label(textRect, item.LabelCap);
                }

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

            Rect configHeaderRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(configHeaderRect, "LawAndOrder_ContrabandSettings".Translate());
            Text.Font = GameFont.Small;

            innerRect.yMin += 30f;

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
                        WorldComponent_ContrabandManager.Instance.SetContraband(selectedContrabandItem, penalty);
                        Messages.Message(
                            "LawAndOrder_ContrabandUpdated".Translate(selectedContrabandItem.LabelCap),
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

                Rect removeButtonRect = new Rect(innerRect.x + buttonWidth + 10f, innerRect.y, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(removeButtonRect, "LawAndOrder_RemoveContraband".Translate()))
                {
                    WorldComponent_ContrabandManager.Instance.RemoveContraband(selectedContrabandItem);
                    Messages.Message(
                        "LawAndOrder_ContrabandRemoved".Translate(selectedContrabandItem.LabelCap),
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
                        WorldComponent_ContrabandManager.Instance.SetContraband(selectedContrabandItem, penalty);
                        Messages.Message(
                            "LawAndOrder_ContrabandAdded".Translate(selectedContrabandItem.LabelCap),
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
                    int count = WorldComponent_ContrabandManager.Instance.SetContrabandBulk(items, penalty);
                    Messages.Message(
                        "LawAndOrder_BulkContrabandAdded".Translate(count, categoryLabel),
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
                    int count = WorldComponent_ContrabandManager.Instance.UpdateContrabandBulk(items, penalty);
                    if (count > 0)
                    {
                        Messages.Message(
                            "LawAndOrder_BulkContrabandUpdated".Translate(count, categoryLabel),
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
                int count = WorldComponent_ContrabandManager.Instance.RemoveContrabandBulk(items);
                if (count > 0)
                {
                    Messages.Message(
                        "LawAndOrder_BulkContrabandRemoved".Translate(count, categoryLabel),
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
    }
}
