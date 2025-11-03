using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Hearings;

namespace Law_and_Order.Source.UI
{
    /// <summary>
    /// Dialog for conducting a formal hearing with plea bargain option
    /// </summary>
    public class Dialog_ConductHearing : Window
    {
        private Pawn prisoner;
        private Pawn adjudicator;
        private Hediff_Crimes criminalRecord;
        private Hediff_Debt debtRecord;
        private HearingRecord hearing;
        private Vector2 scrollPosition;

        private enum HearingPhase
        {
            ReadingCharges,
            CalculatingDebt,
            PleaBargainOption,
            Sentencing
        }

        private HearingPhase currentPhase = HearingPhase.ReadingCharges;

        public override Vector2 InitialSize => new Vector2(700f, 600f);

        public Dialog_ConductHearing(Pawn prisoner, Pawn adjudicator)
        {
            this.prisoner = prisoner;
            this.adjudicator = adjudicator;
            this.criminalRecord = CrimeUtils.GetOrCreateCriminalRecord(prisoner);
            this.debtRecord = DebtUtils.GetOrCreateDebtRecord(prisoner);
            this.hearing = criminalRecord.Hearing;

            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            Widgets.Label(titleRect, $"Hearing: {prisoner.LabelShort}");
            Text.Font = GameFont.Small;

            inRect.yMin += 45f;

            // Phase indicator
            Rect phaseRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            DrawPhaseIndicator(phaseRect);

            inRect.yMin += 35f;

            // Main content area
            Rect contentRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 80f);
            DrawPhaseContent(contentRect);

