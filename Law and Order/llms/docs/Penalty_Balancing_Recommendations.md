# Penalty Balancing Recommendations - Law and Order Mod

**Document Version:** 1.0
**Date:** 2025-11-10
**Purpose:** Comprehensive rebalancing recommendations for crime penalties
**Status:** READY FOR IMPLEMENTATION

---

## Executive Summary

Based on comprehensive analysis of the Law and Order mod's penalty system and RimWorld's economy, this document provides specific numerical recommendations for rebalancing crime penalties. The goal is to create a system where:

1. **Raiders accumulate realistic debt** (typically 500-2,000 silver per raider, not 10,000+)
2. **Penalties are proportionate to harm** (2-3x medical costs, matching actual impact)
3. **Players can use the system** (debt amounts prisoners can realistically work off)
4. **Serious crimes remain serious** (murder, kidnapping, permanent injuries stay severe)
5. **The system feels fair and intuitive** (penalties match player expectations)

### Key Philosophical Principles

1. **Penalties = Medical Costs + Pain & Suffering + Lost Productivity**
   - Medical costs alone are too low (100-200 silver)
   - Multiplier of 2-3x accounts for non-medical impacts
   - Permanent injuries warrant higher multipliers (4-6x)

2. **Proportionality to Colony Wealth**
   - Early game colonies (10k wealth) have different tolerance than late game (300k wealth)
   - But base penalties should work for average mid-game colony (50-100k wealth)

3. **Multiple-Hit Scenarios Must Be Considered**
   - A raider typically hits a colonist 3-5 times in combat
   - Penalties should not multiply excessively in normal combat

4. **Property Damage Should Be Proportionate**
   - Destroying a 50-silver door should not result in 1,000-silver penalty
   - Current 2x multiplier is reasonable, but base ranges need adjustment

---

## 1. Recommended Penalty Adjustments - CRIMES AGAINST PERSONS

### 1.1 Lethal Crimes (Category: Lethal)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| Execution | 5,000 | 10,000 | **4,000** | **8,000** | Reduce slightly; still highest penalty |
| VitalOrganDestruction | 4,000 | 8,000 | **3,500** | **7,000** | Death crime, but less premeditated than execution |
| **Murder** | **3,000** | **6,000** | **2,500** | **5,000** | Most common death crime; 20% reduction |
| Kidnapping | 2,500 | 5,000 | **2,000** | **4,000** | Serious but victim survives; moderate reduction |
| Vaporization | 2,000 | 4,000 | **2,500** | **5,000** | Rare but horrific; slight increase |

**Rationale:**
- Murder penalties should roughly equal colonist value (2,500-5,000 matches skilled colonist value)
- Death is permanent loss, justifying high penalties
- 20% reduction prevents excessive accumulation when raiders kill multiple colonists
- These penalties should "hurt" but not be impossible to work off (100-250 days of labor)

**Example Calculations (Murder):**
- Base penalty at 50% severity: 2,500 + (0.5 × 2,500) = **3,750 silver**
- With RepeatOffender (1.5x): 3,750 × 1.5 = **5,625 silver**
- With VictimNobility (3.0x): 3,750 × 3.0 = **11,250 silver** (justified for noble victim)

### 1.2 Severe Injury Crimes (Category: SevereInjury)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| **LimbDestruction** | **1,500** | **3,000** | **800** | **1,800** | Major reduction; bionic arm costs ~1,000 silver |
| **EyeDestruction** | **1,200** | **2,500** | **700** | **1,500** | Major reduction; bionic eye costs ~700 silver |
| **MajorOrganDamage** | **1,000** | **2,000** | **600** | **1,200** | Major reduction; organ treatment ~300-500 silver |
| **SevereBurns** | **800** | **1,500** | **400** | **900** | Major reduction; glitterworld medicine ~200-400 silver |
| MajorBloodLoss | 800 | 1,200 | **400** | **800** | Significant reduction; treatment ~200 silver |
| **SpineInjury** | **1,500** | **3,000** | **800** | **1,800** | Major reduction; paralysis is severe but treatable |

**Rationale:**
- Severe injuries are permanent or very expensive to treat
- Penalties should be 4-6x medical cost (accounts for bionic replacement if needed)
- Limb destruction penalty (800-1,800) aligns with bionic arm (1,000) + installation (~200) + recovery
- These injuries are life-changing, justifying higher penalties than moderate injuries
- **Current penalties are 2-3x too high** for typical gameplay

**Example Calculations (LimbDestruction):**
- Base penalty at 60% severity: 800 + (0.6 × 1,000) = **1,400 silver**
- Typical case: **1,400 silver** (compared to 1,200 silver bionic replacement cost)
- This is ~4x the medical treatment cost, which is appropriate for permanent injury

### 1.3 Moderate Injury Crimes (Category: ModerateInjury)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| **GunshotWound** | **400** | **800** | **150** | **350** | Major reduction; treatment costs ~100 silver |
| **StabWound** | **350** | **700** | **130** | **300** | Major reduction; similar to gunshot |
| **SlashWound** | **300** | **600** | **120** | **280** | Major reduction; slightly less severe |
| **BluntTrauma** | **300** | **600** | **120** | **280** | Major reduction; similar to slash |
| **BiteWound** | **250** | **500** | **100** | **220** | Major reduction; less severe than stab |
| **ExplosiveInjury** | **500** | **1,000** | **200** | **450** | Major reduction; more severe but still moderate |
| **ArrowWound** | **300** | **600** | **120** | **280** | Major reduction; similar to slash |
| **ModerateBurns** | **400** | **800** | **150** | **350** | Major reduction; treatment ~150 silver |
| Frostbite | 300 | 600 | **120** | **280** | Major reduction; similar treatment |

