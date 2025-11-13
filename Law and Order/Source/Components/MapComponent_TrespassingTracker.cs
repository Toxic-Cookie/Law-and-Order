using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// Tracks when hostile pawns trespass into the player's home area
    /// Records trespassing crimes once per pawn per raid to avoid spam
    /// </summary>
    public class MapComponent_TrespassingTracker : MapComponent
    {
        // Configuration constants
        private const int TICK_INTERVAL = 60; // Check every 60 ticks (1 second) for performance

        // Track which pawns have already been recorded for trespassing
        private HashSet<Pawn> recordedTrespassers = new HashSet<Pawn>();
        private int tickCounter = 0;

        public MapComponent_TrespassingTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            tickCounter++;
            if (tickCounter < TICK_INTERVAL)
            {
                return;
            }

            tickCounter = 0;

            // Check if home area exists
            if (map?.areaManager?.Home == null)
            {
                return;
            }

            // Clean up dead/despawned pawns from tracking set
            CleanupInvalidTrespassers();

            // Find all hostile pawns on the map (humanlike only - animals shouldn't be criminals)
            var hostilePawns = map.mapPawns.AllPawnsSpawned
                .Where(p => p != null &&
                           p.RaceProps.Humanlike &&
                           p.HostileTo(Faction.OfPlayer) &&
                           !p.Downed &&
                           !p.Dead);

            foreach (var pawn in hostilePawns)
            {
                // Check if pawn is in home area and hasn't been recorded yet
                if (map.areaManager.Home[pawn.Position] && !recordedTrespassers.Contains(pawn))
                {
                    // Record trespassing crime
                    recordedTrespassers.Add(pawn);

                    CrimeUtils.RecordCrime(
                        criminal: pawn,
                        crimeType: CrimeType.Trespassing,
                        additionalInfo: $"Entered home area at {pawn.Position}"
                    );

#if DEBUG
                    Mod.Log?.Message($"Recorded trespassing crime: {pawn.LabelShort} entered home area at {pawn.Position}");
#endif
                }
            }
        }

        /// <summary>
        /// Remove dead or despawned pawns from the tracking set to prevent memory leaks
        /// </summary>
        private void CleanupInvalidTrespassers()
        {
            if (recordedTrespassers == null || recordedTrespassers.Count == 0)
            {
                return;
            }

            // Create list of pawns to remove (can't modify set while iterating)
            List<Pawn> toRemove = new List<Pawn>();

            foreach (var pawn in recordedTrespassers)
            {
                // Remove if pawn is null, dead, or no longer spawned on this map
                if (pawn == null || pawn.Dead || pawn.Map != map || !pawn.Spawned)
                {
                    toRemove.Add(pawn);
                }
            }

            // Remove invalid pawns
            foreach (var pawn in toRemove)
            {
                recordedTrespassers.Remove(pawn);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref recordedTrespassers, "recordedTrespassers", LookMode.Reference);

            // Initialize set if null after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit && recordedTrespassers == null)
            {
                recordedTrespassers = new HashSet<Pawn>();
            }
        }
    }
}