            // Buttons at bottom
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - 35f, inRect.width, 35f);
            DrawButtons(buttonRect);
        }

        private void DrawPhaseIndicator(Rect rect)
        {
            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Widgets.DrawBoxSolid(rect, GUI.color);
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.MiddleCenter;
            string phaseText = "";
            switch (currentPhase)
            {
                case HearingPhase.ReadingCharges:
                    phaseText = "Reading of Charges";
                    break;
                case HearingPhase.CalculatingDebt:
                    phaseText = "Calculation of Debt";
                    break;
                case HearingPhase.PleaBargainOption:
                    phaseText = "Plea Bargain";
                    break;
                case HearingPhase.Sentencing:
                    phaseText = "Sentencing";
                    break;
            }
            Widgets.Label(rect, phaseText);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawPhaseContent(Rect rect)
        {
            switch (currentPhase)
            {
                case HearingPhase.ReadingCharges:
                    DrawReadingCharges(rect);
                    break;
                case HearingPhase.CalculatingDebt:
                    DrawCalculatingDebt(rect);
                    break;
                case HearingPhase.PleaBargainOption:
                    DrawPleaBargainOption(rect);
                    break;
                case HearingPhase.Sentencing:
                    DrawSentencing(rect);
                    break;
            }
        }

        private void DrawReadingCharges(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, criminalRecord.Crimes.Count * 80f + 100f);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float yPos = 0f;

            // Adjudicator statement
            Text.Font = GameFont.Small;
            Rect adjudicatorRect = new Rect(0f, yPos, viewRect.width, 60f);
            string statement = $"{adjudicator.LabelShort} reads the charges against {prisoner.LabelShort}:";
            Widgets.Label(adjudicatorRect, statement);
            yPos += 70f;

            // List each crime
            Text.Font = GameFont.Small;
            foreach (var crime in criminalRecord.Crimes)
            {
                Rect crimeRect = new Rect(10f, yPos, viewRect.width - 20f, 75f);
                Widgets.DrawBoxSolid(crimeRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                Widgets.DrawBox(crimeRect);

                Rect crimeContentRect = crimeRect.ContractedBy(5f);

                string crimeText = $"• {crime.crimeType}";
                if (crime.victim != null)
                {
                    crimeText += $" against {crime.victim.LabelShort}";
                }
                if (crime.damageDealt > 0)
                {
                    crimeText += $" ({crime.damageDealt:F1} damage)";
                }
                crimeText += $"\n  {crime.DaysAgo} days ago";

                float crimeDebt = crime.debtAmount > 0 ? crime.debtAmount : DebtUtils.CalculateDebtForCrime(crime);
                crimeText += $"\n  Debt: {crimeDebt:F0} silver";

                Widgets.Label(crimeContentRect, crimeText);
                yPos += 80f;
            }

            Widgets.EndScrollView();
        }

        private void DrawCalculatingDebt(Rect rect)
        {
            float yPos = 0f;

            // Total crimes
            Text.Font = GameFont.Medium;
            Rect crimeCountRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(crimeCountRect, $"Total Crimes: {criminalRecord.TotalCrimeCount}");
            yPos += 40f;

            // Break down debt by category
            Text.Font = GameFont.Small;
            var crimesByType = criminalRecord.Crimes.GroupBy(c => c.crimeType);

            foreach (var group in crimesByType)
            {
                float categoryDebt = 0f;
                foreach (var crime in group)
                {
                    float crimeDebt = crime.debtAmount > 0 ? crime.debtAmount : DebtUtils.CalculateDebtForCrime(crime);
                    categoryDebt += crimeDebt;
                }

                Rect categoryRect = new Rect(rect.x + 20f, rect.y + yPos, rect.width - 40f, 25f);
                string categoryText = $"{group.Key} (x{group.Count()}): {categoryDebt:F0} silver";
                Widgets.Label(categoryRect, categoryText);
                yPos += 30f;
            }

            yPos += 20f;

            // Total debt
            Text.Font = GameFont.Medium;
            Rect totalRect = new Rect(rect.x, rect.y + yPos, rect.width, 40f);
            GUI.color = new Color(0.9f, 0.6f, 0.2f);
            Widgets.Label(totalRect, $"Total Debt Owed: {debtRecord.CurrentDebt:F0} silver");
            GUI.color = Color.white;
            yPos += 50f;

            // Estimated labor time
            Text.Font = GameFont.Small;
            int estimatedDays = debtRecord.EstimatedDaysOfLabor();
            Rect laborRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(laborRect, $"Estimated labor time: ~{estimatedDays} days");
        }

        private void DrawPleaBargainOption(Rect rect)
        {
            float yPos = 0f;

            if (hearing.CanAttemptPleaBargain)
            {
                // Explain plea bargain
                Text.Font = GameFont.Small;
                Rect explanationRect = new Rect(rect.x, rect.y + yPos, rect.width, 100f);
                string explanation = $"{prisoner.LabelShort} may attempt to plead their case to {adjudicator.LabelShort} " +
                                   $"for a reduction in sentence. This is a contest of social skill.\n\n" +
                                   $"The outcome depends on the social skills of both parties, as well as traits " +
                                   $"and current conditions.";
                Widgets.Label(explanationRect, explanation);
                yPos += 110f;

                // Show skill comparison
                int prisonerSkill = HearingUtils.GetEffectiveSocialSkill(prisoner);
                int adjudicatorSkill = HearingUtils.GetEffectiveSocialSkill(adjudicator);
                float chance = HearingUtils.CalculatePleaChance(prisoner, adjudicator);

                Rect skillRect = new Rect(rect.x, rect.y + yPos, rect.width, 80f);
                string skillText = $"{prisoner.LabelShort}'s effective social skill: {prisonerSkill}\n" +
                                  $"{adjudicator.LabelShort}'s effective social skill: {adjudicatorSkill}\n\n" +
                                  $"Success chance: {chance:P0}";
                Widgets.Label(skillRect, skillText);
                yPos += 90f;

                // Warning text
                GUI.color = new Color(1f, 0.8f, 0.3f);
                Rect warningRect = new Rect(rect.x, rect.y + yPos, rect.width, 60f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(warningRect, "Warning: A failed plea bargain may result in no change, or worse, " +
                                          "an increased sentence if the prisoner is deemed to be in contempt of court. " +
                                          "This attempt can only be made once.");
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                yPos += 70f;

                // Attempt plea bargain button
                Rect pleaButtonRect = new Rect(rect.x + 50f, rect.y + yPos, rect.width - 100f, 40f);
                if (Widgets.ButtonText(pleaButtonRect, "Attempt Plea Bargain"))
                {
                    AttemptPleaBargain();
                }
            }
            else
            {
                // Show plea bargain result
                DrawPleaBargainResult(rect);
            }
        }

        private void DrawPleaBargainResult(Rect rect)
        {
            float yPos = 0f;

            // Outcome title
            Text.Font = GameFont.Medium;
            Rect outcomeRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);

            Color outcomeColor = Color.white;
            string outcomeText = "";

            switch (hearing.pleaBargainOutcome)
            {
                case PleaBargainOutcome.Excellent:
                    outcomeColor = new Color(0.2f, 1f, 0.2f);
                    outcomeText = "Excellent Quality!";
                    break;
                case PleaBargainOutcome.Standard:
                    outcomeColor = new Color(0.5f, 1f, 0.5f);
                    outcomeText = "Standard Quality";
                    break;
                case PleaBargainOutcome.Partial:
                    outcomeColor = new Color(1f, 0.7f, 0.3f);
                    outcomeText = "Partial Quality";
                    break;
                case PleaBargainOutcome.Poor:
                    outcomeColor = new Color(1f, 0.2f, 0.2f);
                    outcomeText = "Poor Quality!";
                    break;
            }

            GUI.color = outcomeColor;
            Widgets.Label(outcomeRect, $"Roll: {hearing.pleaBargainRoll} - {outcomeText}");
            GUI.color = Color.white;
            yPos += 40f;

            // Description
            Text.Font = GameFont.Small;
            Rect descRect = new Rect(rect.x + 10f, rect.y + yPos, rect.width - 20f, 120f);
            string description = HearingUtils.GetPleaOutcomeDescription(hearing.pleaBargainOutcome);
            Widgets.Label(descRect, description);
            yPos += 130f;

            // Debt change
            if (hearing.debtBeforePlea > 0 && hearing.debtAfterPlea > 0)
            {
                float debtChange = hearing.debtAfterPlea - hearing.debtBeforePlea;

                Rect debtChangeRect = new Rect(rect.x, rect.y + yPos, rect.width, 60f);
                string debtChangeText = $"Debt before: {hearing.debtBeforePlea:F0} silver\n" +
                                       $"Debt after: {hearing.debtAfterPlea:F0} silver\n";

                if (debtChange < 0)
                {
                    GUI.color = new Color(0.2f, 1f, 0.2f);
                    debtChangeText += $"Reduction: {Mathf.Abs(debtChange):F0} silver";
                }
                else if (debtChange > 0)
                {
                    GUI.color = new Color(1f, 0.2f, 0.2f);
                    debtChangeText += $"Increase: {debtChange:F0} silver";
                }
                else
                {
                    debtChangeText += "No change";
                }

                Widgets.Label(debtChangeRect, debtChangeText);
                GUI.color = Color.white;
            }
        }

        private void DrawSentencing(Rect rect)
        {
            float yPos = 0f;

            // Sentence options
            Text.Font = GameFont.Medium;
            Rect headerRect = new Rect(rect.x, rect.y + yPos, rect.width, 30f);
            Widgets.Label(headerRect, "Sentencing Options");
            yPos += 40f;

            Text.Font = GameFont.Small;
            Rect infoRect = new Rect(rect.x, rect.y + yPos, rect.width, 80f);
            string info = $"{prisoner.LabelShort} has been found guilty and owes {debtRecord.CurrentDebt:F0} silver " +
                         $"(~{debtRecord.EstimatedDaysOfLabor()} days of labor).\n\n" +
                         $"You may now proceed with sentencing as you see fit. The prisoner will remain in custody " +
                         $"until their debt is paid or they are released.";
            Widgets.Label(infoRect, info);
            yPos += 90f;

            // Sentence notes (placeholder for future expansion)
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Rect notesRect = new Rect(rect.x, rect.y + yPos, rect.width, 60f);
            Widgets.Label(notesRect, "Additional sentencing options (forced labor, solitary confinement, execution, etc.) " +
                                    "will be added in future updates. For now, manage the prisoner's imprisonment as normal.");
            GUI.color = Color.white;
        }

        private void AttemptPleaBargain()
        {
            // Record debt before plea
            hearing.debtBeforePlea = debtRecord.CurrentDebt;

            // Attempt the plea bargain
            int roll;
            var outcome = HearingUtils.AttemptPleaBargain(prisoner, adjudicator, out roll);

            // Record the outcome
            hearing.pleaBargainOutcome = outcome;
            hearing.pleaBargainRoll = roll;

            // Apply the outcome
            HearingUtils.ApplyPleaBargainOutcome(prisoner, adjudicator, outcome, hearing.debtBeforePlea);

            // Record debt after plea
            hearing.debtAfterPlea = debtRecord.CurrentDebt;

            // Play sound
            if (outcome == PleaBargainOutcome.Excellent || outcome == PleaBargainOutcome.Standard)
            {
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
            }
            else
            {
                SoundDefOf.ClickReject.PlayOneShotOnCamera();
            }
        }

        private void DrawButtons(Rect rect)
        {
            float buttonWidth = 120f;
            float spacing = 10f;

            // Close button (always available)
            Rect closeRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(closeRect, "Close"))
            {
                Close();
            }

            // Next/Previous phase buttons
            if (currentPhase > HearingPhase.ReadingCharges)
            {
                Rect prevRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
                if (Widgets.ButtonText(prevRect, "Previous"))
                {
                    currentPhase--;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            if (currentPhase < HearingPhase.Sentencing)
            {
                Rect nextRect = new Rect(rect.x + buttonWidth + spacing, rect.y, buttonWidth, rect.height);
                if (Widgets.ButtonText(nextRect, "Next"))
                {
                    if (currentPhase == HearingPhase.ReadingCharges)
                    {
                        hearing.chargesRead = true;
                    }
                    else if (currentPhase == HearingPhase.CalculatingDebt)
                    {
                        hearing.debtCalculated = true;
                    }

                    currentPhase++;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            // Complete hearing button (on sentencing phase)
            if (currentPhase == HearingPhase.Sentencing)
            {
                Rect completeRect = new Rect(rect.x, rect.y, buttonWidth * 1.5f, rect.height);
                if (Widgets.ButtonText(completeRect, "Complete Hearing"))
                {
                    CompleteHearing();
                }
            }
        }

        private void CompleteHearing()
        {
            HearingUtils.ConductHearing(prisoner, adjudicator, hearing.courtroom);

            Messages.Message(
                $"Hearing for {prisoner.LabelShort} has been completed by {adjudicator.LabelShort}.",
                prisoner,
                MessageTypeDefOf.PositiveEvent
            );

            Close();
        }
    }
}
