# Law and Order: Design Philosophy

## Core Vision

**Law and Order is a Dwarf Fortress-inspired total conversion mod** that transforms RimWorld's justice system from a simple mood management mechanic into a complex, emergent narrative system with imperfect information, investigation gameplay, and genuine consequences.

## Fundamental Principles

### 1. Imperfect Information is Essential

**The Problem with RimWorld's Default Visibility**:
- Players see everything at all times
- Perfect information removes uncertainty
- No mystery, no investigation, no emergent drama

**The Dwarf Fortress Solution**:
- Player vision ≠ Character knowledge
- Crimes only exist if witnessed by characters
- Hidden information creates genuine investigation gameplay

**Our Implementation**:
- **Required dependency on Real Fog of War**
- Crimes only detected if colonists have line of sight
- No witnesses = no crime recorded
- Creates genuine uncertainty about what happened

### 2. Embrace Systemic Injustice

Unlike RimWorld's "fair" systems, DF-style justice is **intentionally flawed**:

**False Accusations**:
- Witnesses may lie based on social relationships
- Grudges lead to false testimony
- Wrong person convicted creates colony drama

**Impossible Convictions**:
- System may blame dead people
- Animals accused of crimes
- Physically impossible suspects

**Player Dilemma**:
- Trust witness testimony?
- Investigate thoroughly or accept surface evidence?
- Risk of punishing innocents

### 3. Investigation is Core Gameplay

Not just "crime happens → punishment":

**Evidence Gathering**:
- Witness testimony (unreliable)
- Interrogations (skill-based, can fail)
- Circumstantial evidence
- Conflicting accounts

**Social Skills Matter**:
- Warden/investigator social skills determine success
- High social = better confessions
- Low social = limited information

**Time Investment**:
- Investigations take real time
- Pulling investigators from other duties
- Balance thoroughness vs. efficiency

### 4. Consequences Create Cascades

Every action has ripple effects:

**Punishment Affects Morale**:
- Victim wants justice
- Criminal becomes depressed
- Witnesses stressed by harsh punishment
- Colony-wide mood impact

**Tantrum Spirals**:
- Harsh punishment → stress
- Stress → more crimes
- More crimes → more punishment
- Feedback loop can destroy colony

**Social Network Damage**:
- Punishing popular colonists = widespread unhappiness
- Friends of punished become angry
- Erodes trust in justice system

### 5. Hardcore Experience, Not Casual

**This is NOT a "quality of life" mod**:
- Makes game significantly harder
- Requires Real Fog of War (changes core gameplay)
- Demands active player management
- Punishes neglect and ignorance

**Target Audience**:
- Players who love Dwarf Fortress
- Players seeking challenging, emergent narratives
- Players who enjoy investigation/mystery gameplay
- Players willing to lose colonists to systemic problems

**Not For**:
- Casual players
- Players wanting simple mood management
- Players seeking "fair" systems
- Players who dislike required dependencies

## Design Priorities

### Priority 1: Emergent Narratives Over Balance

**Goal**: Create memorable stories, not balanced gameplay

Examples:
- Innocent colonist falsely accused and executed
- Vampire frames someone else for murder
- Colony torn apart by false accusations
- Infiltrator corrupts justice system from within

**Balance is Secondary**: If a system creates better stories, it's good even if "unfair"

### Priority 2: Player Agency Through Investigation

**Players should feel like detectives**:
- Gather evidence
- Interrogate suspects
- Weigh conflicting testimony
- Make difficult judgment calls

**Not Automation**:
- Avoid "solve crime" button
- Require player thought and effort
- Mistakes should be possible

### Priority 3: Compatibility with DF Research

**Use Dwarf Fortress documentation as design bible**:
- Reference DF mechanics extensively
- Adapt proven systems rather than inventing new ones
- Maintain fidelity to DF's design philosophy

**Documented in**:
- `llms/research/` - Full DF system analysis
- Direct adaptations where possible
- Adjustments only where necessary for RimWorld

### Priority 4: No Legacy Support

**Breaking Changes Are Acceptable**:
- New game save required for major updates
- No backward compatibility guarantee
- Active development prioritized over stability

**Rationale**:
- Mod is in development, not release
- Maintaining legacy code slows innovation
- Testing always with fresh saves anyway

## What This Mod Is NOT

### Not a Raiders-Only System

While initially focused on raider crimes, the **real depth** comes from internal colony crimes:
- Colonist mental breaks
- Theft between colonists
- Corruption and infiltration
- Murder mysteries

Raiders are just the obvious starting point.

### Not a Mood Management Tool

This mod **increases** difficulty and complexity:
- Adds new sources of unhappiness
- Creates colony-threatening cascades
- Requires active management
- Can destroy colonies if mishandled

### Not Optional Systems

