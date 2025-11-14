using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Justice;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// Inspector tab for viewing a pawn's criminal record.
    /// Shows crimes grouped by visibility state (Hidden/Suspected/Convicted).
    /// Phase 3: Basic UI Implementation
    /// </summary>
    public class ITab_Pawn_Judiciary : ITab
    {
        private Vector2 scrollPos = Vector2.zero;
        private const float PADDING = 10f;
        private const float ROW_HEIGHT = 30f;
        private const float SECTION_SPACING = 15f;

        public ITab_Pawn_Judiciary()
        {
            this.size = new Vector2(500f, 600f);
            this.labelKey = "LawAndOrder_TabJudiciary";
        }

        protected override void FillTab()
        {
            Rect mainRect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(PADDING);

            Hediff_Crimes criminalRecord = CrimeUtils.TryGetCriminalRecord(SelPawn);

            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(mainRect, "LawAndOrder_ITab_NoCriminalRecord".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Create scrollable view
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, GetContentHeight(criminalRecord));

            Widgets.BeginScrollView(mainRect, ref scrollPos, viewRect);

            float yPos = 0f;

            // Pawn name header
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(0f, yPos, viewRect.width, 35f);
            Widgets.Label(headerRect, SelPawn.NameFullColored);
            yPos += 35f;
            Text.Font = GameFont.Small;

            // Case information (if any)
            yPos = DrawCaseInfo(viewRect, yPos);

            yPos += SECTION_SPACING;

            // Suspected crimes section
            var suspectedCrimes = criminalRecord.GetSuspectedCrimes();
            if (suspectedCrimes.Count > 0)
            {
                yPos = DrawCrimeSection(viewRect, yPos, "LawAndOrder_State_Suspected".Translate(),
                    suspectedCrimes, new Color(1f, 0.8f, 0f));
                yPos += SECTION_SPACING;
            }

            // Convicted crimes section
            var convictedCrimes = criminalRecord.GetConvictedCrimes();
            if (convictedCrimes.Count > 0)
            {
                yPos = DrawCrimeSection(viewRect, yPos, "LawAndOrder_State_Convicted".Translate(),
                    convictedCrimes, new Color(1f, 0.3f, 0.3f));
                yPos += SECTION_SPACING;
            }

            // Hidden crimes section (only show in god mode)
            if (DebugSettings.godMode)
            {
                var hiddenCrimes = criminalRecord.GetHiddenCrimes();
                if (hiddenCrimes.Count > 0)
                {
                    yPos = DrawCrimeSection(viewRect, yPos, $"{"LawAndOrder_State_Hidden".Translate()} (Debug)",
                        hiddenCrimes, Color.gray);
                }
            }

            Widgets.EndScrollView();
        }

        private float DrawCaseInfo(Rect rect, float yPos)
        {
            var justiceManager = WorldComponent_JusticeManager.Instance;
            if (justiceManager == null)
                return yPos;

            var activeCase = justiceManager.GetActiveCase(SelPawn);

            if (activeCase == null)
                return yPos;

            // Case info section
            Rect sectionHeaderRect = new Rect(0f, yPos, rect.width, 25f);
            Widgets.DrawBoxSolid(sectionHeaderRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(sectionHeaderRect.ContractedBy(5f), $"{"LawAndOrder_ActiveCase".Translate()}: #{activeCase.caseId}");
            Text.Anchor = TextAnchor.UpperLeft;
            yPos += 25f;

            // Case details
            Rect caseDetailsRect = new Rect(PADDING, yPos, rect.width - PADDING * 2, 50f);
            Text.Font = GameFont.Tiny;

            var crimes = activeCase.GetAssociatedCrimes();
            string caseInfo = $"{crimes.Count} {"LawAndOrder_Crimes".Translate()}\n";
            caseInfo += $"{"LawAndOrder_CaseOpenedDaysAgo".Translate(activeCase.DaysOpen)}\n";

            var mostSerious = activeCase.GetMostSeriousCrime();
            caseInfo += $"{"LawAndOrder_MostSerious".Translate()}: {mostSerious}";

            Widgets.Label(caseDetailsRect, caseInfo);

            Text.Font = GameFont.Small;

            return yPos + 50f;
        }

        private float DrawCrimeSection(Rect rect, float yPos, string sectionTitle, List<Crime> crimes, Color titleColor)
        {
            // Section header
            Rect sectionHeaderRect = new Rect(0f, yPos, rect.width, 25f);
            Widgets.DrawBoxSolid(sectionHeaderRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            GUI.color = titleColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(sectionHeaderRect.ContractedBy(5f), $"{sectionTitle} ({crimes.Count})");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            yPos += 25f;

            // Crime list
            foreach (var crime in crimes.OrderByDescending(c => c.tickCommitted))
            {
                Rect crimeRect = new Rect(0f, yPos, rect.width, ROW_HEIGHT);
                DrawCrimeEntry(crimeRect, crime);
                yPos += ROW_HEIGHT;
            }

            return yPos;
        }

        private void DrawCrimeEntry(Rect rect, Crime crime)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Widgets.DrawBox(rect);

            Rect contentRect = rect.ContractedBy(5f);

            // Crime label
            Text.Font = GameFont.Small;
            string crimeLabel = crime.GetCrimeLabel();
            Rect labelRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.6f, contentRect.height);
            Widgets.Label(labelRect, crimeLabel);

            // Details (right side)
            Text.Font = GameFont.Tiny;
            Rect detailsRect = new Rect(contentRect.x + contentRect.width * 0.6f, contentRect.y, contentRect.width * 0.4f, contentRect.height);

            string details = "";

            if (crime.victim != null)
            {
                details += $"{crime.victim.NameShortColored}\n";
            }

            if (crime.evidenceStrength > 0f)
            {
                details += $"{crime.evidenceStrength:P0} {"LawAndOrder_Evidence".Translate()}";
            }
            else
            {
                details += $"{crime.DaysAgo}d ago";
            }

            Widgets.Label(detailsRect, details);

            Text.Font = GameFont.Small;

            // Tooltip with full details
            if (Mouse.IsOver(rect))
            {
                string tooltip = GetCrimeTooltip(crime);
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private string GetCrimeTooltip(Crime crime)
        {
            List<string> parts = new List<string>();

            parts.Add($"<b>{crime.GetCrimeLabel()}</b>");
            parts.Add("");

            parts.Add($"{"LawAndOrder_State".Translate()}: {GetStateLabel(crime.visibilityState)}");

            if (crime.victim != null)
            {
                parts.Add($"{"LawAndOrder_Victim".Translate()}: {crime.victim.NameFullColored}");
            }

            if (crime.witnesses.Count > 0)
            {
                parts.Add($"{"LawAndOrder_Witnesses".Translate()}: {crime.witnesses.Count}");
                foreach (var witness in crime.witnesses.Take(3))
                {
                    if (witness != null)
                        parts.Add($"  - {witness.NameShortColored}");
                }
                if (crime.witnesses.Count > 3)
                {
                    parts.Add($"  ... {"LawAndOrder_AndMore".Translate(crime.witnesses.Count - 3)}");
                }
            }

            if (crime.evidenceStrength > 0f)
            {
                parts.Add($"{"LawAndOrder_EvidenceStrength".Translate()}: {crime.evidenceStrength:P0}");
            }

            parts.Add($"{"LawAndOrder_Committed".Translate()}: {crime.DaysAgo} {"LawAndOrder_DaysAgo".Translate()}");

            if (crime.caseId > 0)
            {
                parts.Add($"{"LawAndOrder_CaseNumber".Translate()}: #{crime.caseId}");
            }

            if (!string.IsNullOrEmpty(crime.additionalInfo))
            {
                parts.Add("");
                parts.Add(crime.additionalInfo);
            }

            return string.Join("\n", parts);
        }

        private string GetStateLabel(CrimeVisibilityState state)
        {
            return state switch
            {
                CrimeVisibilityState.Hidden => "LawAndOrder_State_Hidden".Translate(),
                CrimeVisibilityState.Suspected => "LawAndOrder_State_Suspected".Translate(),
                CrimeVisibilityState.Convicted => "LawAndOrder_State_Convicted".Translate(),
                _ => "Unknown"
            };
        }

        private float GetContentHeight(Hediff_Crimes criminalRecord)
        {
            float height = 35f; // Header
            height += 50f; // Case info (if any)
            height += SECTION_SPACING;

            var suspectedCrimes = criminalRecord.GetSuspectedCrimes();
            if (suspectedCrimes.Count > 0)
            {
                height += 25f; // Section header
                height += suspectedCrimes.Count * ROW_HEIGHT;
                height += SECTION_SPACING;
            }

            var convictedCrimes = criminalRecord.GetConvictedCrimes();
            if (convictedCrimes.Count > 0)
            {
                height += 25f; // Section header
                height += convictedCrimes.Count * ROW_HEIGHT;
                height += SECTION_SPACING;
            }

            if (DebugSettings.godMode)
            {
                var hiddenCrimes = criminalRecord.GetHiddenCrimes();
                if (hiddenCrimes.Count > 0)
                {
                    height += 25f; // Section header
                    height += hiddenCrimes.Count * ROW_HEIGHT;
                    height += SECTION_SPACING;
                }
            }

            return height + 50f; // Extra padding
        }
    }
}
