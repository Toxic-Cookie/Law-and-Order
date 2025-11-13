# Dwarf Fortress Punishment System

## Overview

The punishment system in Dwarf Fortress administers consequences for criminal behavior through a graduated severity structure. Punishments range from brief beatings to extended imprisonment to severe corporal punishment, with specific punishment type determined by crime severity, civilization ethics, and available infrastructure.

## Punishment Types

### Beating

**Description**: Physical punishment delivered by fortress guard through unarmed strikes.

**Duration**: Brief (typically until criminal is subdued/sufficiently injured)

**Administration**:
1. Criminal convicted of crime
2. Fortress guard assigned to deliver beating
3. Guard locates criminal
4. Guard delivers punches/strikes
5. Beating continues until complete

**Typical Crimes Punished with Beating**:
- Minor mandate violations
- Vandalism
- Disorderly conduct
- First-time offenses
- Crimes rated PUNISH_SERIOUS when no jail available

**Injury Effects**:
- Bruising and pain
- Broken bones
- Unconsciousness
- **Critical Risk**: Head strikes frequently lethal

**Skill Dependency**: Guard's fighting skills determine damage dealt:
- Low-skill guard: Non-lethal bruising
- High-skill guard: Broken bones, potential fatality
- Master fighter: Very high death rate

**Morale Effects**:
- Criminal: Unhappy thought from being beaten
- Guard: Happy thought from administering justice
- Witnesses: May become stressed if beating appears excessive or fatal

**Common Problem**: Players often report beatings unintentionally killing criminals, especially for minor offenses.

### Imprisonment

**Description**: Criminal confined to jail cell for specified duration using chains, ropes, or cages.

**Duration**:
- PUNISH_SERIOUS: 1 month
- PUNISH_CAPITAL: 8 months
- May vary based on specific crime and ethics

**Requirements**:
- Designated jail zones with restraints or cages
- Available restraint (not currently occupied)
- Fortress guards to escort prisoner

**Administration Process**:
1. Criminal convicted and sentenced
2. Fortress guard assigned to escort
3. Guard locates criminal
4. Criminal escorted to available jail cell
5. Criminal restrained (chained/caged)
6. Sentence duration begins
7. Criminal released when sentence expires

**Imprisonment Experience**:
- Criminal cannot work or move freely
- Must be fed and given water
- Experiences isolation stress
- Receives unhappy thoughts from imprisonment
- Duration aware (knows sentence length)

**Infrastructure Impact**:
- One restraint occupied per prisoner
- Guards required for escort and supervision
- Food/drink delivery workload for other dwarves
- Jail space limits maximum simultaneous prisoners

**Release**:
- Automatic when sentence expires
- Criminal returns to normal duties
- Restraint becomes available for next prisoner

### Hammering

**Description**: Severe corporal punishment delivered by Hammerer using war hammer strikes.

**Typical Sentence**: 50+ hammer strikes for capital crimes

**Administration**:
1. Criminal convicted of capital crime
2. Hammerer assigned to deliver punishment
3. Criminal escorted to justice location (restraint)
4. Criminal restrained for hammering
5. Hammerer delivers specified number of strikes
6. Criminal released if survives

**Restraint Requirement**:
- Requires justice-designated chains/restraints
- Cannot be performed in cages
- If no justice restraints available, downgraded to beating

**Injury Severity**:
- Massive tissue damage
- Multiple broken bones
- Organ damage
- Severe bleeding
- High fatality rate (often intentional)

**Survival Factors**:
- Hammerer skill (lower = less deadly)
- Criminal toughness and size
- Medical intervention after punishment
- Random strike locations

**Typical Crimes**:
- Murder
- Treason
- Serious theft
- Capital-rated crimes per civilization ethics

**Morale Effects**:
- Hammerer: Strong happy thought
- Guards: Happy thought from witnessing justice
- Criminals: Extreme unhappy thought (if survive)
- Witnesses: Significant stress from witnessing brutality

**Psychological Impact**: Witnessing death from hammering creates strong negative thoughts throughout fortress.

## Punishment Severity Determination

### Civilization Ethics

**Ethics System**: Each civilization has ethical framework defining how seriously they view different behaviors.

**Severity Ratings**:

