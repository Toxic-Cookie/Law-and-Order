# Crime Visibility State Machine

## Overview

**Design Decision**: All crimes are always recorded internally, but visibility to the player and justice system depends on witness mechanics and investigation results.

**Date**: 2025-01-13

## The Three States

Every crime a pawn commits exists in one of three visibility states:

### 1. Hidden
**Definition**: Crime occurred but remains undetected by the justice system.

**Conditions**:
- No colonist had line of sight when crime occurred (FoW check failed)
- Crime happened during fast forward (no witnesses processed)
- Crime happened off-screen (outside colony area)
- Witnesses died before reporting
- Witness chose not to report (future feature)

**Behavior**:
- **NOT shown in UI** - player has no indication it happened
- Tracked internally in pawn's crime record
- Can be discovered through:
  - Confession during interrogation
  - Circumstantial evidence investigation
  - Accidental discovery
  - Other crimes revealing this one
- **Pawn can get away with it** if never discovered

**Example Scenarios**:
```
- Colonist steals silver while everyone is asleep (no witnesses)
- Raider murders a colonist in darkness with no one nearby
- Mental break vandalism in unmapped area
- Vampire drains blood from isolated victim
```

### 2. Suspected
**Definition**: Crime was witnessed and reported, case opened or amended.

**Conditions**:
- At least one colonist witnessed the crime (FoW check passed)
- Witness successfully reported to authorities
- Confession obtained during interrogation
- Evidence discovered linking pawn to crime

**Behavior**:
- **Shown in UI** with "Suspected" designation
- Case opened against pawn (or existing case amended)
- Only ONE case per pawn at a time (can have multiple crimes in one case)
- Pending investigation/trial
- Can still be dismissed if:
  - Evidence insufficient
  - False accusation discovered
  - Player decides to drop charges

**UI Display**:
```
[Pawn Name] - SUSPECTED
├─ Murder (2 witnesses, high confidence)
├─ Theft (1 witness, low confidence)
└─ Assault (0 witnesses, confession only)
```

**Example Scenarios**:
```
- Colonist seen punching another during tantrum
- Raider witnessed shooting colonist
- Thief caught on surveillance camera
- Suspect confesses to theft during interrogation
```

### 3. Convicted
**Definition**: Pawn has been found guilty and assigned consequences.

**Conditions**:
- Player manually convicted pawn
- Automatic conviction (mandate violations, caught red-handed)
- Trial resulted in guilty verdict
- **Can be FALSE CONVICTION** - wrong pawn convicted

**Behavior**:
- **Shown in UI** with "Convicted" designation
- Punishment assigned and pending/in-progress
- Crime record permanent
- Social consequences active:
  - Victim satisfaction/dissatisfaction
  - Witnesses react to verdict
  - Colony-wide mood effects

**UI Display**:
```
[Pawn Name] - CONVICTED
├─ Murder → Death Sentence (pending)
├─ Theft → 5 days imprisonment (day 2/5)
└─ Assault → Beating (completed)
```

**Example Scenarios**:
```
- Murderer convicted and sentenced to execution
- Thief convicted to imprisonment
- Innocent pawn falsely convicted of vampire's murder
- Raider convicted of war crimes
```

## State Transitions

### State Diagram

```
┌─────────┐
│ Hidden  │──┐
└─────────┘  │
     │       │
     │ Witnessed / Confession
     │       │
     ▼       │
┌───────────┐│
│ Suspected ││
└───────────┘│
     │       │
     │ Convicted
     │       │
     ▼       │
┌───────────┐│
│ Convicted ││
└───────────┘│
     │       │
     │ (No backward transitions)
     ▼       ▼
```

### Transition Rules

#### Hidden → Suspected
**Triggers**:
- Witness reports crime (FoW check: colonist had LoS)
- Interrogation yields confession
- Evidence discovered during investigation
- Another pawn reports seeing it

