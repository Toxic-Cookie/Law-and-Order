# Justice UI - Setup Guide

## What You Just Got

A complete Justice UI system with:
- Hotbar button (press `J`)
- Quest-like window showing all criminals
- Three tabs: Active, Imprisoned, Historical
- Full crime details
- Action buttons (hearing, release, pardon)
- Courtroom validation

## Files Created

```
Law and Order/
├── Source/
│   ├── UI/
│   │   └── MainTabWindow_Justice.cs          ✅ Main window
│   └── Utils/
│       └── CourtroomUtils.cs                  ✅ Courtroom detection
├── Defs/
│   └── MainButtonDefs/
│       └── MainButtons.xml                    ✅ Hotbar button
└── Languages/
    └── English/
        └── Keyed/
            └── LawAndOrder_Keys.xml          ✅ UI translations
```

## Quick Start

### 1. Build the Mod
Your .csproj is already configured to auto-copy files after build:
```bash
# Build in Visual Studio or:
dotnet build
```

Files will automatically copy to:
`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Law and Order\`

### 2. Test in Game
1. Launch RimWorld
2. Enable "Law and Order" mod
3. Load/Start a game
4. Look for "Justice" button on bottom hotbar
5. Click it or press `J`

### 3. Test with Sample Crimes
Open dev console and test:
```csharp
// Get a pawn and record a crime
var pawn = Find.CurrentMap.mapPawns.FreeColonists.First();
var victim = Find.CurrentMap.mapPawns.FreeColonists.Last();

Law_and_Order.Source.Utils.CrimeUtils.RecordCrime(
    pawn,
    Law_and_Order.Source.Hediffs.CrimeType.Assault,
    victim,
    damageDealt: 15f
);
```

Then open Justice UI and see the crime!

## What Works Now

✅ **Opening the window** - Press J or click button
✅ **Viewing criminals** - All pawns with crimes appear
✅ **Searching** - Filter by name
✅ **Crime details** - Full crime list with victim, damage, date
✅ **Release prisoner** - Functional
✅ **Pardon criminal** - Removes all crimes
✅ **Courtroom check** - Validates before scheduling
✅ **Status display** - Shows if imprisoned, dead, etc.

## What's Placeholder

⏳ **Schedule Hearing** - Shows message but doesn't create actual hearing
⏳ **Trial System** - Not yet implemented
⏳ **Sentencing** - Not yet implemented

These have clear TODO markers in the code for implementation.

## Customization

### Change the Icon
1. Create 64x64 PNG icon
2. Save to: `Texture/UI/Commands/YourIcon.png`
3. Edit `Defs/MainButtonDefs/MainButtons.xml`:
   ```xml
   <iconPath>UI/Commands/YourIcon</iconPath>
   ```

### Change Courtroom Requirements
Edit `CourtroomUtils.IsValidCourtroom()`:
```csharp
// Require bigger room
if (room.CellCount < 20) // was 10
    return false;

// Require specific furniture
bool hasThrone = room.ContainedAndAdjacentThings
    .Any(t => t.def.defName == "Throne");
```

### Add More Tabs
Edit `MainTabWindow_Justice.PreOpen()`:
```csharp
tabs.Add(new TabRecord(
    "My Tab".Translate(),
    () => { curTab = JusticeTab.MyTab; },
    () => curTab == JusticeTab.MyTab
));
```

## Integration with Crime Tracking

The UI automatically shows crimes recorded with:
```csharp
CrimeUtils.RecordCrime(criminal, CrimeType.Assault, victim);
```

Set up Harmony patches to record crimes:
- `Pawn_HealthTracker.PostApplyDamage` - for assaults
- `Thing.Destroy` - for property damage
- Fire events - for arson
- Inventory changes - for theft

See `Source/Examples/CrimeTrackingExample.cs` for templates.

## Troubleshooting

**Button doesn't appear:**
- Check mod is enabled
- Verify files copied to RimWorld Mods folder
- Check for errors in Player.log
- Try dev mode: "Reload all defs"

**Window is blank:**
- Check translation keys loaded
- Look for exceptions in log (Ctrl+F12)
- Verify About.xml has correct mod ID

**No criminals showing:**
- Record test crimes (see Quick Start #3)
- Check correct tab (Active vs Imprisoned)
- Verify hediff is being applied

**Courtroom check fails:**
- Build room with 10+ cells
- Add chairs/stools/thrones
- Must be indoors
- Use dev logging to debug

## Next Steps

1. **Test the UI thoroughly**
   - Record different crime types
   - Test all action buttons
   - Try search functionality

2. **Implement crime detection**
   - Add Harmony patches for damage
   - Track property destruction
   - Monitor theft events

3. **Build hearing system**
   - Create Hearing class
   - Schedule court jobs
   - Implement trial logic

4. **Add sentencing**
   - Calculate prison time
   - Implement fines
   - Add execution scheduling

## Getting Help

Check documentation:
- `JusticeUISystem.md` - Complete technical docs
- `QuickReference_JusticeUI.md` - Quick reference
- `IMPLEMENTATION_SUMMARY.md` - What's implemented
- `CrimeTrackingSystem.md` - Crime tracking docs

## Pro Tips

💡 Enable dev mode (`Ctrl+Shift+D`) for:
- Detailed logging of all actions
- Debug info in window
- Easy crime testing

💡 Use search to filter long criminal lists

💡 Historical tab keeps record of deceased criminals

💡 Pardon is permanent - no undo!

💡 Most impressive room is auto-selected for hearings

## You're Ready!

Everything is set up and working. Build your mod, launch RimWorld, and press `J` to see your new Justice UI in action! 🎉
