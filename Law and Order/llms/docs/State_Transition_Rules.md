# Crime State Transition Rules

## Quick Reference

This document provides a quick reference for state transitions in the crime visibility system.

**See Also**:
- `Crime_Visibility_States.md` - Design philosophy and detailed examples
- `Technical_Spec_Crime_States.md` - Implementation details and code structure

## State Machine Diagram

```
                    ┌─────────────────────────────────────────────────┐
                    │                                                 │
                    │              CRIME OCCURS                       │
                    │                                                 │
                    └───────────────────┬─────────────────────────────┘
                                        │
                                        ▼
                            ┌───────────────────────┐
                            │                       │
                            │   ALWAYS RECORDED     │
                            │   INTERNALLY          │
                            │                       │
                            └───────────┬───────────┘
                                        │
                                        ▼
                             ┏━━━━━━━━━━━━━━━━━━━┓
                             ┃   Initial State   ┃
                             ┃     HIDDEN        ┃
                             ┗━━━━━━━━┬━━━━━━━━━━┛
                                      │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
             ┌──────▼──────┐                    ┌──────▼──────┐
             │  Witnessed? │                    │ Never Found │
             │  Confessed? │                    │             │
             │ Discovered? │                    │  STAYS      │
             └──────┬──────┘                    │  HIDDEN     │
                    │                           │             │
                    │ YES                       │ FOREVER     │
                    ▼                           └─────────────┘
             ┏━━━━━━━━━━━━━━━┓
             ┃   SUSPECTED   ┃◄──────────────┐
             ┃               ┃               │
             ┃ Case Opened   ┃          Amendment
             ┃ UI Visible    ┃         (New Crime)
             ┗━━━━━━━┬━━━━━━━┛               │
                     │                       │
                     │ Convicted             │
                     ▼                       │
             ┏━━━━━━━━━━━━━━━┓               │
             ┃   CONVICTED   ┃───────────────┘
             ┃               ┃
             ┃ Punishment    ┃
             ┃ Assigned      ┃
             ┗━━━━━━━━━━━━━━━┛
                     │
                     │ Case Closed
                     ▼
               [Case Archived]
```

## Transition Table

| From State | To State | Trigger | Conditions | Reversible? |
|------------|----------|---------|------------|-------------|
| Hidden | Hidden | Time passes | No discovery | N/A (same state) |
| Hidden | Suspected | Witnessed | Colonist has LoS (FoW check) | ❌ No |
| Hidden | Suspected | Confession | Interrogation successful | ❌ No |
| Hidden | Suspected | Investigation | Evidence discovered | ❌ No |
| Hidden | [Expires] | Time passes | Statute of limitations | ❌ No |
| Suspected | Convicted | Player action | Manual conviction | ❌ No |
| Suspected | Convicted | Auto-conviction | Caught red-handed | ❌ No |
| Suspected | Convicted | Trial verdict | Trial concludes | ❌ No |
| Suspected | Dismissed | Player action | Case dismissed | ✅ Back to Hidden |
| Convicted | [Any] | Never | N/A | ❌ No |

## Detailed Transition Rules

### Hidden → Hidden (Stays Hidden)

**Conditions**:
- No colonist witnessed (FoW check failed)
- Never interrogated successfully
- Never investigated
- Pawn never confesses voluntarily

**Duration**: Can stay Hidden forever

**UI**: Never shown to player

**Can escape justice**: YES - pawn gets away with it

**Example**:
```
Bob steals 500 silver at 3 AM
→ All colonists asleep (no LoS)
→ Crime stays Hidden
→ Bob spends the silver
→ Never discovered
→ Bob got away with it!
```

---

### Hidden → Suspected

#### Trigger 1: Witnessed

**Conditions**:
- At least one colonist had line of sight when crime occurred
- FoW check: `MapComponentSeenFog.IsShown(Faction.OfPlayer, location) == true`
- Colonist within their sight range of crime location
- Colonist has sight capacity > 0

**Process**:
1. Crime committed
2. Check all colonists for LoS
3. If any can see: add to witnesses list
4. Calculate evidence strength
5. Transition to Suspected
6. Create or amend case
7. Show notification to player

**Example**:
```csharp
Bob punches Alice in dining room
→ Charlie is 5 tiles away, watching
→ FoW check: Charlie can see dining room
→ Charlie added as witness
→ Crime.TransitionToSuspected([Charlie])
→ Case opened: Bob [Assault]
→ UI shows "Bob suspected of Assault"
```

#### Trigger 2: Confession (Interrogation)

**Conditions**:
- Pawn interrogated by warden/investigator
- Social skill check succeeds
- `Rand.Chance(confessionChance)` passes

**Confession Chance Formula**:
```
Base: 50%
+ (Interrogator Social - Suspect Social) × 5%
- 20% if suspect dislikes interrogator
```