**Requirements**:
- At least one piece of evidence OR witness testimony

**Cannot Reverse**: Once suspected, cannot return to hidden

#### Suspected → Convicted
**Triggers**:
- Player manually convicts via UI
- Automatic conviction (mandate violations)
- Trial results in guilty verdict
- Caught red-handed (100% evidence)

**Requirements**:
- Case must be in "Suspected" state
- At least one crime in the case

**Cannot Reverse**: Once convicted, cannot return to suspected

#### Hidden → Hidden (Stays Hidden)
**Conditions**:
- No witnesses ever find out
- Interrogations never reveal it
- Investigation never uncovers it
- Pawn dies with secret intact

**Result**: Crime never enters justice system, pawn "gets away with it"

#### Special Case: Hidden → Convicted (Skip Suspected)
**Rare scenarios**:
- Pawn caught in act with overwhelming evidence
- Automatic conviction systems (export violations)
- Pre-trial confession with immediate sentencing

**Generally avoided**: Should go through Suspected phase for UI clarity

## Case Management

### One Case Per Pawn Rule

**Design Decision**: Only one active case allowed per pawn at a time, but can contain multiple crimes.

**Rationale**:
- Simplifies UI presentation
- Matches real-world legal bundling
- Reduces cognitive load
- Easier to track case status

**Implementation**:
```csharp
public class CriminalCase
{
    public Pawn accused;
    public CaseStatus status; // Open, UnderInvestigation, InTrial, Closed
    public List<CrimeInstance> crimes; // Can have multiple

    // Only ONE case per pawn
    public static CriminalCase GetOrCreateCase(Pawn pawn)
    {
        var existing = Find.World.GetComponent<JusticeManager>()
            .GetActiveCase(pawn);

        if (existing != null)
            return existing; // Amend existing case

        return new CriminalCase(pawn); // Create new case
    }
}
```

### Amending Cases

**When a new crime is discovered**:
1. Check if pawn has active case
2. If YES: Add crime to existing case
3. If NO: Create new case with crime

**Example**:
```
Day 1: Bob suspected of theft
  → Case #1 opened: Bob [Theft]

Day 3: Bob suspected of assault
  → Case #1 amended: Bob [Theft, Assault]

Day 5: Bob convicted
  → Case #1 closed: Bob convicted of Theft + Assault

Day 7: Bob suspected of murder
  → Case #2 opened: Bob [Murder]
  (New case because previous case closed)
```

## Crime Instance Data Structure

### Internal Tracking (Always Recorded)

```csharp
public class CrimeInstance
{
    // Core Data
    public Pawn perpetrator;
    public CrimeType type;
    public int tick; // When it happened
    public IntVec3 location; // Where it happened
    public Pawn victim; // If applicable

    // Visibility State
    public CrimeVisibilityState state; // Hidden/Suspected/Convicted

    // Evidence & Witnesses
    public List<Pawn> witnesses; // Who saw it (empty if Hidden)
    public float evidenceStrength; // 0.0-1.0
    public bool wasConfessed; // Revealed during interrogation

    // Case Association
    public CriminalCase associatedCase; // Null if Hidden, set when Suspected

    // Conviction Data
    public Punishment assignedPunishment; // Null until Convicted
    public int convictionTick; // When convicted
    public bool falseConviction; // Track if wrong pawn convicted
}

public enum CrimeVisibilityState
{
    Hidden,     // Not visible to justice system
    Suspected,  // Case opened, under investigation
    Convicted   // Found guilty, punishment assigned
}
```

### UI Visibility Logic

```csharp
public static bool ShouldShowInUI(CrimeInstance crime)
{
    switch (crime.state)
    {
        case CrimeVisibilityState.Hidden:
            return false; // NEVER show hidden crimes

        case CrimeVisibilityState.Suspected:
            return true; // Show in "Open Cases" tab

        case CrimeVisibilityState.Convicted:
            return true; // Show in "Convictions" tab

        default:
            return false;
    }
}
```

