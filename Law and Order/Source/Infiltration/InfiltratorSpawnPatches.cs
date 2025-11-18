using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// Patches to inject infiltrators into normal visitor/wanderer events
    /// </summary>
    [HarmonyPatch]
    public static class InfiltratorSpawnPatches
    {
        // Chance for an infiltrator to spawn in various events
        private const float InfiltratorChanceInVisitorGroup = 0.15f; // 15% chance in visitor groups
        private const float InfiltratorChanceInWandererJoin = 0.10f; // 10% chance for wanderer joins
        private const float InfiltratorChanceInTraderCaravan = 0.08f; // 8% chance in trade caravans

        // Debug flags to force next spawn to be an infiltrator
        public static bool forceNextVisitorGroupInfiltrator = false;
        public static bool forceNextWandererInfiltrator = false;

        /// <summary>
        /// Patch VisitorGroup to occasionally include an infiltrator
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_VisitorGroup), "TryExecuteWorker")]
        [HarmonyPostfix]
        public static void VisitorGroup_TryExecuteWorker_Postfix(bool __result, IncidentParms parms)
        {
            if (!__result) return; // Incident failed

            try
            {
                // Check debug flag or roll chance for infiltrator
                bool shouldSpawnInfiltrator = forceNextVisitorGroupInfiltrator || Rand.Chance(InfiltratorChanceInVisitorGroup);
                forceNextVisitorGroupInfiltrator = false; // Reset flag

                if (!shouldSpawnInfiltrator)
                    return;

                Map map = (Map)parms.target;
                if (map == null) return;

                // Get a hostile faction (not the visitor faction)
                Faction hostileFaction = Find.FactionManager.AllFactionsVisible
                    .Where(f => f.HostileTo(Faction.OfPlayer) && f != parms.faction && !f.def.hidden && f.def.humanlikeFaction)
                    .RandomElementWithFallback();

                if (hostileFaction == null) return;

                // Find the visitor group lord that was just created
                var visitorLord = map.lordManager.lords
                    .Where(l => l.faction == parms.faction && l.CurLordToil != null)
                    .OrderByDescending(l => l.loadID)
                    .FirstOrDefault();

                if (visitorLord == null)
                {
                    ModLog.Warning("Could not find visitor lord to add infiltrator to");
                    return;
                }

                // Create infiltrator disguised as part of the visitor group
                Pawn infiltrator = CreateInfiltrator(parms.faction, hostileFaction, map, visitorLord);
                if (infiltrator != null)
                {
                    ModLog.Info($"Infiltrator from {hostileFaction.Name} joined visitor group from {parms.faction.Name}");
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in VisitorGroup_TryExecuteWorker_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Patch WandererJoin to occasionally be an infiltrator
        /// </summary>
        [HarmonyPatch(typeof(IncidentWorker_WandererJoin), "TryExecuteWorker")]
        [HarmonyPrefix]
        public static bool WandererJoin_TryExecuteWorker_Prefix(IncidentWorker_WandererJoin __instance, ref bool __result, IncidentParms parms)
        {
            try
            {
                // Check debug flag or roll chance for infiltrator
                bool shouldSpawnInfiltrator = forceNextWandererInfiltrator || Rand.Chance(InfiltratorChanceInWandererJoin);
                forceNextWandererInfiltrator = false; // Reset flag

                if (!shouldSpawnInfiltrator)
                    return true; // Let vanilla handle it

                Map map = (Map)parms.target;
                if (map == null) return true;

                // Get a hostile faction
                Faction hostileFaction = Find.FactionManager.AllFactionsVisible
                    .Where(f => f.HostileTo(Faction.OfPlayer) && !f.def.hidden && f.def.humanlikeFaction)
                    .RandomElementWithFallback();

                if (hostileFaction == null) return true;

                // Create infiltrator wanderer
                Pawn infiltrator = CreateWandererInfiltrator(hostileFaction, map);
                if (infiltrator != null)
                {
                    // Spawn using vanilla method
                    if (__instance is IncidentWorker_WandererJoin worker)
                    {
                        worker.SpawnJoiner(map, infiltrator);
                    }
                    else
                    {
                        // Fallback spawn
                        IntVec3 loc;
                        if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out loc))
                        {
                            GenSpawn.Spawn(infiltrator, loc, map);
                        }
                    }

                    // Send letter
                    TaggedString letterText = "WandererJoin".Translate(infiltrator.Named("PAWN")).AdjustedFor(infiltrator, "PAWN", true);
                    TaggedString letterLabel = "LetterLabelWandererJoin".Translate(infiltrator.Named("PAWN")).AdjustedFor(infiltrator, "PAWN", true);
                    PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref letterLabel, infiltrator);
                    Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, infiltrator);

                    ModLog.Info($"Infiltrator from {hostileFaction.Name} joined as wanderer");

                    __result = true;
                    return false; // Skip vanilla execution
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error in WandererJoin_TryExecuteWorker_Prefix: {ex}");
            }

            return true; // Let vanilla handle it
        }

        /// <summary>
        /// Create an infiltrator that blends into a visitor group
        /// </summary>
        private static Pawn CreateInfiltrator(Faction coverFaction, Faction realFaction, Map map, Verse.AI.Group.Lord visitorLord)
        {
            try
            {
                // Generate pawn that looks like they're from the cover faction
                PawnKindDef pawnKind = coverFaction.def.pawnGroupMakers
                    .SelectMany(pgm => pgm.options)
                    .Where(opt => opt.kind.RaceProps.Humanlike)
                    .RandomElementWithFallback()?.kind ?? PawnKindDefOf.Villager;

                Pawn infiltrator = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    pawnKind,
                    coverFaction, // Appears to be from cover faction
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    relationWithExtraPawnChanceFactor: 0f
                ));

                // Store original name before we change it
                string originalName = infiltrator.Name.ToStringShort;

                // Add CompHiddenIdentity to ThingDef if not present
                if (!infiltrator.def.comps.Any(cp => cp is CompProperties_HiddenIdentity))
                {
                    CompProperties_HiddenIdentity compProps = new CompProperties_HiddenIdentity();
                    infiltrator.def.comps.Add(compProps);
                }

                // Find a good spawn position near the other visitors
                IntVec3 spawnPos;
                var otherVisitors = visitorLord.ownedPawns;
                if (otherVisitors != null && otherVisitors.Any())
                {
                    // Spawn near the other visitors
                    Pawn nearPawn = otherVisitors.First();
                    spawnPos = CellFinder.RandomClosewalkCellNear(nearPawn.Position, map, 3);
                }
                else
                {
                    // Fallback to map edge
                    if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out spawnPos))
                    {
                        spawnPos = CellFinder.RandomClosewalkCellNear(map.Center, map, 20);
                    }
                }

                // Spawn the infiltrator
                GenSpawn.Spawn(infiltrator, spawnPos, map);

                // Initialize CompHiddenIdentity AFTER spawning
                CompHiddenIdentity comp = infiltrator.GetComp<CompHiddenIdentity>();
                if (comp == null)
                {
                    comp = new CompHiddenIdentity();
                    comp.parent = infiltrator;
                    var compPropsInDef = infiltrator.def.comps.OfType<CompProperties_HiddenIdentity>().FirstOrDefault();
                    if (compPropsInDef != null)
                    {
                        comp.Initialize(compPropsInDef);
                    }
                    infiltrator.AllComps.Add(comp);
                }

                // Initialize hidden identity (changes name and creates hediff)
                comp.InitializeHiddenIdentity(infiltrator);

                // Store real faction in the hediff
                var hediff = comp.Hediff;
                if (hediff != null)
                {
                    hediff.realFaction = realFaction;

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Created infiltrator: Original={originalName}, Fake={infiltrator.Name.ToStringShort}, RealFaction={realFaction.Name}, CoverFaction={coverFaction.Name}, Intel={hediff.intelligenceStat:F2}");
                    }
                }
                else
                {
                    ModLog.Error($"Failed to create hediff for infiltrator {infiltrator.LabelShort}");
                }

                // Add to visitor group lord so they behave like visitors
                visitorLord.AddPawn(infiltrator);

                // Set as guest
                infiltrator.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);

                return infiltrator;
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error creating infiltrator: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Create a wanderer infiltrator that joins immediately
        /// </summary>
        private static Pawn CreateWandererInfiltrator(Faction realFaction, Map map)
        {
            try
            {
                // Generate as player faction (they're joining)
                Pawn infiltrator = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Villager,
                    Faction.OfPlayer, // Joins as colonist
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false,
                    relationWithExtraPawnChanceFactor: 0f
                ));

                // Store original name before we change it
                string originalName = infiltrator.Name.ToStringShort;

                // Add CompHiddenIdentity to ThingDef if not present
                if (!infiltrator.def.comps.Any(cp => cp is CompProperties_HiddenIdentity))
                {
                    CompProperties_HiddenIdentity compProps = new CompProperties_HiddenIdentity();
                    infiltrator.def.comps.Add(compProps);
                }

                // Initialize CompHiddenIdentity (must be done BEFORE spawning for wanderers)
                CompHiddenIdentity comp = infiltrator.GetComp<CompHiddenIdentity>();
                if (comp == null)
                {
                    comp = new CompHiddenIdentity();
                    comp.parent = infiltrator;
                    var compPropsInDef = infiltrator.def.comps.OfType<CompProperties_HiddenIdentity>().FirstOrDefault();
                    if (compPropsInDef != null)
                    {
                        comp.Initialize(compPropsInDef);
                    }
                    infiltrator.AllComps.Add(comp);
                }

                // Initialize hidden identity (changes name and creates hediff)
                comp.InitializeHiddenIdentity(infiltrator);

                // Store real faction in the hediff
                var hediff = comp.Hediff;
                if (hediff != null)
                {
                    hediff.realFaction = realFaction;

                    if (Prefs.DevMode)
                    {
                        ModLog.Debug($"Created wanderer infiltrator: Original={originalName}, Fake={infiltrator.Name.ToStringShort}, RealFaction={realFaction.Name}, Intel={hediff.intelligenceStat:F2}");
                    }
                }
                else
                {
                    ModLog.Error($"Failed to create hediff for wanderer infiltrator {infiltrator.LabelShort}");
                }

                return infiltrator;
            }
            catch (System.Exception ex)
            {
                ModLog.Error($"Error creating wanderer infiltrator: {ex}");
                return null;
            }
        }
    }
}
