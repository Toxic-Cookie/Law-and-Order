using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Law_and_Order.Source.Infiltration;
using UnityEngine;

namespace LawAndOrder
{
    /// <summary>
    /// Categories of intelligence that infiltrators can gather about a colony.
    /// Each category provides different tactical advantages to hostile factions.
    /// </summary>
    public enum IntelCategory
    {
        /// <summary>
        /// Defense intelligence: Turret counts, killbox locations, colonist combat skills, defensive structures
        /// Usage: Enables sappers to bypass killboxes, scales raid size appropriately
        /// </summary>
        Defense,

        /// <summary>
        /// Wealth intelligence: Storage locations, high-value items, silver caches, valuable resources
        /// Usage: Enables drop pod strikes on storage rooms, theft-focused raids
        /// </summary>
        Wealth,

        /// <summary>
        /// Schedule intelligence: Guard shifts, unmanned periods, colonist sleep routines, activity patterns
        /// Usage: Enables night raids during low-guard periods, timing attacks for maximum vulnerability
        /// </summary>
        Schedule,

        /// <summary>
        /// Layout intelligence: Critical infrastructure, power grids, prisoner locations, key buildings
        /// Usage: Enables multi-pronged coordinated assaults, infrastructure sabotage
        /// </summary>
        Layout
    }

    /// <summary>
    /// Represents intelligence gathered by an infiltrator about a specific colony/map.
    /// Intelligence quality improves with time spent gathering and infiltrator intelligence stat.
    /// </summary>
    public class IntelligenceData : IExposable
    {
        // Core identification
        public IntelCategory category;
        public int mapID = -1; // Which map this intel is about
        public Pawn gatheredBy; // Which infiltrator gathered this intel
        public int tickGathered; // When was this intel gathered

        // Quality metrics (0.0 - 1.0 scale)
        public float completeness = 0f; // How complete is this intelligence (0% to 100%)
        public float accuracy = 0f; // How accurate is this intelligence (affected by infiltrator intelligence stat)

        // Specific intel data (category-dependent)
        public Dictionary<string, object> details = new Dictionary<string, object>();

        // Transmission tracking
        public bool transmitted = false; // Has this been sent to hostile faction?
        public int tickTransmitted = -1; // When was it transmitted?

        public IntelligenceData()
        {
        }

        public IntelligenceData(IntelCategory category, int mapID, Pawn gatherer)
        {
            this.category = category;
            this.mapID = mapID;
            this.gatheredBy = gatherer;
            this.tickGathered = Find.TickManager.TicksGame;

            // Base accuracy from infiltrator intelligence
            var comp = gatherer?.TryGetComp<CompHiddenIdentity>();
            if (comp != null && comp.Hediff != null)
            {
                this.accuracy = 0.5f + (comp.Hediff.intelligenceStat * 0.5f); // 50-100% accuracy based on intelligence
            }
            else
            {
                this.accuracy = 0.7f; // Default accuracy for non-infiltrators
            }
        }

        /// <summary>
        /// Intelligence quality level based on completeness and accuracy.
        /// Used for player warnings and raid modifications.
        /// </summary>
        public IntelQuality Quality
        {
            get
            {
                float qualityScore = (completeness + accuracy) / 2f;
                if (qualityScore >= 0.75f) return IntelQuality.High;
                if (qualityScore >= 0.45f) return IntelQuality.Moderate;
                return IntelQuality.Low;
            }
        }

        /// <summary>
        /// Is this intelligence still relevant? Old intel degrades in value.
        /// </summary>
        public bool IsRelevant
        {
            get
            {
                if (!transmitted) return true; // Untransmitted intel is always relevant

                int ticksSinceTransmission = Find.TickManager.TicksGame - tickTransmitted;
                int daysOld = ticksSinceTransmission / GenDate.TicksPerDay;

                // Intel expires after 60 days (colony changes too much)
                return daysOld < 60;
            }
        }

        /// <summary>
        /// How old is this intelligence in days?
        /// </summary>
        public int DaysOld
        {
            get
            {
                int ticksSinceGathered = Find.TickManager.TicksGame - tickGathered;
                return ticksSinceGathered / GenDate.TicksPerDay;
            }
        }

