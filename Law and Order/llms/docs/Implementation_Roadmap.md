# Law and Order: Implementation Roadmap

## Overview

This document provides a complete, step-by-step roadmap for implementing the Law and Order mod from current state to full feature completion.

**Design Philosophy**: See `Design_Philosophy.md`
**Technical Specs**: See `Crime_Visibility_States.md` and `Technical_Spec_Crime_States.md`
**DF Research**: See `llms/research/` directory

## Current State Assessment

### What Exists Now (✅)

Based on the current codebase:

- ✅ Basic mod structure (About.xml, LoadFolders.xml)
- ✅ HugsLib integration
- ✅ Crime tracking system (CrimeRecord, CrimeTracker)
- ✅ Crime type definitions (CrimeTypeDef)
- ✅ Penalty calculation system (base values + multipliers)
- ✅ Trial system foundations (ITab_Judiciary)
- ✅ UI localization framework (LawAndOrder_Keys.xml)
- ✅ Real Fog of War dependency declared
- ✅ Comprehensive DF research documentation

### What Needs Building (❌)

- ❌ Crime visibility state system (Hidden/Suspected/Convicted)
- ❌ Witness detection via Fog of War
- ❌ Criminal case management
- ❌ Justice Manager (WorldComponent)
- ❌ Investigation system
- ❌ Interrogation mechanics
- ❌ Main Justice UI tab
- ❌ Punishment system
- ❌ Conviction workflow
- ❌ False accusation mechanics
- ❌ Corruption/infiltration system
- ❌ Integration with mental breaks
- ❌ Social impact system
- ❌ Polish and balance

## Development Phases

### Phase 0: Preparation & Foundation Cleanup (1-2 weeks)

**Goal**: Audit existing code, establish development workflow, prepare for state system integration

#### Tasks

**0.1: Code Audit & Documentation**
- [ ] Review all existing C# files in `/Source`
- [ ] Document current crime tracking implementation
- [ ] Identify what to keep vs. refactor
- [ ] Create migration plan for existing systems
- [ ] Document current UI components (ITab_Judiciary)

**0.2: Development Environment Setup**
- [ ] Verify build process works
- [ ] Set up test save game with various scenarios
- [ ] Enable dev mode features for testing
- [ ] Create testing checklist document
- [ ] Set up version control practices (commit messages, branching)

**0.3: Dependencies & Integration Check**
- [ ] Test Real Fog of War integration in game
- [ ] Verify FoW APIs accessible from mod code
- [ ] Create test scenarios for FoW witness detection
- [ ] Document FoW edge cases and limitations
- [ ] Test HugsLib features being used

**0.4: Data Structure Planning**
- [ ] Design save/load strategy for new systems
- [ ] Plan backward compatibility approach (or lack thereof)
- [ ] Create data migration plan from current to new system
- [ ] Document save file structure
- [ ] Plan for save file size optimization

**Deliverables**:
- Code audit report
- Migration plan document
- Test environment ready
- Development workflow established

**Testing**: Build mod, load in game, verify no errors

---

### Phase 1: Core State System (2-3 weeks)

**Goal**: Implement the three-state crime visibility system (Hidden/Suspected/Convicted)

**Prerequisites**: Phase 0 complete

#### Tasks

**1.1: CrimeVisibilityState Enum**
- [ ] Create `CrimeVisibilityState.cs`
- [ ] Define three states: Hidden, Suspected, Convicted
- [ ] Add XML documentation
- [ ] Create unit tests (if testing framework added)

**1.2: CrimeInstance Class Refactor**
- [ ] Review existing crime tracking code
- [ ] Create new `CrimeInstance.cs` based on technical spec
- [ ] Add all required fields:
  - [ ] Core crime data (perpetrator, type, location, victim)
  - [ ] Visibility state tracking
  - [ ] Evidence & witnesses list
  - [ ] Case association
  - [ ] Conviction data
- [ ] Implement `TransitionToSuspected()` method
- [ ] Implement `TransitionToConvicted()` method
- [ ] Implement `IsVisibleInUI()` method
- [ ] Add `ExposeData()` for save/load
- [ ] Write XML documentation for all members

**1.3: CriminalCase Class**
- [ ] Create `CriminalCase.cs`
- [ ] Implement case management fields:
  - [ ] Case ID generation
  - [ ] Accused pawn tracking
  - [ ] Case status enum
  - [ ] Crimes list
- [ ] Implement `AddCrime()` method (amendment)
- [ ] Implement `OnCrimeConvicted()` callback
- [ ] Implement `CloseCase()` method
- [ ] Implement `GetOrCreateCase()` static method
- [ ] Add `ExposeData()` for save/load
- [ ] Write XML documentation

**1.4: Pawn Crime Record Component**
- [ ] Create `CompCrimeRecord.cs` (or refactor existing)
- [ ] Attach to all Pawns via CompProperties
- [ ] Store list of `CrimeInstance` objects
- [ ] Implement `AddCrime()` method
- [ ] Implement `GetCrimes()` query methods
- [ ] Implement filtering by state
- [ ] Add `ExposeData()` for save/load
- [ ] Create extension method `Pawn.GetCrimeRecord()`

**1.5: JusticeManager WorldComponent**
- [ ] Create `JusticeManager.cs` as WorldComponent
- [ ] Implement global crime registry
- [ ] Implement global case registry
- [ ] Implement case ID auto-increment
- [ ] Implement `GetActiveCase(pawn)` method
- [ ] Implement `GetOpenCases()` query
- [ ] Implement `GetConvictedCases()` query
- [ ] Add `ExposeData()` for save/load
- [ ] Create extension method `World.GetJusticeManager()`

