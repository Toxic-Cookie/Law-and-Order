# Court Ritual System - Remaining Implementation Phases

## ✅ Phase 1: Chair Designation System (COMPLETED)

### What Was Implemented:
- `CompCourtroomChair` - Component for storing chair roles
- `Gizmo_DesignateChairRole` - UI button for designating chair roles
- `CourtroomUtils` - Validation and seat counting functions
- XML Patch - Automatically adds comp to all sittable furniture
- Harmony Patch - Adds gizmo to all chairs

### Chair Roles Available:
- **Judge** - For the adjudicator/warden
- **Jury** - For jury members (optional)
- **Defendant** - For the accused prisoner
- **Victim** - For crime victims (optional)
- **Spectator** - For general observers

### Minimum Requirements:
- At least 1 Judge seat
- At least 1 Defendant seat

---

## 🚧 Phase 2: Ritual System Foundation

### Overview
Create the RimWorld ritual framework that will execute the hearing as a formal ceremony with multiple participants and stages.

### Files to Create:

#### 1. **Ritual Behavior** (`Source/Rituals/RitualBehavior_Hearing.cs`)
```csharp
// Custom ritual behavior that handles hearing-specific logic
- Extends RitualBehaviorWorker
- Validates courtroom has proper seating
- Assigns roles to participants based on chair designations
- Manages ritual flow and stage progression
```

**Key Methods:**
- `CanStartRitual()` - Check courtroom validity and participant availability
- `GetRitualTargetInfo()` - Return the courtroom room as the ritual target
- `PostCleanup()` - Apply plea bargain results after ritual completes

#### 2. **Ritual Role Definitions** (`Source/Rituals/RitualRole_*.cs`)
Create separate role classes for each participant type:

**RitualRole_Judge.cs**
- Requires: Colonist with Social skill
- Selection: Automatically picks highest social skill colonist, or user can specify
- Seat: Judge chair(s)

**RitualRole_Defendant.cs**
- Requires: The prisoner being tried
- Selection: Specified when starting ritual
- Seat: Defendant chair

**RitualRole_Victim.cs** (Optional)
- Requires: Colonist who was harmed by the defendant's crimes
- Selection: Automatically finds victims from crime record
- Seat: Victim chair(s)

**RitualRole_Jury.cs** (Optional)
- Requires: Colonists (any not already in hearing)
- Selection: Randomly picks colonists up to available jury seats
- Seat: Jury chair(s)

**RitualRole_Spectator.cs** (Optional)
- Requires: Any colonist
- Selection: Any colonist can join as audience
- Seat: Spectator chair(s)

#### 3. **Ritual Stages** (`Source/Rituals/RitualStage_*.cs`)

**RitualStage_OpeningStatements.cs**
- Judge stands and reads opening statement
- Duration: 30-60 seconds
- Visual: Judge gestures, others listen

**RitualStage_ReadingCharges.cs**
- Judge reads each crime from the criminal record
- Duration: 15 seconds per crime
- Visual: Judge speaking, defendant sits solemnly

**RitualStage_PleaBargain.cs** (Conditional)
- Defendant makes their plea (if not already attempted)
- Social skill contest occurs here
- Duration: 60-120 seconds
- Visual: Defendant stands and speaks, judge listens
- Outcome: Debt modified based on result

**RitualStage_Deliberation.cs** (If jury present)
- Jury members "discuss" amongst themselves
- Duration: 30 seconds
- Visual: Jury members turn to each other

**RitualStage_Sentencing.cs**
- Judge delivers final sentence
- Duration: 30 seconds
- Visual: Judge stands and speaks
- Outcome: Hearing status updated to completed

**RitualStage_Closing.cs**
- Participants disperse
- Duration: 10 seconds

### XML Defs to Create:

