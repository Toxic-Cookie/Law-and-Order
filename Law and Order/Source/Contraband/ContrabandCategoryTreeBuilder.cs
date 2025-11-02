using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Builds a hierarchical category tree for contraband items.
    /// </summary>
    public static class ContrabandCategoryTreeBuilder
    {
        /// <summary>
        /// Builds the root category nodes for the contraband tree.
        /// </summary>
        public static List<ContrabandCategoryNode> BuildCategoryTree()
        {
            var rootNodes = new List<ContrabandCategoryNode>();

            // Get all items that can be contraband (physical items, not corpses/buildings)
            var allItems = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.category == ThingCategory.Item && !def.IsCorpse)
                .ToList();

            // Get root categories (categories with no parent)
            var rootCategories = DefDatabase<ThingCategoryDef>.AllDefsListForReading
                .Where(cat => cat.parent == null)
                .OrderBy(cat => cat.label)
                .ToList();

            foreach (var category in rootCategories)
            {
                var node = BuildCategoryNode(category, allItems, 0);
                if (node != null && (node.children.Count > 0 || node.GetTotalItemCount() > 0))
                {
                    rootNodes.Add(node);
                }
            }

            // Add uncategorized items
            var categorizedItems = new HashSet<ThingDef>();
            foreach (var node in rootNodes)
            {
                foreach (var item in node.GetAllItems())
                {
                    categorizedItems.Add(item);
                }
            }

            var uncategorizedItems = allItems.Where(item => !categorizedItems.Contains(item)).ToList();
            if (uncategorizedItems.Count > 0)
            {
                var uncategorizedNode = new ContrabandCategoryNode((ThingCategoryDef)null, 0);
                uncategorizedNode.isExpanded = true; // Start expanded

                foreach (var item in uncategorizedItems.OrderBy(i => i.label))
                {
                    uncategorizedNode.children.Add(new ContrabandCategoryNode(item, 1));
                }

                rootNodes.Add(uncategorizedNode);
            }

            return rootNodes;
        }

        /// <summary>
        /// Recursively builds a category node with all its children.
        /// </summary>
        private static ContrabandCategoryNode BuildCategoryNode(ThingCategoryDef category, List<ThingDef> availableItems, int depth)
        {
            var node = new ContrabandCategoryNode(category, depth);

            // Add child categories
            if (category.childCategories != null)
            {
                foreach (var childCategory in category.childCategories.OrderBy(c => c.label))
                {
                    var childNode = BuildCategoryNode(childCategory, availableItems, depth + 1);
                    if (childNode != null && (childNode.children.Count > 0 || childNode.GetTotalItemCount() > 0))
                    {
                        node.children.Add(childNode);
                    }
                }
            }

            // Add items directly in this category
            if (category.childThingDefs != null)
            {
                foreach (var thing in category.childThingDefs.Where(t => availableItems.Contains(t)).OrderBy(t => t.label))
                {
                    node.children.Add(new ContrabandCategoryNode(thing, depth + 1));
                }
            }

            return node;
        }

        /// <summary>
        /// Flattens the tree into a renderable list based on expansion states.
        /// </summary>
        public static List<ContrabandCategoryNode> FlattenTree(List<ContrabandCategoryNode> rootNodes)
        {
            var flatList = new List<ContrabandCategoryNode>();

            foreach (var node in rootNodes)
            {
                FlattenNode(node, flatList);
            }

            return flatList;
        }

        private static void FlattenNode(ContrabandCategoryNode node, List<ContrabandCategoryNode> flatList)
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
        public static List<ContrabandCategoryNode> SearchTree(List<ContrabandCategoryNode> rootNodes, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return FlattenTree(rootNodes);

            var results = new List<ContrabandCategoryNode>();
            var lowerQuery = query.ToLower();

            foreach (var node in rootNodes)
            {
                SearchNode(node, lowerQuery, results);
            }

            return results;
        }

        private static bool SearchNode(ContrabandCategoryNode node, string query, List<ContrabandCategoryNode> results)
        {
            bool hasMatch = false;

            if (node.IsItem)
            {
                // Check if item matches
                if (node.thingDef.label.ToLower().Contains(query))
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