**1.6: State Transition Methods**
- [ ] Implement full `Hidden → Suspected` transition
- [ ] Implement full `Suspected → Convicted` transition
- [ ] Implement case opening logic
- [ ] Implement case amendment logic
- [ ] Implement case closing logic
- [ ] Add logging for all transitions
- [ ] Add player notifications for state changes

**1.7: Testing**
- [ ] Create test scenario: manual crime with no witnesses (stays Hidden)
- [ ] Create test scenario: manual crime with witnesses (becomes Suspected)
- [ ] Create test scenario: manual conviction (becomes Convicted)
- [ ] Test save/load with crimes in all three states
- [ ] Verify state transitions work correctly
- [ ] Verify case management (one case per pawn)
- [ ] Test case amendment (multiple crimes in one case)

**Deliverables**:
- Complete state system implementation
- All classes compile without errors
- Save/load working for new data structures
- Basic state transitions functional
- No UI yet (internal systems only)

**Testing Checklist**:
- [ ] Mod loads without errors
- [ ] Can create crimes programmatically
- [ ] Crimes save/load correctly
- [ ] State transitions work
- [ ] Cases track crimes correctly
- [ ] No save corruption

**Known Risks**:
- Save file compatibility issues
- Performance with many crimes tracked
- Memory usage with full crime history

---

### Phase 2: Fog of War Integration (2 weeks)

**Goal**: Integrate witness detection using Real Fog of War

**Prerequisites**: Phase 1 complete, FoW installed and working

#### Tasks

**2.1: FoW API Integration**
- [ ] Create `FogOfWarUtils.cs` utility class
- [ ] Implement `GetMapComponentSeenFog(map)` helper
- [ ] Implement `IsLocationVisible(faction, location)` helper
- [ ] Implement `GetWitnesses(location, map)` method
- [ ] Add error handling for missing FoW component
- [ ] Add fallback behavior if FoW not available
- [ ] Test FoW API calls in dev mode

**2.2: Witness Detection System**
- [ ] Create `WitnessDetection.cs` class
- [ ] Implement line-of-sight checking
- [ ] Implement sight range calculations using FoW
- [ ] Implement witness list building
- [ ] Add distance-based witness reliability
- [ ] Add light-level checks (darkness reduces detection)
- [ ] Add weather-based modifiers
- [ ] Implement "caught red-handed" detection (very close)

**2.3: Evidence Strength Calculation**
- [ ] Create `EvidenceCalculator.cs` class
- [ ] Implement base evidence from witness count
- [ ] Add bonus for multiple independent witnesses
- [ ] Add penalty for darkness/poor visibility
- [ ] Add bonus for caught red-handed
- [ ] Add bonus for physical evidence
- [ ] Implement evidence strength formula (0.0-1.0)
- [ ] Document evidence calculation rules

**2.4: Crime Detection Event**
- [ ] Create `CrimeDetectionSystem.cs`
- [ ] Implement `OnCrimeCommitted()` main method
- [ ] Integrate witness detection
- [ ] Integrate evidence calculation
- [ ] Implement automatic Hidden/Suspected determination
- [ ] Add logging for detection results
- [ ] Add player notifications for witnessed crimes
- [ ] Test with various crime scenarios

**2.5: Integration with Existing Crime Types**
- [ ] Hook into assault detection
- [ ] Hook into murder detection
- [ ] Hook into theft detection
- [ ] Hook into vandalism detection
- [ ] Hook into arson detection
- [ ] Test each crime type with FoW

**2.6: Testing**
- [ ] Test crime with colonist nearby (visible in FoW) → Suspected
- [ ] Test crime with no colonists nearby → Hidden
- [ ] Test crime in darkness → reduced detection
- [ ] Test crime with multiple witnesses → high evidence
- [ ] Test crime just outside sight range → not detected
- [ ] Test crime in fog/rain → reduced detection
- [ ] Test with FoW disabled → fallback behavior

**Deliverables**:
- Complete FoW integration
- Automatic witness detection working
- Evidence strength calculated correctly
- Crimes properly Hidden or Suspected based on witnesses
- Player notifications for witnessed crimes

**Testing Checklist**:
- [ ] Witnesses detected correctly
- [ ] Sight range respected
- [ ] Darkness affects detection
- [ ] Multiple witnesses tracked
- [ ] Evidence strength reasonable
- [ ] No false positives
- [ ] No performance issues

**Known Risks**:
- FoW API changes in updates
- Performance with frequent witness checks
- Edge cases (teleportation, fast movement)
- Sleeping pawns not detecting crimes

---

### Phase 3: Basic UI Implementation (2-3 weeks)

**Goal**: Create Justice tab showing Suspected and Convicted crimes

**Prerequisites**: Phase 1 & 2 complete

#### Tasks

**3.1: Main Justice Tab Window**
- [ ] Create `MainTabWindow_Justice.cs`
- [ ] Register as main tab in `MainTabDef`
- [ ] Create tab icon graphic
- [ ] Implement basic window layout
- [ ] Implement tab switching (Open Cases / Convictions / Settings)
- [ ] Add window title and header
- [ ] Test tab opens without errors

