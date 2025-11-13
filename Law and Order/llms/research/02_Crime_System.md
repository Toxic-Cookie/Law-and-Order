# Dwarf Fortress Crime System

## Overview

The crime system in Dwarf Fortress encompasses a wide variety of criminal behaviors, from minor infractions like violating noble mandates to serious crimes like murder. Crimes have multiple causes including stress, supernatural influences, external infiltration, and simple disobedience.

## Crime Categories

### Administrative Crimes (Noble Mandates)

These crimes relate to defying noble orders and requirements.

#### Violation of Production Order

**Definition**: Failing to produce items mandated by a noble within the deadline.

**How It Occurs**:
1. Noble issues production mandate (e.g., "produce 3 crowns")
2. Mandate has approximately 6-month deadline
3. If not fulfilled by deadline, crime is triggered
4. Noble becomes unhappy
5. One or more dwarves automatically convicted

**Target Selection**:
- Noble prefers punishing dwarves whose profession relates to mandate
- If no relevant professional exists, random citizen selected
- Multiple dwarves may be punished for single mandate failure

**Important Notes**:
- Obtaining items from caravans does NOT fulfill mandates
- Items must be produced within fortress after mandate issued
- Even if items exist in fortress, they must be newly crafted post-mandate

**Punishment**: Typically beating or 1 month imprisonment (PUNISH_SERIOUS)

#### Violation of Export Prohibition

**Definition**: Selling items to caravans that a noble has forbidden for export.

**How It Occurs**:
1. Noble issues export ban (e.g., "no exporting steel items")
2. Ban lasts approximately 6 months
3. Player trades banned item to caravan
4. Each hauler who brought banned item to trade depot is convicted
5. Conviction triggers the instant item leaves map edge

**Critical Detail**: The criminal is NOT the trader who authorized the sale, but the hauler(s) who transported the item to the depot.

**Avoiding the Crime**:
- Don't trade banned items
- Melting down banned items is legal (does not violate restriction)
- Remove banned items from trade depot before caravan departs

**Multiple Convictions**: If 5 different haulers brought banned items, all 5 are convicted separately.

**Punishment**: Typically beating or 1 month imprisonment (PUNISH_SERIOUS)

**Known Bug**: Mandates remain in effect even after the noble who issued them is replaced, potentially creating confusion about active restrictions.

#### Violation of Job Order

**Definition**: Failing to complete guild-mandated jobs.

**Context**: Similar to production mandates but issued by guild representatives rather than fortress nobles.

**Punishment**: Beating or 1 month imprisonment (PUNISH_SERIOUS)

### Property Crimes

#### Theft

**Definition**: Stealing items, particularly artifacts.

**Typical Scenario - Artifact Theft Plot**:
1. Villainous organization identifies fortress artifact
2. Agent visits fortress under false identity
3. Agent corrupts fortress resident through social manipulation
4. Corrupted dwarf steals artifact
5. Corrupted dwarf gives artifact to agent
6. Agent leaves fortress with artifact

**Detection**:
- Witnesses may see the theft occur
- More commonly, witnesses notice artifact is missing
- If no witnesses saw theft itself, case shows "theft occurred" with witnesses who discovered it missing

**Challenge**: Often difficult to identify culprit without direct witnesses. May require interrogation of all recent visitors and residents.

**Punishment**: 8 months imprisonment or 50 hammer strikes (PUNISH_CAPITAL)

#### Robbery

**Definition**: Theft accompanied by force or threat.

**Less Common**: Rarely occurs in fortress mode; more common in adventure mode.

**Punishment**: 8 months imprisonment or 50 hammer strikes (PUNISH_CAPITAL)

#### Vandalism

**Definition**: Toppling furniture during tantrum or mental breakdown.

**Typical Scenario**:
1. Dwarf becomes severely stressed
2. Stress triggers tantrum (personality: high anger propensity)
3. Dwarf cancels current job
4. Dwarf topples furniture, throws items
5. Witnesses report vandalism

**Psychological Trigger**: Anxious personality + extreme stress = tantrum behavior

