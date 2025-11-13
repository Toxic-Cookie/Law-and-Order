# Dwarf Fortress Justice System

## Overview

The justice system in Dwarf Fortress is designed to maintain order within the fortress by detecting, investigating, and punishing criminal behavior. It operates through a hierarchy of officials who handle different aspects of crime and punishment.

## Justice Officials

### Sheriff

**Role**: Early-game justice administrator for small fortresses

**Responsibilities**:
- Investigates crimes in small fortresses
- Administers justice for criminal acts
- Eventually replaced by Captain of the Guard as fortress grows

**Requirements**:
- Appointed through nobles/administrators screen
- No special room requirements initially

**Key Skills**:
- Social skills (for investigation)
- Judge of intent (for determining guilt)

### Captain of the Guard

**Role**: Advanced justice administrator and military leader

**Responsibilities**:
- Replaces sheriff in larger, more populous fortresses
- Leads the fortress guard
- Conducts interrogations in office
- Investigates crimes
- Oversees capture and imprisonment of criminals

**Requirements**:
- Office for conducting interrogations
- Assigned when fortress population grows
- May command multiple fortress guards

**Key Skills**:
- Social skills (critical for interrogation success)
- Judge of intent (for investigation)
- Various conversation skills (persuasion, flattery, intimidation)

**Important Mechanic**: Interrogation success is determined by comparing the Captain's social skills against the suspect's resistance. Higher social skills lead to more confessions and discovered information about criminal organizations.

**Vulnerability**: The Captain of the Guard can be corrupted during interrogation if the attempt goes badly. A captured agent may successfully bribe or manipulate your Captain, compromising the entire justice system.

### Hammerer

**Role**: Fortress executioner who administers severe corporal punishment

**Responsibilities**:
- Delivers hammer strikes as punishment for serious crimes
- Executes capital punishment sentences

**Requirements**:
- No room or furniture requirements
- Assigned through nobles screen

**Key Skills**:
- Hammerdwarf skill (weapon proficiency)
- Strength (for effective strikes)

**Important Consideration**: Players often deliberately assign a weak, unskilled dwarf as Hammerer to reduce the likelihood of criminals dying from punishment.

**Psychological Effects**:
- Hammerers receive happy thoughts from administering punishment
- Guards receive happy thoughts from witnessing justice
- Other dwarves may become stressed from witnessing brutal punishments

### Fortress Guard

**Role**: Enforcement officers who carry out sentences

**Responsibilities**:
- Deliver beatings to criminals
- Escort criminals to jail
- Escort criminals to Hammerer
- Maintain order in prisons

**Requirements**:
- Assigned by Captain of the Guard
- Usually drawn from fortress military

**Number Needed**: Game requests approximately one-tenth the population in guards plus restraints/cages combined.

**Critical Warning**: Fortress guards trained to physical perfection have extremely high fatality rates when delivering beatings, as punches to the head frequently kill criminals. This is often considered undesirable.

## Crime Detection Mechanics

### Witness System

**How It Works**:
1. Crime is committed within line of sight of another dwarf
2. Witness observes the criminal act
3. Witness reports to Sheriff/Captain of the Guard
4. Case appears in Justice screen with witness testimony

**Witness Information Displayed**:
- Name of witness(es)
- Accused criminal
- Crime type
- Date of crime

**Witness Reliability Issues**:

**False Accusations**: Witnesses may deliberately lie to frame dwarves they hold grudges against. This creates a social dynamic where personal conflicts can result in false criminal charges.

**Vampire Manipulation**: When vampires commit murder, they often file false reports to frame innocent dwarves, deflecting suspicion from themselves.

**Impossible Accusations**: The system may blame physically impossible suspects including:
- Long-dead dwarves
- Tame animals
- Creatures that weren't present

**Social Consequence**: The populace becomes "affronted at particularly nonsensical convictions," creating negative morale effects when justice is obviously miscarried.

### Crimes Without Witnesses

**No Direct Witnesses**: If a crime (like theft) occurs without anyone seeing the act itself, but witnesses notice the result (missing artifact), the case will show witnesses who discovered the theft but no direct suspect.

**Interrogation Limitation**: When no witnesses observed the crime directly, interrogation options become limited. Players may only be able to interrogate those who noticed the aftermath.

**Investigation Strategy**: Without witnesses, players must rely on:
- Interrogating suspicious arrivals
- Looking for circumstantial evidence (skill levels, social connections for vampires)
- Identifying visitors with false identities
- Tracking item movement

## Investigation and Interrogation

### The Justice Screen

**Access**: Press 'Z' to open status screen, navigate to Justice tab

