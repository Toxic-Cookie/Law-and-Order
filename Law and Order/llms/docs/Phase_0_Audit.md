# Phase 0: Code Audit & Refactor Plan

**Date:** November 13, 2025
**Purpose:** Document existing system and plan complete refactor to align with Implementation Roadmap
**Decision:** Complete refactor from debt/hearing system to Hidden/Suspected/Convicted state system

---

## Executive Summary

The Law and Order mod currently implements a **debt-based punishment system with court hearings**. The Implementation Roadmap expects a **Dwarf Fortress-inspired state-based crime detection system** with Hidden/Suspected/Convicted states and Fog of War witness detection.

**These are fundamentally incompatible systems.** A complete refactor is required.

---

## Current System Architecture

### 1. Crime Tracking
- **Implementation:** `Hediff_Crimes` attached to pawns
- **Storage:** List of `Crime` objects with full details
- **Features:**
  - Crime archival (60+ day old crimes summarized)
  - Crime categories (Lethal, Injury, Property, etc.)
  - Detailed crime records with victim, damage, tick

### 2. Punishment System: DEBT-BASED
- **Core Concept:** Crimes result in financial debt that must be worked off
- **Components:**
  - `Hediff_Debt` - tracks debt amount and payment progress
  - `WorldComponent_DebtManager` - manages debt globally
  - Debt payment through enslavement work
  - Grace periods for debt-free slaves

### 3. Court Hearing System: RITUAL-BASED
- **Implementation:** Custom ritual using RimWorld's Ideology ritual system
- **Components:**
  - `HearingRecord` - tracks hearing status and outcomes
  - `RitualBehaviorWorker_CourtHearing` - ritual behavior
  - Multiple ritual roles: Judge, Defendant, Jury, Victim, Spectator
  - Courtroom furniture: Judge's bench with role assignments
  - Plea bargain outcomes affecting debt and repayment speed

### 4. Contraband System
- **Purpose:** Track forbidden items based on ideology
- **Components:**
  - `ContrabandDefinition` - defines contraband items
  - `WorldComponent_ContrabandManager` - manages contraband globally
  - `IdeologyContrabandMapper` - maps ideology precepts to contraband
  - Extensive UI for configuring contraband penalties
  - Mood effects for enforcing beliefs vs. holding contraband

### 5. User Interface
- **Main Window:** `MainTabWindow_Justice`
- **4 Tabs:**
  - Active Criminals (colonists with crimes)
  - Imprisoned (prisoners with crimes)
  - Contraband (configure forbidden items)
  - Crimes (configure crime penalties)
- **Pawn Inspector:** `ITab_Pawn_Judiciary` - shows individual pawn's crimes and debt

### 6. Supporting Systems
- **Alerts:** Overdue hearings, unreleased debtors, contraband hypocrisy
- **Thoughts:** Debt stress, holding prisoners awaiting hearing, ideology contraband
- **MapComponents:** Trespassing tracker, toxic gas tracker
- **Crime Detection Patches:** Harmony patches detecting assaults, damage, theft

---

## Roadmap Requirements (Target System)

### 1. Three-State Crime Visibility System
- **Hidden:** Crime occurred but no witnesses detected
- **Suspected:** Crime witnessed or confessed, case opened
- **Convicted:** Formally convicted by player, punishment assigned

### 2. Witness Detection via Fog of War
- **Integration:** Real Fog of War mod API
- **Mechanics:**
  - Detect colonists with line-of-sight to crime location
  - Calculate evidence strength based on witnesses, distance, light
  - Automatic Hidden vs. Suspected determination

### 3. Investigation System
- **Investigator Role:** Social skill-based
- **Interrogation:** Reveal Hidden crimes through confession
- **Evidence Collection:** Physical and testimonial evidence
- **False Confessions:** Low chance of framing innocents

### 4. Traditional Punishment System
- **Types:** Imprisonment (duration), Beating, Execution, Fine, Exile
- **Workflow:** Player convicts → selects punishment → punishment executes
- **Social Impact:** Mood effects on victim, perpetrator, witnesses, colony

### 5. Criminal Case Management
- **One Case Per Pawn:** All Suspected crimes bundled into single case
- **Case Amendment:** New crimes added to existing open case
- **Conviction Workflow:** Convict entire case at once

