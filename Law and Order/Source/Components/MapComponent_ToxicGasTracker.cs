using System.Collections.Generic;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// Tracks toxic gas clouds and their instigators (the pawn who threw the grenade/created the gas)
    /// This allows us to attribute toxic damage back to the raider who caused it
    /// </summary>
    public class MapComponent_ToxicGasTracker : MapComponent
    {
        // Track gas cells and who created them
        // Key: Cell index, Value: (Instigator pawn, Creation tick)
        private Dictionary<int, (Pawn instigator, int creationTick)> gasInstigators = new Dictionary<int, (Pawn, int)>();

        // How long to remember gas instigators (in ticks)
        private const int INSTIGATOR_MEMORY_DURATION = GenDate.TicksPerDay; // 1 day

        public MapComponent_ToxicGasTracker(Map map) : base(map)
        {
        }

        /// <summary>
        /// Register a gas cloud with its instigator
        /// </summary>
        public void RegisterGasInstigator(IntVec3 center, float radius, Pawn instigator)
        {
            if (instigator == null || map == null)
                return;

            int currentTick = Find.TickManager.TicksGame;

            // Register all cells in the gas radius
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                int cellIndex = map.cellIndices.CellToIndex(cell);
                gasInstigators[cellIndex] = (instigator, currentTick);
            }
        }

        /// <summary>
        /// Get the instigator for a gas cell
        /// Returns null if no instigator is tracked or it's too old
        /// </summary>
        public Pawn GetGasInstigator(IntVec3 cell)
        {
            if (map == null)
                return null;

            int cellIndex = map.cellIndices.CellToIndex(cell);

            if (gasInstigators.TryGetValue(cellIndex, out var data))
            {
                int currentTick = Find.TickManager.TicksGame;

                // Check if the tracking is still valid
                if (currentTick - data.creationTick < INSTIGATOR_MEMORY_DURATION)
                {
                    return data.instigator;
                }
                else
                {
                    // Remove old tracking
                    gasInstigators.Remove(cellIndex);
                }
            }

            return null;
        }

        /// <summary>
        /// Clean up old instigator tracking periodically
        /// </summary>
        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Clean up every hour (2500 ticks)
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                int currentTick = Find.TickManager.TicksGame;
                List<int> toRemove = new List<int>();

                foreach (var kvp in gasInstigators)
                {
                    if (currentTick - kvp.Value.creationTick >= INSTIGATOR_MEMORY_DURATION)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (int key in toRemove)
                {
                    gasInstigators.Remove(key);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Don't save/load gas tracking - it's transient and will be regenerated
            // Gas clouds dissipate quickly anyway
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                gasInstigators = new Dictionary<int, (Pawn, int)>();
            }
        }
    }
}
