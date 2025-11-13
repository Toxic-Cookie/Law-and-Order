# Dwarf Fortress Prison System

## Overview

The prison system in Dwarf Fortress manages the physical confinement of convicted criminals through restraints and cages. Prison design, restraint type, and amenities significantly impact prisoner psychology, fortress logistics, and justice system effectiveness.

## Prison Construction Basics

### Designation Process

**Creating a Jail**:
1. Build restraint (chain/rope) or cage
2. Query the restraint/cage (q menu)
3. Designate as room
4. Set room for "Justice" use
5. Optionally expand room boundaries to include multiple restraints

**Zone Types**:
- **Jail**: Used for standard imprisonment sentences
- **Dungeon**: Historically used term; functionally identical to jail in current version
- System treats both designations identically

**Multiple Restraints**:
- Single jail zone can contain multiple chains/cages
- Allows efficient use of space
- Prisoners assigned to available restraint within zone
- Shared zone benefits (quality, amenities) apply to all restraints

### Infrastructure Requirements

**Minimum Requirements**:
- At least one restraint or cage
- Accessible path for guards to escort prisoners
- Accessible path for haulers to deliver food/water
- Designated as justice facility

**Recommended Additions**:
- Food stockpile adjacent to restraints
- Drink stockpile adjacent to restraints
- Well within prisoner reach
- Furniture within restraint range
- Quality floor and wall materials
- Engravings on walls

**Capacity Planning**:
Game requests approximately 1/10 of fortress population in combined guards and restraints:
- 50 dwarves → 5 jail spaces + guards
- 100 dwarves → 10 jail spaces + guards
- 200 dwarves → 20 jail spaces + guards

## Restraint Types

### Chains

**Construction**:
- Materials: Metal (iron, steel, copper, silver, gold, platinum)
- Can also be made from bone, wood
- Quality levels: standard, well-crafted, fine, superior, exceptional, masterwork
- Value depends on material and quality

**Movement Range**:
- Prisoner can move in 3x3 area centered on chain anchor
- 9 tiles of movement total
- Can reach items/furniture on any of these 9 tiles
- Cannot move beyond this range

**Interaction Radius**:
- Can access adjacent stockpiles for food/drink
- Can use furniture within range (beds, chairs, tables)
- Can admire engravings on walls within view
- Can drink from wells within reach
- Can interact with other dwarves who enter range

**Advantages**:
- Allows prisoner mobility and agency
- Prisoner can access amenities independently
- Can eat/drink from stockpiles without hauler assistance
- More "humane" as prisoner has some freedom
- Prisoner can admire high-quality chain itself

**Disadvantages**:
- Prisoners can destroy furniture within reach during tantrums
- Requires careful placement for optimal amenity access
- More escape risk than cages (though rare)

**Security**:
- Metal chains: Very secure, rarely broken
- Rope chains: Less secure, can break during tantrums
- Tantrumming dwarves may break free from ropes

**Aesthetic Value**:
Prisoners can admire their own restraints, making high-quality chains beneficial:
- "Urist admired a masterwork platinum chain lately" (+10)
- Jewel-encrusted chains provide additional admiration value
- Material matters: Platinum > Gold > Steel > Iron

### Ropes

**Construction**:
- Made from cloth/thread materials
- Cheaper and faster to produce than metal chains
- Lower material value

**Functionality**:
- Identical 3x3 movement range to chains
- Same interaction capabilities

**Critical Weakness**:
- Significantly less secure than metal chains
- Prisoners observed breaking free from ropes during tantrums
- Break rate increases with prisoner strength and emotional state

**Use Cases**:
- Emergency prisons when metal unavailable
- Temporary holding until better restraints available
- Low-security prisoners unlikely to tantrum

**Recommendation**: Prefer metal chains over ropes for any serious imprisonment.

### Cages

**Construction**:
- Materials: Metal or wood
- Built as furniture, then placed
- Can be pre-constructed and installed quickly

**Confinement Type**:
- Complete enclosure, no movement
- Prisoner forced to sleep on cage floor
- Cannot reach outside cage for anything

