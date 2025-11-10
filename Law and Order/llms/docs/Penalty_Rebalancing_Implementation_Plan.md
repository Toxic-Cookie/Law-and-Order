# Penalty Rebalancing Implementation Plan

**Document Version:** 1.0
**Date:** 2025-11-10
**Status:** READY TO START
**Related Documents:**
- `Crime_Penalty_Analysis.md` - Current system analysis
- `RimWorld_Economy_Reference.md` - Economy context
- `Penalty_Balancing_Recommendations.md` - Detailed recommendations

---

## Overview

This document tracks the implementation of the crime penalty rebalancing for the Law and Order mod. The goal is to reduce typical raider debt from 6,000-12,000 silver to 2,500-5,000 silver (58% reduction) while maintaining proportionate justice.

**Key Files to Modify:**
- `WorldComponent_CrimePenaltyManager.cs` - Crime definitions and multipliers
- `DebtUtils.cs` - Penalty calculation logic and property crime multipliers
- `LawAndOrderSettings.cs` (optional) - Player customization settings

---

## Implementation Status

- **Overall Progress:** 0% (0/6 phases complete)
- **Estimated Time:** 8-12 hours total
- **Priority:** High
- **Breaking Changes:** Yes (requires testing with existing saves)

---

## Phase 1: Preparation & Analysis ⏸️

**Status:** Not Started
**Estimated Time:** 1-2 hours
**Dependencies:** None

### Objectives
- Back up current code and test with existing saves
- Verify code locations and structure
- Create testing framework
- Document baseline behavior

### Tasks

#### 1.1 Code Backup & Environment Setup
- [ ] Create git branch: `feature/penalty-rebalancing`
- [ ] Commit current state with message: "Pre-rebalancing baseline"
- [ ] Document current mod version number
- [ ] Verify HugsLib logging is enabled for testing
- [ ] Create test save file for before/after comparison

#### 1.2 Code Location Verification
- [ ] Locate `WorldComponent_CrimePenaltyManager.cs`
  - [ ] Verify `InitializeDefaultCrimes()` method exists
  - [ ] Verify `InitializeDefaultMultipliers()` method exists
  - [ ] Count current crime definitions (should be 80+)
- [ ] Locate `DebtUtils.cs`
  - [ ] Verify `CalculateDebtForNonVictimCrime()` method exists
  - [ ] Verify `ApplyMultipliers()` method exists
  - [ ] Verify property crime calculations (lines with 1.5x, 2.0x, 3.0x multipliers)
- [ ] Check if `LawAndOrderSettings.cs` exists for mod settings

#### 1.3 Baseline Testing
- [ ] Load test save and trigger small raid (5 raiders)
- [ ] Document current penalties for common scenario:
  - [ ] GunshotWound (record silver amount)
  - [ ] LimbDestruction (record silver amount)
  - [ ] Murder (record silver amount)
  - [ ] PropertyDestruction - door (record silver amount)
- [ ] Calculate total raider debt for test raid
- [ ] Take screenshots of Judiciary tab for comparison
- [ ] Export baseline crime records to text file

**Completion Criteria:**
✅ Git branch created with clean baseline
✅ All code files located and verified
✅ Baseline test data documented

---

## Phase 2: Priority Changes (Must Implement) ⏸️

**Status:** Not Started
**Estimated Time:** 3-4 hours
**Dependencies:** Phase 1 complete
**Impact:** 58% reduction in typical raider debt

### Objectives
- Reduce moderate injury penalties by 55%
- Reduce severe injury penalties by 40%
- Reduce minor injury penalties by 60%
- Reduce VictimNobility multiplier
- Add 4x multiplier stacking cap

### Tasks

#### 2.1 Update Moderate Injury Crimes (HIGH PRIORITY)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **GunshotWound:** 400-800 → 150-350
  ```csharp
  // Find: minPenalty: 400, maxPenalty: 800
  // Replace with: minPenalty: 150, maxPenalty: 350
  ```
- [ ] **StabWound:** 350-700 → 130-300
- [ ] **SlashWound:** 300-600 → 120-280
- [ ] **BluntTrauma:** 300-600 → 120-280
- [ ] **BiteWound:** 250-500 → 100-220
- [ ] **ExplosiveInjury:** 500-1,000 → 200-450
- [ ] **ArrowWound:** 300-600 → 120-280
- [ ] **ModerateBurns:** 400-800 → 150-350
- [ ] **Frostbite:** 300-600 → 120-280

