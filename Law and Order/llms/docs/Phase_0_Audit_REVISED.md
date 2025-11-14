# Phase 0: Code Audit & Refactor Plan (REVISED)

**Date:** November 13, 2025
**Purpose:** Document existing system and plan HYBRID refactor to align with Implementation Roadmap
**Decision:** HYBRID APPROACH - Adapt existing systems rather than complete rebuild

---

## Executive Summary - REVISED

The Law and Order mod currently implements a **debt-based punishment system with court hearings**. The Implementation Roadmap expects a **Dwarf Fortress-inspired state-based crime detection system** with Hidden/Suspected/Convicted states and Fog of War witness detection.

**REVISED DECISION:** These systems are **largely compatible** with smart adaptation. We can keep the strong architectural foundations and add the new state-based detection on top.

---

## Hybrid Approach Strategy

### Core Insight: Additive, Not Replacement

Instead of tearing everything down, we **add** the new systems:

1. **Keep Hediff_Crimes** - It's good architecture (efficient, auto-save/load)
   - **ADD:** `CrimeVisibilityState` to each `Crime` object
   - **ADD:** Evidence tracking, witness lists
   - **KEEP:** Archival system, crime tracking, query methods

2. **Adapt Debt System** - It becomes a punishment type
   - **CHANGE:** Debt is no longer automatic for all crimes
   - **NEW ROLE:** Debt = "Fine" punishment (Phase 4 roadmap)
   - **KEEP:** All debt mechanics (repayment, grace period, thoughts)
   - **ADD:** Debt only assigned when player chooses "Fine" punishment

3. **Adapt Hearing Ritual** - It becomes the formal trial process
   - **NEW ROLE:** Hearing = optional formal conviction process
   - **CHANGE:** Hearings happen AFTER player chooses to convict (not before)
   - **KEEP:** All ritual mechanics, plea bargains, quality outcomes
   - **ADD:** Hearing outcome affects punishment severity or provides bonuses

4. **Keep Contraband** - It's a complete, working system
   - **KEEP:** Entire contraband system as-is
   - **ADD:** Contraband possession crimes use new state system
   - **INTEGRATE:** Contraband detection triggers witness checks

---

## Revised Architecture Plan

### Phase 1: Add State System to Existing Crimes

**Goal:** Enhance `Crime` class with visibility states WITHOUT removing existing features

#### 1.1: Extend Crime Class
```csharp
public class Crime : IExposable
{
    // EXISTING fields (keep all)
    public CrimeType crimeType;
    public int tickCommitted;
    public Pawn victim;
    // ... etc

    // NEW fields (add these)
    public CrimeVisibilityState visibilityState = CrimeVisibilityState.Hidden;
    public List<Pawn> witnesses = new List<Pawn>();
    public float evidenceStrength = 0f;
    public bool wasConfessed = false;
    public int convictedTick = 0;
    public PunishmentType assignedPunishment = PunishmentType.None;
}

public enum CrimeVisibilityState
{
    Hidden,      // No witnesses detected
    Suspected,   // Witnessed or confessed
    Convicted    // Player convicted
}
```

**Impact:** Fully backward compatible - old saves will load with all crimes as Hidden

#### 1.2: Keep Hediff_Crimes
- **NO CHANGE** to Hediff architecture
- **ADD:** New methods for state queries:
  - `GetSuspectedCrimes()`
  - `GetConvictedCrimes()`
  - `GetHiddenCrimes()` (only visible in dev mode)
  - `TransitionToSuspected(Crime, witnesses, evidence)`
  - `TransitionToConvicted(Crime, punishment)`

#### 1.3: Add Case Management (Optional Per Crime)
- `CriminalCase` class groups Suspected crimes
- Cases are created when first crime transitions to Suspected
- Additional Suspected crimes auto-added to existing case
- **Keep:** Individual crime tracking as primary storage
- **Add:** Case as a "view" of Suspected crimes

---

### Phase 2: FoW Witness Detection Integration

**Goal:** Use FoW to determine initial visibility state

#### 2.1: Crime Detection Hook
When a crime occurs (existing detection patches):
```csharp
// OLD: Immediately add crime to hediff
Crime crime = new Crime(type, victim, ...);
hediff.AddCrime(crime);
DebtUtils.CalculateAndApplyDebt(criminal, crime); // <- Remove this

// NEW: Add crime with witness check
Crime crime = new Crime(type, victim, ...);
crime.visibilityState = CrimeVisibilityState.Hidden; // Start hidden

// Check for witnesses using FoW
List<Pawn> witnesses = FogOfWarUtils.GetWitnessesAtLocation(location, map);
if (witnesses.Count > 0)
{
    // Crime was witnessed!
    crime.witnesses = witnesses;
    crime.evidenceStrength = FogOfWarUtils.CalculateEvidenceStrength(location, map, witnesses);
    crime.visibilityState = CrimeVisibilityState.Suspected;

    // Create or update case
    CriminalCase.GetOrCreateCase(criminal).AddCrime(crime);

    // Notify player
    Messages.Message($"{criminal.NameShortColored} suspected of {crime.GetCrimeLabel()}", MessageTypeDefOf.NeutralEvent);
}

hediff.AddCrime(crime);
// NO automatic debt!
```