**Process**:
1. Interrogation initiated
2. Social skill check
3. If success: suspect confesses to random Hidden crime
4. Crime transitions to Suspected
5. Added to case as "Confession"
6. Evidence strength set to 70%+

**Example**:
```csharp
Alice interrogates Bob (Social 10 vs Bob Social 5)
→ Confession chance: 50% + (10-5)×5% = 75%
→ Roll succeeds
→ Bob confesses to Hidden theft from Day 5
→ Crime.TransitionToSuspected() with wasConfessed=true
→ Case amended: Bob [Theft (confession)]
→ UI shows new crime
```

#### Trigger 3: Investigation/Discovery

**Conditions**:
- Investigation of crime scene
- Evidence found linking pawn to crime
- Circumstantial evidence accumulated

**Process**:
1. Player orders investigation of area
2. Investigator checks location
3. Investigation skill check
4. If success: evidence discovered
5. Crime transitions to Suspected
6. Added to case with evidence details

**Example**:
```
Day 10: Player investigates warehouse (Bob stole silver on Day 1)
→ Investigator finds footprints, moved crates
→ Investigation check succeeds
→ Evidence links to Bob
→ Hidden crime transitions to Suspected
→ Case opened: Bob [Theft (evidence)]
```

---

### Suspected → Convicted

#### Trigger 1: Manual Conviction (Player)

**Conditions**:
- Player reviews case
- Clicks "Convict" button
- Selects punishment

**Process**:
1. Player opens Justice tab
2. Selects case
3. Clicks "Convict"
4. Dialog opens: "Select Punishment"
5. Player chooses punishment
6. Crime transitions to Convicted
7. Punishment assigned and scheduled
8. Case marked as closed (if all crimes convicted)

**Example**:
```
Player reviews Case #1: Bob [Assault, Theft]
→ Clicks "Convict"
→ Selects "5 days imprisonment"
→ Both crimes transition to Convicted
→ Case #1 closed
→ Bob sent to prison
```

#### Trigger 2: Auto-Conviction (Caught Red-Handed)

**Conditions**:
- Evidence strength ≥ 95%
- Caught in the act
- Settings: `autoConvictRedHanded = true`

**Process**:
1. Crime witnessed with overwhelming evidence
2. Auto-conviction triggers
3. Default punishment assigned based on crime type
4. Immediate transition to Convicted
5. No player input required

**Example**:
```
Bob attacks Alice in front of 5 colonists in broad daylight
→ Evidence strength: 100%
→ Auto-conviction enabled
→ Immediately convicted
→ Default punishment: Beating
→ Case closed automatically
```

#### Trigger 3: Trial Verdict (Future Feature)

**Conditions**:
- Trial system implemented
- Case goes to trial
- Jury/judge renders verdict
- Verdict: Guilty

**Process**:
1. Case sent to trial
2. Evidence presented
3. Witnesses testify
4. Verdict reached
5. If guilty: transition to Convicted
6. Punishment assigned by trial

**Example** (Future):
```
Case #1: Bob [Murder] goes to trial
→ Witnesses testify
→ Evidence presented
→ Jury finds guilty
→ Judge sentences: Execution
→ Crime transitions to Convicted
→ Execution scheduled
```

---

### Suspected → Dismissed

**Conditions**:
- Player reviews case
- Determines evidence insufficient
- Clicks "Dismiss" button
- Confirms dismissal

**Process**:
1. Player selects case
2. Clicks "Dismiss"
3. Confirmation dialog
4. If confirmed: case dismissed
5. Crimes return to Hidden state
6. Case closed
7. No punishment

**Result**: Effectively returns to Hidden (pawn escapes justice)

**Example**:
```
Case #1: Bob [Theft] (1 unreliable witness, 30% evidence)
→ Player reviews
→ "Evidence too weak"
→ Dismisses case
→ Crime returns to Hidden
→ Bob escapes justice
```

**Important**: This is the ONLY way to reverse a Suspected state!

---

### Convicted → [Terminal State]

**Conditions**: NONE - Cannot transition from Convicted

**Convicted is terminal**:
- No appeals system
- No pardons
- No reversal

**Rationale**:
- Simpler system
- Matches DF design
- False convictions are permanent (storytelling value)
- Player must live with consequences

**Important**: False convictions stay false!
```
Bob falsely convicted of vampire's murder
→ Executed
→ Remains in records as "Convicted"
→ isFalseConviction = true (tracked internally)
→ Real vampire still free
→ Tragic story, permanent consequence
```

---

## UI Visibility Rules

### Hidden Crimes

**UI Visibility**: ❌ **NEVER SHOWN**

**Where NOT shown**:
- Justice tab
- Case lists
- Pawn inspection tab
- Notifications
- Letters

**Only visible**:
- Dev mode (if debug setting enabled)
- Internal logs
- Save file (for modders/debugging)

**Rationale**: Player should not have meta-knowledge

---