**Implementation Notes:**
- Each crime is defined in `InitializeDefaultCrimes()` as:
  ```csharp
  crimeDefinitions.Add(new CrimeDefinition(
      "GunshotWound",
      "LO_Crime_GunshotWound_Label",
      "LO_Crime_GunshotWound_Desc",
      CrimeSeverity.Moderate,
      150,  // minPenalty - UPDATE THIS
      350,  // maxPenalty - UPDATE THIS
      CrimeCategory.ModerateInjury
  ));
  ```

#### 2.2 Update Severe Injury Crimes (HIGH PRIORITY)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **LimbDestruction:** 1,500-3,000 → 800-1,800
- [ ] **EyeDestruction:** 1,200-2,500 → 700-1,500
- [ ] **MajorOrganDamage:** 1,000-2,000 → 600-1,200
- [ ] **SevereBurns:** 800-1,500 → 400-900
- [ ] **MajorBloodLoss:** 800-1,200 → 400-800
- [ ] **SpineInjury:** 1,500-3,000 → 800-1,800

#### 2.3 Update Minor Injury Crimes (HIGH PRIORITY)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **Scratch:** 100-200 → 30-80
- [ ] **Bruise:** 50-150 → 20-60
- [ ] **MinorBurns:** 150-300 → 40-100
- [ ] **SuperficialWound:** 100-250 → 30-80

#### 2.4 Update VictimNobility Multiplier (CRITICAL)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultMultipliers()`

- [ ] Locate VictimNobility multiplier definition
- [ ] Change multiplier value: 3.0f → 2.0f
  ```csharp
  penaltyMultipliers.Add(new CrimePenaltyMultiplier(
      MultiplierType.VictimNobility,
      2.0f,  // Changed from 3.0f
      "LO_Multiplier_VictimNobility_Desc",
      true
  ));
  ```
- [ ] Update translation key if description mentions "3x"

#### 2.5 Add 4x Multiplier Stacking Cap (CRITICAL)
File: `DebtUtils.cs` → `ApplyMultipliers()`

- [ ] Locate the `ApplyMultipliers()` method
- [ ] Find the end of the method (before `return penalty;`)
- [ ] Add multiplier cap logic:
  ```csharp
  // Cap total multiplier at 4x base penalty
  float totalMultiplier = penalty / basePenalty;
  if (totalMultiplier > 4.0f)
  {
      penalty = basePenalty * 4.0f;

      if (Prefs.DevMode)
      {
          Mod.Log?.Message($"[Law & Order] Penalty capped: {totalMultiplier:F2}x reduced to 4.0x (from {penalty:F0} to {basePenalty * 4.0f:F0} silver)");
      }
  }
  ```
- [ ] Test with multiple multipliers to verify cap works

#### 2.6 Compile & Initial Testing
- [ ] Build project (should compile without errors)
- [ ] Copy DLL to RimWorld mods folder
- [ ] Launch RimWorld in dev mode
- [ ] Check log for any initialization errors
- [ ] Verify no red errors in debug log
- [ ] Test with dev mode spawn raid command
- [ ] Verify GunshotWound shows new range (150-350) in judiciary tab

**Completion Criteria:**
✅ All priority crime penalties updated (24 crimes total)
✅ VictimNobility multiplier reduced to 2.0x
✅ 4x multiplier cap implemented and tested
✅ Code compiles without errors
✅ Basic in-game test shows new penalties

---

## Phase 3: Secondary Changes (Recommended) ⏸️

**Status:** Not Started
**Estimated Time:** 2-3 hours
**Dependencies:** Phase 2 complete
**Impact:** Further refinement for proportionality

### Objectives
- Reduce lethal crime penalties by 15%
- Reduce property damage multipliers
- Reduce theft multipliers
- Reduce other multipliers slightly

### Tasks

#### 3.1 Update Lethal Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **Murder:** 3,000-6,000 → 2,500-5,000
- [ ] **Execution:** 5,000-10,000 → 4,000-8,000
- [ ] **VitalOrganDestruction:** 4,000-8,000 → 3,500-7,000
- [ ] **Kidnapping:** 2,500-5,000 → 2,000-4,000
- [ ] **Vaporization:** 2,000-4,000 → 2,500-5,000