**Impact:** Crimes now start Hidden or Suspected based on witnesses. Debt is NOT applied yet.

---

### Phase 3: UI Integration

**Goal:** Adapt existing Justice UI to show state-based crimes

#### 3.1: MainTabWindow_Justice Tabs (Revised)
Keep existing 4 tabs, modify content:

**Tab 1: Open Cases** (was "Active Criminals")
- Show pawns with **Suspected** crimes (cases)
- Group crimes by case
- Show evidence strength, witnesses
- **Actions:**
  - "Convict" → opens punishment selection
  - "Dismiss" → sets crime to Hidden (requires justification)
  - "Investigate" → interrogation option (Phase 5)

**Tab 2: Convictions** (was "Imprisoned")
- Show pawns with **Convicted** crimes
- Show assigned punishment and status
- **Actions:**
  - View punishment progress
  - Pardon (set back to Suspected)

**Tab 3: Contraband** (keep as-is)
- No changes needed

**Tab 4: Crime Settings** (was "Crimes")
- Keep penalty configuration
- **ADD:** State system settings (show hidden crimes in dev mode, etc.)

#### 3.2: ITab_Pawn_Judiciary (Simplified)
- Show crime state breakdown (X hidden, Y suspected, Z convicted)
- Show current case if Suspected crimes exist
- Show active punishment if convicted
- **Removed:** Hearing schedule (move to conviction workflow)

---

### Phase 4: Punishment System Integration

**Goal:** Make debt ONE OF several punishment types

#### 4.1: Punishment Selection Dialog
When player clicks "Convict" on a case:
```csharp
Dialog_SelectPunishment dialog = new Dialog_SelectPunishment(criminal, case);
// Options:
// - Fine (Silver debt) ← Uses existing debt system!
// - Imprisonment (Duration-based)
// - Beating (Immediate)
// - Execution (Scheduled)
// - Exile (Banishment)
// - Hearing (Formal trial ritual) ← Uses existing hearing system!
```

#### 4.2: Debt as "Fine" Punishment
```csharp
if (selectedPunishment == PunishmentType.Fine)
{
    // Use existing debt calculation!
    float debtAmount = DebtUtils.CalculateDebt(criminal, case.GetAllCrimes());

    // Apply existing debt hediff
    DebtUtils.ApplyDebt(criminal, debtAmount);

    // All existing debt mechanics work unchanged:
    // - Enslavement for payment
    // - Grace periods
    // - Debt stress thoughts
    // - Auto-emancipation
}
```

**Impact:** Debt system KEPT INTACT, repurposed as punishment option

#### 4.3: Hearing as "Formal Trial" Punishment Option
```csharp
if (selectedPunishment == PunishmentType.FormalHearing)
{
    // Use existing hearing ritual system!
    HearingRecord hearing = criminal.GetCrimeHediff().Hearing;
    hearing.status = HearingStatus.Scheduled;

    // Schedule hearing ritual
    // Existing ritual code handles the ceremony

    // Hearing outcome affects final punishment:
    // - Excellent: -20% debt or reduced sentence
    // - Poor: +20% debt or harsher sentence
}
```

**Impact:** Hearing system KEPT INTACT, repurposed as optional trial process

---

### Phase 5-11: Follow Roadmap with Existing Foundation

All remaining roadmap phases can build on this foundation:

- **Phase 5:** Investigation/Interrogation → reveals Hidden crimes, transitions to Suspected
- **Phase 6:** False Accusations → adds crimes with Suspected state, wrong perpetrator
- **Phase 7:** Raid Crimes → auto-Suspected (witnessed by nature of combat)
- **Phase 8:** Mental Break Integration → crimes marked with mental state context
- **Phase 9:** Ideology → affects punishment selection preferences
- **Phase 10:** Polish
- **Phase 11:** Advanced features (corruption, courtrooms, etc.)

---

## What to Keep vs. Modify

### ✅ KEEP (No Changes)
- `Hediff_Crimes` architecture
- `Hediff_Debt` and entire debt system
- `HearingRecord` and ritual system
- `ContrabandDefinition` and contraband system
- `WorldComponent_DebtManager`
- `WorldComponent_ContrabandManager`
- `ModLog` and logging system
- All utility classes
- All UI helpers
- Harmony patching framework
- Thought workers (debt stress, contraband, etc.)
- Alerts system
- Crime archival system

### 🔧 MODIFY (Extend/Adapt)
- **Crime class:** Add state fields (visibilityState, witnesses, evidenceStrength)
- **Hediff_Crimes:** Add state query methods
- **MainTabWindow_Justice:** Reorganize tabs, update display logic
- **Crime detection patches:** Add FoW witness checks
- **Punishment workflow:** Change from automatic debt → player-selected punishment
- **Hearing ritual:** Change from pre-conviction → post-conviction formal trial

### ➕ ADD (New Systems)
- `CrimeVisibilityState` enum
- `CriminalCase` class (lightweight, groups Suspected crimes)
- `JusticeManager` WorldComponent (minimal - case registry)
- `FogOfWarUtils` (already created!)
- `PunishmentType` enum
- Investigation/interrogation system (Phase 5)
- False accusation logic (Phase 6)
- State transition methods

