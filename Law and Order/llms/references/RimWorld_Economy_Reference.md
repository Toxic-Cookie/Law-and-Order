# RimWorld Silver Economy Reference

**Document Version:** 1.0
**Date:** 2025-11-10
**Game Version:** RimWorld 1.6
**Purpose:** Comprehensive reference for silver values, costs, and economic benchmarks in RimWorld

---

## Executive Summary

RimWorld's economy is based on silver as the universal currency. Understanding typical costs, colony wealth progression, and item values is essential for balancing the Law and Order mod's crime penalties. This document provides comprehensive economic data gathered from the RimWorld Wiki, community sources, and game files.

### Key Economic Principles

1. **Market Value** is the base value of items/buildings
2. **Trade prices** are modified by negotiation skill and category multipliers
3. **Colony wealth** drives raid difficulty and scales with progression
4. **Medical costs** are relatively low (100-200 silver per injury)
5. **Raid loot** typically yields 500-1,500 silver per raider defeated

---

## 1. Medicine and Medical Treatment

### 1.1 Medicine Types and Costs

| Medicine Type | Market Value | Production Cost | Crafting Requirements | Medical Potency | Availability |
|---------------|--------------|----------------|----------------------|-----------------|--------------|
| Herbal Medicine | ~10 silver | Free (harvest) | 1 Healroot plant | 60% | Easy (early game) |
| Standard Medicine | ~18 silver | ~35 silver materials | 3 Cloth + 1 Herbal + 1 Neutroamine | 100% | Medium (mid game) |
| Glitterworld Medicine | ~55-140 silver | Cannot craft | Trade only | 160% | Hard (late game) |

**Notes:**
- Herbal medicine requires growing healroot (skill 0, 9 days growth)
- Standard medicine requires Drug Lab and Crafting skill 4
- Glitterworld medicine only available from traders, quests, or raids
- Neutroamine is the bottleneck for standard medicine production

### 1.2 Typical Medical Treatment Costs

| Injury Type | Medicine Used | Quantity Needed | Total Cost | Doctor Time | Recovery Time |
|-------------|---------------|-----------------|------------|-------------|---------------|
| Minor Scratch | Herbal | 1-2 | 10-20 silver | ~5 minutes | Few hours |
| Bruise | Herbal | 1 | 10 silver | ~5 minutes | Few hours |
| Gunshot Wound | Standard | 2-4 | 36-72 silver | ~10 minutes | 1-3 days |
| Stab Wound | Standard | 2-3 | 36-54 silver | ~10 minutes | 1-2 days |
| Severe Burns | Glitterworld | 3-5 | 165-700 silver | ~20 minutes | 3-7 days |
| Major Infection | Glitterworld | 4-6 | 220-840 silver | ~15 minutes | 5-10 days |
| Surgery (Install Bionic) | Glitterworld | 2-3 | 110-420 silver | ~30 minutes | 5 days recovery |

**Factors Affecting Cost:**
- **Doctor skill:** Higher skill reduces medicine waste
- **Hospital bed bonus:** +10% tend quality (costs 40 steel, 80 wood)
- **Vitals monitor:** Additional +7% tend quality (costs 50 steel, 3 components)
- **Medicine quality:** Herbal 60%, Standard 100%, Glitterworld 160% potency

### 1.3 Healing Mechanics

**Natural Healing Rates:**
- **In bed with treatment:** 4-8 HP per day (depending on tend quality)
- **Hospital bed:** +14 HP per day bonus
- **Walking around:** Much slower, increased scar chance
- **No treatment:** Very slow, high infection risk

**Tend Quality Impact:**
- **Poor tend (<50%):** Slow healing, infection risk, high scar chance
- **Good tend (70%):** Standard healing, low infection risk
- **Excellent tend (90%+):** Fast healing, minimal scarring

**Treatment Frequency:**
- Minor injuries: Tend every 1-2 days until healed
- Moderate injuries: Tend 1-2 times per day for 3-5 days
- Severe injuries: Tend 2-3 times per day for 7-15 days

### 1.4 Medical Costs Summary

**Estimated Total Medical Costs (medicine + doctor time value):**