#### 3.2 Update Environmental Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **Arson:** 1,000-2,000 → 500-1,200
- [ ] **ToxicGasDeployment:** 800-1,500 → 400-900
- [ ] **PsychicAttack:** 600-1,200 → 300-700
- [ ] **PsychicShock:** 400-800 → 200-450
- [ ] **EMPAttack:** 300-600 → 150-350
- [ ] **AcidBurns:** 500-1,000 → 250-550
- [ ] **ElectricalBurns:** 400-800 → 200-450

#### 3.3 Update Property Destruction Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **Sapping:** 500-1,500 → 300-800
- [ ] **Breaching:** 600-1,800 → 350-900
- [ ] **DoorDestruction:** 200-500 → 100-300
- [ ] **PowerGeneratorDestruction:** 800-2,000 → 500-1,200
- [ ] **DefensiveStructureDestruction:** 400-1,000 → 250-600
- [ ] **BuildingDestruction:** 300-800 → 200-500
- [ ] **CropDestruction:** 200-600 → 150-400

#### 3.4 Update Theft Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **RelicTheft:** 3,000-10,000 → 2,000-6,000
- [ ] **GrandTheft:** 1,000-5,000 → 700-3,000
- [ ] **WeaponTheft:** 500-2,500 → 350-1,500
- [ ] **MedicineTheft:** 400-2,000 → 300-1,200
- [ ] **Theft:** 320-1,000 → 250-700
- [ ] **PettyTheft:** 100-320 → 80-250

#### 3.5 Update Disease Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **IntentionalPlagueInfection:** 1,200-2,500 → 800-1,800
- [ ] **WoundInfection:** 300-600 → 200-450
- [ ] **ToxicBuildup:** 400-800 → 250-550

#### 3.6 Update Social Crimes
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **Trespassing:** 50-150 → 30-100
- [ ] **Insult:** 25-100 → 20-60
- [ ] **Terrorizing:** 200-500 → 150-350
- [ ] **WitnessedExecution:** 300-600 → 200-450

#### 3.7 Update Anomaly Crimes (DLC)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **FleshDevouring:** 2,500-5,000 → 2,000-4,000
- [ ] **Inhumanization:** 2,000-4,000 → 1,500-3,000
- [ ] **ShamblerInfection:** 1,500-3,000 → 1,000-2,200
- [ ] **MetalhorrorImplant:** 1,500-3,000 → 1,000-2,200
- [ ] **VoidCorruption:** 1,200-2,500 → 900-1,800
- [ ] **RevenantHypnosis:** 1,000-2,000 → 700-1,400
- [ ] **BloodRageInduction:** 600-1,200 → 400-900