**PUNISH_SERIOUS**:
- Crime considered serious but not capital
- Punishment options:
  - Beating by guard
  - 1 month imprisonment
- Examples: Minor theft, disorderly conduct, first offenses

**PUNISH_CAPITAL**:
- Crime considered capital offense worthy of severe punishment
- Punishment options:
  - 8 months imprisonment
  - 50 hammer strikes
- Examples: Murder, treason, major theft, espionage

**PUNISH_REPRIMANDED**:
- Minor infraction warranting warning
- Typically results in social consequences rather than physical punishment
- Rare in fortress mode

**NO_PUNISHMENT**:
- Behavior considered acceptable
- Not considered crime in this civilization
- No justice system involvement

### Crime-Specific Severity

**Automatic Assignment**: Most crimes have preset severity based on type:

**Minor Severity (PUNISH_SERIOUS)**:
- Violation of production order
- Violation of export prohibition
- Violation of job order
- Vandalism
- Disorderly conduct

**Major Severity (PUNISH_CAPITAL)**:
- Murder
- Attempted murder
- Treason
- Espionage
- Major theft (artifacts)
- Embezzlement

### Cultural Variation

**Same Crime, Different Punishment**: Different civilizations may rate same act differently.

**Example Scenario**:
- **Elven Civilization**: Cutting trees = PUNISH_CAPITAL
- **Dwarven Civilization**: Cutting trees = NO_PUNISHMENT
- Dwarf trading wood to elves might face different justice in each civilization

**Ethical Conflicts**:
- Create diplomatic tensions
- Influence migration patterns
- Affect cross-civilization justice
- May trigger wars over ethical disagreements

## Punishment Downgrading

### Infrastructure Limitations

**No Jail Available**:
- Imprisonment sentence automatically downgraded to beating
- Guards deliver beating instead of escorting to jail
- Duration not relevant (beating is immediate)

**Morale Consequences**:
- **Criminal**: Receives happy thought "reduced punishment" (+20)
- **Victim/Injured Party**: Receives unhappy thought "justice not properly served"
- Net effect often favors criminal's happiness

**No Justice Restraints**:
- Hammering cannot be performed without restraints
- Sentence downgraded to beating
- Less severe than intended

**No Hammerer Appointed**:
- Hammer strikes impossible to deliver
- System may downgrade to imprisonment or beating
- Capital punishment effectively unavailable

### Systematic Downgrading

**Infrastructure Dependency Chain**:
1. Ideal: Proper infrastructure for assigned punishment
2. Degraded: Missing infrastructure causes downgrade
3. Minimum: Beating always available (requires only guards)

**Player Exploitation**: Some players deliberately avoid building jails to force downgrades and generate "reduced punishment" happiness.

## Punishment Execution Process

### Conviction to Punishment Flow

**Standard Process**:
1. **Crime Occurs**: Witnessed and reported
2. **Case Opened**: Appears in justice screen
3. **Investigation**: Optional interrogation
4. **Conviction**: Automatic or player-selected
5. **Sentencing**: Punishment assigned based on crime + ethics
6. **Scheduling**: Guard assigned to execute punishment
7. **Execution**: Punishment delivered
8. **Completion**: Criminal released or sentence begins
9. **Morale Effects**: Cascade through fortress population

### Timing and Scheduling

**Job Priority**:
- Justice tasks typically high priority for assigned guards
- Guards interrupt other duties to administer punishment
- Captain of the Guard coordinates scheduling

**Criminal Availability**:
- Guards must locate criminal to escort
- Criminal must be conscious and accessible
- Restricted criminals (military duty, important jobs) may delay punishment

**Delays**:
- Insufficient guards available
- Criminal unreachable (trapped, isolated)
- Infrastructure occupied (all jails full)
- Scheduler conflicts with other fortress activities

**Punishment Delayed Thought**: Criminals receive +20 happy thought for each day punishment is delayed, incentivizing inefficient justice.

### Escort and Restraint

**Escort Process**:
1. Guard assigned to escort duty
2. Guard pathfinds to criminal location
3. Guard "arrests" criminal (enters escort state)
4. Criminal follows guard to destination
5. Arrival at jail/hammering location
6. Criminal restrained

**Resistance**:
- Criminals typically comply with escort
- Berserk or tantruming criminals may resist
- Guards may need to subdue violent criminals
- Combat possible during arrest