**Rationale:**
- **This category had the most excessive penalties**
- Moderate injuries heal in 3-5 days with 50-150 silver of medicine
- Penalties of 400-800 (current) are 4-8x medical costs
- **Recommended penalties are 2-3x medical costs**, which is appropriate
- **CRITICAL:** In typical combat, a colonist takes 3-5 hits, so:
  - Current system: 3 × 600 = **1,800 silver per raider**
  - Recommended system: 3 × 250 = **750 silver per raider**
  - This prevents the "10,000+ silver" problem mentioned in the task

**Example Calculations (GunshotWound):**
- **Multiple-hit scenario (most common):**
  - Raider shoots colonist 4 times during raid
  - Current penalties: 4 × 600 = **2,400 silver**
  - Recommended penalties: 4 × 250 = **1,000 silver**
  - Medical cost: ~400 silver total
  - Recommended ratio: **2.5:1** (reasonable)

**Impact Analysis:**
- Small raid (5 raiders, each shoots colonist twice):
  - Current: 5 × (2 × 600) = **6,000 silver total debt**
  - Recommended: 5 × (2 × 250) = **2,500 silver total debt**
  - **Reduction: 58%** (brings debt in line with raid loot value ~2,500)

### 1.4 Minor Injury Crimes (Category: MinorInjury)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| **Scratch** | **100** | **200** | **30** | **80** | Major reduction; heals in hours, ~10 silver to treat |
| **Bruise** | **50** | **150** | **20** | **60** | Major reduction; heals quickly, ~10 silver to treat |
| **MinorBurns** | **150** | **300** | **40** | **100** | Major reduction; minor treatment needed |
| SuperficialWound | 100 | 250 | **30** | **80** | Major reduction; generic minor injury |

**Rationale:**
- Minor injuries are trivial in RimWorld - heal in hours with minimal medicine
- Current penalties (50-300) are 5-30x medical costs - way too high
- **Recommended penalties are 2-3x medical costs**
- These injuries shouldn't accumulate significant debt
- Minor injuries often happen incidentally (stray bullets, etc.)

**Example Calculations (Bruise):**
- Base penalty at 40% severity: 20 + (0.4 × 40) = **36 silver**
- Medical cost: ~10 silver herbal medicine
- Ratio: **3.6:1** (reasonable for minor inconvenience)

---

## 2. Recommended Penalty Adjustments - ENVIRONMENTAL CRIMES

### 2.1 Environmental/Special Attacks (Category: Environmental)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| **Arson** | **1,000** | **2,000** | **500** | **1,200** | Moderate reduction; scales with property value (3x) |
| **ToxicGasDeployment** | **800** | **1,500** | **400** | **900** | Moderate reduction; causes damage over time |
| PsychicAttack | 600 | 1,200 | **300** | **700** | Moderate reduction; psychological harm |
| PsychicShock | 400 | 800 | **200** | **450** | Moderate reduction; temporary effect |
| EMPAttack | 300 | 600 | **150** | **350** | Moderate reduction; damages mechanoids mainly |
| AcidBurns | 500 | 1,000 | **250** | **550** | Moderate reduction; similar to severe burns |
| ElectricalBurns | 400 | 800 | **200** | **450** | Moderate reduction; similar to moderate burns |

**Rationale:**
- Environmental crimes create indirect harm (property damage, area denial, panic)
- Arson is particularly destructive - 3x property value multiplier is appropriate
- Base ranges should be moderate since actual damage is calculated separately
- ToxicGas creates assault crimes separately (via damage tracking), so base should be moderate

**Special Note on Arson:**
- Current: 1,000-2,000 base + (property value × 3)
- Recommended: 500-1,200 base + (property value × 3)
- **Example:** Burning 500 silver building = 500 base + (500 × 3) = **2,000 total** (reasonable)

---

## 3. Recommended Penalty Adjustments - PROPERTY CRIMES

### 3.1 Property Destruction (Category: PropertyDestruction)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| Sapping | 500 | 1,500 | **300** | **800** | Moderate reduction; infiltration tactic |
| Breaching | 600 | 1,800 | **350** | **900** | Moderate reduction; aggressive entry |
| DoorDestruction | 200 | 500 | **100** | **300** | Moderate reduction; common occurrence |
| PowerGeneratorDestruction | 800 | 2,000 | **500** | **1,200** | Moderate reduction; high-value target |
| DefensiveStructureDestruction | 400 | 1,000 | **250** | **600** | Moderate reduction; strategic target |
| BuildingDestruction | 300 | 800 | **200** | **500** | Moderate reduction; general damage |
| CropDestruction | 200 | 600 | **150** | **400** | Minor reduction; affects food supply |

**Rationale:**
- Property crimes use **2x property value multiplier** in code
- Base ranges should be lower since actual property value is calculated
- **Example: Door destruction**
  - Stone door value: 50 silver
  - Current penalty: 200-500 base + (50 × 2) = 300-600 silver (door itself is only 50!)
  - Recommended penalty: 100-300 base + (50 × 2) = 200-400 silver (more reasonable)
- Most property damage in raids is LOW value (doors, walls = 50-100 silver)

**Property Damage Multiplier Adjustment:**
- **Current multipliers:**
  - PropertyDestruction: 2x property value
  - Vandalism: 1x property value
  - Arson: 3x property value