| Injury Severity | Medicine Cost | Doctor Time Value | Total Economic Cost | Notes |
|----------------|---------------|-------------------|---------------------|-------|
| Superficial | 10-30 silver | 10 silver | 20-40 silver | Heals in hours |
| Minor | 30-70 silver | 20 silver | 50-90 silver | Heals in 1-2 days |
| Moderate | 70-150 silver | 30 silver | 100-180 silver | Heals in 3-5 days |
| Severe | 150-400 silver | 50 silver | 200-450 silver | Heals in 7-14 days |
| Critical/Permanent | 500-2,000+ silver | 100 silver | 600-2,100+ silver | May never fully heal |

**Doctor Time Valuation:**
- Assumes doctor skill 8-10
- Based on ~10 silver/hour opportunity cost
- Does not include patient downtime (which is significant)

---

## 2. Weapons and Combat Equipment

### 2.1 Common Weapons (Raider Equipment)

| Weapon Type | Market Value | Damage Type | Typical Raider Weapon | Effective Range |
|-------------|--------------|-------------|----------------------|-----------------|
| **Melee Weapons** | | | | |
| Club (wood) | ~22 silver | Blunt | Tribal | Melee only |
| Knife | ~35 silver | Stab/Cut | Early raider | Melee only |
| Spear | ~52 silver | Stab | Tribal | Melee only |
| Longsword | ~190 silver | Cut | Pirate | Melee only |
| Plasteel Longsword | ~630 silver | Cut | Advanced | Melee only |
| **Ranged Weapons** | | | | |
| Short Bow | ~45 silver | Arrow | Tribal | Short |
| Pila | ~90 silver | Thrown | Tribal | Short |
| Revolver | ~140 silver | Bullet | Pirate | Medium |
| Autopistol | ~170 silver | Bullet | Pirate | Short |
| Bolt-action Rifle | ~250 silver | Bullet | Outlander | Long |
| Assault Rifle | ~420 silver | Bullet | Outlander | Medium |
| Charge Rifle | ~1,100 silver | Charge | Advanced | Long |
| **Heavy Weapons** | | | | |
| Pump Shotgun | ~280 silver | Bullet | Pirate | Short |
| LMG | ~550 silver | Bullet | Outlander | Medium |
| Sniper Rifle | ~580 silver | Bullet | Outlander | Very Long |
| Minigun | ~800 silver | Bullet | Advanced | Short |

**Weapon Trade Penalty:** Weapons sell for only 20% of market value (0.6 sell price × 0.2 category multiplier = 12% effective), making weapon farming unprofitable.

### 2.2 Armor and Apparel

| Armor Type | Market Value | Protection | Typical Raider Gear | Notes |
|-----------|--------------|------------|-------------------|-------|
| **Tribal Gear** | | | | |
| Tribal Wear | ~85 silver | Minimal | Tribal | Combines shirt + pants |
| Tribal Headdress | ~45 silver | Minimal | Tribal | Head protection |
| War Mask | ~100 silver | Minimal | Tribal | Intimidation + minimal protection |
| **Pirate/Outlander Gear** | | | | |
| Duster | ~95 silver | Minimal | Common | Temperature protection |
| Jacket | ~70 silver | Minimal | Common | Temperature protection |
| Pants | ~55 silver | None | Common | No armor value |
| Flak Vest | ~220 silver | Medium | Outlander | Torso protection |
| Flak Jacket | ~315 silver | Medium | Outlander | Better torso protection |
| Flak Pants | ~195 silver | Medium | Outlander | Leg protection |
| **Advanced Gear** | | | | |
| Plate Armor | ~600 silver | High | Advanced | Heavy protection, slow |
| Recon Armor | ~730 silver | High | Advanced | Good protection, faster |
| Marine Armor | ~1,400 silver | Very High | Advanced | Best protection |
| Power Armor | ~2,600 silver | Extreme | Late game | Requires power |

**Armor Condition Penalty:** Tainted (worn by dead raider) apparel sells for 50% value. Damaged armor (below 50% HP) has significantly reduced value.

### 2.3 Typical Raider Loadout Values