#### **Defs/RitualDefs/Ritual_Hearing.xml**
```xml
<PreceptDef>
  <defName>LawAndOrder_Hearing</defName>
  <label>court hearing</label>
  <description>A formal hearing where a prisoner is tried for their crimes.</description>
  <ritualBehavior>LawAndOrder_Hearing</ritualBehavior>
  <canRemoveInUI>false</canRemoveInUI>
  <!-- ... full def structure -->
</PreceptDef>

<RitualBehaviorDef>
  <defName>LawAndOrder_Hearing</defName>
  <workerClass>Law_and_Order.Source.Rituals.RitualBehavior_Hearing</workerClass>
  <durationTicks>2500</durationTicks> <!-- ~1 in-game hour -->
  <stages>
    <!-- List all stages -->
  </stages>
  <roles>
    <!-- List all roles -->
  </roles>
</RitualBehaviorDef>
```

### Integration Points:
- Replace `Dialog_ConductHearing` UI with ritual targeting system
- Ritual outcome triggers plea bargain calculation
- Update `MainTabWindow_Justice` to show "Begin Ritual" button instead of manual hearing dialog

---

## 🚧 Phase 3: Courtroom Room Requirements

### Overview
Implement formal room requirements similar to throne rooms, ensuring proper courtroom setup.

### Files to Create:

#### 1. **Room Requirement** (`Source/Rituals/RoomRequirement_Courtroom.cs`)
```csharp
// Validates that a room meets courtroom standards
- Extends RoomRequirement (or custom validator)
- Checks for minimum seating by role
- Optionally checks room impressiveness
- Provides detailed error messages
```

**Validation Checks:**
- ✅ At least 1 Judge seat
- ✅ At least 1 Defendant seat
- ⚠️ Recommended: 3+ Jury seats
- ⚠️ Recommended: 1+ Victim seat
- ⚠️ Recommended: 2+ Spectator seats
- 📊 Optional: Minimum room impressiveness (configurable)
- 📊 Optional: Minimum room size

#### 2. **Room Role** (`Source/Rituals/RoomRoleDef_Courtroom.cs`)
```csharp
// Defines the "Courtroom" room role
- Similar to Throne Room, Barracks, etc.
- Shows in Room Stats
- Displays seat counts in room info
- Can be assigned/unassigned manually
```

### XML Defs to Create:

#### **Defs/RoomRoleDefs/RoomRole_Courtroom.xml**
```xml
<RoomRoleDef>
  <defName>LawAndOrder_Courtroom</defName>
  <label>courtroom</label>
  <description>A formal room for conducting legal hearings.</description>
  <relatedStat>Impressiveness</relatedStat>
  <!-- Requirements defined in code -->
</RoomRoleDef>
```

### UI Enhancements:

#### **Room Stats Display**
When viewing a room designated as a courtroom, show:
```
Courtroom Quality: Adequate / Good / Excellent
Required Seating:
  ✓ Judge: 1/1
  ✓ Defendant: 1/1
  ⚠ Jury: 1/3 (recommended)
  ✗ Victim: 0/1 (recommended)
  ✓ Spectator: 4/2
```

#### **Architect Menu** (Optional)
Add a "Designate Courtroom" button similar to "Assign Barracks"
- Assigns room role
- Shows whether room meets requirements
- Provides recommendations for missing seats

---

## 🚧 Phase 4: Ritual Integration with Existing Systems

### Overview
Connect the new ritual system with the existing hearing/plea bargain code, ensuring seamless data flow.

### Modifications Needed:

#### 1. **Update HearingUtils.cs**
```csharp
// Add methods to start ritual instead of dialog
public static void StartHearingRitual(Pawn prisoner, Pawn judge, Room courtroom)
{
    // Find the ritual pattern
    var ritualPattern = Find.IdeoManager.GetRitualPattern(LawAndOrder_RitualDefOf.Hearing);

    // Create ritual obligation
    var obligation = new RitualObligation(ritualPattern, prisoner);

    // Start ritual targeting
    // ... ritual system code
}
```

