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

        [DebugAction("Law & Order - Infiltration", "Add Hidden Identity", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddHiddenIdentity(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity hiddenIdentity = pawn.GetComp<CompHiddenIdentity>();
            if (hiddenIdentity != null && hiddenIdentity.Identity != null)
            {
                Messages.Message($"{pawn.NameShortColored} already has a hidden identity.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Note: CompHiddenIdentity needs to be added via XML def, not at runtime
            Messages.Message($"To add hidden identity, add CompHiddenIdentity to pawn's ThingDef", MessageTypeDefOf.RejectInput, false);
        }

        [DebugAction("Law & Order - Infiltration", "Reveal Identity (25%)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RevealIdentityPartial(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                Messages.Message("Invalid pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            CompHiddenIdentity hiddenIdentity = pawn.GetComp<CompHiddenIdentity>();
            if (hiddenIdentity == null || hiddenIdentity.Identity == null)
            {
                Messages.Message($"{pawn.NameShortColored} has no hidden identity.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use ProgressDiscovery which handles revelation automatically
            hiddenIdentity.ProgressDiscovery(0.25f, "Debug action");
            Messages.Message($"Added 25% discovery progress to {pawn.NameShortColored} (now {hiddenIdentity.Identity.discoveryProgress:P0})", MessageTypeDefOf.TaskCompletion, false);

            if (hiddenIdentity.Identity.identityRevealed)
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

            CompHiddenIdentity hiddenIdentity = pawn.GetComp<CompHiddenIdentity>();
            if (hiddenIdentity == null || hiddenIdentity.Identity == null)
            {
                Messages.Message($"{pawn.NameShortColored} has no hidden identity.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Use ProgressDiscovery to trigger full revelation
            hiddenIdentity.ProgressDiscovery(1.0f, "Debug action - full reveal");
            Messages.Message($"Fully revealed identity of {pawn.NameShortColored}", MessageTypeDefOf.TaskCompletion, false);
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
    }
}