**Feeding Requirements**:
- Prisoner cannot access food/drink independently
- Must be fed by haulers manually
- Requires ongoing dwarf labor for prisoner survival
- Risk of starvation/dehydration if haulers busy

**Advantages**:
- Maximum security, no escape possible
- Compact space usage
- Portable (can move caged prisoners)
- Fast to deploy

**Disadvantages**:
- Requires constant feeding attention
- Prisoner has no agency or comfort
- Cannot access amenities or furniture
- More psychologically damaging
- Higher unhappiness from confinement

**Comparison to Chains**:
- Cages: Maximum security, high maintenance
- Chains: Moderate security, low maintenance
- Most players prefer chains for standard imprisonment

## Prison Design Strategies

### Basic Functional Prison

**Layout**:
```
Wall Wall Wall Wall Wall
Wall Chain Food Well Wall
Wall Chain Food Well Wall
Wall Door Floor Floor Wall
Wall Wall Wall Wall Wall
```

**Components**:
- Chains with 3x3 range
- Adjacent food stockpiles (accept prepared meals)
- Wells within reach
- Door for guard access

**Advantages**:
- Functional and efficient
- Low construction cost
- Easy to expand
- Minimal maintenance

**Limitations**:
- No comfort amenities
- Prisoners still unhappy
- No quality bonuses

### Luxury Prison Design

**Philosophy**: Minimize prisoner suffering through high-quality environment.

**Components**:
- High-value metal chains (platinum, gold)
- Masterwork or artifact-quality restraints
- Legendary engraved walls (every surface)
- High-quality furniture within reach:
  - Beds (prisoners can sleep in beds instead of floor)
  - Chairs (for sitting comfort)
  - Tables (for eating meals properly)
  - Cabinets (for storage)
- Adjacent stockpiles of favorite foods
- Wells for water
- Temple zones within reach (for prayer)
- Designated dining room (quality meals bonus)
- Smooth stone or valuable material floors

**Psychological Benefits**:
- Numerous happy thoughts from admiring surroundings
- Comfort from quality furniture
- Satisfaction from proper dining
- Spiritual fulfillment from temple access
- Offset imprisonment unhappiness

**Result**: Net neutral or even positive mood during imprisonment.

**Use Cases**:
- Valuable dwarves you want to rehabilitate
- Reducing tantrum spiral risk
- Maintaining fortress morale
- Storing problematic nobles

### Mass Incarceration Design

**Purpose**: Handle many simultaneous prisoners efficiently.

**Layout**:
- Long hallway with chains on both sides
- Central food/drink stockpiles down middle
- Wells every 10 tiles
- Shared amenities accessible to multiple restraints

**Efficiency**:
- Single hauler can service many prisoners
- Shared resources reduce construction cost
- Guards can escort to nearest available restraint
- Scalable design

**Drawbacks**:
- Prisoners can see each other's suffering (negative thoughts)
- Less individual customization
- Crowded feeling may reduce quality bonus

### Specialized Holding Cells

**Noble Prisons**:
- Extremely high quality to satisfy noble room requirements
- Offices furniture within reach so noble can work
- Meeting areas for liaison visits
- Prevents noble tantrums from inadequate quarters