- **Recommended multipliers:**
  - PropertyDestruction: **1.5x** property value (reduced from 2x)
  - Vandalism: **0.75x** property value (reduced from 1x)
  - Arson: **2.5x** property value (reduced from 3x)
  - Theft: **1.25x** property value (reduced from 1.5x)

**Impact Example (Door Destruction):**
- Stone door: 50 silver value
- Current: Base 350 + (50 × 2) = **450 silver penalty**
- Recommended: Base 200 + (50 × 1.5) = **275 silver penalty**
- **Reduction: 39%** (still significant punishment, but proportionate)

### 3.2 Theft Crimes (Category: Theft)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| RelicTheft | 3,000 | 10,000 | **2,000** | **6,000** | Moderate reduction; still very serious |
| GrandTheft | 1,000 | 5,000 | **700** | **3,000** | Moderate reduction; scales with value |
| WeaponTheft | 500 | 2,500 | **350** | **1,500** | Moderate reduction; scales with value |
| MedicineTheft | 400 | 2,000 | **300** | **1,200** | Moderate reduction; scales with value |
| Theft | 320 | 1,000 | **250** | **700** | Moderate reduction; standard theft |
| PettyTheft | 100 | 320 | **80** | **250** | Minor reduction; low-value items |

**Rationale:**
- Theft penalties scale with item value (currently 1.5x, recommend 1.25x)
- Base ranges represent minimum penalties for generic thefts
- **Theft multiplier reduction from 1.5x to 1.25x is appropriate**
  - Current: Stealing 1,000 silver weapon = 1,500 penalty
  - Recommended: Stealing 1,000 silver weapon = 1,250 penalty
  - Still punitive but not excessive
- Relic theft should remain very high (valuable quest items)

---

## 4. Recommended Penalty Adjustments - SOCIAL/OTHER CRIMES

### 4.1 Disease Crimes (Category: Disease)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| IntentionalPlagueInfection | 1,200 | 2,500 | **800** | **1,800** | Moderate reduction; very serious but hard to detect |
| WoundInfection | 300 | 600 | **200** | **450** | Moderate reduction; complications from wounds |
| ToxicBuildup | 400 | 800 | **250** | **550** | Moderate reduction; environmental toxin |

### 4.2 Social/Psychological Crimes (Category: Social)

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| Trespassing | 50 | 150 | **30** | **100** | Minor reduction; nuisance offense |
| Insult | 25 | 100 | **20** | **60** | Minor reduction; lowest penalty |
| Terrorizing | 200 | 500 | **150** | **350** | Minor reduction; psychological harm |
| WitnessedExecution | 300 | 600 | **200** | **450** | Minor reduction; mood impact |

### 4.3 Anomaly Crimes (Category: Anomaly) - DLC

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| FleshDevouring | 2,500 | 5,000 | **2,000** | **4,000** | Minor reduction; horrific but rate |
| Inhumanization | 2,000 | 4,000 | **1,500** | **3,000** | Moderate reduction; transformation crimes |
| ShamblerInfection | 1,500 | 3,000 | **1,000** | **2,200** | Moderate reduction; corruption effect |
| MetalhorrorImplant | 1,500 | 3,000 | **1,000** | **2,200** | Moderate reduction; implantation |
| VoidCorruption | 1,200 | 2,500 | **900** | **1,800** | Moderate reduction; void effect |
| RevenantHypnosis | 1,000 | 2,000 | **700** | **1,400** | Moderate reduction; mind control |
| BloodRageInduction | 600 | 1,200 | **400** | **900** | Moderate reduction; temporary rage |

### 4.4 Biotech Crimes (Category: Biotech) - DLC

#### Current vs. Recommended

| Crime DefName | Current Min | Current Max | **Recommended Min** | **Recommended Max** | Change Rationale |
|---------------|-------------|-------------|---------------------|---------------------|------------------|
| MechSwarmAttack | 1,000 | 2,000 | **700** | **1,500** | Moderate reduction; mechanoid assault |
| WastpackMortar | 800 | 1,600 | **550** | **1,200** | Moderate reduction; toxic warfare |
| BloodfeederAttack | 400 | 800 | **300** | **600** | Moderate reduction; vampiric attack |

---

## 5. Recommended Multiplier Adjustments

### 5.1 Current Multiplier Problems

The multiplier system is well-designed but some default values are too high, leading to extreme penalties in edge cases.

#### Current Multipliers

| Multiplier Type | Current Value | Current Enabled | Issues |
|----------------|---------------|-----------------|--------|
| RepeatOffender | 1.5x | Yes | Reasonable |
| **VictimNobility** | **3.0x** | Yes | **Too high - creates 10,000+ penalties** |
| VictimAge | 1.5x | Yes | Reasonable |
| Wartime | 0.75x | Yes | Good (reduces penalties) |
| Premeditated | 1.5x | Yes | Reasonable |
| VictimRelationship | 1.25x | Yes | Reasonable |
| RaiderWealth | 1.0x | No | Not implemented |
| ColonyWealth | 1.0x | No | Not implemented |
| DifficultySetting | 1.0x | No | Not implemented |
| FactionRelations | 1.0x | Yes | Needs implementation |

### 5.2 Recommended Multiplier Adjustments

