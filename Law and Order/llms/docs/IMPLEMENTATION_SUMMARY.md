# Law and Order - Implementation Summary

## What Was Implemented

### Justice UI System (Complete)
A fully functional hotbar tab that opens a quest-like window for managing criminals.

## Files Created

### 1. Core UI Components

#### MainTabWindow_Justice.cs
**Location**: `Source/UI/MainTabWindow_Justice.cs`
**Purpose**: Main window class for the justice interface

**Features**:
- ✅ Three tabs: Active Criminals, Imprisoned, Historical
- ✅ Split-panel layout (criminal list + detail view)
- ✅ Search/filter functionality
- ✅ Criminal selection and highlighting
- ✅ Detailed crime display with victim, damage, and time
- ✅ Status indicators (imprisoned, dead, colonist, faction)
- ✅ Action buttons (schedule hearing, release, pardon, view info)
- ✅ Courtroom validation with warnings
- ✅ Real-time integration with crime tracking system

#### MainButtons.xml
**Location**: `Defs/MainButtonDefs/MainButtons.xml`
**Purpose**: Defines the hotbar button

**Configuration**:
- Button label: "Justice"
- Default hotkey: `J`
- Order: 60 (after vanilla tabs)
- Icon: Placeholder (LaunchReport) - can be customized

### 2. Utility Classes

#### CourtroomUtils.cs
**Location**: `Source/Utils/CourtroomUtils.cs`
**Purpose**: Courtroom detection and validation

**Features**:
- `HasCourtroom()` - Quick check for valid courtroom
- `GetPotentialCourtrooms()` - Get all qualifying rooms
- `IsValidCourtroom(Room)` - Validate specific room
- `GetCourtroomQuality(Room)` - Calculate room quality score
- `GetBestCourtroom()` - Find most impressive courtroom
- `GetCourtroomValidationMessage(Room)` - Human-readable validation

**Current Courtroom Requirements**:
- Minimum 10 cells
- At least one sittable furniture piece
- Must be indoors
- Uses room impressiveness for quality scoring

### 3. Translations

#### LawAndOrder_Keys.xml
**Location**: `Languages/English/Keyed/LawAndOrder_Keys.xml`
**Purpose**: All UI text strings

**Includes translations for**:
- Tab names
- Status messages
- Action buttons
- Error messages
- Field labels
- Confirmation messages

### 4. Documentation

#### JusticeUISystem.md
**Location**: `llms/JusticeUISystem.md`
**Purpose**: Comprehensive technical documentation

**Contents**:
- Complete system overview
- File descriptions
- UI layout diagrams
- Usage instructions
- Extension guide
- Performance considerations
- Troubleshooting
- Future enhancement ideas

#### QuickReference_JusticeUI.md
**Location**: `llms/QuickReference_JusticeUI.md`
**Purpose**: Quick reference guide

**Contents**:
- How to open window
- Tab descriptions
- Feature list
- Action buttons
- Courtroom requirements
- Tips and hotkeys

## How It Works

### Opening the Justice Window
1. Click "Justice" button on main hotbar (bottom of screen)
2. Or press `J` hotkey
3. Window opens with quest-like layout

### Viewing Criminals
- **Left Panel**: Scrollable list of criminals with search
- **Right Panel**: Detailed view of selected criminal
- **Tabs**: Filter by Active/Imprisoned/Historical

### Performing Actions

**Schedule Hearing**:
1. Select a criminal
2. Click "Schedule Hearing"
3. System validates courtroom exists
4. Shows success or error message
5. (Hearing logic not yet implemented - placeholder)

**Release Prisoner**:
1. Select an imprisoned criminal
2. Click "Release"
3. Prisoner becomes guest
4. Shows confirmation message

**Pardon Criminal**:
1. Select any criminal
2. Click "Pardon"
3. All crimes removed
4. Criminal record hediff deleted
5. Shows confirmation message

**View Info**:
1. Select any criminal
2. Click "View Info"
3. Opens standard RimWorld character info card

## Integration with Existing Systems

### Crime Tracking Integration
The Justice UI automatically works with the crime tracking system:

```csharp
// Recording a crime automatically makes it visible in UI
CrimeUtils.RecordCrime(raider, CrimeType.Assault, colonist);
// Raider now appears in Justice UI with assault listed

// Checking crimes from UI
var record = CrimeUtils.TryGetCriminalRecord(selectedPawn);
var crimes = record.Crimes; // Displayed in detail panel
```

### HugsLib Logging
All actions are logged when in dev mode:
```csharp
if (Prefs.DevMode)
{
    Mod.Log?.Message($"Scheduled hearing for {criminal.Name}");
}
```

## Current State

