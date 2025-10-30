# HugsLib Logging Guide

## Overview

HugsLib provides a powerful logging system through the `ModLogger` class. Logs appear in the RimWorld dev console and in the log file.

## Basic Usage

### From Within ModBase Class
```csharp
public class Mod : ModBase
{
    public override void DefsLoaded()
    {
        // Use Logger property directly
        Logger.Message("This is an info message");
        Logger.Warning("This is a warning");
        Logger.Error("This is an error");
    }
}
```

### From Anywhere in Your Mod
```csharp
using Law_and_Order.Source;

public class SomeClass
{
    public void SomeMethod()
    {
        // Use the static Log property
        Mod.Log.Message("Crime recorded successfully");
        Mod.Log.Warning("Unusual crime type detected");
        Mod.Log.Error("Failed to record crime");
    }
}
```

## Log Levels

### 1. Message (Info)
For general information and debugging:
```csharp
Mod.Log.Message("Pawn {0} committed {1}", pawn.Name, crimeType);
```

### 2. Warning
For potential issues that aren't critical:
```csharp
Mod.Log.Warning("Crime record not found for pawn {0}", pawn.Name);
```

### 3. Error
For critical issues and exceptions:
```csharp
Mod.Log.Error("Failed to load criminal record: {0}", exception.Message);
```

## Conditional Logging

### Debug-Only Logs
Logs that only appear when dev mode is enabled:
```csharp
if (Prefs.DevMode)
{
    Mod.Log.Message("Debug: Crime list has {0} entries", crimes.Count);
}
```

### Trace Logging
For very detailed debugging:
```csharp
#if DEBUG
Mod.Log.Message("Entering method ProcessCrime()");
#endif
```

## Formatting

### String Interpolation (C# 6+)
```csharp
Mod.Log.Message($"Recording {crimeType} by {criminal.Name}");
```

### String.Format Style
```csharp
Mod.Log.Message("Recording {0} by {1}", crimeType, criminal.Name);
```

### With Object Details
```csharp
Mod.Log.Message("Crime: Type={0}, Victim={1}, Damage={2:F1}",
    crime.crimeType,
    crime.victim?.Name ?? "None",
    crime.damageDealt);
```

## Exception Logging

### Basic Exception Logging
```csharp
try
{
    // Some code
}
catch (Exception e)
{
    Mod.Log.Error("Failed to record crime: {0}", e);
}
```

### Detailed Exception Information
```csharp
try
{
    // Some code
}
catch (Exception e)
{
    Mod.Log.Error("Crime recording failed:\nMessage: {0}\nStack trace:\n{1}",
        e.Message,
        e.StackTrace);
}
```

## Practical Examples

### Example 1: Logging Crime Recording
```csharp
public void AddCrime(Crime crime)
{
    if (crimes == null)
    {
        crimes = new List<Crime>();
    }

    crimes.Add(crime);

    if (Prefs.DevMode)
    {
        Mod.Log.Message($"Crime recorded: {crime} (Total crimes: {crimes.Count})");
    }
}
```

### Example 2: Logging in CrimeUtils
```csharp
public static void RecordCrime(Pawn criminal, CrimeType crimeType, Pawn victim = null)
{
    if (criminal == null)
    {
        Mod.Log.Warning("Attempted to record crime with null criminal");
        return;
    }

    try
    {
        var criminalRecord = GetOrCreateCriminalRecord(criminal);
        criminalRecord?.AddCrime(crimeType, victim);

        if (Prefs.DevMode)
        {
            Mod.Log.Message($"Recorded {crimeType} by {criminal.Name}" +
                          (victim != null ? $" against {victim.Name}" : ""));
        }
    }
    catch (Exception e)
    {
        Mod.Log.Error($"Failed to record crime for {criminal.Name}: {e}");
    }
}
```

