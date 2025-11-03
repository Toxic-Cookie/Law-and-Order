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

### [Section 6: Grace Period & Release System](#section-6-grace-period-release-system) (Added Nov 2025)
- [Overview](#grace-period-overview)
- [Grace Period Mechanics](#grace-period-mechanics)
- [Auto-Emancipation](#auto-emancipation)
- [Mood and Faction Effects](#grace-period-effects)
- [Pardon Integration](#pardon-integration)

### [Section 7: Debt Stress System](#section-7-debt-stress-system) (Added Nov 2025)
- [Overview](#debt-stress-overview)
- [Stress Stages](#debt-stress-stages)
- [Thought Worker Implementation](#debt-stress-worker)
- [Balance Impact](#debt-stress-balance)

### [Section 8: Social Interaction System](#section-8-social-interaction-system)
- [Overview](#social-interaction-overview)
- [Court Interactions](#court-interactions)
- [Implementation](#social-interaction-implementation)
- [Timing and Progression](#interaction-timing)

### [Section 9: Logging System](#section-9-logging-system)
- [Tiered Logging](#tiered-logging)
- [Usage Patterns](#logging-usage-patterns)
- [Best Practices](#logging-best-practices)

### [Section 10: Quick References](#section-10-quick-references)
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

## Section 6: Grace Period & Release System

**Added:** November 2, 2025
**Status:** ✅ Fully implemented and tested

### Grace Period Overview

The grace period system incentivizes releasing enslaved debtors after they've paid their debt, rather than keeping them permanently enslaved. It provides a 10-day grace period for releasing debt-free slaves before penalties apply.

**Key Features:**
- 10-day grace period after debt is paid
- Automatic emancipation queuing
- Mood penalties for both colonists and slaves if grace period expires
- Faction relation bonuses for timely release
- Alert system for overdue releases
- Integrated with pardon system

**Core Files:**
- `Source/Hediffs/Hediff_Debt.cs` - Grace period tracking
- `Source/Components/WorldComponent_DebtManager.cs` - Auto-emancipation and faction relations
- `Source/Alerts/Alert_UnreleasedDebtors.cs` - Overdue release notifications
- `Source/Patches/SlaveEmancipation_Patch.cs` - Faction relation triggers
- `Defs/ThoughtDefs/Thoughts_DebtRelease.xml` - Mood effects

---

### Grace Period Mechanics

#### Tracking System (`Hediff_Debt.cs`)

The debt hediff tracks when debt reaches zero and manages the grace period:

```csharp
// Grace period tracking fields
private int debtPaidTick = -1;         // When debt reached 0
private bool emancipationQueued = false; // Auto-emancipation status

private const int GRACE_PERIOD_DAYS = 10;
private const int GRACE_PERIOD_TICKS = GRACE_PERIOD_DAYS * GenDate.TicksPerDay;

// Properties
public int TicksSinceDebtPaid { get; } // Time since debt paid
public bool IsInGracePeriod { get; }   // Within 10-day window
public bool IsOverdueForRelease { get; } // Grace period expired
```

**When Debt is Paid:**
```csharp
float amountPaid = debtRecord.PayDebt(amount, "Slave Labor");

// Automatically triggers grace period timer if debt reaches 0
if (debtRecord.CurrentDebt <= 0)
{
    // debtPaidTick is set automatically
    // Grace period begins
}
```

#### Grace Period Timeline

```
Day 0:   Debt reaches 0 → Grace period starts
         - Notification sent to player
         - Auto-queued for emancipation
         - Catharsis mood buff applied

Days 1-9: Grace period (no penalties)
         - Player has time to release
         - No negative effects

Day 10+: Grace period expired
         - Alert appears
         - Colonists: -3 mood (persistent)
         - Slave: -6 mood (rebellion risk!)
         - Continues until release

Release: Faction relations improve
         - 0-2 days: +15 goodwill
         - 3-10 days: +10 goodwill
         - 11-20 days: +5 goodwill
         - 21+ days: -5 goodwill
```

---

### Auto-Emancipation

#### Automatic Queuing (`WorldComponent_DebtManager.cs`)

When debt is paid off through labor, the system automatically queues emancipation:

```csharp
public void HandleDebtFullyPaid(Pawn slave)
{
    var debtRecord = DebtUtils.TryGetDebtRecord(slave);
    if (debtRecord == null) return;

    // Send notification
    Messages.Message(
        $"{slave.LabelShort} has fully paid off their debt. " +
        "They will be released in 10 days unless you intervene.",
        slave,
        MessageTypeDefOf.PositiveEvent
    );

    // Apply positive mood
    slave.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.Catharsis);

    // Auto-queue emancipation (one-time only)
    if (!debtRecord.EmancipationQueued)
    {
        AutoQueueEmancipation(slave, debtRecord);
    }
}

private void AutoQueueEmancipation(Pawn slave, Hediff_Debt debtRecord)
{
    // Set slave interaction mode to Emancipate
    slave.guest.slaveInteractionMode = SlaveInteractionModeDefOf.Emancipate;
    debtRecord.EmancipationQueued = true;

    Messages.Message(
        $"{slave.LabelShort} has been queued for emancipation.",
        slave,
        MessageTypeDefOf.NeutralEvent
    );
}
```

**Player Options:**
- Let emancipation proceed (default)
- Cancel emancipation (slave remains enslaved)
- Release immediately (faction bonus)

---

### Mood and Faction Effects

#### Alert System (`Alert_UnreleasedDebtors.cs`)

Medium-priority alert appears when grace period expires:

```csharp
public class Alert_UnreleasedDebtors : Alert
{
    private List<Pawn> UnreleasedDebtors
    {
        get
        {
            // Find all slaves with expired grace period
            foreach (Pawn slave in map.mapPawns.SlavesOfColonySpawned)
            {
                var debtRecord = DebtUtils.TryGetDebtRecord(slave);
                if (debtRecord != null && debtRecord.IsOverdueForRelease)
                {
                    yield return slave;
                }
            }
        }
    }
}
```

**Alert Content:**
- Lists all overdue slaves
- Shows days since debt paid
- Explains consequences (mood, relations, rebellion)

#### Colonist Mood Effects

**Released on Time** (`LawAndOrder_ReleasedDebtor`):
- Memory thought, lasts 5 days
- +2 mood: "Released debt-free prisoner"
- Stacks up to 3 times
- Applies to all colonists

**Holding Past Grace Period** (`LawAndOrder_HoldingDebtFreeSlave`):
- Situational thought, persistent
- -3 mood: "Enslaving freed debtors"
- Active when ANY slave is overdue
- Affects all colonists

```csharp
public class ThoughtWorker_HoldingDebtFreeSlave : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.IsColonist || p.IsSlave) return false;

        // Check all maps for overdue slaves
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn slave in map.mapPawns.SlavesOfColonySpawned)
            {
                var debtRecord = DebtUtils.TryGetDebtRecord(slave);
                if (debtRecord != null && debtRecord.IsOverdueForRelease)
                {
                    return true; // Found at least one
                }
            }
        }
        return false;
    }
}
```

#### Slave Mood Effects

**Debt Paid, Still Enslaved** (`LawAndOrder_DebtPaidButEnslaved`):
- Situational thought, persistent
- -6 mood: "Debt paid, still enslaved"
- High rebellion risk
- Active until released

```csharp
public class ThoughtWorker_DebtPaidButEnslaved : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!p.IsSlave) return false;

        var debtRecord = DebtUtils.TryGetDebtRecord(p);
        return debtRecord != null && debtRecord.IsOverdueForRelease;
    }
}
```

#### Faction Relations (`SlaveEmancipation_Patch.cs`)

Harmony patch detects when slaves are released and adjusts relations:

```csharp
[HarmonyPatch(typeof(GenGuest), "SlaveRelease")]
public static class SlaveEmancipation_Patch
{
    static void Postfix(Pawn p)
    {
        var debtRecord = DebtUtils.TryGetDebtRecord(p);
        if (debtRecord != null && debtRecord.TicksSinceDebtPaid > 0)
        {
            int daysOverdue = debtRecord.TicksSinceDebtPaid / GenDate.TicksPerDay;

            var debtManager = Find.World.GetComponent<WorldComponent_DebtManager>();
            debtManager?.OnDebtorEmancipated(p, daysOverdue);
        }
    }
}
```

**Relation Changes:**
```csharp
public void OnDebtorEmancipated(Pawn freedPawn, int daysOverdue)
{
    int relationChange = 0;

    if (daysOverdue <= 2)        relationChange = 15;  // Prompt
    else if (daysOverdue <= 10)  relationChange = 10;  // Grace period
    else if (daysOverdue <= 20)  relationChange = 5;   // Late
    else                         relationChange = -5;  // Very late

    freedPawn.Faction.TryAffectGoodwillWith(Faction.OfPlayer, relationChange);

    // Give colonists mood buff if released on time
    if (daysOverdue <= 10)
    {
        foreach (Pawn colonist in colonists)
        {
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                DefDatabase<ThoughtDef>.GetNamed("LawAndOrder_ReleasedDebtor")
            );
        }
    }
}
```

---

### Pardon Integration

#### Pardon Triggers Grace Period (`MainTabWindow_Justice.cs`)

When a player pardons an enslaved pawn, it triggers all grace period mechanics:

```csharp
private void PardonCriminal()
{
    var debtRecord = DebtUtils.TryGetDebtRecord(selectedCriminal);
    if (debtRecord != null && debtRecord.CurrentDebt > 0)
    {
        // Pay off debt (triggers grace period)
        debtRecord.PayDebt(debtRecord.CurrentDebt, "Pardoned by colony");

        // If enslaved, trigger grace period immediately
        if (selectedCriminal.IsSlaveOfColony)
        {
            var debtManager = Find.World.GetComponent<WorldComponent_DebtManager>();
            debtManager?.HandleDebtFullyPaid(selectedCriminal);
        }
    }

    // Remove criminal record
    selectedCriminal.health.RemoveHediff(criminalRecord);
}
```

**Pardon Effects:**
1. All debt paid with reason "Pardoned by colony"
2. Grace period starts (10 days)
3. Notification sent to player
4. Auto-queued for emancipation
5. Catharsis mood buff applied
6. Criminal record removed
7. Debt hediff remains (historical record)

---

### Usage Examples

#### Example 1: Natural Debt Repayment

```csharp
// Slave works off debt gradually
var debtRecord = DebtUtils.TryGetDebtRecord(slave);
debtRecord.CurrentDebt; // 500 silver

// Each day, WorldComponent_DebtManager processes payment
ProcessSlaveDebtPayment(slave); // -35 silver/day (average)

// After ~14 days, debt reaches 0
// Automatically:
// - debtPaidTick set to current tick
// - Grace period begins
// - Player notified
// - Emancipation queued
```

#### Example 2: Player Pardons

```csharp
// Player clicks "Pardon" button in Justice UI
PardonCriminal();

// Immediately:
// - Debt paid: "Pardoned by colony"
// - Grace period starts
// - Emancipation queued
// - Criminal record removed
```

#### Example 3: Grace Period Expires

```csharp
// 10+ days after debt paid, still enslaved
var debtRecord = DebtUtils.TryGetDebtRecord(slave);
debtRecord.IsOverdueForRelease; // true

// Automatically:
// - Alert appears
// - Colonists: -3 mood
// - Slave: -6 mood (rebellion risk!)
```

#### Example 4: Timely Release

```csharp
// Player releases within grace period (day 5)
GenGuest.SlaveRelease(slave);

// Automatically:
// - Faction: +10 goodwill
// - All colonists: +2 mood for 5 days
// - Alert clears
```

---

### Bug Fixes

#### Fix 1: Harmony Patch Parameter Mismatch
**Issue:** Patch used `slave` but method uses `p`
**Fix:** Match parameter names exactly

#### Fix 2: Void Method Return Capture
**Issue:** Tried to capture `__result` from void method
**Fix:** Remove `__result` parameter

#### Fix 3: Pardon Didn't Trigger Grace Period
**Issue:** Pardoning only removed crimes, not debt
**Fix:** Pay debt and call `HandleDebtFullyPaid()` on pardon

#### Fix 4: Premature Failure Messages
**Issue:** Failed enslavement without final attempt
**Fix:** Skip checks on retry 20+, make final attempt

---

## Section 7: Debt Stress System

### Debt Stress Overview

The debt stress system applies a **situational mood debuff** that scales with the amount of debt owed by prisoners and slaves. This creates psychological pressure and rebellion risk, naturally limiting exploitative "debt bomb" strategies where players assign excessive debt values.

**Purpose:**
- Prevents unlimited debt accumulation without consequences
- Creates meaningful rebellion risk for high-debt prisoners
- Balances contraband penalty system (can't spam huge penalties)
- Interacts naturally with ritual quality and grace period systems
- Forces players to balance profit extraction vs. managing mental breaks

**Key Features:**
- 8-stage situational thought (-1 to -8 mood)
- Scales at 250 silver per stage
- Only applies to prisoners and slaves
- Updates dynamically as debt changes
- Persists even when pawn is off-map

### Debt Stress Stages

The thought `LawAndOrder_DebtStress` has 8 stages based on current debt:

| Stage | Debt Range | Mood Effect | Label | Description |
|-------|------------|-------------|-------|-------------|
| 0 | 0-249 silver | None | (invisible) | No stress |
| 1 | 250-499 silver | -1 | minor debt stress | "I owe them money. I need to work this off." |
| 2 | 500-749 silver | -2 | debt stress | "The debt is weighing on me. How long will I be here?" |
| 3 | 750-999 silver | -3 | significant debt stress | "This debt feels crushing. Will I ever get out?" |
| 4 | 1000-1249 silver | -4 | heavy debt stress | "The debt is overwhelming. I'm trapped here." |
| 5 | 1250-1499 silver | -5 | severe debt stress | "I'll never pay this off. This is endless." |
| 6 | 1500-1749 silver | -6 | crushing debt stress | "The debt is unbearable. There's no escape." |
| 7 | 1750+ silver | -8 | impossible debt stress | "This debt is a death sentence. I'll die here." |

**Formula:** `stage = (int)(currentDebt / 250)`, capped at stage 7

**Design Rationale:**
- 250 silver per stage scales naturally with RimWorld economy
- Typical raider crimes result in 200-800 silver debt (stages 0-3)
- Stage 4 (1000 silver) = major decision point for players
- Cap at -8 prevents infinite scaling while still being severe
- Leaves room for other mood debuffs to matter

### Debt Stress Worker

**File:** `Source/Thoughts/ThoughtWorker_DebtStress.cs`

```csharp
public class ThoughtWorker_DebtStress : ThoughtWorker
{
    private const float DEBT_PER_STAGE = 250f;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        // Only prisoners and slaves feel debt stress
        if (!p.IsPrisonerOfColony && !p.IsSlaveOfColony)
            return ThoughtState.Inactive;

        var debtRecord = DebtUtils.TryGetDebtRecord(p);
        if (debtRecord == null || debtRecord.CurrentDebt <= 0)
            return ThoughtState.Inactive;

        // Calculate stage: 0-249 = stage 0, 250-499 = stage 1, etc.
        int stage = (int)(debtRecord.CurrentDebt / DEBT_PER_STAGE);

        // Cap at max stage (7 = 1750+ silver)
        if (stage > 7)
            stage = 7;

        return ThoughtState.ActiveAtStage(stage);
    }
}
```

**Key Implementation Details:**
1. **Eligibility Check:** Only prisoners and slaves are affected
2. **Debt Retrieval:** Uses `DebtUtils.TryGetDebtRecord()` to get hediff
3. **Stage Calculation:** Integer division by 250 silver
4. **Capping:** Maximum stage 7 prevents infinite scaling
5. **Inactive State:** Returns inactive if no debt or not eligible

### Debt Stress Balance

#### Rebellion Risk Analysis

**Mental Break Thresholds in RimWorld:**
- Minor mental break: 20% mood or below
- Major mental break: 10% mood or below
- Extreme mental break: 5% mood or below

**Debt Stress Impact:**

| Debt Amount | Mood Penalty | Rebellion Risk | Player Strategy |
|-------------|--------------|----------------|-----------------|
| 250 silver | -1 | Very Low | Safe, normal operations |
| 500 silver | -2 | Low | Manageable with decent conditions |
| 750 silver | -3 | Low-Moderate | Requires attention to other needs |
| 1000 silver | -4 | Moderate | High risk if base mood already low |
| 1250 silver | -5 | Moderate-High | Requires good conditions |
| 1500 silver | -6 | High | Difficult to prevent mental breaks |
| 2000+ silver | -8 | Very High | Extremely difficult to manage |

**Example Scenarios:**

**Scenario 1: Typical Raider (500 silver debt)**
- Base mood: 50%
- Debt stress: -2%
- Enslaved: -20%
- Final mood: ~28%
- **Result:** Manageable, unlikely to rebel

**Scenario 2: Heavy Contraband Offender (1500 silver debt)**
- Base mood: 50%
- Debt stress: -6%
- Enslaved: -20%
- Poor conditions: -10%
- Final mood: ~14%
- **Result:** High rebellion risk, requires excellent conditions

**Scenario 3: Debt Bomb Exploit Attempt (3000 silver debt)**
- Base mood: 50%
- Debt stress: -8% (capped)
- Enslaved: -20%
- Final mood: ~22%
- **Result:** Still risky, but cap prevents guaranteed rebellion

#### System Interactions

**With Ritual Quality (Phase 1):**
- High quality hearing → Faster repayment (1.4x speed) → Less time under stress
- Low quality hearing → Slower repayment (0.6x speed) → More time under stress
- **Impact:** Incentivizes quality hearings without creating profit exploits

**With Contraband Penalties (Phase 2):**
- Can't spam huge penalties (capped at 3x market value)
- But even capped penalties create stress
- **Impact:** Meaningful contraband enforcement without game-breaking debt

**With Grace Period (Phase 3):**
- Debt-free slaves released automatically
- Prevents permanent high-stress enslavement
- **Impact:** Natural exit strategy for completed debts

**Combined Example:**
1. Raider caught with 500 silver contraband
2. Low quality hearing: Debt reduced to 375 (-25%), but pays slowly (0.6x)
3. Debt stress: -2 mood initially
4. Takes ~18 days to pay off (slow repayment)
5. Grace period: Auto-released after 10 days
6. **Total time enslaved:** ~28 days with manageable stress

#### Anti-Exploit Mechanisms

**Prevents:**
1. **Debt Bombs:** Can't assign 10,000 silver penalties without rebellion
2. **Infinite Enslavement:** Stress + grace period forces releases
3. **Contraband Spam:** Each item adds stress proportionally
4. **Low Quality Farming:** Slow repayment = more time under stress

**Allows:**
1. **Meaningful Penalties:** 500-1000 silver debts are viable
2. **Player Choice:** Can risk high debt for valuable prisoners
3. **Story Moments:** Dramatic escapes from overwhelming debt
4. **Thematic Play:** Harsh colonies can still function (at a cost)

### Debt Stress Usage Examples

#### Example 1: Checking Current Stress Level

```csharp
// Get pawn's debt stress
var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
if (debtRecord != null && debtRecord.CurrentDebt > 0)
{
    int stage = (int)(debtRecord.CurrentDebt / 250f);
    stage = Math.Min(stage, 7);

    string stressLevel = stage switch
    {
        0 => "No stress",
        1 => "Minor stress",
        2 => "Moderate stress",
        3 => "Significant stress",
        4 => "Heavy stress",
        5 => "Severe stress",
        6 => "Crushing stress",
        7 => "Impossible stress",
        _ => "Unknown"
    };

    Log.Message($"{pawn.LabelShort} has {stressLevel} from {debtRecord.CurrentDebt} silver debt");
}
```

#### Example 2: Predicting Mental Break Risk

```csharp
// Calculate if pawn is at risk of mental break due to debt
public static bool IsAtRiskOfBreakFromDebt(Pawn pawn)
{
    if (!pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
        return false;

    var debtRecord = DebtUtils.TryGetDebtRecord(pawn);
    if (debtRecord == null || debtRecord.CurrentDebt <= 0)
        return false;

    // Calculate debt stress penalty
    int stage = Math.Min((int)(debtRecord.CurrentDebt / 250f), 7);
    float debtStressPenalty = stage switch
    {
        1 => 1f,
        2 => 2f,
        3 => 3f,
        4 => 4f,
        5 => 5f,
        6 => 6f,
        7 => 8f,
        _ => 0f
    };

    // Check if current mood minus debt stress would cause break
    float currentMood = pawn.needs?.mood?.CurLevel ?? 0f;
    float moodWithoutDebtStress = currentMood + (debtStressPenalty / 100f);

    // At risk if removing debt stress would raise mood above minor break threshold
    return currentMood < 0.2f && moodWithoutDebtStress > 0.2f;
}
```

#### Example 3: Optimizing Debt Assignment

```csharp
// Calculate maximum safe debt for a pawn
public static float GetMaximumSafeDebt(Pawn pawn, float targetMood = 0.3f)
{
    if (!pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony)
        return 0f;

    float currentMood = pawn.needs?.mood?.CurLevel ?? 0f;

    // Account for enslavement penalty
    currentMood -= 0.2f;

    // Calculate how much debt stress we can add before reaching target mood
    float availableMoodBuffer = currentMood - targetMood;

    // Each mood point = 250 silver (approximately)
    float maxDebt = availableMoodBuffer * 250f;

    return Math.Max(0f, maxDebt);
}
```

### Files Modified/Created

**Created (2 files):**
1. `Defs/ThoughtDefs/Thoughts_DebtStress.xml` - 8-stage thought definition
2. `Source/Thoughts/ThoughtWorker_DebtStress.cs` - Stage calculation logic

**XML Definition Location:**
```
Law and Order/
└── Defs/
    └── ThoughtDefs/
        └── Thoughts_DebtStress.xml
```

**C# Implementation Location:**
```
Law and Order/
└── Source/
    └── Thoughts/
        └── ThoughtWorker_DebtStress.cs
```

---

## Section 8: Social Interaction System

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

## Section 9: Logging System

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

## Section 10: Quick References

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