## Integration with Fog of War

### Witness Detection Flow

```csharp
public static void OnCrimeCommitted(Pawn perpetrator, CrimeType type, IntVec3 location, Pawn victim = null)
{
    // ALWAYS record internally
    var crime = new CrimeInstance
    {
        perpetrator = perpetrator,
        type = type,
        location = location,
        victim = victim,
        tick = Find.TickManager.TicksGame,
        state = CrimeVisibilityState.Hidden // Start as Hidden
    };

    // Store in perpetrator's crime record (internal tracking)
    perpetrator.GetCrimeRecord().AddCrime(crime);

    // Check for witnesses using Fog of War
    var witnesses = GetWitnesses(location, perpetrator.Map);

    if (witnesses.Count > 0)
    {
        // Crime was witnessed - transition to Suspected
        crime.witnesses = witnesses;
        crime.state = CrimeVisibilityState.Suspected;

        // Open or amend case
        var criminalCase = CriminalCase.GetOrCreateCase(perpetrator);
        criminalCase.crimes.Add(crime);
        crime.associatedCase = criminalCase;

        // Notify player
        Messages.Message(
            $"{perpetrator.LabelShort} suspected of {type}",
            MessageTypeDefOf.NegativeEvent
        );
    }
    else
    {
        // No witnesses - stays Hidden
        Log.Message($"[Hidden Crime] {perpetrator.LabelShort} committed {type} but no one saw it!");
    }
}

private static List<Pawn> GetWitnesses(IntVec3 location, Map map)
{
    var witnesses = new List<Pawn>();
    var fogComp = map.GetComponent<MapComponentSeenFog>();

    // Check if any colonist can see the location
    foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
    {
        // Must be able to see and location must be visible
        if (colonist.health.capacities.CapableOf(PawnCapacityDefOf.Sight) &&
            fogComp.IsShown(Faction.OfPlayer, location))
        {
            // Check if within sight range
            var watcher = colonist.GetComp<CompFieldOfViewWatcher>();
            if (watcher != null)
            {
                float sightRange = watcher.CalcPawnSightRange(colonist.Position, false, false);
                float distance = colonist.Position.DistanceTo(location);

                if (distance <= sightRange)
                {
                    witnesses.Add(colonist);
                }
            }
        }
    }

    return witnesses;
}
```

## UI Presentation

### Justice Tab Structure

```
┌─────────────────────────────────────────┐
│ Justice System                          │
├─────────────────────────────────────────┤
│ [Open Cases] [Convictions] [Settings]  │
├─────────────────────────────────────────┤
│                                         │
│ OPEN CASES (Suspected)                  │
│ ┌─────────────────────────────────────┐ │
│ │ Case #1: Bob (3 crimes)             │ │
│ │ ├─ [SUSPECTED] Theft                │ │
│ │ │  └─ 2 witnesses, Day 5            │ │
│ │ ├─ [SUSPECTED] Assault              │ │
│ │ │  └─ 1 witness, Day 7              │ │
│ │ └─ [SUSPECTED] Vandalism            │ │
│ │    └─ Confession, Day 9             │ │
│ │ [Convict] [Dismiss] [Investigate]   │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ CONVICTIONS (Convicted)                 │
│ ┌─────────────────────────────────────┐ │
│ │ Case #2: Alice (1 crime)            │ │
│ │ ├─ [CONVICTED] Murder                │ │
│ │ │  └─ Sentenced: Execution          │ │
│ │ │  └─ Status: Awaiting execution    │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ HIDDEN CRIMES: Not shown               │
│ (Player has no UI visibility)           │
└─────────────────────────────────────────┘
```

### Individual Crime Display