All systems are **core and interconnected**:
- Can't just use "trial system" without investigation
- Can't skip witness mechanics
- Can't ignore fog of war
- Everything works together

### Not Vanilla-Friendly

**Requires fundamental gameplay changes**:
- Real Fog of War (required dependency)
- Changes how you play RimWorld
- Incompatible with "casual" approach
- Embraces complexity

## Comparison to Vanilla

| Aspect | Vanilla RimWorld | Law and Order |
|--------|-----------------|---------------|
| Crime Detection | Automatic | Requires witnesses |
| Player Knowledge | Omniscient | Limited to colonist vision |
| Justice System | Simple mood debuff | Complex investigation |
| Punishments | Mood management | Colony-threatening |
| False Accusations | Never | Common |
| Investigation | None | Core gameplay |
| Difficulty | Moderate | High |
| Required Mods | None | Real Fog of War |

## Comparison to Dwarf Fortress

| Aspect | Dwarf Fortress | Law and Order |
|--------|----------------|---------------|
| Witness System | ✓ Line of sight | ✓ Via Fog of War |
| False Accusations | ✓ Common | ✓ Planned |
| Interrogations | ✓ Skill-based | ✓ Planned |
| Corruption | ✓ Officials bribed | ✓ Planned |
| Infiltration | ✓ Spies/villains | ✓ Planned |
| Tantrum Spirals | ✓ Stress cascades | ✓ Integrated |
| Imperfect Justice | ✓ Core design | ✓ Core design |
| Scale | 100+ dwarves | 10-20 colonists |

## Development Roadmap Philosophy

### Phase 1: Foundation (Current)
- Crime tracking system
- Basic trial mechanics
- Witness detection via FoW
- Simple conviction flow

### Phase 2: Investigation
- Interrogation system
- Evidence gathering
- Witness reliability
- Social skill checks

### Phase 3: Corruption & Intrigue
- Infiltrator mechanics
- Corrupted officials
- False testimony
- Hidden loyalties

### Phase 4: Polish & Balance
- UI improvements
- Performance optimization
- Edge case handling
- Player feedback integration

**Each phase builds on previous**, no going back to "fix" old systems unless fundamentally broken.

## User Experience Goals

### What Players Should Feel

**Uncertainty**:
- "Did I convict the right person?"
- "Can I trust this witness?"
- "What really happened?"

**Investigation**:
- "I need to interrogate more people"
- "This testimony doesn't match..."
- "Who saw what?"

**Consequence**:
- "That execution destroyed colony morale"
- "The tantrum spiral started from harsh justice"
- "Should have investigated more thoroughly"

**Emergence**:
- "The vampire was framing people all along!"
- "My best colonist was a spy the whole time"
- "The false accusation tore the colony apart"

### What Players Should NOT Feel

**Fairness**: This system is intentionally unfair
**Simplicity**: Investigation requires effort
**Optional**: Can't ignore and expect good results
**Casual**: Demands active engagement

## Technical Philosophy

### Leverage Existing Systems

**Don't reinvent the wheel**:
- Use RimWorld's social system for relationships
- Use Ideology for cultural variations
- Use mental break system for stress crimes
- Use Real Fog of War for visibility

### Harmony Patching

**Patch, don't replace**:
- Minimal conflicts with other mods
- Integrate with vanilla systems
- Extend rather than override

### Performance Conscious

**But not at narrative cost**:
- Optimize where possible
- Accept some overhead for better gameplay
- Clear documentation of performance impacts

### Modular Code

**Well-organized for future expansion**:
- Separate systems (crimes, trials, investigation)
- Clear interfaces between modules
- Easy to add new crime types
- Documented for future developers

## Success Metrics

**This mod succeeds if**:

1. **Players tell stories** about their justice systems
2. **False accusations create drama** not frustration
3. **Investigation feels engaging** not tedious
4. **Fog of War integration feels natural** not bolted-on
5. **Dwarf Fortress players recognize the mechanics**
6. **Players start new games to try different approaches**

**This mod fails if**:

1. Players ignore the system entirely
2. Investigation feels like busywork
3. Always obvious who's guilty
4. No emergent narratives
5. Just a "punishment menu"
6. Players disable FoW requirement

## Final Thoughts

**Law and Order is an experiment in bringing Dwarf Fortress's design philosophy to RimWorld.**

It prioritizes:
- **Emergent narratives** over balance
- **Player investigation** over automation
- **Imperfect information** over clarity
- **Systemic complexity** over simplicity
- **Hardcore experience** over accessibility

It's not for everyone. **That's by design.**

For players who want genuine investigation gameplay, where you can convict the wrong person, where social dynamics matter, where uncertainty drives engagement - **this is the mod for you**.

For everyone else - there are many excellent, more accessible justice mods available.

---

*"Justice is imperfect, and that's what makes it interesting."* - Dwarf Fortress design philosophy
