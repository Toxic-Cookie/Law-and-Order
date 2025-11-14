# Quick Reference: What to Keep vs. Remove

**Last Updated:** 2025-11-13
**See Full Analysis:** `Codebase_Salvage_Analysis.md` and `Salvage_Strategy_Final.md`

---

## TL;DR Decision

**Salvage Rate:** ~70% of codebase kept or adapted
**Remove Rate:** ~30% removed as incompatible
**Time Saved:** ~70 hours vs. complete rebuild

---

## Quick Lookup: File by File

### ✅ KEEP AS-IS (Use Immediately)

#### Crime Tracking
- ❌ ~~Keep debt fields~~ → **REMOVE debt fields, ADD state fields**
- ✅ `CrimeType` enum - Perfect
- ✅ `Crime.GetCrimeLabel()` - Great helper
- ✅ `CrimeSummary` - Archival system
- ✅ Archive methods - Save optimization

#### Fog of War ⭐ **BIG WIN**
- ✅ `FogOfWarUtils.cs` - **READY TO USE!**
  - GetWitnessesAtLocation()
  - CalculateEvidenceStrength()
  - CanPawnSeeLocation()
  - GetPawnSightRange()
  - **Phase 2 basically complete!**

#### Contraband ⭐
- ✅ `ContrabandDefinition.cs`
- ✅ `ContrabandUtils.cs`
- ✅ `WorldComponent_ContrabandManager.cs`
- ✅ `ContrabandCategoryNode.cs`
- ✅ `ContrabandCategoryTreeBuilder.cs`
- ✅ `IdeologyContrabandMapper.cs`
- ✅ All contraband patches
- **Entire system works as-is!**

#### Map Components
- ✅ `MapComponent_ToxicGasTracker.cs` - Keep as-is
- 🔧 `MapComponent_TrespassingTracker.cs` - Update crime call (10 min)

#### Utilities
- ✅ `ModLog.cs`
- ✅ `CrimeCategoryUIHelper.cs`
- ✅ `JudiciaryTabInjector.cs`

#### Thoughts
- ✅ `ThoughtWorker_EnforcingBeliefs.cs`
- ✅ `ThoughtWorker_ColonyHasContraband.cs`

#### Alerts
- ✅ `Alert_ContrabandHypocrisy.cs`

---

### 🔧 ADAPT (Extend/Modify)

#### Crime Tracking
- 🔧 `Hediff_Crimes.cs` - **ADD state query methods**
- 🔧 `Crime` class - **ADD state fields, REMOVE debt fields**

#### Crime Detection
- 🔧 `CrimeDetectionPatches.cs` - Update to call CrimeDetectionSystem
- 🔧 `CrimeUtils.cs` - Remove penalty methods, keep helpers

#### UI
- 🔧 `MainTabWindow_Justice.cs` - Keep structure + contraband tab, rebuild other tabs
- 🔧 `ITab_Pawn_Judiciary.cs` - Remove debt/hearing, add states/evidence

#### Thoughts
- 🔧 `ThoughtWorker_HoldingPrisonersAwaitingHearing.cs` → "Unconvicted suspects"

#### Settings
- 🔧 `LawAndOrderSettings.cs` - Remove debt/hearing settings
- 🔧 `LawAndOrderSettingsWindow.cs` - Update UI

#### Other
- 🔧 `Mod.cs` - Update references
- 🔧 `DefOf.cs` - Remove ritual/debt defs
- 🔧 `DebugActions.cs` - Add state testing actions

---

### ❌ REMOVE COMPLETELY

#### Debt System (1200+ lines)
- ❌ `Hediff_Debt.cs`
- ❌ `WorldComponent_DebtManager.cs`
- ❌ `DebtUtils.cs`
- ❌ `DebtSystemPatches.cs`
- ❌ `SlaveEmancipation_Patch.cs`

**Why:** Too complex to repurpose. Fine punishment should be simple (50 lines, not 1200).

#### Ritual System (~12 files)
- ❌ `RitualOutcomeEffectWorker_Hearing.cs`
- ❌ `RitualBehaviorWorker_CourtHearing.cs`
- ❌ `RitualRole_Defendant.cs`
- ❌ `RitualRole_Judge.cs`
- ❌ `RitualRole_Jury.cs`
- ❌ `RitualRole_Spectator.cs`
- ❌ `RitualRole_Victim.cs`
- ❌ `RitualTargetFilter_JudgesBench.cs`
- ❌ `RitualOutcomeComp_RoleCount.cs`
- ❌ `Gizmo_DesignateChairRole.cs`
- ❌ `Comp_JudgesBench.cs`
- ❌ `CompProperties_JudgesBench.cs`
- ❌ `CourtroomChairRole.cs`

