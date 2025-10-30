using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

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
            Historical          // Past criminals (dead, released, etc)
        }

        private JusticeTab curTab = JusticeTab.ActiveCriminals;
        private List<TabRecord> tabs = new List<TabRecord>();
        private Pawn selectedCriminal;
        private Vector2 criminalListScrollPos;
        private Vector2 selectedCriminalScrollPos;
        private string searchQuery = "";

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
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Adjust rect for tabs and draw them
            // TabDrawer will draw the tabs ABOVE this rect
            inRect.yMin += 45f;
            TabDrawer.DrawTabs<TabRecord>(inRect, tabs, 200f);

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

            // Trial status (placeholder for now)
            textRect.y += 20f;
            Widgets.Label(textRect, $"{"LawAndOrder_TrialStatus".Translate()}: {"LawAndOrder_NotScheduled".Translate()}");

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

            // Calculate and display debt for this crime
            float crimeDebt = DebtUtils.CalculateDebtForCrime(crime);
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

            // Schedule Hearing button
            Rect hearingButtonRect = new Rect(rect.x, rect.y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(hearingButtonRect, "LawAndOrder_ScheduleHearing".Translate()))
            {
                ScheduleHearing();
            }

            // Release button (if imprisoned)
            Rect releaseButtonRect = new Rect(rect.x + buttonWidth + 10f, rect.y, buttonWidth, buttonHeight);
            if (selectedCriminal.IsPrisonerOfColony)
            {
                if (Widgets.ButtonText(releaseButtonRect, "LawAndOrder_Release".Translate()))
                {
                    ReleasePrisoner();
                }
            }
            else
            {
                GUI.enabled = false;
                Widgets.ButtonText(releaseButtonRect, "LawAndOrder_Release".Translate());
                GUI.enabled = true;
            }

            // Pardon button
            Rect pardonButtonRect = new Rect(rect.x, rect.y + buttonHeight + 5f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(pardonButtonRect, "LawAndOrder_Pardon".Translate()))
            {
                PardonCriminal();
            }

            // View Character Info button
            Rect infoButtonRect = new Rect(rect.x + buttonWidth + 10f, rect.y + buttonHeight + 5f, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(infoButtonRect, "LawAndOrder_ViewInfo".Translate()))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(selectedCriminal));
            }
        }

        private void ScheduleHearing()
        {
            // Check for courtroom
            if (!CourtroomUtils.HasCourtroom())
            {
                Messages.Message(
                    "LawAndOrder_NoCourtroom".Translate(),
                    MessageTypeDefOf.RejectInput
                );
                return;
            }

            // TODO: Implement actual hearing scheduling logic
            // For now, just show a message
            Messages.Message(
                "LawAndOrder_HearingScheduled".Translate(selectedCriminal.NameShortColored),
                MessageTypeDefOf.PositiveEvent
            );

            if (Prefs.DevMode)
            {
                var bestCourtroom = CourtroomUtils.GetBestCourtroom();
                float quality = bestCourtroom != null ? CourtroomUtils.GetCourtroomQuality(bestCourtroom) : 0f;
                Mod.Log?.Message($"Scheduled hearing for {selectedCriminal.Name} in courtroom (quality: {quality:F1})");
            }
        }

        private void ReleasePrisoner()
        {
            if (selectedCriminal.IsPrisonerOfColony)
            {
                selectedCriminal.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
                Messages.Message(
                    "LawAndOrder_Released".Translate(selectedCriminal.NameShortColored),
                    selectedCriminal,
                    MessageTypeDefOf.NeutralEvent
                );
            }
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
    }
}