**Restraint Methods**:
- **Chains**: Criminal fastened to floor chain, can move 3x3 area
- **Ropes**: Similar to chains but less secure (break risk)
- **Cages**: Criminal locked in cage, no movement

### Beating Execution

**Combat Mechanics**:
1. Guard enters "beating job" mode
2. Guard approaches criminal
3. Guard delivers unarmed attacks
4. Attacks continue until job satisfied
5. Criminal likely injured, possibly unconscious
6. Job completes, guard returns to duties

**Targeting**: Game selects random body parts for strikes, including head (high fatality risk).

**Intervention**: Players cannot stop beating once started; must wait for completion.

**Medical Aftermath**: Injured criminals often require medical treatment post-beating.

### Imprisonment Execution

**Cell Assignment**:
1. Available restraint identified
2. Criminal escorted to assigned cell
3. Criminal fastened to restraint
4. Prisoner status begins
5. Timer counts down sentence duration

**During Imprisonment**:
- **Feeding**: Other dwarves bring food/water
- **Hygiene**: Prisoners soil themselves, creating miasma
- **Entertainment**: None unless provided by cell amenities
- **Social**: Isolated unless visitors come to cell
- **Work**: Cannot perform jobs while imprisoned

**Release**:
- Sentence timer expires
- Guard assigned to release prisoner
- Prisoner unfastened from restraint
- Returns to normal dwarf status
- Resumes regular duties

### Hammering Execution

**Setup**:
1. Criminal escorted to justice restraint
2. Criminal fastened to restraint
3. Hammerer summoned
4. Hammerer arrives with war hammer

**Execution**:
1. Hammerer stands adjacent to criminal
2. Delivers strike with war hammer
3. Repeats for specified number of strikes
4. Each strike uses weapon combat mechanics
5. Damage accumulates rapidly

**Typical Outcome**: Most criminals die before 50 strikes completed due to:
- Massive organ damage
- Severe bleeding
- Shock
- Head trauma

**Survival**: Low-skill hammerer using light hammer may allow survival, resulting in:
- Permanent injuries
- Multiple broken bones
- Long recovery period
- Psychological trauma

**Body Disposal**: Dead criminals require burial/disposal like other deaths.

## Morale Impact System

### Direct Effects on Criminal

**Punishment Received Thoughts**:
- "Received beating": -20 to -40 depending on severity
- "Imprisoned": -30 to -60 depending on duration
- "Hammered": -100+ (if survive)

**Punishment Delayed Thoughts**:
- "Glad to have punishment delayed recently": +20
- Stacks over time, incentivizing delays

**Reduced Punishment Thoughts**:
- "Received reduced punishment": +20
- Occurs when downgrading happens

**Duration Effects**:
- Imprisonment: Ongoing unhappiness throughout sentence
- Beating: Short-term pain, longer-term memory
- Hammering: Permanent traumatic memory if survive

### Effects on Officials

**Administering Justice**:
- Guards delivering beatings: +15 "satisfied a need for justice"
- Hammerer: +25 "satisfied a need for justice"
- Captain of the Guard: +10 "maintained order"

**Frequency**: Officials who regularly administer punishment become happier, creating perverse incentive for harsh justice.

**Career Satisfaction**: Dwarves assigned to justice roles derive happiness from system functioning.

### Effects on Injured Party/Victim

**Justice Served**:
- Appropriate punishment: +20 "got justice for crime"
- Harsh punishment: +30 "saw criminal suffer"

**Justice Denied**:
- No punishment: -20 "crime went unpunished"
- Insufficient punishment: -15 "justice not properly served"
- Wrong dwarf punished: -30 "innocent dwarf punished"

**Personal Relationship**: Effects stronger if victim had close relationship with criminal or injured party.

### Effects on Witnesses

**Witnessing Punishment**:
- Fair beating: Neutral to +5
- Severe beating: -10 to -20
- Death from beating: -30 to -50
- Hammering: -20 to -40
- Death from hammering: -40 to -70

**Relationship Modifiers**:
- Witnessing friend punished: Additional -20
- Witnessing enemy punished: +10
- Witnessing family member punished: Additional -40

**Stress Accumulation**: Frequent harsh punishments stress entire fortress through witness effects.