```
Crime: Theft
Status: [SUSPECTED]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Perpetrator: Bob (Colonist)
Victim: Silver stockpile (-500 silver)
Date: Day 5, Hour 14
Location: Warehouse (x:123, z:456)

Evidence:
├─ Witnesses: 2
│  ├─ Alice (high confidence)
│  └─ Charlie (medium confidence)
├─ Evidence Strength: 75%
└─ Confession: No

Actions:
[Convict] [Dismiss] [Interrogate Bob]
```

## State Transition Examples

### Example 1: Hidden → Suspected → Convicted

```
Day 1, 02:00: Bob steals silver while everyone sleeps
  → Crime recorded internally
  → No witnesses (all colonists asleep, no LoS)
  → State: HIDDEN
  → UI: Nothing shown

Day 3, 14:00: Alice interrogates Bob about suspicious wealth
  → Bob confesses to theft
  → State: HIDDEN → SUSPECTED
  → Case #1 opened: Bob [Theft]
  → UI: Shows case with confession evidence

Day 5, 10:00: Player convicts Bob
  → State: SUSPECTED → CONVICTED
  → Punishment: 3 days imprisonment
  → Case #1 closed
  → UI: Moved to Convictions tab
```

### Example 2: Suspected → Convicted (Direct)

```
Day 1, 15:00: Bob punches Charlie during tantrum
  → Crime recorded internally
  → Witnesses: Alice, David, Eve (all nearby)
  → State: SUSPECTED (immediately)
  → Case #1 opened: Bob [Assault]
  → UI: Shows case with 3 witnesses

Day 1, 16:00: Player convicts Bob (same day)
  → State: SUSPECTED → CONVICTED
  → Punishment: Beating
  → Case #1 closed
  → UI: Moved to Convictions tab
```

### Example 3: Hidden Forever (Gets Away With It)

```
Day 1, 03:00: Bob steals silver
  → Crime recorded internally
  → No witnesses (darkness, everyone asleep)
  → State: HIDDEN
  → UI: Nothing shown

Day 5, 10:00: Alice interrogates Bob
  → Bob refuses to confess (social skill check failed)
  → State: HIDDEN (stays hidden)
  → UI: Nothing shown

Day 10: Bob dies of plague
  → Crime remains in internal records
  → State: HIDDEN (died with secret)
  → UI: Never shown
  → Bob got away with it
```

### Example 4: False Conviction

```
Day 1, 02:00: Vampire drains Alice
  → Crime recorded internally (Vampire is perpetrator)
  → No witnesses (darkness, isolated victim)
  → State: HIDDEN
  → UI: Nothing shown

Day 1, 03:00: Vampire files false report blaming Bob
  → Crime instance created: Bob as perpetrator (FALSE)
  → Vampire listed as "witness"
  → State: SUSPECTED (false accusation)
  → Case #1 opened: Bob [Murder]
  → UI: Shows Bob suspected of Alice's murder

Day 2, 10:00: Player convicts Bob (wrong person!)
  → State: SUSPECTED → CONVICTED
  → Punishment: Execution
  → Case #1 closed
  → UI: Bob shown as convicted murderer
  → Bob.crimes[0].falseConviction = true (tracked)

Result: Wrong pawn executed, vampire still free, real crime stays HIDDEN
```

## Advanced Features

### Confession System

Interrogations can reveal Hidden crimes:

```csharp
public static void OnInterrogation(Pawn interrogator, Pawn suspect)
{
    // Get all HIDDEN crimes by suspect
    var hiddenCrimes = suspect.GetCrimeRecord()
        .GetCrimes()
        .Where(c => c.state == CrimeVisibilityState.Hidden);

    foreach (var crime in hiddenCrimes)
    {
        // Social skill check
        if (SocialSkillCheck(interrogator, suspect))
        {
            // Confession! Transition to Suspected
            crime.state = CrimeVisibilityState.Suspected;
            crime.wasConfessed = true;

            // Add to case
            var criminalCase = CriminalCase.GetOrCreateCase(suspect);
            criminalCase.crimes.Add(crime);
            crime.associatedCase = criminalCase;

            Messages.Message(
                $"{suspect.LabelShort} confessed to {crime.type}!",
                MessageTypeDefOf.NegativeEvent
            );
        }
    }
}
```

