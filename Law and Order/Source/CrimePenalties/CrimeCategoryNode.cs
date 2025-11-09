using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Represents a node in the crime category tree (either a category or a crime).
    /// </summary>
    public class CrimeCategoryNode
    {
        public CrimeCategory? category;
        public CrimeDefinition crimeDefinition;
        public List<CrimeCategoryNode> children = new List<CrimeCategoryNode>();
        public bool isExpanded = false;
        public int depth = 0;

        // Category node constructor
        public CrimeCategoryNode(CrimeCategory category, int depth)
        {
            this.category = category;
            this.depth = depth;
            this.isExpanded = depth == 0; // Root categories start expanded
        }

        // Crime node constructor
        public CrimeCategoryNode(CrimeDefinition crimeDefinition, int depth)
        {
            this.crimeDefinition = crimeDefinition;
            this.depth = depth;
        }

        public bool IsCategory => category.HasValue;
        public bool IsCrime => crimeDefinition != null;

        /// <summary>
        /// Gets the label for this node.
        /// </summary>
        public string GetLabel()
        {
            if (IsCategory)
            {
                return GetCategoryLabel(category.Value);
            }
            else if (IsCrime)
            {
                return crimeDefinition.label;
            }
            return "Unknown";
        }

        /// <summary>
        /// Gets all crimes under this node (recursively for categories).
        /// </summary>
        public List<CrimeDefinition> GetAllCrimes()
        {
            var crimes = new List<CrimeDefinition>();

            if (IsCrime)
            {
                crimes.Add(crimeDefinition);
            }
            else if (IsCategory)
            {
                foreach (var child in children)
                {
                    crimes.AddRange(child.GetAllCrimes());
                }
            }

            return crimes;
        }

        /// <summary>
        /// Gets count of crimes with pending changes in this node.
        /// </summary>
        public int GetPendingChangesCount()
        {
            return GetAllCrimes().Count(crime => crime.hasPendingChanges);
        }

        /// <summary>
        /// Gets total count of crimes in this node.
        /// </summary>
        public int GetTotalCrimeCount()
        {
            return GetAllCrimes().Count;
        }

        /// <summary>
        /// Gets a human-readable label for a crime category.
        /// </summary>
        public static string GetCategoryLabel(CrimeCategory category)
        {
            switch (category)
            {
                case CrimeCategory.Lethal:
                    return "LawAndOrder_CrimeCategory_Lethal".Translate();
                case CrimeCategory.SevereInjury:
                    return "LawAndOrder_CrimeCategory_SevereInjury".Translate();
                case CrimeCategory.ModerateInjury:
                    return "LawAndOrder_CrimeCategory_ModerateInjury".Translate();
                case CrimeCategory.MinorInjury:
                    return "LawAndOrder_CrimeCategory_MinorInjury".Translate();
                case CrimeCategory.Environmental:
                    return "LawAndOrder_CrimeCategory_Environmental".Translate();
                case CrimeCategory.Disease:
                    return "LawAndOrder_CrimeCategory_Disease".Translate();
                case CrimeCategory.PropertyDestruction:
                    return "LawAndOrder_CrimeCategory_PropertyDestruction".Translate();
                case CrimeCategory.Theft:
                    return "LawAndOrder_CrimeCategory_Theft".Translate();
                case CrimeCategory.Social:
                    return "LawAndOrder_CrimeCategory_Social".Translate();
                case CrimeCategory.Anomaly:
                    return "LawAndOrder_CrimeCategory_Anomaly".Translate();
                case CrimeCategory.Biotech:
                    return "LawAndOrder_CrimeCategory_Biotech".Translate();
                default:
                    return "Unknown";
            }
        }
    }
}