**3.2: Open Cases Tab**
- [ ] Implement case list rendering
- [ ] Create case entry UI element
- [ ] Display case summary (case #, accused, crime count)
- [ ] Implement scroll view for multiple cases
- [ ] Add "No open cases" message
- [ ] Implement case selection highlighting
- [ ] Test with 0, 1, and many cases

**3.3: Case Details Display**
- [ ] Create detailed case view
- [ ] Display accused pawn info with portrait
- [ ] List all crimes in case
- [ ] Show crime details:
  - [ ] Crime type
  - [ ] Date/time
  - [ ] Location
  - [ ] Witnesses (count and names)
  - [ ] Evidence strength (percentage bar)
  - [ ] Confession status
- [ ] Add tooltips for detailed info
- [ ] Test with various crime types

**3.4: Crime Entry Widget**
- [ ] Create reusable crime entry widget
- [ ] Display [SUSPECTED] or [CONVICTED] label
- [ ] Show crime icon (if available)
- [ ] Show crime type label
- [ ] Show brief details
- [ ] Implement hover tooltips
- [ ] Add color coding by severity
- [ ] Test appearance and layout

**3.5: Convictions Tab**
- [ ] Implement convicted cases list
- [ ] Display conviction details:
  - [ ] Crime type
  - [ ] Conviction date
  - [ ] Punishment type
  - [ ] Punishment status (pending/in-progress/completed)
- [ ] Add punishment progress bars
- [ ] Show "No convictions" message
- [ ] Test with various punishment states

**3.6: Action Buttons**
- [ ] Implement "Convict" button (opens punishment dialog)
- [ ] Implement "Dismiss" button (with confirmation)
- [ ] Implement "Investigate" button (placeholder for Phase 4)
- [ ] Add button tooltips
- [ ] Add button enable/disable logic
- [ ] Test button actions

**3.7: Settings Tab**
- [ ] Create settings UI
- [ ] Add debug option: Show hidden crimes
- [ ] Add option: Auto-convict caught red-handed
- [ ] Add option: Enable false accusations
- [ ] Add option: Statute of limitations duration
- [ ] Implement settings save/load
- [ ] Test settings persistence

**3.8: Localization**
- [ ] Add all UI strings to `LawAndOrder_Keys.xml`
- [ ] Create translation keys:
  - [ ] Tab names
  - [ ] Button labels
  - [ ] Crime state labels
  - [ ] Tooltips
  - [ ] Messages
- [ ] Test all strings display correctly
- [ ] Verify no missing translations

**3.9: Testing**
- [ ] Test tab opens and displays correctly
- [ ] Test with no cases (empty state)
- [ ] Test with one case
- [ ] Test with many cases (scrolling)
- [ ] Test tab switching
- [ ] Test all buttons work
- [ ] Test UI scales with different resolutions
- [ ] Test UI with different font sizes

**Deliverables**:
- Functional Justice tab
- Open Cases and Convictions visible
- Clean, readable UI
- All actions functional (even if basic)
- Fully localized

**Testing Checklist**:
- [ ] No UI errors or exceptions
- [ ] Text readable and clear
- [ ] Buttons respond correctly
- [ ] Scrolling works smoothly
- [ ] No layout overlap
- [ ] Performance acceptable with many crimes

**Known Risks**:
- UI layout issues with different screen sizes
- Performance with large lists
- Text overflow issues
- Missing icons/graphics

---

### Phase 4: Conviction & Punishment System (2 weeks)

**Goal**: Implement full conviction workflow and punishment assignment

**Prerequisites**: Phase 3 complete

#### Tasks

**4.1: Punishment Definitions**
- [ ] Create `PunishmentDef` definition class
- [ ] Define punishment types:
  - [ ] Imprisonment (duration-based)
  - [ ] Beating (immediate)
  - [ ] Execution (delayed)
  - [ ] Fine (silver penalty)
  - [ ] Exile (banishment)
- [ ] Add XML definitions in `/Defs/PunishmentDefs/`
- [ ] Add punishment severity ratings
- [ ] Test defs load correctly

**4.2: Punishment Selection Dialog**
- [ ] Create `Dialog_SelectPunishment.cs`
- [ ] Display available punishments for crime
- [ ] Show punishment descriptions
- [ ] Implement punishment filtering by severity
- [ ] Add recommended punishment highlighting
- [ ] Implement punishment confirmation
- [ ] Test dialog appearance and flow

**4.3: Punishment Assignment**
- [ ] Create `Punishment.cs` class
- [ ] Implement punishment data structure:
  - [ ] Punishment type
  - [ ] Duration (if applicable)
  - [ ] Target pawn
  - [ ] Start tick
  - [ ] Completion tick
  - [ ] Status (pending/active/completed)
- [ ] Implement `AssignPunishment()` method
- [ ] Add punishment to pawn's needs/thoughts
- [ ] Implement `ExposeData()` for save/load

**4.4: Conviction Workflow**
- [ ] Implement full conviction flow:
  1. [ ] Player clicks "Convict"
  2. [ ] Punishment selection dialog opens
  3. [ ] Player selects punishment
  4. [ ] Crime transitions to Convicted
  5. [ ] Punishment assigned
  6. [ ] Case updated
  7. [ ] Notifications sent
- [ ] Add conviction confirmation dialog
- [ ] Implement multi-crime conviction (all in case)
- [ ] Test full workflow end-to-end

**4.5: Imprisonment System**
- [ ] Hook into existing prison system
- [ ] Implement duration tracking
- [ ] Add "Imprisoned for crime" thought
- [ ] Implement release after duration
- [ ] Add warden interactions
- [ ] Test imprisonment flow

**4.6: Beating System**
- [ ] Implement beating job
- [ ] Create `JobDriver_AdministerBeating.cs`
- [ ] Assign guard to deliver beating
- [ ] Implement damage calculation (non-lethal)
- [ ] Add pain and injury
- [ ] Add "Beaten for crime" thought
- [ ] Test beating execution

**4.7: Execution System**
- [ ] Implement execution scheduling
- [ ] Create execution job
- [ ] Assign executioner role
- [ ] Implement execution method (shooting/melee)
- [ ] Add execution location (designated spot)
- [ ] Add witness effects (morale impact)
- [ ] Test execution flow

**4.8: Social Impact System**
- [ ] Implement victim satisfaction thoughts
- [ ] Implement criminal unhappiness thoughts
- [ ] Implement witness thoughts:
  - [ ] Harsh punishment witnessed (negative)
  - [ ] Justice served (positive)
  - [ ] Unfair conviction (negative)
- [ ] Implement colony-wide mood effects
- [ ] Scale impact by relationships
- [ ] Test mood cascades

**4.9: Testing**
- [ ] Test imprisonment: assign, track duration, release
- [ ] Test beating: assign, execute, effects
- [ ] Test execution: schedule, carry out, effects
- [ ] Test conviction of multiple crimes
- [ ] Test dismissal of case
- [ ] Test mood effects on colony
- [ ] Test save/load with active punishments

**Deliverables**:
- Complete conviction workflow
- All punishment types functional
- Social impact system working
- Punishments persist through save/load

**Testing Checklist**:
- [ ] Can convict pawns successfully
- [ ] Punishments execute correctly
- [ ] Prisoners released after time
- [ ] Beatings non-lethal (usually)
- [ ] Executions work
- [ ] Mood effects apply
- [ ] No stuck states
- [ ] Performance acceptable

**Known Risks**:
- Punishment bugs causing stuck pawns
- Beatings too lethal
- Executions failing
- Mood cascades causing tantrum spirals
- Balance issues with punishment severity

---

### Phase 5: Investigation & Interrogation (3 weeks)

**Goal**: Implement investigation mechanics to reveal Hidden crimes

**Prerequisites**: Phase 4 complete

#### Tasks

**5.1: Investigator Role**
- [ ] Create `InvestigatorRoleDef`
- [ ] Implement role assignment UI
- [ ] Add social skill requirements
- [ ] Create investigator work type
- [ ] Implement investigator priorities
- [ ] Test role assignment

**5.2: Interrogation System Foundation**
- [ ] Create `InterrogationSystem.cs`
- [ ] Implement `PerformInterrogation()` method
- [ ] Implement social skill check formula
- [ ] Add relationship modifiers
- [ ] Implement confession chance calculation
- [ ] Test basic interrogation

**5.3: Interrogation Job**
- [ ] Create `JobDriver_Interrogate.cs`
- [ ] Implement interrogation workflow:
  1. [ ] Investigator gets job
  2. [ ] Suspect escorted to interrogation room
  3. [ ] Interrogation animation/activity
  4. [ ] Skill check performed
  5. [ ] Result applied
- [ ] Add interrogation duration (based on skills)
- [ ] Test job execution

**5.4: Interrogation Room**
- [ ] Create interrogation room requirement (optional)
- [ ] Add interrogation table furniture (optional)
- [ ] Implement room quality modifier
- [ ] Add comfort/impressiveness effects on success
- [ ] Test with and without dedicated room

**5.5: Confession Mechanics**
- [ ] Implement Hidden crime discovery
- [ ] Random selection of crime to confess
- [ ] Transition Hidden → Suspected on confession
- [ ] Mark crime as confessed
- [ ] Set evidence strength to 70%+
- [ ] Add to case
- [ ] Send notification
- [ ] Test confession flow

**5.6: Interrogation UI**
- [ ] Add "Interrogate" button to Justice tab
- [ ] Create suspect selection dialog
- [ ] Display interrogation results
- [ ] Show success/failure message
- [ ] Display what was confessed (if anything)
- [ ] Add interrogation history log
- [ ] Test UI flow

**5.7: Investigation Job System**
- [ ] Create `JobDriver_InvestigateCrime.cs`
- [ ] Implement crime scene investigation
- [ ] Add investigation skill checks
- [ ] Implement evidence discovery
- [ ] Search for clues at location
- [ ] Link evidence to suspects
- [ ] Test investigation jobs

**5.8: Evidence System**
- [ ] Create `Evidence.cs` class
- [ ] Define evidence types:
  - [ ] Physical (blood, items, etc.)
  - [ ] Circumstantial (presence, motive)
  - [ ] Testimonial (witness statements)
- [ ] Implement evidence collection
- [ ] Link evidence to crimes
- [ ] Display evidence in UI
- [ ] Test evidence tracking

**5.9: False Interrogation Results**
- [ ] Implement false confession chance (low)
- [ ] Implement innocent pawn framing
- [ ] Track false accusations
- [ ] Add storytelling elements
- [ ] Test false confession scenarios

**5.10: Testing**
- [ ] Test interrogation with high social skill (high success)
- [ ] Test interrogation with low social skill (low success)
- [ ] Test confession reveals Hidden crime
- [ ] Test multiple interrogations
- [ ] Test interrogation failure
- [ ] Test false confessions
- [ ] Test investigation job
- [ ] Test evidence discovery
- [ ] Test UI displays correctly

**Deliverables**:
- Complete interrogation system
- Investigation jobs functional
- Hidden crimes can be revealed
- Evidence tracked properly
- Full UI integration

**Testing Checklist**:
- [ ] Can interrogate suspects
- [ ] Confessions reveal Hidden crimes
- [ ] Social skills affect outcome
- [ ] Investigation jobs work
- [ ] Evidence collected
- [ ] UI shows results clearly
- [ ] Performance acceptable
- [ ] Save/load works

**Known Risks**:
- Balance: too easy or too hard to get confessions
- False confessions too frequent
- Investigation jobs interfering with other work
- Performance with many suspects

---

### Phase 6: False Accusations & Social Dynamics (2 weeks)

**Goal**: Implement DF-style false accusations and social dynamics

**Prerequisites**: Phase 5 complete

#### Tasks

**6.1: False Witness System**
- [ ] Create `FalseAccusationSystem.cs`
- [ ] Implement grudge-based false reporting
- [ ] Check social relationships for grudges
- [ ] Calculate false accusation chance
- [ ] Implement false witness filing
- [ ] Track false accusations separately
- [ ] Test grudge-based accusations

**6.2: False Report Mechanics**
- [ ] Implement `FileFalseReport()` method
- [ ] Create false crime instance (wrong perpetrator)
- [ ] Set false witness as reporter
- [ ] Transition to Suspected (false)
- [ ] Track as potential false accusation
- [ ] Test false report flow

**6.3: Truth Discovery**
- [ ] Implement investigation revealing truth
- [ ] Add skill check to detect false accusations
- [ ] Implement dismissal of false charges
- [ ] Add consequences for false accusers
- [ ] Implement exoneration of innocent
- [ ] Test truth discovery

**6.4: Vampire False Reporting**
- [ ] Hook into vampire kill events
- [ ] Implement vampire false report filing
- [ ] Select random innocent pawn to blame
- [ ] Track vampire's actual crime as Hidden
- [ ] Test vampire frame-ups
- [ ] Test discovery of real vampire

**6.5: Social Relationship Impact**
- [ ] Implement opinion modifiers:
  - [ ] Victim opinion of perpetrator (very negative)
  - [ ] Witnesses opinion of perpetrator (negative)
  - [ ] Friends of victim opinion of perpetrator (negative)
  - [ ] Friends of perpetrator (sympathy)
- [ ] Scale by crime severity
- [ ] Implement "unjust conviction" opinion modifier
- [ ] Test relationship changes

**6.6: Witness Reliability System**
- [ ] Create witness reliability calculation
- [ ] Factor in:
  - [ ] Distance from crime
  - [ ] Light level
  - [ ] Sight capacity
  - [ ] Relationship to accused
  - [ ] Intelligence stat
- [ ] Display reliability in UI
- [ ] Use reliability in conviction decisions
- [ ] Test reliability calculations

**6.7: Testing**
- [ ] Test grudge-based false accusation
- [ ] Test vampire false accusation
- [ ] Test innocent pawn Suspected
- [ ] Test truth discovery
- [ ] Test false accuser consequences
- [ ] Test relationship impacts
- [ ] Test witness reliability

**Deliverables**:
- False accusation system functional
- Social dynamics impact justice
- Witness reliability tracked
- Truth can be discovered
- Vampire integration working

**Testing Checklist**:
- [ ] False accusations occur
- [ ] Can detect false accusations
- [ ] Social relationships affected
- [ ] Witness reliability calculated
- [ ] Vampires frame others successfully
- [ ] Truth discovery works
- [ ] No infinite loops
- [ ] Performance acceptable

**Known Risks**:
- Too many false accusations (annoying)
- Too few false accusations (no drama)
- Balance of truth discovery difficulty
- Vampire detection too easy/hard

---

### Phase 7: Raid Crimes & Prisoner System (2 weeks)

**Goal**: Implement crime tracking for raiders and prisoners

**Prerequisites**: Phase 6 complete

#### Tasks

**7.1: Raid Crime Detection**
- [ ] Hook into raid events
- [ ] Detect raider actions:
  - [ ] Shooting colonists (assault/murder)
  - [ ] Arson
  - [ ] Destruction of property
  - [ ] Theft
- [ ] Record crimes automatically
- [ ] Handle visibility (obvious vs. unseen)
- [ ] Test raid crime detection

**7.2: Raider Crime Recording**
- [ ] Create crimes for each raider action
- [ ] Determine visibility (usually Suspected immediately)
- [ ] Track victim colonists
- [ ] Track property damage
- [ ] Aggregate crimes per raider
- [ ] Test crime recording during raids

**7.3: Prisoner Integration**
- [ ] Hook into capture events
- [ ] Transfer raider crimes to prisoner record
- [ ] Display crimes in prisoner tab
- [ ] Implement "Trial Prisoner" option
- [ ] Show case in Justice tab
- [ ] Test prisoner crime display

**7.4: Prisoner Trial System**
- [ ] Create prisoner trial dialog
- [ ] Display all prisoner's crimes
- [ ] Allow conviction/dismissal per crime
- [ ] Implement batch conviction
- [ ] Add execution option for convicted prisoners
- [ ] Add release option for dismissed charges
- [ ] Test prisoner trials

**7.5: War Crimes Definition**
- [ ] Create `WarCrimeDef` definitions
- [ ] Define war crimes:
  - [ ] Targeting medical personnel
  - [ ] Killing downed/fleeing
  - [ ] Destroying essential buildings
  - [ ] Using forbidden weapons
- [ ] Implement war crime detection
- [ ] Add extra severity to war crimes
- [ ] Test war crime detection

**7.6: Prisoner Sentiment**
- [ ] Implement prisoner thoughts during trial
- [ ] Add "Awaiting trial" mood
- [ ] Add "Convicted" mood
- [ ] Add "Expecting execution" mood
- [ ] Implement plea for mercy
- [ ] Test prisoner morale

**7.7: Execute Prisoner Integration**
- [ ] Hook into existing execute prisoner command
- [ ] Require conviction first (optional setting)
- [ ] Display crime record in execution confirmation
- [ ] Implement "Executed criminal" vs "Executed innocent" thoughts
- [ ] Test execution integration

**7.8: Testing**
- [ ] Test raid with various crimes
- [ ] Test capture of raiders
- [ ] Test prisoner trial flow
- [ ] Test conviction of prisoner
- [ ] Test execution of convicted prisoner
- [ ] Test release of dismissed prisoner
- [ ] Test war crime detection
- [ ] Test prisoner moods

**Deliverables**:
- Raid crimes tracked automatically
- Prisoner trial system functional
- War crimes defined and detected
- Execution integrated with justice system

**Testing Checklist**:
- [ ] Raid crimes recorded
- [ ] Prisoners show crime record
- [ ] Can try prisoners
- [ ] Convictions work for prisoners
- [ ] Executions require conviction (if enabled)
- [ ] War crimes detected
- [ ] Prisoner moods appropriate
- [ ] Performance during raids acceptable

**Known Risks**:
- Performance impact during large raids
- Too many crimes overwhelming UI
- Edge cases with prisoner death
- Balance of war crime severity

---

### Phase 8: Mental Break Integration (1 week)

**Goal**: Integrate crimes with RimWorld's mental break system

**Prerequisites**: Phase 7 complete

#### Tasks

**8.1: Mental Break Hook**
- [ ] Hook into mental break events
- [ ] Detect break types:
  - [ ] Tantrum (vandalism, assault)
  - [ ] Berserk (murder, assault)
  - [ ] Sadistic rage (murder)
- [ ] Record crimes during break
- [ ] Handle witness detection
- [ ] Test break integration

**8.2: Tantrum Crime Recording**
- [ ] Hook into furniture destruction
- [ ] Record vandalism crimes
- [ ] Hook into tantrum punch
- [ ] Record assault crimes
- [ ] Determine visibility based on FoW
- [ ] Test tantrum crimes

**8.3: Berserk Crime Recording**
- [ ] Hook into berserk attacks
- [ ] Record assault/murder
- [ ] Track injuries and deaths
- [ ] Determine if witnessed
- [ ] Test berserk crimes

**8.4: Mental State Context**
- [ ] Add "Mental break" context to crimes
- [ ] Display in UI (mitigating factor)
- [ ] Implement reduced punishment option
- [ ] Add "Not in control" defense
- [ ] Test mental state display

**8.5: Tantrum Spiral Integration**
- [ ] Track punishment → stress → tantrum chain
- [ ] Detect tantrum spirals
- [ ] Add warning notifications
- [ ] Implement preventative suggestions
- [ ] Test spiral detection

**8.6: Testing**
- [ ] Trigger tantrum → record vandalism
- [ ] Trigger berserk → record assault
- [ ] Test mental break shown in UI
- [ ] Test reduced punishment for mental break
- [ ] Test tantrum spiral detection
- [ ] Test witness detection during breaks

**Deliverables**:
- Mental breaks create crimes
- Mental state context tracked
- Tantrum spirals detected
- UI shows mental state

**Testing Checklist**:
- [ ] Tantrums create crimes
- [ ] Berserk creates crimes
- [ ] Mental state shown
- [ ] Can reduce punishment
- [ ] Spiral warnings work
- [ ] Performance acceptable

**Known Risks**:
- Too many crimes from mental breaks
- Cascading punishment spirals
- Balance of punishment reduction

---

### Phase 9: Ideology Integration (1 week)

**Goal**: Integrate crime severity and punishment with Ideology system

**Prerequisites**: Phase 8 complete

#### Tasks

**9.1: Ideology Crime Severity**
- [ ] Create ideology precepts for crime severity
- [ ] Map crimes to precept categories
- [ ] Implement severity modifiers per ideology
- [ ] Test with different ideologies

**9.2: Punishment Preferences**
- [ ] Add ideology punishment type preferences
- [ ] Implement preferred punishment suggestions
- [ ] Add disapproval for "wrong" punishment type
- [ ] Test punishment preferences

**9.3: Execution Attitudes**
- [ ] Hook into ideology execution attitudes
- [ ] Apply existing "Execution" precepts to convictions
- [ ] Implement mood modifiers based on ideology
- [ ] Test execution attitudes

**9.4: Justice Roles**
- [ ] Integrate with ideology roles
- [ ] Assign investigator based on ideology
- [ ] Assign executioner based on ideology
- [ ] Test role integration

**9.5: Testing**
- [ ] Test with various ideologies
- [ ] Test crime severity differences
- [ ] Test punishment preferences
- [ ] Test execution attitudes
- [ ] Test role assignments

**Deliverables**:
- Ideology determines crime severity
- Punishment preferences respected
- Execution attitudes integrated
- Roles assigned by ideology

**Testing Checklist**:
- [ ] Different ideologies behave differently
- [ ] Punishment suggestions appropriate
- [ ] Mood effects scaled by ideology
- [ ] Roles assigned correctly
- [ ] No conflicts with vanilla ideology

**Known Risks**:
- Ideology DLC not owned by all players
- Compatibility with ideology updates
- Balance across different ideologies

---

### Phase 10: Polish & Balance (2-3 weeks)

**Goal**: Polish UI, balance systems, fix bugs, optimize performance

**Prerequisites**: All core phases complete

#### Tasks

**10.1: UI Polish**
- [ ] Add icons for all crime types
- [ ] Improve color scheme
- [ ] Add animations (fade in/out)
- [ ] Improve tooltips
- [ ] Add context menus (right-click)
- [ ] Improve scrolling performance
- [ ] Polish notification messages
- [ ] Add sound effects

**10.2: Performance Optimization**
- [ ] Profile mod performance
- [ ] Optimize crime queries (caching)
- [ ] Optimize UI rendering
- [ ] Reduce save file size
- [ ] Optimize witness detection
- [ ] Test with large colonies (20+ pawns)
- [ ] Test with many crimes (100+)

**10.3: Balance Pass 1: Crime Detection**
- [ ] Review witness detection rates
- [ ] Adjust sight range modifiers
- [ ] Balance evidence strength calculations
- [ ] Tune confession rates
- [ ] Test edge cases

**10.4: Balance Pass 2: Punishments**
- [ ] Review punishment severity
- [ ] Adjust imprisonment durations
- [ ] Balance beating damage
- [ ] Review execution justifications
- [ ] Test punishment morale impacts

**10.5: Balance Pass 3: Social Impact**
- [ ] Review mood effect magnitudes
- [ ] Adjust relationship opinion changes
- [ ] Balance tantrum spiral triggers
- [ ] Test with different colony sizes
- [ ] Gather playtest feedback

**10.6: Bug Fixing**
- [ ] Fix all known bugs
- [ ] Test edge cases:
  - [ ] Pawn dies during trial
  - [ ] Pawn leaves map
  - [ ] Case for non-existent pawn
  - [ ] Corrupted save data
- [ ] Add error handling
- [ ] Add logging for debugging

**10.7: Documentation**
- [ ] Write player-facing mod description
- [ ] Create Steam Workshop page
- [ ] Write user guide
- [ ] Document known issues
- [ ] Create FAQ
- [ ] Write changelog

**10.8: Compatibility**
- [ ] Test with popular mods:
  - [ ] Psychology
  - [ ] Vanilla Expanded series
  - [ ] Combat Extended
  - [ ] Prison Labor
  - [ ] Hospitality
- [ ] Create compatibility patches if needed
- [ ] Document incompatibilities

**10.9: Localization**
- [ ] Complete English localization
- [ ] Provide translation templates
- [ ] Test string replacements
- [ ] Verify no missing keys

**10.10: Testing & Playtesting**
- [ ] Full playthrough test (100+ days)
- [ ] Test with multiple ideologies
- [ ] Test with various colony sizes
- [ ] Gather community feedback
- [ ] Beta release for testers
- [ ] Address feedback

**Deliverables**:
- Polished, professional UI
- Balanced gameplay
- All major bugs fixed
- Complete documentation
- Ready for public release

**Testing Checklist**:
- [ ] No crashes or errors
- [ ] Performance acceptable
- [ ] Balance feels good
- [ ] UI looks professional
- [ ] Documentation complete
- [ ] Compatibility tested
- [ ] Localization complete

**Known Risks**:
- Unanticipated bugs from testers
- Balance disagreements
- Compatibility issues with mods
- Performance issues at scale

---

### Phase 11: Advanced Features (Optional, 2-4 weeks)

**Goal**: Implement advanced DF-inspired features

**Prerequisites**: Phase 10 complete, mod in public testing

#### Tasks

**11.1: Corruption System**
- [ ] Implement official corruption
- [ ] Create bribery mechanics
- [ ] Implement corrupted investigators
- [ ] Add infiltrator agents
- [ ] Test corruption gameplay

**11.2: Conspiracy & Intrigue**
- [ ] Implement conspiracy detection
- [ ] Create villain networks
- [ ] Implement plot mechanics
- [ ] Add conspiracy investigation
- [ ] Test intrigue system

**11.3: Courtroom System**
- [ ] Create courtroom building
- [ ] Implement trial proceedings
- [ ] Add judge role
- [ ] Add jury system
- [ ] Implement verdict mechanics
- [ ] Test courtroom trials

**11.4: Appeal System**
- [ ] Implement conviction appeals
- [ ] Add appeal chance mechanics
- [ ] Implement reversal of convictions
- [ ] Add pardon system
- [ ] Test appeals

**11.5: Statute of Limitations**
- [ ] Implement time-based crime expiration
- [ ] Add cold case mechanics
- [ ] Implement investigation time limits
- [ ] Test limitations

**11.6: Testing**
- [ ] Test all advanced features
- [ ] Balance advanced mechanics
- [ ] Ensure optional features toggleable
- [ ] Test performance impact

**Deliverables**:
- Advanced DF features implemented
- Corruption and intrigue functional
- Courtroom trials working
- Optional feature toggles

**Testing Checklist**:
- [ ] Advanced features work
- [ ] Can be disabled if desired
- [ ] Balance appropriate
- [ ] Performance acceptable
- [ ] Documentation updated

**Known Risks**:
- Feature creep
- Complexity overwhelming players
- Performance degradation
- Balance issues

---

## Milestone Schedule

### Minimum Viable Product (MVP)
**Estimated Time**: 8-12 weeks
**Includes**: Phases 0-4
**Features**:
- Crime state system
- FoW witness detection
- Basic UI
- Conviction and punishment
**Release**: Alpha testing

### Feature Complete
**Estimated Time**: 16-20 weeks
**Includes**: Phases 0-9
**Features**:
- Full investigation system
- False accusations
- Raid crimes
- Mental break integration
- Ideology integration
**Release**: Beta testing

### Public Release
**Estimated Time**: 20-24 weeks
**Includes**: Phases 0-10
**Features**:
- All core features
- Polished UI
- Balanced gameplay
- Complete documentation
**Release**: 1.0 on Steam Workshop

### Post-Release
**Estimated Time**: Ongoing
**Includes**: Phase 11+
**Features**:
- Advanced features
- Community requests
- Bug fixes
- Balance updates
**Release**: 1.1, 1.2, etc.

---

## Dependencies & Blockers

### Critical Dependencies
1. **Real Fog of War**: Must be installed and working
2. **HugsLib**: Required for mod framework
3. **RimWorld 1.6**: Target version
4. **Harmony**: Patching library

### Potential Blockers
- FoW API changes (monitor mod updates)
- RimWorld updates breaking compatibility
- Performance issues at scale
- Save corruption bugs
- Balance issues causing negative feedback

---

## Testing Strategy

### Unit Testing (Per Phase)
- Test each class/method individually
- Verify data structures save/load
- Check edge cases
- Performance profiling

### Integration Testing (Per Phase)
- Test phase features together
- Verify no conflicts with previous phases
- Test save/load compatibility
- Check performance impact

### System Testing (End of Each Milestone)
- Full playthrough test
- Test all features together
- Stress testing (many crimes, large colony)
- Compatibility testing

### User Acceptance Testing (Before Release)
- Beta testers play full campaigns
- Gather feedback
- Identify balance issues
- Find obscure bugs

---

## Risk Mitigation

### Save Corruption
- **Risk**: High
- **Mitigation**:
  - Extensive testing of save/load
  - Backup save files during development
  - Implement save validation
  - Document breaking changes

### Performance Degradation
- **Risk**: Medium
- **Mitigation**:
  - Profile early and often
  - Optimize critical paths
  - Cache frequently accessed data
  - Test with large colonies

### Balance Issues
- **Risk**: High
- **Mitigation**:
  - Playtest extensively
  - Gather community feedback
  - Iterate on balance
  - Provide settings for customization

### Compatibility Breaks
- **Risk**: Medium
- **Mitigation**:
  - Monitor RimWorld updates
  - Test with popular mods
  - Provide compatibility patches
  - Document incompatibilities

### Scope Creep
- **Risk**: High
- **Mitigation**:
  - Stick to roadmap
  - Mark features as "Phase 11+"
  - Focus on MVP first
  - Community features post-release

---

## Success Metrics

### Technical Metrics
- [ ] No save corruption bugs
- [ ] Load time < 2 seconds with 100 crimes
- [ ] UI framerate > 30 FPS
- [ ] Memory usage < 100 MB
- [ ] No infinite loops or deadlocks

### Gameplay Metrics
- [ ] Players report engaging investigation gameplay
- [ ] False accusation rate 5-15%
- [ ] Conviction rate 60-80% of Suspected crimes
- [ ] Tantrum spiral prevention possible
- [ ] Punishment feels appropriately severe

### Community Metrics
- [ ] Positive Steam reviews (>80%)
- [ ] Active user base
- [ ] Community stories and emergent narratives
- [ ] Mod compatibility maintained
- [ ] Regular updates addressing feedback

---

## Development Tools & Practices

### Version Control
- Git repository with clear commit messages
- Feature branches for each phase
- Tag releases (v0.1-alpha, v1.0, etc.)
- Maintain changelog

### Code Quality
- XML documentation for all public members
- Consistent naming conventions
- Code reviews (if team)
- Regular refactoring

### Testing
- Manual testing checklist per phase
- Automated tests where possible
- Community beta testing
- Bug tracking system

### Documentation
- Keep roadmap updated
- Document all design decisions
- Maintain technical specs
- User-facing documentation

---

## Communication & Release

### Development Log
- Weekly progress updates
- Document blockers and solutions
- Share screenshots/videos
- Community engagement

### Release Strategy
1. **Alpha**: Phases 0-4 complete, private testing
2. **Beta**: Phases 0-9 complete, public testing
3. **Release Candidate**: Phase 10 complete, final testing
4. **1.0**: Public release on Steam Workshop
5. **Post-Release**: Phase 11+, ongoing updates

### Marketing
- Steam Workshop page with screenshots
- Reddit posts showing features
- Video demonstration
- Community storytelling

---

## Conclusion

This roadmap provides a complete path from current state to fully-featured Law and Order mod. The phased approach allows for:

✅ **Incremental development** - Test and validate each phase
✅ **Early feedback** - Alpha/beta releases get community input
✅ **Manageable scope** - MVP first, advanced features later
✅ **Quality focus** - Polish and balance before adding more
✅ **Flexibility** - Adjust based on feedback and blockers

**Estimated Total Time**: 20-24 weeks for public release

**Key Success Factors**:
- Stick to the roadmap
- Test thoroughly at each phase
- Gather and incorporate feedback
- Maintain code quality
- Document everything

This is an ambitious mod that will fundamentally change how RimWorld is played - but with this roadmap, it's achievable!
