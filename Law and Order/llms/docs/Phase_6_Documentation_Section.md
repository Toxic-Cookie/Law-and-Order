---

## Section 12: Phase 6 - False Accusations & Infiltration (Foundation)

**Status:** Week 1 (Foundation) COMPLETE ✅
**Next:** Phase 6.2 - False Accusation Mechanics

### Overview

Phase 6 introduces deception and mystery mechanics to the justice system, including:
- Hidden identities for infiltrators and sanguophages
- Physical clue spawning and analysis at crime scenes
- False accusations based on grudges or framing
- Truth discovery through investigation
- Evidence planting mechanics

### Phase 6.1: Foundation Components

#### HiddenIdentity System

**File:** `Source/Infiltration/HiddenIdentity.cs` (170 lines)

The `HiddenIdentity` class stores concealed identity information for pawns who are secretly hostile.

**Key Features:**
- Progressive discovery system (0-100% progress)
- Intelligence stat determines behavior complexity
- Masked traits (Psychopath, Bloodlust hidden from player)
- Automatic sanguophage gene masking

#### CompHiddenIdentity ThingComp

**File:** `Source/Infiltration/CompHiddenIdentity.cs` (230 lines)

ThingComp attached to pawns with hidden identities.

**Infiltrator Reactions (by intelligence):**

All Infiltrators:
- FleeMap: Try to escape
- FightToTheEnd: Go down fighting

Intelligence > 0.6:
- FakeInnocence: "This is a misunderstanding!"
- AttemptBribery: Offer to reveal secrets

Intelligence > 0.8:
- ActivateAccomplices: Trigger sabotage NOW
- TransmitIntelNow: Emergency intel transmission

#### CrimeSceneClue System

**File:** `Source/Investigation/CrimeSceneClue.cs` (290 lines)

Physical evidence Things that spawn at crime scenes with progressive revelation system as clues are studied.

**Clue Types:**
- BloodStain: Blood at crime scene
- Footprint: Shoe prints
- ToolMark: Damage pattern
- DroppedItem: Item left behind
- FabricScrap: Torn clothing
- FingerprintTrace: Touch evidence
- WitnessReport: Verbal clue from interrogation

#### Extended Crime & Evidence Classes

**Crime Class Extensions:**
- `isFalseAccusation`: Is this a false accusation?
- `actualPerpetrator`: If false, who really did it?
- `falselyAccused`: Who was blamed?
- `location`: Crime scene location for clue spawning

**Evidence Class Extensions:**
- `isPlanted`: Was this evidence fabricated?
- `plantedBy`: Who planted the false evidence?

### Phase 6 Settings

Six new tuning parameters:
- False Accusation Rate (0-30%, default 12%)
- Infiltrator Spawn Chance (0-15%, default 5%)
- Max Accomplices per Infiltrator (0-5, default 2)
- Truth Discovery Chance (5-40%, default 15%)
- Clues Per Crime Min/Max (default 1-3)

### Translation Keys

60+ Phase 6 translation keys added covering:
- Settings, hidden identity revelation, clue types
- Clue analysis, witness reliability, truth discovery
- False accusation indicators, infiltrator reactions
- Intelligence gathering, accomplice system
- Social impact thoughts, sanguophage frame-ups

### Implementation Statistics

**Phase 6.1 Deliverables:**
- ✅ 3 new files created (690 total lines)
- ✅ 6 existing files extended
- ✅ 60+ translation keys added
- ✅ 0 compilation errors
- ✅ Successfully deployed

**Next Steps: Phase 6.2**
- False accusation generation mechanics
- Grudge-based false accusations
- Sanguophage murder frame-ups
- Truth discovery investigation jobs
