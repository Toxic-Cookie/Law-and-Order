# Project Notepad

This file serves as a temporary working space for ongoing tasks and development notes.

---

## ACTIVE TASK: Implementing Crimes Tab in Justice Menu

### Comprehensive Crime List from RimWorld Source Code Analysis

Based on analysis of RimWorld 1.6 source code at `C:\Users\Giovanni\source\repos\Rimworld\V_1_6\Assembly-CSharp`.

---

## CRIMES AGAINST PERSONS - LETHAL

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Execution** | Critical | 5000-10000 | Instant death, 999 AP, targets vital organs |
| **Vital Organ Destruction** (Brain/Heart) | Critical | 4000-8000 | Permanent death or severe disability |
| **Murder/Killing** | Critical | 3000-6000 | Any attack resulting in death |
| **Kidnapping** | Critical | 2500-5000 | Permanent loss of colonist |
| **Vaporization** | Critical | 2000-4000 | Complete body part destruction |

## CRIMES AGAINST PERSONS - SEVERE INJURY

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Limb Destruction** | High | 1500-3000 | Arms, legs, hands, feet permanently lost |
| **Eye Destruction** | High | 1200-2500 | Permanent blindness or vision loss |
| **Major Organ Damage** | High | 1000-2000 | Liver, kidney, lung, stomach damage |
| **Severe Burns** (3rd degree) | High | 800-1500 | Permanent scarring, function loss |
| **Major Blood Loss** | High | 800-1200 | Life-threatening hemorrhaging |
| **Spine Injury** | High | 1500-3000 | Paralysis or mobility loss |

## CRIMES AGAINST PERSONS - MODERATE INJURY

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Gunshot Wound** | Moderate | 400-800 | Bullet damage, bleeding |
| **Stab Wound** | Moderate | 350-700 | Piercing injury, bleeding |
| **Slash/Cut Wound** | Moderate | 300-600 | Bladed weapon injury |
| **Blunt Trauma** | Moderate | 300-600 | Bludgeoning injury |
| **Bite Wound** | Moderate | 250-500 | Animal/human bite, infection risk |
| **Explosive Injury** | Moderate | 500-1000 | Bomb/grenade damage |
| **Arrow Wound** | Moderate | 300-600 | Projectile injury |
| **Moderate Burns** | Moderate | 400-800 | 2nd degree burns |
| **Frostbite** | Moderate | 300-600 | Cold damage to extremities |

## CRIMES AGAINST PERSONS - MINOR INJURY

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Scratch** | Low | 100-200 | Minor lacerations |
| **Bruise** | Low | 50-150 | Blunt force bruising |
| **Minor Burns** | Low | 150-300 | 1st degree burns |
| **Superficial Wound** | Low | 100-250 | Treatable minor injury |

## ENVIRONMENTAL/SPECIAL ATTACKS

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Arson/Fire Starting** | High | 1000-2000 | Intentional fire setting |
| **Toxic Gas Deployment** | High | 800-1500 | Chemical warfare (Biotech) |
| **Psychic Attack** | High | 600-1200 | Mental breaks, madness |
| **Psychic Shock** | Moderate | 400-800 | Stunning, mental trauma |
| **EMP Attack** (vs augmented) | Moderate | 300-600 | Disables implants |
| **Acid Burns** | Moderate | 500-1000 | Corrosive damage |
| **Electrical Burns** | Moderate | 400-800 | Electric shock (Anomaly) |

## DISEASE & INFECTION CRIMES

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Intentional Plague Infection** | High | 1200-2500 | Spreading disease |
| **Wound Infection** (caused) | Moderate | 300-600 | Infected combat injuries |
| **Toxic Buildup** (inflicted) | Moderate | 400-800 | Poisoning |

## PROPERTY CRIMES - DESTRUCTION

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Sapping/Mining Through Walls** | High | 500-1500 | Structural breach by sappers |
| **Breaching/Explosive Entry** | High | 600-1800 | Explosive wall destruction |
| **Door Destruction** | Moderate | 200-500 | Breaking down doors |
| **Power Generator Destruction** | High | 800-2000 | Critical infrastructure |
| **Defensive Structure Destruction** | Moderate | 400-1000 | Turrets, walls, traps |
| **Building Destruction (general)** | Moderate | 300-800 | Per building destroyed |
| **Crop Destruction** | Moderate | 200-600 | Burning/trampling fields |