### ❌ REMOVE (Nothing!)
- No major removals needed
- Some refactoring of debt auto-application
- Some reorganization of UI

---

## Migration Strategy - REVISED

### Backward Compatibility: POSSIBLE!

With this approach, we can maintain **partial save compatibility**:

**Old Saves Loading:**
1. Existing crimes load with default `visibilityState = Hidden`
2. Player can "review" old crimes and decide to make Suspected/Convicted
3. Existing debt hediffs continue working
4. Existing hearings continue working
5. Existing contraband continues working

**Breaking Change: MINIMAL**
- Old saves: crimes will be Hidden initially
- Player must manually review and suspect/convict as desired
- New crimes automatically use witness detection

**Migration Period:**
- Support both old and new crime formats
- Auto-upgrade crimes on first access
- Gradual transition as crimes expire/archive

---

## Revised Timeline

### Phase 0: Foundation ✅ COMPLETE
- FoW integration done
- Audit complete (revised)

### Phase 1: State System Addition (1 week)
- Add state fields to Crime class
- Add state query methods to Hediff_Crimes
- Add CriminalCase class
- Minimal breaking changes

### Phase 2: FoW Witness Detection (1 week)
- Integrate witness checks into crime detection
- Automatic Hidden/Suspected determination
- Test with existing crimes

### Phase 3: UI Updates (1-2 weeks)
- Reorganize tabs
- Add case display
- Update crime display with state badges
- Test with existing data

### Phase 4: Punishment Workflow (1-2 weeks)
- Add punishment selection dialog
- Integrate debt as Fine option
- Integrate hearing as Formal Trial option
- Make other punishment types (imprisonment, beating, execution)

**Revised MVP Timeline: 4-6 weeks** (down from 8-12!)

### Phase 5-11: Follow Roadmap (14-18 weeks)
- All advanced features as planned

**Revised Total Timeline: 18-24 weeks** (similar to original, but LESS RISKY)

---

## Advantages of Hybrid Approach

✅ **Keep Working Features:**
- Debt system stays (800+ lines of tested code)
- Hearing rituals stay (500+ lines of tested code)
- Contraband stays (1000+ lines of tested code)

✅ **Reduced Risk:**
- Less code rewrite = fewer bugs
- Existing systems proven to work
- Gradual enhancement vs. rebuild

✅ **Better Save Compatibility:**
- Partial backward compatibility possible
- Gradual migration path
- Less user disruption

✅ **Faster Development:**
- Build on existing foundation
- 4-6 weeks to MVP vs. 8-12 weeks
- Less testing needed for kept systems

✅ **More Features Sooner:**
- Contraband system immediately available
- Hearing rituals immediately available
- Debt system immediately available

✅ **Emergent Gameplay:**
- Player can choose debt/hearing/other punishments
- More strategic choices
- Better RimWorld integration (rituals are cool!)

---

## Revised Phase 1 Plan

### Phase 1: Add State System (1 week)

**Task 1.1:** Extend Crime Class
- Add `visibilityState` field (default Hidden)
- Add `witnesses` list
- Add `evidenceStrength` float
- Add `wasConfessed` bool
- Add `convictedTick` int
- Add `assignedPunishment` enum
- Update `ExposeData()` with backward compatibility

**Task 1.2:** Extend Hediff_Crimes
- Add `GetCrimesByState(state)` method
- Add `GetSuspectedCrimes()` convenience method
- Add `GetConvictedCrimes()` convenience method
- Add `GetHiddenCrimes()` (dev mode only)
- Add `TransitionToSuspected(crime, witnesses, evidence)` method
- Add `TransitionToConvicted(crime, punishment)` method

**Task 1.3:** Add CriminalCase Class
- Lightweight wrapper around list of Suspected crimes
- `GetOrCreateCase(pawn)` static method
- `AddCrime(crime)` method
- `GetAllCrimes()` method
- `ExposeData()` for save/load

**Task 1.4:** Add JusticeManager WorldComponent
- Registry of active cases
- `GetActiveCase(pawn)` method
- `GetAllCases()` method
- Minimal functionality (mostly a registry)

**Task 1.5:** Testing
- Create test crimes with states
- Verify save/load works
- Test backward compatibility with old crimes
- Verify state transitions

**Deliverables:**
- State system implemented
- Backward compatible with existing saves
- Build succeeds, no errors
- Basic state queries work

---

## Conclusion - REVISED

The hybrid approach **keeps the best of both worlds**:
- Existing working systems (debt, hearings, contraband)
- New roadmap features (states, witnesses, investigations)

**This is a much smarter refactor strategy** that:
- Reduces risk
- Reduces development time
- Maintains backward compatibility (partial)
- Keeps polished features
- Adds new capabilities

**Estimated Time to MVP: 4-6 weeks** (vs. 8-12 weeks)
**Breaking Changes: Minimal** (vs. Complete)
**Risk Level: Medium** (vs. High)

**Recommendation:** Proceed with HYBRID approach - extend rather than replace.
