using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Represents a node in the contraband category tree (either a category or an item).
    /// </summary>
    public class ContrabandCategoryNode
    {
        public ThingCategoryDef categoryDef;
        public ThingDef thingDef;
        public List<ContrabandCategoryNode> children = new List<ContrabandCategoryNode>();
        public bool isExpanded = false;
        public int depth = 0;

        // Category node constructor
        public ContrabandCategoryNode(ThingCategoryDef category, int depth)
        {
            this.categoryDef = category;
            this.depth = depth;
            this.isExpanded = depth == 0; // Root categories start expanded
        }

        // Item node constructor
        public ContrabandCategoryNode(ThingDef thing, int depth)
        {
            this.thingDef = thing;
            this.depth = depth;
        }

        public bool IsCategory => categoryDef != null;
        public bool IsItem => thingDef != null;

        /// <summary>
        /// Gets all items under this node (recursively for categories).
        /// </summary>
        public List<ThingDef> GetAllItems()
        {
            var items = new List<ThingDef>();

            if (IsItem)
            {
                items.Add(thingDef);
            }
            else if (IsCategory)
            {
                foreach (var child in children)
                {
                    items.AddRange(child.GetAllItems());
                }
            }

            return items;
        }

        /// <summary>
        /// Gets count of contraband items in this node.
        /// </summary>
        public int GetContrabandCount()
        {
            var manager = WorldComponent_ContrabandManager.Instance;
            if (manager == null)
                return 0;

            return GetAllItems().Count(item => manager.IsContraband(item));
        }

        /// <summary>
        /// Gets total count of items in this node.
        /// </summary>
        public int GetTotalItemCount()
        {
            return GetAllItems().Count;
        }
    }
}
