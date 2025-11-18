using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using LudeonTK;
using Law_and_Order.Source;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;
using Law_and_Order.Source.Utils;
using Law_and_Order.Source.Investigation;
using Law_and_Order.Source.Infiltration;

namespace Law_and_Order.Source.Debug
{
    /// <summary>
    /// Debug actions for testing Law and Order mod functionality.
    /// These actions provide quick ways to test all implemented systems without elaborate setup.
    /// </summary>
    public static class DebugActions_LawAndOrder
    {
        // ==================== PHASE 1 & 2: CRIME GENERATION & WITNESSES ====================

        [DebugAction("Law & Order - Crime", "Create Hidden Crime (Theft)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateHiddenCrime(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create a theft crime with no witnesses - should be Hidden
            CrimeUtils.RecordCrime(pawn, CrimeType.Theft, null, null, 0f, "Debug: Hidden crime");

            Messages.Message($"Created HIDDEN crime (Theft) for {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Crime", "Create Assault (close witness)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateAssaultWithWitness(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Find a victim
            Pawn victim = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault(p => p != pawn);
            if (victim == null)
            {
                Messages.Message("No victim available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create assault - FoW will detect nearby witnesses automatically
            CrimeUtils.RecordCrime(pawn, CrimeType.Assault, victim, null, 10f, "Debug: Assault with witness");

            Messages.Message($"Created ASSAULT by {pawn.NameShortColored} against {victim.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Crime", "Create Murder", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateMurder(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn victim = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault(p => p != pawn);
            if (victim == null)
            {
                Messages.Message("No victim available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CrimeUtils.RecordCrime(pawn, CrimeType.Murder, victim, null, 100f, "Debug: Murder", wasVictimKilled: true);

            Messages.Message($"Created MURDER by {pawn.NameShortColored} of {victim.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Crime", "Create Random Crime", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateRandomCrime(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Pick random crime type
            CrimeType[] crimeTypes = new CrimeType[] {
                CrimeType.Assault, CrimeType.Murder, CrimeType.Theft,
                CrimeType.PropertyDestruction, CrimeType.Arson, CrimeType.AnimalAbuse,
                CrimeType.Kidnapping, CrimeType.Trespassing, CrimeType.Vandalism
            };
            CrimeType randomType = crimeTypes.RandomElement();

            // Random victim for relevant crimes
            Pawn victim = null;
            if (randomType == CrimeType.Assault || randomType == CrimeType.Murder)
            {
                victim = Find.CurrentMap.mapPawns.FreeColonists.Where(p => p != pawn).RandomElementWithFallback();
            }

            CrimeUtils.RecordCrime(pawn, randomType, victim, null, Rand.Range(0f, 50f), $"Debug: Random {randomType}");

            Messages.Message($"Created {randomType} for {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Crime", "Add Multiple Crimes (5)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddMultipleCrimes(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn victim = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault(p => p != pawn);

            CrimeUtils.RecordCrime(pawn, CrimeType.Theft, null, null, 0f, "Debug #1");
            CrimeUtils.RecordCrime(pawn, CrimeType.Vandalism, null, null, 0f, "Debug #2");
            CrimeUtils.RecordCrime(pawn, CrimeType.Assault, victim, null, 10f, "Debug #3");
            CrimeUtils.RecordCrime(pawn, CrimeType.PropertyDestruction, null, null, 25f, "Debug #4");
            CrimeUtils.RecordCrime(pawn, CrimeType.Arson, null, null, 50f, "Debug #5");

            Messages.Message($"Added 5 crimes to {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 3 & 4: CASE MANAGEMENT & PUNISHMENT ====================

        [DebugAction("Law & Order - Cases", "Open Justice Tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenJusticeTab()
        {
            Find.MainTabsRoot.SetCurrentTab(MainTabDefOf.LawAndOrder_Justice, true);
            Messages.Message("Opened Justice tab", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Cases", "Log All Active Cases", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogAllCases()
        {
            WorldComponent_JusticeManager manager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            var openCases = manager.GetOpenCases();
            var convictedCases = manager.GetConvictedCases();
            var dismissedCases = manager.GetDismissedCases();

            Log.Message($"=== LAW AND ORDER CASE SUMMARY ===");
            Log.Message($"Open Cases: {openCases.Count}");
            foreach (var c in openCases)
            {
                var crimes = c.GetAssociatedCrimes();
                Log.Message($"  - Case #{c.caseId}: {c.accused?.NameShortColored ?? "Unknown"} ({crimes.Count} crimes)");
            }

            Log.Message($"Convicted Cases: {convictedCases.Count}");
            foreach (var c in convictedCases)
            {
                var crimes = c.GetAssociatedCrimes();
                Log.Message($"  - Case #{c.caseId}: {c.accused?.NameShortColored ?? "Unknown"} ({crimes.Count} crimes)");
            }

            Log.Message($"Dismissed Cases: {dismissedCases.Count}");
            Messages.Message($"Logged {openCases.Count} open, {convictedCases.Count} convicted, {dismissedCases.Count} dismissed cases to console", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Punishment", "Apply Imprisonment (15 days)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyImprisonment(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorldComponent_PunishmentManager manager = Find.World.GetComponent<WorldComponent_PunishmentManager>();
            Punishment punishment = new Punishment(PunishmentType.Imprisonment, pawn, -1);
            punishment.durationDays = 15;

            manager.AddPunishment(punishment);
            Messages.Message($"Applied 15-day imprisonment to {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Punishment", "Apply Beating", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyBeating(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorldComponent_PunishmentManager manager = Find.World.GetComponent<WorldComponent_PunishmentManager>();
            Punishment punishment = new Punishment(PunishmentType.Beating, pawn, -1);

            manager.AddPunishment(punishment);
            Messages.Message($"Applied beating to {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Punishment", "Apply Execution", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyExecution(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorldComponent_PunishmentManager manager = Find.World.GetComponent<WorldComponent_PunishmentManager>();
            Punishment punishment = new Punishment(PunishmentType.Execution, pawn, -1);

            manager.AddPunishment(punishment);
            Messages.Message($"Applied execution to {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Punishment", "Apply Exile", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyExile(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorldComponent_PunishmentManager manager = Find.World.GetComponent<WorldComponent_PunishmentManager>();
            Punishment punishment = new Punishment(PunishmentType.Exile, pawn, -1);

            manager.AddPunishment(punishment);
            Messages.Message($"Applied exile to {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 5: INVESTIGATION & INTERROGATION ====================

        [DebugAction("Law & Order - Investigation", "Log Interrogation Furniture", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogInterrogationFurniture()
        {
            var tables = Comp_InterrogationTable.GetAllInterrogationTables(Find.CurrentMap);
            var chairs = Comp_InterrogationChair.GetAllInterrogationChairs(Find.CurrentMap);

            Log.Message($"=== INTERROGATION FURNITURE ===");
            Log.Message($"Tables: {tables.Count}");
            foreach (var table in tables)
            {
                var comp = table.TryGetComp<Comp_InterrogationTable>();
                Log.Message($"  - {table.Label} at {table.Position} (Designated: {comp?.IsDesignated ?? false})");
            }

            Log.Message($"Chairs: {chairs.Count}");
            foreach (var chair in chairs)
            {
                var comp = chair.TryGetComp<Comp_InterrogationChair>();
                Log.Message($"  - {chair.Label} at {chair.Position} (Designated: {comp?.IsDesignated ?? false})");
            }

            Messages.Message($"Logged {tables.Count} tables and {chairs.Count} chairs to console. Use vanilla furniture and designate it for interrogation.", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 6: CLUE & EVIDENCE SYSTEM ====================

        [DebugAction("Law & Order - Evidence", "Spawn Blood Clue", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnBloodClue()
        {
            SpawnClueAtLocation(Verse.UI.MouseCell(), ClueType.BloodStain);
        }

        [DebugAction("Law & Order - Evidence", "Spawn Footprint Clue", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnFootprintClue()
        {
            SpawnClueAtLocation(Verse.UI.MouseCell(), ClueType.Footprint);
        }

        [DebugAction("Law & Order - Evidence", "Spawn Tool Mark Clue", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnToolMarkClue()
        {
            SpawnClueAtLocation(Verse.UI.MouseCell(), ClueType.ToolMark);
        }

        [DebugAction("Law & Order - Evidence", "Spawn Random Clue", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnRandomClue()
        {
            ClueType[] clueTypes = new ClueType[] {
                ClueType.BloodStain, ClueType.Footprint, ClueType.ToolMark,
                ClueType.DroppedItem, ClueType.FabricScrap, ClueType.FingerprintTrace
            };
            ClueType randomType = clueTypes.RandomElement();
            SpawnClueAtLocation(Verse.UI.MouseCell(), randomType);
        }

        private static void SpawnClueAtLocation(IntVec3 cell, ClueType clueType)
        {
            if (!cell.InBounds(Find.CurrentMap) || cell.Impassable(Find.CurrentMap))
            {
                Messages.Message("Invalid location.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Pick a random colonist as perpetrator
            Pawn perp = Find.CurrentMap.mapPawns.FreeColonists.RandomElementWithFallback();
            if (perp == null)
            {
                Messages.Message("No colonist available to link clue to.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Get the appropriate ThingDef for this clue type
            ThingDef clueDef = GetClueThingDef(clueType);
            if (clueDef == null)
            {
                Messages.Message($"Failed to find ThingDef for {clueType}", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Spawn the clue Thing
            CrimeSceneClue clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);
            clue.clueType = clueType;
            clue.linkedCriminal = perp;
            clue.clueQuality = Rand.Range(0.5f, 1.0f);
            clue.isPlanted = false;
            clue.studyProgress = 0f;
            clue.analyzed = false;

            GenSpawn.Spawn(clue, cell, Find.CurrentMap);

            Messages.Message($"Spawned {clueType} clue at {cell} (linked to {perp.NameShortColored})", MessageTypeDefOf.TaskCompletion, false);
        }

        /// <summary>
        /// Helper to get the appropriate ThingDef for a clue type
        /// </summary>
        private static ThingDef GetClueThingDef(ClueType clueType)
        {
            switch (clueType)
            {
                case ClueType.BloodStain:
                    return ThingDefOf.CrimeSceneClue_Blood;
                case ClueType.Footprint:
                    return ThingDefOf.CrimeSceneClue_Footprint;
                case ClueType.ToolMark:
                    return ThingDefOf.CrimeSceneClue_ToolMark;
                case ClueType.DroppedItem:
                    return ThingDefOf.CrimeSceneClue_DroppedItem;
                case ClueType.FabricScrap:
                    return ThingDefOf.CrimeSceneClue_FabricScrap;
                case ClueType.FingerprintTrace:
                    return ThingDefOf.CrimeSceneClue_Fingerprint;
                default:
                    return ThingDefOf.CrimeSceneClue_Blood; // Fallback
            }
        }

        [DebugAction("Law & Order - Evidence", "Spawn Crime Scene (w/ clues)", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnCrimeScene()
        {
            IntVec3 cell = Verse.UI.MouseCell();

            if (!cell.InBounds(Find.CurrentMap))
            {
                Messages.Message("Invalid location.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn perp = Find.CurrentMap.mapPawns.FreeColonists.RandomElementWithFallback();
            if (perp == null)
            {
                Messages.Message("No colonist available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Create a murder crime at this location
            CrimeUtils.RecordCrime(perp, CrimeType.Murder, null, null, 100f, $"Debug crime scene at {cell}");

            // Spawn 2-3 clues in the area
            int clueCount = Rand.Range(2, 4);
            ClueType[] clueTypes = new ClueType[] { ClueType.BloodStain, ClueType.Footprint, ClueType.FabricScrap };

            for (int i = 0; i < clueCount; i++)
            {
                IntVec3 cluePos = cell + GenRadial.RadialPattern[Rand.Range(0, 9)];
                if (cluePos.InBounds(Find.CurrentMap) && !cluePos.Impassable(Find.CurrentMap))
                {
                    ClueType clueType = clueTypes.RandomElement();
                    ThingDef clueDef = GetClueThingDef(clueType);
                    if (clueDef != null)
                    {
                        CrimeSceneClue clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);
                        clue.clueType = clueType;
                        clue.linkedCriminal = perp;
                        clue.clueQuality = Rand.Range(0.6f, 1.0f);
                        clue.isPlanted = false;
                        GenSpawn.Spawn(clue, cluePos, Find.CurrentMap);
                    }
                }
            }

            Messages.Message($"Spawned crime scene with {clueCount} clues at {cell} ({perp.NameShortColored})", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 6: INFILTRATION & HIDDEN IDENTITY ====================

        [DebugAction("Law & Order - Infiltration", "Check Hidden Identity", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CheckHiddenIdentity(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null)
            {
                Messages.Message($"{pawn.NameShortColored} has NO CompHiddenIdentity component.", MessageTypeDefOf.RejectInput, false);
                ModLog.Warning($"Pawn {pawn.LabelShort} has no CompHiddenIdentity");
                return;
            }

            var hediff = comp.Hediff;
            if (hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} has CompHiddenIdentity but NO hediff data.", MessageTypeDefOf.RejectInput, false);
                ModLog.Warning($"Pawn {pawn.LabelShort} has comp but no hediff");
                return;
            }

            string status = hediff.identityRevealed ? "REVEALED" : "HIDDEN";
            string currentName = pawn.Name.ToStringShort;
            string realName = hediff.realName?.ToStringShort ?? "Unknown";
            Messages.Message($"{pawn.NameShortColored} has hidden identity: Current Name='{currentName}', Real Name='{realName}' ({status})",
                MessageTypeDefOf.TaskCompletion, false);
            ModLog.Info($"Hidden Identity Check: Real={realName}, Current={currentName}, Status={status}");
        }

        [DebugAction("Law & Order - Infiltration", "Add Hidden Identity", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddHiddenIdentity(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp != null && comp.Hediff != null)
            {
                string fakeName = pawn.Name.ToStringShort;
                Messages.Message($"{pawn.NameShortColored} already has a hidden identity (current name: {fakeName})", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // If comp doesn't exist, add it dynamically to the ThingDef
            if (comp == null)
            {
                // Check if the comp is already in the def
                if (!pawn.def.comps.Any(cp => cp is CompProperties_HiddenIdentity))
                {
                    // Add comp properties to ThingDef
                    CompProperties_HiddenIdentity compProps = new CompProperties_HiddenIdentity();
                    pawn.def.comps.Add(compProps);
                }

                // Create and initialize the comp instance
                comp = new CompHiddenIdentity();
                comp.parent = pawn;

                // Initialize the comp properties
                var compPropsInDef = pawn.def.comps.FirstOrDefault(cp => cp is CompProperties_HiddenIdentity);
                if (compPropsInDef != null)
                {
                    comp.Initialize(compPropsInDef);
                }

                // Add to pawn's comps list
                pawn.AllComps.Add(comp);
            }

            // Initialize the hidden identity (this will create the hediff and set fake name)
            string originalName = pawn.Name.ToStringShort;
            comp.InitializeHiddenIdentity(pawn);

            var hediff = comp.Hediff;
            if (hediff != null)
            {
                string fakeName = pawn.Name.ToStringShort; // Now shows fake name
                Messages.Message($"Added hidden identity to pawn.\nOriginal: {originalName}\nFake Name: {fakeName}\nIntelligence: {hediff.intelligenceStat:F2}",
                    MessageTypeDefOf.TaskCompletion, false);
                ModLog.Info($"Debug: Generated hidden identity for {originalName}: Fake={fakeName}, Intel={hediff.intelligenceStat:F2}");
            }
            else
            {
                Messages.Message("Failed to initialize hidden identity", MessageTypeDefOf.RejectInput, false);
            }
        }

        [DebugAction("Law & Order - Infiltration", "Reveal Identity (25%)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RevealIdentityPartial(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} has no hidden identity.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use ProgressDiscovery which handles revelation automatically
            comp.ProgressDiscovery(25f, "Debug action");
            Messages.Message($"Added 25% discovery progress to {pawn.NameShortColored} (now {comp.Hediff.discoveryProgress:F0}%)", MessageTypeDefOf.TaskCompletion, false);

            if (comp.Hediff.identityRevealed)
            {
                Messages.Message($"Identity revealed!", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("Law & Order - Infiltration", "Reveal Identity (100%)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RevealIdentityFull(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} has no hidden identity.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use ProgressDiscovery to trigger full revelation
            comp.ProgressDiscovery(100f, "Debug action - full reveal");
            Messages.Message($"Fully revealed identity of {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 6.5: INTELLIGENCE GATHERING ====================

        [DebugAction("Law & Order - Intelligence", "Spawn Infiltrator (Visitor)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnInfiltrator()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Set flag to guarantee infiltrator in next visitor group
            InfiltratorSpawnPatches.forceNextVisitorGroupInfiltrator = true;

            // Trigger vanilla visitor group incident
            IncidentDef visitorGroupDef = IncidentDefOf.VisitorGroup;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);

            // Find a friendly faction to send the visitors
            Faction faction = Find.FactionManager.AllFactionsVisible
                .Where(f => !f.HostileTo(Faction.OfPlayer) && !f.IsPlayer && !f.def.hidden && f.def.humanlikeFaction)
                .RandomElementWithFallback();

            if (faction == null)
            {
                Messages.Message("No friendly faction available to send visitors.", MessageTypeDefOf.RejectInput, false);
                InfiltratorSpawnPatches.forceNextVisitorGroupInfiltrator = false;
                return;
            }

            parms.faction = faction;
            parms.forced = true;

            if (visitorGroupDef.Worker.TryExecute(parms))
            {
                Messages.Message($"Visitor group from {faction.Name} spawned with guaranteed infiltrator!", MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("Failed to spawn visitor group.", MessageTypeDefOf.RejectInput, false);
                InfiltratorSpawnPatches.forceNextVisitorGroupInfiltrator = false;
            }
        }

        [DebugAction("Law & Order - Intelligence", "Spawn Infiltrator (Wanderer)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnInfiltratorWanderer()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Set flag to guarantee infiltrator in next wanderer join
            InfiltratorSpawnPatches.forceNextWandererInfiltrator = true;

            // Trigger vanilla wanderer join incident
            IncidentDef wandererJoinDef = IncidentDefOf.WandererJoin;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, map);
            parms.forced = true;

            if (wandererJoinDef.Worker.TryExecute(parms))
            {
                Messages.Message("Wanderer spawned with guaranteed infiltrator!", MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("Failed to spawn wanderer.", MessageTypeDefOf.RejectInput, false);
                InfiltratorSpawnPatches.forceNextWandererInfiltrator = false;
            }
        }

        [DebugAction("Law & Order - Intelligence", "Gather Defense Intel (Instant)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GatherDefenseIntel(Pawn pawn)
        {
            GatherIntelForCategory(pawn, LawAndOrder.IntelCategory.Defense);
        }

        [DebugAction("Law & Order - Intelligence", "Gather Wealth Intel (Instant)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GatherWealthIntel(Pawn pawn)
        {
            GatherIntelForCategory(pawn, LawAndOrder.IntelCategory.Wealth);
        }

        [DebugAction("Law & Order - Intelligence", "Gather Schedule Intel (Instant)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GatherScheduleIntel(Pawn pawn)
        {
            GatherIntelForCategory(pawn, LawAndOrder.IntelCategory.Schedule);
        }

        [DebugAction("Law & Order - Intelligence", "Gather Layout Intel (Instant)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void GatherLayoutIntel(Pawn pawn)
        {
            GatherIntelForCategory(pawn, LawAndOrder.IntelCategory.Layout);
        }

        private static void GatherIntelForCategory(Pawn pawn, LawAndOrder.IntelCategory category)
        {
            if (pawn == null || pawn.Dead || pawn.Map == null)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} is not an infiltrator (no hidden identity).", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var hediff = comp.Hediff;
            var intel = hediff.GetOrCreateIntelligence(category, pawn.Map);

            // Set to 100% complete instantly
            intel.completeness = 1.0f;
            LawAndOrder.IntelligenceUtils.PopulateIntelDetails(intel, pawn.Map);

            Messages.Message($"{pawn.NameShortColored} instantly gathered {category} intel (100% complete, quality: {intel.Quality})",
                MessageTypeDefOf.TaskCompletion, false);
            ModLog.Info($"Debug: {pawn.LabelShort} gathered {category} intel - Quality: {intel.Quality}");
        }

        [DebugAction("Law & Order - Intelligence", "View Gathered Intel", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ViewGatheredIntel(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} is not an infiltrator.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var allIntel = comp.Hediff.GetAllGatheredIntelligence();
            if (allIntel.Count == 0)
            {
                Messages.Message($"{pawn.NameShortColored} has not gathered any intelligence yet.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            Log.Message($"=== INTELLIGENCE GATHERED BY {pawn.LabelShort} ===");
            foreach (var intel in allIntel)
            {
                Log.Message($"  - {intel.category}: {intel.completeness * 100f:F1}% complete, {intel.Quality} quality");
                Log.Message($"    Accuracy: {intel.accuracy * 100f:F1}%, Age: {intel.DaysOld} days, Transmitted: {intel.transmitted}");
            }

            Messages.Message($"{pawn.NameShortColored} has gathered {allIntel.Count} intelligence reports (see console)", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Intelligence", "Force Intel Transmission", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceIntelTransmission(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} is not an infiltrator.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var network = WorldComponent_IntelligenceNetwork.Instance;
            if (network == null)
            {
                Messages.Message("IntelligenceNetwork component not found!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var hediff = comp.Hediff;
            var allIntel = hediff.GetAllGatheredIntelligence();
            if (allIntel.Count == 0)
            {
                Messages.Message($"{pawn.NameShortColored} has no intelligence to transmit.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            network.TransmitIntelligence(pawn, hediff);
            Messages.Message($"{pawn.NameShortColored} transmitted {allIntel.Count} intelligence reports!", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Intelligence", "Log Intelligence Network", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogIntelligenceNetwork()
        {
            var network = WorldComponent_IntelligenceNetwork.Instance;
            if (network == null)
            {
                Messages.Message("IntelligenceNetwork component not found!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Log.Message($"=== INTELLIGENCE NETWORK STATUS ===");

            int totalIntel = 0;
            foreach (var faction in Find.FactionManager.AllFactionsVisible.Where(f => f.HostileTo(Faction.OfPlayer)))
            {
                var factionIntel = network.GetFactionIntelligence(faction);
                if (factionIntel.Count > 0)
                {
                    totalIntel += factionIntel.Count;
                    Log.Message($"Faction: {faction.Name} ({factionIntel.Count} intelligence reports)");
                    foreach (var intel in factionIntel)
                    {
                        Log.Message($"  - {intel.category}: {intel.completeness * 100f:F1}% complete, {intel.Quality} quality (Age: {intel.DaysOld} days)");
                    }
                }
            }

            if (totalIntel == 0)
            {
                Log.Message("No intelligence has been transmitted yet.");
            }

            Messages.Message($"Logged intelligence network ({totalIntel} total reports) to console", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Intelligence", "Set Intelligence Stat (Low)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetIntelligenceLow(Pawn pawn)
        {
            SetInfiltratorIntelligence(pawn, 0.3f);
        }

        [DebugAction("Law & Order - Intelligence", "Set Intelligence Stat (Medium)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetIntelligenceMedium(Pawn pawn)
        {
            SetInfiltratorIntelligence(pawn, 0.6f);
        }

        [DebugAction("Law & Order - Intelligence", "Set Intelligence Stat (High)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetIntelligenceHigh(Pawn pawn)
        {
            SetInfiltratorIntelligence(pawn, 0.9f);
        }

        private static void SetInfiltratorIntelligence(Pawn pawn, float intelligenceStat)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity comp = pawn.GetComp<CompHiddenIdentity>();
            if (comp == null || comp.Hediff == null)
            {
                Messages.Message($"{pawn.NameShortColored} is not an infiltrator.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            comp.Hediff.intelligenceStat = intelligenceStat;
            string level = intelligenceStat < 0.5f ? "Low" : intelligenceStat < 0.8f ? "Medium" : "High";
            Messages.Message($"{pawn.NameShortColored} intelligence set to {level} ({intelligenceStat:F2})", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Intelligence", "Simulate Intel-Based Raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SimulateIntelBasedRaid()
        {
            // Get a hostile faction with intelligence
            var network = WorldComponent_IntelligenceNetwork.Instance;
            if (network == null)
            {
                Messages.Message("IntelligenceNetwork component not found!", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Faction hostileFaction = Find.FactionManager.AllFactionsVisible
                .Where(f => f.HostileTo(Faction.OfPlayer) && !f.def.hidden)
                .FirstOrDefault(f => network.GetFactionIntelligence(f).Count > 0);

            if (hostileFaction == null)
            {
                Messages.Message("No hostile faction has intelligence. Use 'Force Intel Transmission' first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var intel = network.GetFactionIntelligence(hostileFaction);
            Messages.Message($"Triggering raid from {hostileFaction.Name} with {intel.Count} intelligence reports...", MessageTypeDefOf.ThreatBig, false);

            // Trigger a raid incident
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.faction = hostileFaction;
            parms.forced = true;

            // The RaidModificationPatches will automatically apply intel-based modifications
            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
        }

        // ==================== COMPREHENSIVE TESTING ====================

        [DebugAction("Law & Order - Testing", "Create Full Test Scenario", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateFullTestScenario()
        {
            List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists.ToList();
            if (colonists.Count < 3)
            {
                Messages.Message("Need at least 3 colonists for full test scenario.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Criminal with multiple crimes
            Pawn criminal = colonists[0];
            Pawn victim1 = colonists[1];
            Pawn victim2 = colonists[2];

            // Create various crimes
            CrimeUtils.RecordCrime(criminal, CrimeType.Theft, null, null, 0f, "Test Scenario - Hidden");
            CrimeUtils.RecordCrime(criminal, CrimeType.Assault, victim1, null, 10f, "Test Scenario - Assault");
            CrimeUtils.RecordCrime(criminal, CrimeType.PropertyDestruction, null, null, 35f, "Test Scenario - Property");
            CrimeUtils.RecordCrime(criminal, CrimeType.Vandalism, null, null, 5f, "Test Scenario - Vandalism");

            // Spawn crime scene with clues near criminal
            IntVec3 crimeScene = criminal.Position + new IntVec3(3, 0, 3);
            if (crimeScene.InBounds(Find.CurrentMap))
            {
                for (int i = 0; i < 3; i++)
                {
                    IntVec3 cluePos = crimeScene + GenRadial.RadialPattern[i];
                    if (cluePos.InBounds(Find.CurrentMap) && !cluePos.Impassable(Find.CurrentMap))
                    {
                        ClueType clueType = i == 0 ? ClueType.BloodStain : i == 1 ? ClueType.Footprint : ClueType.FabricScrap;
                        ThingDef clueDef = GetClueThingDef(clueType);
                        if (clueDef != null)
                        {
                            CrimeSceneClue clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);
                            clue.clueType = clueType;
                            clue.linkedCriminal = criminal;
                            clue.clueQuality = 0.8f;
                            GenSpawn.Spawn(clue, cluePos, Find.CurrentMap);
                        }
                    }
                }
            }

            Messages.Message($"Created full test scenario:\n- Criminal: {criminal.NameShortColored} (4 crimes)\n- Crime scene with clues at {crimeScene}", MessageTypeDefOf.TaskCompletion, false);

            // Open Justice tab
            Find.MainTabsRoot.SetCurrentTab(MainTabDefOf.LawAndOrder_Justice, true);
        }

        [DebugAction("Law & Order - Testing", "Clear All Crimes & Cases", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearAllCrimes()
        {
            int crimeCount = 0;

            // Clear all crimes from all pawns
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
            {
                Hediff_Crimes crimeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(LawAndOrder_HediffDefOf.LawAndOrder_CriminalRecord) as Hediff_Crimes;
                if (crimeHediff != null)
                {
                    crimeCount += crimeHediff.TotalCrimeCount;
                    pawn.health.RemoveHediff(crimeHediff);
                }
            }

            // Reset case manager
            WorldComponent_JusticeManager manager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            int caseCount = manager.GetOpenCases().Count + manager.GetConvictedCases().Count;

            // Note: Can't directly clear allCases as it's private
            // Cases will auto-cleanup when associated crimes are removed

            Messages.Message($"Cleared {crimeCount} crimes from pawns", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Testing", "Log Crime Statistics", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogCrimeStatistics()
        {
            int totalCrimes = 0;
            int hiddenCrimes = 0;
            int suspectedCrimes = 0;
            int convictedCrimes = 0;

            foreach (Pawn pawn in Find.CurrentMap.mapPawns.AllPawns)
            {
                Hediff_Crimes crimeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(LawAndOrder_HediffDefOf.LawAndOrder_CriminalRecord) as Hediff_Crimes;
                if (crimeHediff != null)
                {
                    totalCrimes += crimeHediff.TotalCrimeCount;
                    hiddenCrimes += crimeHediff.GetHiddenCrimes().Count;
                    suspectedCrimes += crimeHediff.GetSuspectedCrimes().Count;
                    convictedCrimes += crimeHediff.GetConvictedCrimes().Count;
                }
            }

            WorldComponent_JusticeManager manager = Find.World.GetComponent<WorldComponent_JusticeManager>();

            Log.Message($"=== LAW AND ORDER STATISTICS ===");
            Log.Message($"Total Crimes: {totalCrimes}");
            Log.Message($"  - Hidden: {hiddenCrimes}");
            Log.Message($"  - Suspected: {suspectedCrimes}");
            Log.Message($"  - Convicted: {convictedCrimes}");
            Log.Message($"Open Cases: {manager.GetOpenCases().Count}");
            Log.Message($"Convicted Cases: {manager.GetConvictedCases().Count}");
            Log.Message($"Dismissed Cases: {manager.GetDismissedCases().Count}");

            int totalClues = Find.CurrentMap.listerThings.AllThings.Count(t => t is CrimeSceneClue);
            Log.Message($"Crime Scene Clues: {totalClues}");

            Messages.Message($"Logged crime statistics to console (Total: {totalCrimes})", MessageTypeDefOf.TaskCompletion, false);
        }

        // ==================== PHASE 6.6: ACCOMPLICE SYSTEM ====================

        [DebugAction("Law & Order - Accomplice", "Recruit Selected as Accomplice", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RecruitAsAccomplice(Pawn target)
        {
            if (target == null || target.Dead || !target.IsColonist)
            {
                Messages.Message("Invalid target. Select a living colonist.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Find an infiltrator on the map
            var infiltrators = Find.CurrentMap.mapPawns.FreeColonistsSpawned
                .Where(p => p.TryGetComp<CompHiddenIdentity>()?.HasHiddenIdentity == true)
                .ToList();

            if (infiltrators.Count == 0)
            {
                Messages.Message("No infiltrators on map. Create one first with 'Make Infiltrator'.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn infiltrator = infiltrators.First();

            // Force recruitment
            if (AccompliceUtils.TryRecruitAccomplice(infiltrator, target, out var comp))
            {
                Messages.Message($"{target.NameShortColored} recruited as accomplice by {infiltrator.NameShortColored} (Loyalty: {comp.LoyaltyToRecruiter:P0})", MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message($"Failed to recruit {target.NameShortColored}. Check max accomplices or vulnerability.", MessageTypeDefOf.RejectInput, false);
            }
        }

        [DebugAction("Law & Order - Accomplice", "Show Accomplices for Selected Infiltrator", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ShowAccomplices(Pawn infiltrator)
        {
            if (infiltrator == null || infiltrator.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = infiltrator.TryGetComp<CompHiddenIdentity>();
            if (comp == null || !comp.HasHiddenIdentity)
            {
                Messages.Message($"{infiltrator.NameShortColored} is not an infiltrator.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var accomplices = AccompliceUtils.GetAccomplicesForInfiltrator(infiltrator);

            if (accomplices.Count == 0)
            {
                Log.Message($"[Accomplice Debug] {infiltrator.LabelShort} has NO accomplices");
                Messages.Message($"{infiltrator.NameShortColored} has no accomplices.", MessageTypeDefOf.TaskCompletion, false);
                return;
            }

            Log.Message($"=== ACCOMPLICES for {infiltrator.LabelShort} ===");
            foreach (var accomplice in accomplices)
            {
                var accompliceComp = accomplice.TryGetComp<CompAccomplice>();
                if (accompliceComp != null)
                {
                    Log.Message($"  - {accomplice.LabelShort}:");
                    Log.Message($"      Loyalty: {accompliceComp.LoyaltyToRecruiter:P0}");
                    Log.Message($"      Days since recruitment: {accompliceComp.DaysSinceRecruitment}");
                    Log.Message($"      Discovered: {accompliceComp.Discovered}");
                    Log.Message($"      Pending sabotage tasks: {accompliceComp.GetPendingSabotage().Count}");
                }
            }

            Messages.Message($"{infiltrator.NameShortColored} has {accomplices.Count} accomplice(s). Check console for details.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Accomplice", "Trigger Sabotage (Raid Simulation)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TriggerSabotage()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Count accomplices who will sabotage
            int accompliceCount = 0;
            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited && comp.ShouldPerformSabotage())
                {
                    accompliceCount++;
                }
            }

            if (accompliceCount == 0)
            {
                Messages.Message("No active accomplices on map to sabotage. Recruit some first.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Log.Message($"[Sabotage Debug] Triggering sabotage for {accompliceCount} accomplice(s)");

            // Trigger sabotage
            SabotageExecutor.AssignSabotageForRaid(map);

            Messages.Message($"Sabotage triggered! {accompliceCount} accomplice(s) performing sabotage. Check console for details.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Accomplice", "Discover Selected Accomplice", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void DiscoverAccomplice(Pawn accomplice)
        {
            if (accomplice == null || accomplice.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = accomplice.TryGetComp<CompAccomplice>();
            if (comp == null || !comp.IsRecruited)
            {
                Messages.Message($"{accomplice.NameShortColored} is not an accomplice.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (comp.Discovered)
            {
                Messages.Message($"{accomplice.NameShortColored} is already discovered.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Discover the accomplice
            comp.DiscoverAccomplice();

            Messages.Message($"{accomplice.NameShortColored} discovered as accomplice! Check for dramatic letter.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Accomplice", "Set Accomplice Loyalty (High)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetAccompliceLoyaltyHigh(Pawn accomplice)
        {
            if (accomplice == null || accomplice.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = accomplice.TryGetComp<CompAccomplice>();
            if (comp == null || !comp.IsRecruited)
            {
                Messages.Message($"{accomplice.NameShortColored} is not an accomplice.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use reflection to set loyalty since it's a private field
            var loyaltyField = typeof(CompAccomplice).GetField("loyaltyToRecruiter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loyaltyField != null)
            {
                loyaltyField.SetValue(comp, 0.9f);
                Messages.Message($"{accomplice.NameShortColored} loyalty set to 90% (High)", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("Law & Order - Accomplice", "Set Accomplice Loyalty (Low)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetAccompliceLoyaltyLow(Pawn accomplice)
        {
            if (accomplice == null || accomplice.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var comp = accomplice.TryGetComp<CompAccomplice>();
            if (comp == null || !comp.IsRecruited)
            {
                Messages.Message($"{accomplice.NameShortColored} is not an accomplice.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use reflection to set loyalty
            var loyaltyField = typeof(CompAccomplice).GetField("loyaltyToRecruiter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loyaltyField != null)
            {
                loyaltyField.SetValue(comp, 0.15f);
                Messages.Message($"{accomplice.NameShortColored} loyalty set to 15% (Low, may refuse sabotage)", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        [DebugAction("Law & Order - Accomplice", "Show Vulnerability for Selected", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ShowVulnerability(Pawn colonist)
        {
            if (colonist == null || colonist.Dead || !colonist.IsColonist)
            {
                Messages.Message("Invalid pawn selected. Select a living colonist.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Find an infiltrator to test against
            var infiltrators = Find.CurrentMap.mapPawns.FreeColonistsSpawned
                .Where(p => p.TryGetComp<CompHiddenIdentity>()?.HasHiddenIdentity == true)
                .ToList();

            Pawn infiltrator = infiltrators.FirstOrDefault();

            float vulnerability = AccompliceUtils.CalculateVulnerability(colonist, infiltrator);

            Log.Message($"=== VULNERABILITY for {colonist.LabelShort} ===");
            Log.Message($"Total Vulnerability: {vulnerability:P0}");

            // Break down factors
            if (colonist.needs?.mood != null)
            {
                float moodLevel = colonist.needs.mood.CurLevelPercentage;
                Log.Message($"  Mood: {moodLevel:P0}" + (moodLevel < 0.3f ? " (+20% vulnerability)" : (moodLevel < 0.5f ? " (+10% vulnerability)" : "")));
            }

            if (colonist.story?.traits != null)
            {
                Log.Message($"  Traits:");
                var traits = colonist.story.traits.allTraits;
                foreach (var trait in traits)
                {
                    Log.Message($"    - {trait.Label}");
                }
            }

            if (infiltrator != null && colonist.relations != null)
            {
                float opinion = colonist.relations.OpinionOf(infiltrator);
                Log.Message($"  Opinion of infiltrator: {opinion}" + (opinion >= 20 ? " (+15% vulnerability)" : ""));
            }

            Messages.Message($"{colonist.NameShortColored} vulnerability: {vulnerability:P0}. Check console for breakdown.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Accomplice", "Create Test Scenario (Infiltrator + 2 Accomplices)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CreateAccompliceTestScenario()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var colonists = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Dead).ToList();
            if (colonists.Count < 3)
            {
                Messages.Message("Need at least 3 colonists for test scenario.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Pick random colonist to be infiltrator
            Pawn infiltrator = colonists.RandomElement();
            colonists.Remove(infiltrator);

            // Make them an infiltrator
            var hiddenComp = infiltrator.TryGetComp<CompHiddenIdentity>();
            if (hiddenComp == null)
            {
                hiddenComp = new CompHiddenIdentity();
                hiddenComp.parent = infiltrator;
                infiltrator.AllComps.Add(hiddenComp);
                hiddenComp.Initialize(new CompProperties_HiddenIdentity());
            }
            hiddenComp.InitializeHiddenIdentity(infiltrator);

            Log.Message($"=== ACCOMPLICE TEST SCENARIO ===");
            Log.Message($"Infiltrator: {infiltrator.LabelShort}");

            // Recruit 2 accomplices
            int recruited = 0;
            foreach (var target in colonists.InRandomOrder().Take(2))
            {
                if (AccompliceUtils.TryRecruitAccomplice(infiltrator, target, out var comp))
                {
                    recruited++;
                    Log.Message($"Accomplice {recruited}: {target.LabelShort} (Loyalty: {comp.LoyaltyToRecruiter:P0})");
                }
            }

            Messages.Message($"Test scenario created! Infiltrator: {infiltrator.NameShortColored}, Accomplices: {recruited}. Check console.", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Law & Order - Accomplice", "Log All Accomplices", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogAllAccomplices()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int totalAccomplices = 0;
            int discoveredAccomplices = 0;

            Log.Message($"=== ALL ACCOMPLICES ===");

            foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
            {
                var comp = pawn.TryGetComp<CompAccomplice>();
                if (comp != null && comp.IsRecruited)
                {
                    totalAccomplices++;
                    if (comp.Discovered)
                        discoveredAccomplices++;

                    Log.Message($"{pawn.LabelShort}:");
                    Log.Message($"  Recruiter: {comp.Recruiter?.LabelShort ?? "Unknown"}");
                    Log.Message($"  Loyalty: {comp.LoyaltyToRecruiter:P0}");
                    Log.Message($"  Days since recruitment: {comp.DaysSinceRecruitment}");
                    Log.Message($"  Discovered: {comp.Discovered}");
                    Log.Message($"  Pending sabotage: {comp.GetPendingSabotage().Count}");
                }
            }

            if (totalAccomplices == 0)
            {
                Log.Message("No accomplices found on map.");
            }
            else
            {
                Log.Message($"Total: {totalAccomplices} accomplice(s), {discoveredAccomplices} discovered");
            }

            Messages.Message($"Found {totalAccomplices} accomplice(s). Check console for details.", MessageTypeDefOf.TaskCompletion, false);
        }
    }
}
