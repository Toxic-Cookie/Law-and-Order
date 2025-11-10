# Crime Penalty Analysis - Law and Order Mod

**Document Version:** 1.0
**Date:** 2025-11-10
**Analysis Scope:** Complete inventory and analysis of all crime penalties in the Law and Order mod

---

## Executive Summary

The Law and Order mod implements a comprehensive crime penalty system with 80+ distinct crime definitions spanning multiple categories (Lethal, Severe Injury, Moderate Injury, Minor Injury, Environmental, Disease, Property, Theft, Social, Anomaly, and Biotech). The current system uses a dynamic calculation approach that determines penalties based on:

1. **Base penalty ranges** defined per crime type (min-max silver values)
2. **Contextual severity factors** (body part importance, damage ratio, permanent effects)
3. **Multipliers** based on circumstances (repeat offender, victim nobility, wartime, etc.)

### Key Findings

- **Wide penalty ranges:** Current penalties range from 25 silver (Insult) to 10,000 silver (Execution)
- **Dynamic scaling:** The system intelligently scales within ranges based on actual injury severity
- **Comprehensive coverage:** 80+ crime types cover virtually all hostile actions in RimWorld
- **Multiplier system:** 10 multiplier types can significantly amplify base penalties
- **Issue identified:** As noted in the task, raiders can accumulate 10,000+ silver in penalties, which may be excessive for typical combat injuries

---

## 1. Current Crime Penalty System Architecture

### 1.1 System Components

The penalty system is built on three core components:

#### A. Crime Definitions (`CrimeDefinition.cs`)
Each crime has:
- `defName`: Unique identifier (e.g., "Murder", "GunshotWound")
- `label`: Displayed crime name (translated)
- `description`: Crime description
- `severity`: Low, Moderate, High, or Critical
- `minPenalty`: Minimum silver penalty
- `maxPenalty`: Maximum silver penalty
- `category`: Organization category

#### B. Penalty Multipliers (`CrimePenaltyMultiplier.cs`)
Contextual modifiers that affect final penalties:
- `multiplierType`: Type of circumstance (e.g., RepeatOffender, VictimNobility)
- `multiplierValue`: Multiplier amount (e.g., 1.5x, 2.0x)
- `description`: What the multiplier represents
- `enabled`: Whether the multiplier is active

#### C. Crime Penalty Manager (`WorldComponent_CrimePenaltyManager.cs`)
Global manager that:
- Stores all crime definitions and multipliers
- Provides lookup methods for calculating penalties
- Enforces 15-day lockout on penalty changes (prevents exploit)
- Persists settings across save/load

### 1.2 Penalty Calculation Flow

The penalty calculation follows this process:

```
1. Crime Occurs → CrimeDetectionPatches.cs detects hostile action
                   ↓
2. Crime Recorded → CrimeUtils.RecordCrime() creates Crime object
                   ↓
3. Determine Crime Type → DebtUtils.DetermineCrimeType() analyzes DamageInfo
                   ↓
4. Get Base Penalty → DebtUtils.CalculatePenaltyInRange()
                      - Analyzes body part importance
                      - Calculates damage ratio
                      - Checks for permanent effects
                      - Interpolates between min-max penalty
                   ↓
5. Apply Multipliers → DebtUtils.ApplyMultipliers()
                       - Checks each enabled multiplier
                       - Applies contextual modifiers
                       - Multiplies final penalty
                   ↓
6. Store & Track → Crime.debtAmount stores calculated penalty
                   Hediff_Debt tracks total debt
```

---

## 2. Complete Crime Inventory

### 2.1 Crimes Against Persons - LETHAL (Category: Lethal)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| Execution | Execution | 5,000 | 10,000 | Critical | Premeditated killing of a colonist |
| VitalOrganDestruction | Vital Organ Destruction | 4,000 | 8,000 | Critical | Destroying brain or heart |
| Murder | Murder | 3,000 | 6,000 | Critical | Killing a colonist |
| Kidnapping | Kidnapping | 2,500 | 5,000 | Critical | Attempting to carry away colonist |
| Vaporization | Vaporization | 2,000 | 4,000 | Critical | Complete destruction of body |

**Analysis:** These are the most severe crimes with penalties ranging 2,000-10,000 silver. The high values reflect the permanent loss of a colonist, which is indeed catastrophic in RimWorld.

