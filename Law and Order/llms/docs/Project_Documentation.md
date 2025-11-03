# Law and Order - Technical Documentation

**Version:** 1.6
**For RimWorld:** 1.6.4628
**Last Updated:** November 2, 2025

---

## Table of Contents

### [Section 1: Crime Tracking System](#section-1-crime-tracking-system)
- [Overview](#crime-tracking-overview)
- [Core Components](#crime-tracking-core-components)
- [Usage Examples](#crime-tracking-usage-examples)
- [Integration with Harmony](#crime-tracking-harmony-integration)

### [Section 2: Debt Management System](#section-2-debt-management-system)
- [Overview](#debt-management-overview)
- [Debt Calculation](#debt-calculation)
- [Payment System](#payment-system)
- [Crime Archival](#crime-archival)

### [Section 3: Justice UI System](#section-3-justice-ui-system)
- [Main Justice Window](#main-justice-window)
- [Judiciary Inspector Tab](#judiciary-inspector-tab)
- [Courtroom Validation](#courtroom-validation)

### [Section 4: Court Hearing System](#section-4-court-hearing-system)
- [Courtroom Setup](#courtroom-setup)
- [Ritual System Integration](#ritual-system-integration)
- [Hearing Quality Mechanics](#hearing-quality-mechanics) (Updated Nov 2025)

### [Section 5: Contraband System](#section-5-contraband-system)
- [Overview](#contraband-overview)
- [Penalty Caps](#contraband-penalty-caps) (Added Nov 2025)
- [Usage Examples](#contraband-usage-examples)

### [Section 6: Social Interaction System](#section-6-social-interaction-system)
- [Overview](#social-interaction-overview)
- [Court Interactions](#court-interactions)
- [Implementation](#social-interaction-implementation)
- [Timing and Progression](#interaction-timing)

### [Section 7: Logging System](#section-7-logging-system)
- [Tiered Logging](#tiered-logging)
- [Usage Patterns](#logging-usage-patterns)
- [Best Practices](#logging-best-practices)

### [Section 8: Quick References](#section-8-quick-references)
- [Crime System Quick Reference](#crime-quick-reference)
- [UI Quick Reference](#ui-quick-reference)
- [Logging Quick Reference](#logging-quick-reference)

---

## Section 1: Crime Tracking System

### Crime Tracking Overview

The crime tracking system uses a custom `Hediff_Crimes` hediff to efficiently store crime data per-pawn. This approach:
- Only stores data for pawns that have committed crimes
- Automatically saves/loads with the game
- Doesn't bloat the save file for pawns without crimes
- Provides easy querying and filtering of crime data
- Automatically archives old crimes (60+ days) to prevent save file bloat

### Crime Tracking Core Components

#### 1. Crime Class (`Crime.cs`)

Represents a single criminal act with:
- `CrimeType` - The type of crime (Assault, Murder, etc.)
- `tickCommitted` - When the crime occurred
- `victim` - The victim pawn (if applicable)
- `targetThing` - The target object (if applicable)
- `damageDealt` - Amount of damage dealt
- `additionalInfo` - Any extra context

#### 2. CrimeType Enum

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

#### 3. Hediff_Crimes

The main hediff that stores a list of crimes. Key features:
- Automatically saves/loads with game state
- Only exists on pawns that have committed crimes
- Provides methods to query, filter, and manage crimes
- Supports automatic archival of old crimes

Methods:
- `AddCrime(Crime crime)` - Record a new crime
- `GetCrimesByType(CrimeType type)` - Filter by crime type
- `GetRecentCrimes(int days)` - Get crimes from last X days
- `GetCrimesAgainstVictim(Pawn victim)` - Filter by victim
- `ClearCrimesOlderThan(int days)` - Manual cleanup
- `ArchiveOldCrimes(int daysOld = 60)` - Archive old crimes to summary

#### 4. CrimeUtils

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

#### 5. Crime Archival System

**Purpose:** Prevents save file bloat by archiving old crimes into summaries.

**How it works:**
- Crimes older than 60 days (configurable) are automatically archived
- Archives store: count, total debt, date range, crime type breakdown
- Original detailed crime records are deleted after archival
- Archives displayed in hediff tooltip
- Processed daily by WorldComponent_DebtManager

**Example:**
```csharp
// Archived automatically, or manually:
criminalRecord.ArchiveOldCrimes(60); // Archive crimes older than 60 days

// Archive data structure:
public class CrimeSummary
{
    public int crimeCount;
    public int totalDebt;
    public int earliestTick;
    public int latestTick;
    public Dictionary<CrimeType, int> crimeTypeBreakdown;
}
```

### Crime Tracking Usage Examples

#### Recording Crimes

**Simple Crime Recording:**
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

**Advanced Crime Recording with Details:**
```csharp
CrimeUtils.RecordCrime(
    criminal: raider,
    crimeType: CrimeType.Murder,
    victim: colonist,
    damageDealt: 45.0f,
    additionalInfo: "Killed with charge rifle during raid"
);
```

#### Querying Crimes

**Get All Crimes for a Pawn:**
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    foreach (var crime in record.Crimes)
    {
        ModLog.Message($"Crime: {crime}");
    }
}
```

**Filter Crimes by Type:**
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    var murders = record.GetCrimesByType(CrimeType.Murder);
    var assaults = record.GetCrimesByType(CrimeType.Assault);

    ModLog.Message($"Committed {murders.Count} murders and {assaults.Count} assaults");
}
```

**Get Recent Crimes:**
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    var recentCrimes = record.GetRecentCrimes(7); // Last 7 days
    var monthlyCrimes = record.GetRecentCrimes(30); // Last 30 days
}
```

**Get Crimes Against Specific Victim:**
```csharp
var record = CrimeUtils.TryGetCriminalRecord(prisoner);
if (record != null)
{
    var crimesAgainstBob = record.GetCrimesAgainstVictim(bobThePawn);
    ModLog.Message($"Committed {crimesAgainstBob.Count} crimes against Bob");
}
```

### Crime Tracking Harmony Integration

**Example: Track Damage Events**
```csharp
[HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
public static class TrackDamage_Patch
{
    static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
    {
        try
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
        catch (Exception e)
        {
            ModLog.Error($"Error in TrackDamage_Patch: {e}");
        }
    }
}
```

**Example: Track Theft**
```csharp
[HarmonyPatch(typeof(Pawn_InventoryTracker), "TryAddItemNotForSale")]
public static class TrackTheft_Patch
{
    static void Postfix(Pawn_InventoryTracker __instance, Thing item, bool __result)
    {
        try
        {
            if (!__result) return;

            Pawn pawn = __instance.pawn;

            if (item.Faction == Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer))
            {
                CrimeUtils.RecordCrime(
                    criminal: pawn,
                    crimeType: CrimeType.Theft,
                    targetThing: item
                );
            }
        }
        catch (Exception e)
        {
            ModLog.Error($"Error in TrackTheft_Patch: {e}");
        }
    }
}
```

### HediffDef Configuration

The hediff is defined in `Defs/HediffDefs/Hediff_Crimes.xml`:
- **defName**: `LawAndOrder_CriminalRecord`
- **visible**: `false` (doesn't show in health tab by default)
- **isBad**: `true` (marked as negative)
- **ShouldRemove**: `false` (persists until manually cleared)

To make the hediff visible in the health tab, change `<visible>false</visible>` to `<visible>true</visible>`.

### Performance Considerations

1. **Minimal Save File Impact**: Only pawns with crimes get the hediff
2. **Efficient Queries**: Use LINQ-based filtering methods
3. **Automatic Cleanup**: Crimes older than 60 days are archived automatically
4. **DefOf Usage**: Uses RimWorld's DefOf system for fast def lookups

### Best Practices

1. **Only track meaningful crimes**: Don't track every minor action
2. **Trust automatic archival**: System handles old crime cleanup
3. **Use appropriate crime types**: Pick the most specific crime type
4. **Add context**: Use `additionalInfo` field for important details
5. **Check for null**: Always null-check pawns before recording crimes
6. **Error handling**: Wrap crime recording in try-catch blocks

---

## Section 2: Debt Management System

### Debt Management Overview

The debt management system tracks financial debt owed by criminals and processes payment through slave labor.

**Key Features:**
- Hediff-based debt tracking
- Automatic debt calculation from crimes
- Debt payment through slave labor
- Manual payment support
- Payment history tracking
- Save/load compatible

### Debt Calculation

**Automatic Debt from Crimes:**
```csharp
// Assault: 50-150 silver based on damage
// Murder: 500-1000 silver based on pawn value
// Theft: Item market value × 1.5
// Property Destruction: Building cost × 1.2
// Kidnapping: Kidnapped pawn's market value × 2.5
```

**Manual Debt Addition:**
```csharp
DebtUtils.AddDebt(pawn, 500f, "Custom fine");
```

**Debt Modification:**
```csharp
// Increase debt
DebtUtils.AddDebt(pawn, 100f, "Additional penalty");

// Reduce debt
DebtUtils.PayDebt(pawn, 50f, "Partial payment");
```

### Payment System

#### Automatic Slave Labor Payments

**How it Works:**
1. After court hearing, prisoners with debt are automatically enslaved
2. WorldComponent_DebtManager processes payments once per day
3. Payment amount calculated based on slave capabilities
4. Debt reduced until fully paid
5. Player notified when debt complete

**Payment Calculation:**

Base Payment: **35 silver/day**

**Multipliers:**
1. **Health Efficiency** (consciousness, downed status)
   - Healthy: 1.0x
   - Injured (80% consciousness): 0.8x
   - Downed: 0x

2. **Skill Level** (average of work skills)
   - Low (0-4): 0.7x
   - Medium (5-9): 1.0x
   - High (10-14): 1.3x
   - Expert (15+): 1.6x

3. **Traits**
   - Lazy/Slothful: 0.7x
   - Hard worker: 1.3x

4. **Suppression Level**
   - Formula: 50% base + (suppression × 50%)
   - Fully suppressed (100%): 1.0x
   - Rebellious (0%): 0.5x

**Example Calculation:**
```
Slave with:
- 8 average skill (Medium) = 1.0x
- 90% health = 0.9x
- Hard worker trait = 1.3x
- 60% suppression = 0.8x

Daily payment = 35 × 1.0 × 0.9 × 1.3 × 0.8 = 32.76 silver/day
```

#### Manual Payments

```csharp
// Manual payment
var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
if (debtRecord != null)
{
    debtRecord.PayDebt(100f, "Manual payment");
}
```

### Crime Archival

**Automatic Archival System:**
- Processes crimes older than 60 days (DEFAULT_ARCHIVE_AGE_DAYS)
- Runs daily via WorldComponent_DebtManager
- Archives store summary data: count, debt, date range, breakdown
- Original detailed records deleted to save space
- Fully serializable for save/load

**Archive Data:**
```csharp
public class CrimeSummary
{
    public int crimeCount;           // Total crimes archived
    public int totalDebt;            // Total debt from archived crimes
    public int earliestTick;         // First crime date
    public int latestTick;           // Last crime date
    public Dictionary<CrimeType, int> crimeTypeBreakdown; // Crimes by type
}
```

**Benefits:**
- Prevents save file bloat in long-running colonies
- 100+ crime records → compact summary
- Maintains historical record for display
- No performance impact on gameplay

---

## Section 3: Justice UI System

### Main Justice Window

**Access:**
- Click "Justice" button on hotbar
- Press `J` hotkey

**Layout:**
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
│                  │                                       │
│                  │ [Schedule Hearing]  [Release]        │
│                  │ [Pardon]           [View Info]       │
└──────────────────┴──────────────────────────────────────┘
```

**Features:**

**Three Tabs:**
- **Active Criminals**: Living criminals not in prison
- **Imprisoned**: Criminals currently imprisoned
- **Historical**: Deceased criminals

**Criminal List:**
- Scrollable list with portraits
- Search/filter by name
- Crime count display
- Status indicators

**Detail Panel:**
- Full name and status
- Trial status
- Complete crime list with:
  - Crime type
  - Victim (if applicable)
  - Damage dealt
  - Days since committed
  - Additional information

**Action Buttons:**
1. **Schedule Hearing** - Validates courtroom, opens ritual dialog
2. **Release** - Converts prisoner to guest (imprisoned only)
3. **Pardon** - Removes all crimes permanently
4. **View Info** - Opens character info card

### Judiciary Inspector Tab

**Access:** Click on pawn, select "Judiciary" tab

**Displays:**
- Criminal record summary
- Current debt amount
- Debt payment history
- Hearing status and details
- Crimes committed with full details

**Localization:**
- All UI strings localized in `LawAndOrder_Keys.xml`
- Ready for translation to other languages

### Courtroom Validation

**Requirements for Valid Courtroom:**
- At least 10 cells of space (MIN_COURTROOM_SIZE)
- At least one piece of sittable furniture
- Must be indoors
- Must have designated Judge's Bench

**CourtroomUtils Methods:**
```csharp
// Quick check
bool hasCourtroom = CourtroomUtils.HasCourtroom();

// Get all valid courtrooms
List<Room> courtrooms = CourtroomUtils.GetPotentialCourtrooms();

// Validate specific room
bool isValid = CourtroomUtils.IsValidCourtroom(room);

// Get quality score
float quality = CourtroomUtils.GetCourtroomQuality(room);

// Get best courtroom (highest quality)
Room bestRoom = CourtroomUtils.GetBestCourtroom();
```

**Courtroom Caching:**
- Courtrooms cached per map for 2500 ticks (~1 minute)
- Cache automatically invalidated when:
  - Buildings spawn (construction/placement)
  - Buildings despawn (destruction/deconstruction)
  - Save game loaded
- Reduces performance impact on large maps
- Trace logging available for cache debugging

---

## Section 4: Court Hearing System

### Courtroom Setup

The Law and Order mod uses a **two-component system** for courtroom setup.

#### Component 1: Comp_JudgesBench (Table/Bench Designation)

**Purpose:** Marks a building (usually a table) as the Judge's Bench, which serves as the ritual target.

**Applied to:** Tables, desks, or similar furniture

**How it works:**
- Right-click on a table and select "Designate as Judge's Bench"
- Toggles boolean flag `isDesignatedAsJudgesBench`
- Ritual system searches for rooms containing designated Judge's Bench
- Becomes target location for court hearing rituals

**Code Location:** `Source/Buildings/Comp_JudgesBench.cs`

#### Component 2: CompCourtroomChair (Seat Role Assignment)

**Purpose:** Designates individual chairs/seats for specific courtroom roles.

**Applied to:** Chairs, stools, thrones, or other sittable furniture

**How it works:**
- Players can assign chairs to specific roles:
  - **Judge** - The adjudicator's seat
  - **Jury** - Seats for jury members (if used)
  - **Defendant** - Seat for the accused
  - **Victim** - Seat for crime victims (if participating)
  - **Spectator** - General audience seating
  - **Unassigned** - Default state

**Code Location:** `Source/Rituals/CourtroomChairRole.cs`

#### Setting Up a Courtroom

**Minimum Requirements:**
1. Build an enclosed room (walls and door)
2. Place a table and designate as Judge's Bench
3. Place chairs with roles:
   - Required: At least one Judge chair
   - Required: At least one Defendant chair
   - Optional: Victim, jury, and spectator chairs

**Recommended Layout:**
```
    +-----------------+
    |                 |
    |  [S] [S] [S]   |  S = Spectator chairs
    |                 |
    |  [J] [J] [J]   |  J = Jury chairs (optional)
    |                 |
    |     [D]         |  D = Defendant chair
    |                 |
    |  [===JB===]     |  JB = Judge's Bench (table)
    |     [Jg]        |  Jg = Judge chair
    |                 |
    |     [Door]      |
    +-----------------+
```

### Ritual System Integration

**Architecture:** Integrated with Custom Ritual Framework (CRF)

**Benefits:**
- Built-in prisoner escort mechanics
- Quality-based outcome system
- Proven ritual completion
- Extensible via XML

**Ritual Flow:**
```
User clicks "Begin Hearing"
    ↓
Gets/creates ritual precept in ideology
    ↓
Opens RimWorld ritual dialog
    ↓
Player assigns roles (judge, defendant, optional jury/victim)
    ↓
Ritual starts
    ↓
Stage 0: Judge escorts prisoner to courtroom
    ↓
Stage 1: Hearing proceedings (spectating)
    ↓
CRF calculates quality and selects outcome
    ↓
RitualBehaviorWorker_CourtHearing.PostCleanup()
    ↓
Detects outcome from memories
    ↓
Applies debt modifications
    ↓
Updates criminal record
    ↓
Sends player message
    ↓
Prisoner returned to cell
```

**Ritual Roles:**
1. **Judge** (required) - Has prisoner escort capability
2. **Defendant** (required) - Prisoner role
3. **Victim** (optional) - Crime victim
4. **Jury** (optional, max 12) - Jury members
5. **Spectators** (automatic) - Colony members

**Ritual Stages:**
- **Stage 0: Escort** (duration varies)
  - Judge picks up prisoner
  - Carries to courtroom
  - Prisoner delivered to Judge's Bench
- **Stage 1: Hearing** (2500-3500 ticks, ~1-2 hours)
  - All participants spectate
  - Ritual quality calculated
  - Ends at 100% duration

### Hearing Quality Mechanics

**Updated:** November 2, 2025 - System reframed to eliminate ritual sabotage exploit

**Outcomes:** 4 quality-based hearing outcomes

| Outcome | Positivity | Chance | Debt Change | Repayment Speed | Effects |
|---------|-----------|--------|-------------|-----------------|---------|
| **Poor Quality** | -2 | 10% | -25% | 0.6x (slow) | +20% suppression, -1.0 will, -1 mood (5 days) |
| **Partial Quality** | -1 | 25% | -15% | 1.0x (normal) | +10% suppression, -0.5 will, 0 mood (5 days) |
| **Standard Quality** | +1 | 50% | No change | 1.15x (faster) | +5% suppression, +1 mood (5 days) |
| **Excellent Quality** | +2 | 15% | +10% | 1.4x (fastest) | +2% suppression, +2 mood (5 days) |

**Key Balance Change:**
- **High quality hearings** now impose harsher sentences BUT slaves work them off faster (motivated)
- **Low quality hearings** now give lenient sentences BUT slaves work them off slower (demoralized)
- **Result:** Similar total labor value, but high quality = efficient, low quality = slow
- **Exploit eliminated:** No longer profitable to sabotage hearings

**Example (500 silver debt):**
- Excellent: 550 silver @ 49/day = ~11 days
- Poor: 375 silver @ 21/day = ~18 days

**Outcome Determination:**
1. CRF calculates ritual quality (0.0-1.0)
2. Quality factors: ritual seats, participant count, expectations
3. CRF selects outcome based on quality
4. CRF applies thought/memory to defendant
5. RitualBehaviorWorker detects which memory was applied
6. Maps memory to HearingOutcome enum (renamed from PleaBargainOutcome)
7. Applies debt modifications via HearingUtils
8. Daily repayment speed adjusted by WorldComponent_DebtManager

**Mood Effects:**
- **Defendant:** Receives outcome-specific thought
- **Judge:** +4 mood for 2 days (stackable up to 3)
- **Spectators:** +2 mood for 1 day (stackable up to 2)

**After Hearing:**
- If defendant has debt > 0, automatically enslaved
- Smart delay system waits for prisoner to settle
- Daily debt payments begin (modified by hearing quality)
- Player notified of enslavement and estimated days

---

## Section 5: Contraband System

### Contraband Overview

The contraband system allows players to designate specific items as contraband, which results in debt penalties when prisoners are found possessing these items.

**Key Features:**
- Global contraband definitions per game world
- Customizable silver penalties per item
- Automatic scanning when prisoners captured or downed hostiles killed
- Penalty caps to prevent exploitation
- Category-based bulk operations
- UI integration in Justice tab

**How it Works:**
1. Player designates items as contraband with silver penalty
2. System automatically scans prisoner inventories during capture
3. If contraband found, debt added to prisoner's record
4. Debt appears in judiciary tab and justice window
5. Penalties enforced through existing debt system

### Contraband Penalty Caps

**Added:** November 2, 2025 - Prevents exploitation of contraband penalties

**Problem Solved:**
Players could previously set absurd penalties (e.g., 10,000 silver for bread) with no consequences, leading to game-breaking exploitation.

**Solution:**
Contraband penalties are now capped at **3x the item's market value**, with a **minimum cap of 10 silver** for worthless items.

**Cap Rules:**
- **Normal items:** Maximum penalty = `item market value × 3`
- **Worthless items (0 value):** Minimum cap = `10 silver`
- **Cap notification:** User notified when attempted penalty exceeds cap
- **Automatic enforcement:** SetContraband() method validates all penalties

**Implementation Details:**

**Constants:**
```csharp
// WorldComponent_ContrabandManager.cs
private const float MAX_PENALTY_MULTIPLIER = 3.0f;
private const int MIN_PENALTY_CAP = 10;
```

**Validation Logic:**
```csharp
public void SetContraband(ThingDef thingDef, int silverPenalty)
{
    float marketValue = thingDef.BaseMarketValue;
    int maxPenalty = Mathf.RoundToInt(marketValue * MAX_PENALTY_MULTIPLIER);

    // Ensure minimum cap for worthless items
    if (maxPenalty < MIN_PENALTY_CAP)
    {
        maxPenalty = MIN_PENALTY_CAP;
    }

    if (silverPenalty > maxPenalty)
    {
        silverPenalty = maxPenalty;
        // User notification displayed
    }
    // ... rest of method
}
```

**Example Caps:**

| Item | Market Value | Maximum Penalty | Calculation |
|------|-------------|-----------------|-------------|
| Stone chunks | 0 silver | 10 silver | Minimum cap |
| Bread | 2 silver | 6 silver | 2 × 3 = 6 |
| Medicine | 18 silver | 54 silver | 18 × 3 = 54 |
| Gold | 10 silver | 30 silver | 10 × 3 = 30 |
| Uranium | 70 silver | 210 silver | 70 × 3 = 210 |

**UI Integration:**

The Justice window contraband tab displays the maximum penalty for selected items:
- **Worthless items:** "Maximum Penalty: 10 silver (min for worthless items)"
- **Normal items:** "Maximum Penalty: X silver (3x market value)"

**User Messages:**
- When cap applied to worthless item: "Item penalty capped at 10 silver (minimum cap for worthless items)"
- When cap applied to normal item: "Item penalty capped at X silver (3x market value of Y)"

**Balance Impact:**
- Prevents absurd penalties like 10,000 silver for bread
- Automatically scales with item value
- Allows meaningful penalties while maintaining balance
- Even worthless items can be penalized (prevents junk hoarding)
- Cannot be exploited for infinite debt generation

### Contraband Usage Examples

**Setting Contraband:**
```csharp
var manager = WorldComponent_ContrabandManager.Instance;

// Single item
manager.SetContraband(ThingDefOf.Smokeleaf, 50); // Will cap at marketValue × 3

// Bulk category operations
List<ThingDef> alcoholItems = GetAllAlcohol();
manager.SetContrabandBulk(alcoholItems, 100);
```

**Querying Contraband:**
```csharp
var manager = WorldComponent_ContrabandManager.Instance;

// Check if item is contraband
bool isContraband = manager.IsContraband(ThingDefOf.Beer);

// Get contraband definition
var contrabandDef = manager.GetContrabandDefinition(ThingDefOf.Beer);
if (contrabandDef != null)
{
    int penalty = contrabandDef.silverPenaltyPerItem;
}

// Calculate total contraband penalty for pawn
int totalPenalty = manager.CalculateContrabandPenalty(prisoner, out var itemBreakdown);
```

**Removing Contraband:**
```csharp
var manager = WorldComponent_ContrabandManager.Instance;

// Remove single item
manager.RemoveContraband(ThingDefOf.Smokeleaf);

// Remove category
List<ThingDef> drugs = GetAllDrugs();
manager.RemoveContrabandBulk(drugs);
```

**Penalty Cap Calculation Example:**
```csharp
ThingDef bread = ThingDefOf.MealSimple; // Market value: ~2 silver
int maxPenalty = Mathf.RoundToInt(bread.BaseMarketValue * 3.0f); // = 6 silver

// Attempt to set 10,000 silver penalty
manager.SetContraband(bread, 10000);
// Result: Capped at 6 silver, user notified

ThingDef stoneChunk = ThingDefOf.ChunkSandstone; // Market value: 0 silver
int minPenalty = 10; // Minimum cap applied

// Attempt to set any penalty
manager.SetContraband(stoneChunk, 50);
// Result: Capped at 10 silver (minimum cap)
```

**Files Modified:**
- `Source/Contraband/WorldComponent_ContrabandManager.cs` - Cap validation and enforcement
- `Source/UI/MainTabWindow_Justice.cs` - UI display of caps

---

## Section 6: Social Interaction System

### Social Interaction Overview

The court hearing system includes a **social interaction logging system** that records interactions between participants in the pawns' social logs. This creates a permanent record of the courtroom proceedings that players can view in the Social tab.

**Key Features:**
- Interactions appear in pawn social logs
- Logged at different stages throughout the ritual
- Includes judge, defendant, and victim interactions
- Grants skill XP to participants
- May affect pawn mood and relationships
- Permanent record viewable in Social tab

**Integration:**
- Uses RimWorld's native `PlayLogEntry_Interaction` system
- Fully compatible with vanilla social mechanics
- Interactions stored in Find.PlayLog
- Survives save/load cycles

### Court Interactions

#### Four Core Interactions

**1. Judge Questions Defendant** (`LawAndOrder_JudgeQuestions`)
- **Timing:** 25% ritual progress
- **Initiator:** Judge
- **Recipient:** Defendant
- **Effects:**
  - Judge gains 10 Social skill XP
  - No mood effects
  - No social fight chance
- **Log Variants:**
  - "Judge questioned Defendant about the charges"
  - "Judge examined Defendant's testimony"
  - "Judge interrogated Defendant in court"

**2. Defendant Pleads** (`LawAndOrder_DefendantPleads`)
- **Timing:** 50% ritual progress
- **Initiator:** Defendant
- **Recipient:** Judge
- **Effects:**
  - Defendant gains 15 Social skill XP
  - No mood effects
  - No social fight chance
- **Log Variants:**
  - "Defendant pleaded their case to Judge"
  - "Defendant made a plea for leniency before Judge"
  - "Defendant argued for mercy in front of Judge"

**3. Victim Confronts Defendant** (`LawAndOrder_VictimConfronts`)
- **Timing:** 75% ritual progress (if victim present)
- **Initiator:** Victim
- **Recipient:** Defendant
- **Effects:**
  - Victim: +6 mood for 3 days ("confronted criminal")
  - Defendant: -4 mood, -5 opinion of victim for 3 days
  - 2% social fight chance
- **Log Variants:**
  - "Victim confronted Defendant about their crimes"
  - "Victim spoke out against Defendant in court"
  - "Victim testified against Defendant"
  - "Victim accused Defendant before the court"

**4. Judge Sentences Defendant** (`LawAndOrder_JudgeSentences`)
- **Timing:** 100% ritual complete (PostCleanup)
- **Initiator:** Judge
- **Recipient:** Defendant
- **Effects:**
  - Judge gains 20 Social skill XP
  - No mood effects
  - No social fight chance
- **Log Variants:**
  - "Judge pronounced judgment on Defendant"
  - "Judge sentenced Defendant for their crimes"
  - "Judge delivered the court's verdict to Defendant"

### Social Interaction Implementation

#### System Architecture

```
RitualBehaviorWorker_CourtHearing
    ├── Tick() - Monitors ritual progress
    │   ├── 25% progress → Log judge questions
    │   ├── 50% progress → Log defendant pleads
    │   └── 75% progress → Log victim confronts (if present)
    └── PostCleanup() - Called when ritual ends
        └── 100% complete → Log judge sentences
```

#### Code Structure

**Interaction Tracking:**
```csharp
// RitualBehaviorWorker_CourtHearing.cs:16
private bool hasQuestionedDefendant = false;
private bool hasDefendantPleaded = false;
private bool hasVictimConfronted = false;
```

**Progress Monitoring:**
```csharp
// RitualBehaviorWorker_CourtHearing.cs:32
public override void Tick(LordJob_Ritual ritual)
{
    base.Tick(ritual);

    // Only during Stage 1 (the hearing)
    if (ritual.StageIndex != 1) return;

    float progress = ritual.Progress;

    // Trigger interactions at milestones
    if (progress >= 0.25f && !hasQuestionedDefendant)
    {
        LogInteraction("LawAndOrder_JudgeQuestions", judge, defendant);
        hasQuestionedDefendant = true;
    }
    // ... more milestones
}
```

**Interaction Logging:**
```csharp
// RitualBehaviorWorker_CourtHearing.cs:224
private void LogInteraction(string interactionDefName, Pawn initiator, Pawn recipient)
{
    InteractionDef interactionDef = DefDatabase<InteractionDef>.GetNamedSilentFail(interactionDefName);

    PlayLogEntry_Interaction entry = new PlayLogEntry_Interaction(
        interactionDef,
        initiator,
        recipient,
        null // extraSentencePacks
    );

    Find.PlayLog.Add(entry);
}
```

#### InteractionDef XML Structure

**Example: Judge Questions Defendant**
```xml
<InteractionDef>
  <defName>LawAndOrder_JudgeQuestions</defName>
  <label>question defendant</label>
  <symbol>UI/Icons/Rituals/CourtHearing</symbol>
  <ignoreTimeSinceLastInteraction>true</ignoreTimeSinceLastInteraction>
  <socialFightBaseChance>0</socialFightBaseChance>

  <!-- Judge gets Social XP -->
  <initiatorXpGainSkill>Social</initiatorXpGainSkill>
  <initiatorXpGainAmount>10</initiatorXpGainAmount>

  <!-- Log text from judge's perspective -->
  <logRulesInitiator>
    <rulesStrings>
      <li>r_logentry->[INITIATOR_nameIndef] questioned [RECIPIENT_nameIndef] about the charges.</li>
    </rulesStrings>
  </logRulesInitiator>

  <!-- Log text from defendant's perspective -->
  <logRulesRecipient>
    <rulesStrings>
      <li>r_logentry->[RECIPIENT_nameIndef] was questioned by [INITIATOR_nameIndef].</li>
    </rulesStrings>
  </logRulesRecipient>
</InteractionDef>
```

### Interaction Timing

#### Ritual Progress Breakdown

```
Stage 0: Escort (variable duration)
  └── Judge carries prisoner to courtroom

Stage 1: Hearing (2500-3500 ticks)
  ├── 0-25% progress - Opening proceedings
  ├── 25% progress ━━━━━━━━━━━━━━━━━━┓
  │                                  ┗→ Judge Questions Defendant
  ├── 25-50% progress - Examination
  ├── 50% progress ━━━━━━━━━━━━━━━━━┓
  │                                  ┗→ Defendant Pleads
  ├── 50-75% progress - Testimony
  ├── 75% progress ━━━━━━━━━━━━━━━━━┓
  │                                  ┗→ Victim Confronts Defendant (if present)
  └── 75-100% progress - Deliberation

PostCleanup (ritual complete)
  └── 100% ━━━━━━━━━━━━━━━━━━━━━━━━┓
                                   ┗→ Judge Sentences Defendant
```

#### Viewing Interactions

**In-Game:**
1. Select any participant (judge, defendant, or victim)
2. Open their character info
3. Click "Social" tab
4. Scroll to view logged interactions

**Example Display:**
```
Social Log - Alice

3 hours ago: Bob questioned Alice about the charges
2 hours ago: Alice pleaded their case to Bob
1 hour ago: Charlie confronted Alice about their crimes (-4 mood, -5 opinion)
Now: Bob sentenced Alice for their crimes
```

#### Benefits

**Gameplay:**
- Creates narrative history of court proceedings
- Affects pawn relationships over time
- Provides skill training opportunities
- Adds depth to justice system

**Immersion:**
- Pawns remember courtroom experiences
- Social dynamics reflect legal proceedings
- Victims get closure through confrontation
- Creates memorable colony stories

**Technical:**
- Uses vanilla systems (no custom UI)
- Lightweight (minimal performance impact)
- Persistent (survives save/load)
- Compatible with other mods

---

## Section 7: Logging System

### Tiered Logging

**ModLog System:** Provides 6 log levels with runtime configuration.

**Log Levels:**
1. **Trace** - Very verbose (method enter/exit, cache operations)
2. **Debug** - Development debugging (detailed state information)
3. **Info** - Normal informational messages (default)
4. **Warning** - Potential issues only
5. **Error** - Failures only
6. **None** - Silent (no logging)

**Configuration:**
- Accessible via mod settings menu
- Changes take effect immediately
- Persists across game sessions

**Usage:**
```csharp
// Use ModLog instead of Mod.Log
ModLog.Trace("Entering ComplexMethod()");
ModLog.Debug($"Processing {items.Count} items");
ModLog.Info("Court hearing completed successfully");
ModLog.Warning("Courtroom not found, using default");
ModLog.Error($"Failed to process hearing: {exception}");

// Check if logging enabled before expensive operations
if (ModLog.IsEnabled(LogLevel.Debug))
{
    string expensiveDebugInfo = BuildDetailedReport();
    ModLog.Debug(expensiveDebugInfo);
}
```

**Conditional Compilation:**

Verbose debug logging wrapped in `#if DEBUG` for Release build optimization:

```csharp
#if DEBUG
// Very verbose logging only in Debug builds
ModLog.Trace("Method entry");
ModLog.Trace($"Parameter value: {param}");
#endif

// Critical errors always logged
ModLog.Error("Something went wrong!");
```

### Logging Usage Patterns

**Method Entry/Exit Tracing:**
```csharp
public void ComplexMethod()
{
    ModLog.Trace("Entering ComplexMethod");

    try
    {
        // Complex logic
        ModLog.Trace("ComplexMethod completed successfully");
    }
    catch (Exception e)
    {
        ModLog.Error($"ComplexMethod failed: {e}");
        throw;
    }
}
```

**Null Check Logging:**
```csharp
if (pawn == null)
{
    ModLog.Warning("Attempted to process null pawn");
    return;
}
```

**Collection Logging:**
```csharp
ModLog.Debug($"Processing {crimes.Count} crimes");
foreach (var crime in crimes)
{
    if (ModLog.IsEnabled(LogLevel.Trace))
    {
        ModLog.Trace($"  - {crime}");
    }
}
```

**Harmony Patch Logging:**
```csharp
[HarmonyPostfix]
public static void Postfix(/* parameters */)
{
    try
    {
        // Patch logic

        ModLog.Debug("Patch executed successfully");
    }
    catch (Exception e)
    {
        ModLog.Error($"Error in patch: {e}");
        // Don't rethrow - let game continue
    }
}
```

### Logging Best Practices

**Do:**
- ✅ Log errors and warnings
- ✅ Use appropriate log levels
- ✅ Include context (pawn names, values, etc.)
- ✅ Wrap risky operations in try-catch
- ✅ Check IsEnabled() before expensive logging
- ✅ Use Trace for very detailed debugging
- ✅ Wrap verbose logging in #if DEBUG

**Don't:**
- ❌ Log every tick (performance impact)
- ❌ Log in hot paths without log level check
- ❌ Use vague messages like "Failed"
- ❌ Log sensitive information
- ❌ Include debug logging in Release builds

**Viewing Logs:**
1. In-game: Press `~` (tilde) for console
2. Full log: Press `Ctrl+F12`
3. Log file: `AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`

---

## Section 8: Quick References

### Crime Quick Reference

**Record a Crime:**
```csharp
CrimeUtils.RecordCrime(criminal, CrimeType.Assault, victim, damageDealt: 15f);
```

**Check Criminal Record:**
```csharp
bool hasCrimes = CrimeUtils.HasCriminalRecord(pawn);
int crimeCount = CrimeUtils.GetCrimeCount(pawn);
```

**Get Crimes:**
```csharp
var record = CrimeUtils.TryGetCriminalRecord(pawn);
var allCrimes = record?.Crimes;
var murders = record?.GetCrimesByType(CrimeType.Murder);
var recent = record?.GetRecentCrimes(7);
```

**Crime Types:**
- Assault, Murder, PropertyDestruction, Arson, Theft
- AnimalAbuse, Trespassing, Kidnapping, Vandalism

### UI Quick Reference

**Opening Windows:**
- Main Justice Window: Press `J` or click "Justice" on hotbar
- Judiciary Tab: Select pawn → "Judiciary" tab

**Justice Window Tabs:**
- Active Criminals - Living criminals not imprisoned
- Imprisoned - Criminals currently imprisoned
- Historical - Deceased criminals

**Actions:**
- Schedule Hearing - Requires valid courtroom
- Release - Converts prisoner to guest
- Pardon - Removes all crimes (permanent)
- View Info - Opens character info card

**Courtroom Requirements:**
- 10+ cells of space
- At least one chair/throne/stool
- Indoor room
- Designated Judge's Bench

### Logging Quick Reference

**Log Levels:**
```csharp
ModLog.Trace("Very verbose");      // Method enter/exit
ModLog.Debug("Debug info");        // Detailed state
ModLog.Info("Normal info");        // Default
ModLog.Warning("Potential issue"); // Non-critical
ModLog.Error("Failure");           // Critical errors
```

**Common Patterns:**
```csharp
// Null check
if (pawn == null)
{
    ModLog.Warning("Null pawn detected");
    return;
}

// Try-catch
try
{
    // Code
}
catch (Exception e)
{
    ModLog.Error($"Operation failed: {e}");
}

// Expensive logging
if (ModLog.IsEnabled(LogLevel.Debug))
{
    ModLog.Debug(BuildExpensiveString());
}
```

**View Logs:**
- In-game: `~` (tilde)
- Full log: `Ctrl+F12`
- File: `AppData\LocalLow\Ludeon Studios\RimWorld\Player.log`

---

## Performance Optimization Tips

**Crime Tracking:**
- Crimes automatically archived after 60 days
- Only pawns with crimes have hediff
- Efficient LINQ queries for filtering
- DefOf system for fast def lookups

**Courtroom Validation:**
- Results cached per map for 2500 ticks
- Automatic cache invalidation on structure changes
- Trace logging for cache debugging

**Logging:**
- Debug logging excluded from Release builds
- Use `IsEnabled()` before expensive operations
- Tiered logging lets users control verbosity

**Debt System:**
- WorldComponent ticks daily (not every tick)
- Efficient hediff-based storage
- Smart enslavement delays prevent issues

---

## Troubleshooting

**Crime not recording:**
- Check Harmony patches are active
- Verify crime meets conditions (e.g., victim is colonist)
- Check logs for errors

**Justice window blank:**
- Verify translation keys loaded
- Check for exceptions in log
- Ensure About.xml correct

**Courtroom validation fails:**
- Check room has 10+ cells
- Verify table designated as Judge's Bench
- Ensure at least one chair
- Room must be indoors

**Enslavement not working:**
- Check prisoner has debt > 0
- Verify no active Lord/job preventing enslavement
- Check logs for failure reasons
- Smart delay will retry up to 20 times

**Debt not decreasing:**
- Ensure slave is not downed
- Check slave work assignments
- Verify suppression level (affects payment)
- Check logs for payment processing

---

## API Reference for Modders

### Adding Custom Crime Types

```csharp
// Extend CrimeType enum (requires C# modification)
public enum CrimeType
{
    // ... existing types
    YourCustomCrime
}

// Add debt calculation
public static class DebtUtils
{
    public static float CalculateDebtForCrime(Crime crime)
    {
        switch (crime.crimeType)
        {
            // ... existing cases
            case CrimeType.YourCustomCrime:
                return CalculateCustomDebt(crime);
        }
    }
}
```

### Hooking into Debt Events

```csharp
// Listen for debt changes
public class YourDebtObserver : GameComponent
{
    public override void GameComponentTick()
    {
        // Check debt changes
        foreach (var pawn in allPawns)
        {
            var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
            if (debtRecord != null && debtRecord.HasDebtChanged)
            {
                OnDebtChanged(pawn, debtRecord);
            }
        }
    }
}
```

### Custom Courtroom Requirements

```csharp
public static class CourtroomUtils
{
    public static bool IsValidCourtroom(Room room)
    {
        // Add your custom requirements
        bool hasCustomFurniture = room.ContainedAndAdjacentThings
            .Any(t => t.def.defName == "YourCustomCourtFurniture");

        return hasCustomFurniture && /* other checks */;
    }
}
```

---

## Credits and License

**Development:** Law and Order Mod Team
**RimWorld Version:** 1.6.4628
**Last Updated:** November 2, 2025

**Dependencies:**
- RimWorld 1.6
- Harmony 2.4.1
- HugsLib 12.0.0
- Custom Ritual Framework (optional, for court hearings)

**Documentation compiled from:**
- CrimeTrackingSystem.md
- JusticeUISystem.md
- HugsLibLogging.md
- COURT_RITUAL_PHASES.md
- courtroom-setup-guide.md
- Various quick reference guides

---

**End of Documentation**