**Why:** Requires Ideology DLC, too complex for core system. Save for Phase 11 optional feature.

#### Hearing System
- ❌ `HearingRecord.cs`
- ❌ `HearingUtils.cs`
- ❌ `Dialog_ConductHearing.cs`

**Why:** Tied to ritual system being removed.

#### Penalty System
- ❌ `WorldComponent_CrimePenaltyManager.cs`
- ❌ `CrimeDefinition.cs`
- ❌ `CrimePenaltyMultiplier.cs`
- ❌ `PenaltyBreakdown.cs`
- ❌ `CrimeCategoryNode.cs`
- ❌ `CrimeCategoryTreeBuilder.cs`

**Why:** Old auto-penalty approach. New system uses player choice.

#### Thoughts (Debt-Related)
- ❌ `ThoughtWorker_DebtStress.cs`
- ❌ `ThoughtWorker_DebtPaidButEnslaved.cs`
- ❌ `ThoughtWorker_HoldingDebtFreeSlave.cs`

#### Alerts
- ❌ `Alert_UnreleasedDebtors.cs`
- ❌ `Alert_OverdueHearings.cs`
- ❌ `Alert_CourtroomNeeded.cs`

#### Patches
- ❌ `CourtroomCacheInvalidation_Patch.cs`
- ❌ `RoleBasedSeating_Patch.cs`
- ❌ `Thing_GetGizmos_Patch.cs` (courtroom designation)

#### Utilities
- ❌ `DebtUtils.cs`
- ❌ `CourtroomUtils.cs`

#### Debug
- ❌ `CourtroomChairDebug.cs`

---

## Code Changes Summary

### Add to Crime Class
```csharp
// ADD:
public CrimeVisibilityState visibilityState = CrimeVisibilityState.Hidden;
public List<Pawn> witnesses = new List<Pawn>();
public float evidenceStrength = 0f;
public bool isConfessed = false;
public string caseId = null;

// REMOVE:
public float debtAmount;  // DELETE THIS
public PenaltyBreakdown penaltyBreakdown;  // DELETE THIS
```

### Add to Hediff_Crimes
```csharp
// ADD these methods:
public List<Crime> GetCrimesByState(CrimeVisibilityState state)
public List<Crime> GetSuspectedCrimes()
public List<Crime> GetConvictedCrimes()
public List<Crime> GetHiddenCrimes()  // dev mode only

public void TransitionToSuspected(Crime crime, List<Pawn> witnesses, float evidence)
public void TransitionToConvicted(Crime crime, string punishment)

// REMOVE this field:
private HearingRecord hearingRecord;  // DELETE THIS
```

### Crime Detection Pattern
```csharp
// OLD (remove):
CrimeUtils.RecordCrime(pawn, crimeType, ...);
DebtUtils.CalculateAndApplyDebt(pawn, crime);

// NEW (use):
CrimeDetectionSystem.OnCrimeCommitted(
    criminal: pawn,
    crimeType: crimeType,
    location: location,
    map: map,
    victim: victim,
    targetThing: target,
    damageDealt: damage,
    damageType: damageType
);
// Automatically:
// - Detects witnesses via FoW
// - Calculates evidence strength
// - Sets Hidden or Suspected
// - Creates case if Suspected
// - Sends notifications
```

---

## Time Estimates

### Removal Tasks
- Delete debt system files: 1 hour
- Delete ritual system files: 1 hour
- Delete penalty system files: 30 min
- Clean up references: 2-3 hours
- **Total:** ~5 hours

### Extension Tasks
- Add state fields to Crime: 1 hour
- Add state methods to Hediff_Crimes: 2 hours
- Update ExposeData: 1 hour
- Test save/load: 1 hour
- **Total:** ~5 hours

### Integration Tasks
- Create CrimeDetectionSystem: 6 hours
- Update all crime patches: 6 hours
- Update MapComponent_TrespassingTracker: 15 min
- **Total:** ~13 hours

### UI Rebuild
- Open Cases tab: 8 hours
- Convictions tab: 6 hours
- Settings tab: 3 hours
- Adapt ITab: 6 hours
- Keep contraband tab: 0 hours (already done!)
- **Total:** ~23 hours

---

## Big Wins (Time Savers)

1. **FogOfWarUtils.cs** - ⭐⭐⭐
   - **Time Saved:** ~15-20 hours
   - **Status:** READY TO USE
   - **Impact:** Phase 2 mostly complete!

2. **Contraband System** - ⭐⭐⭐
   - **Time Saved:** ~40 hours
   - **Status:** 100% compatible
   - **Impact:** Entire feature ready!

3. **Crime Tracking Architecture** - ⭐⭐
   - **Time Saved:** ~30 hours
   - **Status:** Needs state fields added
   - **Impact:** Solid foundation