| Multiplier Type | **Recommended Value** | **Recommended Enabled** | Change Rationale |
|----------------|----------------------|------------------------|------------------|
| RepeatOffender | **1.3x** | Yes | Reduced from 1.5x; still significant (30% increase) |
| **VictimNobility** | **2.0x** | Yes | **Major reduction from 3.0x; prevents extreme penalties** |
| VictimAge | **1.3x** | Yes | Reduced from 1.5x; children still protected but not extreme |
| Wartime | 0.75x | Yes | Keep current (reduces penalties appropriately) |
| Premeditated | **1.4x** | Yes | Slight reduction from 1.5x |
| VictimRelationship | 1.25x | Yes | Keep current (moderate increase) |
| RaiderWealth | 1.0x | No | Keep disabled (not implemented) |
| **ColonyWealth** | **Dynamic** | **Yes (Optional)** | Implement if desired (see below) |
| DifficultySetting | 1.0x | No | Keep disabled (let players control difficulty) |
| **FactionRelations** | **Dynamic** | Yes | Properly implement (see below) |

### 5.3 Multiplier Stacking Prevention

**Problem:** Multiple multipliers compound multiplicatively:
- Murder (3,750) × RepeatOffender (1.5) × VictimNobility (3.0) × VictimAge (1.5) = **25,312 silver**

**Recommended Solution:** Implement diminishing returns on multiplier stacking.

**Option A: Cap Total Multiplier**
```csharp
// After applying all multipliers
float totalMultiplier = penalty / basePenalty;
if (totalMultiplier > 4.0f)
{
    penalty = basePenalty * 4.0f; // Cap at 4x base penalty
}
```

**Option B: Additive Multipliers (Alternative)**
```csharp
// Instead of: penalty *= multiplier1 * multiplier2 * multiplier3
// Use: penalty *= (1 + (multiplier1 - 1) + (multiplier2 - 1) + (multiplier3 - 1))
// Example: 1.5x, 2.0x, 1.3x
// Current: 1.5 × 2.0 × 1.3 = 3.9x
// Proposed: 1 + 0.5 + 1.0 + 0.3 = 2.8x
```

**Recommended Approach:** **Option A (cap at 4x)** - simpler and preserves multiplicative system while preventing extremes.

**Impact Example (Murder of Noble Child, Repeat Offender):**
- Base penalty: 3,750 silver (mid-range murder)
- Multipliers: 1.3 (repeat) × 2.0 (nobility) × 1.3 (age) = 3.38x
- Without cap: 3,750 × 3.38 = **12,675 silver**
- With 4x cap: 3,750 × 4.0 = **15,000 silver** (still extreme but capped)
- **Alternative with new multipliers:** 3,750 × 3.38 = 12,675 (no cap needed if multipliers are lowered)

### 5.4 Colony Wealth Multiplier Implementation

If enabling the Colony Wealth multiplier, adjust brackets:

| Colony Wealth | **Recommended Multiplier** | Current | Change |
|---------------|---------------------------|---------|--------|
| < 20,000 | **0.6x** | 0.5x | Slight increase (very poor colonies) |
| 20,000-75,000 | **1.0x** | 1.0x | No change (typical mid-game) |
| 75,000-200,000 | **1.3x** | 1.5x | Reduced (wealthy colonies) |
| > 200,000 | **1.5x** | 2.0x | Reduced (very wealthy colonies) |

**Rationale:**
- Wealth multiplier should be subtle (10-50% adjustment)
- Very poor colonies shouldn't be crippled by debt
- Very wealthy colonies can afford higher penalties but 2.0x is excessive

### 5.5 Faction Relations Multiplier Implementation

Implement as specified in original code, but with adjusted values:

| Faction Goodwill | **Recommended Multiplier** | Current | Change |
|------------------|---------------------------|---------|--------|
| < -75 (Hostile) | **1.5x** | 2.0x | Reduced (hostile should pay more, but not double) |
| -75 to -25 (Unfriendly) | **1.2x** | 2.0x | New tier (slightly elevated) |
| -25 to 25 (Neutral) | **1.0x** | 1.0x | No change |
| 25 to 75 (Friendly) | **0.8x** | 0.5x | Increased (friends pay closer to normal) |
| > 75 (Allied) | **0.7x** | 0.5x | Increased (allies get discount but not half) |

---

## 6. Implementation Summary

### 6.1 Changes at a Glance

**Overall Penalty Reductions:**

| Crime Category | Average Current Penalty | Average Recommended Penalty | Reduction % |
|----------------|------------------------|----------------------------|-------------|
| Minor Injury | 125 silver | **45 silver** | **64% reduction** |
| Moderate Injury | 500 silver | **225 silver** | **55% reduction** |
| Severe Injury | 1,600 silver | **950 silver** | **41% reduction** |
| Lethal | 4,000 silver | **3,400 silver** | **15% reduction** |
| Property | 650 silver | **425 silver** | **35% reduction** |
| Environmental | 800 silver | **500 silver** | **38% reduction** |

**Overall Impact:**
- Average penalty per crime: **Reduced by ~45%**
- Typical raider debt (small raid): **Reduced from 6,000 to 2,500 silver** (58% reduction)
- Typical raider debt (medium raid): **Reduced from 12,000 to 5,000 silver** (58% reduction)

### 6.2 Property Damage Multiplier Changes

| Crime Type | Current Multiplier | Recommended Multiplier | Impact |
|-----------|-------------------|----------------------|--------|
| PropertyDestruction | 2.0x | **1.5x** | 25% reduction |
| Vandalism | 1.0x | **0.75x** | 25% reduction |
| Arson | 3.0x | **2.5x** | 17% reduction |
| Theft | 1.5x | **1.25x** | 17% reduction |