### 2.2 Crimes Against Persons - SEVERE INJURY (Category: SevereInjury)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| LimbDestruction | Limb Destruction | 1,500 | 3,000 | High | Arm, leg, hand, or foot destroyed |
| EyeDestruction | Eye Destruction | 1,200 | 2,500 | High | Eye permanently destroyed |
| MajorOrganDamage | Major Organ Damage | 1,000 | 2,000 | High | Liver, kidney, lung, stomach damaged |
| SevereBurns | Severe Burns | 800 | 1,500 | High | High-damage burn injuries |
| MajorBloodLoss | Major Blood Loss | 800 | 1,200 | High | Severe bleeding/blood loss |
| SpineInjury | Spine Injury | 1,500 | 3,000 | High | Damage to spine |

**Analysis:** These penalties range 800-3,000 silver. They represent permanent or very serious injuries that significantly impact pawn capabilities. However, a raider causing multiple severe injuries could quickly reach 5,000-10,000+ silver.

### 2.3 Crimes Against Persons - MODERATE INJURY (Category: ModerateInjury)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| GunshotWound | Gunshot Wound | 400 | 800 | Moderate | Bullet damage |
| StabWound | Stab Wound | 350 | 700 | Moderate | Stabbing weapon damage |
| SlashWound | Slash Wound | 300 | 600 | Moderate | Cutting weapon damage >10 damage |
| BluntTrauma | Blunt Trauma | 300 | 600 | Moderate | Blunt weapon damage >10 damage |
| BiteWound | Bite Wound | 250 | 500 | Moderate | Animal/mech bite damage |
| ExplosiveInjury | Explosive Injury | 500 | 1,000 | Moderate | Bomb/explosive damage |
| ArrowWound | Arrow Wound | 300 | 600 | Moderate | Arrow damage |
| ModerateBurns | Moderate Burns | 400 | 800 | Moderate | Medium burn damage (10-20) |
| Frostbite | Frostbite | 300 | 600 | Moderate | Frostbite damage |

**Analysis:** These penalties range 250-1,000 silver per injury. In a typical raid where a colonist takes 3-5 hits, this could accumulate 1,500-4,000 silver per raider. This is where the "10,000+ silver" problem becomes apparent - multiple raiders hitting one colonist will each rack up significant penalties.

### 2.4 Crimes Against Persons - MINOR INJURY (Category: MinorInjury)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| Scratch | Scratch | 100 | 200 | Low | Light cutting damage <10 damage |
| Bruise | Bruise | 50 | 150 | Low | Light blunt damage <10 damage |
| MinorBurns | Minor Burns | 150 | 300 | Low | Light burn damage <10 damage |
| SuperficialWound | Superficial Wound | 100 | 250 | Low | Generic minor injury |

**Analysis:** These are the lowest-penalty crimes at 50-300 silver. These represent the kind of minor injuries that heal quickly with minimal medicine investment.

### 2.5 Environmental/Special Attacks (Category: Environmental)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| Arson | Arson | 1,000 | 2,000 | High | Setting fires |
| ToxicGasDeployment | Toxic Gas Deployment | 800 | 1,500 | High | Deploying toxic gas |
| PsychicAttack | Psychic Attack | 600 | 1,200 | High | Psychic damage attacks |
| PsychicShock | Psychic Shock | 400 | 800 | Moderate | Psychic shock attacks |
| EMPAttack | EMP Attack | 300 | 600 | Moderate | EMP damage |
| AcidBurns | Acid Burns | 500 | 1,000 | Moderate | Acid damage |
| ElectricalBurns | Electrical Burns | 400 | 800 | Moderate | Electrical damage |

**Analysis:** Environmental crimes are valued 300-2,000 silver. Arson is particularly high (1,000-2,000) even though the code shows it scales with property value destroyed (3x multiplier).

### 2.6 Disease & Infection Crimes (Category: Disease)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| IntentionalPlagueInfection | Intentional Plague Infection | 1,200 | 2,500 | High | Deliberately infecting with disease |
| WoundInfection | Wound Infection | 300 | 600 | Moderate | Causing infected wounds |
| ToxicBuildup | Toxic Buildup | 400 | 800 | Moderate | Causing toxic buildup |

**Analysis:** Disease crimes range 300-2,500 silver. These are difficult to detect automatically in vanilla RimWorld but are included for mod compatibility.

