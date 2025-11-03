using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using LawAndOrder;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Detect when colonists produce contraband items to apply mood debuff.
    /// Similar to butchering humanlike - creates guilt for crafting illegal items.
    /// </summary>
    [HarmonyPatch(typeof(Toils_Recipe), "FinishRecipeAndStartStoringProduct")]
    public static class ContrabandProduction_Patch
    {
        static void Postfix()
        {
            // Note: This patch fires when a recipe completes
            // We'll use a different approach - patch the UnfinishedThing completion
        }
    }

    /// <summary>
    /// Better patch point - when an item is made by a pawn
    /// </summary>
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class ContrabandCrafting_Patch
    {
        static void Postfix(RecipeDef recipeDef, Pawn worker, System.Collections.Generic.IEnumerable<Thing> __result)
        {
            if (worker == null || !worker.IsColonist)
                return;

            if (__result == null)
                return;

            try
            {
                // Get contraband manager
                var contrabandManager = WorldComponent_ContrabandManager.Instance;
                if (contrabandManager == null)
                    return;

                // Check if any of the produced items are contraband
                foreach (var product in __result)
                {
                    if (product != null && contrabandManager.IsContraband(product.def))
                    {
                        // Give the crafter a mood debuff for producing contraband
                        ThoughtDef producedThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("LawAndOrder_ProducedContraband");
                        if (producedThought != null && worker.needs?.mood?.thoughts?.memories != null)
                        {
                            worker.needs.mood.thoughts.memories.TryGainMemory(producedThought);

                            #if DEBUG
                            Mod.Log?.Message($"{worker.LabelShort} produced contraband: {product.LabelCap} - mood debuff applied");
                            #endif
                        }

                        break; // Only apply once per recipe completion
                    }
                }
            }
            catch (System.Exception e)
            {
                Mod.Log?.Error($"Error in ContrabandCrafting_Patch: {e}");
            }
        }
    }
}
