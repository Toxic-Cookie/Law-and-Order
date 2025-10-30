# Justice UI System

## Overview

The Justice UI provides a centralized interface for managing criminals, viewing their crimes, and performing actions like scheduling hearings, releasing prisoners, and pardoning offenders. It's accessed via a custom tab on the main hotbar.

## Files Created

### 1. MainTabWindow_Justice.cs
**Location**: `Source/UI/MainTabWindow_Justice.cs`

The main window class that displays the justice interface. Inherits from RimWorld's `MainTabWindow` class.

**Key Features**:
- Three tabs: Active Criminals, Imprisoned, Historical
- Split-panel layout (list on left, details on right)
- Search functionality
- Crime list with details
- Action buttons for hearings, releases, pardons

### 2. MainButtons.xml
**Location**: `Defs/MainButtonDefs/MainButtons.xml`

Defines the hotbar button that opens the justice window.

**Configuration**:
- DefName: `LawAndOrder_Justice`
- Default hotkey: `J`
- Order: 60 (appears after vanilla tabs)
- Icon: Uses placeholder (LaunchReport), should be replaced with custom icon

### 3. CourtroomUtils.cs
**Location**: `Source/Utils/CourtroomUtils.cs`

Utility class for detecting and validating courtrooms.

**Methods**:
- `HasCourtroom()` - Check if colony has any valid courtroom
- `GetPotentialCourtrooms()` - Get all rooms that could be courtrooms
- `IsValidCourtroom(Room)` - Validate a specific room
- `GetCourtroomQuality(Room)` - Get quality score for a courtroom
- `GetBestCourtroom()` - Find the best available courtroom

### 4. LawAndOrder_Keys.xml
**Location**: `Languages/English/Keyed/LawAndOrder_Keys.xml`

Translation strings for the UI.

## UI Layout

```
┌─────────────────────────────────────────────────────────┐
│  [Active Criminals] [Imprisoned] [Historical]           │
├──────────────────┬──────────────────────────────────────┤
│ Criminal List    │ Selected Criminal Details            │
│                  │                                       │
│ [Search box]     │ Name: [Raider Bob]                   │
│                  │ Status: Imprisoned                    │
│ • Criminal 1     │ Trial Status: Not Scheduled           │
│ • Criminal 2     │                                       │
│ • Criminal 3     │ Crimes Committed:                     │
│   ...            │  ┌───────────────────────────────┐   │
│                  │  │ Assault                       │   │
│ Criminals: 12    │  │ Victim: Colonist Alice       │   │
│                  │  │ Damage: 15.0                  │   │
│                  │  │ Days ago: 2                   │   │
│                  │  └───────────────────────────────┘   │
│                  │  ┌───────────────────────────────┐   │
│                  │  │ Murder                        │   │
│                  │  │ Victim: Colonist Bob         │   │
│                  │  │ Damage: 45.0                  │   │
│                  │  │ Days ago: 5                   │   │
│                  │  └───────────────────────────────┘   │
│                  │                                       │
│                  │ [Schedule Hearing]  [Release]        │
│                  │ [Pardon]           [View Info]       │
└──────────────────┴──────────────────────────────────────┘
```

## Usage

### Opening the Window

1. Click the "Justice" button on the hotbar (bottom of screen)
2. Or press the `J` hotkey

### Viewing Criminals

**Active Criminals Tab**:
- Shows all living pawns with criminal records who are not imprisoned
- Includes raiders, visitors, and colonists with crimes

**Imprisoned Tab**:
- Shows all prisoners who have criminal records
- Useful for tracking who's awaiting trial

**Historical Tab**:
- Shows deceased criminals
- Useful for reviewing past crimes

### Searching

Use the search box to filter criminals by name. The search is case-insensitive and searches the full name.

### Viewing Criminal Details

Click on any criminal in the list to view:
- Their current status (imprisoned, colonist, faction member, etc.)
- Trial status (not yet implemented)
- Complete list of crimes with:
  - Crime type
  - Victim (if applicable)
  - Damage dealt
  - Days since crime was committed
  - Additional information

### Actions

**Schedule Hearing**:
- Validates that a courtroom exists
- Shows error message if no courtroom available
- TODO: Implement actual hearing scheduling

**Release**:
- Only available for imprisoned criminals
- Converts prisoner to guest status
- Shows confirmation message

**Pardon**:
- Removes all crimes from the criminal's record
- Removes the criminal record hediff entirely
- Shows confirmation message

**View Info**:
- Opens the standard RimWorld info card for the pawn
- Shows health, needs, mood, etc.

## Courtroom Requirements

A valid courtroom must have:
- At least 10 cells of space
- At least one piece of sittable furniture (chair, stool, throne, etc.)
- Be indoors (not outdoor for work)

The system will automatically select the most impressive valid room when scheduling hearings.

