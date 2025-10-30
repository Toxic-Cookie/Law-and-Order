# HugsLib Logging - Quick Reference

## Setup (Already Done!)
```csharp
// In Mod.cs
public static ModLogger Log => Instance?.Logger;
```

## Basic Usage

### Three Log Levels
```csharp
Mod.Log.Message("Info message");   // General info
Mod.Log.Warning("Warning message"); // Potential issues
Mod.Log.Error("Error message");     // Critical failures
```

### From Anywhere
```csharp
using Law_and_Order.Source;

Mod.Log.Message("Your message here");
```

### With Formatting
```csharp
Mod.Log.Message($"Pawn: {pawn.Name}, Crime: {crimeType}");
Mod.Log.Error($"Failed to process {item.Label}: {exception.Message}");
```

## Common Patterns

### Dev Mode Only
```csharp
if (Prefs.DevMode)
{
    Mod.Log.Message("Debug info here");
}
```

### Try-Catch
```csharp
try
{
    // Code
}
catch (Exception e)
{
    Mod.Log.Error($"Operation failed: {e}");
}
```

### Null Check
```csharp
if (pawn == null)
{
    Mod.Log.Warning("Null pawn detected");
    return;
}
```

## View Logs

1. In-game: Press `~` (tilde) for console
2. Full log: Press `Ctrl+F12`
3. Log file: `AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`

## Best Practices

✅ **DO**
- Log errors and warnings
- Use dev mode checks for verbose logging
- Include context (pawn names, values, etc.)
- Wrap risky operations in try-catch

❌ **DON'T**
- Log every tick (performance!)
- Log in hot paths without dev mode check
- Use vague messages like "Failed"

## Example: Crime Recording
```csharp
public static void RecordCrime(Pawn criminal, CrimeType type)
{
    if (criminal == null)
    {
        Mod.Log.Warning("Attempted to record crime with null criminal");
        return;
    }

    try
    {
        // Your code here

        if (Prefs.DevMode)
        {
            Mod.Log.Message($"Recorded {type} by {criminal.Name}");
        }
    }
    catch (Exception e)
    {
        Mod.Log.Error($"Failed to record crime: {e}");
    }
}
```