**Complication**: Punishing stressed dwarf may worsen stress, potentially triggering tantrum spiral across fortress.

**Punishment**: Beating or 1 month imprisonment (PUNISH_SERIOUS)

#### Building Destruction

**Definition**: Destroying buildings during tantrum.

**How It Occurs**: Similar to vandalism but targets constructed buildings (workshops, doors, bridges) rather than furniture.

**Impact**: More serious than vandalism due to higher replacement cost and fortress functionality disruption.

**Punishment**: 8 months imprisonment or 50 hammer strikes depending on ethics

#### Embezzlement

**Definition**: Position holder stealing from organization they're part of.

**Context**: Typically part of villain plots where corrupted officials steal resources.

**Punishment**: 8 months imprisonment or severe corporal punishment

### Violent Crimes

#### Murder

**Definition**: Killing another dwarf or tame animal.

**Common Scenarios**:

**Vampire Murder**:
1. Vampire feeds on sleeping dwarf
2. Victim dies from blood loss (or survives but crime still considered murder)
3. Vampire makes false report blaming innocent dwarf
4. Player must use interrogation or circumstantial evidence to identify real culprit

**Tantrum Murder**:
1. Enraged dwarf attacks another during tantrum
2. Victim dies from injuries
3. Witnesses report murder
4. Attacker convicted

**Premeditated Murder**:
1. Villain-corrupted dwarf assassinates target
2. Part of larger intrigue plot
3. May be directed against fortress leadership

**Detection Challenges**:
- Vampires create false witnesses
- No witnesses = unsolved murder
- Body may be hidden or disposed of
- Circumstantial evidence needed

**Player Conviction Required**: Unlike mandate violations, player must manually convict murderers.

**Punishment**: 8 months imprisonment or 50 hammer strikes (PUNISH_CAPITAL)

**Special Case**: Even if vampire's victim survives the blood-drinking, it's still considered murder if witnessed.

#### Attempted Murder

**Definition**: Attempting to kill another dwarf but failing.

**Less Common**: Typically only applies when clear intent to kill is established but victim survives.

**Punishment**: 8 months imprisonment or severe corporal punishment

#### Disorderly Conduct

**Definition**: Attacking another dwarf during tantrum without killing them.

**Typical Scenario**:
1. Stressed dwarf throws tantrum
2. Starts fist fight with another dwarf
3. Punches thrown but victim survives
4. Witnesses report disorderly conduct

**Less Severe Than Murder**: Since victim survives, punishment is lighter.

**Punishment**: Beating or 1 month imprisonment (PUNISH_SERIOUS)

### Conspiracy and Intrigue Crimes

#### Espionage

**Definition**: Agent infiltrating fortress as part of larger plot.

**How It's Discovered**:
1. Captain of the Guard successfully interrogates visitor
2. Visitor confesses membership in villainous organization
3. Espionage charge filed
4. Additional interrogations may reveal co-conspirators

**Critical Detail**: Espionage charge appears AFTER successful interrogation, not from witness observation.

**What Spies Do**:
- Gather intelligence on fortress artifacts
- Identify corruption targets
- Recruit fortress residents into criminal organizations
- Coordinate with corrupted residents for plots

**Detection Method**: Routine interrogation of all new arrivals is primary detection method.

**Punishment**: 8 months imprisonment or 50 hammer strikes (PUNISH_CAPITAL)

#### Conspiracy to Slow Labor

**Definition**: Deliberately coordinating to reduce fortress productivity.

**Context**: Part of sabotage plots by villainous networks.

**Detection**: Requires interrogation to uncover coordinated effort.

**Punishment**: 8 months imprisonment or corporal punishment

#### Treason

**Definition**: Betraying fortress/civilization to enemies.

**Scenarios**:
- Corrupted official sharing fortress secrets
- Opening gates to invaders
- Sabotaging fortress defenses

**Most Serious Crime**: Considered ultimate betrayal.

**Punishment**: 50 hammer strikes or permanent imprisonment (PUNISH_CAPITAL)

#### Bribery

**Definition**: Offering illicit payments to officials for favors.

