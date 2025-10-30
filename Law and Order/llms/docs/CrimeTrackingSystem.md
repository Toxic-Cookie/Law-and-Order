# Crime Tracking System

## Overview

The crime tracking system uses a custom `Hediff_Crimes` hediff to efficiently store crime data per-pawn. This approach:
- Only stores data for pawns that have committed crimes
- Automatically saves/loads with the game
- Doesn't bloat the save file for pawns without crimes
- Provides easy querying and filtering of crime data

## Core Components

### 1. Crime Class (`Crime.cs`)
Represents a single criminal act with:
- `CrimeType` - The type of crime (Assault, Murder, etc.)
- `tickCommitted` - When the crime occurred
- `victim` - The victim pawn (if applicable)
- `targetThing` - The target object (if applicable)
- `damageDealt` - Amount of damage dealt
- `additionalInfo` - Any extra context

### 2. CrimeType Enum
Predefined crime types:
- `Assault` - Attacking a colonist
- `Murder` - Killing a colonist
- `PropertyDestruction` - Destroying colony property
- `Arson` - Setting fires
- `Theft` - Stealing items
- `AnimalAbuse` - Harming colony animals
- `Trespassing` - Entering forbidden areas
- `Kidnapping` - Taking colonists prisoner
- `Vandalism` - Minor property damage

### 3. Hediff_Crimes
The main hediff that stores a list of crimes. Key features:
- Automatically saves/loads with game state
- Only exists on pawns that have committed crimes
- Provides methods to query, filter, and manage crimes

### 4. CrimeUtils
Static utility class for easy crime tracking:
```csharp
// Record a crime
CrimeUtils.RecordCrime(raider, CrimeType.Assault, victim: colonist, damageDealt: 15f);

// Check if pawn has crimes
bool hasCrimes = CrimeUtils.HasCriminalRecord(pawn);

// Get crime count
int crimeCount = CrimeUtils.GetCrimeCount(pawn);

// Check for specific crime type
bool isMurderer = CrimeUtils.HasCommittedCrime(pawn, CrimeType.Murder);
```

## Usage Examples

### Recording Crimes

#### Simple Crime Recording
```csharp
// Record an assault
CrimeUtils.RecordCrime(
    criminal: raiderPawn,
    crimeType: CrimeType.Assault,
    victim: colonistPawn,
    damageDealt: 12.5f
);

// Record property destruction
CrimeUtils.RecordCrime(
    criminal: raiderPawn,
    crimeType: CrimeType.PropertyDestruction,
    targetThing: destroyedWall
);
```

#### Advanced Crime Recording with Details
```csharp
CrimeUtils.RecordCrime(
    criminal: raider,
    crimeType: CrimeType.Murder,
    victim: colonist,
    damageDealt: 45.0f,
    additionalInfo: "Killed with charge rifle during raid"
);
```

### Querying Crimes

#### Get All Crimes for a Pawn
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    foreach (var crime in record.Crimes)
    {
        Log.Message($"Crime: {crime}");
    }
}
```

#### Filter Crimes by Type
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    var murders = record.GetCrimesByType(CrimeType.Murder);
    var assaults = record.GetCrimesByType(CrimeType.Assault);

    Log.Message($"Committed {murders.Count} murders and {assaults.Count} assaults");
}
```

#### Get Recent Crimes
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    // Get crimes from last 7 days
    var recentCrimes = record.GetRecentCrimes(7);

    // Get crimes from last 30 days
    var monthlyCrimes = record.GetRecentCrimes(30);
}
```

#### Get Crimes Against Specific Victim
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    var crimesAgainstBob = record.GetCrimesAgainstVictim(bobThePawn);
    Log.Message($"Committed {crimesAgainstBob.Count} crimes against Bob");
}
```

### Cleaning Up Old Crimes
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    // Remove crimes older than 60 days
    // Will auto-remove hediff if no crimes remain
    record.ClearCrimesOlderThan(60);
}
```

## Integration with Harmony Patches

### Example: Track Damage Events
```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
public static class TrackDamage_Patch
{
    static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        Pawn victim = __instance.pawn;
        Pawn attacker = dinfo.Instigator as Pawn;

        if (victim == null || attacker == null)
            return;

        // Only track hostile actions against colonists
        if (!victim.IsColonist || !attacker.HostileTo(victim.Faction))
            return;

        CrimeType crimeType = victim.Dead ? CrimeType.Murder : CrimeType.Assault;

        CrimeUtils.RecordCrime(
            criminal: attacker,
            crimeType: crimeType,
            victim: victim,
            damageDealt: totalDamageDealt
        );
    }
}
```

### Example: Track Theft
```csharp
[HarmonyPatch(typeof(Pawn_InventoryTracker), "TryAddItemNotForSale")]
public static class TrackTheft_Patch
{
    static void Postfix(Pawn_InventoryTracker __instance, Thing item, bool __result)
    {
        if (!__result) return; // Item wasn't added

        Pawn pawn = __instance.pawn;

        // Check if item belonged to player and pawn is hostile
        if (item.Faction == Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer))
        {
            CrimeUtils.RecordCrime(
                criminal: pawn,
                crimeType: CrimeType.Theft,
                targetThing: item
            );
        }
    }
}
```

## Performance Considerations

1. **Minimal Save File Impact**: Only pawns with crimes get the hediff
2. **Efficient Queries**: Use LINQ-based filtering methods
3. **Optional Cleanup**: Use `ClearCrimesOlderThan()` to limit list size
4. **DefOf Usage**: Uses RimWorld's DefOf system for fast def lookups

## Best Practices

1. **Only track meaningful crimes**: Don't track every minor action
2. **Clean up old records**: Periodically clear ancient crimes to keep lists manageable
3. **Use appropriate crime types**: Pick the most specific crime type
4. **Add context**: Use `additionalInfo` field for important details
5. **Check for null**: Always null-check pawns before recording crimes

## HediffDef Configuration

The hediff is defined in `Defs/HediffDefs/Hediff_Crimes.xml`:
- **defName**: `LawAndOrder_CriminalRecord`
- **visible**: `false` (doesn't show in health tab by default)
- **isBad**: `true` (marked as negative)
- **ShouldRemove**: `false` (persists until manually cleared)

To make the hediff visible in the health tab, change `<visible>false</visible>` to `<visible>true</visible>`.

## Advanced Features

### Custom Crime Inspection
You can override `TipStringExtra` to customize what's shown in the tooltip:
```csharp
public override string TipStringExtra
{
    get
    {
        if (crimes == null || crimes.Count == 0)
            return "No crimes recorded";

        var recentCount = GetRecentCrimes(7).Count;
        var murderCount = GetCrimesByType(CrimeType.Murder).Count;

        return $"Total crimes: {crimes.Count}\n" +
               $"Recent (7 days): {recentCount}\n" +
               $"Murders: {murderCount}";
    }
}
```

### Crime Statistics
```csharp
var record = CrimeUtils.TryGetCriminalRecord(pawn);
if (record != null)
{
    // Group by type
    var crimesByType = record.Crimes
        .GroupBy(c => c.crimeType)
        .ToDictionary(g => g.Key, g => g.Count());

    foreach (var kvp in crimesByType)
    {
        Log.Message($"{kvp.Key}: {kvp.Value} occurrences");
    }

    // Calculate total damage dealt
    float totalDamage = record.Crimes.Sum(c => c.damageDealt);
}
```