### Cascading Social Effects

**Tantrum Spiral Risk**:
1. Crime occurs
2. Harsh punishment administered
3. Witnesses become stressed
4. Stressed witnesses throw tantrums
5. Tantrums create new crimes
6. New crimes require punishment
7. Cycle escalates

**Social Network Propagation**:
- Friends of punished dwarf become unhappy
- Family members severely affected
- Enemies slightly pleased
- Neutral dwarves affected by witnessing

**Trust in Justice**:
- Fair, appropriate punishments maintain trust
- Excessive or unjust punishments erode trust
- Lost trust increases future crime likelihood

## Special Punishment Cases

### Vampire Punishment

**Standard System Inadequacy**: Vampires convicted of murder typically receive standard punishment (imprisonment or hammering).

**Survival Advantage**: Vampires' enhanced durability allows survival of punishment that would kill normal dwarves.

**Effectiveness Problems**:
- Imprisonment: Vampire doesn't need food/water, serves time easily
- Hammering: May survive 50+ strikes, returns to population
- Beating: Essentially ineffective

**Player Response**: Most players handle vampires extrajudicially:
- "Accidents" (cave-ins, drowning)
- Exile
- Military training "accidents"
- Permanent imprisonment without trial

**Moral Complication**: Vampires compelled by curse, not choosing crimes voluntarily.

### Noble Punishment

**Exemption**: Many nobles have PUNISHMENT_EXEMPTION tag, making them immune to justice system.

**When Punishable**: Lower nobles without exemption can be convicted.

**Complications**:
- Punishing nobles creates diplomatic incidents
- Other nobles become unhappy
- Civilization may react negatively
- Fortress may lose noble benefits

**Office Access**: Imprisoned nobles cannot access offices, disrupting administrative functions.

**Political Fallout**: Harsh punishment of nobles may trigger:
- Replacement by angry new noble
- Civilization sanctions
- Reduced trade/immigration
- Military "assistance" from civilization

### Child Offenders

**Exemption**: Children automatically exempt from justice system.

**Rationale**: Game treats children as not fully responsible for actions.

**Tantrums**: Children throwing tantrums don't result in criminal charges despite property damage.

### Insane and Strange Mood Dwarves

**Exemption**: Dwarves in strange moods or permanently insane exempt from justice.

**Rationale**: Not considered mentally competent to hold criminally responsible.

**Dangerous Behavior**: Berserk insane dwarves may kill others but cannot be tried for murder.

**Handling**: Military must handle violent insane dwarves rather than justice system.

## Punishment Infrastructure

### Justice Restraints vs Prison Restraints

**Justice Restraints**:
- Designated specifically for justice use
- Used for hammering
- Used for imprisonment
- Must be explicitly marked as justice

**Regular Restraints**:
- Not designated for justice
- Cannot be used for punishment
- May be used for other purposes (hospital, defense)

**Configuration**: Players must manually designate restraints for justice use through zone system.

### Jail Design Considerations

**Basic Requirements**:
- Restraint (chain/rope) or cage
- Designated as jail zone
- Accessible to guards for escort
- Accessible to haulers for food/water delivery

**Quality Improvements**:
- Adjacent food/drink stockpiles
- High-quality furniture within reach
- Engravings on walls
- Quality restraint (jewel-encrusted platinum chain)
- Wells for water access
- Temples within reach for prayer
- Dining room designation

**Psychological Benefits**: Well-designed jails reduce prisoner stress, preventing additional tantrums during imprisonment.

**Practical Benefits**: Reduced prisoner stress means:
- Less likely to go berserk
- Less strain on fortress morale
- Easier reintegration after release
- Potential for prisoner to admire surroundings

### Capacity Planning

**Formula**: Game requests approximately 1/10 of population in combined guards + restraints.

**Example**: 100-dwarf fortress should have:
- ~10 total (guards + jail spaces)
- Possible split: 5 guards, 5 jail spaces
- Or: 7 guards, 3 jail spaces

**Overflow**: If all jails full:
- Additional sentences wait for available space
- May cause punishment delays (happy thought for criminals)
- Or trigger downgrades to beatings

**Hammering Restraints**: Recommend separate restraints for hammering vs imprisonment to avoid scheduling conflicts.

## Alternative Justice Strategies