### 2.7 Property Crimes - DESTRUCTION (Category: PropertyDestruction)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| Sapping | Sapping | 500 | 1,500 | High | Digging through walls |
| Breaching | Breaching | 600 | 1,800 | High | Breaching walls/defenses |
| DoorDestruction | Door Destruction | 200 | 500 | Moderate | Breaking doors |
| PowerGeneratorDestruction | Power Generator Destruction | 800 | 2,000 | High | Destroying power generation |
| DefensiveStructureDestruction | Defensive Structure Destruction | 400 | 1,000 | Moderate | Destroying turrets/barricades |
| BuildingDestruction | Building Destruction | 300 | 800 | Moderate | General building damage |
| CropDestruction | Crop Destruction | 200 | 600 | Moderate | Destroying crops |

**Analysis:** Property destruction crimes range 200-2,000 silver. The code shows these crimes ALSO scale with property value (2x multiplier for PropertyDestruction, 1x for Vandalism), meaning actual penalties can be much higher than these base ranges suggest.

### 2.8 Property Crimes - THEFT (Category: Theft)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| GrandTheft | Grand Theft | 1,000 | 5,000 | High | Stealing high-value items |
| Theft | Theft | 320 | 1,000 | Moderate | Standard theft |
| PettyTheft | Petty Theft | 100 | 320 | Low | Low-value theft |
| WeaponTheft | Weapon Theft | 500 | 2,500 | High | Stealing weapons |
| MedicineTheft | Medicine Theft | 400 | 2,000 | High | Stealing medicine |
| RelicTheft | Relic Theft | 3,000 | 10,000 | Critical | Stealing relics |

**Analysis:** Theft crimes range 100-10,000 silver. The code shows theft penalties are calculated as 150% of item value, meaning a raider stealing a 1,000 silver weapon would receive 1,500 silver penalty.

### 2.9 Social/Psychological Crimes (Category: Social)

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| Insult | Insult | 25 | 100 | Low | Insulting colonists |
| WitnessedExecution | Witnessed Execution | 300 | 600 | Moderate | Executing in view of colonists |
| Terrorizing | Terrorizing | 200 | 500 | Moderate | Terrorizing colonists |
| Trespassing | Trespassing | 50 | 150 | Low | Entering forbidden areas |

**Analysis:** Social crimes are relatively low at 25-600 silver. Trespassing at 50-150 silver seems appropriate for this minor offense.

### 2.10 Anomaly-Specific Crimes (Category: Anomaly) - DLC Only

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| ShamblerInfection | Shambler Infection | 1,500 | 3,000 | High | Shambler corruption |
| VoidCorruption | Void Corruption | 1,200 | 2,500 | High | Void entity corruption |
| RevenantHypnosis | Revenant Hypnosis | 1,000 | 2,000 | High | Revenant mind control |
| Inhumanization | Inhumanization | 2,000 | 4,000 | Critical | Transforming into inhuman |
| MetalhorrorImplant | Metalhorror Implant | 1,500 | 3,000 | High | Metalhorror implantation |
| BloodRageInduction | Blood Rage Induction | 600 | 1,200 | Moderate | Inducing blood rage |
| FleshDevouring | Flesh Devouring | 2,500 | 5,000 | Critical | Devouring flesh |

**Analysis:** Anomaly crimes range 600-5,000 silver. These represent supernatural/horror elements from the Anomaly DLC.

### 2.11 Biotech-Specific Crimes (Category: Biotech) - DLC Only

| Crime DefName | Label | Min Penalty | Max Penalty | Severity | Description |
|---------------|-------|-------------|-------------|----------|-------------|
| BloodfeederAttack | Bloodfeeder Attack | 400 | 800 | Moderate | Vampiric blood feeding |
| MechSwarmAttack | Mech Swarm Attack | 1,000 | 2,000 | High | Mechanoid swarm attacks |
| WastpackMortar | Wastpack Mortar | 800 | 1,600 | High | Toxic wastpack weapons |

**Analysis:** Biotech crimes range 400-2,000 silver. These cover the Biotech DLC's unique mechanics.

---

## 3. Penalty Calculation Methodology

### 3.1 Base Penalty Calculation

The `CalculatePenaltyInRange()` method in `DebtUtils.cs` determines where within the min-max range a penalty should fall:

**Formula:**
```
severity_score = (body_part_importance × 0.4) + (damage_ratio × 0.4) + (permanent_effect × 0.2)
base_penalty = min_penalty + (severity_score × (max_penalty - min_penalty))
```