**Context**: Part of corruption mechanics where villains bribe officials to:
- Overlook crimes
- Provide information
- Assist in plots

**Punishment**: 8 months imprisonment or corporal punishment

### Supernatural Crimes

#### Blood-Drinking (Vampire Crime)

**Definition**: Vampire feeding on another dwarf.

**How It Occurs**:
1. Vampire identifies isolated sleeping dwarf
2. Vampire drains blood from victim
3. If witnessed mid-feeding, reported as murder
4. Vampire receives large happiness boost
5. Victim (if alive) has no memory of attack

**Detection Indicators**:
- Drained corpse found (if victim dies)
- Pale, weak dwarf with blood loss (if victim survives)
- Witness catches vampire in act

**Vampire Deception**: Vampires typically file false reports to frame others for their murders.

**Identifying Vampires Without Witnesses**:
- Unusually high number of skills (10-15+ at Novice or better)
- Abnormally long lists of relations (dozens of historical connections)
- Dozens of group associations spanning centuries
- Never have thoughts about food, drink, sleep, or furniture
- Observation during combat reveals true nature (unit marked as "Urist McVampire, Vampire")

**Justice System Inadequacy**: Even when convicted, vampires typically receive normal punishment (imprisonment) rather than execution, forcing players to handle them extrajudicially.

**Punishment**: 8 months imprisonment or 50 hammer strikes (though often insufficient to kill vampire)

## Crime Motivations and Triggers

### Stress-Induced Crime (Tantrums)

**Trigger Mechanism**:
1. Dwarf accumulates stress from negative experiences:
   - Witnessing death
   - Working in terrible conditions
   - Lack of food/alcohol/sleep
   - Loss of loved ones
   - Masterwork destroyed
2. Stress exceeds threshold based on personality
3. Mental breakdown occurs
4. Behavior depends on personality type:
   - **High Anger Propensity** → Tantrum (criminal behavior)
   - **High Anxiety** → Oblivious stumbling (non-criminal)
   - **High Depression** → Melancholic moping (non-criminal)

**Tantrum Behaviors**:
- Throwing items around
- Starting fist fights (disorderly conduct)
- Toppling furniture (vandalism)
- Destroying buildings (building destruction)
- Attacking pets or other dwarves (murder/assault)

**Tantrum Spiral Risk**:
1. Stressed dwarf commits crime during tantrum
2. Crime requires punishment
3. Other dwarves witness harsh punishment
4. Witnesses become stressed
5. Stressed witnesses throw tantrums
6. Cycle repeats, cascading through fortress

**Prevention**: Managing stress is more effective than punishing tantrum-related crimes after the fact.

### Supernatural Compulsion (Vampirism)

**Not Voluntary**: Vampires must feed on blood to survive. This is a compulsion, not a choice.

**Curse Origin**: Vampires were cursed during world generation for profaning against deities. They don't choose vampirism.

**Behavior Pattern**:
- Vampire identifies vulnerable target (sleeping, isolated)
- Feeds when opportunity arises
- Cannot control feeding urge
- Receives happiness boost from feeding (reinforcing behavior)
- Will not stop drinking even if caught in the act

**Long-Term Consequences**: Vampires that remain naked for years (rotting clothes) become prone to tantrums and insanity, potentially causing additional crimes beyond blood-drinking.

### External Manipulation (Corruption)

**Villain-Directed Crime**: Corrupted dwarves commit crimes at direction of villainous organizations.

**Corruption Process**:
1. Agent establishes contact with fortress resident
2. Agent uses social manipulation techniques:
   - Intimidation and asserting rank
   - Blackmail (if agent knows secrets)
   - Flattery and exploitation of ego
   - Religious sympathies exploitation
   - Promises of revenge against enemies
   - Direct bribery with treasure, artifacts, or positions
3. Target's resistance vs agent's intrigue skill determines success
4. If successful, target joins villainous organization
5. Corrupted dwarf receives instructions to commit crimes

**Crimes Directed by Villains**:
- Artifact theft
- Sabotage of fortress operations
- Assassination of key personnel
- Opening fortress to attacks
- Spreading false information

