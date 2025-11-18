using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Handles sabotage execution by accomplices during raids
    /// </summary>
    public static class SabotageExecutor
    {
        private const float DISCOVERY_CHANCE = 0.30f; // 30% chance to be caught

        /// <summary>
        /// Assign sabotage tasks to all accomplices for a specific infiltrator
        /// Called when infiltrator's "ActivateAccomplices" reaction is triggered
        /// </summary>
        public static void AssignSabotageTasks(Pawn infiltrator, Map map)
        {
            if (infiltrator == null || map == null)
                return;

            var infiltratorComp = infiltrator.TryGetComp<CompHiddenIdentity>();
            if (infiltratorComp?.Hediff == null)
                return;

            float intelligence = infiltratorComp.Hediff.intelligenceStat;

            // Get all accomplices for this infiltrator
            var accomplices = AccompliceUtils.GetAccomplicesForInfiltrator(infiltrator);

            if (accomplices.Count == 0)
            {
                ModLog.Debug($"Infiltrator {infiltrator.LabelShort} has no accomplices to activate");
                return;
            }

            ModLog.Info($"Assigning sabotage tasks to {accomplices.Count} accomplices for {infiltrator.LabelShort}");

            // Assign different tasks based on intelligence level
            if (intelligence > 0.7f)
            {
                // High intelligence: Strategic sabotage (turrets, gates)
                AssignStrategicSabotage(accomplices, map);
            }
            else if (intelligence > 0.5f)
            {
                // Medium intelligence: Disruptive sabotage (fires, equipment)
                AssignDisruptiveSabotage(accomplices, map);
            }
            else
            {
                // Low intelligence: Simple distractions
                AssignSimpleSabotage(accomplices, map);
            }
        }

        /// <summary>
        /// Assign sabotage tasks during an active raid
        /// Called from raid patches
        /// </summary>
        public static void AssignSabotageForRaid(Map map)
        {
            if (map == null)
                return;

            // Find all accomplices who should perform sabotage
            var activeAccomplices = new List<Pawn>();

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited && comp.ShouldPerformSabotage())
                {
                    activeAccomplices.Add(pawn);
                }
            }

            if (activeAccomplices.Count == 0)
                return;

            ModLog.Info($"Raid triggered: {activeAccomplices.Count} accomplices will perform sabotage");

            // Assign tasks to each accomplice
            foreach (var accomplice in activeAccomplices)
            {
                var comp = accomplice.TryGetComp<CompAccomplice>();
                if (comp?.Recruiter == null)
                    continue;

                var recruiterComp = comp.Recruiter.TryGetComp<CompHiddenIdentity>();
                float intelligence = recruiterComp?.Hediff?.intelligenceStat ?? 0.5f;

                // Assign 1-2 tasks per accomplice
                int taskCount = intelligence > 0.7f ? 2 : 1;

                for (int i = 0; i < taskCount; i++)
                {
                    AssignRandomSabotageTask(accomplice, comp, map, intelligence);
                }
            }

            // Execute sabotage tasks immediately
            ExecuteAllPendingSabotage(map);
        }

        /// <summary>
        /// Assign strategic sabotage (high intelligence)
        /// Disables turrets, opens defensive gates
        /// </summary>
        private static void AssignStrategicSabotage(List<Pawn> accomplices, Map map)
        {
            var tasks = new List<SabotageTask>();

            // Find turrets to disable
            var turrets = map.listerBuildings.allBuildingsColonist
                .Where(b => b is Building_Turret && b.TryGetComp<CompPowerTrader>()?.PowerOn == true)
                .OrderByDescending(b => (b as Building_TurretGun)?.GunCompEq?.PrimaryVerb?.verbProps?.range ?? 0f)
                .Take(2) // Disable up to 2 turrets
                .ToList();

            foreach (var turret in turrets)
            {
                tasks.Add(new SabotageTask(SabotageType.DisableTurret, turret.Position, turret, requiresHighIntel: true));
            }

            // Find defensive doors to open
            var defensiveDoors = map.listerBuildings.allBuildingsColonist
                .OfType<Building_Door>()
                .Where(d => d.Position.InHorDistOf(map.Center, map.Size.x / 3f)) // Near perimeter
                .OrderBy(d => d.Position.DistanceTo(map.Center))
                .Take(3) // Open up to 3 doors
                .ToList();

            foreach (var door in defensiveDoors)
            {
                tasks.Add(new SabotageTask(SabotageType.OpenDoor, door.Position, door, requiresHighIntel: true));
            }

            // Distribute tasks to accomplices
            DistributeTasks(accomplices, tasks);
        }

        /// <summary>
        /// Assign disruptive sabotage (medium intelligence)
        /// Starts fires, breaks equipment
        /// </summary>
        private static void AssignDisruptiveSabotage(List<Pawn> accomplices, Map map)
        {
            var tasks = new List<SabotageTask>();

            // Start 2-3 fires in the colony
            var fireLocations = FindFireLocations(map, 3);
            foreach (var location in fireLocations)
            {
                tasks.Add(new SabotageTask(SabotageType.StartFire, location, requiresHighIntel: false));
            }

            // Break critical equipment
            var equipment = map.listerBuildings.allBuildingsColonist
                .Where(b => b.TryGetComp<CompPowerTrader>() != null || b.TryGetComp<CompBreakdownable>() != null)
                .Where(b => !(b is Building_Turret)) // Don't double-count turrets
                .OrderBy(x => Rand.Value)
                .Take(2)
                .ToList();

            foreach (var item in equipment)
            {
                tasks.Add(new SabotageTask(SabotageType.BreakEquipment, item.Position, item, requiresHighIntel: false));
            }

            // Distribute tasks to accomplices
            DistributeTasks(accomplices, tasks);
        }

        /// <summary>
        /// Assign simple sabotage (low intelligence)
        /// Just causes distractions
        /// </summary>
        private static void AssignSimpleSabotage(List<Pawn> accomplices, Map map)
        {
            var tasks = new List<SabotageTask>();

            // Create simple distractions
            foreach (var accomplice in accomplices)
            {
                IntVec3 location = accomplice.Position;
                tasks.Add(new SabotageTask(SabotageType.CauseDistraction, location, requiresHighIntel: false));
            }

            // Distribute tasks to accomplices
            DistributeTasks(accomplices, tasks);
        }

        /// <summary>
        /// Assign a random sabotage task to an accomplice
        /// </summary>
        private static void AssignRandomSabotageTask(Pawn accomplice, CompAccomplice comp, Map map, float intelligence)
        {
            SabotageType type;
            IntVec3 location;
            Thing target = null;

            if (intelligence > 0.7f && Rand.Chance(0.6f))
            {
                // High intel: Prefer strategic targets
                if (Rand.Chance(0.5f))
                {
                    // Find a turret
                    target = map.listerBuildings.allBuildingsColonist
                        .Where(b => b is Building_Turret && b.TryGetComp<CompPowerTrader>()?.PowerOn == true)
                        .RandomElementWithFallback();

                    if (target != null)
                    {
                        type = SabotageType.DisableTurret;
                        location = target.Position;
                    }
                    else
                    {
                        type = SabotageType.CauseDistraction;
                        location = accomplice.Position;
                    }
                }
                else
                {
                    // Find a door
                    target = map.listerBuildings.allBuildingsColonist
                        .OfType<Building_Door>()
                        .RandomElementWithFallback();

                    if (target != null)
                    {
                        type = SabotageType.OpenDoor;
                        location = target.Position;
                    }
                    else
                    {
                        type = SabotageType.CauseDistraction;
                        location = accomplice.Position;
                    }
                }
            }
            else
            {
                // Lower intel: Fires and equipment damage
                if (Rand.Chance(0.5f))
                {
                    type = SabotageType.StartFire;
                    location = FindFireLocations(map, 1).FirstOrDefault();
                }
                else
                {
                    type = SabotageType.CauseDistraction;
                    location = accomplice.Position;
                }
            }

            var task = new SabotageTask(type, location, target, intelligence > 0.7f);
            comp.AddSabotageTask(task);
        }

        /// <summary>
        /// Distribute sabotage tasks evenly among accomplices
        /// </summary>
        private static void DistributeTasks(List<Pawn> accomplices, List<SabotageTask> tasks)
        {
            if (accomplices.Count == 0 || tasks.Count == 0)
                return;

            int accompliceIndex = 0;
            foreach (var task in tasks)
            {
                var accomplice = accomplices[accompliceIndex % accomplices.Count];
                var comp = accomplice.TryGetComp<CompAccomplice>();

                if (comp != null && comp.IsRecruited)
                {
                    comp.AddSabotageTask(task);
                }

                accompliceIndex++;
            }
        }

        /// <summary>
        /// Find good locations to start fires
        /// </summary>
        private static List<IntVec3> FindFireLocations(Map map, int count)
        {
            var locations = new List<IntVec3>();

            // Find flammable buildings or stockpiles
            var flammableThings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                .Where(t => t.def.useHitPoints && t.Stuff?.GetStatValueAbstract(StatDefOf.Flammability) > 0f)
                .OrderBy(x => Rand.Value)
                .Take(count)
                .ToList();

            foreach (var thing in flammableThings)
            {
                locations.Add(thing.Position);
            }

            // Fill remaining with random indoor locations
            while (locations.Count < count)
            {
                IntVec3 randomCell;
                if (CellFinderLoose.TryGetRandomCellWith(
                    cell => cell.Roofed(map) && cell.Walkable(map) && !cell.ContainsStaticFire(map),
                    map,
                    1000,
                    out randomCell))
                {
                    locations.Add(randomCell);
                }
                else
                {
                    break; // Can't find more locations
                }
            }

            return locations;
        }

        /// <summary>
        /// Execute all pending sabotage tasks for all accomplices on the map
        /// </summary>
        public static void ExecuteAllPendingSabotage(Map map)
        {
            if (map == null)
                return;

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp == null || !comp.IsRecruited)
                    continue;

                var tasks = comp.GetPendingSabotage();
                if (tasks.Count == 0)
                    continue;

                foreach (var task in tasks.ToList()) // ToList() to avoid modification during iteration
                {
                    ExecuteSabotageTask(pawn, comp, task, map);
                }
            }
        }

        /// <summary>
        /// Execute a single sabotage task
        /// </summary>
        private static void ExecuteSabotageTask(Pawn accomplice, CompAccomplice comp, SabotageTask task, Map map)
        {
            if (accomplice == null || accomplice.Dead || task == null)
                return;

            ModLog.Debug($"Accomplice {accomplice.LabelShort} executing sabotage: {task.type}");

            bool success = false;

            switch (task.type)
            {
                case SabotageType.DisableTurret:
                    success = SabotageDisableTurret(task.targetThing, map);
                    break;

                case SabotageType.OpenDoor:
                    success = SabotageOpenDoor(task.targetThing);
                    break;

                case SabotageType.StartFire:
                    success = SabotageStartFire(task.targetLocation, map);
                    break;

                case SabotageType.BreakEquipment:
                    success = SabotageBreakEquipment(task.targetThing);
                    break;

                case SabotageType.CauseDistraction:
                    success = SabotageCauseDistraction(accomplice);
                    break;

                case SabotageType.SabotageDefenses:
                    success = SabotageGenericDefense(task.targetLocation, map);
                    break;
            }

            if (success)
            {
                comp.CompleteSabotageTask(task);

                // Discovery check
                if (Rand.Chance(DISCOVERY_CHANCE))
                {
                    comp.DiscoverAccomplice();
                }
            }
            else
            {
                // Failed sabotage, remove task
                comp.CompleteSabotageTask(task);
            }
        }

        /// <summary>
        /// Sabotage: Disable a turret (damage it to 70% HP or disable power)
        /// </summary>
        private static bool SabotageDisableTurret(Thing turret, Map map)
        {
            if (turret == null || turret.Destroyed)
                return false;

            Building_Turret buildingTurret = turret as Building_Turret;
            if (buildingTurret == null)
                return false;

            // Damage the turret to ~70% HP
            int damageAmount = (int)(turret.MaxHitPoints * 0.3f);
            turret.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount, instigator: null));

            // Also try to disable power
            var powerComp = turret.TryGetComp<CompPowerTrader>();
            if (powerComp != null)
            {
                powerComp.PowerOutput = 0f;
            }

            Messages.Message(
                "LawAndOrder_SabotageTurretDisabled".Translate(turret.Label),
                turret,
                MessageTypeDefOf.NegativeEvent
            );

            ModLog.Info($"Sabotage: Turret {turret.Label} disabled");
            return true;
        }

        /// <summary>
        /// Sabotage: Open a defensive door/gate
        /// </summary>
        private static bool SabotageOpenDoor(Thing door)
        {
            if (door == null || door.Destroyed)
                return false;

            Building_Door buildingDoor = door as Building_Door;
            if (buildingDoor == null)
                return false;

            // Force the door open
            buildingDoor.StartManualOpenBy(null);

            Messages.Message(
                "LawAndOrder_SabotageDoorOpened".Translate(door.Label),
                door,
                MessageTypeDefOf.NegativeEvent
            );

            ModLog.Info($"Sabotage: Door {door.Label} opened");
            return true;
        }

        /// <summary>
        /// Sabotage: Start a fire at a location
        /// </summary>
        private static bool SabotageStartFire(IntVec3 location, Map map)
        {
            if (!location.IsValid || !location.InBounds(map))
                return false;

            // Spawn fire
            FireUtility.TryStartFireIn(location, map, 0.5f, null);

            Messages.Message(
                "LawAndOrder_SabotageFireStarted".Translate(),
                new TargetInfo(location, map),
                MessageTypeDefOf.ThreatBig
            );

            ModLog.Info($"Sabotage: Fire started at {location}");
            return true;
        }

        /// <summary>
        /// Sabotage: Break critical equipment
        /// </summary>
        private static bool SabotageBreakEquipment(Thing equipment)
        {
            if (equipment == null || equipment.Destroyed)
                return false;

            // Damage the equipment
            int damageAmount = (int)(equipment.MaxHitPoints * 0.4f);
            equipment.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount, instigator: null));

            // Trigger breakdown if possible
            var breakdownComp = equipment.TryGetComp<CompBreakdownable>();
            if (breakdownComp != null && !breakdownComp.BrokenDown)
            {
                breakdownComp.DoBreakdown();
            }

            Messages.Message(
                "LawAndOrder_SabotageEquipmentBroken".Translate(equipment.Label),
                equipment,
                MessageTypeDefOf.NegativeEvent
            );

            ModLog.Info($"Sabotage: Equipment {equipment.Label} damaged");
            return true;
        }

        /// <summary>
        /// Sabotage: Cause a distraction (mental break)
        /// </summary>
        private static bool SabotageCauseDistraction(Pawn accomplice)
        {
            if (accomplice == null || accomplice.Dead)
                return false;

            // Cause a minor mental break (daze)
            if (accomplice.mindState?.mentalStateHandler != null)
            {
                accomplice.mindState.mentalStateHandler.TryStartMentalState(
                    MentalStateDefOf.Wander_Sad,
                    reason: "Sabotage distraction",
                    forceWake: false
                );
            }

            ModLog.Debug($"Sabotage: {accomplice.LabelShort} caused a distraction");
            return true;
        }

        /// <summary>
        /// Sabotage: Generic defense sabotage
        /// </summary>
        private static bool SabotageGenericDefense(IntVec3 location, Map map)
        {
            if (!location.IsValid || !location.InBounds(map))
                return false;

            // Find anything defensive at the location
            var things = location.GetThingList(map);
            foreach (var thing in things)
            {
                if (thing is Building_Turret)
                {
                    return SabotageDisableTurret(thing, map);
                }
                else if (thing is Building_Door)
                {
                    return SabotageOpenDoor(thing);
                }
            }

            // No specific target, start a fire instead
            return SabotageStartFire(location, map);
        }
    }
}