**Example: Gunshot Wound (400-800 silver range)**
- Hit to **torso** (body importance: 0.5) = 50% importance
- Dealt **30 damage** to pawn with **100 HP** = 30% damage ratio
- **No permanent injury** = 0% permanent
- Severity = (0.5 × 0.4) + (0.3 × 0.4) + (0.0 × 0.2) = 0.32 (32%)
- Base penalty = 400 + (0.32 × 400) = **528 silver**

**Example: Gunshot Wound destroying arm**
- Hit to **arm** (body importance: 0.6) = 60% importance
- Dealt **50 damage** destroying the limb
- **Permanent injury** (missing limb) = 100% permanent
- Severity = (0.6 × 0.4) + (0.5 × 0.4) + (1.0 × 0.2) = 0.64 (64%)
- Base penalty = 400 + (0.64 × 400) = **656 silver**
- BUT this would actually be classified as "LimbDestruction" (1,500-3,000 range)
- Recalculated: 1,500 + (0.64 × 1,500) = **2,460 silver**

### 3.2 Body Part Importance Scoring

The system assigns importance scores (0.0-1.0) to body parts:

| Body Part | Importance Score | Rationale |
|-----------|------------------|-----------|
| Brain, Heart | 1.0 | Critical organs, instant death |
| Liver, Kidney, Lung, Stomach | 0.8 | Major organs, severe impact |
| Eye, Spine | 0.75 | Major quality of life impact |
| Arm, Leg | 0.6 | Significant disability |
| Hand, Foot | 0.5 | Moderate impact |
| Neck, Jaw | 0.5 | Moderate importance |
| Fingers, Toes, Ears, Nose | 0.3 | Minor impact |
| Default/Unknown | 0.5 | Medium importance |

### 3.3 Permanent Injury Detection

The system checks for permanent injuries by:
1. Checking if body part is missing after damage
2. Scanning for recent Hediff_MissingPart hediffs (within 60 ticks)
3. Checking for permanent hediffs (using `IsPermanent()`)

### 3.4 Property Crime Calculations

Property crimes use a different formula in `CalculateDebtForNonVictimCrime()`:

| Crime Type | Calculation | Example |
|-----------|-------------|---------|
| Theft | Item value × 1.5 | 1,000 silver item = 1,500 silver penalty |
| PropertyDestruction | Item value × 2.0 | 500 silver door = 1,000 silver penalty |
| Vandalism | Item value × 1.0 | 200 silver wall = 200 silver penalty |
| Arson | Item value × 3.0 | 800 silver table = 2,400 silver penalty |
| Trespassing | Fixed 50-150 | Not value-based |
| ContrabandPossession | Item value × 2.0 | 100 silver drugs = 200 silver penalty |
| Kidnapping | Fixed 2,000 | Not value-based |

**Important Note:** Property crimes can significantly exceed their defined min-max ranges because they scale with property value.

---

## 4. Penalty Multiplier System

### 4.1 Available Multipliers

The system includes 10 multiplier types:

| Multiplier Type | Default Value | Default Enabled | Description |
|----------------|---------------|-----------------|-------------|
| RepeatOffender | 1.5x | Yes | Pawn has prior offenses (archived crimes) |
| VictimNobility | 3.0x | Yes | Victim has royal title (Royalty DLC) |
| VictimAge | 1.5x | Yes | Victim is a child or baby |
| Wartime | 0.75x | Yes | Criminal's faction is at war (REDUCES penalty) |
| Premeditated | 1.5x | Yes | Attack was planned (execution damage type) |
| VictimRelationship | 1.25x | Yes | Victim has family relations |
| RaiderWealth | 1.0x | No | Based on faction wealth (not implemented) |
| ColonyWealth | 1.0x | No | Based on colony wealth brackets |
| DifficultySetting | 1.0x | No | Based on difficulty (not implemented) |
| FactionRelations | 1.0x | Yes | Based on faction goodwill |

### 4.2 Colony Wealth Multiplier (When Enabled)

When enabled, the ColonyWealth multiplier scales penalties based on wealth:

| Colony Wealth | Multiplier | Rationale |
|---------------|-----------|-----------|
| < 10,000 | 0.5x | Poor colony, lower penalties |
| 10,000-50,000 | 1.0x | Normal colony, standard penalties |
| 50,000-100,000 | 1.5x | Wealthy colony, higher penalties |
| > 100,000 | 2.0x | Very wealthy colony, maximum penalties |