4. **MapComponents** - ⭐
   - **Time Saved:** ~8 hours
   - **Status:** Minor updates needed
   - **Impact:** Specialized tracking ready

5. **UI Helpers** - ⭐
   - **Time Saved:** ~10 hours
   - **Status:** Ready to reuse
   - **Impact:** Faster UI development

**Total Time Saved: ~70 hours (9-10 days of work!)**

---

## Phase 0 Checklist

### Delete Phase (Day 1)
- [ ] Delete `Hediff_Debt.cs`
- [ ] Delete `WorldComponent_DebtManager.cs`
- [ ] Delete `DebtUtils.cs`
- [ ] Delete all debt patches
- [ ] Delete all ritual files (~12 files)
- [ ] Delete `HearingRecord.cs`, `HearingUtils.cs`, `Dialog_ConductHearing.cs`
- [ ] Delete penalty system files (~6 files)
- [ ] Delete debt thoughts (~3 files)
- [ ] Delete debt/hearing alerts (~3 files)
- [ ] Delete courtroom patches (~3 files)

### Clean Phase (Day 2)
- [ ] Remove ritual defs from XML
- [ ] Remove debt defs from XML
- [ ] Remove penalty defs from XML
- [ ] Update `DefOf.cs` (remove deleted defs)
- [ ] Fix compiler errors from deletions
- [ ] Remove unused using statements

### Extend Phase (Day 3-4)
- [ ] Add state fields to `Crime` class
- [ ] Remove debt fields from `Crime` class
- [ ] Update `Crime.ExposeData()`
- [ ] Add state methods to `Hediff_Crimes`
- [ ] Remove `hearingRecord` field from `Hediff_Crimes`
- [ ] Update `Hediff_Crimes.ExposeData()`

### Test Phase (Day 5)
- [ ] Build succeeds with no errors
- [ ] Load existing save (crimes become Hidden)
- [ ] Create new crime with state fields
- [ ] Save and load new save
- [ ] Verify state fields persist
- [ ] Verify no null reference errors

**Phase 0 Complete: 1 week**

---

## Quick Decision Guide

**"Should I keep this file?"**

1. Does it relate to debt system? → ❌ Remove
2. Does it relate to hearings/rituals? → ❌ Remove
3. Does it relate to penalty calculations? → ❌ Remove
4. Does it relate to contraband? → ✅ Keep
5. Does it relate to crime tracking? → 🔧 Extend
6. Does it relate to FoW? → ✅ Keep (it's perfect!)
7. Does it relate to UI? → 🔧 Rebuild content, keep structure
8. Is it a utility/helper? → ✅ Probably keep

---

## What Gets Built from Scratch

### Phase 1 (New Classes)
- `CrimeVisibilityState` enum
- `CriminalCase` class
- `JusticeManager` WorldComponent

### Phase 2 (New System)
- `CrimeDetectionSystem` class

### Phase 3 (New UI)
- Open Cases tab content
- Convictions tab content
- State badges/labels
- Evidence bars

### Phase 4 (New Punishments)
- `Punishment` base class
- `Punishment_Imprisonment`
- `Punishment_Beating`
- `Punishment_Execution`
- `Punishment_Fine` (simple, not debt-based!)
- `Punishment_Exile`
- `Dialog_SelectPunishment`
- Social impact system

---

## Common Questions

**Q: Why not keep debt as Fine punishment?**
A: Debt system is 1200+ lines with auto-payment, enslavement, skill calculations, grace periods, etc. Fine should be simple: amount owed, amount paid. Building new Fine from scratch = 50 lines and 4 hours. Adapting debt = 12+ hours and ongoing complexity.

**Q: Why not keep hearings as formal trials?**
A: Hearings require Ideology DLC, furniture setup, role assignments, and time to execute. Roadmap wants simple dialog-based convictions. Can rebuild better courtroom trials in Phase 11 as OPTIONAL advanced feature for players who want them.

**Q: Can old saves load?**
A: Partially. Existing crimes will load as Hidden (can manually review). New crimes auto-detect witnesses. Breaking change but minimal impact.

**Q: What about players who liked debt/hearings?**
A: Phase 11 can recreate these as OPTIONAL advanced features. Core system needs to be streamlined.

**Q: Is FogOfWarUtils really ready?**
A: YES! It implements witness detection, evidence calculation, light/weather modifiers, and everything Phase 2 needs. Just call `GetWitnessesAtLocation()` and `CalculateEvidenceStrength()`. Phase 2 integration is mostly just hooking it up to crime detection.

---

**See Full Analysis:**
- `Codebase_Salvage_Analysis.md` - Detailed file-by-file breakdown
- `Salvage_Strategy_Final.md` - Strategic rationale and timeline
- `Implementation_Roadmap.md` - Full feature roadmap