**Current Implementation Note**: Courtroom detection is basic. You can expand it to require:
- Specific furniture pieces (judge's throne, witness stand, etc.)
- Room role/designation
- Custom courtroom marker building
- Minimum impressiveness score

## Extending the System

### Adding Custom Courtroom Requirements

Edit `CourtroomUtils.IsValidCourtroom()`:

```csharp
public static bool IsValidCourtroom(Room room)
{
    if (room == null || room.OutdoorsForWork)
        return false;

    if (room.CellCount < 20) // Increase minimum size
        return false;

    // Check for specific furniture
    bool hasJudgeThrone = room.ContainedAndAdjacentThings
        .Any(t => t.def.defName == "MyCustomJudgeThrone");

    if (!hasJudgeThrone)
        return false;

    // Add more requirements...

    return true;
}
```

### Implementing Hearing Scheduling

The `ScheduleHearing()` method currently just shows a message. To implement actual hearings:

1. Create a hearing job/event system
2. Schedule a job for the criminal to appear in court
3. Schedule jobs for judges, witnesses, etc.
4. Create a hearing resolution system
5. Update trial status display

Example structure:

```csharp
private void ScheduleHearing()
{
    if (!CourtroomUtils.HasCourtroom())
    {
        // Error message
        return;
    }

    // Create hearing data
    var hearing = new Hearing
    {
        criminal = selectedCriminal,
        courtroom = CourtroomUtils.GetBestCourtroom(),
        scheduledTick = Find.TickManager.TicksGame + GenDate.TicksPerDay,
        judge = FindAvailableJudge(),
        // ... other hearing data
    };

    // Add to hearing manager
    Find.World.GetComponent<HearingManager>().ScheduleHearing(hearing);

    // Update UI
    Messages.Message(...);
}
```

### Adding Custom Tabs

To add more tabs to the window, edit `PreOpen()` in MainTabWindow_Justice.cs:

```csharp
tabs.Add(new TabRecord(
    "My Custom Tab".Translate(),
    () => { curTab = JusticeTab.CustomTab; selectedCriminal = null; },
    () => curTab == JusticeTab.CustomTab
));
```

And update the `JusticeTab` enum:

```csharp
private enum JusticeTab
{
    ActiveCriminals,
    Imprisoned,
    Historical,
    CustomTab  // Your new tab
}
```

### Customizing the Icon

Replace the placeholder icon in `MainButtons.xml`:

1. Create a 64x64 PNG icon
2. Place it in `Texture/UI/Commands/` folder
3. Update the XML:
   ```xml
   <iconPath>UI/Commands/YourCustomIcon</iconPath>
   ```

### Adding Criminal Status Indicators

In `DrawCriminalListEntry()`, add visual indicators:

```csharp
// Add colored border based on crime severity
Color borderColor = Color.white;
int crimeCount = record?.TotalCrimeCount ?? 0;

if (crimeCount > 10)
    borderColor = Color.red;
else if (crimeCount > 5)
    borderColor = Color.yellow;

Widgets.DrawBoxSolid(rect, borderColor);
```

## Integration with Crime Tracking

The UI automatically integrates with the crime tracking system:

- Uses `CrimeUtils.TryGetCriminalRecord()` to get criminal data
- Uses `CrimeUtils.HasCriminalRecord()` to filter pawns
- Displays crimes from the `Hediff_Crimes` system

No additional setup is required - as soon as crimes are recorded using `CrimeUtils.RecordCrime()`, they'll appear in the Justice UI.

## Performance Considerations

**Criminal List Caching**:
The current implementation queries all pawns every frame. For better performance with many pawns:

```csharp
private List<Pawn> cachedCriminals;
private int lastCacheUpdate;

private List<Pawn> GetCriminalsForTab()
{
    // Only update cache every 60 ticks (1 second)
    if (Find.TickManager.TicksGame - lastCacheUpdate > 60)
    {
        cachedCriminals = RecalculateCriminals();
        lastCacheUpdate = Find.TickManager.TicksGame;
    }

    return cachedCriminals;
}
```

**Scroll View Optimization**:
For very long crime lists, consider implementing virtual scrolling to only render visible entries.

## Troubleshooting

**Button doesn't appear on hotbar**:
- Check that MainButtons.xml is in the correct Defs folder
- Verify the `tabWindowClass` matches the fully qualified class name
- Check for XML syntax errors
- Reload the game or use dev mode to reload defs

**Window opens but is blank**:
- Check for exceptions in the log (Ctrl+F12)
- Verify translation keys are loaded
- Check that `DoWindowContents()` is being called

**Courtroom validation always fails**:
- Verify the map has rooms with the minimum requirements
- Check that `CourtroomUtils.HasCourtroom()` is correctly detecting rooms
- Use dev mode logging to debug room detection

**Criminals not appearing**:
- Verify crimes are being recorded correctly
- Check that the hediff is being applied
- Use `CrimeUtils.HasCriminalRecord()` in dev console to verify
- Check the correct tab is selected (active vs imprisoned vs historical)

## Future Enhancements

Potential features to add:

1. **Trial Scheduling System**
   - Calendar integration
   - Judge assignment
   - Witness selection
   - Trial duration

2. **Sentence System**
   - Automatic sentence calculation based on crimes
   - Prison time tracking
   - Execution scheduling
   - Community service

3. **Evidence System**
   - Witness testimonies
   - Physical evidence collection
   - Evidence quality affecting trial outcome

4. **Lawyer/Defense System**
   - Assign lawyers to criminals
   - Legal skill affecting outcomes
   - Appeals process

5. **Fines and Restitution**
   - Silver penalties
   - Item confiscation
   - Forced labor

6. **Crime Severity Levels**
   - Petty, Minor, Serious, Severe, Capital
   - Different trial processes for each
   - Automatic vs manual handling

7. **Statistics and Reports**
   - Crime trends over time
   - Most common crimes
   - Conviction rates
   - Graphs and charts
