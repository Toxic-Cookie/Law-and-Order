using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hearings;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// ITab for viewing a pawn's criminal record and debt from their bio screen
    /// </summary>
    public class ITab_Pawn_Judiciary : ITab
    {
        private Vector2 scrollPosition;
        private const float LeftColumnWidth = 0.5f;

        public ITab_Pawn_Judiciary()
        {
            this.size = new Vector2(500f, 450f);
            this.labelKey = "TabJudiciary";
            this.tutorTag = "Judiciary";
        }

        /// <summary>
        /// Only show this tab if the pawn has a criminal record
        /// </summary>
        public override bool IsVisible
        {
            get
            {
                return CrimeUtils.HasCriminalRecord(this.SelPawn);
            }
        }

        protected override void FillTab()
        {
            Pawn pawn = this.SelPawn;
            if (pawn == null)
            {
                return;
            }

            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            var criminalRecord = CrimeUtils.TryGetCriminalRecord(pawn);
            var debtRecord = DebtUtils.TryGetDebtRecord(pawn);

            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                // No criminal record
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "LawAndOrder_ITab_NoCriminalRecord".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Split into two columns
            Rect leftColumn = rect;
            leftColumn.width = rect.width * LeftColumnWidth - 5f;

            Rect rightColumn = rect;
            rightColumn.xMin = leftColumn.xMax + 10f;

            // Draw left column (crimes)
            DrawCrimesColumn(leftColumn, criminalRecord);

            // Draw right column (debt and hearing info)
            DrawDebtAndHearingColumn(rightColumn, debtRecord, criminalRecord);
        }

        private void DrawCrimesColumn(Rect rect, Hediff_Crimes criminalRecord)
        {
            // Header
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 30f);
            Widgets.Label(headerRect, "LawAndOrder_ITab_CrimesHeader".Translate(criminalRecord.TotalCrimeCount));
            Text.Font = GameFont.Small;

            rect.yMin += 35f;

            // Crimes list
            var crimes = criminalRecord.Crimes.ToList();
            crimes.Reverse(); // Most recent first

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, crimes.Count * 70f);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float yPos = 0f;
            foreach (var crime in crimes)
            {
                Rect crimeRect = new Rect(0f, yPos, viewRect.width, 65f);
                DrawCrimeEntry(crimeRect, crime);
                yPos += 70f;
            }

            Widgets.EndScrollView();
        }

        private void DrawCrimeEntry(Rect rect, Crime crime)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            Widgets.DrawBox(rect);

            Rect innerRect = rect.ContractedBy(5f);

            // Crime type
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 18f);

            // Color code by severity
            switch (crime.crimeType)
            {
                case CrimeType.Murder:
                    GUI.color = new Color(1f, 0.2f, 0.2f);
                    break;
                case CrimeType.Assault:
                case CrimeType.Kidnapping:
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }

            Widgets.Label(titleRect, crime.crimeType.ToString());
            GUI.color = Color.white;

            // Debt amount
            float crimeDebt = crime.debtAmount > 0 ? crime.debtAmount : DebtUtils.CalculateDebtForCrime(crime);
            if (crimeDebt > 0)
            {
                Rect debtRect = new Rect(innerRect.x + innerRect.width * 0.7f, innerRect.y, innerRect.width * 0.3f, 18f);
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                Widgets.Label(debtRect, $"{crimeDebt:F0}§");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // Details
            Text.Font = GameFont.Tiny;
            Rect detailsRect = new Rect(innerRect.x, innerRect.y + 20f, innerRect.width, 40f);

            string details = "";
            if (crime.victim != null)
            {
                details += "LawAndOrder_ITab_Victim".Translate(crime.victim.LabelShort) + "\n";
            }
            if (crime.damageDealt > 0)
            {
                details += "LawAndOrder_ITab_Damage".Translate(crime.damageDealt.ToString("F1")) + " | ";
            }
            details += "LawAndOrder_ITab_DaysAgoShort".Translate(crime.DaysAgo);

            Widgets.Label(detailsRect, details);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawDebtAndHearingColumn(Rect rect, Hediff_Debt debtRecord, Hediff_Crimes criminalRecord)
        {
            float yPos = 0f;

            // Debt section
            Text.Font = GameFont.Medium;
            Rect debtHeaderRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(debtHeaderRect, "LawAndOrder_ITab_DebtHeader".Translate());
            yPos += 35f;

            Text.Font = GameFont.Small;
            if (debtRecord != null && debtRecord.CurrentDebt > 0)
            {
                Rect debtInfoRect = new Rect(rect.x, rect.y + yPos, rect.width, 80f);
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                string debtInfo = "LawAndOrder_ITab_DebtCurrent".Translate(debtRecord.CurrentDebt.ToString("F0")) + "\n";
                GUI.color = Color.white;
                debtInfo += "LawAndOrder_ITab_DebtTotalOwed".Translate(debtRecord.TotalDebtOwed.ToString("F0")) + "\n";
                debtInfo += "LawAndOrder_ITab_DebtTotalPaid".Translate(debtRecord.TotalDebtPaid.ToString("F0")) + "\n";
                debtInfo += "LawAndOrder_ITab_EstLabor".Translate(debtRecord.EstimatedDaysOfLabor());

                Widgets.Label(debtInfoRect, debtInfo);
                yPos += 90f;
            }
            else if (debtRecord != null && debtRecord.TotalDebtOwed > 0)
            {
                Rect paidRect = new Rect(rect.x, rect.y + yPos, rect.width, 40f);
                GUI.color = new Color(0.2f, 0.8f, 0.2f);
                Widgets.Label(paidRect, "LawAndOrder_ITab_DebtFullyPaid".Translate(debtRecord.TotalDebtOwed.ToString("F0")));
                GUI.color = Color.white;
                yPos += 50f;
            }
            else
            {
                Rect noDebtRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
                Widgets.Label(noDebtRect, "LawAndOrder_ITab_NoDebt".Translate());
                yPos += 40f;
            }

            yPos += 20f;

            // Hearing section
            Text.Font = GameFont.Medium;
            Rect hearingHeaderRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(hearingHeaderRect, "LawAndOrder_ITab_HearingHeader".Translate());
            yPos += 35f;

            Text.Font = GameFont.Small;
            var hearing = criminalRecord.Hearing;

            Rect hearingInfoRect = new Rect(rect.x, rect.y + yPos, rect.width, 120f);
            string hearingInfo = "LawAndOrder_ITab_Status".Translate(hearing.status) + "\n";

            if (hearing.status == HearingStatus.Completed)
            {
                hearingInfo += "LawAndOrder_ITab_Completed".Translate(hearing.DaysSinceCompleted) + "\n";
                if (hearing.adjudicator != null)
                {
                    hearingInfo += "LawAndOrder_ITab_Adjudicator".Translate(hearing.adjudicator.LabelShort) + "\n";
                }

                if (hearing.pleaBargainOutcome != PleaBargainOutcome.NotAttempted)
                {
                    hearingInfo += "\n" + "LawAndOrder_ITab_Plea".Translate(hearing.pleaBargainOutcome) + "\n";
                    hearingInfo += "LawAndOrder_ITab_Roll".Translate(hearing.pleaBargainRoll);
                }
            }
            else if (hearing.status == HearingStatus.Scheduled)
            {
                hearingInfo += "LawAndOrder_ITab_Scheduled".Translate(hearing.DaysSinceScheduled) + "\n";
                if (hearing.adjudicator != null)
                {
                    hearingInfo += "LawAndOrder_ITab_Adjudicator".Translate(hearing.adjudicator.LabelShort);
                }
            }
            else
            {
                hearingInfo += "LawAndOrder_ITab_NotScheduled".Translate();
            }

            Widgets.Label(hearingInfoRect, hearingInfo);
            yPos += 130f;

            // Button to open Justice tab
            Rect buttonRect = new Rect(rect.x, rect.yMax - 40f, rect.width, 35f);
            if (Widgets.ButtonText(buttonRect, "LawAndOrder_ITab_OpenJusticeTab".Translate()))
            {
                MainButtonDefOf.Menu.Worker.Activate();
                // Find and open the Law and Order main tab
                var justiceTab = DefDatabase<MainButtonDef>.AllDefsListForReading
                    .FirstOrDefault(d => d.defName == "LawAndOrder_Justice");
                if (justiceTab != null)
                {
                    justiceTab.Worker.Activate();
                }
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            this.size = new Vector2(500f, 450f);
        }
    }
}
