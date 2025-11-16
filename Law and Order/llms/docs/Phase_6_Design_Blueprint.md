# Phase 6: False Accusations & Social Dynamics + Infiltration System
## Complete Design Blueprint & Implementation Roadmap

**Version:** 1.0
**Date:** November 16, 2025
**Status:** Design Proposal - Pending Approval
**Estimated Scope:** 4-6 weeks implementation

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Design Philosophy & Goals](#design-philosophy--goals)
3. [System Architecture Overview](#system-architecture-overview)
4. [Core Systems](#core-systems)
   - 4.1 [Hidden Identity System](#41-hidden-identity-system)
   - 4.2 [Clue & Evidence System](#42-clue--evidence-system)
   - 4.3 [Intelligence Gathering System](#43-intelligence-gathering-system)
   - 4.4 [False Accusation System](#44-false-accusation-system)
   - 4.5 [Social Manipulation & Accomplice System](#45-social-manipulation--accomplice-system)
   - 4.6 [Witness Reliability System](#46-witness-reliability-system)
   - 4.7 [Social Relationship Impact System](#47-social-relationship-impact-system)
5. [Integration Points](#integration-points)
6. [Data Structures](#data-structures)
7. [Implementation Roadmap](#implementation-roadmap)
8. [Testing Strategy](#testing-strategy)
9. [Balance & Tuning](#balance--tuning)
10. [Risk Mitigation](#risk-mitigation)

---

## Executive Summary

Phase 6 represents a transformative expansion of the Law and Order mod, implementing Dwarf Fortress-inspired systems for **false accusations**, **hidden identities**, **espionage**, and **social manipulation**. This phase converts the mod from a crime-tracking system into a **deep investigative thriller** where players must navigate uncertain information, unreliable witnesses, and active saboteurs.

### Key Features

**Deception & Intrigue:**
- Sanguophages and infiltrators hide their true identities using fake names and backgrounds
- Bad actors conduct espionage to gather intelligence on colony defenses and valuables
- Manipulated colonists become accomplices who sabotage during raids
- False accusations based on grudges, mistakes, and intentional frame-ups

**Evidence & Investigation:**
- Physical clues left at crime scenes (inspired by Anomaly DLC's Sightstealer mechanics)
- Clues can be discovered, studied, and linked to criminal cases
- Evidence planting by intelligent criminals to frame innocents
- Progressive revelation system as investigation deepens

**Social Dynamics:**
- Witness reliability affected by distance, lighting, relationships, and intelligence
- Opinion modifiers cascade through social networks
- Colony morale impacts from unjust convictions
- Accomplice recruitment based on mood, traits, and backstories

**Espionage Consequences:**
- Intelligence transmitted when infiltrators leave the map
- Harder raids with tactical advantages (sappers, drop pod strikes on valuables)
- Player warnings about unchecked espionage
- Multiple infiltrator visits required to gather complete intelligence

### Design Pillars

1. **Emergent Narratives Over Balance** - Create memorable stories, even if "unfair"
2. **Player Agency Through Investigation** - Feel like a detective, make mistakes
3. **Moderate Drama** - 10-15% false accusation rate for engaging uncertainty
4. **Theatrical Presentation** - Dramatic reveals, event messages, storytelling emphasis
5. **Intelligent Adversaries** - Bad actors use Intelligence stat to determine behavior cleverness

---

## Design Philosophy & Goals

### Inspiration: Dwarf Fortress Justice System

From the DF design philosophy document:

> **"False Accusations create drama, not frustration"**
>
> - Innocent colonist falsely accused and executed
> - Vampire frames someone else for murder
> - Colony torn apart by false accusations
> - Infiltrator corrupts justice system from within

### Core Goals

**1. Uncertainty is Engaging**
- Players never have perfect information
- Must weigh conflicting evidence and testimony
- Investigation reveals truth, but requires effort and skill
- Mistakes have permanent, story-defining consequences

**2. Active Adversaries**
- Infiltrators aren't passive threats waiting to be discovered
- They actively work to undermine the colony
- Intelligence stat creates smart vs. dumb criminals
- Multiple infiltrators can create conspiracy networks

**3. Social Networks Matter**
- Crimes ripple through relationships
- Friends defend, enemies accuse
- Witnesses have biases
- Unjust convictions destroy colony morale

**4. Investigation is Rewarding**
- Finding clues feels like detective work
- Interrogations can break cases or lead astray
- Evidence builds a compelling picture
- Truth discovery is a climactic moment

### What This System is NOT

**Not Optional:**
- False accusations are core to the drama
- **Cannot be disabled** - this is a fundamental feature
- Designed to create tension, not convenience
- Fully integrated with all other mod systems

**Not Deterministic:**
- No "solve crime" button
- Investigation requires time, skill, and luck
- Wrong conclusions are possible and intentional

**Not Fair:**
- Innocent pawns can be convicted and executed
- Players must live with mistakes
- System intentionally creates cascading failures

---

## System Architecture Overview

### High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Phase 6 System Layer                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌───────────────────┐      ┌────────────────────────┐      │
│  │ Hidden Identity   │      │ Clue & Evidence System │      │
│  │ System            │◄────►│                        │      │
│  │                   │      │ - Crime Scene Clues    │      │
│  │ - Fake Names      │      │ - StudyUnlocks         │      │
│  │ - Masked Traits   │      │ - Evidence Linking     │      │
│  │ - False Backstory │      └────────────────────────┘      │
│  └───────────────────┘                                       │
│           ▲                                                  │
│           │                                                  │
│           │         ┌────────────────────────┐              │
│           └────────►│ Intelligence Gathering │              │
│                     │ System                 │              │
│                     │                        │              │
│                     │ - Colony Scan Jobs     │              │
│                     │ - Intel Categories     │              │
│                     │ - Transmission Events  │              │
│                     └────────────────────────┘              │
│                                                               │
│  ┌───────────────────┐      ┌────────────────────────┐      │
│  │ False Accusation  │      │ Social Manipulation    │      │
│  │ System            │◄────►│ System                 │      │
│  │                   │      │                        │      │
│  │ - Grudge Reports  │      │ - Accomplice Recruit   │      │
│  │ - Frame-Ups       │      │ - Mood Vulnerability   │      │
│  │ - Evidence Plant  │      │ - Sabotage Actions     │      │
│  └───────────────────┘      └────────────────────────┘      │
│           ▲                          ▲                       │
│           │                          │                       │
│           └──────────┬───────────────┘                       │
│                      │                                       │
│              ┌───────┴────────┐                              │
│              │ Witness        │                              │
│              │ Reliability    │                              │
│              │ System         │                              │
│              │                │                              │
│              │ - Credibility  │                              │
│              │ - Bias Tracking│                              │
│              │ - Accuracy     │                              │
│              └────────────────┘                              │
│                                                               │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              Existing Phase 1-5 Foundation                   │
├─────────────────────────────────────────────────────────────┤
│  • Crime State System (Hidden/Suspected/Convicted)          │
│  • CriminalCase Management                                  │
│  • FogOfWar Witness Detection                               │
│  • Interrogation System                                     │
│  • Evidence Class (Physical/Circumstantial/Testimonial)     │
│  • Punishment System                                        │
│  • Justice UI                                               │
└─────────────────────────────────────────────────────────────┘
```

### System Dependencies

**Phase 6 Requires:**
- ✅ Phase 1: Crime State System (Hidden/Suspected/Convicted)
- ✅ Phase 2: FogOfWar Integration (witness detection)
- ✅ Phase 3: Justice UI (case display)
- ✅ Phase 4: Punishment System (conviction consequences)
- ✅ Phase 5: Investigation & Interrogation

**Phase 6 Extends:**
- `Crime` class → Add `isFalseAccusation`, `actualPerpetrator`, `plantedEvidence`
- `Evidence` class → Add `isPlanted`, `planter` fields
- `CriminalCase` class → Add `falseAccusationRevealed`, `realCulprit`
- `InterrogationSystem` → Add false accusation detection
- `MainTabWindow_Justice` → Display false accusation indicators

**Phase 6 Adds:**
- 15+ new C# classes
- 3 new WorldComponents
- 2 new MapComponents
- 10+ new Defs (ThingDefs for clues, JobDefs for espionage)
- 50+ translation keys

---

## Core Systems

### 4.1 Hidden Identity System

#### Purpose
Allow sanguophages and infiltrators to conceal their true nature, creating mystery and investigation opportunities.

#### Components

**4.1.1: HiddenIdentity Class**

```csharp
public class HiddenIdentity : IExposable
{
    // Display identity (what player sees)
    public string displayName;           // Fake name shown to player
    public string displayBackstory;      // Fabricated background
    public List<string> maskedTraits;    // Traits hidden from inspection
    public bool maskSanguophageStatus;   // Hide bloodsucker gene

    // True identity (hidden until discovered)
    public string realName;              // Actual name
    public string realBackstory;         // True background
    public Faction realFaction;          // Hostile faction affiliation
    public PawnKindDef realKind;         // Actual pawn type

    // Discovery tracking
    public bool identityRevealed;        // Has player discovered the truth?
    public int discoveryProgress;        // 0-100%, investigation uncovers truth
    public List<string> discoveredClues; // Which clues have been found

    // Behavior intelligence
    public float intelligenceStat;       // 0.0-1.0, how smart this infiltrator is

    public void ExposeData() { /* ... */ }
}
```

**4.1.2: CompHiddenIdentity**

```csharp
public class CompHiddenIdentity : ThingComp
{
    private HiddenIdentity identity;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        // Generate fake identity when pawn spawns
        if (!respawningAfterLoad && ShouldHaveHiddenIdentity(parent as Pawn))
        {
            identity = GenerateFakeIdentity();
        }
    }

    public string GetDisplayName()
    {
        return identity?.identityRevealed == false
            ? identity.displayName
            : (parent as Pawn).Name.ToStringShort;
    }

    public void ProgressDiscovery(float amount, string clueType)
    {
        // Investigation uncovers truth progressively
        identity.discoveryProgress += amount;
        identity.discoveredClues.Add(clueType);

        if (identity.discoveryProgress >= 100f)
        {
            RevealIdentity();
        }
    }

    private void RevealIdentity()
    {
        identity.identityRevealed = true;

        // Dramatic reveal event!
        TriggerRevealEvent();
    }
}
```

**4.1.3: Fake Identity Generation**

Infiltrators generate convincing fake identities:

```csharp
private HiddenIdentity GenerateFakeIdentity()
{
    var identity = new HiddenIdentity();

    // Generate believable fake name
    identity.displayName = NameGenerator.GenerateInnocentName();

    // Create fake backstory (matches colony culture)
    identity.displayBackstory = BackstoryGenerator.GenerateCoverStory(
        pawn.story.Childhood.identifier,
        desiredSkills: new[] { "Social", "Crafting" } // Innocent skills
    );

    // Mask sanguophage gene
    identity.maskSanguophageStatus = pawn.genes?.HasActiveGene(GeneDefOf.Bloodfeeder) ?? false;

    // Hide suspicious traits
    identity.maskedTraits = new List<string>();
    if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
        identity.maskedTraits.Add("Psychopath");
    if (pawn.story.traits.HasTrait(TraitDefOf.Bloodlust))
        identity.maskedTraits.Add("Bloodlust");

    // Intelligence determines how well they hide
    identity.intelligenceStat = CalculateIntelligence(pawn);

    return identity;
}

private float CalculateIntelligence(Pawn pawn)
{
    float intel = 0.5f; // Base

    // Social skill affects deception
    intel += pawn.skills.GetSkill(SkillDefOf.Social).Level * 0.02f;

    // Intellectual skill affects planning
    intel += pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * 0.015f;

    // Traits modify intelligence
    if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
        intel += 0.1f; // Cold, calculating
    if (pawn.story.traits.HasTrait(TraitDefOf.SlowLearner))
        intel -= 0.2f; // Less effective

    return Mathf.Clamp01(intel);
}
```

#### Discovery Mechanics

**Clue Types That Reveal Identity:**
1. **Behavioral Clues** (5-10% discovery each)
   - Never seen eating normal food
   - Always works night shift, sleeps during day
   - Avoids social gatherings
   - No relationships formed after 30+ days

2. **Investigation Clues** (15-25% discovery each)
   - Interrogation reveals inconsistencies
   - Background check fails (fake backstory doesn't match)
   - Witness recognizes from hostile faction
   - Blood analysis reveals sanguophage gene

3. **Crime Clues** (30-50% discovery each)
   - Caught feeding on colonist (if sanguophage)
   - Spotted planting evidence
   - Intercepted intelligence transmission
   - Sabotage witnessed directly

**Discovery Progress UI:**
```
Hidden Identity Investigation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Progress: 65%

Discovered Clues:
 • Never eats during day (Behavioral)
 • Background verification failed (Investigation)
 • Spotted near intelligence cache (Crime)

Estimated Revelation: 2-3 more clues needed
```

#### Integration with UI

**Modified Pawn Inspection:**
- Name displays as `displayName` until revealed
- Backstory shows `displayBackstory` until revealed
- Genes hidden if `maskSanguophageStatus == true`
- Traits filtered by `maskedTraits` list

**Reveal Event:**
```csharp
private void TriggerRevealEvent()
{
    Pawn infiltrator = parent as Pawn;

    // Dramatic letter
    Find.LetterStack.ReceiveLetter(
        "LetterLabelInfiltratorRevealed".Translate(),
        "LetterInfiltratorRevealed".Translate(
            identity.displayName,
            identity.realName,
            infiltrator.Named("INFILTRATOR")
        ),
        LetterDefOf.ThreatBig,
        infiltrator,
        null, null, null, null
    );

    // Sound effect
    SoundDefOf.Pawn_InfiltratorRevealed.PlayOneShotOnCamera();

    // Random reaction based on intelligence
    ChooseReaction(infiltrator);
}

private void ChooseReaction(Pawn infiltrator)
{
    // Smart infiltrators have more options
    var reactions = new List<InfiltratorReaction>
    {
        InfiltratorReaction.FleeMap,      // Always available
        InfiltratorReaction.FightToTheend // Always available
    };

    if (identity.intelligenceStat > 0.6f)
    {
        reactions.Add(InfiltratorReaction.FakeInnocence);  // "This is a mistake!"
        reactions.Add(InfiltratorReaction.AttemptBribery); // "I'll tell you everything!"
    }

    if (identity.intelligenceStat > 0.8f)
    {
        reactions.Add(InfiltratorReaction.ActivateAccomplices); // Sabotage NOW
        reactions.Add(InfiltratorReaction.TransmitIntelNow);    // Emergency transmission
    }

    var chosen = reactions.RandomElement();
    ExecuteReaction(infiltrator, chosen);
}
```

---

### 4.2 Clue & Evidence System

#### Purpose
Physical evidence left at crime scenes that can be discovered, studied, and linked to cases. Inspired by Anomaly DLC's Sightstealer mechanics.

#### Components

**4.2.1: CrimeSceneClue Class**

```csharp
// Thing class for physical clues
public class CrimeSceneClue : Thing, IThingStudied
{
    // What crime does this clue relate to?
    public Crime linkedCrime;
    public Pawn linkedCriminal;     // Who left this clue?
    public CrimeType crimeType;

    // Clue properties
    public ClueType clueType;       // Blood, Footprint, Tool Mark, etc.
    public float clueQuality;       // 0-1, how revealing is this clue?
    public bool isPlanted;          // False evidence?
    public Pawn plantedBy;          // Who planted it?

    // Study/discovery system
    public int studyProgress;       // Progress toward full analysis
    public bool analyzed;           // Has this been fully studied?
    public List<ClueRevelation> revelations; // What does this reveal?

    // Visual
    public bool discovered;         // Has player found it yet?
    public int ticksToDecay;        // Clues fade over time
}

public enum ClueType
{
    BloodStain,        // Blood at crime scene
    Footprint,         // Shoe prints
    ToolMark,          // Damage pattern
    DroppedItem,       // Item left behind
    FabricScrap,       // Torn clothing
    FingerprintTrace,  // Touch evidence
    WitnessReport      // Verbal clue from interrogation
}

public class ClueRevelation
{
    public float threshold;         // Study progress needed (0-1)
    public string label;            // "Blood Type Analysis"
    public string description;      // "The blood matches [Pawn]..."
    public float accuracyChance;    // Can be wrong!
    public Pawn pointsTo;           // Who does this implicate?
}
```

**4.2.2: Clue Generation System**

Clues spawn at crime scenes:

```csharp
public static class ClueGenerator
{
    public static void GenerateCluesForCrime(
        Crime crime,
        Pawn criminal,
        IntVec3 location,
        Map map)
    {
        // Intelligence determines if criminal leaves clues
        float clueLikelihood = 1.0f;

        var hiddenIdentity = criminal.TryGetComp<CompHiddenIdentity>();
        if (hiddenIdentity != null)
        {
            // Smart criminals leave fewer clues
            clueLikelihood -= hiddenIdentity.identity.intelligenceStat * 0.5f;
        }

        // Crime type affects clue generation
        int numClues = GetClueCountForCrime(crime.crimeType);

        for (int i = 0; i < numClues; i++)
        {
            if (Rand.Chance(clueLikelihood))
            {
                SpawnClue(crime, criminal, location, map);
            }
        }
    }

    private static void SpawnClue(Crime crime, Pawn criminal, IntVec3 loc, Map map)
    {
        // Determine clue type based on crime
        ClueType type = DetermineClueType(crime.crimeType);

        // Create clue thing
        ThingDef clueDef = GetClueThingDef(type);
        CrimeSceneClue clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);

        // Link to crime
        clue.linkedCrime = crime;
        clue.linkedCriminal = criminal;
        clue.crimeType = crime.crimeType;
        clue.clueType = type;

        // Quality based on circumstances
        clue.clueQuality = CalculateClueQuality(crime, criminal, loc, map);

        // Setup revelations
        clue.revelations = GenerateRevelations(clue, criminal);

        // Spawn in world
        IntVec3 spawnLoc = FindClueSpawnLocation(loc, map);
        GenSpawn.Spawn(clue, spawnLoc, map);
    }

    private static float CalculateClueQuality(Crime crime, Pawn criminal, IntVec3 loc, Map map)
    {
        float quality = 0.5f; // Base

        // Lighting affects quality
        float light = map.glowGrid.GroundGlowAt(loc);
        quality += light * 0.2f;

        // Hasty crimes leave better clues
        if (crime.wasWitnessed)
            quality += 0.2f; // Criminal rushed

        // Intelligence reduces clue quality
        var hiddenId = criminal.TryGetComp<CompHiddenIdentity>();
        if (hiddenId != null)
            quality -= hiddenId.identity.intelligenceStat * 0.3f;

        return Mathf.Clamp01(quality);
    }

    private static List<ClueRevelation> GenerateRevelations(CrimeSceneClue clue, Pawn criminal)
    {
        var revelations = new List<ClueRevelation>();

        // Basic revelation at 25% study
        revelations.Add(new ClueRevelation
        {
            threshold = 0.25f,
            label = $"Initial {clue.clueType} Analysis",
            description = GetBasicDescription(clue.clueType),
            accuracyChance = 0.7f
        });

        // Intermediate at 50%
        revelations.Add(new ClueRevelation
        {
            threshold = 0.50f,
            label = $"Detailed {clue.clueType} Examination",
            description = GetDetailedDescription(clue.clueType, criminal),
            accuracyChance = 0.85f,
            pointsTo = criminal // May be wrong if planted!
        });

        // Complete at 100%
        revelations.Add(new ClueRevelation
        {
            threshold = 1.0f,
            label = $"Complete {clue.clueType} Profile",
            description = GetCompleteDescription(clue.clueType, criminal),
            accuracyChance = clue.isPlanted ? 0.95f : 1.0f, // Still small chance of error
            pointsTo = clue.isPlanted ? clue.plantedBy : criminal
        });

        return revelations;
    }
}
```

**4.2.3: Clue Discovery & Study**

Clues work like Anomaly DLC's study system:

```csharp
// JobGiver for wardens/researchers
public class WorkGiver_AnalyzeClue : WorkGiver_Scanner
{
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        // Find all undiscovered or unstudied clues
        return pawn.Map.listerThings.ThingsOfDef(ThingDefOf.CrimeSceneClue_Blood)
            .Cast<CrimeSceneClue>()
            .Where(c => !c.analyzed);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CrimeSceneClue clue = t as CrimeSceneClue;

        if (clue == null || clue.analyzed)
            return null;

        // Create study job
        return JobMaker.MakeJob(JobDefOf.AnalyzeCrimeClue, clue);
    }
}

// Job to analyze clues
public class JobDriver_AnalyzeClue : JobDriver
{
    private const int StudyDuration = 600; // 10 seconds base

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil study = new Toil();
        study.tickAction = delegate()
        {
            CrimeSceneClue clue = (CrimeSceneClue)job.targetA.Thing;

            // Study progress based on intellectual skill
            int intellectSkill = pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;
            float progressRate = 1f + (intellectSkill * 0.1f);

            clue.studyProgress += (int)progressRate;

            // Check for revelations
            CheckRevelations(clue, pawn);

            if (clue.studyProgress >= 100)
            {
                clue.analyzed = true;
                ReadyForNextToil();
            }
        };
        study.defaultCompleteMode = ToilCompleteMode.Never;
        study.WithProgressBar(TargetIndex.A, () =>
            ((CrimeSceneClue)job.targetA.Thing).studyProgress / 100f);
        study.PlaySustainerOrSound(SoundDefOf.AnalyzeClue);

        yield return study;
    }

    private void CheckRevelations(CrimeSceneClue clue, Pawn analyst)
    {
        float progress = clue.studyProgress / 100f;

        foreach (var revelation in clue.revelations)
        {
            if (progress >= revelation.threshold && !clue.revealedNotes.Contains(revelation))
            {
                clue.revealedNotes.Add(revelation);
                RevealClueInformation(clue, revelation, analyst);
            }
        }
    }

    private void RevealClueInformation(CrimeSceneClue clue, ClueRevelation revelation, Pawn analyst)
    {
        // Roll accuracy check
        bool accurate = Rand.Chance(revelation.accuracyChance);

        Pawn pointsTo = accurate ? revelation.pointsTo : GetRandomInnocentPawn();

        // Create message
        Messages.Message(
            "ClueRevelation".Translate(
                analyst.LabelShort,
                revelation.label,
                clue.clueType.ToString(),
                revelation.description
            ),
            clue,
            MessageTypeDefOf.PositiveEvent
        );

        // Link clue to case
        if (pointsTo != null)
        {
            LinkClueToCase(clue, pointsTo);
        }
    }
}
```

**4.2.4: Evidence Planting**

Smart criminals plant false evidence:

```csharp
public static class EvidencePlanter
{
    public static void PlantFalseEvidence(
        Pawn infiltrator,
        Crime crime,
        Pawn scapegoat,
        IntVec3 location,
        Map map)
    {
        // Only intelligent criminals attempt this
        var hiddenId = infiltrator.TryGetComp<CompHiddenIdentity>();
        if (hiddenId == null || hiddenId.identity.intelligenceStat < 0.6f)
            return;

        // Generate fake clue
        ClueType type = ClueGenerator.DetermineClueType(crime.crimeType);
        ThingDef clueDef = ClueGenerator.GetClueThingDef(type);
        CrimeSceneClue fakeClue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);

        // Point to scapegoat
        fakeClue.linkedCrime = crime;
        fakeClue.linkedCriminal = scapegoat;  // WRONG!
        fakeClue.crimeType = crime.crimeType;
        fakeClue.clueType = type;
        fakeClue.isPlanted = true;
        fakeClue.plantedBy = infiltrator;

        // High quality fake (smart criminal)
        fakeClue.clueQuality = 0.8f + (hiddenId.identity.intelligenceStat * 0.2f);

        // Revelations point to scapegoat
        fakeClue.revelations = new List<ClueRevelation>
        {
            new ClueRevelation
            {
                threshold = 0.5f,
                label = $"{type} Analysis",
                description = $"Evidence matches {scapegoat.LabelShort}",
                accuracyChance = 0.9f, // Very convincing!
                pointsTo = scapegoat
            }
        };

        // Spawn fake evidence
        GenSpawn.Spawn(fakeClue, location, map);

        // Log for dev/debug
        ModLog.Debug($"Infiltrator {infiltrator.LabelShort} planted false {type} implicating {scapegoat.LabelShort}");
    }
}
```

---

### 4.3 Intelligence Gathering System

#### Purpose
Infiltrators scan the colony to gather tactical intelligence, which hostile factions use to conduct smarter, more devastating raids.

#### Components

**4.3.1: IntelligenceData Class**

```csharp
public class IntelligenceData : IExposable
{
    // Categories of intelligence
    public ColonyDefenseIntel defenseIntel;
    public ColonyWealth wealthIntel;
    public ColonyScheduleIntel scheduleIntel;
    public ColonyLayoutIntel layoutIntel;

    // Metadata
    public Pawn gatheredBy;
    public int tickGathered;
    public bool transmitted; // Has this been sent to faction?

    public void ExposeData() { /* ... */ }
}

// Defense intelligence
public class ColonyDefenseIntel
{
    public int turretCount;
    public List<IntVec3> turretLocations;
    public int colonistCount;
    public float avgColonistCombatSkill;
    public List<string> colonistWeapons;
    public bool hasKillbox;
    public IntVec3 killboxLocation;

    // Cap: Can only gather some info per visit
    public float completeness; // 0-1, how complete is this intel?
}

// Wealth intelligence
public class ColonyWealthIntel
{
    public float estimatedWealth;
    public List<IntVec3> valuableStorageLocations;
    public List<ThingDef> highValueItems;
    public IntVec3 silverStorageLocation;

    public float completeness;
}

// Schedule intelligence
public class ColonyScheduleIntel
{
    public Dictionary<Pawn, List<int>> colonistSchedules; // Hour -> Where they'll be
    public int guardsOnNightShift;
    public int guardsOnDayShift;
    public List<IntVec3> unmanned IntervalTimes;

    public float completeness;
}

// Layout intelligence
public class ColonyLayoutIntel
{
    public IntVec3 prisonerBarracksLocation;
    public IntVec3 hospitalLocation;
    public IntVec3 bedroomsLocation;
    public IntVec3 powerGrid CentralNode;
    public List<IntVec3> criticalInfrastructure;

    public float completeness;
}
```

**4.3.2: Intelligence Gathering Jobs**

Infiltrators gather intel through jobs:

```csharp
// WorkGiver for infiltrators
public class WorkGiver_GatherIntelligence : WorkGiver
{
    public override Job NonScanJob(Pawn pawn)
    {
        // Only infiltrators gather intelligence
        var hiddenId = pawn.TryGetComp<CompHiddenIdentity>();
        if (hiddenId == null || hiddenId.identity.identityRevealed)
            return null;

        // Check if current intel is complete
        var intelComp = Find.World.GetComponent<WorldComponent_IntelligenceNetwork>();
        var currentIntel = intelComp.GetIntelForInfiltrator(pawn);

        if (currentIntel.IsComplete())
            return null; // Already gathered everything

        // Choose next intel category to gather
        IntelCategory nextCategory = DetermineNextIntelCategory(currentIntel);
        IntVec3 targetLocation = FindIntelGatherLocation(pawn, nextCategory);

        return JobMaker.MakeJob(JobDefOf.GatherIntelligence, targetLocation);
    }

    private IntelCategory DetermineNextIntelCategory(IntelligenceData intel)
    {
        // Prioritize by usefulness
        if (intel.defenseIntel.completeness < 1.0f)
            return IntelCategory.Defense; // Most important
        if (intel.wealthIntel.completeness < 1.0f)
            return IntelCategory.Wealth;  // High value targets
        if (intel.scheduleIntel.completeness < 1.0f)
            return IntelCategory.Schedule; // Timing info
        if (intel.layoutIntel.completeness < 1.0f)
            return IntelCategory.Layout;   // Least critical

        return IntelCategory.None; // All complete
    }
}

// Job driver for gathering intel
public class JobDriver_GatherIntelligence : JobDriver
{
    private const int ObservationTime = 300; // 5 seconds per observation
    private const float IntelPerGather = 0.15f; // 15% per successful gather

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

        Toil observe = new Toil();
        observe.initAction = delegate()
        {
            observe.ticksLeftThisToil = ObservationTime;
        };
        observe.tickAction = delegate()
        {
            // Act casual - no one suspects you're a spy!
            if (pawn.IsHashIntervalTick(60))
            {
                MoteMaker.ThrowMetaIcon(pawn.Position, pawn.Map, ThingDefOf.Mote_ThoughtBubble);
            }
        };
        observe.defaultCompleteMode = ToilCompleteMode.Delay;
        observe.defaultDuration = ObservationTime;
        observe.AddFinishAction(delegate()
        {
            GatherIntelligence();
        });
        observe.socialMode = RandomSocialMode.Off; // Don't chat while spying

        yield return observe;
    }

    private void GatherIntelligence()
    {
        var intelComp = Find.World.GetComponent<WorldComponent_IntelligenceNetwork>();
        var intel = intelComp.GetIntelForInfiltrator(pawn);

        // What category are we gathering?
        IntelCategory category = DetermineCategory(job.targetA.Cell);

        // Intelligence stat affects how much info gathered
        var hiddenId = pawn.TryGetComp<CompHiddenIdentity>();
        float intelMult = 1.0f + hiddenId.identity.intelligenceStat;
        float gathered = IntelPerGather * intelMult;

        // Update intel
        switch (category)
        {
            case IntelCategory.Defense:
                GatherDefenseIntel(intel.defenseIntel, gathered);
                break;
            case IntelCategory.Wealth:
                GatherWealthIntel(intel.wealthIntel, gathered);
                break;
            case IntelCategory.Schedule:
                GatherScheduleIntel(intel.scheduleIntel, gathered);
                break;
            case IntelCategory.Layout:
                GatherLayoutIntel(intel.layoutIntel, gathered);
                break;
        }

        ModLog.Debug($"{pawn.LabelShort} gathered {gathered*100}% {category} intelligence");
    }

    private void GatherDefenseIntel(ColonyDefenseIntel intel, float amount)
    {
        // Scan for turrets
        var turrets = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
            .Where(t => t.def.building?.IsTurret ?? false);

        intel.turretCount = turrets.Count();
        intel.turretLocations.AddRange(turrets.Select(t => t.Position).Take(3)); // Cap: Only 3 per visit

        // Count colonists
        intel.colonistCount = pawn.Map.mapPawns.FreeColonistsSpawnedCount;

        // Check for killbox
        if (amount >= 0.5f) // Requires thorough observation
        {
            intel.hasKillbox = DetectKillbox(pawn.Map, out IntVec3 killboxLoc);
            intel.killboxLocation = killboxLoc;
        }

        intel.completeness += amount;
        intel.completeness = Mathf.Clamp01(intel.completeness);
    }
}
```

**4.3.3: Intelligence Transmission**

Intel is sent when infiltrator leaves map:

```csharp
[HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
public class IntelligenceTransmission_Patch
{
    static void Prefix(Pawn __instance)
    {
        // Is this an infiltrator leaving the map?
        var hiddenId = __instance.TryGetComp<CompHiddenIdentity>();
        if (hiddenId == null)
            return;

        var intelComp = Find.World.GetComponent<WorldComponent_IntelligenceNetwork>();
        var intel = intelComp.GetIntelForInfiltrator(__instance);

        if (intel != null && !intel.transmitted && intel.HasAnyIntel())
        {
            // TRANSMIT INTELLIGENCE!
            TransmitIntelToFaction(__instance, intel);
            intel.transmitted = true;
        }
    }

    static void TransmitIntelToFaction(Pawn infiltrator, IntelligenceData intel)
    {
        Faction hostileFaction = infiltrator.Faction;

        // Calculate raid buff based on intel completeness
        float intelQuality = CalculateOverallIntelQuality(intel);

        // Apply intelligence to faction
        var factionComp = hostileFaction.GetComponent<FactionComp_RaidIntelligence>();
        if (factionComp == null)
        {
            factionComp = new FactionComp_RaidIntelligence();
            hostileFaction.AddComponent(factionComp);
        }

        factionComp.ApplyIntelligence(intel);

        // Warn player
        string warning = GenerateIntelWarning(intelQuality);
        Messages.Message(warning, MessageTypeDefOf.ThreatBig);

        ModLog.Warning($"Infiltrator {infiltrator.LabelShort} transmitted {intelQuality*100}% quality intel to {hostileFaction.Name}");
    }

    static string GenerateIntelWarning(float quality)
    {
        if (quality < 0.3f)
            return "IntelTransmitted_Low".Translate(); // Minor threat increase
        else if (quality < 0.7f)
            return "IntelTransmitted_Moderate".Translate(); // Significant danger
        else
            return "IntelTransmitted_High".Translate(); // SEVERE THREAT
    }
}
```

**4.3.4: Intelligence Usage in Raids**

Hostile factions use gathered intel:

```csharp
public class FactionComp_RaidIntelligence : FactionComp
{
    private IntelligenceData storedIntel;

    public void ApplyIntelligence(IntelligenceData intel)
    {
        storedIntel = intel;
    }

    // Called when faction plans a raid
    public void ModifyRaidStrategy(IncidentParms parms)
    {
        if (storedIntel == null)
            return; // No intel, standard raid

        // DEFENSE INTEL: Avoid killbox, use sappers
        if (storedIntel.defenseIntel.completeness > 0.5f)
        {
            if (storedIntel.defenseIntel.hasKillbox)
            {
                // Force sapper strategy
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackSappers;
                ModLog.Debug("Raid using sappers to avoid killbox (intel-based)");
            }

            // Scale raid size based on defenses
            float defenseMult = 1.0f + (storedIntel.defenseIntel.turretCount * 0.1f);
            parms.points *= defenseMult;
        }

        // WEALTH INTEL: Drop pod onto valuables
        if (storedIntel.wealthIntel.completeness > 0.7f
            && storedIntel.wealthIntel.valuableStorageLocations.Any())
        {
            // Use drop pod assault on high-value storage
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackSmart;
            parms.spawnCenter = storedIntel.wealthIntel.valuableStorageLocations.RandomElement();

            Messages.Message(
                "RaidTargetingValuables".Translate(),
                new LookTargets(parms.spawnCenter, Find.CurrentMap),
                MessageTypeDefOf.ThreatBig
            );

            ModLog.Warning($"Drop pod raid targeting valuables at {parms.spawnCenter}");
        }

        // SCHEDULE INTEL: Attack during low-guard periods
        if (storedIntel.scheduleIntel.completeness > 0.6f)
        {
            int currentHour = GenLocalDate.HourOfDay(Find.CurrentMap);
            if (storedIntel.scheduleIntel.guardsOnNightShift < 2 && currentHour >= 22)
            {
                // Attack at night when guards are few
                parms.raidArrivalMode = PawnsArriveMode IdeDefOf.EdgeWalkIn; // Silent approach
                ModLog.Debug("Night raid during low-guard period (intel-based)");
            }
        }

        // LAYOUT INTEL: Multi-pronged assault
        if (storedIntel.layoutIntel.completeness > 0.8f)
        {
            // Strike critical infrastructure simultaneously
            // (This would require more complex raid system modifications)
            parms.points *= 1.3f; // Bigger, more coordinated raid

            Messages.Message(
                "MultiProngedAssault".Translate(),
                MessageTypeDefOf.ThreatBig
            );
        }
    }
}

// Patch raid incident worker
[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
public class IntelligenceBasedRaids_Patch
{
    static void Prefix(IncidentWorker_RaidEnemy __instance, IncidentParms parms)
    {
        if (parms.faction == null)
            return;

        var factionComp = parms.faction.GetComponent<FactionComp_RaidIntelligence>();
        if (factionComp != null)
        {
            factionComp.ModifyRaidStrategy(parms);
        }
    }
}
```

---

### 4.4 False Accusation System

#### Purpose
Create uncertainty and drama through grudge-based false reports, mistaken witnesses, and intentional frame-ups.

#### Components

**4.4.1: False Witness System**

```csharp
public static class FalseAccusationSystem
{
    private const float GRUDGE_FALSE_REPORT_CHANCE = 0.12f; // 12% when witnessing enemy

    public static void ProcessWitnessReport(
        Pawn witness,
        Crime crime,
        Pawn suspectedCriminal,
        float evidenceStrength)
    {
        // Check if witness might file false report
        bool isFalseReport = ShouldFileF alseReport(witness, suspectedCriminal, crime);

        if (isFalseReport)
        {
            FileFalseReport(witness, crime, suspectedCriminal);
        }
        else
        {
            // Normal accurate report
            FileAccurateReport(witness, crime, suspectedCriminal, evidenceStrength);
        }
    }

    private static bool ShouldFileFalseReport(Pawn witness, Pawn suspect, Crime crime)
    {
        // Can't file false report if actually witnessed it!
        if (crime.witnesses.Contains(witness))
            return false;

        // Check for grudge
        float opinion = witness.relations?.OpinionOf(suspect) ?? 0f;
        if (opinion < -20)
        {
            // Strong negative opinion increases false report chance
            float grudgeChance = GRUDGE_FALSE_REPORT_CHANCE;
            grudgeChance += Mathf.Abs(opinion) / 500f; // Very low opinion = higher chance

            if (Rand.Chance(grudgeChance))
            {
                ModLog.Debug($"{witness.LabelShort} filing FALSE report against {suspect.LabelShort} due to grudge");
                return true;
            }
        }

        // Check if witness is themselves an infiltrator
        var hiddenId = witness.TryGetComp<CompHiddenIdentity>();
        if (hiddenId != null && !hiddenId.identity.identityRevealed)
        {
            // Infiltrators file false reports to sow chaos
            float chaosChance = hiddenId.identity.intelligenceStat * 0.15f;
            if (Rand.Chance(chaosChance))
            {
                ModLog.Debug($"Infiltrator {witness.LabelShort} filing false report to create chaos");
                return true;
            }
        }

        return false;
    }

    private static void FileFalseReport(Pawn witness, Crime realCrime, Pawn innocent)
    {
        // Create FALSE crime instance
        var falseCrime = new Crime
        {
            crimeType = realCrime.crimeType,
            tickCommitted = realCrime.tickCommitted,
            victim = realCrime.victim,
            damageDealt = realCrime.damageDealt,

            // KEY: Wrong perpetrator!
            visibilityState = CrimeVisibilityState.Suspected,
            witnesses = new List<Pawn> { witness },
            evidenceStrength = 0.6f, // Seems credible

            // Mark as false accusation
            isFalseAccusation = true,
            actualPerpetrator = realCrime.perpetrator, // Track real criminal
            falselyAccused = innocent
        };

        // Add to innocent pawn's record
        CrimeUtils.AddCrimeToRecord(innocent, falseCrime);

        // Create/amend case
        var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
        var criminalCase = justiceManager.GetOrCreateCase(innocent);
        criminalCase.AddCrime(falseCrime);

        // Notification
        Messages.Message(
            "FalseAccusationFiled".Translate(
                innocent.LabelShort,
                falseCrime.crimeType.ToString(),
                witness.LabelShort
            ),
            innocent,
            MessageTypeDefOf.NegativeEvent
        );

        ModLog.Warning($"FALSE ACCUSATION: {innocent.LabelShort} accused of {falseCrime.crimeType} by {witness.LabelShort}");
    }
}
```

**4.4.2: Sanguophage Frame-Ups**

```csharp
public static class SanguophageFrameUpSystem
{
    public static void OnSanguophageKill(Pawn sanguophage, Pawn victim, IntVec3 location, Map map)
    {
        var hiddenId = sanguophage.TryGetComp<CompHiddenIdentity>();
        if (hiddenId == null)
            return; // Not hiding identity

        // Does sanguophage attempt frame-up?
        bool attemptFrameUp = DecideFrameUpAttempt(hiddenId.identity.intelligenceStat);

        if (!attemptFrameUp)
        {
            // Just leave, don't frame anyone
            RecordHiddenCrime(sanguophage, victim, location);
            return;
        }

        // Choose scapegoat
        Pawn scapegoat = ChooseScapegoat(map, location);
        if (scapegoat == null)
        {
            // No good scapegoat, flee
            RecordHiddenCrime(sanguophage, victim, location);
            return;
        }

        // FRAME THE SCAPEGOAT
        FrameInnocentPawn(sanguophage, victim, scapegoat, location, map);
    }

    private static bool DecideFrameUpAttempt(float intelligence)
    {
        // Smart sanguophages always try
        if (intelligence > 0.7f)
            return true;

        // Dumb ones sometimes just flee
        return Rand.Chance(intelligence);
    }

    private static Pawn ChooseScapegoat(Map map, IntVec3 crimeLoc)
    {
        // Find colonists who could plausibly be blamed
        var candidates = map.mapPawns.FreeColonists
            .Where(p => p.Position.InHorDistOf(crimeLoc, 30f)) // Nearby
            .Where(p => !p.Downed) // Conscious
            .ToList();

        if (!candidates.Any())
            return null;

        // Prefer colonists with negative traits
        var suspects = candidates
            .OrderByDescending(p => GetSuspicionScore(p))
            .ToList();

        return suspects.FirstOrDefault();
    }

    private static float GetSuspicionScore(Pawn pawn)
    {
        float score = 0f;

        // Negative traits make better scapegoats
        if (pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
            score += 0.3f;
        if (pawn.story.traits.HasTrait(TraitDefOf.Bloodlust))
            score += 0.25f;
        if (pawn.story.traits.HasTrait(TraitDefOf.Abrasive))
            score += 0.15f;

        // Low social standing
        int friendCount = pawn.relations.PotentiallyRelatedPawns.Count(r =>
            pawn.relations.OpinionOf(r) > 20);
        score += (5 - friendCount) * 0.05f; // Fewer friends = more suspicious

        return score;
    }

    private static void FrameInnocentPawn(
        Pawn sanguophage,
        Pawn victim,
        Pawn scapegoat,
        IntVec3 location,
        Map map)
    {
        // Create murder crime pointing to scapegoat
        var falseCrime = new Crime
        {
            crimeType = CrimeType.Murder,
            tickCommitted = Find.TickManager.TicksGame,
            victim = victim,
            damageDealt = victim.health.hediffSet.GetHediffs<Hediff_Injury>()
                .Sum(h => h.Severity),

            // Frame-up details
            visibilityState = CrimeVisibilityState.Suspected,
            witnesses = new List<Pawn> { sanguophage }, // Sanguophage is "witness"!
            evidenceStrength = 0.75f, // Very convincing

            isFalseAccusation = true,
            actualPerpetrator = sanguophage,
            falselyAccused = scapegoat
        };

        // Add to scapegoat's record
        CrimeUtils.AddCrimeToRecord(scapegoat, falseCrime);

        // Create case
        var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
        var criminalCase = justiceManager.GetOrCreateCase(scapegoat);
        criminalCase.AddCrime(falseCrime);

        // Plant evidence!
        EvidencePlanter.PlantFalseEvidence(sanguophage, falseCrime, scapegoat, location, map);

        // Sanguophage acts as witness
        Messages.Message(
            "SanguophageFrameUp".Translate(
                sanguophage.LabelShort,
                scapegoat.LabelShort,
                victim.LabelShort
            ),
            scapegoat,
            MessageTypeDefOf.ThreatBig
        );

        ModLog.Critical($"FRAME-UP: Sanguophage {sanguophage.LabelShort} framed {scapegoat.LabelShort} for murder of {victim.LabelShort}");

        // Record actual crime as Hidden
        var realCrime = new Crime
        {
            crimeType = CrimeType.Murder,
            tickCommitted = Find.TickManager.TicksGame,
            victim = victim,
            visibilityState = CrimeVisibilityState.Hidden, // No one knows
            isFalseAccusation = false
        };
        CrimeUtils.AddCrimeToRecord(sanguophage, realCrime);
    }
}
```

**4.4.3: Truth Discovery System**

Investigation can reveal false accusations:

```csharp
public static class TruthDiscoverySystem
{
    private const float TRUTH_DISCOVERY_BASE_CHANCE = 0.15f;

    public static void CheckForTruthDiscovery(CriminalCase criminalCase, Pawn investigator)
    {
        // Check each crime for false accusation
        var falseCrimes = criminalCase.crimes
            .Where(c => c.isFalseAccusation && c.visibilityState == CrimeVisibilityState.Suspected)
            .ToList();

        foreach (var crime in falseCrimes)
        {
            bool discovered = AttemptTruthDiscovery(crime, investigator);

            if (discovered)
            {
                RevealTruth(crime, criminalCase);
            }
        }
    }

    private static bool AttemptTruthDiscovery(Crime crime, Pawn investigator)
    {
        float discoveryChance = TRUTH_DISCOVERY_BASE_CHANCE;

        // Investigator's intellectual skill helps
        int intellectSkill = investigator.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        discoveryChance += intellectSkill * 0.02f;

        // Social skill for reading lies
        int socialSkill = investigator.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
        discoveryChance += socialSkill * 0.015f;

        // Quality of evidence affects discovery
        if (crime.plantedEvidence != null && crime.plantedEvidence.Any())
        {
            // Planted evidence can be detected
            float avgPlantQuality = crime.plantedEvidence.Average(e => e.clueQuality);
            discoveryChance += (1.0f - avgPlantQuality) * 0.3f; // Poor fakes easier to spot
        }

        // Multiple clues pointing to different people is suspicious
        if (crime.evidence.Count > 3)
        {
            discoveryChance += 0.1f;
        }

        return Rand.Chance(discoveryChance);
    }

    private static void RevealTruth(Crime falseCrime, CriminalCase falseCase)
    {
        Pawn innocent = falseCase.accused;
        Pawn realCriminal = falseCrime.actualPerpetrator;

        // Dismiss false charges
        falseCrime.visibilityState = CrimeVisibilityState.Dismissed;
        falseCase.Dismiss();

        // Reveal real criminal's crime
        var realCrime = realCriminal.GetComp<CompCrimeRecord>()
            .GetHiddenCrimes()
            .FirstOrDefault(c => c.tickCommitted == falseCrime.tickCommitted);

        if (realCrime != null)
        {
            realCrime.TransitionToSuspected(falseCrime.witnesses, 0.9f);

            // Create case against real criminal
            var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            var realCase = justiceManager.GetOrCreateCase(realCriminal);
            realCase.AddCrime(realCrime);
        }

        // DRAMATIC REVEAL!
        Find.LetterStack.ReceiveLetter(
            "LetterLabelTruthRevealed".Translate(),
            "LetterTruthRevealed".Translate(
                innocent.LabelShort,
                realCriminal.LabelShort,
                falseCrime.crimeType.ToString()
            ),
            LetterDefOf.PositiveEvent,
            new LookTargets(new[] { innocent, realCriminal })
        );

        // Social consequences
        ApplyTruthRevealOpinions(innocent, realCriminal, falseCrime);

        ModLog.Message($"TRUTH REVEALED: {innocent.LabelShort} exonerated, {realCriminal.LabelShort} exposed");
    }

    private static void ApplyTruthRevealOpinions(Pawn innocent, Pawn realCriminal, Crime crime)
    {
        // Colonists feel bad about false accusation
        foreach (var colonist in innocent.Map.mapPawns.FreeColonists)
        {
            // Sympathy for innocent
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.InnocentlyAccused,
                innocent
            );

            // Anger at real criminal
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.FramedInnocent,
                realCriminal
            );
        }
    }
}
```

---

*(Document continues with remaining systems: 4.5 Social Manipulation & Accomplice System, 4.6 Witness Reliability System, 4.7 Social Relationship Impact System, followed by Integration Points, Data Structures, Implementation Roadmap, Testing Strategy, Balance & Tuning, and Risk Mitigation)*

---

**[DOCUMENT CONTINUES - This is Part 1 of comprehensive blueprint. Remaining sections to follow in continuation...]**
