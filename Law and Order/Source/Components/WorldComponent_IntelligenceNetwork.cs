using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Law_and_Order.Source.Infiltration;
using LawAndOrder;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// WorldComponent that tracks all intelligence gathered by infiltrators across all maps.
    /// Manages intelligence transmission to hostile factions and periodic gathering checks.
    /// </summary>
    public class WorldComponent_IntelligenceNetwork : WorldComponent
    {
        // Intelligence storage per faction
        private Dictionary<int, List<IntelligenceData>> factionIntelligence = new Dictionary<int, List<IntelligenceData>>();

        // Timing
        private int nextGatherCheck = 0;
        private int nextCleanupCheck = 0;

        // Constants
        private const int GatherCheckInterval = GenDate.TicksPerHour; // Check every hour
        private const int CleanupInterval = GenDate.TicksPerDay; // Cleanup old intel daily

        public WorldComponent_IntelligenceNetwork(World world) : base(world)
        {
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            int currentTick = Find.TickManager.TicksGame;

            // Periodic intelligence gathering check
            if (currentTick >= nextGatherCheck)
            {
                CheckInfiltratorsGatherIntelligence();
                nextGatherCheck = currentTick + GatherCheckInterval;
            }

            // Periodic cleanup of old/irrelevant intelligence
            if (currentTick >= nextCleanupCheck)
            {
                CleanupOldIntelligence();
                nextCleanupCheck = currentTick + CleanupInterval;
            }
        }

        /// <summary>
        /// Check all infiltrators on all player maps and trigger intelligence gathering
        /// </summary>
        private void CheckInfiltratorsGatherIntelligence()
        {
            // Check all player home maps
            foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
            {
                // Find all pawns with hidden identities
                var infiltrators = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.health?.hediffSet != null)
                    .Select(p => new { Pawn = p, Hediff = p.health.hediffSet.GetFirstHediffOfDef(LawAndOrder_HediffDefOf.LawAndOrder_HiddenIdentity) as Hediff_HiddenIdentity })
                    .Where(x => x.Hediff != null && !x.Hediff.identityRevealed)
                    .ToList();

                foreach (var infiltrator in infiltrators)
                {
                    // Try to gather intelligence
                    if (infiltrator.Hediff.CanGatherIntelNow())
                    {
                        infiltrator.Hediff.GatherIntelligence(map);
                    }
                }

                if (Prefs.DevMode && infiltrators.Any())
                {
                    Utils.ModLog.Debug($"Checked {infiltrators.Count} infiltrators on {map.Parent.Label} for intel gathering");
                }
            }
        }

        /// <summary>
        /// Remove old/irrelevant intelligence that is no longer useful
        /// </summary>
        private void CleanupOldIntelligence()
        {
            if (factionIntelligence == null)
                return;

            int removedCount = 0;

            foreach (var factionId in factionIntelligence.Keys.ToList())
            {
                if (!factionIntelligence.ContainsKey(factionId))
                    continue;

                var intelList = factionIntelligence[factionId];
                int before = intelList.Count;

                // Remove irrelevant intel
                intelList.RemoveAll(intel => !intel.IsRelevant);

                int removed = before - intelList.Count;
                removedCount += removed;

                // Remove empty lists
                if (intelList.Count == 0)
                {
                    factionIntelligence.Remove(factionId);
                }
            }

            if (Prefs.DevMode && removedCount > 0)
            {
                Utils.ModLog.Debug($"Cleaned up {removedCount} old intelligence reports");
            }
        }

        /// <summary>
        /// Add intelligence to a faction's intelligence database
        /// </summary>
        public void AddIntelligence(Faction faction, IntelligenceData intel)
        {
            if (faction == null || intel == null)
                return;

            if (factionIntelligence == null)
                factionIntelligence = new Dictionary<int, List<IntelligenceData>>();

            int factionId = faction.loadID;

            if (!factionIntelligence.ContainsKey(factionId))
            {
                factionIntelligence[factionId] = new List<IntelligenceData>();
            }

            // Check if we already have this exact intel (avoid duplicates)
            if (!factionIntelligence[factionId].Any(i => i.category == intel.category && i.mapID == intel.mapID && !i.transmitted))
            {
                factionIntelligence[factionId].Add(intel);

                if (Prefs.DevMode)
                {
                    Utils.ModLog.Debug($"Added {intel.category} intel for faction {faction.Name} (quality: {intel.Quality})");
                }
            }
        }

        /// <summary>
        /// Get all intelligence that a faction has about player colonies
        /// </summary>
        public List<IntelligenceData> GetFactionIntelligence(Faction faction)
        {
            if (faction == null || factionIntelligence == null)
                return new List<IntelligenceData>();

            int factionId = faction.loadID;

            if (factionIntelligence.TryGetValue(factionId, out List<IntelligenceData> intel))
            {
                // Return only relevant intel
                return intel.Where(i => i.IsRelevant).ToList();
            }

            return new List<IntelligenceData>();
        }

        /// <summary>
        /// Get intelligence for a specific map
        /// </summary>
        public List<IntelligenceData> GetMapIntelligence(Faction faction, Map map)
        {
            if (map == null)
                return new List<IntelligenceData>();

            return GetFactionIntelligence(faction)
                .Where(i => i.mapID == map.uniqueID)
                .ToList();
        }

        /// <summary>
        /// Transmit all intelligence from an infiltrator to their faction when they leave
        /// </summary>
        public void TransmitIntelligence(Pawn infiltrator, Hediff_HiddenIdentity hediff)
        {
            if (infiltrator == null || hediff == null)
                return;

            // Get the infiltrator's real faction
            Faction hostileFaction = hediff.realFaction;
            if (hostileFaction == null || !hostileFaction.HostileTo(Faction.OfPlayer))
            {
                Utils.ModLog.Warning($"Cannot transmit intel: {infiltrator.LabelShort} has no hostile faction");
                return;
            }

            // Get all gathered intelligence
            List<IntelligenceData> gatheredIntel = hediff.GetAllGatheredIntelligence();

            if (gatheredIntel.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Utils.ModLog.Debug($"Infiltrator {infiltrator.LabelShort} left without gathering any intel");
                }
                return;
            }

            // Mark all as transmitted
            hediff.TransmitAllIntelligence();

            // Add to faction intelligence
            foreach (var intel in gatheredIntel)
            {
                AddIntelligence(hostileFaction, intel);
            }

            // Calculate overall threat level
            IntelQuality overallQuality = CalculateOverallThreat(gatheredIntel);

            // Send warning letter to player
            SendEspionageWarning(infiltrator, hostileFaction, gatheredIntel, overallQuality);

            Utils.ModLog.Info($"Infiltrator {infiltrator.LabelShort} transmitted {gatheredIntel.Count} intel reports to {hostileFaction.Name} (threat: {overallQuality})");
        }

        /// <summary>
        /// Calculate overall threat level from multiple intelligence reports
        /// </summary>
        private IntelQuality CalculateOverallThreat(List<IntelligenceData> intel)
        {
            if (intel == null || intel.Count == 0)
                return IntelQuality.Low;

            // Average quality across all intel
            float avgCompleteness = intel.Average(i => i.completeness);
            float avgAccuracy = intel.Average(i => i.accuracy);
            float avgQuality = (avgCompleteness + avgAccuracy) / 2f;

            if (avgQuality >= 0.75f) return IntelQuality.High;
            if (avgQuality >= 0.45f) return IntelQuality.Moderate;
            return IntelQuality.Low;
        }

        /// <summary>
        /// Send warning letter to player about espionage
        /// </summary>
        private void SendEspionageWarning(Pawn infiltrator, Faction faction, List<IntelligenceData> intel, IntelQuality threat)
        {
            string threatLabel = ("LawAndOrder.IntelQuality." + threat.ToString()).Translate();
            string categoriesList = string.Join(", ", intel.Select(i => ("LawAndOrder.IntelCategory." + i.category.ToString()).Translate()));

            LetterDef letterType;
            switch (threat)
            {
                case IntelQuality.High:
                    letterType = LetterDefOf.ThreatBig;
                    break;
                case IntelQuality.Moderate:
                    letterType = LetterDefOf.NegativeEvent;
                    break;
                default:
                    letterType = LetterDefOf.NeutralEvent;
                    break;
            }

            Find.LetterStack.ReceiveLetter(
                "LawAndOrder.LetterLabelEspionageWarning".Translate(),
                "LawAndOrder.LetterEspionageWarning".Translate(
                    infiltrator.LabelShort,
                    faction.Name,
                    intel.Count,
                    categoriesList,
                    threatLabel
                ),
                letterType
            );
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref factionIntelligence, "factionIntelligence", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref nextGatherCheck, "nextGatherCheck", 0);
            Scribe_Values.Look(ref nextCleanupCheck, "nextCleanupCheck", 0);

            // Post-load init
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (factionIntelligence == null)
                    factionIntelligence = new Dictionary<int, List<IntelligenceData>>();
            }
        }

        /// <summary>
        /// Get static instance helper
        /// </summary>
        public static WorldComponent_IntelligenceNetwork Instance
        {
            get
            {
                return Find.World.GetComponent<WorldComponent_IntelligenceNetwork>();
            }
        }
    }
}