**Vampire Prisons**:
- No food/drink stockpiles needed (vampires don't eat)
- Maximum security (vampires are dangerous)
- Isolated from other dwarves (prevent feeding)
- Consider permanent imprisonment vs rehabilitation

**Tantrum-Proof Cells**:
- No furniture to destroy
- Smooth stone only (can't damage walls)
- Food/drink outside 3x3 range, fed manually
- Prevents prisoner from causing damage during breakdowns

## Prisoner Experience

### Daily Life

**Routine**:
- Wake naturally (no jobs to interrupt sleep)
- Attempt to satisfy needs within available range:
  - Eating from stockpiles or wait for meals
  - Drinking from stockpiles or wells
  - Wandering within 3x3 chain range
  - Admiring available objects
  - Praying if temple accessible
  - Sleeping when tired

**Work Status**:
- Cannot perform any jobs while imprisoned
- Labor calculations don't include prisoners
- Skills remain unchanged (no skill rust, no practice)
- Cannot attend meetings or social events

**Social Interaction**:
- Other dwarves may visit prisoner
- Conversations possible if visitors enter range
- Relationships maintained through visits
- Social needs partially satisfied

**Hygiene**:
- Prisoners urinate/defecate where restrained
- Creates miasma if not properly managed
- No cleaning themselves
- Requires cleaning by other dwarves (if designated)

### Psychological Effects

**Negative Thoughts**:
- "Confined in jail recently": -30 to -60 based on duration
- "Suffered punishment": -20 to -40
- "Unable to practice skills": -5
- "Unable to attend party": -10
- "Made acquaintance with prison floor": -5 (if no bed)
- Accumulating stress from isolation

**Positive Thoughts (if available)**:
- "Admired [quality object] lately": +5 to +20
- "Ate in a legendary dining room": +15
- "Drank fine wine lately": +10
- "Prayed to [deity] at temple": +10
- "Had wonderful conversation": +5

**Mental Break Risk**:
- Extended imprisonment + negative environment = high stress
- May trigger additional tantrums while imprisoned
- Berserk prisoners may break chains/destroy furniture
- Insanity possible from extended isolation

### Health Concerns

**Starvation Risk**:
- Caged prisoners depend entirely on haulers
- If haulers busy/dead, prisoners starve
- Chained prisoners can access stockpiles (self-sufficient)

**Dehydration Risk**:
- Critical concern for all prisoners
- Wells within reach essential for long sentences
- Caged prisoners at highest risk

**Injury Recovery**:
- Prisoners injured during beatings/hammering
- May require medical attention during sentence
- Medical dwarves should have access to treat prisoners
- Hospital not accessible; prisoners treated in cells

**Exercise and Atrophy**:
- No mechanical effects in game
- Prisoners emerge in same physical condition

## Prison Management

### Guard Duties

**Escort to Prison**:
1. Guard assigned to escort job
2. Travels to criminal's location
3. "Arrests" criminal (enters escort state)
4. Leads criminal to assigned restraint
5. Fastens criminal to restraint
6. Returns to other duties

**Escort from Prison**:
1. Sentence expires
2. Guard assigned to release job
3. Travels to prison
4. Unfastens prisoner from restraint
5. Prisoner released to normal status

**Supervision**:
- No ongoing guard presence required
- Prisoners largely self-managing if chained with amenities
- Guards only involved during escort

**Problem Prisoners**:
- Berserk prisoners may require military response
- Guards may need to subdue violent prisoners
- Medical attention arranged for injured prisoners

### Hauler Responsibilities

**Food Delivery**:
- Prepare meals in kitchen
- Haul to prison food stockpiles
- Or directly feed caged prisoners
- Ongoing duty throughout sentence

**Water Delivery**:
- Haul drinks to prison drink stockpiles
- Or bring water to caged prisoners
- Critical for prisoner survival

**Maintenance**:
- Clean miasma sources if designated
- Remove refuse from cells
- Haul destroyed furniture out
- Replace broken amenities

**Workload Impact**:
Multiple long-term prisoners create significant hauling burden.

### Administrative Tracking

**Justice Screen**:
- Shows currently imprisoned dwarves
- Displays sentence remaining
- Tracks which restraint occupied
- Can manually release if desired (overriding sentence)

**Restraint Status**:
- Query restraint to see occupant
- Shows prisoner name and crime
- Displays sentence duration
- Can manually release from this interface

**Capacity Management**:
- Track available vs occupied restraints
- Plan expansions when near capacity
- Identify restraints needing maintenance

## Special Cases

### Noble Imprisonment

**Complications**:
- Nobles lose access to offices (mandates cease)
- Room requirements still apply (quality matters more)
- Other nobles may object to imprisonment
- Potential diplomatic consequences

**Office Access Workaround**:
- Place office furniture within chain reach
- Noble can continue administrative work
- Mandates can still be issued
- Meetings with liaisons possible if meeting zone within reach

**Luxury Requirement**:
- Noble prisons must be extremely high quality
- Failure to meet standards = additional noble tantrums
- May need legendary engraved walls, artifact furniture

### Vampire Imprisonment

**Unique Considerations**:
- Don't require food or water (no need for stockpiles/wells)
- Don't sleep (save space, no beds needed)
- Extremely durable (survive long sentences easily)
- Dangerous if escape (will feed on others)

**Isolation Concerns**:
- Vampires dangerous to other prisoners
- Keep separate from general prison population
- Prevent access to any other dwarves
- Consider permanent imprisonment

**Long-Term Effects**:
- Vampires with rotting clothes become stressed
- Stressed vampires may tantrum
- Tantrumming vampires may break restraints
- Provide clothing access to prevent clothing stress

### Insane Prisoner Management

**Berserk Prisoners**:
- Extreme danger to others
- May break furniture and restraints
- Require maximum security (cages preferred)
- Military may need to suppress

**Melancholic Prisoners**:
- Minimal management needs
- Won't eat or drink (will die)
- Inevitable death during sentence
- Becomes more of a hospice than prison

**Stark Raving Mad**:
- Unpredictable behavior
- Alternates between calm and violent
- Difficult to manage safely
- May need isolation from other prisoners

## Release and Reintegration

### Sentence Completion

**Automatic Release**:
- Timer expires
- Guard assigned to release duty
- Prisoner unfastened from restraint
- Returns to normal dwarf status immediately

**Manual Release**:
- Player can release prisoners early via justice screen
- Sentence not served = justice not satisfied
- May cause "punishment delayed" or "insufficient justice" thoughts

### Post-Release Effects

**Criminal's State**:
- Immediately returns to job duties
- May still have punishment-related unhappy thoughts
- Possible lingering stress from imprisonment
- Relationships may have degraded during absence

**Social Reintegration**:
- Other dwarves remember imprisonment
- Some may avoid or judge ex-prisoner
- Friends/family happy to see prisoner released
- Enemies may be disappointed

**Psychological Recovery**:
- Unhappy thoughts from imprisonment fade over time
- Stress may persist if imprisonment was harsh
- Positive experiences post-release help recovery
- Some dwarves never fully recover from trauma

### Recidivism

**Repeat Offenders**:
- Dwarves with certain personalities more likely to re-offend
- High anger propensity → repeated tantrum crimes
- Vampires → will feed again eventually
- Corrupted dwarves → continue serving villain organizations

**Escalation**:
- Game doesn't track criminal history for sentencing
- Each crime sentenced independently
- No "three strikes" system
- Repeat murders treated same as first murder

## Strategic Considerations

### Investment vs Return

**High-Quality Prison Benefits**:
- Reduced prisoner stress → fewer mid-sentence tantrums
- Happier reintegration → better productivity post-release
- Less fortress morale impact from witnessing suffering
- Demonstrates fortress values (mercy, quality of life)

**Costs**:
- Expensive materials (platinum chains, masterwork furniture)
- Labor for engraving and construction
- Opportunity cost of skilled craftsdwarves' time

**ROI Analysis**:
- Luxury prisons economical for valuable dwarves (legendary craftsdwarves, essential roles)
- Basic prisons sufficient for common criminals
- Minimal prisons acceptable for terminal cases (vampires, permanent insanity)

### Location Planning

**Central vs Remote**:

**Central Location**:
- Advantages: Easy guard/hauler access, integrated with fortress
- Disadvantages: Negative thoughts from proximity, noise

**Remote Location**:
- Advantages: Isolated suffering, reduced morale impact
- Disadvantages: Long escort times, difficult hauler access

**Underground vs Surface**:
- Underground: Traditional dwarven, easier security
- Surface: Natural light (no mechanical benefit), easier well access

**Proximity Considerations**:
- Near guard posts → faster response
- Near kitchens → easier food delivery
- Near meeting areas → nobles can still function
- Away from bedrooms → less disturbance

### Scalability Planning

**Modular Design**:
- Build initial section (5-10 cells)
- Design allows easy expansion
- Add sections as population grows
- Maintain consistent design for efficiency

**Overflow Management**:
- Plan for capacity spikes (multiple crimes simultaneously)
- Emergency cage storage if permanent prison full
- Downgrade options if insufficient space

**Future-Proofing**:
- Build in high-traffic areas anticipating expansion
- Reserve adjacent space for additional wings
- Plan water access for expanded sections

## RimWorld Modding Implications

### Direct Adaptations

1. **Room Quality System**: RimWorld already has room quality mechanics; directly applicable to prison cells

2. **Prisoner Needs**: RimWorld pawns have needs (food, recreation, comfort); prison amenities address these

3. **Furniture Benefits**: RimWorld furniture provides comfort/beauty; same principle for prison quality

4. **Recreation Access**: Provide recreation items within cells to reduce mental break risk

5. **Social Visits**: Allow colonists to visit prisoners, reducing isolation effects

6. **Restraint Types**: Implement different security levels (low/medium/high security cells)

### Adaptation Enhancements

**RimWorld-Specific Features**:

1. **Prisoner Labor**: RimWorld prisoners can work; integrate with justice system
   - Work reduces sentence duration
   - Labor therapy improves mood
   - Skills developed during imprisonment

2. **Medical Integration**: RimWorld's detailed health system
   - Treat injuries from corporal punishment
   - Provide medical care during imprisonment
   - Mental health treatment as part of rehabilitation

3. **Recreation Types**: Leverage RimWorld recreation variety
   - Books/TV in cells
   - Exercise equipment
   - Musical instruments
   - Art creation

4. **Temperature Control**: RimWorld environmental management
   - Comfortable temperature important for prisoner mood
   - Extreme temperatures as harsh punishment option
   - Climate control as luxury amenity

5. **Drug Management**: RimWorld's drug system
   - Medication for mental health
   - Drug policy for prisoners
   - Withdrawal management during imprisonment

### Recommended Implementations

**Cell Quality Tiers**:

**Minimal Cell** (Cramped, Depressing):
- Sleeping spot on floor
- No furniture
- No amenities
- Strong negative mood impact
- High mental break risk

**Basic Cell** (Functional):
- Bed
- Light source
- Door
- Neutral mood impact
- Standard rehabilitation

**Standard Cell** (Comfortable):
- Bed, end table, dresser
- Recreation item
- Decent flooring
- Slight positive mood impact
- Good rehabilitation

**Luxury Cell** (Resort):
- Quality furniture
- Multiple recreation options
- Art, TV, stereo
- Temperature control
- Positive mood impact
- Excellent rehabilitation

**Security Levels**:

**Low Security** (Non-violent offenders):
- Basic door
- Can socialize with other prisoners
- Some freedom of movement within prison area
- Work detail access

**Medium Security** (Standard criminals):
- Locked door
- Supervised recreation time
- Limited movement
- Restricted work access

**High Security** (Dangerous criminals):
- Reinforced door/walls
- No communal areas
- Constant guard presence
- No work detail
- Maximum isolation

**Quality Impact Mechanics**:
- Calculate cell quality using RimWorld room score system
- Map quality to mood modifiers:
  - Very Low: -15 mood, high mental break chance
  - Low: -10 mood
  - Medium: -5 mood
  - High: 0 mood
  - Very High: +5 mood (imprisonment still negative, but cell itself positive)

**Work Integration**:
- Prisoners assigned simple labor tasks
- Work reduces boredom
- Skill development possible
- Each day worked reduces sentence by 1.5 days (incentive)
- Dangerous tools restricted based on security level

**Social System**:
- Allow prisoner-colonist visits
- Prison breaks/meal times for social interaction
- Family members receive mood benefits from visiting
- Wardens build relationship through interaction

**Rehabilitation Tracking**:
- Track prisoner attitude over sentence
- Quality treatment → better attitude → easier recruitment/release
- Poor treatment → worse attitude → likely recidivism
- Display rehabilitation progress in UI