| Raider Type | Weapon | Armor | Total Value | Loot Value (After Trade Penalties) |
|------------|--------|-------|-------------|-----------------------------------|
| Tribal | Short Bow (45) + Tribal Wear (85) | Minimal | 130 silver | ~40 silver |
| Pirate Scavenger | Revolver (140) + Duster (95) | Minimal | 235 silver | ~65 silver |
| Outlander Villager | Bolt Rifle (250) + Flak Vest (220) | Medium | 470 silver | ~140 silver |
| Outlander Town Guard | Assault Rifle (420) + Flak Jacket (315) | Medium | 735 silver | ~220 silver |
| Advanced Pirate | Charge Rifle (1,100) + Plate Armor (600) | High | 1,700 silver | ~510 silver |

**Note:** Actual loot value is much lower due to:
- Weapon category penalty (0.2x multiplier)
- Tainted apparel (50% value if dead)
- Damaged condition (combat damage)
- Typical realistic loot per raider: 50-200 silver

---

## 3. Buildings and Construction

### 3.1 Common Buildings (Raider Targets)

| Building Type | Market Value | Materials Cost | HP | Typical Raid Damage |
|---------------|--------------|----------------|----|--------------------|
| **Doors** | | | | |
| Wooden Door | ~26 silver | 25 Wood | 100 HP | Often destroyed |
| Stone Door | ~50 silver | 25 Stone | 250 HP | Commonly damaged |
| Steel Door | ~55 silver | 25 Steel | 250 HP | Commonly damaged |
| Plasteel Door | ~300 silver | 25 Plasteel | 500 HP | Rarely damaged |
| Autodoor | ~130 silver | 50 Steel + 2 Comp | 150 HP | Sometimes damaged |
| **Walls** | | | | |
| Wooden Wall | ~8 silver | 5 Wood | 75 HP | Sometimes destroyed (sapping) |
| Stone Wall | ~12 silver | 5 Stone | 225 HP | Rarely destroyed (breaching) |
| Steel Wall | ~13 silver | 5 Steel | 225 HP | Sometimes damaged |
| Plasteel Wall | ~110 silver | 5 Plasteel | 450 HP | Very rarely damaged |
| **Power** | | | | |
| Wood-Fired Generator | ~70 silver | 100 Wood | 200 HP | Sometimes targeted |
| Chemfuel Generator | ~200 silver | 100 Steel + 2 Comp | 200 HP | Sometimes targeted |
| Solar Generator | ~370 silver | 100 Steel + 3 Comp | 200 HP | Sometimes targeted |
| Geothermal Generator | ~1,100 silver | 340 Steel + 8 Comp | 500 HP | High-value target |
| Battery | ~280 silver | 75 Steel + 2 Comp | 200 HP | High-value target |
| **Defenses** | | | | |
| Sandbags | ~7 silver | 5 Stuff | 100 HP | Often damaged |
| Barricade | ~25 silver | 30 Stuff | 400 HP | Sometimes damaged |
| Mini-Turret | ~440 silver | 70 Steel + 3 Comp | 100 HP | High-priority target |
| Autocannon Turret | ~710 silver | 200 Steel + 7 Comp | 150 HP | High-priority target |
| **Furniture** | | | | |
| Bed (poor) | ~100 silver | 40 Stuff | 100 HP | Rarely targeted |
| Hospital Bed | ~680 silver | 40 Steel + 3 Comp | 100 HP | Sometimes targeted |
| Research Bench | ~220 silver | 150 Stuff + 6 Comp | 200 HP | Rarely targeted |
| Machining Table | ~370 silver | 150 Stuff + 5 Comp | 200 HP | Rarely targeted |

### 3.2 Property Destruction Scenarios

| Scenario | Buildings Destroyed | Total Property Value | Realistic Penalty (2x multiplier) |
|----------|---------------------|---------------------|----------------------------------|
| Raider breaks down door | 1 Stone Door | 50 silver | 100 silver |
| Sapper tunnels through 3 walls | 3 Stone Walls | 36 silver | 72 silver |
| Raider destroys mini-turret | 1 Mini-Turret | 440 silver | 880 silver |
| Arson destroys wooden building | 10 walls + 2 doors + furniture | ~400 silver | 1,200 silver (3x for arson) |
| Breacher destroys defensive line | 5 sandbags + 2 walls | ~75 silver | 150 silver |