**Key Insight**: These crimes are not organic to the dwarf's personality or situation, but externally directed by villain networks.

### Disobedience and Negligence

**Production Mandate Violations**: Not necessarily deliberate crime. Often result from:
- Player not noticing mandate
- Lack of materials to fulfill mandate
- Lack of skilled craftsdwarf in relevant profession
- Workshop unavailable or occupied

**Export Violations**: Usually player error rather than dwarf crime. Player authorizes trade of banned item, innocent haulers convicted.

**Systemic Injustice**: Many "criminals" are victims of circumstances beyond their control.

## Witness Mechanics

### Direct Observation

**Line of Sight Requirement**: Witness must have clear view of crime as it occurs.

**Darkness Limitation**: Crimes in dark areas may go unwitnessed even if dwarves are nearby.

**What Witnesses See**:
- Criminal identity
- Crime type
- Location and time
- Victim (if applicable)

**Automatic Reporting**: Witnesses automatically report to Sheriff/Captain of the Guard without player input.

### Indirect Discovery

**Finding Evidence**: Dwarf discovers aftermath of crime without seeing it occur:
- Finding corpse (murder)
- Noticing missing artifact (theft)
- Seeing destroyed furniture (vandalism)

**Limited Information**: Indirect witnesses know crime occurred but not necessarily who committed it.

**Investigation Required**: Cases with only indirect witnesses require interrogation to identify suspects.

### False Witness Testimony

**Grudge-Based False Accusations**:
- Dwarf with personal grudge against another
- Falsely accuses enemy of crime
- Files report to frame innocent dwarf
- Justice system may wrongly convict based on false testimony

**Vampire Frame Jobs**:
- Vampire commits murder
- Files false witness report
- Accuses innocent dwarf
- Creates confusion and misdirection

**Impossible Accusations**:
System bugs or oversights may result in accusations against:
- Dead dwarves
- Creatures that weren't present
- Tame animals
- Physically impossible suspects

**Player Dilemma**: How much to trust witness testimony when false accusations are possible?

### Social Dynamics of Witnessing

**Relationships Affect Reporting**: Witnesses may be more likely to report crimes against friends and less likely to report crimes against enemies.

**Stress Affects Perception**: Highly stressed dwarves may misremember or misreport what they witnessed.

**Multiple Witnesses**: Cases with multiple witnesses are more reliable than single-witness cases, though still not infallible.

## Evidence and Investigation

### Types of Evidence

**Witness Testimony**: Primary evidence type. Reliability varies based on:
- Number of witnesses
- Relationship between witness and accused
- Circumstantial vs direct observation

**Confessions**: Obtained through interrogation. Most reliable evidence when genuine.

**Circumstantial Evidence**: Indicators requiring player interpretation:
- Vampire: Skill patterns, lack of food/drink thoughts, excessive historical connections
- Spy: False name, suspicious behavior, presence during crimes
- Stressed dwarf: Recent negative thoughts, personality traits

**Physical Evidence**: Limited in game mechanics:
- Corpses indicate murder occurred
- Missing items indicate theft
- Destroyed buildings indicate vandalism

### Investigation Process

**Reviewing Cases**:
1. Access Justice screen (Z menu)
2. Examine open cases
3. Note witnesses and accused
4. Check case details (victim, date, type)

**Identifying Suspects**:
- All creatures in and around fortress appear in interrogation list
- List includes dead creatures, visitors, residents
- No search function or organization
- Player must manually identify relevant suspects

**Conducting Interrogations**:
1. Select suspect from list
2. Captain of the Guard escorts suspect to office
3. Interrogation occurs (skill check behind scenes)
4. Results appear in report
5. Confession or organization membership revealed (if successful)

**Cross-Referencing Information**:
- Compare confessions from multiple suspects
- Identify organization members through network analysis
- Track timeline of visitor arrivals vs crime occurrences
- Correlate interrogation results with witness testimony

### Investigation Challenges

**Time Investment**: Interrogations take significant time, pulling Captain and suspect away from other duties.

