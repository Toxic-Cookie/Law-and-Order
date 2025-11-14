# Law and Order - Project Notepad

**Current Phase:** Phase 0 - Foundation & Preparation (Complete)
**Date:** November 13, 2025

---

## Phase 0 Progress Summary

###  Completed Tasks

1. **FoW Integration Setup**
   - Added Real Fog of War DLL reference to project (.csproj)
   - Updated target framework from .NET 4.7.2 to 4.8 (matches FoW requirement)
   - Build succeeds with no errors

2. **Code Audit & Documentation**
   - Created comprehensive Phase_0_Audit.md documenting:
     - Current system architecture (debt/hearing model)
     - Roadmap requirements (Hidden/Suspected/Convicted states)
     - Gap analysis between current vs. target
     - Migration strategy and refactor plan
     - Code to keep vs. replace

3. **FoW API Testing**
   - Created FogOfWarUtils.cs with:
     - `GetFoWComponent()` - Get MapComponentSeenFog
     - `IsLocationVisible()` - Check if location is in fog of war
     - `GetWitnessesAtLocation()` - Find pawns who can see a location
     - `CanPawnSeeLocation()` - Check specific pawn visibility
     - `GetPawnSightRange()` - Calculate sight range with light/weather
     - `CalculateEvidenceStrength()` - Compute evidence from witnesses
     - `IsFoWActive()` - Check if FoW mod is loaded
     - `TestFoWIntegration()` - Debug test method
   - Build confirmed - FoW APIs are accessible and working

4. **Git Branch Setup**
   - Created `phase-0-foundation` branch for this work
   - Ready to commit Phase 0 changes

---

## Key Decisions Made

### Decision: Complete Refactor (Option 2)
**Rationale:** Current debt/hearing system is incompatible with roadmap's state-based detection system. A complete refactor is required rather than incremental changes.

**Impact:**
- **Breaking Change:** Requires new game saves
- **Lost Features:** Debt system, ritual hearings, contraband system (temporarily)
- **Timeline:** 20-24 weeks for full implementation per roadmap

---

## What We're Keeping (Salvage List)

 **Utilities:**
- `ModLog.cs` - Logging system
- `CrimeUtils.cs` - Helper methods (may need refactor)
- Build system and project structure

 **UI Helpers:**
- `CrimeCategoryUIHelper.cs` - Tree-based category display
- `CrimeCategoryTreeBuilder.cs` - Category organization

 **Harmony Patching Framework:**
- Crime detection patches (adapt for new witness detection)

 **Crime Types:**
- Basic `CrimeType` enum (assault, murder, theft, etc.)
- Category definitions (inform new CrimeDefinition)

---

## What We're Replacing (Removal List)

L **Debt System (Entire)**
L **Hearing/Ritual System (Entire)**
L **Current Crime Tracking (Hediff_Crimes)**
L **Contraband System (Defer to Phase 11+)**
L **Current UI (Complete rewrite)**
L **Most WorldComponents/MapComponents**

---

## Next Steps (Phase 1)

### Phase 1: Core State System (2-3 weeks)

**Goal:** Implement three-state crime visibility system

**Key Tasks:**
1. Create `CrimeVisibilityState` enum (Hidden/Suspected/Convicted)
2. Implement `CrimeInstance` class with state tracking
3. Create `CriminalCase` class for case management
4. Implement `CompCrimeRecord` (replace Hediff_Crimes)
5. Create `JusticeManager` WorldComponent
6. Implement state transition methods
7. Testing

**Before Starting Phase 1:**
- Commit Phase 0 changes
- Get user confirmation on refactor approach
- Create Phase 1 branch

---

## Technical Notes

### FoW Integration Details

**Key API Methods:**
- `MapComponentSeenFog.IsShown(Faction, IntVec3)` - Check if cell visible to faction
- `MapComponentSeenFog.GetFactionShownCells(Faction)` - Get full visibility grid

**Witness Detection Strategy:**
1. Check if crime location is visible to player faction (FoW)
2. Find all conscious colonists on map
3. For each colonist:
   - Check sight capacity
   - Calculate effective sight range (base * capacity * light * weather)
   - Check distance to crime location
4. Build witness list
5. Calculate evidence strength (0.0-1.0) based on:
   - Number of witnesses (diminishing returns)
   - Distance (bonus for close witnesses)
   - Light level (penalty for darkness)
   - Corroboration (bonus for multiple witnesses)

**Evidence Thresholds (Planned):**
- 0.0 - 0.2: No witnesses ’ Hidden
- 0.3 - 0.6: Weak evidence ’ Suspected
- 0.7 - 1.0: Strong evidence ’ Suspected (high priority)

---

## Warnings & Risks

  **Breaking Save Compatibility**
- New game saves will be required
- No migration path from old system
- Existing mod users will lose all data

  **FoW Dependency**
- If FoW mod updates and breaks API, our mod breaks
- Mitigation: Implement fallback witness detection without FoW

  **Scope & Timeline**
- 20-24 week estimate for full implementation
- Risk of scope creep (contraband, ideology, etc.)
- Mitigation: Strict adherence to roadmap phases

---

## Current Branch Status

**Branch:** `phase-0-foundation`
**Status:** Complete, ready to commit
**Files Changed:**
- Law and Order.csproj (FoW DLL reference, .NET 4.8)
- Source/Utils/FogOfWarUtils.cs (new file)
- llms/docs/Phase_0_Audit.md (new file)
- llms/docs/Project_Notepad.md (this file)

**Next Git Action:** Commit Phase 0, then merge to master or proceed to Phase 1 branch

---

## End of Phase 0 Notes

Phase 0 is **COMPLETE**. All foundation work done:
-  FoW integration verified
-  Existing system documented
-  Migration plan created
-  Code salvage list identified
-  Test utilities created

**Phase 1 is ready to begin.**