**Important:** Property damage in raids is typically LOW value (50-500 silver per raider) because raiders target entry points (doors, walls) which are cheap.

---

## 4. Colony Wealth Progression

### 4.1 Wealth Brackets and Progression

| Game Stage | Days Played | Colony Wealth | Colonist Count | Silver on Hand | Typical Events |
|-----------|-------------|---------------|----------------|----------------|----------------|
| **Crash Landing** | 0 | 5,000 | 3 | 0 | Survival mode |
| **Early Game** | 0-30 | 5,000-20,000 | 3-5 | 500-2,000 | First raids |
| **Establishing** | 30-60 | 20,000-50,000 | 5-8 | 2,000-5,000 | Regular raids |
| **Mid Game** | 60-180 | 50,000-150,000 | 8-12 | 5,000-15,000 | Sieges, infestations |
| **Advanced** | 180-360 | 150,000-350,000 | 12-15 | 15,000-50,000 | Major threats |
| **Late Game** | 360+ | 350,000+ | 15-20 | 50,000-200,000 | End-game content |

**Wealth Composition:**
- **Buildings:** 40-60% of total wealth
- **Items:** 20-30% of total wealth
- **Pawns:** 20-30% of total wealth (skills, implants, gear)
- **Silver on hand:** Usually <10% of total wealth

### 4.2 Wealth-to-Raid Scaling

| Total Wealth | Raid Points Generated | Typical Raid Size | Approximate Threat |
|--------------|----------------------|-------------------|-------------------|
| 10,000 | ~50 points | 1-2 raiders | Very small |
| 30,000 | ~150 points | 3-5 raiders | Small |
| 50,000 | ~250 points | 5-8 raiders | Medium |
| 100,000 | ~500 points | 10-15 raiders | Large |
| 200,000 | ~1,000 points | 20-30 raiders | Very large |
| 400,000 | ~2,400 points | 40-60 raiders | Massive |

**Note:** Raid points also scale with colonist count, storyteller difficulty, and time played.

### 4.3 Liquid Silver Availability

How much silver do players typically have on hand?

| Wealth Bracket | Typical Silver on Hand | % of Total Wealth | Notes |
|----------------|------------------------|-------------------|-------|
| <10,000 | 0-500 | 0-5% | Spending everything on basics |
| 10,000-30,000 | 500-2,000 | 5-10% | Building buffer |
| 30,000-100,000 | 2,000-10,000 | 5-10% | Comfortable reserves |
| 100,000-300,000 | 10,000-30,000 | 5-10% | Significant reserves |
| 300,000+ | 30,000-100,000+ | 5-10% | Wealthy colony |

**Silver Sources:**
- Selling excess items (art, drugs, excess food)
- Completing quests (100-3,000 silver typical)
- Mining silver ore (rare, limited quantity)
- Selling prisoners to slavers (500-2,000 per prisoner)

**Silver Sinks:**
- Buying medicine, components, advanced weapons
- Purchasing exotic items (thrumbofur, gold, jade)
- Buying prisoners/slaves (500-3,000 per pawn)
- Orbital trade beacon items (various)
- Caravanning costs (pack animals, supplies)

---

## 5. Raid Loot Economics

### 5.1 Typical Raid Loot Values

Based on community data and testing:

| Raid Size | Weapons Dropped | Apparel Dropped | Other Items | Corpse Value | Total Loot Value |
|-----------|----------------|-----------------|-------------|--------------|------------------|
| Small (5 raiders) | 5 × 150 = 750 | 5 × 100 = 500 | 100 | 5 × 60 = 300 | 1,650 silver |
| Medium (10 raiders) | 10 × 200 = 2,000 | 10 × 120 = 1,200 | 300 | 10 × 60 = 600 | 4,100 silver |
| Large (20 raiders) | 20 × 250 = 5,000 | 20 × 140 = 2,800 | 600 | 20 × 60 = 1,200 | 9,600 silver |

