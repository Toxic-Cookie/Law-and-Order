using System;
using System.Collections.Generic;
using System.Linq;

namespace LawAndOrder
{
    /// <summary>
    /// Builds a hierarchical category tree for crimes.
    /// </summary>
    public static class CrimeCategoryTreeBuilder
    {
        /// <summary>
        /// Builds the root category nodes for the crime tree.
        /// </summary>
        public static List<CrimeCategoryNode> BuildCategoryTree(List<CrimeDefinition> crimeDefinitions)
        {
            var rootNodes = new List<CrimeCategoryNode>();

            // Get all unique categories from crime definitions
            var categories = Enum.GetValues(typeof(CrimeCategory)).Cast<CrimeCategory>().ToList();

            foreach (var category in categories)
            {
                var crimesInCategory = crimeDefinitions.Where(cd => cd.category == category).OrderBy(cd => cd.label).ToList();

                if (crimesInCategory.Count > 0)
                {
                    var categoryNode = new CrimeCategoryNode(category, 0);

                    foreach (var crime in crimesInCategory)
                    {
                        categoryNode.children.Add(new CrimeCategoryNode(crime, 1));
                    }

                    rootNodes.Add(categoryNode);
                }
            }

            return rootNodes;
        }

        /// <summary>
        /// Flattens the tree into a renderable list based on expansion states.
        /// </summary>
        public static List<CrimeCategoryNode> FlattenTree(List<CrimeCategoryNode> rootNodes)
        {
            var flatList = new List<CrimeCategoryNode>();

            foreach (var node in rootNodes)
            {
                FlattenNode(node, flatList);
            }

            return flatList;
        }

        private static void FlattenNode(CrimeCategoryNode node, List<CrimeCategoryNode> flatList)
        {
            flatList.Add(node);

            if (node.IsCategory && node.isExpanded)
            {
                foreach (var child in node.children)
                {
                    FlattenNode(child, flatList);
                }
            }
        }

        /// <summary>
        /// Searches the tree for nodes matching a query.
        /// </summary>
        public static List<CrimeCategoryNode> SearchTree(List<CrimeCategoryNode> rootNodes, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return FlattenTree(rootNodes);

            var results = new List<CrimeCategoryNode>();
            var lowerQuery = query.ToLower();

            foreach (var node in rootNodes)
            {
                SearchNode(node, lowerQuery, results);
            }

            return results;
        }

        private static bool SearchNode(CrimeCategoryNode node, string query, List<CrimeCategoryNode> results)
        {
            bool hasMatch = false;

            if (node.IsCrime)
            {
                // Check if crime matches
                if (node.crimeDefinition.label.ToLower().Contains(query) ||
                    node.crimeDefinition.description.ToLower().Contains(query))
                {
                    results.Add(node);
                    return true;
                }
            }
            else if (node.IsCategory)
            {
                // Check children first
                foreach (var child in node.children)
                {
                    if (SearchNode(child, query, results))
                    {
                        hasMatch = true;
                    }
                }

                // If any child matched, add this category
                if (hasMatch)
                {
                    // Add category before its children in results
                    int insertIndex = results.Count;
                    foreach (var child in node.children)
                    {
                        int childIndex = results.IndexOf(child);
                        if (childIndex >= 0 && childIndex < insertIndex)
                        {
                            insertIndex = childIndex;
                        }
                    }
                    results.Insert(insertIndex, node);
                }
            }

            return hasMatch;
        }
    }
}