### 6. False Accusation System
- **Grudge-Based:** Pawns with negative relationships file false reports
- **Vampire Integration:** Vampires frame innocents for their kills
- **Truth Discovery:** Investigation can reveal false accusations

### 7. User Interface (Target)
- **3 Tabs:**
  - Open Cases (Suspected crimes awaiting conviction)
  - Convictions (Convicted crimes and active punishments)
  - Settings (debug options, mod settings)

---

## Gap Analysis: Current vs. Target

| Feature | Current System | Target System | Gap |
|---------|---------------|---------------|-----|
| **Crime Visibility** | All crimes visible immediately | Hidden/Suspected/Convicted states | **CRITICAL** - Core system missing |
| **Detection Method** | Automatic on event | FoW witness detection | **CRITICAL** - No FoW integration |
| **Punishment** | Debt-based (work off financial penalty) | Traditional (imprisonment, beatings, etc.) | **CRITICAL** - Wrong punishment model |
| **Court System** | Ritual-based hearings with plea bargains | Player-driven conviction workflow | **CRITICAL** - Wrong conviction model |
| **Investigation** | None | Interrogation reveals Hidden crimes | **MISSING** - Not implemented |
| **False Accusations** | None | Grudge & vampire frame-ups | **MISSING** - Not implemented |
| **Case Management** | Individual crimes tracked | Cases bundle multiple crimes | **CRITICAL** - Wrong data model |
| **UI Structure** | 4 tabs (criminals + config) | 3 tabs (cases + convictions + settings) | **MAJOR** - Wrong UI paradigm |
| **Contraband** | Extensive system with ideology | Not in roadmap | **EXCESS** - Remove or defer to Phase 11+ |

---

## Migration Strategy

### Phase 0: Foundation (Current Phase)
**Status:** IN PROGRESS

**Tasks:**
1. ✅ Add FoW DLL reference
2. ✅ Verify build with FoW
3. ⏳ Document existing system (this document)
4. ⏳ Test FoW API accessibility
5. ⏳ Create migration plan
6. ⏳ Identify salvageable code

### What to KEEP (Salvage)
These components can be reused or adapted:

✅ **Utilities:**
- `ModLog.cs` - Logging system (HugsLib integration)
- `CrimeUtils.cs` - General crime helper methods (may need refactoring)
- `CourtroomUtils.cs` - Might be useful for future courtroom features

✅ **UI Helpers:**
- `CrimeCategoryUIHelper.cs` - Tree-based category display
- `CrimeCategoryTreeBuilder.cs` - Category organization logic

✅ **Patches Framework:**
- Harmony patching structure for crime detection
- Can adapt existing patches to trigger new witness detection

✅ **Basic Crime Types:**
- `CrimeType` enum - assault, murder, theft, etc.
- Core crime categories can inform `CrimeDefinition` in new system

✅ **Mod Structure:**
- `Mod.cs` - HugsLib mod entry point
- `DefOf.cs` - Def references pattern
- Build system and folder structure

### What to REPLACE (Discard)
These components conflict with roadmap and must be removed:

❌ **Debt System (Entire):**
- `Hediff_Debt.cs`
- `WorldComponent_DebtManager.cs`
- `DebtUtils.cs`
- `DebtSystemPatches.cs`
- All debt-related UI, thoughts, and alerts

❌ **Hearing/Ritual System (Entire):**
- All files in `Source/Rituals/`
- `HearingRecord.cs`
- `HearingUtils.cs`
- `Dialog_ConductHearing.cs`
- Courtroom chair roles and gizmos
- Ritual integration patches

❌ **Current Crime Tracking:**
- `Hediff_Crimes.cs` (replace with `CompCrimeRecord`)
- `Crime.cs` (replace with `CrimeInstance` using new state model)

❌ **Contraband System (Defer to Phase 11+):**
- All files in `Source/Contraband/`
- Contraband UI tab
- Ideology contraband mapping
- Move to "future features" or post-release

❌ **Current UI:**
- `MainTabWindow_Justice.cs` (complete rewrite)
- `ITab_Pawn_Judiciary.cs` (simplify to show case/conviction status)

❌ **MapComponents (Evaluate):**
- `MapComponent_TrespassingTracker.cs` - May keep for Phase 7 (raid crimes)
- `MapComponent_ToxicGasTracker.cs` - Not in roadmap, remove