**After Trade Penalties:**
- Weapons: × 0.12 (60% sell × 20% category) = ~90 silver/weapon
- Tainted Apparel: × 0.21 (60% sell × 50% tainted × 70% category) = ~25 silver/piece
- Corpses: × 0.6 (60% sell) = ~36 silver/corpse
- **Realistic Loot Value:** 500-1,500 silver per raider defeated

### 5.2 Raid Loot vs. Damage Caused

Comparing what raiders bring vs. what they destroy:

| Raid Size | Loot Brought | Medical Costs | Property Damage | Total Costs | Loot-to-Cost Ratio |
|-----------|-------------|---------------|----------------|-------------|-------------------|
| Small (5 raiders) | 2,500 | 500-1,500 | 200-1,000 | 700-2,500 | 1:1 (balanced) |
| Medium (10 raiders) | 6,000 | 1,500-4,000 | 500-2,000 | 2,000-6,000 | 1:1 (balanced) |
| Large (20 raiders) | 15,000 | 3,000-8,000 | 1,000-4,000 | 4,000-12,000 | 1.25:1 to 3.75:1 |

**Key Insight:** In balanced raids, loot value roughly equals or slightly exceeds costs. This means raider debt should align with actual costs (medical + property), not far exceed them.

### 5.3 Prisoner Sale Value

Alternative to debt system - selling prisoners:

| Prisoner Type | Sale Value (Slaver) | Sale Value (Empire) | Ethical Issues |
|--------------|---------------------|---------------------|----------------|
| Healthy Adult | 500-1,000 | 800-1,500 | Slavery mood debuff |
| Injured Adult | 200-600 | 400-900 | Slavery + guilt |
| Skilled Pawn | 1,000-2,500 | 1,500-3,500 | Higher value |
| Noble/Special | 2,000-5,000 | 3,000-7,000 | Rare, high value |

**Prisoner Labor Alternative:**
- Prison Labor mod allows prisoners to work off debt
- Typical labor value: 10-50 silver/day per prisoner
- Debt of 2,000 silver = 40-200 days of labor
- This provides an alternative to "impossible to pay" debts

---

## 6. Common Item Values

### 6.1 Resources and Materials

| Resource | Market Value | Common Uses | Abundance |
|----------|--------------|-------------|-----------|
| **Raw Materials** | | | |
| Wood | 1.9 silver | Buildings, furniture, fuel | Very common |
| Steel | 2.0 silver | Buildings, weapons, components | Common |
| Stone (blocks) | 1.9 silver | Buildings, walls | Very common |
| Silver | 10 silver | Currency, electronics | Rare (mining) |
| Gold | 10 silver | Electronics, art, trade | Rare |
| Jade | 6 silver | Art, trade | Rare |
| Plasteel | 9 silver | Advanced buildings, weapons | Rare |
| Uranium | 6 silver | Weapons, power | Very rare |
| **Manufactured** | | | |
| Cloth | 1.8 silver | Clothing, beds | Common |
| Component | 32 silver | Buildings, electronics | Medium |
| Advanced Component | 280 silver | Advanced buildings | Rare |
| Chemfuel | 2.3 silver | Power, explosives | Medium (crafted) |
| Neutroamine | 6 silver | Drugs, medicine | Medium (trade) |

### 6.2 Food and Consumables

| Food Type | Market Value | Nutrition | Silver per Meal | Notes |
|-----------|--------------|-----------|-----------------|-------|
| Raw Rice | 1.2 silver | 0.05 | 24 silver/meal | Most efficient crop |
| Corn | 1.4 silver | 0.05 | 28 silver/meal | High yield, slow |
| Simple Meal | 15 silver | 0.9 | 16.7 silver/meal | Basic food |
| Fine Meal | 20 silver | 0.9 | 22.2 silver/meal | Mood bonus |
| Lavish Meal | 40 silver | 1.0 | 40 silver/meal | Large mood bonus |
| Pemmican | 1.4 silver | 0.05 | 28 silver/meal | Travel food |
| Packaged Survival Meal | 24 silver | 0.9 | 26.7 silver/meal | Long-term storage |