### Ignoring Justice System

**Method**: Never appoint Sheriff or Captain of the Guard.

**Consequences**:
- Crimes still occur
- No investigation or punishment
- Criminals receive "glad punishment delayed" thought (+20)
- Victims unhappy about lack of justice
- Nobles upset about mandate violations

**Net Effect**: May be positive in noble-free fortresses, as criminal happiness outweighs victim unhappiness.

**Limitations**: Cannot investigate infiltrators or villainous networks without Captain of the Guard.

### Minimal Justice

**Method**: Appoint officials but provide no jail infrastructure.

**Effect**: All sentences downgraded to beatings.

**Advantages**:
- Simple infrastructure
- Fast punishment resolution
- Criminals happy about downgrade

**Disadvantages**:
- High fatality rate from beatings
- Victims unhappy about reduced punishment
- No serious punishment for capital crimes

### Luxury Prison Strategy

**Method**: Create extremely high-quality jails with amenities.

**Goal**: Minimize prisoner stress during sentences.

**Features**:
- Legendary engraved walls
- Artifact-quality furniture within reach
- Master-quality platinum chains
- Adjacent legendary dining room
- Wells for drinking
- Temples within range
- Stockpiles of favorite foods

**Result**: Imprisonment becomes almost pleasant, minimal psychological damage.

### Deliberate Incompetence

**Method**: Assign weak, unskilled dwarves to Hammerer position.

**Goal**: Reduce fatality rate from hammering.

**Mechanism**: Low skill + weak strength = less damage per strike.

**Effect**: Criminals survive 50 strikes but suffer permanent injuries.

**Trade-off**: "Merciful" execution vs "effective" execution depending on player goals.

## RimWorld Modding Implications

### Transferable Concepts

1. **Graduated Severity**: Scale punishment with crime severity (warning → arrest → imprisonment → execution)

2. **Infrastructure Dependency**: Require prison cells for imprisonment; lacking them forces alternative punishments

3. **Morale Cascades**: Punishment affects not just criminal but witnesses, creating social ripples

4. **Official Satisfaction**: Wardens/authorities gain mood benefits from maintaining justice

5. **Victim Satisfaction**: Crime victims want justice; appropriate punishment satisfies them

6. **Punishment Delay Benefits**: Criminals prefer delayed justice, creating time pressure

7. **Quality Matters**: Prison quality affects prisoner psychology and rehabilitation

8. **Cultural Variation**: Ideology system could define punishment preferences

### Adaptation Requirements

1. **Lethality Reduction**: RimWorld players likely want non-lethal punishments; avoid DF's casual death rate

2. **Medical Integration**: Connect punishment damage to RimWorld's detailed health system

3. **Prison Labor**: RimWorld prisoners can work; integrate with justice system

4. **Recruitment Potential**: RimWorld prisoners can be recruited; punishment shouldn't prevent this

5. **Warden Skills**: Use existing Social skill for punishment effectiveness and prisoner management

### Recommended Implementations

**Punishment Hierarchy**:
1. **Warning**: Social interaction, no confinement
2. **Arrest**: Temporary restraint (hours)
3. **Imprisonment**: Days to weeks in prison cell
4. **Corporal Punishment**: Non-lethal beating (damage but recover)
5. **Execution**: Permanent removal (severe crimes only)

**Infrastructure System**:
- Require prison cells (room with sleeping spot + door)
- Quality affects prisoner mood
- Capacity limits simultaneous prisoners
- Lacking space forces alternative punishments

**Morale Integration**:
- Use existing mood system for punishment effects
- Victims: +mood if justice served, -mood if not
- Witnesses: -mood for harsh/unjust punishment
- Wardens: +mood for maintaining order
- Criminals: -mood from punishment

**Cultural Variation via Ideology**:
- Pain is virtue → prefer corporal punishment
- Guilty → harsh imprisonment + hard labor
- Body purist → no executions
- Collectivist → public humiliation rituals

**Quality Prison Benefits**:
- Better furniture → less prisoner stress
- Recreational items → reduced mental break chance
- Social interaction → better rehabilitation
- Work opportunities → skill development

**Skill-Based Execution**:
- Warden social skill affects:
  - Prisoner compliance
  - Reformation likelihood
  - Punishment effectiveness
  - Escape prevention
