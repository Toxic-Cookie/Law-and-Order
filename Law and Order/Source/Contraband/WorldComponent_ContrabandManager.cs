using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Manages contraband definitions globally across the game world.
    /// </summary>
    public class WorldComponent_ContrabandManager : WorldComponent
    {
        // Maximum penalty is 3x the item's market value
        private const float MAX_PENALTY_MULTIPLIER = 3.0f;

        // Minimum penalty cap for worthless items (market value = 0)
        private const int MIN_PENALTY_CAP = 10;

        // Lockout duration: 15 days (one quadrum)
        // RimWorld: 1 day = 60,000 ticks
        private const int LOCKOUT_DURATION_TICKS = 15 * 60000;

        private List<ContrabandDefinition> contrabandDefinitions = new List<ContrabandDefinition>();
        internal int lastCommitTick = -1; // -1 means never committed (internal for UI access)

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
            // Cap penalty at 3x market value, with minimum cap for worthless items
            float marketValue = thingDef.BaseMarketValue;
            int maxPenalty = Mathf.RoundToInt(marketValue * MAX_PENALTY_MULTIPLIER);

            // Ensure minimum cap for worthless items (e.g., stone chunks)
            if (maxPenalty < MIN_PENALTY_CAP)
            {
                maxPenalty = MIN_PENALTY_CAP;
            }

            if (silverPenalty > maxPenalty)
            {
                silverPenalty = maxPenalty;

                // Notify player when cap is applied
                string capReason = marketValue <= 0
                    ? $"{thingDef.LabelCap} penalty capped at {maxPenalty} silver (minimum cap for worthless items)"
                    : $"{thingDef.LabelCap} penalty capped at {maxPenalty} silver (3x market value of {marketValue:F1})";

                Messages.Message(capReason, MessageTypeDefOf.RejectInput);

                #if DEBUG
                Law_and_Order.Source.Mod.Log?.Warning($"Contraband penalty for {thingDef.label} capped at {maxPenalty} (market value: {marketValue})");
                #endif
            }

            var existing = GetContrabandDefinition(thingDef);
            if (existing != null)
            {
                existing.silverPenaltyPerItem = silverPenalty;
            }
            else
            {
                contrabandDefinitions.Add(new ContrabandDefinition(thingDef, silverPenalty));
            }

            // Update burn contraband bill filters
            Law_and_Order.Source.Patches.BurnContrabandFilterUpdater.UpdateAllBurnContrabandBills();
        }

        /// <summary>
        /// Removes a ThingDef from the contraband list.
        /// </summary>
        public void RemoveContraband(ThingDef thingDef)
        {
            contrabandDefinitions.RemoveAll(cd => cd.thingDef == thingDef);

            // Update burn contraband bill filters
            Law_and_Order.Source.Patches.BurnContrabandFilterUpdater.UpdateAllBurnContrabandBills();
        }

        /// <summary>
        /// Marks all items in a list as contraband with the specified penalty.
        /// </summary>
        public int SetContrabandBulk(List<ThingDef> items, int silverPenalty)
        {
            int count = 0;
            foreach (var item in items)
            {
                SetContraband(item, silverPenalty);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Updates all contraband items in a list with a new penalty.
        /// </summary>
        public int UpdateContrabandBulk(List<ThingDef> items, int silverPenalty)
        {
            int count = 0;
            foreach (var item in items)
            {
                if (IsContraband(item))
                {
                    SetContraband(item, silverPenalty);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Removes all contraband items in a list from contraband.
        /// </summary>
        public int RemoveContrabandBulk(List<ThingDef> items)
        {
            int count = 0;
            foreach (var item in items)
            {
                if (IsContraband(item))
                {
                    RemoveContraband(item);
                    count++;
                }
            }
            return count;
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
        /// Records that contraband changes have been committed.
        /// </summary>
        public void RecordCommit()
        {
            lastCommitTick = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Checks if contraband changes are currently locked out.
        /// God mode bypasses the lockout.
        /// </summary>
        public bool IsInLockout()
        {
            // God mode bypasses lockout
            if (Verse.DebugSettings.godMode)
            {
                return false;
            }

            if (lastCommitTick < 0)
            {
                return false; // Never committed before
            }

            int ticksSinceCommit = Find.TickManager.TicksGame - lastCommitTick;
            return ticksSinceCommit < LOCKOUT_DURATION_TICKS;
        }

        /// <summary>
        /// Gets the number of days remaining in the lockout period.
        /// </summary>
        public float GetLockoutDaysRemaining()
        {
            if (!IsInLockout())
            {
                return 0f;
            }

            int ticksSinceCommit = Find.TickManager.TicksGame - lastCommitTick;
            int ticksRemaining = LOCKOUT_DURATION_TICKS - ticksSinceCommit;
            return ticksRemaining / 60000f; // Convert ticks to days
        }

        /// <summary>
        /// Gets the tick when the lockout will expire.
        /// </summary>
        public int GetLockoutExpiryTick()
        {
            if (lastCommitTick < 0)
            {
                return -1;
            }
            return lastCommitTick + LOCKOUT_DURATION_TICKS;
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
            Scribe_Values.Look(ref lastCommitTick, "lastCommitTick", -1);

            if (Scribe.mode == LoadSaveMode.LoadingVars && contrabandDefinitions == null)
            {
                contrabandDefinitions = new List<ContrabandDefinition>();
            }
        }
    }
}
