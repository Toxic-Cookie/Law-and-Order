using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Helper class for rendering collapsible crime categories in UI
    /// </summary>
    public class CrimeCategoryUIHelper
    {
        private Dictionary<CrimeType, bool> expandedCategories = new Dictionary<CrimeType, bool>();
        private const float CategoryHeaderHeight = 30f;
        private const float CategoryIndent = 10f;

        /// <summary>
        /// Group crimes by their category type
        /// </summary>
        public Dictionary<CrimeType, List<Crime>> GroupCrimesByCategory(IEnumerable<Crime> crimes)
        {
            var grouped = new Dictionary<CrimeType, List<Crime>>();

            foreach (var crime in crimes)
            {
                if (!grouped.ContainsKey(crime.crimeType))
                {
                    grouped[crime.crimeType] = new List<Crime>();
                }
                grouped[crime.crimeType].Add(crime);
            }

            // Sort groups by severity (most severe first)
            return grouped
                .OrderBy(kvp => GetCrimeSeverityOrder(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Check if a category is currently expanded
        /// </summary>
        public bool IsCategoryExpanded(CrimeType crimeType)
        {
            // Default to collapsed
            if (!expandedCategories.ContainsKey(crimeType))
            {
                expandedCategories[crimeType] = false;
            }
            return expandedCategories[crimeType];
        }

        /// <summary>
        /// Toggle a category's expanded state
        /// </summary>
        public void ToggleCategory(CrimeType crimeType)
        {
            if (!expandedCategories.ContainsKey(crimeType))
            {
                expandedCategories[crimeType] = false;
            }
            expandedCategories[crimeType] = !expandedCategories[crimeType];
        }

        /// <summary>
        /// Draw a collapsible category header and return true if the category is expanded
        /// </summary>
        /// <param name="rect">The rectangle to draw in</param>
        /// <param name="crimeType">The crime category type</param>
        /// <param name="crimeCount">Number of crimes in this category</param>
        /// <returns>True if the category is expanded and crimes should be shown</returns>
        public bool DrawCategoryHeader(Rect rect, CrimeType crimeType, int crimeCount)
        {
            bool isExpanded = IsCategoryExpanded(crimeType);

            // Draw background
            Color bgColor = GetCategoryColor(crimeType);
            bgColor.a = 0.15f;
            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect);

            // Check if clicked
            if (Widgets.ButtonInvisible(rect))
            {
                ToggleCategory(crimeType);
                isExpanded = !isExpanded;
            }

            // Highlight on hover
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect innerRect = rect.ContractedBy(5f);

            // Draw expand/collapse arrow
            Rect arrowRect = new Rect(innerRect.x, innerRect.y, 20f, innerRect.height);
            string arrow = isExpanded ? "▼" : "►";
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(arrowRect, arrow);

            // Draw category label
            Rect labelRect = new Rect(arrowRect.xMax + 5f, innerRect.y, innerRect.width - 80f, innerRect.height);
            Text.Font = GameFont.Small;

            Color textColor = GetCategoryColor(crimeType);
            GUI.color = textColor;
            string categoryLabel = GetCategoryLabel(crimeType);
            Widgets.Label(labelRect, categoryLabel);
            GUI.color = Color.white;

            // Draw crime count
            Rect countRect = new Rect(innerRect.xMax - 70f, innerRect.y, 70f, innerRect.height);
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(countRect, $"({crimeCount})");
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            return isExpanded;
        }

        /// <summary>
        /// Calculate the total height needed to render crimes with categories
        /// </summary>
        public float CalculateTotalHeight(Dictionary<CrimeType, List<Crime>> groupedCrimes, float crimeEntryHeight)
        {
            float totalHeight = 0f;

            foreach (var kvp in groupedCrimes)
            {
                // Category header
                totalHeight += CategoryHeaderHeight + 5f;

                // Crimes if expanded
                if (IsCategoryExpanded(kvp.Key))
                {
                    totalHeight += kvp.Value.Count * (crimeEntryHeight + 5f);
                }
            }

            return totalHeight;
        }

        /// <summary>
        /// Get a color associated with a crime category
        /// </summary>
        private Color GetCategoryColor(CrimeType crimeType)
        {
            switch (crimeType)
            {
                case CrimeType.Murder:
                    return new Color(1f, 0.2f, 0.2f);
                case CrimeType.Assault:
                    return new Color(1f, 0.5f, 0.2f);
                case CrimeType.Kidnapping:
                    return new Color(1f, 0.4f, 0.4f);
                case CrimeType.Arson:
                    return new Color(1f, 0.6f, 0.1f);
                case CrimeType.PropertyDestruction:
                case CrimeType.Vandalism:
                    return new Color(0.8f, 0.6f, 0.3f);
                case CrimeType.Theft:
                    return new Color(0.7f, 0.5f, 0.8f);
                case CrimeType.AnimalAbuse:
                    return new Color(0.9f, 0.5f, 0.5f);
                case CrimeType.Trespassing:
                    return new Color(0.6f, 0.6f, 0.6f);
                case CrimeType.ContrabandPossession:
                    return new Color(0.8f, 0.7f, 0.5f);
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Get the translated label for a crime category
        /// </summary>
        private string GetCategoryLabel(CrimeType crimeType)
        {
            string translationKey = $"LawAndOrder_CrimeCategory_{crimeType}";
            if (translationKey.CanTranslate())
            {
                return translationKey.Translate();
            }
            // Fallback to enum name
            return crimeType.ToString();
        }

        /// <summary>
        /// Get the severity order for sorting (lower = more severe)
        /// </summary>
        private int GetCrimeSeverityOrder(CrimeType crimeType)
        {
            switch (crimeType)
            {
                case CrimeType.Murder:
                    return 0;
                case CrimeType.Kidnapping:
                    return 1;
                case CrimeType.Assault:
                    return 2;
                case CrimeType.Arson:
                    return 3;
                case CrimeType.PropertyDestruction:
                    return 4;
                case CrimeType.AnimalAbuse:
                    return 5;
                case CrimeType.Theft:
                    return 6;
                case CrimeType.Vandalism:
                    return 7;
                case CrimeType.ContrabandPossession:
                    return 8;
                case CrimeType.Trespassing:
                    return 9;
                default:
                    return 100;
            }
        }

        /// <summary>
        /// Get the height of a category header
        /// </summary>
        public float GetCategoryHeaderHeight()
        {
            return CategoryHeaderHeight;
        }
    }
}