### 6.3 Luxury and Trade Goods

| Item | Market Value | Weight | Trade Value | Common Use |
|------|--------------|--------|-------------|------------|
| Beer | 10 silver | 0.8 kg | Medium | Recreation, trade |
| Smokeleaf Joint | 11 silver | 0.05 kg | Medium | Recreation |
| Yayo | 21 silver | 0.05 kg | Medium | Drug (combat) |
| Flake | 14 silver | 0.05 kg | Low | Drug (addictive) |
| Thrumbofur | 21 silver | 0.04 kg | High | Luxury clothing |
| Hyperweave | 15 silver | 0.04 kg | Very High | Luxury clothing |
| Art (Poor) | 100 silver | Varies | Medium | Mood, beauty |
| Art (Normal) | 750 silver | Varies | High | Trade, beauty |
| Art (Good) | 2,700 silver | Varies | Very High | Major trade good |
| Art (Excellent) | 8,700 silver | Varies | Extreme | Wealth builder |

---

## 7. Quest Rewards and Income

### 7.1 Typical Quest Rewards

| Quest Type | Difficulty | Silver Reward | Item Rewards | Notes |
|-----------|-----------|---------------|--------------|-------|
| Small Favor | Easy | 300-800 | Basic items | Common |
| Medium Mission | Medium | 800-2,000 | Moderate items | Uncommon |
| Large Mission | Hard | 2,000-5,000 | Good items | Rare |
| Epic Quest | Very Hard | 5,000-15,000 | Excellent items | Very rare |

### 7.2 Alternative Income Sources

| Income Source | Silver per Event | Frequency | Effort Required | Notes |
|--------------|------------------|-----------|-----------------|-------|
| Selling Art | 100-2,000 | Continuous | Medium | Requires artist |
| Selling Drugs | 200-1,000 | Continuous | Low | Moral concerns |
| Selling Excess Food | 50-300 | Continuous | Low | Limited demand |
| Mining Silver | 500-2,000 | One-time | High | Limited ore |
| Quest Rewards | 300-5,000 | Occasional | High | Risk vs. reward |
| Prisoner Sales | 500-2,000 | After raids | Medium | Ethical concerns |

---

## 8. Economic Context for Penalty Balancing

### 8.1 What Silver Amounts "Feel Like" to Players

| Silver Amount | Player Perception | Examples | Impact on Colony |
|---------------|-------------------|----------|------------------|
| 10-50 | Trivial | Small items, repairs | No impact |
| 50-100 | Minor | Basic medicine, food | Minor inconvenience |
| 100-300 | Small | Herbal medicine stock, basic trade | Noticeable |
| 300-1,000 | Moderate | Standard medicine, components | Significant |
| 1,000-3,000 | Large | Glitterworld medicine, bionic | Major expense |
| 3,000-10,000 | Very Large | Quest item, emergency fund | Very significant |
| 10,000-30,000 | Huge | Major building project | Colony-changing |
| 30,000+ | Enormous | Archonexus quest, army equipment | Requires planning |

### 8.2 Penalty-to-Cost Ratio Analysis

Current penalties vs. actual costs:

| Crime Type | Current Penalty | Actual Cost to Colony | Ratio | Assessment |
|-----------|----------------|----------------------|-------|------------|
| Minor scratch | 150 silver | 20 silver | 7.5:1 | Excessive |
| Gunshot wound | 600 silver | 100 silver | 6:1 | High |
| Limb destruction | 2,000 silver | 1,500 silver (bionic) | 1.3:1 | Reasonable |
| Murder | 4,500 silver | Pawn value (varies) | N/A | Context-dependent |
| Property damage (door) | 100 silver | 50 silver | 2:1 | Reasonable |
| Arson (building) | 1,500 silver | 500 silver | 3:1 | Moderate |

**Recommended Ratio:** 2-3:1 penalty-to-cost for moderate crimes, higher for severe/permanent crimes.

### 8.3 Player Liquid Silver vs. Raider Debt

How much debt can prisoners realistically pay off through labor?