#### 2. **Update MainTabWindow_Justice.cs**
Replace the "Schedule Hearing" button logic:
```csharp
// OLD: Opens Dialog_ConductHearing
Find.WindowStack.Add(new Dialog_ConductHearing(prisoner, adjudicator));

// NEW: Starts ritual targeting
HearingUtils.StartHearingRitual(prisoner, adjudicator, courtroom);
```

#### 3. **Create RitualOutcomeEffectWorker** (`Source/Rituals/RitualOutcomeEffectWorker_Hearing.cs`)
```csharp
// Handles post-ritual effects
- Applies plea bargain outcome
- Updates hearing record
- Grants mood thoughts
- Logs the verdict
- Updates prisoner status
```

**Key Outcome Data:**
- Quality of ritual (based on room impressiveness, roles filled)
- Plea bargain success/failure
- Participant memories

#### 4. **Update Dialog_ConductHearing.cs**
Two options:

**Option A:** Convert to ritual-based system entirely (delete dialog)
**Option B:** Keep as "manual" hearing for storytelling/testing
- Add note: "This is a simplified hearing. For full experience, use the Hearing ritual."

### DefOf Updates:

#### **Source/DefOf.cs**
```csharp
[DefOf]
public static class LawAndOrder_RitualDefOf
{
    public static PreceptDef LawAndOrder_Hearing;
    public static RitualPatternDef LawAndOrder_HearingPattern;

    static LawAndOrder_RitualDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(LawAndOrder_RitualDefOf));
    }
}
```

---

## 🚧 Phase 5: Ritual Thought/Memory System

### Overview
Add appropriate thoughts and memories for ritual participants based on outcomes.

### Thoughts to Create:

#### **Defs/ThoughtDefs/Thoughts_Hearing.xml**

**For Defendant:**
- `LawAndOrder_AttendedOwnHearing` (-5 mood, 3 days) - Stressful experience
- Combined with existing plea bargain thoughts

**For Judge:**
- `LawAndOrder_PresidedOverHearing` (+3 mood, 2 days) - Fulfilling duty
- `LawAndOrder_JudgedInnocent` (+5 mood, 3 days) - If debt reduced significantly
- `LawAndOrder_JudgedGuilty` (+2 mood, 2 days) - Justice served

**For Victims:**
- `LawAndOrder_SawJusticeServed` (+8 mood, 5 days) - Perpetrator held accountable
- `LawAndOrder_JusticeDenied` (-8 mood, 5 days) - If plea bargain too successful

**For Spectators:**
- `LawAndOrder_AttendedHearing` (+1 mood, 1 day) - Interesting event
- `LawAndOrder_AttendedSpectacle` (+3 mood, 2 days) - If critical success/failure

**For Jury:**
- `LawAndOrder_ServedOnJury` (+4 mood, 2 days) - Civic duty

### Memory Qualifiers:
```csharp
// In RitualOutcomeEffectWorker_Hearing
protected override void ApplyMemory(Pawn pawn, RitualRole role, RitualOutcome outcome)
{
    if (role == judge)
    {
        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.PresidedOverHearing);
    }
    // etc.
}
```

---

## 🚧 Phase 6: Polish and Testing

### Testing Checklist:

#### **Courtroom Setup**
- [ ] Can designate chairs for all roles
- [ ] Room validation shows correct seat counts
- [ ] Multiple rooms can be set up as courtrooms
- [ ] Chair designations persist through save/load

#### **Ritual Initiation**
- [ ] "Begin Hearing" shows in Justice tab when prisoner has crimes
- [ ] Ritual targeting works correctly
- [ ] Can select specific courtroom if multiple exist
- [ ] Can specify judge if desired

#### **Ritual Execution**
- [ ] All participants path to their designated seats
- [ ] Stages progress in correct order
- [ ] Plea bargain occurs at correct stage
- [ ] Visual effects display properly
- [ ] Pawns return to normal activities after ritual

#### **Plea Bargain Integration**
- [ ] Social skill calculation works during ritual
- [ ] Critical success/failure outcomes apply correctly
- [ ] Debt modifications are applied
- [ ] Thoughts are granted appropriately