### Example 3: Logging in Harmony Patches
```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
public static class TrackAssault_Patch
{
    static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        try
        {
            Pawn victim = dinfo.IntendedTarget as Pawn;
            Pawn attacker = dinfo.Instigator as Pawn;

            if (victim == null || attacker == null)
                return;

            if (!victim.IsColonist || !attacker.HostileTo(victim.Faction))
                return;

            CrimeType crimeType = victim.Dead ? CrimeType.Murder : CrimeType.Assault;

            CrimeUtils.RecordCrime(attacker, crimeType, victim, damageDealt: totalDamageDealt);

            if (Prefs.DevMode)
            {
                Mod.Log.Message($"Tracked {crimeType}: {attacker.Name} -> {victim.Name} ({totalDamageDealt:F1} damage)");
            }
        }
        catch (Exception e)
        {
            Mod.Log.Error($"Error in TrackAssault_Patch: {e}");
        }
    }
}
```

### Example 4: Startup Logging
```csharp
public class Mod : ModBase
{
    public override void DefsLoaded()
    {
        base.DefsLoaded();

        Logger.Message("Law and Order mod initialized");
        Logger.Message($"Loaded {DefDatabase<HediffDef>.AllDefsListForReading.Count} hediff defs");
    }

    public override void WorldLoaded()
    {
        base.WorldLoaded();

        Logger.Message("World loaded, crime tracking active");
    }
}
```

## Performance Considerations

### Don't Log in Hot Paths
Avoid logging in methods called every tick:
```csharp
// BAD - logs every tick
public override void Tick()
{
    base.Tick();
    Mod.Log.Message("Ticking..."); // Too much logging!
}

// GOOD - only log important events
public override void Tick()
{
    base.Tick();

    if (someImportantEventHappened)
    {
        Mod.Log.Message("Important event occurred");
    }
}
```

### Use Conditional Compilation
For expensive logging operations:
```csharp
#if DEBUG
    var details = GenerateExpensiveDebugInfo();
    Mod.Log.Message($"Debug info: {details}");
#endif
```

### Check Dev Mode First
```csharp
if (Prefs.DevMode)
{
    // Build expensive log message
    var details = BuildDetailedLogMessage();
    Mod.Log.Message(details);
}
```

## Best Practices

1. **Use appropriate log levels**
   - Message: Normal operation, debug info
   - Warning: Recoverable issues
   - Error: Critical failures

2. **Include context in messages**
   ```csharp
   // BAD
   Mod.Log.Error("Failed");

   // GOOD
   Mod.Log.Error($"Failed to record crime for {pawn.Name}: Missing hediff def");
   ```

3. **Wrap risky operations in try-catch**
   ```csharp
   try
   {
       // Risky operation
   }
   catch (Exception e)
   {
       Mod.Log.Error($"Operation failed: {e}");
   }
   ```

4. **Use dev mode for verbose logging**
   - Keep production logs minimal
   - Add detailed logs behind `Prefs.DevMode` checks

5. **Log initialization and shutdown**
   ```csharp
   Logger.Message("Mod initialized");
   Logger.Message("Registering Harmony patches");
   Logger.Message("Crime tracking system ready");
   ```

## Viewing Logs

### In-Game Console
1. Press `~` (tilde) to open dev console
2. Or press `Ctrl+F12` for full log window
3. Filter by mod name: "Law and Order"

### Log File Location
```
Windows: C:\Users\[Username]\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
Mac: ~/Library/Logs/Unity/Player.log
Linux: ~/.config/unity3d/Ludeon Studios/RimWorld/Player.log
```

## Common Patterns

### Method Entry/Exit Tracing
```csharp
public void ComplexMethod()
{
    Mod.Log.Message("Entering ComplexMethod");

    try
    {
        // Complex logic

        Mod.Log.Message("ComplexMethod completed successfully");
    }
    catch (Exception e)
    {
        Mod.Log.Error($"ComplexMethod failed: {e}");
        throw;
    }
}
```

### Null Check Logging
```csharp
if (pawn == null)
{
    Mod.Log.Warning("Attempted to process null pawn");
    return;
}
```

### Collection Logging
```csharp
Mod.Log.Message($"Processing {crimes.Count} crimes");
foreach (var crime in crimes)
{
    if (Prefs.DevMode)
    {
        Mod.Log.Message($"  - {crime}");
    }
}
```
