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
                Widgets.Label(rect, "No criminal record");
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
            Widgets.Label(headerRect, $"Crimes ({criminalRecord.TotalCrimeCount})");
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
                details += $"Victim: {crime.victim.LabelShort}\n";
            }
            if (crime.damageDealt > 0)
            {
                details += $"Damage: {crime.damageDealt:F1} | ";
            }
            details += $"{crime.DaysAgo}d ago";

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
            Widgets.Label(debtHeaderRect, "Debt");
            yPos += 35f;

            Text.Font = GameFont.Small;
            if (debtRecord != null && debtRecord.CurrentDebt > 0)
            {
                Rect debtInfoRect = new Rect(rect.x, rect.y + yPos, rect.width, 80f);
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                string debtInfo = $"Current: {debtRecord.CurrentDebt:F0} silver\n";
                GUI.color = Color.white;
                debtInfo += $"Total Owed: {debtRecord.TotalDebtOwed:F0} silver\n";
                debtInfo += $"Total Paid: {debtRecord.TotalDebtPaid:F0} silver\n";
                debtInfo += $"Est. Labor: ~{debtRecord.EstimatedDaysOfLabor()} days";

                Widgets.Label(debtInfoRect, debtInfo);
                yPos += 90f;
            }
            else if (debtRecord != null && debtRecord.TotalDebtOwed > 0)
            {
                Rect paidRect = new Rect(rect.x, rect.y + yPos, rect.width, 40f);
                GUI.color = new Color(0.2f, 0.8f, 0.2f);
                Widgets.Label(paidRect, $"Debt Fully Paid\n({debtRecord.TotalDebtOwed:F0} silver)");
                GUI.color = Color.white;
                yPos += 50f;
            }
            else
            {
                Rect noDebtRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
                Widgets.Label(noDebtRect, "No debt recorded");
                yPos += 40f;
            }

            yPos += 20f;

            // Hearing section
            Text.Font = GameFont.Medium;
            Rect hearingHeaderRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(hearingHeaderRect, "Hearing");
            yPos += 35f;

            Text.Font = GameFont.Small;
            var hearing = criminalRecord.Hearing;

            Rect hearingInfoRect = new Rect(rect.x, rect.y + yPos, rect.width, 120f);
            string hearingInfo = $"Status: {hearing.status}\n";

            if (hearing.status == HearingStatus.Completed)
            {
                hearingInfo += $"Completed: {hearing.DaysSinceCompleted}d ago\n";
                if (hearing.adjudicator != null)
                {
                    hearingInfo += $"Adjudicator: {hearing.adjudicator.LabelShort}\n";
                }

                if (hearing.pleaBargainOutcome != PleaBargainOutcome.NotAttempted)
                {
                    hearingInfo += $"\nPlea: {hearing.pleaBargainOutcome}\n";
                    hearingInfo += $"Roll: {hearing.pleaBargainRoll}";
                }
            }
            else if (hearing.status == HearingStatus.Scheduled)
            {
                hearingInfo += $"Scheduled: {hearing.DaysSinceScheduled}d ago\n";
                if (hearing.adjudicator != null)
                {
                    hearingInfo += $"Adjudicator: {hearing.adjudicator.LabelShort}";
                }
            }
            else
            {
                hearingInfo += "Not scheduled";
            }

            Widgets.Label(hearingInfoRect, hearingInfo);
            yPos += 130f;

            // Button to open Justice tab
            Rect buttonRect = new Rect(rect.x, rect.yMax - 40f, rect.width, 35f);
            if (Widgets.ButtonText(buttonRect, "Open Justice Tab"))
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
