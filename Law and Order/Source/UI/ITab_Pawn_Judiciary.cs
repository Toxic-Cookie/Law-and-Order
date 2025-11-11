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
        private CrimeCategoryUIHelper categoryHelper = new CrimeCategoryUIHelper();

        public ITab_Pawn_Judiciary()
        {
            this.size = new Vector2(630f, 450f);
            this.labelKey = "TabJudiciary";
            this.tutorTag = "Judiciary";
        }

        /// <summary>
        /// Always show this tab for pawns - displays "No criminal record" if none exists
        /// </summary>
        public override bool IsVisible
        {
            get
            {
                // Always show the tab if we have a valid pawn
                return this.SelPawn != null;
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

            // Crimes list - grouped by category
            var crimes = criminalRecord.Crimes.ToList();
            crimes.Reverse(); // Most recent first

            // Group crimes by category
            var groupedCrimes = categoryHelper.GroupCrimesByCategory(crimes);

            // Calculate total height needed
            const float crimeEntryHeight = 70f;
            float totalHeight = categoryHelper.CalculateTotalHeight(groupedCrimes, crimeEntryHeight);

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

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
                        Rect crimeRect = new Rect(10f, yPos, viewRect.width - 10f, 65f);
                        DrawCrimeEntry(crimeRect, crime);
                        yPos += crimeEntryHeight;
                    }
                }
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

            Widgets.Label(titleRect, crime.GetCrimeLabel());
            GUI.color = Color.white;

            // Use pre-calculated debt amount
            float crimeDebt = crime.debtAmount;
            if (crimeDebt > 0)
            {
                Rect debtRect = new Rect(innerRect.x + innerRect.width * 0.7f, innerRect.y, innerRect.width * 0.3f, 18f);
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = new Color(0.9f, 0.6f, 0.2f);
                Widgets.Label(debtRect, $"{crimeDebt:F0}§");

                // Add penalty breakdown tooltip
                string penaltyTooltip = GetPenaltyBreakdownTooltip(crime);
                if (!string.IsNullOrEmpty(penaltyTooltip))
                {
                    TooltipHandler.TipRegion(debtRect, penaltyTooltip);
                }

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

                // Calculate percent paid
                float totalDebt = debtRecord.TotalDebtOwed;
                float paidDebt = debtRecord.TotalDebtPaid;
                float percentPaid = totalDebt > 0 ? (paidDebt / totalDebt) * 100f : 0f;
                debtInfo += string.Format("LawAndOrder_ITab_DebtPercentPaid".Translate(), percentPaid.ToString("F1")) + "\n";
                debtInfo += "LawAndOrder_ITab_EstLabor".Translate(debtRecord.EstimatedDaysOfLabor());

                Widgets.Label(debtInfoRect, debtInfo);
                yPos += 100f;

                // Debt forgiveness button
                bool isEligible = DebtUtils.IsEligibleForDebtForgiveness(this.SelPawn);
                Rect forgivenessButtonRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);

                if (isEligible)
                {
                    if (Widgets.ButtonText(forgivenessButtonRect, "LawAndOrder_ITab_ForgiveDebt".Translate()))
                    {
                        string failReason;
                        if (DebtUtils.TryForgiveDebt(this.SelPawn, out failReason))
                        {
                            Messages.Message("LawAndOrder_DebtForgiveness_Success".Translate(this.SelPawn.LabelShort), MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput);
                        }
                    }
                }
                else
                {
                    // Show disabled button with tooltip
                    GUI.color = new Color(0.5f, 0.5f, 0.5f);
                    Widgets.ButtonText(forgivenessButtonRect, "LawAndOrder_ITab_ForgiveDebt".Translate());
                    GUI.color = Color.white;

                    int requiredPercent = Law_and_Order.Source.Settings.LawAndOrderSettings.DebtForgivenessThreshold.Value;
                    string tooltipText = string.Format("LawAndOrder_ITab_ForgiveDebt_Tooltip".Translate(), requiredPercent, percentPaid.ToString("F1"));
                    TooltipHandler.TipRegion(forgivenessButtonRect, tooltipText);
                }

                yPos += 35f;
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
            string hearingInfo = string.Format("LawAndOrder_ITab_Status".Translate().ToString(), hearing.status) + "\n";

            if (hearing.status == HearingStatus.Completed)
            {
                hearingInfo += "LawAndOrder_ITab_Completed".Translate(hearing.DaysSinceCompleted) + "\n";
                if (hearing.adjudicator != null)
                {
                    hearingInfo += "LawAndOrder_ITab_Adjudicator".Translate(hearing.adjudicator.LabelShort) + "\n";
                }

                if (hearing.pleaBargainOutcome != PleaBargainOutcome.NotAttempted)
                {
                    hearingInfo += "\n" + string.Format("LawAndOrder_ITab_Plea".Translate().ToString(), hearing.pleaBargainOutcome) + "\n";
                    hearingInfo += string.Format("LawAndOrder_ITab_Roll".Translate().ToString(), hearing.pleaBargainRoll);
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
            this.size = new Vector2(630f, 450f);
        }

        /// <summary>
        /// Get a penalty breakdown tooltip showing how the penalty was calculated
        /// </summary>
        private string GetPenaltyBreakdownTooltip(Crime crime)
        {
            if (crime == null || crime.debtAmount <= 0)
            {
                return null;
            }

            // Get the crime penalty manager
            var manager = LawAndOrder.WorldComponent_CrimePenaltyManager.Instance;
            if (manager == null)
            {
                return "LawAndOrder_PenaltyBreakdown_NotAvailable".Translate();
            }

            // Determine crime definition name based on crime type and damage type
            string crimeDefName = GetCrimeDefinitionName(crime);
            var crimeDef = manager.GetCrimeDefinition(crimeDefName);

            if (crimeDef == null)
            {
                // Fallback tooltip without base penalty info
                return string.Format("LawAndOrder_PenaltyBreakdown_Simple".Translate(), crime.debtAmount);
            }

            // Build detailed breakdown
            System.Text.StringBuilder tooltip = new System.Text.StringBuilder();
            tooltip.AppendLine("LawAndOrder_PenaltyBreakdown_Header".Translate());
            tooltip.AppendLine();
            tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_Crime".Translate(), crimeDef.label));
            tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_BaseRange".Translate(), crimeDef.minPenalty, crimeDef.maxPenalty));
            tooltip.AppendLine();

            // Show victim if applicable
            if (crime.victim != null)
            {
                tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_Victim".Translate(), crime.victim.LabelShort));
            }

            // Show damage if applicable
            if (crime.damageDealt > 0)
            {
                tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_Damage".Translate(), crime.damageDealt.ToString("F1")));
            }

            tooltip.AppendLine();
            tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_Final".Translate(), crime.debtAmount));

            // Show global penalty scale if it's not 1.0
            var globalScale = Law_and_Order.Source.Settings.LawAndOrderSettings.GlobalPenaltyScale?.Value ?? 1.0f;
            if (globalScale != 1.0f)
            {
                tooltip.AppendLine(string.Format("LawAndOrder_PenaltyBreakdown_GlobalScale".Translate(), (globalScale * 100).ToString("F0")));
            }

            return tooltip.ToString();
        }

        /// <summary>
        /// Map a Crime object to its corresponding crime definition name
        /// </summary>
        private string GetCrimeDefinitionName(Crime crime)
        {
            // For Assault crimes, use the damageType if available (e.g., "GunshotWound", "StabWound")
            if (crime.crimeType == CrimeType.Assault && !string.IsNullOrEmpty(crime.damageType))
            {
                // Remove spaces from damage type to match crime definition names
                return crime.damageType.Replace(" ", "");
            }

            // Map CrimeType enum to crime definition names
            switch (crime.crimeType)
            {
                case CrimeType.Murder:
                    return "Murder";

                case CrimeType.Kidnapping:
                    return "Kidnapping";

                case CrimeType.Arson:
                    return "Arson";

                case CrimeType.Trespassing:
                    return "Trespassing";

                case CrimeType.PropertyDestruction:
                    // Could be various types like DoorDestruction, BuildingDestruction, etc.
                    // Default to BuildingDestruction as the most common
                    return "BuildingDestruction";

                case CrimeType.Theft:
                    // Could be PettyTheft, Theft, or GrandTheft depending on value
                    // Default to Theft as mid-range
                    return "Theft";

                case CrimeType.Vandalism:
                    return "BuildingDestruction"; // Vandalism uses same penalty calculation

                case CrimeType.AnimalAbuse:
                    return "Assault"; // Animal abuse uses assault-like penalties

                case CrimeType.ContrabandPossession:
                    return "Theft"; // Contraband uses theft-like penalties

                default:
                    return "SuperficialWound"; // Fallback for unknown types
            }
        }
    }
}