### ✅ Complete Features
- [x] Main window with three tabs
- [x] Criminal list with search
- [x] Detailed criminal view
- [x] Crime list display
- [x] Release prisoner functionality
- [x] Pardon criminal functionality
- [x] View character info
- [x] Courtroom validation
- [x] Status indicators
- [x] HugsLib logging integration
- [x] Translation support

### ⏳ Placeholder Features (Ready for Implementation)
- [ ] Actual hearing scheduling system
- [ ] Trial process
- [ ] Sentence calculation
- [ ] Judge assignment
- [ ] Courtroom marker building/designation

### 🎨 Customization Opportunities
- [ ] Custom icon for hotbar button (currently uses placeholder)
- [ ] Enhanced courtroom requirements
- [ ] Additional crime statistics
- [ ] Graphical crime timeline
- [ ] Faction crime tracking

## Testing the Implementation

### Basic Testing
1. Start a game with the mod loaded
2. Verify "Justice" button appears on hotbar
3. Press `J` or click button to open window
4. Record some test crimes:
   ```csharp
   // In dev console or test code
   CrimeUtils.RecordCrime(somePawn, CrimeType.Assault, victimPawn, damageDealt: 10f);
   ```
5. Open Justice window - pawn should appear
6. Click pawn - crimes should display
7. Test action buttons

### Courtroom Testing
1. Build a room with:
   - At least 10 cells
   - Some chairs/stools
2. Select a criminal
3. Click "Schedule Hearing"
4. Should show success message
5. Without courtroom, should show error

### Search Testing
1. Have multiple criminals
2. Type partial name in search box
3. List should filter

## Code Quality

### Design Patterns Used
- **Separation of Concerns**: UI logic separate from business logic
- **Utility Classes**: Reusable helper methods
- **Translation Keys**: All text externalized for localization
- **Documentation**: Comprehensive inline comments

### Error Handling
- Null checks throughout
- Try-catch in critical methods
- User-friendly error messages
- Dev mode logging for debugging

### Performance
- Efficient LINQ queries
- Minimal allocations in render loop
- Scroll view optimization
- Ready for caching implementation (documented)

## Next Steps

### Immediate (Can be implemented now)
1. **Replace placeholder icon**
   - Create 64x64 PNG icon
   - Place in `Texture/UI/Commands/`
   - Update MainButtons.xml

2. **Enhance courtroom detection**
   - Define specific furniture requirements
   - Add room role/designation system
   - Create courtroom marker building

3. **Add crime statistics**
   - Total crimes by type
   - Crime trends over time
   - Most common victims

### Short-term (Extend current features)
1. **Implement hearing system**
   - Create Hearing class
   - Schedule hearing jobs
   - Courtroom assignment
   - Participant selection (judge, witnesses)

2. **Add trial process**
   - Evidence presentation
   - Witness testimonies
   - Verdict calculation
   - Sentence determination

3. **Implement sentencing**
   - Prison time calculation
   - Fines/restitution
   - Community service
   - Execution scheduling

### Long-term (New systems)
1. **Legal skill system**
   - Lawyer pawns
   - Defense attorneys
   - Skill affects trial outcomes

2. **Evidence system**
   - Crime scene investigation
   - Evidence quality
   - Witness reliability

3. **Appeals process**
   - Review sentences
   - Overturn convictions
   - Modify sentences

## Usage Example

Here's a complete example of using the Justice UI:

```csharp
// 1. Crime occurs during raid
[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
public static class TrackRaiderAssaults
{
    static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        Pawn victim = dinfo.IntendedTarget as Pawn;
        Pawn attacker = dinfo.Instigator as Pawn;

        if (victim?.IsColonist == true && attacker?.HostileTo(Faction.OfPlayer) == true)
        {
            // Record the crime
            CrimeUtils.RecordCrime(
                attacker,
                victim.Dead ? CrimeType.Murder : CrimeType.Assault,
                victim,
                damageDealt: totalDamageDealt
            );
        }
    }
}

// 2. Player captures the raider
// Raider is now imprisoned

// 3. Player opens Justice UI (press J)
// Raider appears in "Imprisoned" tab

// 4. Player selects raider
// UI shows:
//   - Status: Imprisoned
//   - Crimes: Assault (or Murder)
//   - Victim name
//   - Damage dealt

// 5. Player clicks "Schedule Hearing"
// System checks for courtroom
// If exists: "Hearing scheduled for [Raider Name]"
// If not: "Cannot schedule hearing: No courtroom available"

// 6. Player can also:
//   - Release (converts to guest)
//   - Pardon (removes all crimes)
//   - View Info (opens character card)
```

## Summary

The Justice UI system is **fully functional** for viewing and managing criminals. The core infrastructure is complete and ready for extension. The placeholder features (hearing scheduling, trials, sentencing) have clear integration points and documentation for implementation.

**Key Achievement**: Players can now see all criminals in one place, review their crimes, and take actions through a clean, intuitive interface - exactly like the quest window!
