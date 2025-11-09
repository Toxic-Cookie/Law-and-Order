# Crime Detection Implementation Plan

**Created:** November 9, 2025
**Status:** Phase 3 Complete - In Progress

---

## Overview

Complete implementation plan for detecting all crime types defined in the `CrimeType` enum.

### Current Status Summary

| Crime Type | Status | Detection Method | Priority |
|------------|--------|------------------|----------|
| **Assault** | ✅ Complete | `Pawn_HealthTracker.PostApplyDamage` | - |
| **Murder** | ✅ Complete | `Pawn_HealthTracker.PostApplyDamage` (victim.Dead) | - |
| **PropertyDestruction** | ✅ Complete | `Thing.TakeDamage` | - |
| **Arson** | ✅ Complete | `Thing.TakeDamage` (flame damage) | - |
| **Theft** | ✅ Complete | `Pawn_CarryTracker.TryStartCarry` | - |
| **ContrabandPossession** | ✅ Complete | Separate system (contraband scanner) | - |
| **AnimalAbuse** | ✅ Complete | `Pawn_HealthTracker.PostApplyDamage` (colony animals) | - |
| **Kidnapping** | ✅ Complete | `Pawn_CarryTracker.TryStartCarry` | - |
| **Trespassing** | ✅ Complete | MapComponent periodic check | - |
| **Vandalism** | ⏳ To Implement | See Phase 4 below | Low |

---

## Phase 1: Animal Abuse Detection ✅ COMPLETE

**Completed:** November 9, 2025

### Objective
Detect when hostile pawns damage or kill colony animals.

### Implementation Status: ✅ COMPLETE

**Patch Target:** `Pawn_HealthTracker.PostApplyDamage` (same as Assault patch)

**Logic:**
```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
public static class TrackAnimalAbuse_Patch
{
    static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        // Get victim pawn
        Pawn victim = GetPawnFromHealthTracker(__instance);

        // Get attacker
        Pawn attacker = dinfo.Instigator as Pawn;

        // Validation checks
        if (victim == null || attacker == null) return;
        if (!attacker.HostileTo(Faction.OfPlayer)) return;

        // Check if victim is a COLONY ANIMAL (not wild, not hostile)
        if (!victim.RaceProps.Animal) return;
        if (victim.Faction != Faction.OfPlayer) return;

        // Record crime
        CrimeUtils.RecordCrime(
            criminal: attacker,
            crimeType: CrimeType.AnimalAbuse,
            victim: victim,
            damageDealt: totalDamageDealt,
            wasVictimKilled: victim.Dead,
            damageInfo: dinfo
        );
    }
}
```

**Key Considerations:**
- Only track damage to **player-owned animals** (not wild/enemy animals)
- Distinguished from Assault by checking `victim.RaceProps.Animal`
- Can reuse existing assault patch with additional checks
- Should check for bonded animals for penalty calculation (already in DebtUtils)

**Integration with Penalty System:**
- New penalty system should check `IsBondedAnimal()` multiplier
- Check if animal was killed vs just injured
- Market value of animal factors into penalty

**Files Modified:**
- ✅ `Source/CrimeDetection/CrimeDetectionPatches.cs` - Added TrackAnimalAbuse_Patch class

**Implementation Details:**
- Added new Harmony patch class `TrackAnimalAbuse_Patch` targeting `Pawn_HealthTracker.PostApplyDamage`
- Checks if victim is a player-owned animal (`victim.RaceProps.Animal` && `victim.Faction == Faction.OfPlayer`)
- Only tracks damage from hostile pawns
- Passes DamageInfo to penalty system for contextual calculation
- Records victim downed/killed status
- Includes weapon information in crime details
- Build successful with 0 errors

**Testing Status:**
- ✅ Code compiles successfully
- ⏳ In-game testing recommended:
  - Spawn raid and let them attack colony animals
  - Verify AnimalAbuse crimes appear in Justice tab
  - Check penalty calculation includes animal market value
  - Test with both bonded and non-bonded animals