### 6.3 Multiplier System Changes

| Multiplier | Current Value | Recommended Value | Impact |
|-----------|---------------|------------------|--------|
| VictimNobility | 3.0x | **2.0x** | 33% reduction |
| RepeatOffender | 1.5x | **1.3x** | 13% reduction |
| VictimAge | 1.5x | **1.3x** | 13% reduction |
| Premeditated | 1.5x | **1.4x** | 7% reduction |
| **Max Stacking** | Unlimited | **4.0x cap** | Prevents extremes |

---

## 7. Scenario Analysis - Before & After

### 7.1 Scenario 1: Small Raid (5 Raiders, Moderate Combat)

**Situation:** 5 raiders attack, each shoots colonists 2-3 times, destroy 2 doors.

#### Raider 1: Shoots colonist twice
- **Current:** 2 × GunshotWound (600) = **1,200 silver**
- **Recommended:** 2 × GunshotWound (250) = **500 silver**

#### Raider 2: Shoots colonist three times
- **Current:** 3 × GunshotWound (600) = **1,800 silver**
- **Recommended:** 3 × GunshotWound (250) = **750 silver**

#### Raider 3: Destroys door, shoots colonist once
- **Current:** DoorDestruction (350 + 50×2) + GunshotWound (600) = **1,050 silver**
- **Recommended:** DoorDestruction (200 + 50×1.5) + GunshotWound (250) = **525 silver**

#### Raider 4: Shoots colonist twice, destroys door
- **Current:** 2 × GunshotWound (600) + DoorDestruction (450) = **1,650 silver**
- **Recommended:** 2 × GunshotWound (250) + DoorDestruction (275) = **775 silver**

#### Raider 5: Shoots colonist four times
- **Current:** 4 × GunshotWound (600) = **2,400 silver**
- **Recommended:** 4 × GunshotWound (250) = **1,000 silver**

**Total Raid Debt:**
- **Current:** 8,100 silver
- **Recommended:** 3,550 silver
- **Reduction:** 56%

**Comparison to Raid Loot:** ~2,500 silver loot value
- **Current debt-to-loot ratio:** 3.2:1 (excessive)
- **Recommended debt-to-loot ratio:** 1.4:1 (reasonable)

### 7.2 Scenario 2: Serious Raid (Colonist Loses Arm)

**Situation:** Raider shoots colonist multiple times, one shot destroys arm.

#### Crimes:
- 3 × GunshotWound (non-limb hits)
- 1 × LimbDestruction

**Current Penalties:**
- 3 × GunshotWound (600) = 1,800
- 1 × LimbDestruction (2,000) = 2,000
- **Total: 3,800 silver**

**Recommended Penalties:**
- 3 × GunshotWound (250) = 750
- 1 × LimbDestruction (1,400) = 1,400
- **Total: 2,150 silver**

**Medical/Replacement Costs:**
- Treatment: ~200 silver medicine
- Bionic arm: ~1,000 silver
- Installation: ~200 silver
- **Total cost: ~1,400 silver**

**Penalty-to-Cost Ratio:**
- **Current:** 3,800 / 1,400 = **2.7:1** (reasonable)
- **Recommended:** 2,150 / 1,400 = **1.5:1** (appropriate)

**Analysis:** Both systems appropriately penalize permanent injuries. Recommended system is slightly lower but still significant.

### 7.3 Scenario 3: Murder of Colonist

**Situation:** Raider kills colonist after several gunshots.

#### Crimes:
- 4 × GunshotWound (leading to death)
- 1 × Murder

**Current Penalties:**
- 4 × GunshotWound (600) = 2,400
- 1 × Murder (4,500) = 4,500
- **Total: 6,900 silver**

**Recommended Penalties:**
- 4 × GunshotWound (250) = 1,000
- 1 × Murder (3,750) = 3,750
- **Total: 4,750 silver**

**Colonist Value:**
- Average skilled colonist: ~3,000-5,000 silver equivalent
- **Penalty-to-value ratio:**
  - Current: 6,900 / 4,000 = 1.7:1
  - Recommended: 4,750 / 4,000 = 1.2:1

**Analysis:** Murder should be heavily penalized. Recommended penalty (~5,000 total) is appropriate for this most serious crime. Reduction from 7,000 prevents excessive accumulation when multiple colonists are killed.

### 7.4 Scenario 4: Noble Child Attacked (Multipliers Active)

**Situation:** Repeat offender raider shoots noble child colonist twice.

#### Crimes:
- 2 × GunshotWound

**Multipliers:**
- RepeatOffender (1.3x recommended, 1.5x current)
- VictimNobility (2.0x recommended, 3.0x current)
- VictimAge (1.3x recommended, 1.5x current)

**Current Penalties:**
- Base: 2 × 600 = 1,200
- Multipliers: 1.5 × 3.0 × 1.5 = 6.75x
- **Total: 8,100 silver** (for two gunshots!)

**Recommended Penalties:**
- Base: 2 × 250 = 500
- Multipliers: 1.3 × 2.0 × 1.3 = 3.38x
- Uncapped: 500 × 3.38 = 1,690 silver
- Capped (4x): 500 × 4.0 = **2,000 silver**

**Analysis:**
- Current system creates absurd penalty (8,100) for two gunshots
- Recommended system (2,000) is significant but reasonable
- Multipliers still provide appropriate escalation for aggravating circumstances
- 4x cap prevents extreme outliers