### Suspected Crimes

**UI Visibility**: ✅ **SHOWN IN OPEN CASES**

**Where shown**:
- Justice tab → Open Cases
- Pawn inspection tab → Criminal Record
- Notifications (when case opened/amended)
- Case details dialog

**Information displayed**:
- Crime type
- Date/time committed
- Location
- Witnesses (count and names)
- Evidence strength (%)
- Confession status

**Actions available**:
- Convict
- Dismiss
- Investigate further
- Interrogate

---

### Convicted Crimes

**UI Visibility**: ✅ **SHOWN IN CONVICTIONS**

**Where shown**:
- Justice tab → Convictions
- Pawn inspection tab → Criminal Record
- Punishment schedule
- Conviction history

**Information displayed**:
- Crime type
- Conviction date
- Punishment type
- Punishment status (pending/in-progress/completed)
- False conviction indicator (if debug mode)

**Actions available**:
- View details
- Check punishment progress
- (Future: Commute sentence, pardon)

---

## Special Cases

### Multiple Crimes → One Case

**Rule**: One case per pawn at a time

**Example**:
```
Day 1: Bob suspected of Theft
  → Case #1 created: Bob [Theft]

Day 3: Bob suspected of Assault
  → Case #1 amended: Bob [Theft, Assault]

Day 5: Both convicted
  → Case #1 closed: Bob convicted of both

Day 7: Bob suspected of Murder
  → Case #2 created: Bob [Murder]
  (New case because #1 is closed)
```

### Hidden Crime Never Discovered

**Outcome**: Pawn gets away with it

**Tracking**: Crime remains in internal records

**Uses**:
- Stat tracking (crimes per pawn)
- AI decision making
- Storytelling (revealed after death)
- Mod integration

**Example**:
```
Bob steals 1000 silver on Day 1
→ Hidden (no witnesses)
→ Never interrogated
→ Never investigated
→ Bob dies on Day 50
→ Secret dies with him
→ Colony never knew
```

### False Conviction

**Possible?**: ✅ YES - This is a feature!

**How it happens**:
1. Real perpetrator's crime stays Hidden
2. False witness reports (grudge, mistake)
3. Wrong pawn Suspected
4. Player convicts based on false evidence
5. Innocent pawn punished

**Tracked**: `crime.isFalseConviction = true`

**Consequences**:
- Innocent pawn punished
- Real perpetrator free (crime still Hidden)
- Colony morale impact
- Storytelling opportunity

**Example**:
```
Vampire drains Alice at night
→ Vampire's crime: Hidden
→ Vampire files false report blaming Bob
→ Bob: Suspected of Murder
→ Player convicts Bob (wrong!)
→ Bob executed
→ Vampire still free, will strike again
→ Tragic story
```

### Statute of Limitations

**Rule**: Hidden crimes become uninvestigatable after time

**Default**: 60 days (1 year)

**Effect**:
- Crime marked as `canBeInvestigated = false`
- Cannot be revealed through interrogation
- Cannot be discovered through investigation
- Marked as "cold case"
- Effectively escaped justice

**Example**:
```
Day 1: Bob steals silver (Hidden)
Day 30: Investigation fails
Day 61: Statute expires
→ Crime now cold case
→ Cannot be discovered
→ Bob got away with it permanently
```

## Summary Table

| State | UI Visible? | Case Opened? | Can Transition To | Actions Available |
|-------|-------------|--------------|-------------------|-------------------|
| Hidden | ❌ No | ❌ No | Suspected, Expired | Discover, Interrogate |
| Suspected | ✅ Yes | ✅ Yes | Convicted, Dismissed | Convict, Dismiss, Investigate |
| Convicted | ✅ Yes | ✅ Closed | None (terminal) | View details only |

## Implementation Checklist

When implementing state transitions, ensure:

- [ ] Always record crime internally first
- [ ] Check FoW for witnesses immediately
- [ ] Calculate evidence strength based on circumstances
- [ ] Create/amend case when transitioning to Suspected
- [ ] Show notification to player on state change
- [ ] Update UI immediately
- [ ] Track state change tick for animation
- [ ] Mark isFalseConviction if applicable
- [ ] Close case when all crimes convicted
- [ ] No backward transitions (except Dismissed)
- [ ] Save/load state correctly

## Common Mistakes to Avoid

❌ **DON'T**:
- Show Hidden crimes in UI (breaks immersion)
- Allow backward transitions from Convicted
- Create multiple cases for same pawn
- Forget to check FoW when crime occurs
- Assume player knows who committed crime
- Auto-convict without evidence

✅ **DO**:
- Always record internally regardless of visibility
- Use FoW to determine witnesses
- Allow false convictions (storytelling!)
- Keep one case per pawn
- Track evidence strength accurately
- Let players make mistakes (wrong convictions)

---

**This state machine is the CORE of Law and Order's DF-inspired justice system!**