| Colony Wealth | Typical Silver on Hand | Max Reasonable Debt per Raider | Labor Days at 20 silver/day |
|---------------|------------------------|------------------------------|---------------------------|
| 10,000 | 500 | 200-500 | 10-25 days |
| 30,000 | 2,000 | 500-1,000 | 25-50 days |
| 100,000 | 10,000 | 1,000-2,000 | 50-100 days |
| 300,000 | 30,000 | 2,000-5,000 | 100-250 days |

**Reality Check:** A 10,000 silver debt requires 500 days of labor at 20 silver/day. That's 1.4 RimWorld years. This is clearly excessive for a raider who shot someone a few times.

---

## 9. Comparative Economics

### 9.1 What Can You Buy With Various Silver Amounts?

Understanding purchasing power helps contextualize penalties:

| Silver Amount | What It Buys | Equivalent Actions |
|---------------|-------------|-------------------|
| 50 | 3 herbal medicine OR 1 component | Treating 3 minor injuries |
| 100 | 6 herbal medicine OR 3 standard medicine | Treating 1 moderate injury |
| 500 | 15 standard medicine OR 1 bionic eye | Treating several colonists OR permanent upgrade |
| 1,000 | 30 standard medicine OR 1 excellent sculpture | Full medical stockpile OR mood booster |
| 2,000 | Bionic arm + leg OR 10 components + 50 medicine | Restoring a crippled pawn |
| 5,000 | Full set of bionics OR 20 excellent meals + clothes | Making a super soldier |
| 10,000 | Full recon armor set OR power generator | Major defensive upgrade |

### 9.2 Pawn Value Economics

How much is a colonist "worth"?

| Pawn Type | Baseline Value | Skill Value | Gear Value | Total Value | Notes |
|-----------|----------------|-------------|------------|-------------|-------|
| Fresh Recruit | 1,500 | 0 | 200 | 1,700 | No skills, basic gear |
| Average Colonist | 1,500 | 500 | 800 | 2,800 | Some skills, decent gear |
| Skilled Colonist | 1,500 | 2,000 | 1,500 | 5,000 | High skills, good gear |
| Expert Colonist | 1,500 | 5,000 | 3,000 | 9,500 | Expert skills, bionics |
| Super Soldier | 1,500 | 3,000 | 10,000 | 14,500 | Combat focused, archotech |

**Murder Penalty Context:** Current murder penalty (3,000-6,000) is roughly the value of an average colonist, which makes sense. However, assault penalties (400-800) accumulate rapidly when one colonist takes multiple hits.

---

## 10. Summary: Key Economic Benchmarks

### Quick Reference Table

| Category | Low Value | Medium Value | High Value | Very High Value |
|----------|-----------|--------------|------------|-----------------|
| **Individual Items** | <50 silver | 50-300 | 300-1,000 | >1,000 |
| **Medical Treatment** | <50 silver | 50-200 | 200-500 | >500 |
| **Weapons** | <100 silver | 100-400 | 400-1,000 | >1,000 |
| **Buildings** | <50 silver | 50-500 | 500-2,000 | >2,000 |
| **Raid Loot** | <1,000 | 1,000-5,000 | 5,000-15,000 | >15,000 |
| **Colony Wealth** | <20,000 | 20,000-100,000 | 100,000-300,000 | >300,000 |
| **Silver on Hand** | <1,000 | 1,000-10,000 | 10,000-50,000 | >50,000 |

### Penalty Balancing Guidelines

Based on this economic analysis:

1. **Minor injuries (scratches, bruises):** 20-50 silver (2-3x medical cost)
2. **Moderate injuries (gunshots, stabs):** 100-300 silver (2-3x medical cost)
3. **Severe injuries (burns, organs):** 300-800 silver (2-3x medical cost)
4. **Permanent injuries (limbs, eyes):** 800-2,000 silver (matches bionic replacement cost)
5. **Death (murder):** 2,000-5,000 silver (matches colonist value)
6. **Property damage:** 1-2x property value (reasonable restitution)
7. **Theft:** 1.5x item value (current system is good)

These recommendations will be detailed in the next document.

---

**Document Status:** Complete
**Next Steps:** Create Penalty_Balancing_Recommendations.md with specific numerical recommendations
