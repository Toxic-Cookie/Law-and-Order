using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Manages contraband definitions globally across the game world.
    /// </summary>
    public class WorldComponent_ContrabandManager : WorldComponent
    {
        private List<ContrabandDefinition> contrabandDefinitions = new List<ContrabandDefinition>();

        public WorldComponent_ContrabandManager(World world) : base(world)
        {
        }

        /// <summary>
        /// Gets all contraband definitions.
        /// </summary>
        public List<ContrabandDefinition> ContrabandDefinitions => contrabandDefinitions;

        /// <summary>
        /// Checks if a ThingDef is marked as contraband.
        /// </summary>
        public bool IsContraband(ThingDef thingDef)
        {
            return contrabandDefinitions.Any(cd => cd.thingDef == thingDef);
        }

        /// <summary>
        /// Gets the contraband definition for a specific ThingDef.
        /// </summary>
        public ContrabandDefinition GetContrabandDefinition(ThingDef thingDef)
        {
            return contrabandDefinitions.FirstOrDefault(cd => cd.thingDef == thingDef);
        }

        /// <summary>
        /// Adds or updates a contraband definition.
        /// </summary>
        public void SetContraband(ThingDef thingDef, int silverPenalty)
        {
            var existing = GetContrabandDefinition(thingDef);
            if (existing != null)
            {
                existing.silverPenaltyPerItem = silverPenalty;
            }
            else
            {
                contrabandDefinitions.Add(new ContrabandDefinition(thingDef, silverPenalty));
            }
        }

        /// <summary>
        /// Removes a ThingDef from the contraband list.
        /// </summary>
        public void RemoveContraband(ThingDef thingDef)
        {
            contrabandDefinitions.RemoveAll(cd => cd.thingDef == thingDef);
        }

        /// <summary>
        /// Calculates the total contraband penalty for items in a pawn's inventory.
        /// </summary>
        public int CalculateContrabandPenalty(Pawn pawn, out Dictionary<ThingDef, int> contrabandItems)
        {
            contrabandItems = new Dictionary<ThingDef, int>();
            int totalPenalty = 0;

            if (pawn?.inventory?.innerContainer == null)
                return 0;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                var contrabandDef = GetContrabandDefinition(thing.def);
                if (contrabandDef != null)
                {
                    int count = thing.stackCount;
                    int penalty = contrabandDef.silverPenaltyPerItem * count;
                    totalPenalty += penalty;

                    if (contrabandItems.ContainsKey(thing.def))
                        contrabandItems[thing.def] += count;
                    else
                        contrabandItems[thing.def] = count;
                }
            }

            return totalPenalty;
        }

        /// <summary>
        /// Gets a static reference to the contraband manager.
        /// </summary>
        public static WorldComponent_ContrabandManager Instance
        {
            get
            {
                return Find.World.GetComponent<WorldComponent_ContrabandManager>();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref contrabandDefinitions, "contrabandDefinitions", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && contrabandDefinitions == null)
            {
                contrabandDefinitions = new List<ContrabandDefinition>();
            }
        }
    }
}