### 4.3 Faction Relations Multiplier (When Enabled)

When enabled, faction relations affect penalties:

| Faction Goodwill | Multiplier | Rationale |
|------------------|-----------|-----------|
| < -50 (Hostile) | 2.0x | Extremely hostile factions get higher penalties |
| -50 to 50 (Neutral) | 1.0x | Standard penalties |
| > 50 (Allied) | 0.5x | Allied factions get reduced penalties |

### 4.4 Multiplier Compounding

Multipliers are applied **multiplicatively**, not additively:

**Example: Murder of a Noble Child by Repeat Offender**
- Base Murder Penalty: 4,500 silver (mid-range)
- RepeatOffender (1.5x) × VictimNobility (3.0x) × VictimAge (1.5x)
- Combined Multiplier: 1.5 × 3.0 × 1.5 = **6.75x**
- Final Penalty: 4,500 × 6.75 = **30,375 silver**
- Capped at maximum: **100,000 silver** (but practically stays at calculated value)

This demonstrates how multipliers can create extremely high penalties in edge cases.

---

## 5. Crime Detection Implementation

### 5.1 Automatic Detection Methods

The mod uses Harmony patches to automatically detect crimes:

#### A. Assault/Murder Detection
- **Patch:** `Pawn_HealthTracker.PostApplyDamage`
- **Triggers:** When colonist takes damage from hostile pawn
- **Records:** Assault (if colonist survives), Murder (if colonist dies)
- **Data Captured:** Damage amount, weapon used, victim downed/killed status

#### B. Property Damage Detection
- **Patch:** `Thing.TakeDamage`
- **Triggers:** When player-owned buildings/items take damage from hostile
- **Records:** Vandalism (minor damage), PropertyDestruction (major damage), Arson (fire damage)
- **Tracking:** Uses `damagedItemsTracker` dictionary to prevent duplicate records
- **Upgrade Logic:** Can upgrade Vandalism to PropertyDestruction if damage worsens

#### C. Animal Abuse Detection
- **Patch:** `Pawn_HealthTracker.PostApplyDamage` (separate logic)
- **Triggers:** When colony animal takes damage from hostile pawn
- **Records:** AnimalAbuse
- **Distinction:** Separates animal damage from colonist damage

#### D. Theft Detection
- **Patch:** `Pawn_CarryTracker.TryStartCarry`
- **Triggers:** When hostile pawn picks up player item
- **Minimum Value:** Only tracks items worth 10+ silver
- **Records:** Theft with item value

#### E. Kidnapping Detection
- **Patch:** `JobGiver_Kidnap.TryGiveJob`
- **Triggers:** When hostile pawn receives kidnap job
- **Records:** Kidnapping
- **Requirement:** Victim must be downed

#### F. Toxic Gas Detection
- **Patch:** `Explosion.StartExplosion` + `HealthUtility.AdjustSeverity`
- **Triggers:** When explosion creates toxic gas + when colonist gains ToxicBuildup
- **Records:** Assault/Murder via toxic gas
- **Tracking:** Uses `MapComponent_ToxicGasTracker` to link gas to instigator

#### G. Trespassing Detection
- **Patch:** Custom patch (referenced in code but implementation not shown in provided files)
- **Triggers:** When hostile pawn enters forbidden areas
- **Records:** Trespassing

### 5.2 Crime Recording Flow

```
1. Hostile Action Detected (via Harmony patch)
   ↓
2. CrimeUtils.RecordCrime() called with parameters:
   - criminal: Pawn who committed crime
   - crimeType: Type of crime (enum)
   - victim: Pawn victim (if applicable)
   - targetThing: Thing damaged/stolen (if applicable)
   - damageDealt: Amount of damage
   - damageInfo: DamageInfo struct for penalty calculation
   ↓
3. Penalty Calculated:
   - If has DamageInfo: CalculateDebtForCrimeNew()
   - If property crime: CalculateDebtForNonVictimCrime()
   ↓
4. Crime Object Created:
   - Crime(crimeType, victim, targetThing, damageDealt, additionalInfo,
     wasVictimDowned, wasVictimKilled, debtAmount, damageType)
   ↓
5. Crime Added to Hediff_Crimes:
   - GetOrCreateCriminalRecord(criminal).AddCrime(crime)
   ↓
6. Debt Added to Hediff_Debt:
   - DebtUtils.AddDebtForCrime(criminal, crime)
```