        /// <summary>
        /// Add a piece of information to this intelligence report.
        /// </summary>
        public void AddDetail(string key, object value)
        {
            if (details.ContainsKey(key))
            {
                details[key] = value;
            }
            else
            {
                details.Add(key, value);
            }
        }

        /// <summary>
        /// Get a detail value with type safety.
        /// </summary>
        public T GetDetail<T>(string key, T defaultValue = default(T))
        {
            if (details.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Mark this intelligence as transmitted to hostile faction.
        /// </summary>
        public void MarkTransmitted()
        {
            transmitted = true;
            tickTransmitted = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Improve completeness of this intelligence (called during gathering).
        /// </summary>
        public void ImproveCompleteness(float amount)
        {
            completeness = Mathf.Clamp01(completeness + amount);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref category, "category");
            Scribe_Values.Look(ref mapID, "mapID", -1);
            Scribe_References.Look(ref gatheredBy, "gatheredBy");
            Scribe_Values.Look(ref tickGathered, "tickGathered");
            Scribe_Values.Look(ref completeness, "completeness", 0f);
            Scribe_Values.Look(ref accuracy, "accuracy", 0f);
            Scribe_Collections.Look(ref details, "details", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref transmitted, "transmitted", false);
            Scribe_Values.Look(ref tickTransmitted, "tickTransmitted", -1);

            // Initialize dictionary if null after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit && details == null)
            {
                details = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Generate a human-readable summary of this intelligence.
        /// </summary>
        public string GetSummary()
        {
            string categoryName = ("LawAndOrder.IntelCategory." + category.ToString()).Translate();
            string qualityName = ("LawAndOrder.IntelQuality." + Quality.ToString()).Translate();

            return string.Format(
                "LawAndOrder.IntelSummary".Translate(
                    categoryName,
                    qualityName,
                    (completeness * 100f).ToString("F0"),
                    DaysOld
                )
            );
        }
    }

    /// <summary>
    /// Overall quality level of gathered intelligence.
    /// Determines raid modification strength and player warning severity.
    /// </summary>
    public enum IntelQuality
    {
        /// <summary>Low quality intelligence: Minor raid modifications</summary>
        Low,

        /// <summary>Moderate quality intelligence: Significant raid modifications</summary>
        Moderate,

        /// <summary>High quality intelligence: Major raid modifications, significant threat</summary>
        High
    }

    /// <summary>
    /// Utility methods for working with intelligence data.
    /// </summary>
    public static class IntelligenceUtils
    {
        /// <summary>
        /// Gather intelligence for a specific category on a map.
        /// Returns progress made (0.0 - 1.0).
        /// </summary>
        public static float GatherIntelligence(Pawn infiltrator, Map map, IntelCategory category)
        {
            if (infiltrator == null || map == null) return 0f;

            var comp = infiltrator.TryGetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null) return 0f;

            // Base gathering rate (per gathering action)
            float baseRate = 0.05f; // 5% per action

            // Intelligence modifier (smarter = faster gathering)
            float intelligenceModifier = 0.5f + comp.Hediff.intelligenceStat;

            // Social skill helps with gathering (talking to colonists, observation)
            float socialModifier = 1.0f;
            if (infiltrator.skills != null)
            {
                int socialSkill = infiltrator.skills.GetSkill(SkillDefOf.Social).Level;
                socialModifier = 1.0f + (socialSkill / 20f); // Up to +100% at level 20
            }

            float gatheringProgress = baseRate * intelligenceModifier * socialModifier;

            return Mathf.Clamp01(gatheringProgress);
        }

        /// <summary>
        /// Gather specific details for a category based on current map state.
        /// </summary>
        public static void PopulateIntelDetails(IntelligenceData intel, Map map)
        {
            if (intel == null || map == null) return;

            switch (intel.category)
            {
                case IntelCategory.Defense:
                    GatherDefenseIntel(intel, map);
                    break;
                case IntelCategory.Wealth:
                    GatherWealthIntel(intel, map);
                    break;
                case IntelCategory.Schedule:
                    GatherScheduleIntel(intel, map);
                    break;
                case IntelCategory.Layout:
                    GatherLayoutIntel(intel, map);
                    break;
            }
        }

        private static void GatherDefenseIntel(IntelligenceData intel, Map map)
        {
            // Count turrets
            int turretCount = map.listerBuildings.allBuildingsColonist
                .Count(b => b.def.building?.IsTurret ?? false);
            intel.AddDetail("turretCount", turretCount);

            // Count colonists with combat skills
            int combatColonists = map.mapPawns.FreeColonistsSpawned
                .Count(p => p.skills?.GetSkill(SkillDefOf.Shooting)?.Level >= 8);
            intel.AddDetail("combatColonists", combatColonists);

            // Estimate defensive strength
            float defenseStrength = turretCount * 2f + combatColonists * 1.5f;
            intel.AddDetail("defenseStrength", defenseStrength);

            // Identify killbox (areas with high turret density)
            // This is a simplified heuristic
            bool hasKillbox = turretCount >= 5;
            intel.AddDetail("hasKillbox", hasKillbox);
        }

        private static void GatherWealthIntel(IntelligenceData intel, Map map)
        {
            // Find storage zones
            List<IntVec3> storageLocations = new List<IntVec3>();
            foreach (var zone in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
            {
                if (zone.cells.Any())
                {
                    storageLocations.Add(zone.cells.First());
                }
            }
            intel.AddDetail("storageLocationCount", storageLocations.Count);

            // Estimate wealth (simplified - just count silver and valuable items)
            float estimatedWealth = 0f;
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing.def.category == ThingCategory.Item)
                {
                    estimatedWealth += thing.MarketValue * thing.stackCount;
                }
            }
            intel.AddDetail("estimatedWealth", estimatedWealth);

            // Find high-value items
            bool hasValuables = estimatedWealth > 10000f;
            intel.AddDetail("hasValuables", hasValuables);
        }

        private static void GatherScheduleIntel(IntelligenceData intel, Map map)
        {
            // Count colonists on night shift (work assignments during night hours)
            // This is simplified - in real implementation would check work schedules
            int totalColonists = map.mapPawns.FreeColonistsSpawnedCount;
            int nightGuards = Mathf.CeilToInt(totalColonists * 0.3f); // Estimate 30% on night duty
            intel.AddDetail("nightGuards", nightGuards);

            // Identify vulnerable time periods (when fewer colonists are active)
            // Simplified: night = vulnerable if < 3 guards
            bool vulnerableAtNight = nightGuards < 3;
            intel.AddDetail("vulnerableAtNight", vulnerableAtNight);

            // Average colonist count
            intel.AddDetail("totalColonists", totalColonists);
        }

        private static void GatherLayoutIntel(IntelligenceData intel, Map map)
        {
            // Find prisoner cells
            int prisonCells = map.listerBuildings.allBuildingsColonist
                .Count(b => b is Building_Bed bed && bed.ForPrisoners);
            intel.AddDetail("prisonCells", prisonCells);

            // Map entry points (count number of doors on map edge)
            int entryPoints = 0;
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Door door)
                {
                    if (door.Position.x == 0 || door.Position.x == map.Size.x - 1 ||
                        door.Position.z == 0 || door.Position.z == map.Size.z - 1)
                    {
                        entryPoints++;
                    }
                }
            }
            intel.AddDetail("entryPoints", entryPoints);

            // Count critical buildings (workshops, storage, etc.)
            int criticalBuildings = map.listerBuildings.allBuildingsColonist
                .Count(b => b.def.designationCategory != null);
            intel.AddDetail("criticalBuildings", criticalBuildings);

            // Central base location (approximation)
            var colonistPosition = map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? IntVec3.Invalid;
            intel.AddDetail("baseCenter", colonistPosition);
        }

        /// <summary>
        /// Calculate raid point modifier based on intelligence quality and category.
        /// Target: +30-50% raid points total with multiple intel types
        /// </summary>
        public static float GetRaidPointModifier(List<IntelligenceData> intel)
        {
            if (intel == null || intel.Count == 0) return 1.0f;

            float modifier = 1.0f;

            foreach (var data in intel.Where(i => i.IsRelevant))
            {
                switch (data.Quality)
                {
                    case IntelQuality.Low:
                        modifier += 0.08f; // +8% raid points
                        break;
                    case IntelQuality.Moderate:
                        modifier += 0.15f; // +15% raid points
                        break;
                    case IntelQuality.High:
                        modifier += 0.20f; // +20% raid points
                        break;
                }
            }

            // Cap at +60% raid points (1.6x) to stay within target range
            return Mathf.Min(modifier, 1.6f);
        }
    }
}