### 7.5 Scenario 5: Arson Attack

**Situation:** Raider sets fire to wooden structure worth 600 silver.

#### Crimes:
- 1 × Arson

**Current Penalty:**
- Base: 1,500 (mid-range)
- Property: 600 × 3.0 = 1,800
- **Total: 3,300 silver**

**Recommended Penalty:**
- Base: 850 (mid-range)
- Property: 600 × 2.5 = 1,500
- **Total: 2,350 silver**

**Property Replacement Cost:** 600 silver

**Penalty-to-Cost Ratio:**
- Current: 3,300 / 600 = 5.5:1
- Recommended: 2,350 / 600 = 3.9:1

**Analysis:** Arson should be punished severely (it's intentional destruction), but current system is excessive. Recommended ratio of ~4:1 is appropriate for this serious crime.

---

## 8. Implementation Guide

### 8.1 Code Changes Required

#### A. Update Crime Definitions in WorldComponent_CrimePenaltyManager.cs

Locate `InitializeDefaultCrimes()` method and update min/max penalty values for each crime definition:

```csharp
// Example changes (see full table above for all values)
crimeDefinitions.Add(new CrimeDefinition(
    "GunshotWound",
    "...",
    "...",
    CrimeSeverity.Moderate,
    150,  // Changed from 400
    350,  // Changed from 800
    CrimeCategory.ModerateInjury
));

crimeDefinitions.Add(new CrimeDefinition(
    "Murder",
    "...",
    "...",
    CrimeSeverity.Critical,
    2500,  // Changed from 3000
    5000,  // Changed from 6000
    CrimeCategory.Lethal
));
```

#### B. Update Multipliers in WorldComponent_CrimePenaltyManager.cs

Locate `InitializeDefaultMultipliers()` method:

```csharp
// Update these multiplier values
penaltyMultipliers.Add(new CrimePenaltyMultiplier(
    MultiplierType.RepeatOffender,
    1.3f,  // Changed from 1.5f
    "...",
    true
));

penaltyMultipliers.Add(new CrimePenaltyMultiplier(
    MultiplierType.VictimNobility,
    2.0f,  // Changed from 3.0f
    "...",
    true
));

penaltyMultipliers.Add(new CrimePenaltyMultiplier(
    MultiplierType.VictimAge,
    1.3f,  // Changed from 1.5f
    "...",
    true
));
```

#### C. Update Property Crime Multipliers in DebtUtils.cs

Locate `CalculateDebtForNonVictimCrime()` method:

```csharp
case CrimeType.Theft:
    if (targetThing != null)
    {
        float itemValue = targetThing.MarketValue * targetThing.stackCount;
        return itemValue * 1.25f; // Changed from 1.5f
    }
    return 100f;

case CrimeType.PropertyDestruction:
    if (targetThing != null)
    {
        return targetThing.MarketValue * 1.5f; // Changed from 2.0f
    }
    return 500f;

case CrimeType.Vandalism:
    if (targetThing != null)
    {
        return targetThing.MarketValue * 0.75f; // Changed from 1.0f
    }
    return 200f;

case CrimeType.Arson:
    if (targetThing != null)
    {
        return targetThing.MarketValue * 2.5f; // Changed from 3.0f
    }
    // ... rest of arson logic
```

#### D. Add Multiplier Cap in DebtUtils.cs

Locate `ApplyMultipliers()` method, add cap at the end:

```csharp
private static float ApplyMultipliers(
    float basePenalty,
    WorldComponent_CrimePenaltyManager manager,
    Pawn criminal,
    Pawn victim,
    DamageInfo? damageInfo)
{
    // ... existing multiplier application logic ...

    // Add this at the end, before returning penalty:

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

    return penalty;
}
```

#### E. Optional: Colony Wealth Multiplier Refinement

If enabling colony wealth multiplier, update `GetColonyWealthFactor()` in DebtUtils.cs:

```csharp
private static float GetColonyWealthFactor()
{
    Map map = Find.Maps?.FirstOrDefault(m => m.IsPlayerHome);
    if (map?.wealthWatcher == null)
    {
        return 1.0f;
    }

    float wealth = map.wealthWatcher.WealthTotal;

    // Revised wealth brackets (more granular)
    if (wealth < 20000f)
    {
        return 0.6f;  // Changed from 0.5f
    }
    else if (wealth < 75000f)  // Changed from 50000f
    {
        return 1.0f;
    }
    else if (wealth < 200000f)  // Changed from 100000f
    {
        return 1.3f;  // Changed from 1.5f
    }
    else
    {
        return 1.5f;  // Changed from 2.0f
    }
}
```

### 8.2 Testing Checklist

After implementing changes, test these scenarios:

- [ ] **Minor combat:** Raider shoots colonist 2-3 times, verify penalty ~500-750 silver
- [ ] **Serious combat:** Raider destroys colonist limb, verify penalty ~1,500-2,000 silver
- [ ] **Murder:** Raider kills colonist, verify penalty ~4,000-5,000 silver
- [ ] **Property damage:** Raider destroys door, verify penalty ~200-300 silver
- [ ] **Arson:** Raider burns building, verify penalty scales with property value (2.5x)
- [ ] **Theft:** Raider steals item, verify penalty is 1.25x item value
- [ ] **Multipliers:** Noble victim, verify 2.0x multiplier applies (not 3.0x)
- [ ] **Multiplier stacking:** Multiple multipliers, verify cap at 4x base penalty
- [ ] **Small raid total:** 5 raiders, verify total debt ~2,500-4,000 silver
- [ ] **Large raid total:** 15 raiders, verify total debt ~8,000-15,000 silver

### 8.3 Migration Strategy for Existing Saves

For players with existing saves that have high penalties:

**Option A: Automatic Recalculation (Recommended)**

Add migration code to `WorldComponent_CrimePenaltyManager.ExposeData()`:

```csharp
public override void ExposeData()
{
    base.ExposeData();
    // ... existing code ...

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
        InitializeDefaults();

        // Check if this is an old save with old penalty values
        // (detect by checking if any crime has penalties in old ranges)
        bool needsMigration = crimeDefinitions.Any(cd =>
            cd.defName == "GunshotWound" && cd.minPenalty > 200);

        if (needsMigration)
        {
            // Force-update to new values
            InitializeDefaultCrimes();

            if (Prefs.DevMode)
            {
                Mod.Log?.Message("[Law & Order] Migrated crime penalties to new balanced values");
            }
        }
    }
}
```

**Option B: Manual Reset via Settings**

Add "Reset Penalties to Defaults" button in mod settings that calls `InitializeDefaultCrimes()`.

**Option C: Debt Reduction (Player-Friendly)**

Add one-time debt reduction for all prisoners:

```csharp
// Run once on load
foreach (Pawn pawn in Find.World.worldPawns.AllPawns)
{
    var debtHediff = DebtUtils.TryGetDebtRecord(pawn);
    if (debtHediff != null && debtHediff.CurrentDebt > 0)
    {
        // Reduce debt by 40% (average penalty reduction)
        float reduction = debtHediff.CurrentDebt * 0.4f;
        debtHediff.PayDebt(reduction, "Penalty rebalancing");
    }
}
```

---

## 9. Additional Recommendations

### 9.1 Add Mod Settings for Penalty Scaling

Allow players to adjust overall penalty scale via mod settings:

```csharp
public class LawAndOrderSettings : ModSettings
{
    // ... existing settings ...

    public float penaltyScaleFactor = 1.0f; // 0.5 - 2.0 range

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        // ... existing settings ...

        listingStandard.Label($"Global Penalty Scale: {penaltyScaleFactor:P0}");
        penaltyScaleFactor = listingStandard.Slider(penaltyScaleFactor, 0.5f, 2.0f);
        listingStandard.Gap();

        if (penaltyScaleFactor < 0.9f || penaltyScaleFactor > 1.1f)
        {
            listingStandard.Label("Non-default scaling active", -1f,
                "All crime penalties multiplied by this factor");
        }

        listingStandard.End();
    }
}
```

Then apply in `CalculateDebtForCrimeNew()`:

```csharp
// At the end of the calculation
float finalPenalty = ApplyMultipliers(basePenalty, manager, criminal, victim, damageInfo);

// Apply global penalty scale from settings
var settings = LoadedModManager.GetMod<Mod>().GetSettings<LawAndOrderSettings>();
finalPenalty *= settings.penaltyScaleFactor;

return Mathf.Clamp(Mathf.RoundToInt(finalPenalty), 1, 100000);
```

**Benefits:**
- Players can fine-tune penalties to their preference
- Storyteller compatibility (players using Randy/Casandra can adjust)
- Colony wealth scaling (poor colonies can use 0.5x, rich colonies 1.5x)

### 9.2 Add "Penalty Preview" in Judiciary Tab

Add UI element showing expected penalties for common crimes:

```csharp
// In ITab_Pawn_Judiciary or similar
private void DrawPenaltyPreview()
{
    Rect previewRect = new Rect(/* position */);

    Text.Font = GameFont.Tiny;
    Listing_Standard listing = new Listing_Standard();
    listing.Begin(previewRect);

    listing.Label("Expected Penalties (at current settings):");
    listing.Gap(4f);

    // Show common crimes and their penalty ranges
    var commonCrimes = new[]
    {
        "GunshotWound",
        "LimbDestruction",
        "Murder",
        "PropertyDestruction"
    };

    foreach (var crimeName in commonCrimes)
    {
        var crimeDef = manager.GetCrimeDefinition(crimeName);
        if (crimeDef != null)
        {
            listing.Label($"  {crimeDef.label}: {crimeDef.minPenalty}-{crimeDef.maxPenalty} silver");
        }
    }

    listing.End();
}
```

### 9.3 Add Warning for High-Debt Prisoners

Add UI warning when prisoner debt exceeds reasonable amount:

```csharp
// In debt display
if (debtAmount > 5000f)
{
    GUI.color = Color.yellow;
    Widgets.Label(rect, "⚠ High debt - consider reduction");
    GUI.color = Color.white;
}
```

### 9.4 Add "Debt Forgiveness" Mechanic

Allow players to manually reduce debt (for balance or storytelling):

```csharp
// Add button in prisoner tab
if (Widgets.ButtonText(buttonRect, "Forgive 50% Debt"))
{
    float reduction = debtHediff.CurrentDebt * 0.5f;
    debtHediff.PayDebt(reduction, "Debt forgiveness");

    // Mood bonus for prisoner
    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
        LawAndOrder_ThoughtDefOf.DebtForgiveness);
}
```

---

## 10. Final Recommendations Summary

### 10.1 Priority Changes (Must Implement)

1. **Reduce moderate injury penalties by 55%** (GunshotWound: 400-800 → 150-350)
2. **Reduce severe injury penalties by 40%** (LimbDestruction: 1,500-3,000 → 800-1,800)
3. **Reduce minor injury penalties by 60%** (Scratch: 100-200 → 30-80)
4. **Reduce VictimNobility multiplier** (3.0x → 2.0x)
5. **Add 4x multiplier stacking cap** to prevent extreme outliers

**Impact:** Reduces typical raider debt from 6,000-12,000 to 2,500-5,000 silver (58% reduction)

### 10.2 Secondary Changes (Recommended)

6. **Reduce property damage multipliers** (PropertyDestruction: 2.0x → 1.5x, Arson: 3.0x → 2.5x)
7. **Reduce theft multiplier** (1.5x → 1.25x)
8. **Reduce other multipliers slightly** (RepeatOffender: 1.5x → 1.3x, VictimAge: 1.5x → 1.3x)
9. **Reduce lethal crime penalties by 15%** (Murder: 3,000-6,000 → 2,500-5,000)

**Impact:** Further refinement for proportionality and balance

### 10.3 Optional Enhancements

10. **Add global penalty scale setting** (let players adjust 0.5x-2.0x)
11. **Add penalty preview in UI** (show expected penalties)
12. **Add debt forgiveness mechanic** (player choice for storytelling)
13. **Refine colony wealth multiplier** (if enabled)

**Impact:** Player customization and quality-of-life improvements

---

## 11. Conclusion

The Law and Order mod's crime penalty system is architecturally sound but numerically imbalanced. The recommended changes reduce penalties by an average of 45%, bringing them in line with RimWorld's economy and actual costs to treat injuries.

**Key Achievements of Recommended System:**

1. **Typical raider debt: 500-2,000 silver** (down from 2,000-10,000)
2. **Penalties proportionate to harm:** 2-3x medical costs for moderate injuries, 4-6x for severe
3. **Death appropriately severe:** 2,500-5,000 silver (matches colonist value)
4. **Multiple-hit scenarios balanced:** 3-5 hits = 750-1,750 silver (reasonable)
5. **Property damage proportionate:** 1.5-2.5x property value (restitution + damages)
6. **Multipliers controlled:** 4x cap prevents extreme outliers
7. **Player-friendly:** Debt amounts prisoners can realistically work off (40-250 days)

**Philosophy Achieved:**
> "Raiders shouldn't rack up 10,000+ silver unless they've caused considerable damages. Since pawns with intact body parts mostly just need time and medicine to heal, penalties should be proportionate to actual harm."

The recommended changes achieve this goal while maintaining appropriate punishment for serious crimes like murder, permanent injuries, and property destruction.

---

**Document Status:** COMPLETE - READY FOR IMPLEMENTATION
**Next Steps:**
1. Review recommendations with mod team
2. Implement priority changes (items 1-5)
3. Test thoroughly with various raid scenarios
4. Consider optional enhancements based on player feedback
5. Document changes in mod changelog

---

## Appendix A: Quick Reference Table

### Complete Recommended Penalty List (Top 30 Most Common Crimes)

| Rank | Crime | Current Min-Max | **Recommended Min-Max** | Reduction % |
|------|-------|-----------------|------------------------|-------------|
| 1 | GunshotWound | 400-800 | **150-350** | 56% |
| 2 | Murder | 3,000-6,000 | **2,500-5,000** | 17% |
| 3 | Theft (standard) | 320-1,000 | **250-700** | 22% |
| 4 | LimbDestruction | 1,500-3,000 | **800-1,800** | 47% |
| 5 | DoorDestruction | 200-500 | **100-300** | 40% |
| 6 | StabWound | 350-700 | **130-300** | 57% |
| 7 | Arson | 1,000-2,000 | **500-1,200** | 40% |
| 8 | PropertyDestruction | 300-800 | **200-500** | 33% |
| 9 | Scratch | 100-200 | **30-80** | 60% |
| 10 | SlashWound | 300-600 | **120-280** | 53% |
| 11 | Bruise | 50-150 | **20-60** | 60% |
| 12 | BluntTrauma | 300-600 | **120-280** | 53% |
| 13 | EyeDestruction | 1,200-2,500 | **700-1,500** | 42% |
| 14 | MajorOrganDamage | 1,000-2,000 | **600-1,200** | 40% |
| 15 | Kidnapping | 2,500-5,000 | **2,000-4,000** | 20% |
| 16 | SevereBurns | 800-1,500 | **400-900** | 50% |
| 17 | BiteWound | 250-500 | **100-220** | 56% |
| 18 | ExplosiveInjury | 500-1,000 | **200-450** | 55% |
| 19 | Sapping | 500-1,500 | **300-800** | 40% |
| 20 | ToxicGasDeployment | 800-1,500 | **400-900** | 50% |
| 21 | ModerateBurns | 400-800 | **150-350** | 56% |
| 22 | PsychicAttack | 600-1,200 | **300-700** | 50% |
| 23 | Trespassing | 50-150 | **30-100** | 33% |
| 24 | Breaching | 600-1,800 | **350-900** | 42% |
| 25 | ArrowWound | 300-600 | **120-280** | 53% |
| 26 | GrandTheft | 1,000-5,000 | **700-3,000** | 30% |
| 27 | WeaponTheft | 500-2,500 | **350-1,500** | 30% |
| 28 | MedicineTheft | 400-2,000 | **300-1,200** | 25% |
| 29 | SpineInjury | 1,500-3,000 | **800-1,800** | 47% |
| 30 | Execution | 5,000-10,000 | **4,000-8,000** | 20% |

---

**END OF DOCUMENT**