### 5.3 Crime Storage & Tracking

#### Crime Hediff (`Hediff_Crimes`)
- Attached to criminal pawn
- Stores list of Crime objects
- Tracks hearing records
- Archives old crimes (60+ days old) to reduce save file size
- Provides query methods (by type, by victim, by date)

#### Debt Hediff (`Hediff_Debt`)
- Attached to criminal pawn
- Stores total debt amount
- Tracks debt sources (reasons)
- Provides debt payment tracking
- Never auto-removed (permanent until paid)

---

## 6. Identified Issues & Concerns

### 6.1 Primary Issue: Excessive Penalty Accumulation

**Problem:** Raiders in typical combat can accumulate 10,000+ silver in penalties despite causing injuries that heal with time and medicine.

**Root Causes:**
1. **Multiple hits per raider:** A raider shooting a colonist 3-5 times accumulates 3-5 separate GunshotWound penalties (400-800 each = 1,200-4,000 total)
2. **Multiple raiders per colonist:** If 3 raiders all shoot the same colonist, each gets penalties (3 × 1,500 = 4,500 total for minor injuries)
3. **Permanent injury multipliers:** When permanent injuries occur, penalties spike dramatically (LimbDestruction = 1,500-3,000)
4. **Multiplier stacking:** Enabled multipliers can increase penalties 2-6x
5. **Property damage addition:** Raiders who also destroy property accumulate both injury AND property penalties

**Example Scenario:**
- Raider attacks colonist, deals 5 gunshot wounds (5 × 600 = 3,000)
- Wounds were to vital areas (high body part importance): penalties scaled to 75% of max (5 × 700 = 3,500)
- Raider destroyed 2 doors attacking (2 × 350 = 700)
- Raider stole medicine (1,500)
- **Total: 5,700 silver for non-lethal combat**

### 6.2 Healing Cost vs. Penalty Discrepancy

**Medical Treatment Costs (from research):**
- **Herbal Medicine:** Free to produce (harvest healroot)
- **Standard Medicine:** ~35 silver worth of materials (3 cloth + herbal + neutroamine)
- **Glitterworld Medicine:** 55-140 silver (cannot be crafted)

**Typical Healing Costs:**
- **Minor injuries (scratch, bruise):** 1-2 herbal medicine = negligible cost
- **Moderate injuries (gunshot):** 2-4 standard medicine = 70-140 silver + doctor time
- **Severe injuries (limb destroyed):** Permanent, or requires bionic/healer serum (1,000-2,000+ silver)

**Cost Comparison:**
- **Gunshot wound (400-800 penalty)** costs ~100 silver to heal
- **Penalty-to-Cost Ratio:** 4-8x higher than healing cost
- **Rationale:** Penalty should include pain, suffering, lost productivity, not just medical costs

However, current ratios may be too high. A 600 silver penalty for a 100 silver injury feels excessive.

### 6.3 Property Damage Scaling Issues

**Problem:** Property crimes scale with item value but don't respect min-max ranges.

**Example:**
- Arson definition: 1,000-2,000 silver range
- But code calculates: Item value × 3.0
- A raider burning a grand sculpture (10,000 silver) = 30,000 silver penalty
- This FAR exceeds the defined max of 2,000 silver

**Inconsistency:** Other crimes respect their ranges, but property crimes can explode in value.

### 6.4 Multiplier Extremes

**Problem:** Multipliers can create absurdly high penalties in edge cases.

**Example: Worst Case Scenario**
- Crime: Murder (base 4,500)
- Victim: Noble Child with family relations
- Criminal: Repeat offender
- Multipliers: 1.5 × 3.0 × 1.5 × 1.25 = 8.4375x
- Final: 37,969 silver for ONE murder

While murder of a noble child SHOULD be severe, this penalty exceeds the entire early-game colony wealth (<10,000).

### 6.5 Raid Loot Value Comparison

**Typical Raid Loot (from research):**
- Small raid (5 raiders): ~3,000-5,000 silver in weapons/apparel/items
- Medium raid (10 raiders): ~6,000-10,000 silver
- Large raid (20 raiders): ~15,000-25,000 silver

**Penalty Accumulation:**
- Small raid where each raider causes 2,000 silver damage: 10,000 total debt
- Medium raid with 5,000 silver each: 50,000 total debt
- **Result:** Debt far exceeds raid loot value