### Evidence Discovery

Investigations can uncover Hidden crimes:

```csharp
public static void OnInvestigation(IntVec3 location, Map map)
{
    // Find all crimes that occurred at this location
    var hiddenCrimesAtLocation = Find.World.GetComponent<JusticeManager>()
        .GetAllCrimes()
        .Where(c => c.state == CrimeVisibilityState.Hidden &&
                    c.location == location);

    foreach (var crime in hiddenCrimesAtLocation)
    {
        // Investigation skill check
        if (InvestigationSuccessful())
        {
            // Evidence found! Transition to Suspected
            crime.state = CrimeVisibilityState.Suspected;
            crime.evidenceStrength = 0.5f; // Medium evidence

            // Create case
            var criminalCase = CriminalCase.GetOrCreateCase(crime.perpetrator);
            criminalCase.crimes.Add(crime);

            Messages.Message(
                $"Investigation revealed {crime.perpetrator.LabelShort} committed {crime.type} here!",
                MessageTypeDefOf.NegativeEvent
            );
        }
    }
}
```

### Statute of Limitations

Hidden crimes can become unsolvable over time:

```csharp
public static void CheckStatuteOfLimitations(CrimeInstance crime)
{
    const int DAYS_UNTIL_UNSOLVABLE = 60; // 1 year

    if (crime.state == CrimeVisibilityState.Hidden)
    {
        int ticksElapsed = Find.TickManager.TicksGame - crime.tick;
        int daysElapsed = ticksElapsed / GenDate.TicksPerDay;

        if (daysElapsed > DAYS_UNTIL_UNSOLVABLE)
        {
            // Crime is too old to prosecute
            crime.canBeInvestigated = false;

            // Mark as "cold case"
            crime.coldCase = true;
        }
    }
}
```

## Performance Considerations

### Internal Tracking vs UI Visibility

**All crimes tracked internally**:
- Full crime history for storytelling
- Data for mods/debugging
- AI decision making
- Stat tracking

**Only Suspected/Convicted shown in UI**:
- Reduces UI clutter
- Maintains mystery
- Better performance (less rendering)
- Clearer player communication

### Optimization

```csharp
// Cache UI-visible crimes (only Suspected + Convicted)
private List<CrimeInstance> cachedVisibleCrimes;
private int lastCacheUpdate;

public List<CrimeInstance> GetVisibleCrimes()
{
    if (Find.TickManager.TicksGame - lastCacheUpdate > 250) // Update every ~4 seconds
    {
        cachedVisibleCrimes = allCrimes
            .Where(c => c.state != CrimeVisibilityState.Hidden)
            .ToList();

        lastCacheUpdate = Find.TickManager.TicksGame;
    }

    return cachedVisibleCrimes;
}
```

## Summary

**Three-State System**:
1. **Hidden**: Tracked internally, not visible, can be discovered
2. **Suspected**: Case opened, shown in UI, under investigation
3. **Convicted**: Found guilty, punishment assigned

**Key Benefits**:
✅ Maintains DF-style mystery (crimes can stay hidden)
✅ Clean UI (only show relevant information)
✅ Full data tracking (all crimes recorded)
✅ Emergent narratives (pawns getting away with crimes)
✅ False convictions possible (wrong pawn can be convicted)
✅ One case per pawn (simplified management)

**Integration Points**:
- Fog of War determines initial visibility
- Interrogations reveal Hidden crimes
- Investigations transition Hidden → Suspected
- Player/trial transitions Suspected → Convicted
- No backward transitions (ratchet system)

This system creates the perfect balance between internal simulation depth and player-facing clarity!
