# Law and Order - Project Notepad

**Date:** November 16, 2025
**Phase:** Phase 6.2 - Clue & Evidence System - BALANCING & FIXES 🔧
**Next:** Phase 6.3 - False Accusation System

---

## Recent Fixes (Nov 16, 2025)

### Bug Fixes Applied:
1. **Texture Loading Error** ✅
   - Fixed texture path from `ChunkSlagSteel` → `ChunkSlag`
   - Added `drawerType=MapMeshOnly` to ThingDef
   - Changed `Graphic_Single` → `Graphic_Random`
   - Files: `Things_CrimeSceneClues.xml`

2. **TickRare NotImplementedException** ✅
   - Removed `base.TickRare()` call (Entity throws NotImplementedException)
   - Follows RimWorld pattern (Corpse, Blight, etc. don't call base)
   - Files: `CrimeSceneClue.cs:122`

3. **Inspect String Empty Lines** ✅
   - Changed `AppendLine(base.GetInspectString())` → `Append(base.GetInspectString())`
   - Changed all subsequent `AppendLine()` → `AppendInNewLine()`
   - Follows RimWorld pattern (Building_Bed, etc.)
   - Files: `CrimeSceneClue.cs:138-166`

### Balance Adjustments:
1. **Clue Spawning Cooldown** ✅
   - Added 2-day cooldown per pawn for clue spawning
   - Prevents spam from single criminal
   - Tracked via static Dictionary<pawnID, lastSpawnTick>
   - Files: `CrimeSceneClue.cs:198-226`

2. **Analysis Speed Rebalance** ✅
   - Increased StudyDuration: 600 ticks → 2400 ticks (40 seconds = 1 in-game hour)
   - Reduced StudyProgressPerTick: 0.1f → 0.00025f (400x slower)
   - Makes clue analysis take meaningful time
   - Files: `JobDriver_AnalyzeClue.cs:17-18`

3. **Translation Key Fixes** ✅
   - Removed string interpolation from revelation labels
   - Changed dynamic translation keys to static English strings
   - Fixed: "Initial Analysis", "Detailed Examination", "Complete Profile"
   - Files: `CrimeSceneClue.cs:398-426`

4. **Translation Pattern Fix** ✅
   - Updated to RimWorld 1.6 NamedArgument pattern
   - Changed `.Translate(arg1, arg2)` → `.Translate(arg1.Named("KEY1"), arg2.Named("KEY2"))`
   - Added "LawAndOrder_" prefix to all translation keys (project convention)
   - Updated XML keys from `{0}, {1}, {2}` → `{PAWN}, {LABEL}, {CLUETYPE}, {DESCRIPTION}`
   - Follows RimWorld pattern (ChildcareUtility, etc.)
   - Fixed 3 translation calls in JobDriver_AnalyzeClue.cs
   - Files: `JobDriver_AnalyzeClue.cs:95, 134, 173`, `LawAndOrder_Keys.xml:683-685`

**Build Status:** ✅ 0 errors, 0 warnings - CLEAN BUILD!
**Deploy Status:** ✅ Successfully deployed to RimWorld mods folder

---

## Phase 6.2: Clue & Evidence System (Week 2) - COMPLETE ✅

**Status:** ✅ All clue system components implemented and compiling successfully!

### Components Implemented:

**Core Systems:**
- ✅ `CrimeSceneClue.cs` (500+ lines) - Physical evidence Thing class with study interface
- ✅ `ClueRevelation.cs` (60+ lines) - Progressive discovery data structure
- ✅ `ClueGenerator` static class - Complete clue spawning system
  - Clue type determination based on crime type
  - Quality calculation (lighting, intelligence, witnesses)
  - Spawn location finding
  - Revelation generation (3 stages: 25%, 50%, 100%)
- ✅ `EvidencePlanter.cs` (150+ lines) - False evidence planting system
  - Smart criminals (intelligence 0.6+) can plant evidence
  - High-quality fakes (0.8-1.0 quality)
  - False revelations pointing to scapegoats

**Job System:**
- ✅ `JobDriver_AnalyzeClue.cs` (200+ lines) - Study job with workflow:
  - Progress based on Intellectual skill
  - Progressive revelations at thresholds
  - Accuracy rolls for each revelation
  - Crime visibility transitions (Hidden → Suspected)
- ✅ `WorkGiver_AnalyzeClue.cs` (80+ lines) - Automatic clue analysis
  - Assigned to Research work (priority 90)
  - Assigned to Warden work (priority 25)

**ThingDefs (XML):**
- ✅ `Things_CrimeSceneClues.xml` - 6 clue ThingDefs:
  - CrimeSceneClue_Blood (blood stains)
  - CrimeSceneClue_Footprint (shoe prints)
  - CrimeSceneClue_ToolMark (weapon marks)
  - CrimeSceneClue_DroppedItem (left items)
  - CrimeSceneClue_FabricScrap (torn clothing)
  - CrimeSceneClue_Fingerprint (touch evidence)

**JobDefs (XML):**
- ✅ `Jobs_Investigation.xml` updated with:
  - AnalyzeCrimeClue JobDef
  - AnalyzeCrimeClue_Research WorkGiverDef
  - AnalyzeCrimeClue_Warden WorkGiverDef

**Translation Keys:**
- ✅ 40+ new clue-related translation keys:
  - Clue revelation stages (initial/detailed/complete × 6 types)
  - Basic/Detailed/Complete descriptions for each clue type
  - Job reports and progress messages
  - Revelation discovery messages

**Build Status:**
- ✅ 0 errors, 0 warnings
- ✅ Successfully deployed to RimWorld mods folder

**Files Created:** 3 new files
**Files Modified:** 3 existing files
**Total Lines Added:** ~1,000+ lines

**Key Features:**
1. **Clue Spawning:** Crimes automatically spawn 1-3 physical clues at scene
2. **Progressive Study:** Researchers/wardens analyze clues over time
3. **Staged Revelations:** 3 stages unlock as study progresses (25%, 50%, 100%)
4. **Evidence Planting:** Infiltrators plant false evidence to frame innocents
5. **Crime Discovery:** Analyzing clues can reveal Hidden crimes
6. **Automatic Decay:** Clues disappear after 10 days

**Integration:**
- ✅ Fully integrated with Phase 6.1 CompHiddenIdentity system
- ✅ Intelligence stat affects clue quality and planting ability
- ✅ Links to Crime class and CrimeVisibilityState system
- ✅ **INTEGRATED**: `ClueGenerator.GenerateCluesForCrime()` called in `CrimeUtils.RecordCrime()`
- ✅ Crime location stored automatically when crimes are committed
- ✅ Clues spawn for ALL crimes detected by the system (assault, theft, property damage, etc.)
- Ready for Phase 6.3 False Accusation integration

**Testing Notes:**
To test in-game:
1. Start/load a game
2. Wait for a raid or provoke a fight
3. After crimes are committed, physical clues should spawn at crime locations
4. Assign researchers or wardens to analyze clues (they should auto-assign)
5. Watch for revelation messages as clues are studied (25%, 50%, 100% progress)
6. Check that clues decay after 10 in-game days

**Next Steps:**
- **READY FOR IN-GAME TESTING** ✅
- Phase 6.3: Implement false accusation mechanics
- Phase 6.4: Truth discovery system

---

## Phase 6.1: Foundation (Week 1) - COMPLETE ✅

**Status:** ✅ All foundation components implemented and compiling successfully!

### Components Implemented:

**Data Structures:**
- ✅ `HiddenIdentity.cs` (170 lines) - Core identity concealment system
- ✅ `CompHiddenIdentity.cs` (230 lines) - ThingComp for infiltrators
- ✅ `CrimeSceneClue.cs` (290 lines) - Physical evidence tracking
- ✅ Extended `Crime` class with Phase 6 fields (false accusation tracking)
- ✅ Extended `Evidence` class with planting support

**Settings:**
- ✅ 6 new Phase 6 tuning parameters added to LawAndOrderSettings
- ✅ Settings accessor convenience class (LawAndOrderMod.Settings)
- ✅ Validation and default values

**Translation Keys:**
- ✅ 60+ Phase 6 translation keys added to LawAndOrder_Keys.xml
- ✅ Infiltrator revelation messages
- ✅ Clue types and analysis
- ✅ False accusation indicators
- ✅ Accomplice system
- ✅ Social impact thoughts

**Build Status:**
- ✅ 0 errors, 0 warnings
- ✅ Successfully deployed to RimWorld mods folder

**Files Created:** 3
**Files Modified:** 6
**Total Lines Added:** ~900+ lines

**Next Steps:**
- Phase 6.2: Implement false accusation mechanics
- Phase 6.3: Add truth discovery system
- Phase 6.4: Create clue spawning and analysis jobs

---

## Phase 5: Investigation & Interrogation - IN PROGRESS 🔄

### Scope & Requirements
- **Implementation:** Full Implementation (all 10 tasks from roadmap)
- **Room Requirement:** Required Room (designated interrogation table needed)
- **Work Integration:** Use Warden Work (wardens interrogate as part of duties)

### Implementation Plan

**5.1: Interrogation Table Designation System** ⏳
- [ ] Create `CompProperties_InterrogationTable.cs`
- [ ] Create `Comp_InterrogationTable.cs` with designation gizmo
- [ ] Create `RitualTargetFilter_InterrogationTable.cs` for targeting
- [ ] Add ThingComp patch to tables

**5.2: Interrogation System Foundation** ⏳
- [ ] Create `InterrogationSystem.cs` with core logic
- [ ] Implement `PerformInterrogation()` method
- [ ] Social skill check formula
- [ ] Confession chance calculation
- [ ] Hidden → Suspected crime transition
- [ ] Relationship modifiers

**5.3: Interrogation Job & Warden Work** ⏳
- [ ] Create `JobDriver_Interrogate.cs`
- [ ] Create `WorkGiver_Warden_Interrogate.cs`
- [ ] Implement interrogation workflow:
  - Warden gets job
  - Escort prisoner to interrogation table
  - Interrogation activity (duration based on skills)
  - Apply results
- [ ] Add to Warden work type def

**5.4: Evidence System** ⏳
- [ ] Create `Evidence.cs` class
- [ ] Define evidence types (Physical, Circumstantial, Testimonial)
- [ ] Implement evidence collection
- [ ] Link evidence to crimes
- [ ] Evidence strength calculation

**5.5: UI Integration** ⏳
- [ ] Wire up "Investigate" button in MainTabWindow_Justice
- [ ] Create `Dialog_InterrogationResult.cs` for results
- [ ] Display confession outcomes
- [ ] Show evidence discovered
- [ ] Interrogation history log

**5.6: False Accusations & Confessions** ⏳
- [ ] Implement false confession mechanics (low chance)
- [ ] Track false accusations
- [ ] Innocent pawn framing
- [ ] Truth discovery mechanics

**5.7: Localization** ⏳
- [ ] Add translation keys for interrogation system
- [ ] Evidence type labels
- [ ] Result messages
- [ ] UI strings

**5.8: Testing & Polish** ⏳
- [ ] Test interrogation with high/low social skill
- [ ] Test Hidden crime revelation
- [ ] Test false confessions
- [ ] Test warden job assignment
- [ ] Performance testing
- [ ] Save/load testing

**Files to Create:** ~15-20 new files
**Estimated Lines:** ~3,000-4,000 lines

---

## Phase 4: Conviction & Punishment System - COMPLETE ✅

### Status: FULLY IMPLEMENTED & DEPLOYED

**All tasks completed:**
- ✅ Punishment definition system (PunishmentDef, PunishmentType enum)
- ✅ Punishment tracking class (Punishment.cs with save/load)
- ✅ Punishment manager (WorldComponent_PunishmentManager)
- ✅ Five punishment types implemented:
  - ✅ Imprisonment (vanilla prison + duration tracking)
  - ✅ Fine (hediff-based silver debt)
  - ✅ Execution (vanilla mechanics + JobDriver)
  - ✅ Beating (JobDriver + non-lethal damage)
  - ✅ Exile (vanilla banishment)
- ✅ Punishment selection dialog (Dialog_SelectPunishment)
- ✅ Social impact system (12 ThoughtDefs + SocialImpactUtils)
- ✅ Auto-convict red-handed feature (setting + hourly checks)
- ✅ Full localization (77 new translation keys)
- ✅ Convictions tab enhancement (shows punishment status)
- ✅ Build successful (0 errors, 0 warnings)
- ✅ Deployed to RimWorld mods folder

**Files Created:** 13
**Files Modified:** 4
**Total Lines Added:** ~2,455 lines

**Documentation:**
- Full summary: `Phase_4_Summary.md`
- Updated: `Project_Tracker.md`

---

**Status:** ✅ Phase 5 COMPLETE - All investigation & interrogation features implemented and deployed!

---

## Phase 5 Implementation Summary - COMPLETE ✅

**Files Created:** 9 new files
**Files Modified:** 5 existing files
**Total Lines Added:** ~3,500+ lines
**Build Status:** ✅ 0 errors, 0 warnings

### Components Implemented:

**5.1: Interrogation Table System** ✅
- `CompProperties_InterrogationTable.cs` - Properties for table designation
- `Comp_InterrogationTable.cs` - Table designation component with gizmo UI
- `Patch_Tables_InterrogationTable.xml` - XML patch to add comp to all tables
- Right-click any table to designate as interrogation table

**5.2: Interrogation System Foundation** ✅
- `InterrogationSystem.cs` (370+ lines) - Core interrogation logic
- Social skill-based confession chance (30% base + up to 50% skill bonus)
- Relationship modifiers (friends harder, enemies easier)
- Room quality bonuses
- Evidence strength calculation (0.7-0.9 for confessions)
- Hidden → Suspected crime transitions
- False confession mechanics (5% base chance)

**5.3: Interrogation Job & Warden Work** ✅
- `JobDriver_InvestigateCase.cs` (200+ lines) - Job driver with workflow:
  - Escort prisoner to interrogation table
  - Warm-up phase
  - Interrogation activity (duration based on social skill)
  - Result application and messaging
- `WorkGiver_Warden_InvestigateCase.cs` (120+ lines) - Automatic warden assignment
- `Jobs_Investigation.xml` - JobDef and WorkGiverDef definitions
- Integrated with Warden work type (priority 30)

**5.4: Evidence System** ✅
- `Evidence.cs` (320+ lines) - Complete evidence tracking system
- Three evidence types: Physical, Circumstantial, Testimonial
- Evidence reliability calculation (0.0-1.0 scale)
- Evidence-to-crime linking
- Automatic evidence generation from witnesses

**5.5: UI Integration** ✅
- Wired "Investigate" button in MainTabWindow_Justice
- `StartInvestigation()` method with validation:
  - Checks if prisoner can be interrogated
  - Finds designated interrogation table
  - Assigns available warden
  - Creates and queues interrogation job
- Result messages with detailed outcomes
- UI enables/disables based on case state

**5.6: False Accusations & Confessions** ✅
- False confession chance (5-20% based on interrogator skill and suspect mental state)
- Low social skill increases false confession chance
- Mental break threshold increases false confession chance
- Tracking and messaging for false confessions
- Foundation for Phase 6 false accusation expansion

**5.7: Localization** ✅
- Added 35+ translation keys to `LawAndOrder_Keys.xml`:
  - Interrogation table designation (7 keys)
  - Investigation UI (6 keys)
  - Interrogation results (7 keys)
  - Evidence types and descriptions (8 keys)
  - False accusations (2 keys)

**5.8: Additional Files** ✅
- `HediffDefOf.cs` - DefOf class for Crimes hediff reference
- Fixed namespace conflicts and compilation errors
- Added all necessary using directives

### Key Features:

1. **Manual Investigation:**
   - Click "Investigate" button in Justice tab
   - System finds interrogation table and warden
   - Warden escorts prisoner and conducts interrogation

2. **Automatic Investigation:**
   - Wardens automatically interrogate prisoners with Hidden crimes
   - Integrated with warden work priority system
   - Smart prisoner selection (prioritizes most Hidden crimes)

3. **Dynamic Outcomes:**
   - Success chance: 10-95% based on skills, relationships, room quality
   - Confessions reveal 1-3 Hidden crimes (more with higher success)
   - Evidence strength: 70-90% for confessions
   - False confessions: 5-20% chance

4. **Evidence System:**
   - Physical, Circumstantial, and Testimonial evidence types
   - Evidence reliability tracking
   - Auto-generation from witnesses
   - Evidence strength affects crime visibility

5. **Room Requirement:**
   - Must designate at least one table as interrogation table
   - Better room quality = higher confession chance
   - No dedicated furniture required (uses existing tables)

### Next Steps:
- In-game testing and balance tuning
- Optional: Expand false accusations (Phase 6)
- Optional: Add investigation jobs for crime scenes
- Optional: Physical evidence collection mechanics

**Status:** Phase 5 complete, ready for testing and Phase 6 planning.