**Information Available**:
- **Open Cases**: Unsolved crimes with witness testimony
- **Closed Cases**: Resolved crimes with conviction records
- **Case Details**: Injured party, crime type, date, witnesses, accused
- **Interrogation Targets**: List of all creatures in and around fortress

**UI Limitations**: The interrogation list shows all creatures (dead or alive) in no particular order with no search function, making it difficult to use in large fortresses with many visitors.

### Interrogation Mechanics

**Process**:
1. Player selects suspect from interrogation list
2. Captain of the Guard is assigned to interrogate
3. Suspect is escorted to Captain's office
4. Interrogation occurs (behind the scenes)
5. Results appear in report

**Success Determination**:
- Captain's social skills vs suspect's resistance
- Relevant skills: persuasion, judge of intent, flattery, intimidation
- Higher skill differential = higher success chance

**Successful Interrogation Yields**:
- Confession of crimes committed by suspect
- Membership in villainous organizations
- Names of other organization members
- Information about ongoing plots
- Intelligence on artifact theft schemes

**Failed Interrogation**:
- No information gained
- Suspect remains silent
- Wasted time for Captain and suspect
- No negative consequence for innocent dwarves

**Critical Risk**: If interrogation goes very badly, the suspect may successfully corrupt the Captain of the Guard, turning your justice administrator into an agent of the criminal organization.

### Investigation Best Practices

**Routine Screening**: Interrogate all new arrivals (immigrants and visitors) to identify:
- Infiltrators using false identities
- Members of criminal organizations
- Corrupted individuals
- Potential threats

**No Penalty for Innocent**: Interrogating innocent dwarves has no negative effects beyond lost work time, so systematic interrogation of all suspects is viable.

**Priority Targets**:
- Visitors with unusually long residence times
- Dwarves with excessive skills (possible vampires)
- Those present when crimes occurred
- Anyone with abnormal social connections

**Social Skill Investment**: Ensuring your Captain of the Guard has high social skills is critical for effective interrogation. Consider training conversation skills through practice.

## Conviction Process

### Automatic Conviction

**Noble Mandate Violations**: These crimes automatically result in conviction without player input:
- Violation of production order
- Violation of export prohibition
- Violation of job order (guild mandates)

**Process**:
1. Mandate expires or is violated
2. Noble automatically selects target(s) for punishment
3. Noble prefers punishing dwarves whose profession relates to mandate
4. If no relevant professional available, random citizen selected
5. Punishment assigned immediately

**No Player Control**: Players cannot prevent or override these automatic convictions.

### Player Conviction Required

**Murder and Serious Crimes**: Player must manually convict for:
- Murder
- Assault
- Theft of artifacts
- Espionage
- Other major crimes

**Process**:
1. Review case in Justice screen
2. Examine witness testimony
3. Consider interrogation results
4. Select accused dwarf
5. Choose to convict or dismiss

**Moral Dilemma**: Players must balance:
- Witness reliability (may be lying)
- Evidence strength
- Risk of false conviction
- Community morale
- Need for justice

### Conviction Consequences

**Criminal Effects**:
- Receives unhappy thought
- May become depressed or stressed
- Could trigger tantrum if already stressed
- Social reputation damaged

**Community Effects**:
- Victims/injured party receive satisfaction if justice served
- Unfair convictions create unhappy thoughts
- Nonsensical convictions affront the populace
- Harsh punishments may stress witnesses

**Official Effects**:
- Hammerer receives happy thought from punishing
- Guards receive happy thoughts from enforcing justice
- Captain gains experience in social skills

## Punishment Exemptions

Certain creatures are exempt from justice system:

**Automatically Exempt**:
- Dead creatures
- Insane dwarves
- Dwarves in strange moods
- Children
- Babies
- Non-humanoid creatures

**Position-Based Exemption**: Position holders with `PUNISHMENT_EXEMPTION` tag in their position definition are immune to justice system.

**Practical Impact**: This means some nobles and officials may commit crimes with impunity, creating potential for corrupt leadership.

## Ethics and Cultural Variation

### Civilization Ethics

Each civilization has ethical frameworks that determine punishment severity:

**Ethical Tags**:
- **PUNISH_SERIOUS**: Crime considered serious
  - Results in: Beating OR 1 month imprisonment
- **PUNISH_CAPITAL**: Crime considered capital offense
  - Results in: 8 months imprisonment OR 50 hammer strikes

**Crime-Specific Ethics**: Different civilizations may rate the same crime differently. For example:
- One civilization treats theft as PUNISH_SERIOUS
- Another treats same theft as PUNISH_CAPITAL
- Results in drastically different punishment for same act