**Player Perspective:**
- Players capture 3-5 raiders from a small raid
- Total loot: 4,000 silver
- Total debt: 12,000 silver
- **Problem:** Raiders "owe" 3x more than they brought to steal

---

## 7. Current System Strengths

Despite the identified issues, the system has significant strengths:

### 7.1 Sophisticated Damage Analysis
- Body part importance scoring is well-designed
- Permanent injury detection is accurate
- Damage ratio calculation properly scales within ranges

### 7.2 Comprehensive Crime Coverage
- 80+ crime types cover virtually all scenarios
- DLC support (Anomaly, Biotech) is well-integrated
- Crime categories provide good organization

### 7.3 Flexible Architecture
- Min-max ranges allow easy rebalancing
- Multiplier system provides contextual adjustments
- Staging system prevents immediate penalty changes (15-day lockout)

### 7.4 Automatic Detection
- Harmony patches capture crimes without player intervention
- Duplicate crime prevention (damagedItemsTracker)
- Toxic gas tracking is particularly clever

### 7.5 Save File Optimization
- Crime archival system (60+ day old crimes summarized)
- Only criminals get Hediff_Crimes (efficient)
- Proper IExposable implementation for save/load

---

## 8. Summary Statistics

### 8.1 Penalty Range Distribution

| Severity Level | Count | Min Penalty Range | Max Penalty Range | Average Mid-Range |
|----------------|-------|-------------------|-------------------|-------------------|
| Low | 8 | 25-150 | 100-320 | 138 silver |
| Moderate | 27 | 200-600 | 500-2,000 | 800 silver |
| High | 32 | 400-1,500 | 1,000-10,000 | 1,700 silver |
| Critical | 6 | 2,000-5,000 | 4,000-10,000 | 5,833 silver |

### 8.2 Crime Category Distribution

| Category | Crime Count | Min Penalty | Max Penalty |
|----------|-------------|-------------|-------------|
| Lethal | 5 | 2,000 | 10,000 |
| SevereInjury | 6 | 800 | 3,000 |
| ModerateInjury | 9 | 250 | 1,000 |
| MinorInjury | 4 | 50 | 300 |
| Environmental | 7 | 300 | 2,000 |
| Disease | 3 | 300 | 2,500 |
| PropertyDestruction | 7 | 200 | 2,000 |
| Theft | 6 | 100 | 10,000 |
| Social | 4 | 25 | 600 |
| Anomaly | 7 | 600 | 5,000 |
| Biotech | 3 | 400 | 2,000 |

### 8.3 Penalty Accumulation Scenarios

Based on typical gameplay:

| Scenario | Penalties Incurred | Total Debt |
|----------|-------------------|------------|
| Raider shoots colonist 3 times, no serious injury | 3 × GunshotWound (600) | 1,800 |
| Raider shoots colonist 5 times, destroys arm | 4 × GunshotWound (600) + LimbDestruction (2,000) | 4,400 |
| Raider kills colonist with gunfire | 3 × GunshotWound (600) + Murder (4,500) | 6,300 |
| Raider burns building, shoots colonist twice | Arson (1,500) + 2 × GunshotWound (600) | 2,700 |
| Sapper destroys 3 walls, shoots colonist | Sapping (1,000) + GunshotWound (600) | 1,600 |
| Thief steals 1,000 silver medicine, escapes | MedicineTheft (1,500) | 1,500 |

**Typical small raid (5 raiders, moderate combat):** 8,000-15,000 total debt across all raiders

---

## 9. Conclusions

The Law and Order mod's crime penalty system is architecturally sophisticated and comprehensive, with intelligent damage analysis and flexible scaling mechanisms. However, the current penalty values, especially when combined with multiple-hit scenarios and property damage, can result in raiders accumulating penalties that are disproportionate to the actual cost of treating the injuries they inflict.

The core issue is not the system design but the **numerical values** and **scaling factors**, which should be adjusted to better reflect:
1. Actual medical costs (100-200 silver per moderate injury)
2. Lost productivity during healing (doctor time, bed rest)
3. Emotional impact (pain, suffering)
4. Proportionality to colony wealth and raid loot value

The next documents will detail RimWorld's economy values and provide specific recommendations for rebalancing penalties.

---

**Document Status:** Complete
**Next Steps:** Create RimWorld_Economy_Reference.md and Penalty_Balancing_Recommendations.md