#### 3.8 Update Biotech Crimes (DLC)
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultCrimes()`

- [ ] **MechSwarmAttack:** 1,000-2,000 → 700-1,500
- [ ] **WastpackMortar:** 800-1,600 → 550-1,200
- [ ] **BloodfeederAttack:** 400-800 → 300-600

#### 3.9 Update Property Crime Multipliers
File: `DebtUtils.cs` → `CalculateDebtForNonVictimCrime()`

- [ ] Locate the property crime calculation section
- [ ] **PropertyDestruction:** 2.0x → 1.5x
  ```csharp
  case CrimeType.PropertyDestruction:
      if (targetThing != null)
      {
          return targetThing.MarketValue * 1.5f; // Changed from 2.0f
      }
      return 500f;
  ```
- [ ] **Vandalism:** 1.0x → 0.75x
  ```csharp
  case CrimeType.Vandalism:
      if (targetThing != null)
      {
          return targetThing.MarketValue * 0.75f; // Changed from 1.0f
      }
      return 200f;
  ```
- [ ] **Arson:** 3.0x → 2.5x
  ```csharp
  case CrimeType.Arson:
      if (targetThing != null)
      {
          return targetThing.MarketValue * 2.5f; // Changed from 3.0f
      }
      // ... rest of arson logic
  ```
- [ ] **Theft:** 1.5x → 1.25x
  ```csharp
  case CrimeType.Theft:
      if (targetThing != null)
      {
          float itemValue = targetThing.MarketValue * targetThing.stackCount;
          return itemValue * 1.25f; // Changed from 1.5f
      }
      return 100f;
  ```

#### 3.10 Update Other Multipliers
File: `WorldComponent_CrimePenaltyManager.cs` → `InitializeDefaultMultipliers()`

- [ ] **RepeatOffender:** 1.5x → 1.3x
- [ ] **VictimAge:** 1.5x → 1.3x
- [ ] **Premeditated:** 1.5x → 1.4x
- [ ] Keep **VictimRelationship:** 1.25x (no change)
- [ ] Keep **Wartime:** 0.75x (no change)

#### 3.11 Compile & Test
- [ ] Build project
- [ ] Copy DLL to RimWorld mods folder
- [ ] Launch RimWorld and verify no errors
- [ ] Test property crime (door destruction) shows new multiplier
- [ ] Test theft shows new multiplier (1.25x item value)
- [ ] Test arson shows new multiplier (2.5x property value)

**Completion Criteria:**
✅ All remaining crime penalties updated (56+ crimes)
✅ All property crime multipliers updated
✅ All other multipliers updated
✅ Code compiles without errors
✅ Property crimes show correct new values

---

## Phase 4: Testing & Validation ⏸️

**Status:** Not Started
**Estimated Time:** 2-3 hours
**Dependencies:** Phase 3 complete
**Impact:** Verify system works as intended

### Objectives
- Test common scenarios with new penalties
- Compare before/after behavior
- Verify multiplier stacking cap works
- Check for edge cases and bugs

### Tasks

#### 4.1 Test Common Combat Scenarios
- [ ] **Test 1: Minor Combat (2-3 gunshots)**
  - [ ] Spawn colonist and raider
  - [ ] Let raider shoot colonist 2-3 times
  - [ ] Check debt amount (expect: ~500-750 silver)
  - [ ] Compare to baseline (was: ~1,200-1,800 silver)
  - [ ] Record actual values in test log

- [ ] **Test 2: Serious Combat (limb destroyed)**
  - [ ] Spawn colonist and raider
  - [ ] Use dev mode to cause limb destruction
  - [ ] Check debt amount (expect: ~1,500-2,000 silver)
  - [ ] Compare to baseline (was: ~2,000-3,000 silver)
  - [ ] Verify LimbDestruction crime recorded

- [ ] **Test 3: Murder**
  - [ ] Spawn colonist and raider
  - [ ] Let raider kill colonist
  - [ ] Check debt amount (expect: ~4,000-5,000 silver)
  - [ ] Compare to baseline (was: ~6,000-7,000 silver)
  - [ ] Verify Murder crime recorded

- [ ] **Test 4: Property Damage (door)**
  - [ ] Spawn raider near door
  - [ ] Let raider destroy stone door (50 silver value)
  - [ ] Check debt amount (expect: ~200-300 silver)
  - [ ] Verify calculation: base (~200) + (50 × 1.5) = ~275 silver
  - [ ] Compare to baseline (was: ~350 + 100 = ~450 silver)

- [ ] **Test 5: Arson**
  - [ ] Place wooden furniture (600 silver value)
  - [ ] Spawn raider with molotov/incendiary
  - [ ] Let raider burn building
  - [ ] Check debt amount (expect: ~850 + (600 × 2.5) = ~2,350 silver)
  - [ ] Compare to baseline (was: ~1,500 + (600 × 3) = ~3,300 silver)

- [ ] **Test 6: Theft**
  - [ ] Place valuable item (1,000 silver)
  - [ ] Let raider steal item
  - [ ] Check debt amount (expect: 1,000 × 1.25 = ~1,250 silver)
  - [ ] Compare to baseline (was: 1,000 × 1.5 = ~1,500 silver)

#### 4.2 Test Multiplier System
- [ ] **Test 7: Noble victim (VictimNobility multiplier)**
  - [ ] Spawn noble colonist (add royal title)
  - [ ] Let raider shoot noble 2 times
  - [ ] Check debt amount (expect: ~500 × 2.0 = ~1,000 silver)
  - [ ] Compare to baseline (was: ~1,200 × 3.0 = ~3,600 silver)
  - [ ] Verify 2.0x multiplier in debug log

- [ ] **Test 8: Child victim (VictimAge multiplier)**
  - [ ] Spawn child colonist
  - [ ] Let raider shoot child 2 times
  - [ ] Check debt amount (expect: ~500 × 1.3 = ~650 silver)
  - [ ] Compare to baseline (was: ~1,200 × 1.5 = ~1,800 silver)
  - [ ] Verify 1.3x multiplier in debug log

- [ ] **Test 9: Multiplier stacking cap**
  - [ ] Spawn noble child colonist
  - [ ] Create repeat offender raider (record previous crime)
  - [ ] Let raider shoot noble child multiple times
  - [ ] Check debug log for "Penalty capped" message
  - [ ] Verify penalty does not exceed 4x base penalty
  - [ ] Record actual multiplier and capped value

#### 4.3 Test Raid Scenarios
- [ ] **Test 10: Small raid (5 raiders)**
  - [ ] Trigger small raid with 5 raiders
  - [ ] Let combat play out naturally
  - [ ] Calculate total debt across all raiders
  - [ ] Expected total: ~2,500-4,000 silver
  - [ ] Compare to baseline (was: ~6,000-8,000 silver)
  - [ ] Calculate debt-to-loot ratio (expect: ~1.4:1)

- [ ] **Test 11: Medium raid (10 raiders)**
  - [ ] Trigger medium raid with 10 raiders
  - [ ] Let combat play out naturally
  - [ ] Calculate total debt across all raiders
  - [ ] Expected total: ~5,000-8,000 silver
  - [ ] Compare to baseline (was: ~12,000-15,000 silver)

- [ ] **Test 12: Large raid (15 raiders)**
  - [ ] Trigger large raid with 15 raiders
  - [ ] Let some raiders kill colonists
  - [ ] Calculate total debt across all raiders
  - [ ] Expected total: ~8,000-15,000 silver
  - [ ] Verify murders still result in high penalties (4,000-5,000 each)

#### 4.4 Test Edge Cases
- [ ] **Test 13: Multiple crimes same tick**
  - [ ] Create scenario where raider commits multiple crimes simultaneously
  - [ ] Verify all crimes recorded
  - [ ] Verify debt accumulates correctly

- [ ] **Test 14: Zero damage hit**
  - [ ] Create scenario where raider hits but deals 0 damage (armor blocks)
  - [ ] Verify no crime recorded or minimal penalty

- [ ] **Test 15: Friendly fire**
  - [ ] Let colonist accidentally shoot another colonist
  - [ ] Verify crime recorded for colonist (not just raiders)
  - [ ] Check if debt accumulates for colonists

- [ ] **Test 16: Property with no value**
  - [ ] Let raider destroy low-value item (< 10 silver)
  - [ ] Verify penalty still reasonable

#### 4.5 Test Save/Load Compatibility
- [ ] **Test 17: Fresh save**
  - [ ] Start new colony with updated mod
  - [ ] Trigger raid and check penalties
  - [ ] Save game
  - [ ] Load game
  - [ ] Verify penalties persist correctly
  - [ ] Verify no errors in log

- [ ] **Test 18: Existing save (migration)**
  - [ ] Load old save from Phase 1 baseline
  - [ ] Check existing prisoner debts
  - [ ] Trigger new raid
  - [ ] Verify new penalties use updated values
  - [ ] Check for any migration errors in log

#### 4.6 Performance & Stability Testing
- [ ] **Test 19: Large raid performance**
  - [ ] Trigger massive raid (30+ raiders)
  - [ ] Monitor FPS and TPS during combat
  - [ ] Check debug log for performance warnings
  - [ ] Verify crime recording doesn't lag game

- [ ] **Test 20: Long-term stability**
  - [ ] Play colony for 1-2 in-game years
  - [ ] Process multiple raids over time
  - [ ] Accumulate 20+ prisoners with debt
  - [ ] Verify no memory leaks or errors
  - [ ] Check judiciary tab performance with many records

**Completion Criteria:**
✅ All 20 test scenarios completed
✅ Results documented in test log
✅ All penalties within expected ranges
✅ Multiplier cap verified working
✅ No critical bugs or errors
✅ Performance acceptable

---

## Phase 5: Optional Enhancements ⏸️

**Status:** Not Started
**Estimated Time:** 3-4 hours
**Dependencies:** Phase 4 complete
**Impact:** Quality of life and player customization

### Objectives
- Add global penalty scale setting
- Add penalty preview in UI
- Add debt forgiveness mechanic
- Refine colony wealth multiplier (if desired)

### Tasks

#### 5.1 Add Global Penalty Scale Setting
File: `LawAndOrderSettings.cs` (or create if doesn't exist)

- [ ] Add `penaltyScaleFactor` field (float, default 1.0)
- [ ] Add UI slider in mod settings (range: 0.5 - 2.0)
- [ ] Add label showing current scale percentage
- [ ] Add tooltip explaining what this does
- [ ] Save/load setting with `ExposeData()`

File: `DebtUtils.cs` → `CalculateDebtForCrimeNew()`

- [ ] Locate final penalty calculation
- [ ] Apply global scale factor:
  ```csharp
  // Apply global penalty scale from settings
  var settings = LoadedModManager.GetMod<Mod>().GetSettings<LawAndOrderSettings>();
  finalPenalty *= settings.penaltyScaleFactor;
  ```
- [ ] Test with scale at 0.5x (half penalties)
- [ ] Test with scale at 2.0x (double penalties)

#### 5.2 Add Penalty Preview in Judiciary Tab
File: `ITab_Pawn_Judiciary.cs` (or relevant UI file)

- [ ] Add "Expected Penalties" section to UI
- [ ] Display common crimes and their current penalty ranges:
  - GunshotWound
  - LimbDestruction
  - Murder
  - PropertyDestruction
- [ ] Update display when settings change
- [ ] Add tooltip with calculation explanation

#### 5.3 Add Debt Forgiveness Mechanic
File: Judiciary tab UI or relevant command file

- [ ] Add "Forgive Debt" button in prisoner tab
- [ ] Add slider for forgiveness amount (10%-100%)
- [ ] Add confirmation dialog
- [ ] Implement debt reduction:
  ```csharp
  float reduction = debtHediff.CurrentDebt * forgivenessPercent;
  debtHediff.PayDebt(reduction, "Debt forgiveness");
  ```
- [ ] Add mood bonus thought for prisoner:
  ```csharp
  pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
      LawAndOrder_ThoughtDefOf.DebtForgiveness);
  ```
- [ ] Add translation keys for UI labels
- [ ] Test forgiveness with various amounts

#### 5.4 Add Debt Reduction Warning UI
File: Judiciary tab UI

- [ ] Add warning icon/text when debt > 5,000 silver
- [ ] Add tooltip suggesting debt reduction
- [ ] Color-code debt amounts:
  - Green: < 1,000 silver
  - Yellow: 1,000-5,000 silver
  - Red: > 5,000 silver

#### 5.5 Refine Colony Wealth Multiplier (OPTIONAL)
File: `DebtUtils.cs` → `GetColonyWealthFactor()`

**Note:** Only implement if colony wealth multiplier is enabled and desired

- [ ] Update wealth brackets:
  - < 20,000: 0.6x (was 0.5x)
  - 20,000-75,000: 1.0x (was 50,000 threshold)
  - 75,000-200,000: 1.3x (was 1.5x)
  - > 200,000: 1.5x (was 2.0x)
- [ ] Test with poor colony (< 20k wealth)
- [ ] Test with wealthy colony (> 200k wealth)
- [ ] Verify penalties scale appropriately

#### 5.6 Add Migration for Existing Saves
File: `WorldComponent_CrimePenaltyManager.cs` → `ExposeData()`

- [ ] Add version tracking field: `private int penaltySystemVersion = 1;`
- [ ] Add migration code in `PostLoadInit`:
  ```csharp
  if (Scribe.mode == LoadSaveMode.PostLoadInit)
  {
      if (penaltySystemVersion < 1)
      {
          // Old save, update to new penalty values
          InitializeDefaultCrimes();
          InitializeDefaultMultipliers();
          penaltySystemVersion = 1;

          if (Prefs.DevMode)
          {
              Mod.Log?.Message("[Law & Order] Migrated to penalty system v1");
          }
      }
  }
  ```
- [ ] Test migration with old save file
- [ ] Verify new penalties apply after migration

#### 5.7 Add "Reset to Defaults" Button
File: `LawAndOrderSettings.cs`

- [ ] Add button in mod settings: "Reset All Penalties to Defaults"
- [ ] Add confirmation dialog
- [ ] Call `InitializeDefaultCrimes()` and `InitializeDefaultMultipliers()`
- [ ] Show success message
- [ ] Test reset functionality

**Completion Criteria:**
✅ Global penalty scale setting implemented and tested
✅ UI enhancements added (preview, warnings, forgiveness)
✅ Migration system implemented
✅ All features tested and working

---

## Phase 6: Deployment & Documentation ⏸️

**Status:** Not Started
**Estimated Time:** 1-2 hours
**Dependencies:** Phase 4 complete (Phase 5 optional)
**Impact:** Release preparation

### Objectives
- Document all changes
- Update mod version
- Create changelog
- Update project documentation
- Prepare release

### Tasks

#### 6.1 Update Project Documentation
File: `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms\docs\Project_Documentation.md`

- [ ] Add new section: "Crime Penalty System"
- [ ] Document penalty calculation formula
- [ ] Document multiplier system and cap
- [ ] List all crime categories and typical ranges
- [ ] Document global penalty scale setting (if implemented)
- [ ] Add examples of common scenarios and penalties

#### 6.2 Update Project Tracker
File: `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\llms\docs\Project_Tracker.md`

- [ ] Add entry: "Penalty Rebalancing - 2025-11-10"
- [ ] List major changes:
  - Reduced moderate injuries by 55%
  - Reduced severe injuries by 40%
  - Reduced minor injuries by 60%
  - Reduced VictimNobility 3.0x → 2.0x
  - Added 4x multiplier cap
  - Updated 80+ crime penalties
  - Reduced property multipliers
- [ ] Document testing completed
- [ ] Note any remaining issues or future improvements

#### 6.3 Create Changelog Entry
File: `About/Changelog.txt` (or create if doesn't exist)

- [ ] Add version entry (e.g., "Version 1.1.0 - 2025-11-10")
- [ ] Write player-facing changelog:
  ```
  ## Major Changes
  - Rebalanced all crime penalties for better economy integration
  - Reduced typical raider debt by ~58% (now 2,500-5,000 silver instead of 6,000-12,000)
  - Moderate injuries (gunshots, stabs) reduced to be 2-3x medical costs
  - Severe injuries (limb loss) remain significant but proportionate
  - Property damage penalties reduced by 25-35%
  - Added multiplier cap to prevent extreme outliers

  ## Technical Changes
  - VictimNobility multiplier reduced from 3.0x to 2.0x
  - All 80+ crime definitions updated
  - Property damage multipliers reduced (PropertyDestruction 2.0x → 1.5x, Arson 3.0x → 2.5x)
  - Added 4x total multiplier cap to prevent stacking issues

  ## Optional Features (if implemented)
  - Added global penalty scale setting (adjust 0.5x-2.0x in mod settings)
  - Added debt forgiveness mechanic
  - Added penalty preview in Judiciary tab
  ```

#### 6.4 Update Translation Keys
File: `C:\Users\Giovanni\source\repos\Law and Order\Law and Order\Languages\English\Keyed\LawAndOrder_Keys.xml`

- [ ] Verify all crime label/description keys exist
- [ ] Add new keys for optional features (if implemented):
  - Penalty scale setting label/description
  - Debt forgiveness button/tooltip
  - Penalty preview labels
- [ ] Update any keys that reference old multiplier values (e.g., "3x for nobles" → "2x for nobles")

#### 6.5 Update About.xml
File: `About/About.xml`

- [ ] Update version number (e.g., 1.0.5 → 1.1.0)
- [ ] Update description if significant changes
- [ ] Verify dependencies listed correctly

#### 6.6 Final Testing & QA
- [ ] Full clean build of project
- [ ] Test in fresh RimWorld install (no other mods)
- [ ] Test with common mod combinations:
  - Combat Extended (if compatible)
  - HugsLib (required dependency)
  - Prison Labor (related mod)
- [ ] Verify no errors in log on:
  - Game startup
  - New colony start
  - Loading existing save
  - Raid trigger
  - Prisoner processing
- [ ] Check workshop description needs update

#### 6.7 Git Management
- [ ] Review all changes in branch
- [ ] Commit any remaining changes
- [ ] Write comprehensive commit message:
  ```
  Major rebalancing of crime penalty system

  - Reduced moderate injury penalties by 55%
  - Reduced severe injury penalties by 40%
  - Reduced minor injury penalties by 60%
  - Reduced VictimNobility multiplier from 3.0x to 2.0x
  - Added 4x multiplier stacking cap
  - Updated all 80+ crime definitions
  - Reduced property damage multipliers by 25-35%
  - [Optional] Added global penalty scale setting
  - [Optional] Added debt forgiveness mechanic

  Result: Typical raider debt reduced from 6,000-12,000 to 2,500-5,000 silver (58% reduction)
  Penalties now proportionate to actual harm: 2-3x medical costs for moderate injuries

  Testing: All 20 test scenarios passed
  Compatibility: Tested with fresh saves and migration from old saves
  ```
- [ ] Merge branch to master
- [ ] Tag release (e.g., `v1.1.0`)

#### 6.8 Backup & Archive
- [ ] Archive baseline test save file
- [ ] Archive test results and comparison data
- [ ] Save before/after screenshots
- [ ] Document any edge cases discovered

**Completion Criteria:**
✅ All documentation updated
✅ Changelog created
✅ Translation keys verified
✅ Final testing passed
✅ Git history clean and organized
✅ Ready for release

---

## Testing Checklist Summary

Quick reference for core testing (from Phase 4):

### Must Test Before Release
- [x] Small raid: Total debt 2,500-4,000 silver (not 6,000+)
- [x] GunshotWound: 150-350 silver (not 400-800)
- [x] LimbDestruction: 800-1,800 silver (not 1,500-3,000)
- [x] Murder: 2,500-5,000 silver (not 3,000-6,000)
- [x] Door destruction: ~275 silver for 50 silver door (not 450)
- [x] Noble victim: 2.0x multiplier (not 3.0x)
- [x] Multiplier cap: No penalty > 4x base
- [x] Save/load: No errors or corruption
- [x] Performance: No lag with large raids

---

## Known Issues & Future Improvements

### Current Limitations
- [ ] Colony wealth multiplier disabled (requires player testing to determine if desired)
- [ ] Faction relations multiplier implemented but untested
- [ ] No automatic debt adjustment for existing prisoners (manual reset required)

### Future Enhancement Ideas
- [ ] Add debt payment schedule preview
- [ ] Add "crime history" UI showing all past crimes
- [ ] Add notifications when prisoners finish paying debt
- [ ] Add work productivity multiplier based on debt (demoralized prisoners)
- [ ] Add reputation system: reduced penalties for reformed prisoners
- [ ] Add "plea bargain" option: reduce debt in exchange for loyalty

---

## Rollback Plan

If critical issues are discovered after deployment:

1. **Immediate Actions:**
   - [ ] Revert to previous git commit
   - [ ] Rebuild and redeploy old version
   - [ ] Notify users of issue

2. **Investigation:**
   - [ ] Document the issue in detail
   - [ ] Identify root cause
   - [ ] Determine if quick fix or full rework needed

3. **Recovery:**
   - [ ] Fix identified issues
   - [ ] Re-test thoroughly
   - [ ] Deploy patched version

---

## Success Metrics

### Primary Goals (Must Achieve)
- ✅ Typical small raid (5 raiders): 2,500-4,000 silver total debt (baseline: 6,000-8,000)
- ✅ Typical medium raid (10 raiders): 5,000-8,000 silver total debt (baseline: 12,000-15,000)
- ✅ GunshotWound penalty: 150-350 silver (baseline: 400-800)
- ✅ Murder penalty: 2,500-5,000 silver (baseline: 3,000-6,000)
- ✅ No penalty exceeds 4x base penalty
- ✅ Debt-to-loot ratio: ~1.4:1 (baseline: ~3.2:1)

### Secondary Goals (Should Achieve)
- ✅ Penalty-to-medical-cost ratio for moderate injuries: 2-3x (baseline: 4-8x)
- ✅ Property damage proportionate to replacement cost
- ✅ All 80+ crimes updated consistently
- ✅ No performance degradation
- ✅ Save/load compatibility maintained

### Player Experience Goals
- ✅ Players feel penalties are fair and proportionate
- ✅ Prisoners can realistically work off debt (40-250 days)
- ✅ Serious crimes (murder, permanent injury) still feel serious
- ✅ Raiders don't accumulate absurd debt from minor injuries

---

## Notes & Observations

### Implementation Notes
- Update this section during implementation with any findings, issues, or deviations from plan

### Testing Notes
- Update this section during testing with actual results and comparisons

### Player Feedback
- Update this section after release with player feedback and suggestions

---

**End of Implementation Plan**

**Next Action:** Complete Phase 1 (Preparation & Analysis)