**Skill Dependency**: Investigation effectiveness depends heavily on Captain's social skills. Low-skill Captain gets few confessions.

**Information Overload**: Large fortresses with many visitors create overwhelming interrogation lists.

**False Confessions**: Possible (though rare) for innocent dwarves to falsely confess under interrogation pressure.

**Corruption Risk**: Extended interrogation of skilled infiltrator may corrupt Captain, compromising entire investigation.

**No Forensics**: Game lacks CSI-style physical evidence analysis. Investigation relies on social interaction and testimony.

## Crime Impact on Fortress

### Economic Impact

**Labor Loss**: Criminals in prison don't work, creating labor shortages for:
- Key craftsdwarves
- Haulers
- Essential services

**Destroyed Infrastructure**: Vandalism and building destruction require:
- Materials for replacement
- Labor for reconstruction
- Time for repairs

**Stolen Items**: Particularly artifacts, which provide:
- Happy thoughts to admirers
- Fortress wealth
- Historical significance

### Social Impact

**Morale Effects**:
- Victims/injured parties want justice (unhappy if not delivered)
- Criminals receive unhappy thoughts from punishment
- Witnesses to harsh punishment become stressed
- Unfair convictions affront the populace
- Justice delayed creates happiness (+20 thought)

**Trust Erosion**: Frequent false accusations undermine faith in justice system.

**Social Network Damage**: Punishing popular dwarves creates widespread unhappiness.

**Tantrum Spiral Risk**: Harsh justice + crime + stress = potential fortress collapse.

### Security Impact

**Corruption Penetration**: Each corrupted dwarf is:
- Potential spy reporting to enemies
- Asset for future plots
- Security vulnerability
- Social infection vector

**Justice System Compromise**: Corrupted Captain of the Guard:
- Protects infiltrators
- Provides false intelligence
- Undermines investigations
- Enables criminal networks

**Defensive Weaknesses**: Sabotage plots may:
- Damage defenses
- Create vulnerabilities for attacks
- Coordinate with external sieges

## RimWorld Modding Implications

### Direct Adaptations

1. **Stress-Crime Link**: Connect pawn mental breaks to criminal behavior, making crime partly a mental health management issue

2. **Witness System**: Implement line-of-sight crime detection with false accusation possibilities based on social relationships

3. **Corruption Mechanics**: Allow hostile factions to turn colonists into sleeper agents through social manipulation

4. **Investigation Mini-Game**: Create interrogation system where warden social skills determine success in extracting confessions

5. **Evidence Types**: Combine witness testimony, confessions, and circumstantial evidence for investigation gameplay

6. **Artifact Theft Plots**: Multi-stage infiltration operations where agents corrupt colonists to steal valuables

### Adaptation Considerations

1. **Scale Differences**: DF fortresses have 100+ dwarves; RimWorld colonies have 5-20 pawns. Each crime has proportionally larger impact in RimWorld.

2. **Player Attachment**: RimWorld players more attached to individual pawns, may resist false accusation/unjust punishment mechanics.

3. **Pawn Disposability**: DF players accept dwarf deaths more readily. RimWorld implementation may need non-lethal focus.

4. **Complexity Level**: DF accepts UI complexity. RimWorld needs streamlined investigation interface.

5. **Automation Balance**: Too much automation removes player agency; too little creates micromanagement burden.

### Recommended Implementations

1. **Tiered Crime System**: Minor (mandate-style), moderate (property), serious (violence), capital (murder/treason)

2. **Witness Requirements**: Crimes only detected if observed by pawn with line of sight, with skill checks for accuracy

3. **Social Manipulation**: Use RimWorld's existing social system for corruption attempts, with relationship modifiers affecting success

4. **Investigation UI**: Simple suspect list with filters, interrogation button, results in clear report format

5. **Mental Break Integration**: Tie tantrum crimes to existing mental break system, making them stress-management issue

6. **Morale Cascades**: Punishment severity affects colony mood through existing mood system, creating feedback loops

7. **Cultural Variation**: Use Ideology system to define different crime severity ratings and punishment preferences across ideologies