## PROPERTY CRIMES - THEFT

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Grand Theft** (>1000 silver value) | High | Value × 2 | Major items stolen |
| **Theft** (320-1000 silver) | Moderate | Value × 1.5 | Standard theft (minimum 320) |
| **Petty Theft** (<320 silver) | Low | Value × 1.2 | Minor items |
| **Weapon Theft** | High | Value × 2.5 | Dangerous weapon loss |
| **Medicine Theft** | High | Value × 2 | Critical supplies |
| **Relic/Artifact Theft** | Critical | 3000+ | Irreplaceable items |

## SOCIAL/PSYCHOLOGICAL CRIMES

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Insult/Verbal Attack** | Low | 25-100 | Social interaction damage |
| **Witnessed Execution** (causing trauma) | Moderate | 300-600 | Psychological warfare |
| **Terrorizing** | Moderate | 200-500 | Intentional fear tactics |

## ANOMALY-SPECIFIC CRIMES (DLC)

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Shambler Infection** | High | 1500-3000 | Zombie conversion |
| **Void Corruption** | High | 1200-2500 | VoidTouched hediff |
| **Revenant Hypnosis** | High | 1000-2000 | Mind control |
| **Inhumanization** | Critical | 2000-4000 | Loss of humanity |
| **Metalhorror Implant** | High | 1500-3000 | Forced biomechanical conversion |
| **Blood Rage Induction** | Moderate | 600-1200 | Forced berserk state |
| **Flesh Devouring** | Critical | 2500-5000 | Partial digestion |

## BIOTECH-SPECIFIC CRIMES (DLC)

| Crime | Severity | Default Silver Penalty | Notes |
|-------|----------|------------------------|-------|
| **Bloodfeeder Attack** | Moderate | 400-800 | Vampire bite (Biotech) |
| **Mech Swarm Attack** | High | 1000-2000 | Coordinated mechanoid assault |
| **Wastpack Mortar** | High | 800-1600 | Pollution bombardment |

---

## PENALTY MULTIPLIERS (Configurable)

### Recommended Default Multipliers:

- **Repeat Offender**: +50% per prior offense (1.5x multiplier)
- **Victim Nobility** (Royalty): +100-300% (2.0x to 4.0x multiplier)
- **Victim Age** (child): +50% (1.5x multiplier)
- **Wartime vs Peacetime**: -25% during active war (0.75x multiplier)
- **Premeditated vs Opportunistic**: +50% if planned (1.5x multiplier)
- **Multiple Victims**: Cumulative penalties (additive)
- **Victim Relationship**: +25% if colonist family member (1.25x multiplier)

### Additional Adjustment Factors:

- **Raider Wealth**: Scale penalties to raider faction wealth
- **Colony Wealth**: Higher wealth colonies = higher penalties
- **Difficulty Setting**: Adjust for game difficulty
- **Storyteller**: Randy = more variance, Cassandra = balanced
- **Faction Relations**: Hostile factions = higher penalties

---

## IMPLEMENTATION PLAN

### Phase 1: Data Model & Structure
1. Create `CrimeDefinition` class to define individual crimes
   - Properties: defName, label, description, severity enum, defaultPenalty (min/max range), category
2. Create `CrimePenaltyMultiplier` class for multiplier settings
   - Properties: multiplierType enum, multiplierValue, description
3. Create `WorldComponent_CrimePenaltyManager` similar to `WorldComponent_ContrabandManager`
   - Stores crime definitions and current penalty values
   - Stores multiplier settings
   - Has lockout mechanism (similar to contraband)
   - ExposeData for save/load
4. Create pending change system (like contraband)
   - `PendingCrimePenaltyChange` class
   - Tracks staged changes before commit

### Phase 2: UI Implementation
1. Add "Crimes" tab to `MainTabWindow_Justice.cs`
   - Add `JusticeTab.Crimes` to enum
   - Add tab record in PreOpen()
2. Implement `DrawCrimesUI(Rect inRect)` method
   - Similar structure to `DrawContrabandUI`
   - Left panel: Crime list (categorized)
   - Right panel: Configuration panel
   - Bottom: Commit/Cancel buttons with lockout display
3. Create crime category tree builder
   - Categories: Lethal, Severe Injury, Moderate Injury, Minor Injury, Environmental, Disease, Property Destruction, Theft, Social, Anomaly (DLC), Biotech (DLC)
4. Implement multipliers section in right panel
   - Separate expandable section below crime configuration
   - List all multipliers with input fields
   - Apply button (included in commit system)

### Phase 3: Integration (NEXT PHASE)
**Goal:** Integrate the crime penalty management system with the existing crime detection and debt calculation system.

**Detailed Tasks:**