#### **Edge Cases**
- [ ] What if judge dies mid-ritual?
- [ ] What if defendant escapes mid-ritual?
- [ ] What if courtroom is destroyed mid-ritual?
- [ ] Can prisoner attend their own hearing if not imprisoned?
- [ ] What if no colonist can path to courtroom?

### Performance Optimization:
- Cache courtroom validations
- Don't recalculate seat counts every frame
- Pool gizmo instances

### User Experience:
- Add tooltips explaining each ritual stage
- Show progress bar during ritual
- Display outcome summary letter after ritual
- Add sound effects for key moments (gavel sound, gasps, etc.)

---

## 🎯 Phase 7: Optional Enhancements

### Advanced Features (Post-MVP):

#### **Multiple Hearing Types**
- **Summary Hearing** - Fast, informal (minor crimes)
- **Full Hearing** - Standard procedure (current implementation)
- **Grand Trial** - Large, formal with full jury (serious crimes)

#### **Jury Verdict System**
- Jury members have individual opinions based on traits
- Final verdict is majority vote
- Judge can override in extreme cases

#### **Defense Attorney**
- Optional colonist role that assists defendant
- Provides bonus to plea bargain chance
- Requires Social skill

#### **Evidence Presentation**
- Items can be "marked as evidence"
- Shown during ritual for atmosphere
- Affects jury/judge opinion

#### **Sentencing Options**
- **Community Service** - Forced labor with specific quotas
- **House Arrest** - Restricted to specific areas
- **Banishment** - Permanent exile (release to wilderness)
- **Capital Punishment** - Execution (if crime is severe)

#### **Appeal System**
- Defendant can request second hearing
- Higher cost/requirements
- Lower success chance

#### **Historical Record**
- Permanent log of all hearings held
- Statistics tracking (conviction rate, average debt, etc.)
- "Notable Trials" list

---

## 📋 Implementation Priority

### Recommended Order:

1. **Phase 2** - Ritual System Foundation (Core functionality)
2. **Phase 4** - Integration with Existing Systems (Make it work end-to-end)
3. **Phase 3** - Room Requirements (Polish and validation)
4. **Phase 5** - Thought/Memory System (Flavor and story)
5. **Phase 6** - Testing and Polish
6. **Phase 7** - Optional Enhancements (Post-release)

### Estimated Complexity:
- **Phase 2:** High (requires understanding RimWorld ritual system)
- **Phase 3:** Medium (mostly UI and validation)
- **Phase 4:** Medium (refactoring existing code)
- **Phase 5:** Low (straightforward XML and simple code)
- **Phase 6:** Medium (time-consuming but straightforward)
- **Phase 7:** Varies (nice-to-haves)

---

## 🔍 Technical Notes

### RimWorld Ritual System Architecture

RimWorld's ritual system is complex. Key classes to study:

1. **`RitualBehaviorWorker`** - Base class for ritual logic
2. **`RitualRoleAssignments`** - Assigns pawns to roles
3. **`RitualStage`** - Individual stage definitions
4. **`RitualOutcomeEffectWorker`** - Post-ritual effects
5. **`LordJob_Ritual`** - AI job for ritual participation

### Reference Mods:
- **Vanilla Expanded - Ideology** - Has custom rituals
- **Royalty DLC** - Bestowing ceremonies are similar structure

### Debugging Tips:
- Use Dev Mode "Debug Actions" to force rituals
- Check `RitualDebug` class in RimWorld source
- Enable verbose logging for ritual system
- Use `Find.WindowStack` debug to track ritual states

---

## 📝 Next Steps

1. Study RimWorld's ritual system by examining vanilla ritual defs
2. Create basic `RitualBehaviorWorker_Hearing` skeleton
3. Define ritual stages with proper durations and transitions
4. Test ritual can be started and completes without errors
5. Integrate plea bargain calculation into ritual flow
6. Add UI polish and player feedback

**Good luck with the implementation!** The foundation is solid, and the ritual system will make this feature truly immersive.
