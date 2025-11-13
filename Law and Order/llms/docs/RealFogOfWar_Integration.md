# Real Fog of War Integration

## Design Decision: Required Dependency

**Date**: 2025-01-13

**Decision**: Made **(NWN) Real Fog of War (Continued)** a required dependency for Law and Order.

**Rationale**: Solves the fundamental "player omniscience" problem that would break Dwarf Fortress-style justice mechanics.

## The Problem

RimWorld's default visibility model creates a design conflict with DF-style justice:

### Without Fog of War:
- Player sees everything on the map at all times
- Raiders shooting colonists = directly visible
- Arson, theft, assault = all visible to player
- **Why would investigation/witnesses matter if you SAW it happen?**
- Player knowledge = meta-knowledge that breaks immersion

### Dwarf Fortress Model:
- **Player vision ≠ In-game knowledge**
- You can watch a vampire drain a dwarf, but if no dwarf witnessed it, **no crime exists in justice system**
- Crimes only detected through in-game witness mechanics
- Hidden information creates investigation gameplay

## The Solution

By requiring Real Fog of War:
- **Colonist vision ≠ Player vision**
- Crimes only detected if colonists have line of sight
- Perfect alignment with DF witness mechanics
- Creates genuine uncertainty and investigation gameplay
- Transforms Law and Order into a "hardcore DF experience" mod

## Real Fog of War Features

### Core Mechanics
- Map initially unrevealed, must be explored
- Each pawn has Field of View (FoV) based on:
  - Sight capacity stat
  - Darkness (reduced vision at night)
  - Weather (reduced in rain/fog)
  - Bionic eyes (reduce darkness penalty)
  - Weapon range (extends when aiming)
- Vision shared across faction members
- Animals provide vision if trained for release with master set
- Sleeping pawns have 20% base FoV
- Surveillance cameras monitor areas (requires research)
- Watchtowers increase view range

### Advanced Features
- **Blind pawns provide vision through hearing**
- **Colonists can HEAR movement in fog** (sound waves)
- Built-in raid letter suppressor
- Night vision integration (e.g., VE Apparel night vision goggles)
- Animal body size affects vision range
- Toggle for prisoners providing vision

## API Reference

### Key Classes

#### MapComponentSeenFog
Main component for tracking visibility (one per map).

**Location**: `RimWorldRealFoW.MapComponentSeenFog`

**Key Fields**:
- `knownCells[]`: bool array - cells that have EVER been revealed (permanent)
- `factionsShownCells[][]`: short array - what each faction can CURRENTLY see
- `fowWatchers`: List of all CompFieldOfViewWatcher components
- `viewBlockerCells[]`: bool array - cells that block line of sight

**Key Methods**:
```csharp
// Check if a faction can currently see a cell
public bool IsShown(Faction faction, IntVec3 cell)
public bool IsShown(Faction faction, int x, int z)

// Get the map component from a map
public static MapComponentSeenFog GetMapComponentSeenFog(Map map)
{
    return map.GetComponent<MapComponentSeenFog>();
}
```

**Usage Example**:
```csharp
Map map = Find.CurrentMap;
MapComponentSeenFog fogComp = map.GetComponent<MapComponentSeenFog>();

// Check if player can see a cell
if (fogComp.IsShown(Faction.OfPlayer, somePawn.Position))
{
    // Crime is witnessed!
}
```

#### CompFieldOfViewWatcher
Component attached to things that provide vision (pawns, turrets, cameras).

**Location**: `RimWorldRealFoW.CompFieldOfViewWatcher`

**Key Fields**:
- `lastSightRange`: Current sight range in cells
- `lastPosition`: Last known position

**Key Methods**:
```csharp
// Calculate pawn's current sight range
public float CalcPawnSightRange(IntVec3 position, bool forTargeting, bool shouldMove)

// Force FoV recalculation
public void UpdateFoV(bool forceUpdate = false)
```

### Extension Methods

**Getting MapComponentSeenFog**:
```csharp
using RimWorldRealFoW;

// Method 1: Direct component access
MapComponentSeenFog fogComp = map.GetComponent<MapComponentSeenFog>();

// Method 2: Extension method (if available)
MapComponentSeenFog fogComp = map.GetMapComponentSeenFog();
```

## Integration Points for Justice System

### 1. Witness Detection

**When a crime occurs**, check if ANY colonist can see the location:

```csharp
public static bool HasWitness(Map map, IntVec3 crimeLocation)
{
    MapComponentSeenFog fogComp = map.GetComponent<MapComponentSeenFog>();

    // Check if player faction can see the crime location
    return fogComp.IsShown(Faction.OfPlayer, crimeLocation);
}
```

**More sophisticated witness check** (individual pawns):

```csharp
public static List<Pawn> GetWitnesses(Map map, IntVec3 crimeLocation)
{
    List<Pawn> witnesses = new List<Pawn>();
    MapComponentSeenFog fogComp = map.GetComponent<MapComponentSeenFog>();

    foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
    {
        // Check if colonist is awake and not blind
        if (!colonist.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            continue;

        // Check if colonist can see crime location
        CompFieldOfViewWatcher watcher = colonist.GetComp<CompFieldOfViewWatcher>();
        if (watcher != null)
        {
            // Get colonist's current sight range
            float sightRange = watcher.CalcPawnSightRange(colonist.Position, false, false);
            float distance = colonist.Position.DistanceTo(crimeLocation);

            // Check if within sight range AND faction can see the cell
            if (distance <= sightRange && fogComp.IsShown(Faction.OfPlayer, crimeLocation))
            {
                witnesses.Add(colonist);
            }
        }
    }

    return witnesses;
}
```

### 2. False Accusations

With FoW, implement DF-style false accusations:

```csharp
public static Pawn GetAccused(IntVec3 crimeLocation, CrimeType crimeType)
{
    List<Pawn> witnesses = GetWitnesses(Find.CurrentMap, crimeLocation);

    if (witnesses.Count == 0)
    {
        // No witnesses = no crime detected
        return null;
    }

    // With witnesses, determine accuracy
    Pawn primaryWitness = witnesses.First();

    // Check witness reliability based on:
    // - Distance to crime
    // - Light level (darkness)
    // - Witness social skills
    // - Witness relationships (may falsely accuse enemies)

    if (ShouldFalselyAccuse(primaryWitness, crimeLocation))
    {
        return GetRandomInnocentPawn();
    }

    return GetActualCriminal(crimeLocation, crimeType);
}
```

### 3. Raid Crimes

**For obvious raid crimes** (shooting, arson during combat):

```csharp
public static void RecordRaidCrime(Pawn raider, CrimeType crime, IntVec3 location)
{
    // Raid crimes are obvious and don't require witness checks
    // However, SPECIFIC details may require witnesses:

    if (crime == CrimeType.Murder)
    {
        // Murder is obvious, but WHO was killed might need witnesses
        if (HasWitness(raider.Map, location))
        {
            // Can identify specific victim
            RecordMurder(raider, victim, witnessed: true);
        }
        else
        {
            // Know someone died, but not who
            RecordMurder(raider, victim: null, witnessed: false);
        }
    }
}
```

### 4. Internal Colonist Crimes

**For colonist-on-colonist crimes**:

```csharp
public static void HandleColonistCrime(Pawn criminal, Pawn victim, CrimeType crime)
{
    List<Pawn> witnesses = GetWitnesses(criminal.Map, criminal.Position);

    if (witnesses.Count == 0)
    {
        // Crime undetected - no case filed
        Log.Message($"{criminal.LabelShort} committed {crime} but no one saw it!");
        return;
    }

    // Crime detected - but is accusation accurate?
    bool accurate = DetermineAccuracy(witnesses, criminal.Position);

    if (accurate)
    {
        FileCaseAgainst(criminal, crime, witnesses);
    }
    else
    {
        // False accusation - frame someone else
        Pawn scapegoat = PickScapegoat(witnesses, criminal);
        FileCaseAgainst(scapegoat, crime, witnesses);
    }
}
```

## Settings & Configuration

Real FoW has extensive mod settings:

- `BaseViewRange`: Adjustable vision range (recommend 55 vanilla, 65 with CE)
- `BaseHearingRange`: How far pawns can hear movement
- `PrisonerGiveVision`: Whether prisoners provide vision
- `AllyGiveVision`: Whether allies provide vision
- `AnimalVisionModifier`: Multiplier for animal vision
- `OnlyOutsideColony`: If true, only hide things outside home area
- `MapRevealAtStart`: Start with map revealed (defeats purpose for us)

**For Law and Order**, recommend:
- Keep default settings for maximum DF-like experience
- `PrisonerGiveVision = false` (prisoners shouldn't help with crime detection)
- `OnlyOutsideColony = false` (crimes can happen in home area too)

## Performance Considerations

Real FoW can cause lag with:
- Many pawns constantly recalculating vision
- Mods that make pawns constantly target enemies
- The "hiding interaction bubble in FoW" feature (recommend disable)

**Optimization tips**:
- FoW updates every 30 ticks for stationary pawns
- FoW updates immediately when pawns move
- Vision shared across faction (efficient)
- Shadow casting algorithm is optimized

## Compatibility Notes

**Known Compatible**:
- Combat Extended (set vision range to 65)
- CAI 5000 Advanced AI (disable their FoW)
- Dubs Mint Minimap (recommended for RTS experience)
- Guard For Me (patrolling colonists have purpose)
- Vanilla Expanded Apparel (night vision goggles work)

**Watch Out For**:
- Mods that reveal full map (e.g., map reroll mods)
- Mods that give player omniscience
- Mods with custom visibility logic

## Future Enhancements

### Phase 1: Basic Witness System
- Check `IsShown()` when crimes occur
- Simple witnessed/unwitnessed binary

### Phase 2: Individual Witnesses
- Track which specific colonists witnessed
- Witness reliability based on sight stats
- Distance and lighting affect accuracy

### Phase 3: False Accusations
- Social relationships affect accusations
- Grudges lead to false reports
- Interrogation reveals truth

### Phase 4: Investigation Gameplay
- Interrogate witnesses to piece together truth
- Conflicting testimonies
- Evidence vs. witness testimony

## References

- **Mod Page**: https://steamcommunity.com/sharedfiles/filedetails/?id=3391128917
- **GitHub**: https://github.com/emipa606/NWNRealFogOfWar
- **Package ID**: `Mlie.NWNRealFogOfWar`
- **Source Location**: `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3391128917\1.6\Source\rimworld-mod-real-fow\RimWorldRealFoW`

## Breaking Change Warning

**This is a MAJOR breaking change** that fundamentally alters Law and Order's gameplay:

- Players MUST install Real Fog of War
- Gameplay becomes significantly harder
- No longer compatible with "casual" playstyles
- Embraces "Dwarf Fortress hardcore experience"

**Save Compatibility**: Adding FoW mid-game may cause issues. Recommend starting new save when enabling Law and Order + FoW together.

## Summary

By making Real Fog of War a required dependency:

✅ Solves player omniscience problem
✅ Enables true witness mechanics
✅ Creates genuine investigation gameplay
✅ Aligns with Dwarf Fortress design philosophy
✅ Adds uncertainty and emergent narratives
✅ Makes crimes feel consequential

This transforms Law and Order from a "justice addon" to a **total conversion** that fundamentally changes how RimWorld is played - exactly like Dwarf Fortress.