1. **Update DebtUtils.cs** - Main integration point
   - Add `CalculatePenaltyInRange(CrimeDefinition, DamageInfo, Pawn)` method
     - Implement body part importance scoring (0.0-1.0 scale)
     - Calculate damage ratio relative to victim health
     - Detect permanent injuries (limb loss, organ destruction)
     - Interpolate penalty within min-max range using Mathf.Lerp()
   - Add `ApplyMultipliers(basePenalty, manager, criminal, victim, damageInfo)` method
     - Loop through all enabled multipliers
     - Check context for each multiplier type (repeat offender, nobility, etc.)
     - Apply multipliers multiplicatively (not additively)
   - Add helper methods:
     - `GetBodyPartImportance(BodyPartRecord)` - Score 0.0-1.0 based on part type
     - `HasPriorOffenses(Pawn)` - Check crime tracker for prior convictions
     - `IsWartime(Faction)` - Check if faction is hostile to player
     - `WasPremeditated(DamageInfo)` - Detect execution or planned attacks
     - `IsFamily(Pawn, Pawn)` - Check for family relationships
     - `GetFactionWealthFactor(Faction)` - Scale based on faction wealth
     - `GetColonyWealthFactor()` - Scale based on colony wealth (use WealthUtility)
     - `GetDifficultyFactor()` - Scale based on storyteller difficulty
     - `GetFactionRelationFactor(Faction)` - Scale based on goodwill (-100 to 100)
   - Add `DetermineCrimeType(DamageInfo, Pawn)` method
     - Map damage type to crime defName (e.g., Bullet → "GunshotWound")
     - Detect death crimes (Execution, Murder, VitalOrganDestruction)
     - Detect severe injuries (LimbDestruction, EyeDestruction)
     - Fall back to SuperficialWound for unknown damage types
   - Update existing `CalculateDebtForCrime()` to use new system
     - Get WorldComponent_CrimePenaltyManager.Instance
     - Call DetermineCrimeType() to identify crime
     - Get CrimeDefinition from manager
     - Calculate base penalty in range
     - Apply multipliers
     - Clamp result (1 to 100,000 silver)
     - Add fallback to legacy calculation if manager not available

2. **Update Crime Detection Points** - Where crimes are currently detected
   - Find all locations where `DebtUtils.AddDebt()` is called
   - Update to call new `CalculateDebtForCrime()` with crime type
   - Pass DamageInfo for context-aware penalty calculation
   - Update debt reason string to include crime type and penalty amount

3. **Add Missing Helper Methods to DebtUtils**
   - `IsPermanentInjury(DamageInfo)` - Detect if injury causes permanent damage
   - `IsLimbDestroyed(DamageInfo)` - Check if limb was destroyed
   - `IsEyeDestroyed(DamageInfo)` - Check if eye was destroyed
   - `IsVitalOrganDestroyed(DamageInfo)` - Check if brain/heart destroyed

4. **Testing & Validation**
   - Test penalty calculation for each crime category
   - Verify multipliers apply correctly (enable/disable each one)
   - Test extreme cases (max multipliers, minimum damage, etc.)
   - Verify backward compatibility (legacy calculation fallback)
   - Test save/load with new penalty values
   - Test DLC detection (Anomaly/Biotech crimes)

5. **Performance Optimization** (if needed)
   - Cache penalty calculations to avoid recalculating every frame
   - Consider caching manager instance access
   - Profile performance with large crime lists

### Phase 4: Testing & Polish
1. Test lockout system
2. Test commit/cancel functionality
3. Test save/load persistence
4. Test multiplier application
5. Verify UI responsiveness and layout

---

## Key Design Patterns to Follow

