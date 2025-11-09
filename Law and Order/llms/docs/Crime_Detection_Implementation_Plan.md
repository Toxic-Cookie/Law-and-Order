# Crime Detection Implementation Plan

**Created:** November 9, 2025
**Status:** Phase 1 Complete - In Progress

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
| **Kidnapping** | ⏳ To Implement | See Phase 2 below | High |
| **Trespassing** | ⏳ To Implement | See Phase 3 below | Medium |
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

## Phase 2: Kidnapping Detection

### Objective
Detect when hostile pawns capture/carry away colonists.

### Implementation

**Patch Target:** `Pawn_CarryTracker.TryStartCarry` (similar to theft patch)

**Logic:**
```csharp
[HarmonyPatch(typeof(Pawn_CarryTracker), "TryStartCarry", new Type[] { typeof(Thing) })]
public static class TrackKidnapping_Patch
{
    static void Postfix(Pawn_CarryTracker __instance, Thing item, bool __result)
    {
        if (!__result) return;

        Pawn carrier = GetPawnFromCarryTracker(__instance);
        Pawn victim = item as Pawn;

        if (carrier == null || victim == null) return;

        // Only track if:
        // 1. Carrier is hostile to player
        // 2. Victim is a colonist (or player-owned pawn)
        // 3. Victim is downed or unconscious

        if (!carrier.HostileTo(Faction.OfPlayer)) return;
        if (!victim.IsColonist && victim.Faction != Faction.OfPlayer) return;
        if (!victim.Downed) return;

        CrimeUtils.RecordCrime(
            criminal: carrier,
            crimeType: CrimeType.Kidnapping,
            victim: victim,
            additionalInfo: $"Attempted to kidnap {victim.NameShortColored}"
        );
    }
}
```

**Alternative Patch:** `GenGuest.TrySetAsGuestOfHostileFaction` - catches actual capture

**Key Considerations:**
- Distinguish from "rescue" (friendly faction)
- Only record when hostile faction carries downed colonist
- May want to track "attempted kidnapping" vs "successful kidnapping"
- Consider recording when raider leaves map with colonist

**Penalty Calculation:**
- High severity crime (similar to assault/murder)
- Should factor in victim's importance (skills, social standing)
- Could use VictimNobility multiplier if victim is noble

**Files to Modify:**
- `Source/CrimeDetection/CrimeDetectionPatches.cs` - Add kidnapping patch

**Testing:**
- Down a colonist and let raiders try to carry them away
- Verify Kidnapping crime appears
- Test penalty calculation

---

## Phase 3: Trespassing Detection

### Objective
Detect when hostile pawns enter the player's designated Home Area.

### Implementation

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

**Files to Create/Modify:**
- `Source/Components/MapComponent_TrespassingTracker.cs` - New file
- `Defs/MapComponentDefs/MapComponents.xml` - Add def for auto-spawn

**Testing:**
- Let raiders enter home area
- Verify Trespassing crime appears once per raider
- Check no spam when moving around in home area

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

### Phase 2: Kidnapping
- **Priority:** HIGH
- **Complexity:** Medium (need to identify correct patch point)
- **Effort:** 3-5 hours
- **Files:** 1 file (CrimeDetectionPatches.cs)

**Tasks:**
1. Research best patch point (TryStartCarry vs GenGuest methods)
2. Add `TrackKidnapping_Patch` class
3. Implement victim validation
4. Test with downed colonists
5. Verify penalties are calculated correctly
6. Update documentation

**Why Second:** High priority crime, clear scope, important for gameplay

---

### Phase 3: Trespassing
- **Priority:** MEDIUM
- **Complexity:** Low-Medium (uses existing home area)
- **Effort:** 3-4 hours
- **Files:** 2 files (new MapComponent + XML def)

**Tasks:**
1. Create `MapComponent_TrespassingTracker.cs` with home area checking
2. Add MapComponent def to XML for auto-spawning
3. Implement once-per-raid tracking with HashSet
4. Test with raiders entering home area
5. Verify no spam and proper cleanup
6. Update documentation

**Why Third:** Now simple with home area approach, good middle-priority crime

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

### Phase 2 (Kidnapping):
- ✅ Hostile carrying downed colonist = Kidnapping crime
- ✅ Only records for player-owned pawns
- ✅ High severity penalty applied
- ✅ No false positives (rescue, friendly factions)

### Phase 3 (Trespassing):
- ✅ Uses RimWorld's built-in home area
- ✅ Records once per pawn per raid
- ✅ Efficient periodic checking (every 60 ticks)
- ✅ No spam during normal raids
- ✅ Proper cleanup of dead/despawned pawns

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