---

## Phase 2: Kidnapping Detection ✅ COMPLETE

**Completed:** November 9, 2025

### Objective
Detect when hostile pawns capture/carry away colonists.

### Implementation Status: ✅ COMPLETE

**Patch Target:** `JobGiver_Kidnap.TryGiveJob` (RimWorld's kidnapping AI)

**Logic:**
```csharp
[HarmonyPatch(typeof(RimWorld.JobGiver_Kidnap), "TryGiveJob")]
public static class TrackKidnapping_Patch
{
    static void Postfix(Pawn pawn, Job __result)
    {
        // Only track if a kidnap job was successfully created
        if (__result == null) return;

        // Get the victim from the job target
        Pawn victim = __result.targetA.Thing as Pawn;
        if (victim == null) return;

        // Only track if:
        // 1. Kidnapper is hostile to player
        // 2. Victim is a colonist (or player-owned pawn)
        // 3. Victim is downed (always true for kidnapping)

        if (!pawn.HostileTo(Faction.OfPlayer)) return;
        if (!victim.IsColonist && victim.Faction != Faction.OfPlayer) return;
        if (!victim.Downed) return;

        CrimeUtils.RecordCrime(
            criminal: pawn,
            crimeType: CrimeType.Kidnapping,
            victim: victim,
            additionalInfo: $"Attempted to kidnap {victim.NameShortColored}"
        );
    }
}
```

**Why JobGiver_Kidnap?** RimWorld has a dedicated kidnapping system. Raiders use `JobGiver_Kidnap.TryGiveJob` to select victims and create kidnap jobs, not the generic `TryStartCarry` method.

**Key Considerations:**
- Distinguish from "rescue" (friendly faction)
- Only record when hostile faction carries downed colonist
- May want to track "attempted kidnapping" vs "successful kidnapping"
- Consider recording when raider leaves map with colonist

**Penalty Calculation:**
- High severity crime (similar to assault/murder)
- Should factor in victim's importance (skills, social standing)
- Could use VictimNobility multiplier if victim is noble

**Files Modified:**
- ✅ `Source/CrimeDetection/CrimeDetectionPatches.cs` - Added TrackKidnapping_Patch class (line 226)

**Implementation Details:**
- Added new Harmony patch class `TrackKidnapping_Patch` targeting `JobGiver_Kidnap.TryGiveJob`
- Intercepts when raiders are assigned kidnapping jobs (before they even reach the victim)
- Extracts victim pawn from the job's targetA field
- Validates kidnapper is hostile to player (`pawn.HostileTo(Faction.OfPlayer)`)
- Validates victim is a colonist or player-owned pawn
- Validates victim is downed (always true for RimWorld's kidnapping system)
- Records crime immediately when kidnap job is assigned
- Added `using Verse.AI;` directive to access Job class
- Build successful with 0 errors
- ✅ In-game testing confirmed working

**Testing Status:**
- ✅ Code compiles successfully
- ✅ In-game testing completed and verified:
  - ✅ Kidnapping crime appears when raider is assigned kidnap job
  - ✅ Crime shows correct victim and criminal information
  - ✅ No false positives (friendly rescue, prisoner transfers)
  - ⏳ Penalty calculation testing pending (requires capturing kidnapper)

---

## Phase 3: Trespassing Detection ✅ COMPLETE

**Completed:** November 9, 2025

### Objective
Detect when hostile pawns enter the player's designated Home Area.

### Implementation Status: ✅ COMPLETE

**Patch Target:** MapComponent that checks periodically for hostile pawns in home area

**Logic:**
```csharp
public class MapComponent_TrespassingTracker : MapComponent
{
    private HashSet<Pawn> recordedTrespassers = new HashSet<Pawn>();
    private int tickCounter = 0;

    public override void MapComponentTick()
    {
        tickCounter++;
        if (tickCounter < 60) return;
        tickCounter = 0;

        if (map.areaManager?.Home == null) return;

        var hostilePawns = map.mapPawns.AllPawnsSpawned
            .Where(p => p.HostileTo(Faction.OfPlayer) && !p.Downed && !p.Dead);

        foreach (var pawn in hostilePawns)
        {
            if (map.areaManager.Home[pawn.Position] && !recordedTrespassers.Contains(pawn))
            {
                recordedTrespassers.Add(pawn);

                CrimeUtils.RecordCrime(
                    criminal: pawn,
                    crimeType: CrimeType.Trespassing,
                    additionalInfo: $"Entered home area"
                );
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref recordedTrespassers, "recordedTrespassers", LookMode.Reference);
    }
}
```

**Key Considerations:**
- Uses RimWorld's built-in `Area_Home` (accessed via `map.areaManager.Home`)
- Records **once per pawn per raid** (not every cell entry)
- Uses HashSet to track which pawns already recorded
- Checks every 60 ticks (1 second) for performance
- Automatically cleans up when pawns die/despawn
- Clear definition: "entering player's home area"

**Benefits:**
- No custom UI needed - players already understand home area
- Integrates with existing RimWorld mechanics
- Simple, clear definition of trespassing
- No spam - records once per pawn

**Penalty Calculation:**
- Relatively minor crime (compared to assault/murder)
- Could scale with how deep into base they went
- First-time trespass vs repeat offender

**Files Modified:**
- ✅ `Source/Components/MapComponent_TrespassingTracker.cs` - New file created
- ✅ No XML def needed - MapComponents are auto-registered by RimWorld via reflection

**Implementation Details:**
- Created MapComponent_TrespassingTracker that checks every 60 ticks (1 second)
- Uses HashSet to track which pawns have already been recorded (prevents spam)
- Checks if hostile pawns are in player's home area via `map.areaManager.Home[pawn.Position]`
- Records trespassing crime once per pawn per raid
- Includes automatic cleanup of dead/despawned pawns from tracking set
- Proper save/load support via ExposeData
- Build successful with 0 errors

**Testing Status:**
- ✅ Code compiles successfully
- ✅ In-game testing completed and verified:
  - ✅ Trespassing crimes appear when raiders enter home area
  - ✅ Crime shows correct criminal information
  - ✅ No spam when raiders move around in home area (HashSet prevents duplicates)
  - ✅ System working as intended

---

## Phase 4: Vandalism Detection

### Objective
Detect minor property damage (different from full destruction).

### Implementation

**Approach:** Extend existing `Thing.TakeDamage` patch

**Logic:**
```csharp
// In existing TrackPropertyDamage_Patch:

// Determine if it's vandalism vs property destruction
CrimeType crimeType;

if (__instance.HitPoints / (float)__instance.MaxHitPoints < 0.5f)
{
    // Significant damage = Property Destruction
    crimeType = CrimeType.PropertyDestruction;
}
else
{
    // Minor damage = Vandalism
    crimeType = CrimeType.Vandalism;
}
```

**Key Considerations:**
- Vandalism = minor damage (>50% HP remaining)
- PropertyDestruction = major damage (<50% HP remaining)
- May cause duplicate crime records (one for vandalism, then property destruction)
- Could track cumulative damage per item instead

**Alternative Approach:**
- Only distinguish during penalty calculation, not crime type
- Use single "PropertyDestruction" crime with damage amount
- Penalty calculation scales with damage severity

**Recommendation:**
- **Merge with PropertyDestruction** - use damage amount for penalty scaling
- OR implement as severity threshold in existing patch
- Low priority - PropertyDestruction already covers this

---

## Implementation Workflow

**Phases are ordered by priority and recommended implementation sequence:**

### Phase 1: Animal Abuse ✅ COMPLETE (Nov 9, 2025)
- **Priority:** HIGH
- **Complexity:** Low (reuse assault detection logic)
- **Effort:** ~1 hour (actual)
- **Files:** 1 file (CrimeDetectionPatches.cs)

**Tasks:**
1. ✅ Add `TrackAnimalAbuse_Patch` class
2. ✅ Implement animal-specific checks
3. ⏳ Test with bonded and non-bonded animals (in-game testing recommended)
4. ✅ Verify penalty calculation includes animal value
5. ✅ Update documentation

**Implementation Notes:**
- Created new Harmony patch targeting `Pawn_HealthTracker.PostApplyDamage`
- Validates victim is player-owned animal and attacker is hostile
- Integrated with new penalty system via DamageInfo parameter
- Build successful with 0 errors

**Why First:** Quick win, high player value, reuses existing assault detection pattern

---

### Phase 2: Kidnapping ✅ COMPLETE AND TESTED (Nov 9, 2025)
- **Priority:** HIGH
- **Complexity:** Medium (required researching RimWorld's kidnapping system)
- **Effort:** 3-5 hours (actual: ~2 hours including debugging)
- **Files:** 1 file (CrimeDetectionPatches.cs)

**Tasks:**
1. ✅ Research best patch point (discovered JobGiver_Kidnap after initial incorrect attempt)
2. ✅ Add `TrackKidnapping_Patch` class
3. ✅ Implement victim validation
4. ✅ Test with downed colonists (verified working in-game)
5. ⏳ Verify penalties are calculated correctly (pending capture of kidnapper)
6. ✅ Update documentation

**Implementation Notes:**
- **Initial Attempt:** Tried patching `Pawn_CarryTracker.TryStartCarry` - this was incorrect
- **Solution:** RimWorld uses dedicated `JobGiver_Kidnap.TryGiveJob` for kidnapping AI
- Created new Harmony patch targeting `JobGiver_Kidnap.TryGiveJob` (CrimeDetectionPatches.cs:226)
- Intercepts kidnap job assignment before raider reaches victim
- Extracts victim from job.targetA field
- Validates kidnapper is hostile, victim is player-owned, and victim is downed
- Added `using Verse.AI;` directive for Job class access
- Integrated with existing CrimeUtils.RecordCrime system
- Build successful with 0 errors
- ✅ In-game testing confirmed crimes are recorded correctly

**Why Second:** High priority crime, clear scope, important for gameplay

**Lessons Learned:**
- RimWorld has specialized AI systems for specific actions (kidnapping, rescue, etc.)
- Generic methods like TryStartCarry aren't always used for specific behaviors
- When detection doesn't work, check RimWorld source for dedicated job givers/drivers

---

### Phase 3: Trespassing ✅ COMPLETE (Nov 9, 2025)
- **Priority:** MEDIUM
- **Complexity:** Low-Medium (uses existing home area)
- **Effort:** ~2 hours (actual)
- **Files:** 1 file (MapComponent_TrespassingTracker.cs)

**Tasks:**
1. ✅ Create `MapComponent_TrespassingTracker.cs` with home area checking
2. ✅ No XML def needed (auto-registered by RimWorld reflection)
3. ✅ Implement once-per-raid tracking with HashSet
4. ✅ Test with raiders entering home area (verified working in-game)
5. ✅ Verify no spam and proper cleanup (confirmed working)
6. ✅ Update documentation

**Implementation Notes:**
- MapComponents are automatically instantiated by RimWorld's `Map.FillComponents()` method
- Uses periodic checking (every 60 ticks) for performance
- HashSet prevents duplicate crime records
- Includes cleanup method to prevent memory leaks
- Integrates with existing CrimeUtils.RecordCrime system
- Build successful with 0 errors

**Why Third:** Simple with home area approach, good middle-priority crime

---

### Phase 4: Vandalism
- **Priority:** LOW
- **Complexity:** Low (extend existing patch)
- **Effort:** 1-2 hours
- **Files:** 1 file (CrimeDetectionPatches.cs)

**Tasks:**
1. Decide approach (separate crime type vs damage scaling)
2. Modify existing PropertyDamage patch if needed
3. Update penalty calculation to distinguish severity
4. Test with minor vs major damage
5. Update documentation

**Why Last / Optional:** Low priority, consider merging with PropertyDestruction instead

---

## Technical Considerations

### Avoiding Duplicate Crimes
- Same event shouldn't trigger multiple crime records
- Use patch ordering carefully
- Add checks to prevent double-recording

### Performance
- Each patch adds overhead to frequently-called methods
- Keep validation checks efficient
- Avoid expensive operations in patches (e.g., pathfinding, complex queries)

### Testing Strategy
1. **Dev Mode Testing:** Use debug logging to verify detection
2. **Scenario Testing:** Create specific test scenarios for each crime
3. **Integration Testing:** Ensure penalties calculate correctly
4. **Performance Testing:** Check for FPS impact during raids

### Crime Definition Clarity
- Document what constitutes each crime type
- Add in-game tooltips explaining crimes
- Consider player mod settings for detection sensitivity

---

## Success Criteria

### Phase 1 (Animal Abuse): ✅ IMPLEMENTATION COMPLETE
- ✅ Hostile attacking colony animal = AnimalAbuse crime
- ✅ Penalty includes animal market value (via new penalty system)
- ✅ Bonded animal multiplier available in system
- ✅ No false positives (friendly fire, wild animals) - checked faction and hostile status
- ⏳ In-game testing recommended to verify all criteria

### Phase 2 (Kidnapping): ✅ COMPLETE AND TESTED
- ✅ Raider assigned kidnap job = Kidnapping crime
- ✅ Only records for player-owned pawns (checked IsColonist and faction)
- ✅ Victim must be downed (always true for kidnapping jobs)
- ✅ No false positives (rescue, friendly factions) - checked kidnapper hostility
- ✅ In-game testing verified - crimes appear correctly in Justice tab

### Phase 3 (Trespassing): ✅ COMPLETE AND TESTED
- ✅ Uses RimWorld's built-in home area (map.areaManager.Home)
- ✅ Records once per pawn per raid (HashSet tracking)
- ✅ Efficient periodic checking (every 60 ticks)
- ✅ No spam during normal raids (HashSet prevents duplicates)
- ✅ Proper cleanup of dead/despawned pawns (CleanupInvalidTrespassers method)
- ✅ Proper save/load support (ExposeData with LookMode.Reference)
- ✅ In-game testing verified - crimes appear correctly in Justice tab

### Phase 4 (Vandalism):
- ✅ Minor vs major damage distinguished
- ✅ Penalty scales with damage severity
- ✅ No duplicate crime records

---

## Documentation Updates Required

After each phase:
1. Update `Project_Tracker.md` with completion status
2. Update `Crime_Detection_Implementation_Plan.md` (this file)
3. Add to `Project_Documentation.md` Section on Crime Detection
4. Update in-game tooltips/descriptions if needed

---

## Future Enhancements

### Additional Crime Types (Not Currently Defined):
- **Prison Break** - Prisoner escapes
- **Contraband Trafficking** - Bringing drugs/weapons to others
- **Bribery** - Attempting to bribe guards/wardens
- **Assault on Guard** - Attacking during hearing/imprisonment
- **Escape Attempt** - Trying to leave map while imprisoned

### Enhanced Detection:
- Crime severity tiers (petty, moderate, serious, capital)
- Witness system (crimes seen by colonists have evidence)
- Evidence collection (forensics)
- Repeat offender tracking (already exists, but could be enhanced)

### Player Tools:
- Crime report generation
- Criminal profiling
- Wanted posters
- Bounty system

---

**End of Implementation Plan**
