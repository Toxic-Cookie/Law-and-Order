# Dwarf Fortress Justice, Intrigue, and Crime Systems - Overview

## Executive Summary

Dwarf Fortress features an intricate system of justice, intrigue, and crime that creates dynamic social challenges within fortress management. This documentation covers the comprehensive mechanics of how crimes are committed, detected, investigated, and punished, as well as the underlying intrigue system that allows villains to corrupt fortresses from within.

## System Components

### 1. Justice System
The justice system is administered by designated officials (Sheriff/Captain of the Guard, Hammerer) who investigate crimes, convict criminals, and administer punishments. The system includes:
- Crime detection through witnesses
- Investigation and interrogation mechanics
- Multiple punishment types based on severity
- Prison and restraint systems

### 2. Intrigue System
A network-based corruption system where villains use social manipulation to:
- Recruit agents within fortresses
- Coordinate complex plots across multiple sites
- Steal artifacts through corrupted dwarves
- Undermine fortress stability through sabotage

### 3. Crime System
Multiple crime categories with varying severity:
- Noble mandate violations (production orders, export prohibitions)
- Violent crimes (murder, assault, disorderly conduct)
- Property crimes (theft, vandalism, building destruction)
- Conspiracy crimes (espionage, treason, bribery)

### 4. Punishment System
Graduated punishment severity based on civilization ethics:
- Beatings (immediate physical punishment)
- Imprisonment (temporary incarceration)
- Hammering (severe corporal punishment)

## Key Mechanics

### Crime Detection Flow
1. Crime is committed by dwarf/visitor/creature
2. Witnesses observe the crime
3. Witnesses report to Sheriff/Captain of the Guard
4. Case appears in Justice screen
5. Player/System convicts the criminal
6. Punishment is assigned and executed

### Intrigue Operation Flow
1. Villain identifies target fortress with valuable artifacts
2. Agent visits fortress under false identity
3. Agent socializes with residents and identifies corruption targets
4. Agent uses corruption techniques (bribery, flattery, intimidation, etc.)
5. Resident becomes corrupted and joins villainous organization
6. Corrupted resident assists in plot (artifact theft, sabotage, etc.)
7. Plot is executed and evidence may be discovered

### Punishment Execution Flow
1. Criminal is convicted in Justice screen
2. Punishment severity determined by crime type and civilization ethics
3. Fortress guard escorts criminal to punishment location
4. Punishment administered (beating, imprisonment, or hammering)
5. Morale effects cascade through fortress population

## Design Philosophy

The Dwarf Fortress justice system emphasizes:

**Imperfection and Chaos**: False accusations, incorrect convictions, and harsh punishments that kill criminals create emergent drama rather than clean resolution.

**Social Networks**: Grudges, relationships, and social dynamics influence accusations and morale impacts.

**Player Agency vs Automation**: Some crimes auto-convict (mandate violations) while others require player judgment (murder), creating moral dilemmas.

**Cascading Consequences**: Punishments affect fortress morale, potentially triggering tantrum spirals where stressed dwarves commit more crimes.

**Hidden Information**: Infiltrators use false identities, witnesses may lie, and interrogation success depends on skill checks, creating uncertainty.

## Relevance to RimWorld Modding

Key concepts translatable to RimWorld's Law and Order mod:

1. **Witness-Based Detection**: Crimes require observation rather than omniscient detection
2. **Social Manipulation Mechanics**: Intrigue system's corruption techniques could inspire social manipulation of colonists
3. **Graduated Punishment Severity**: Punishment type scales with crime severity and cultural values
4. **Investigation and Interrogation**: Active player involvement in solving crimes through interrogation
5. **Morale Impact System**: Punishments affect entire community, not just criminal
6. **False Accusations**: Imperfect justice system creates emergent storytelling
7. **Infiltrator Mechanics**: External agents corrupting internal colonists
8. **Evidence and Confession Systems**: Information gathering through interrogation

## Documentation Structure

This research is organized into the following documents:

- **01_Justice_System.md**: Detailed mechanics of crime detection, roles, and conviction
- **02_Crime_System.md**: Comprehensive crime types, triggers, and witness mechanics
- **03_Intrigue_System.md**: Villain networks, corruption techniques, and plot types
- **04_Punishment_System.md**: Punishment types, severity calculation, and execution
- **05_Prison_System.md**: Jail construction, restraint types, and prisoner management
- **06_Special_Cases.md**: Vampires, tantrums, and edge cases in the justice system

## Version Information

Research conducted: November 2025
Primary sources: Dwarf Fortress Wiki (main version and DF2014 legacy documentation)
Game versions covered: Primarily current release version with references to DF2014 mechanics where relevant

## Quick Reference

### Crime Severity Tiers
- **Minor**: Production/export violations → Beating or 1 month imprisonment
- **Major**: Assault, theft, vandalism → 8 months imprisonment or beating
- **Capital**: Murder → 8 months imprisonment or 50 hammer strikes

### Key Officials
- **Sheriff**: Early-game justice administrator for small fortresses
- **Captain of the Guard**: Replaces sheriff in larger fortresses, conducts interrogations
- **Hammerer**: Executes severe corporal punishments
- **Fortress Guard**: Enforces sentences, delivers beatings, escorts prisoners

### Essential Investigation Tools
- Justice screen (Z menu): View open cases, witnesses, suspects
- Interrogation: Captain of the Guard questions suspects in office
- Witness reports: Dwarves automatically report observed crimes
- Social skills: Determine interrogation success (Captain vs suspect)

### Common Pitfalls
- False accusations from vampires or grudge-holding dwarves
- Beatings killing criminals due to head trauma
- No available jails causing punishment downgrade
- Interrogating wrong suspects wastes time
- Punishments triggering tantrum spirals
- Corrupted Captain of the Guard protecting infiltrators