❌ **WorldComponents:**
- `WorldComponent_CrimePenaltyManager.cs` (replace with `JusticeManager`)
- `WorldComponent_ContrabandManager.cs` (remove with contraband system)

---

## Refactor Plan: Step-by-Step

### Step 1: Preserve Current State (Backup)
1. Create git branch for current system: `legacy-debt-system`
2. Tag current commit as `v0.9-pre-refactor`
3. Document all features in current system that will be lost

### Step 2: Gut Obsolete Systems (Phase 0 End)
1. Delete all debt-related code
2. Delete all ritual/hearing code
3. Delete contraband system (archive for later)
4. Remove old UI files
5. Update Project_Documentation.md with "DEPRECATED - See Phase_0_Audit.md"

### Step 3: Implement Core State System (Phase 1)
Follow roadmap Phase 1 exactly:
1. Create `CrimeVisibilityState` enum
2. Implement `CrimeInstance` class with state tracking
3. Create `CriminalCase` class
4. Implement `CompCrimeRecord` (replace Hediff)
5. Create `JusticeManager` WorldComponent
6. Implement state transition methods

### Step 4: FoW Integration (Phase 2)
Follow roadmap Phase 2:
1. Create `FogOfWarUtils` wrapper
2. Implement witness detection
3. Implement evidence strength calculation
4. Hook into crime detection events

### Step 5: Rebuild UI (Phase 3)
Follow roadmap Phase 3:
1. Create new `MainTabWindow_Justice` (3 tabs)
2. Implement Open Cases tab
3. Implement Convictions tab
4. Implement Settings tab

### Subsequent Phases: Follow roadmap 4-11

---

## Breaking Changes Warning

**This refactor will require a NEW GAME SAVE.**

Players will lose:
- All existing crime records
- All debt balances
- All hearing records
- All contraband configurations
- All crime penalty configurations (will reset to defaults)

**Migration is NOT POSSIBLE** due to fundamental data structure incompatibility.

---

## Technical Debt & Risks

### Risks
1. **Scope Creep:** Current system has many features (contraband, ideology, rituals) that roadmap defers to Phase 11+
2. **Lost Features:** Players using current system may be unhappy with lost debt/hearing mechanics
3. **Development Time:** Complete refactor will take ~20-24 weeks per roadmap estimate
4. **FoW Dependency:** If FoW mod breaks or changes API, our system breaks

### Mitigation
1. **Clear Communication:** Mark as alpha/beta, warn about breaking changes
2. **Feature Parity:** Ensure new system is fun before removing old
3. **Modular Design:** Keep systems decoupled for easier iteration
4. **FoW Fallback:** Implement fallback witness detection without FoW

---

## Testing Strategy for Phase 0

### FoW Integration Tests (Next Task)
1. ✅ Verify FoW DLL loads
2. ⏳ Test FoW API accessible
3. ⏳ Create test scenarios:
   - Crime with colonist nearby (visible in FoW) → detect witness
   - Crime with no colonists nearby → no witnesses
   - Crime in darkness → reduced detection
   - Crime through walls → no line-of-sight

### Build Tests
1. ✅ Project builds with FoW reference
2. ⏳ Mod loads in-game without errors
3. ⏳ No conflicts with FoW mod

---

## Next Steps (Phase 0 Continuation)

1. ⏳ **Test FoW API** - Verify we can access FoW's MapComponent and visibility data
2. ⏳ **Create test save** - Generate save with various crime scenarios for testing
3. ⏳ **Update Project_Notepad.md** - Track ongoing refactor progress
4. ⏳ **Update Project_Tracker.md** - Log refactor decision and Phase 0 progress
5. ⏳ **Get user confirmation** - Ensure user approves of migration plan before gutting code
6. 📋 **Begin Phase 1** - Start implementing core state system

---

## Conclusion

The current Law and Order mod is a complete, functional system with debt-based punishment and ritual hearings. However, it fundamentally conflicts with the Implementation Roadmap's vision of a Dwarf Fortress-inspired state-based detection system.

**A complete refactor is required.** This is not a modification or extension - it's a from-scratch rebuild using the roadmap as the blueprint.

**Estimated Time:** 20-24 weeks for full implementation (per roadmap)
**Breaking Change:** Yes - requires new game saves
**Risk Level:** High - complete system replacement

**Recommendation:** Proceed with Phase 0 → Phase 1 refactor, following Implementation Roadmap exactly.
