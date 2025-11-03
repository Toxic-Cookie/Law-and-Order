using RimWorld;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Maps ideology precepts to item categories for contraband alignment.
    /// Determines if enforcing contraband rules aligns with colony ideology.
    /// </summary>
    public static class IdeologyContrabandMapper
    {
        /// <summary>
        /// Check if a contraband item aligns with the colony's ideology.
        /// Returns true if banning this item is consistent with ideology beliefs.
        /// </summary>
        public static bool ItemAlignsWithIdeology(ThingDef item, Ideo ideology)
        {
            if (ideology == null || item == null)
                return false;

            // Check various ideology precepts and see if they would support banning this item

            // 1. Animal Personhood / Ranching precepts
            if (HasPrecept(ideology, "AnimalPersonhood") || HasPrecept(ideology, "Ranching"))
            {
                if (IsAnimalProduct(item))
                    return true; // Ideology would support banning animal products
            }

            // 2. Cannibalism precepts (if they FORBID cannibalism, they'd ban human meat)
            if (HasPrecept(ideology, "Cannibalism"))
            {
                if (item.IsIngestible && item.ingestible?.sourceDef?.race?.Humanlike == true)
                {
                    // Check if precept forbids or allows cannibalism
                    if (!IdeoAllowsCannibalism(ideology))
                        return true; // Banning human meat aligns with anti-cannibalism ideology
                }
            }

            // 3. Tree Connection / Nature precepts
            if (HasPrecept(ideology, "TreeConnection") || HasPrecept(ideology, "TreesDesired"))
            {
                // Check if item is wood or wooden products
                if (item.defName.ToLower().Contains("wood") ||
                    item.label.ToLower().Contains("wood") ||
                    (item.IsStuff && item.stuffProps?.categories?.Contains(StuffCategoryDefOf.Woody) == true))
                    return true; // Tree-loving ideology would support banning wood products
            }

            // 4. Drugs - check for drug precepts
            if (HasPrecept(ideology, "DrugUse"))
            {
                if (item.IsDrug && !IdeoAllowsDrugs(ideology))
                    return true; // Anti-drug ideology supports banning drugs
            }

            // 5. Blindness / Darkness precepts
            if (HasPrecept(ideology, "Blindness") || HasPrecept(ideology, "Darkness"))
            {
                // Blindness ideology might ban vision-related items
                if (item.defName.ToLower().Contains("goggles") ||
                    item.defName.ToLower().Contains("eyes") ||
                    item.defName.ToLower().Contains("sight"))
                    return true;
            }

            // 6. Violence-related precepts
            if (HasPrecept(ideology, "Violence") && !IdeoAllowsViolence(ideology))
            {
                if (item.IsWeapon)
                    return true; // Pacifist ideology supports banning weapons
            }

            // 7. Nudity precepts
            if (HasPrecept(ideology, "Nudity"))
            {
                if (item.IsApparel && IdeoRequiresClothing(ideology))
                    return false; // Pro-clothing ideology would NOT support banning clothes
            }

            return false;
        }

        /// <summary>
        /// Check if item is an animal product (meat, leather, wool, etc.)
        /// </summary>
        private static bool IsAnimalProduct(ThingDef item)
        {
            // Leather
            if (item.IsLeather)
                return true;

            // Wool and other fabrics from animals
            if (item.IsStuff && item.stuffProps?.categories?.Contains(StuffCategoryDefOf.Fabric) == true)
            {
                if (item.defName.ToLower().Contains("wool") ||
                    item.defName.ToLower().Contains("silk") ||
                    item.label.ToLower().Contains("wool") ||
                    item.label.ToLower().Contains("silk"))
                    return true;
            }

            // Meat (except human meat, handled separately)
            if (item.IsMeat && item.ingestible?.sourceDef?.race?.Animal == true)
                return true;

            // Eggs, milk, etc
            if (item.IsIngestible)
            {
                if (item.defName.ToLower().Contains("milk") ||
                    item.defName.ToLower().Contains("egg") ||
                    item.label.ToLower().Contains("milk") ||
                    item.label.ToLower().Contains("egg"))
                    return true;
            }

            // Chemfuel from animals
            if (item.defName == "Chemfuel")
            {
                // Chemfuel can be made from corpses
                return false; // Too ambiguous
            }

            return false;
        }

        /// <summary>
        /// Check if ideology has a specific precept (by partial name match)
        /// </summary>
        private static bool HasPrecept(Ideo ideo, string preceptNamePart)
        {
            if (ideo?.PreceptsListForReading == null)
                return false;

            return ideo.PreceptsListForReading.Any(p =>
                p?.def?.defName?.Contains(preceptNamePart) == true);
        }

        /// <summary>
        /// Check if ideology allows cannibalism
        /// </summary>
        private static bool IdeoAllowsCannibalism(Ideo ideo)
        {
            if (ideo?.PreceptsListForReading == null)
                return false;

            // Check for cannibalism precepts
            var cannibalismPrecept = ideo.PreceptsListForReading.FirstOrDefault(p =>
                p?.def?.defName?.Contains("Cannibalism") == true);

            if (cannibalismPrecept == null)
                return false; // No precept = default (forbidden)

            // Check the impact (some precepts have "impact" field)
            // For simplicity, check if the precept name contains positive indicators
            string defName = cannibalismPrecept.def.defName;
            return defName.Contains("Acceptable") ||
                   defName.Contains("Preferred") ||
                   defName.Contains("Required") ||
                   defName.Contains("Horrible"); // "Horrible" means it's forbidden
        }

        /// <summary>
        /// Check if ideology allows drug use
        /// </summary>
        private static bool IdeoAllowsDrugs(Ideo ideo)
        {
            if (ideo?.PreceptsListForReading == null)
                return true; // Default: drugs allowed

            var drugPrecept = ideo.PreceptsListForReading.FirstOrDefault(p =>
                p?.def?.defName?.Contains("DrugUse") == true);

            if (drugPrecept == null)
                return true; // No precept = allowed

            string defName = drugPrecept.def.defName;
            return !defName.Contains("Prohibited") && !defName.Contains("Horrible");
        }

        /// <summary>
        /// Check if ideology allows violence
        /// </summary>
        private static bool IdeoAllowsViolence(Ideo ideo)
        {
            if (ideo?.PreceptsListForReading == null)
                return true; // Default: violence allowed

            var violencePrecept = ideo.PreceptsListForReading.FirstOrDefault(p =>
                p?.def?.defName?.Contains("Violence") == true);

            if (violencePrecept == null)
                return true; // No precept = allowed

            string defName = violencePrecept.def.defName;
            return !defName.Contains("Abhorrent") && !defName.Contains("Horrible");
        }

        /// <summary>
        /// Check if ideology requires clothing (nudity is bad)
        /// </summary>
        private static bool IdeoRequiresClothing(Ideo ideo)
        {
            if (ideo?.PreceptsListForReading == null)
                return true; // Default: clothing required

            var nudityPrecept = ideo.PreceptsListForReading.FirstOrDefault(p =>
                p?.def?.defName?.Contains("Nudity") == true);

            if (nudityPrecept == null)
                return true; // No precept = clothing preferred

            string defName = nudityPrecept.def.defName;
            return defName.Contains("Horrible") || defName.Contains("Disapproved");
        }
    }
}