### Inter-Civilization Relations

**Ethics-Based Diplomacy**: Civilizations with similar ethics become friends, while conflicting ethics create animosity.

**Asymmetric Offense**: Civilizations are more offended by others committing their taboos than by others punishing those taboos:
- Civ A allows slavery (Acceptable)
- Civ B bans slavery (Capital Offense)
- Civ A finds Civ B "most unreasonable" (-5) for executing slavers
- Civ B is "shocked and disgusted" (-15) that Civ A practices slavery
- Civ B is 3x more offended, more likely to declare war

## Justice System Dysfunction

### Common Problems

**Excessive Lethality**: Guards with high fighting skills frequently kill criminals during beatings, especially with head strikes. This often feels disproportionate to minor crimes.

**False Accusations**: Grudges and vampire manipulation lead to innocent dwarves being punished, creating injustice within the fortress.

**Tantrum Spirals**: Harsh punishment stresses fortress population, triggering breakdowns that cause more crimes, creating cascading justice problems.

**Infrastructure Failures**:
- No jail available → punishment downgraded to beating
- No chains/restraints → imprisonment impossible
- No Hammerer appointed → capital punishment impossible
- Captain corrupted → entire system compromised

**UI Limitations**: Interrogation interface becomes unusable in large fortresses with many visitors, making investigation impractical.

### Player Strategies

**Deliberate Incompetence**: Assign weak, unskilled hammerer to reduce fatality rates.

**Luxury Prisons**: Create comfortable jails with quality furniture, food, and alcohol to minimize prisoner stress.

**Ignoring Justice**: Some players simply don't appoint justice officials, accepting unhappy nobles rather than dealing with problematic system. This creates +20 "glad to have punishment delayed" thoughts.

**Vampire Identification**: Use skill/relationship checks rather than justice system to identify vampires, then handle extrajudicially through accidents or arena fights.

**Systematic Interrogation**: Interrogate all visitors immediately upon arrival to identify infiltrators before they corrupt residents.

**Corruption Prevention**: Lock Captain in office during critical interrogations to prevent escape of confessed criminals or corrupted Captain.

## Integration with Other Systems

### Stress and Mental Health

Stressed dwarves commit tantrum-related crimes (vandalism, assault, building destruction), creating workload for justice system. Harsh punishments increase stress, potentially creating feedback loop.

### Social Relationships

Personal grudges influence witness testimony. Dwarves may falsely accuse those they dislike, making social network awareness important for fair justice.

### Nobility System

Nobles issue mandates that create automatic convictions. More powerful nobles (Duke, Monarch) can issue more simultaneous mandates, increasing justice system burden.

### Military and Security

Fortress guard is drawn from military, so training guards improves military but increases punishment lethality. Balance needed between effective defense and non-lethal justice.

### Economic Impact

Imprisoned dwarves don't work, creating labor shortages. Long sentences for key workers (legendary craftsdwarves) can significantly impact fortress economy.

## RimWorld Modding Implications

### Transferable Concepts

1. **Witness-Based Detection**: Crimes require line-of-sight observation, creating uncertainty and false accusations
2. **Social Skill Checks**: Investigation effectiveness depends on investigator social skills vs suspect resistance
3. **Graduated Punishment**: Severity scales with crime type and cultural ethics
4. **Corruption Vulnerability**: Justice officials can be compromised, undermining the system
5. **Automatic vs Manual Conviction**: Some crimes auto-convict while others require player judgment
6. **Morale Cascades**: Punishments affect community mood, potentially triggering more crime
7. **Infrastructure Requirements**: Justice system needs physical spaces (offices, jails) to function
8. **False Accusations**: Imperfect information creates emergent drama

### Adaptation Challenges

1. **RimWorld's Smaller Scale**: DF fortresses reach 200+ dwarves; RimWorld colonies typically 10-20 pawns
2. **Player Attachment**: RimWorld players more attached to individual colonists than DF players to dwarves
3. **Lethality Expectations**: RimWorld players may not accept DF's casual punishment deaths
4. **Automation Level**: Balance between automated justice and player micromanagement
5. **UI Design**: DF's problematic interrogation UI should be improved in RimWorld implementation

### Recommended Implementations

1. **Witness System**: Crimes only detected if observed, with false accusation chance based on social relationships
2. **Investigation Role**: Dedicated warden/investigator with social skills determining success
3. **Evidence Gathering**: Multiple investigation attempts reveal more information
4. **Cultural Justice**: Ideology system determines punishment severity preferences
5. **Morale Integration**: Tie punishment effects into existing mood system
6. **Physical Infrastructure**: Require interrogation rooms, quality cells for prisoner management
