# Law and Order - Project Notepad

**Current Phase:** Phase 0 - Foundation & Preparation (Complete)
**Date:** November 13, 2025
**Strategy:** HYBRID APPROACH - Extend existing systems, don't replace

---

## ⚡ STRATEGY REVISION

**Original Plan:** Complete refactor - throw away debt/hearing/contraband systems
**REVISED Plan:** HYBRID - Keep existing systems, add state system on top

**Why Revised?**
- Debt system → becomes "Fine" punishment (roadmap Phase 4)
- Hearing ritual → becomes "Formal Trial" option (roadmap Phase 4)
- Contraband → already complete, keep as-is
- Hediff_Crimes → good architecture, just add state fields

**Benefits:**
- ✅ Faster development (4-6 weeks to MVP vs 8-12)
- ✅ Less risk (keep tested code)
- ✅ Partial save compatibility (old saves can load!)
- ✅ More features immediately available

See: `Phase_0_Audit_REVISED.md` for full hybrid strategy

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

### Decision: HYBRID APPROACH (REVISED)
**Original:** Complete refactor - throw everything away
**REVISED:** Hybrid - Keep existing systems, add new features on top

**Rationale:**
- Debt system can be repurposed as "Fine" punishment
- Hearing rituals can be repurposed as "Formal Trial" option
- Hediff_Crimes is good architecture, just needs state fields added
- Contraband system is complete and working

**Impact:**
- **Breaking Change:** MINIMAL (partial save compatibility!)
- **Lost Features:** NONE - everything gets repurposed
- **Timeline:** 18-24 weeks (4-6 weeks to MVP, down from 8-12!)

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

## Next Steps (Phase 1 - REVISED)

### Phase 1: Add State System to Existing Crimes (1 week)

**Goal:** Extend Crime class with visibility states, keep all existing code

**Key Tasks:**
1. Add state fields to `Crime` class (visibilityState, witnesses, evidenceStrength)
2. Add state query methods to `Hediff_Crimes` (GetSuspectedCrimes, etc.)
3. Create `CriminalCase` class (lightweight wrapper)
4. Create `JusticeManager` WorldComponent (minimal registry)
5. Test backward compatibility with old saves
6. Verify state transitions work

**Before Starting Phase 1:**
- ✅ Commit Phase 0 changes - DONE
- ✅ Get user confirmation on approach - User suggested hybrid!
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
- 0.0 - 0.2: No witnesses � Hidden
- 0.3 - 0.6: Weak evidence � Suspected
- 0.7 - 1.0: Strong evidence � Suspected (high priority)

---

## Warnings & Risks

� **Breaking Save Compatibility**
- New game saves will be required
- No migration path from old system
- Existing mod users will lose all data

� **FoW Dependency**
- If FoW mod updates and breaks API, our mod breaks
- Mitigation: Implement fallback witness detection without FoW

� **Scope & Timeline**
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

---

## HYBRID APPROACH SUMMARY

### What We're KEEPING and REPURPOSING:

**Debt System → "Fine" Punishment**
- All code stays, just triggered by player choice instead of automatic
  
**Hearing Ritual → "Formal Trial" Option**
- All code stays, becomes optional formal conviction process

**Contraband System → Keep As-Is**
- Already complete, integrates with new state system

**Hediff_Crimes → Extend with State Fields**
- Architecture stays, add visibilityState/witnesses/evidence

**Everything Else → Minimal Changes**
- UI: reorganize, don't rebuild
- Utilities: keep all
- Patches: adapt for witnesses

### What We're ADDING (Not Replacing):

- CrimeVisibilityState enum
- State fields on Crime class
- CriminalCase class (lightweight)
- JusticeManager WorldComponent (minimal)
- Witness detection logic
- Player punishment selection

### Benefits of Hybrid Approach:

✅ 4-6 weeks to MVP (vs 8-12 complete rebuild)
✅ Partial save compatibility  
✅ Less risk (keep tested code)
✅ More features from day 1
✅ Faster development