### Pattern: Contraband Tab Style
- Use pending changes system (don't apply immediately)
- 15-day lockout after commit (god mode bypass)
- Staged changes indicator (yellow highlight)
- Commit/Cancel buttons at bottom
- Search functionality
- Scroll views for large lists

### Pattern: Category Tree
- Expandable/collapsible categories
- Show count of crimes vs total
- Allow bulk operations on categories
- Visual hierarchy with indentation

### Pattern: Configuration Panel
- Header with crime name
- Description box
- Stats/info box
- Input fields for penalty values (min/max range)
- Action buttons (Update/Add/Remove)

### Pattern: Multipliers Section
- Separate collapsible section
- Each multiplier has:
  - Label
  - Description tooltip
  - Numeric input field
  - Enabled/disabled toggle
- Include in pending changes system

---

## Files to Create/Modify

### New Files:
- `Source/CrimePenalties/CrimeDefinition.cs`
- `Source/CrimePenalties/CrimePenaltyMultiplier.cs`
- `Source/CrimePenalties/PendingCrimePenaltyChange.cs`
- `Source/Components/WorldComponent_CrimePenaltyManager.cs`
- `Source/CrimePenalties/CrimeCategoryTreeBuilder.cs` (similar to ContrabandCategoryTreeBuilder)
- `Source/CrimePenalties/CrimeCategoryNode.cs`

### Files to Modify:
- `Source/UI/MainTabWindow_Justice.cs` - Add Crimes tab
- `Source/Utils/DebtUtils.cs` - Use penalty manager for calculations
- `Languages/English/Keyed/LawAndOrder_Keys.xml` - Add translation keys

### Defs to Create (Optional):
- `Defs/CrimeDefs/` - XML defs for crimes (if using def system)
  - Could define crimes in XML for moddability
  - Or hardcode in C# for simplicity

---

## Notes & Considerations

1. **DLC Detection**: Need to check for Anomaly/Biotech DLC before showing those crimes
2. **Moddability**: Consider allowing other mods to add custom crimes via defs
3. **Performance**: Cache penalty calculations to avoid recalculating every frame
4. **Compatibility**: Ensure backward compatibility with existing saves (use defaults for missing data)
5. **Validation**: Min/max penalty validation (prevent negative or excessive values)
6. **UI Caps**: Similar to contraband, limit penalty ranges (e.g., 1-10000 silver)
7. **Multiplier Stacking**: Define clear rules for how multipliers combine (additive vs multiplicative)

---

## Current Status: Phase 2 Complete ✅

### Phase 1 Complete ✅
**Completed:**
- ✅ CrimeDefinition class with pending changes system
- ✅ CrimePenaltyMultiplier class with pending changes system
- ✅ CrimeCategoryNode class for tree organization
- ✅ CrimeCategoryTreeBuilder class for tree operations
- ✅ WorldComponent_CrimePenaltyManager with 70+ default crimes
- ✅ 10 default penalty multipliers
- ✅ DLC detection for Anomaly/Biotech crimes
- ✅ Lockout system (15 days, god mode bypass)
- ✅ ExposeData implementation for save/load

**Files Created:**
1. `Source/CrimePenalties/CrimeDefinition.cs`
2. `Source/CrimePenalties/CrimePenaltyMultiplier.cs`
3. `Source/CrimePenalties/CrimeCategoryNode.cs`
4. `Source/CrimePenalties/CrimeCategoryTreeBuilder.cs`
5. `Source/Components/WorldComponent_CrimePenaltyManager.cs`

### Phase 2 Complete ✅
**Completed:**
- ✅ Added JusticeTab.Crimes enum value
- ✅ Added Crimes tab record in PreOpen()
- ✅ Implemented DrawCrimesUI(Rect inRect) method
- ✅ Created crime list left panel with category tree
- ✅ Created configuration panel for individual crimes
- ✅ Created configuration panel for crime categories (bulk operations)
- ✅ Implemented multipliers section with edit functionality
- ✅ Added commit/cancel buttons with lockout display
- ✅ Added all translation keys to LawAndOrder_Keys.xml

**Files Modified:**
1. `Source/UI/MainTabWindow_Justice.cs` - Added Crimes tab UI (~1000 lines)
2. `Languages/English/Keyed/LawAndOrder_Keys.xml` - Added 20+ translation keys

**UI Features Implemented:**
- Searchable crime category tree (left panel)
- Individual crime configuration (min/max penalty ranges)
- Category bulk operations (update all crimes in category)
- Penalty multipliers list with edit functionality
- Pending changes tracking with visual indicators
- Commit/Cancel buttons with 15-day lockout
- God mode bypass for lockout
- Tooltips and validation messages

### Bug Fixes Applied (Phase 2 Complete)
- ✅ Fixed tab label capitalization ("crimes" → "Crimes")
- ✅ Fixed WorldComponent initialization (crimes now populate on new worlds)
- ✅ Added all 70+ crime translation keys (labels + descriptions)
- ✅ Added all 10 multiplier translation keys (labels + descriptions)
- ✅ Total translation keys added: ~160 keys

**Files Updated:**
- `MainTabWindow_Justice.cs` - Fixed tab label
- `WorldComponent_CrimePenaltyManager.cs` - Added constructor initialization
- `LawAndOrder_Keys.xml` - Added 160+ translation keys

**Next Step:** Begin Phase 3 (Integration) when ready.

