# Phase 6 Design Blueprint - Part 2: Remaining Systems & Implementation

**Continuation of Phase_6_Design_Blueprint.md**

---

### 4.5 Social Manipulation & Accomplice System

#### Purpose
Infiltrators manipulate vulnerable colonists into becoming accomplices who sabotage the colony during raids.

#### Components

**4.5.1: Accomplice Recruitment**

```csharp
public class CompAccomplice : ThingComp
{
    public Pawn recruiter;              // Who recruited this accomplice?
    public float manipulationLevel;     // 0-1, how controlled are they?
    public List<string> tasksAssigned;  // What sabotage will they do?
    public bool activatedDuringRaid;    // Have they acted yet?

    public override void CompTick()
    {
        // Check if manipulation is wearing off
        if (manipulationLevel > 0f && Find.TickManager.TicksGame % 60000 == 0) // Daily check
        {
            manipulationLevel -= 0.05f; // Decays 5% per day without reinforcement
        }
    }
}

public static class AccompliceRecruitmentSystem
{
    private const float MANIPULATION_SUCCESS_BASE = 0.10f;

    public static void AttemptRecruitment(Pawn infiltrator, Pawn target)
    {
        var hiddenId = infiltrator.TryGetComp<CompHiddenIdentity>();
        if (hiddenId == null || hiddenId.identity.identityRevealed)
            return; // Can't recruit if exposed

        // Calculate recruitment chance
        float successChance = CalculateRecruitmentChance(infiltrator, target);

        if (Rand.Chance(successChance))
        {
            RecruitAccomplice(infiltrator, target);
        }
        else
        {
            // Failed recruitment might raise suspicion
            if (Rand.Chance(0.1f))
            {
                hiddenId.ProgressDiscovery(5f, "Failed Recruitment");
            }
        }
    }

    private static float CalculateRecruitmentChance(Pawn infiltrator, Pawn target)
    {
        float chance = MANIPULATION_SUCCESS_BASE;

        // Infiltrator's social skill
        int socialSkill = infiltrator.skills.GetSkill(SkillDefOf.Social).Level;
        chance += socialSkill * 0.02f;

        // Target's vulnerabilities
        float targetMood = target.needs.mood.CurLevel;
        if (targetMood < 0.3f)
            chance += 0.2f; // Desperate colonists easier to manipulate
        else if (targetMood < 0.5f)
            chance += 0.1f;

        // Trait susceptibility
        if (target.story.traits.HasTrait(TraitDefOf.Greedy))
            chance += 0.15f;
        if (target.story.traits.HasTrait(TraitDefOf.Jealous))
            chance += 0.12f;
        if (target.story.traits.HasTrait(TraitDefOf.Depressive))
            chance += 0.10f;

        // Backstory susceptibility
        if (HasSusceptibleBackstory(target))
            chance += 0.10f;

        // Relationship with infiltrator
        float opinion = target.relations.OpinionOf(infiltrator);
        if (opinion > 50)
            chance += 0.15f; // Trusts the infiltrator
        else if (opinion < 0)
            chance -= 0.2f; // Dislikes them

        return Mathf.Clamp01(chance);
    }

    private static bool HasSusceptibleBackstory(Pawn pawn)
    {
        // Check for backstories indicating criminal past or rebellious nature
        string childhoodId = pawn.story.Childhood?.identifier ?? "";
        string adulthoodId = pawn.story.Adulthood?.identifier ?? "";

        List<string> susceptibleKeywords = new List<string>
        {
            "Rebel", "Criminal", "Outlaw", "Pirate", "Slave",
            "Betrayed", "Outcast", "Fugitive"
        };

        return susceptibleKeywords.Any(k =>
            childhoodId.Contains(k) || adulthoodId.Contains(k));
    }

    private static void RecruitAccomplice(Pawn infiltrator, Pawn target)
    {
        // Add accomplice comp
        var accompliceComp = target.TryGetComp<CompAccomplice>();
        if (accompliceComp == null)
        {
            // Would need to add comp via XML def attachment
            ModLog.Error("CompAccomplice missing from pawn - check ThingComp defs");
            return;
        }

        accompliceComp.recruiter = infiltrator;
        accompliceComp.manipulationLevel = 0.5f; // Initial manipulation
        accompliceComp.tasksAssigned = AssignSabotageTasks(infiltrator, target);

        Messages.Message(
            "AccompliceRecruited".Translate(
                target.LabelShort,
                infiltrator.LabelShort
            ),
            target,
            MessageTypeDefOf.NegativeEvent
        );

        ModLog.Warning($"{infiltrator.LabelShort} recruited {target.LabelShort} as accomplice");
    }

    private static List<string> AssignSabotageTasks(Pawn infiltrator, Pawn accomplice)
    {
        var tasks = new List<string>();
        var hiddenId = infiltrator.TryGetComp<CompHiddenIdentity>();
        float intelligence = hiddenId?.identity.intelligenceStat ?? 0.5f;

        // Smart infiltrators assign more impactful tasks
        if (intelligence > 0.7f)
        {
            tasks.Add("DisableTurrets");
            tasks.Add("OpenGates");
        }

        if (intelligence > 0.5f)
        {
            tasks.Add("StartFires");
            tasks.Add("BreakEquipment");
        }

        // All accomplices do basic disruption
        tasks.Add("CauseDistraction");

        return tasks;
    }
}
```

**4.5.2: Sabotage Execution**

```csharp
// Triggered during raids
[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
public class AccompliceSabotage_Patch
{
    static void Postfix(bool __result)
    {
        if (!__result)
            return; // Raid didn't spawn

        // Activate all accomplices
        var accomplices = Find.CurrentMap.mapPawns.FreeColonists
            .Select(p => new { Pawn = p, Comp = p.TryGetComp<CompAccomplice>() })
            .Where(x => x.Comp != null && x.Comp.manipulationLevel > 0.3f)
            .ToList();

        foreach (var accomplice in accomplices)
        {
            ActivateAccomplice(accomplice.Pawn, accomplice.Comp);
        }
    }

    static void ActivateAccomplice(Pawn accomplice, CompAccomplice comp)
    {
        if (comp.activatedDuringRaid)
            return; // Already acted

        // Execute assigned sabotage tasks
        foreach (var task in comp.tasksAssigned)
        {
            ExecuteSabotageTask(accomplice, task);
        }

        comp.activatedDuringRaid = true;

        Messages.Message(
            "AccompliceSabotage".Translate(accomplice.LabelShort),
            accomplice,
            MessageTypeDefOf.ThreatBig
        );
    }

    static void ExecuteSabotageTask(Pawn accomplice, string task)
    {
        switch (task)
        {
            case "DisableTurrets":
                DisableNearbyTurrets(accomplice);
                break;
            case "OpenGates":
                OpenNearbyDoors(accomplice);
                break;
            case "StartFires":
                StartFiresNearby(accomplice);
                break;
            case "BreakEquipment":
                DamageEquipment(accomplice);
                break;
            case "CauseDistraction":
                CauseDistraction(accomplice);
                break;
        }
    }

    static void DisableTurrets(Pawn accomplice)
    {
        // Find nearby turrets
        var turrets = GenRadial.RadialDistinctThingsAround(
            accomplice.Position,
            accomplice.Map,
            15f,
            true
        )
        .OfType<Building_TurretGun>()
        .Take(2); // Disable up to 2 turrets

        foreach (var turret in turrets)
        {
            // Damage turret to disable
            turret.TakeDamage(new DamageInfo(
                DamageDefOf.Deterioration,
                turret.HitPoints * 0.7f // 70% damage = disabled
            ));

            ModLog.Warning($"Accomplice {accomplice.LabelShort} disabled turret at {turret.Position}");
        }
    }

    static void OpenNearbyDoors(Pawn accomplice)
    {
        // Find nearby doors
        var doors = GenRadial.RadialDistinctThingsAround(
            accomplice.Position,
            accomplice.Map,
            20f,
            true
        )
        .OfType<Building_Door>()
        .Where(d => d.Faction == Faction.OfPlayer)
        .Take(3);

        foreach (var door in doors)
        {
            // Open and lock open
            door.Open();
            door.StartManualOpenBy(accomplice);

            ModLog.Warning($"Accomplice {accomplice.LabelShort} opened door at {door.Position}");
        }
    }

    static void StartFiresNearby(Pawn accomplice)
    {
        // Start 2-3 fires nearby
        int fireCount = Rand.RangeInclusive(2, 3);
        for (int i = 0; i < fireCount; i++)
        {
            IntVec3 firePos = accomplice.Position + GenRadial.RadialPattern[Rand.Range(1, 9)];
            if (firePos.InBounds(accomplice.Map))
            {
                FireUtility.TryStartFireIn(firePos, accomplice.Map, Rand.Range(0.1f, 0.5f));
                ModLog.Warning($"Accomplice {accomplice.LabelShort} started fire at {firePos}");
            }
        }
    }
}
```

**4.5.3: Accomplice Discovery**

```csharp
public static class AccompliceDiscoverySystem
{
    public static void CheckForDiscovery(Pawn accomplice)
    {
        var comp = accomplice.TryGetComp<CompAccomplice>();
        if (comp == null || comp.recruiter == null)
            return;

        // Caught sabotaging = instant revelation
        if (comp.activatedDuringRaid && Rand.Chance(0.3f))
        {
            RevealAccomplice(accomplice, comp.recruiter);
        }
    }

    private static void RevealAccomplice(Pawn accomplice, Pawn infiltrator)
    {
        // Reveal both accomplice and their recruiter
        Find.LetterStack.ReceiveLetter(
            "LetterLabelAccompliceRevealed".Translate(),
            "LetterAccompliceRevealed".Translate(
                accomplice.LabelShort,
                infiltrator.LabelShort
            ),
            LetterDefOf.ThreatBig,
            new LookTargets(new[] { accomplice, infiltrator })
        );

        // Reveal infiltrator's identity if hidden
        var hiddenId = infiltrator.TryGetComp<CompHiddenIdentity>();
        if (hiddenId != null && !hiddenId.identity.identityRevealed)
        {
            hiddenId.identity.discoveryProgress = 100;
            hiddenId.ProgressDiscovery(0f, "Accomplice Revealed");
        }

        // Social consequences
        ApplyAccompliceDiscoveryOpinions(accomplice, infiltrator);
    }

    private static void ApplyAccompliceDiscoveryOpinions(Pawn accomplice, Pawn infiltrator)
    {
        foreach (var colonist in accomplice.Map.mapPawns.FreeColonists)
        {
            if (colonist == accomplice || colonist == infiltrator)
                continue;

            // Betrayal by accomplice
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.BetrayedByAccomplice,
                accomplice
            );

            // Anger at infiltrator
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.InfiltratorManipulated,
                infiltrator
            );
        }
    }
}
```

---

### 4.6 Witness Reliability System

#### Purpose
Track witness credibility based on multiple factors, introducing uncertainty into testimony.

#### Components

**4.6.1: WitnessReliability Class**

```csharp
public class WitnessReliability
{
    public Pawn witness;
    public Crime crime;
    public float reliabilityScore; // 0-1

    // Factors affecting reliability
    public float distanceFactor;      // How far from crime
    public float lightingFactor;      // Lighting conditions
    public float sightCapacityFactor; // Pawn's sight stat
    public float relationshipBias;    // Relationship to accused
    public float intelligenceFactor;  // Intelligence stat

    public string GetReliabilityDescription()
    {
        if (reliabilityScore > 0.8f)
            return "Highly Reliable";
        if (reliabilityScore > 0.6f)
            return "Reliable";
        if (reliabilityScore > 0.4f)
            return "Questionable";
        if (reliabilityScore > 0.2f)
            return "Unreliable";
        return "Very Unreliable";
    }
}

public static class WitnessReliabilityCalculator
{
    public static WitnessReliability CalculateReliability(
        Pawn witness,
        Crime crime,
        IntVec3 crimeLocation,
        Pawn accused)
    {
        var reliability = new WitnessReliability
        {
            witness = witness,
            crime = crime
        };

        // Distance factor (0-1, closer = more reliable)
        float distance = witness.Position.DistanceTo(crimeLocation);
        reliability.distanceFactor = Mathf.Clamp01(1.0f - (distance / 30f));

        // Lighting factor
        float lighting = witness.Map.glowGrid.GroundGlowAt(crimeLocation);
        reliability.lightingFactor = lighting;

        // Sight capacity
        reliability.sightCapacityFactor = witness.health.capacities.GetLevel(
            PawnCapacityDefOf.Sight
        );

        // Relationship bias
        float opinion = witness.relations.OpinionOf(accused);
        if (opinion > 20)
            reliability.relationshipBias = -0.2f; // Friend = unreliable accuser
        else if (opinion < -20)
            reliability.relationshipBias = -0.3f; // Enemy = biased accuser
        else
            reliability.relationshipBias = 0f;

        // Intelligence
        int intellectLevel = witness.skills.GetSkill(SkillDefOf.Intellectual).Level;
        reliability.intelligenceFactor = intellectLevel / 20f; // 0-1 scale

        // Calculate overall score
        reliability.reliabilityScore = CalculateOverallScore(reliability);

        return reliability;
    }

    private static float CalculateOverallScore(WitnessReliability r)
    {
        float score = 0.5f; // Base

        // Weight factors
        score += r.distanceFactor * 0.25f;
        score += r.lightingFactor * 0.20f;
        score += r.sightCapacityFactor * 0.15f;
        score += r.relationshipBias; // Can be negative!
        score += r.intelligenceFactor * 0.10f;

        return Mathf.Clamp01(score);
    }
}
```

**4.6.2: UI Integration**

```csharp
// Add to MainTabWindow_Justice crime display
private void DisplayWitnessReliability(Crime crime, Rect rect)
{
    float y = rect.y;

    Widgets.Label(new Rect(rect.x, y, rect.width, 24f), "Witnesses:");
    y += 26f;

    foreach (var witness in crime.witnesses)
    {
        var reliability = WitnessReliabilityCalculator.CalculateReliability(
            witness,
            crime,
            crime.location,
            crime.perpetrator
        );

        Rect witnessRow = new Rect(rect.x, y, rect.width, 30f);

        // Witness name
        Widgets.Label(
            new Rect(witnessRow.x, witnessRow.y, 150f, 30f),
            witness.LabelShort
        );

        // Reliability bar
        Rect barRect = new Rect(witnessRow.x + 160f, witnessRow.y + 8f, 120f, 14f);
        Widgets.FillableBar(barRect, reliability.reliabilityScore, ReliabilityBarTex, null, false);

        // Reliability label
        Color prevColor = GUI.color;
        GUI.color = GetReliabilityColor(reliability.reliabilityScore);
        Widgets.Label(
            new Rect(witnessRow.x + 290f, witnessRow.y, 120f, 30f),
            reliability.GetReliabilityDescription()
        );
        GUI.color = prevColor;

        // Tooltip with details
        if (Mouse.IsOver(witnessRow))
        {
            TooltipHandler.TipRegion(witnessRow, GetReliabilityTooltip(reliability));
        }

        y += 32f;
    }
}

private string GetReliabilityTooltip(WitnessReliability r)
{
    StringBuilder sb = new StringBuilder();
    sb.AppendLine($"Witness Reliability: {r.reliabilityScore:P0}");
    sb.AppendLine();
    sb.AppendLine($"Distance: {r.distanceFactor:P0}");
    sb.AppendLine($"Lighting: {r.lightingFactor:P0}");
    sb.AppendLine($"Sight: {r.sightCapacityFactor:P0}");
    sb.AppendLine($"Intelligence: {r.intelligenceFactor:P0}");
    if (r.relationshipBias != 0)
        sb.AppendLine($"Relationship Bias: {r.relationshipBias:+0.0;-0.0}");

    return sb.ToString();
}

private Color GetReliabilityColor(float score)
{
    if (score > 0.8f) return Color.green;
    if (score > 0.6f) return Color.yellow;
    if (score > 0.4f) return new Color(1f, 0.6f, 0f); // Orange
    return Color.red;
}
```

---

### 4.7 Social Relationship Impact System

#### Purpose
Crimes and justice decisions ripple through social networks, affecting colony morale and relationships.

#### Components

**4.7.1: Opinion Modifiers**

```xml
<!-- Defs/ThoughtDefs/Thoughts_SocialImpact.xml -->
<Defs>
  <!-- Crime victim thoughts -->
  <ThoughtDef>
    <defName>VictimOfCrime</defName>
    <thoughtClass>Thought_MemorySocial</thoughtClass>
    <stages>
      <li>
        <label>victimized by {0}</label>
        <baseOpinionOffset>-40</baseOpinionOffset>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Witness thoughts -->
  <ThoughtDef>
    <defName>WitnessedCrime</defName>
    <thoughtClass>Thought_MemorySocial</thoughtClass>
    <stages>
      <li>
        <label>saw {0} commit crime</label>
        <baseOpinionOffset>-25</baseOpinionOffset>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Friend of victim -->
  <ThoughtDef>
    <defName>FriendVictimized</defName>
    <thoughtClass>Thought_MemorySocial</thoughtClass>
    <stages>
      <li>
        <label>hurt my friend</label>
        <baseOpinionOffset>-30</baseOpinionOffset>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Friend of perpetrator (sympathy) -->
  <ThoughtDef>
    <defName>FriendAccused</defName>
    <thoughtClass>Thought_MemorySocial</thoughtClass>
    <stages>
      <li>
        <label>accused my friend</label>
        <baseOpinionOffset>-15</baseOpinionOffset>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Unjust conviction witnessed -->
  <ThoughtDef>
    <defName>UnjustConviction</defName>
    <thoughtClass>Thought_Memory</thoughtClass>
    <stages>
      <li>
        <label>unjust conviction</label>
        <description>I witnessed an innocent person being convicted. This colony's justice system is broken.</description>
        <baseMoodEffect>-8</baseMoodEffect>
        <durationDays>30</durationDays>
      </li>
    </stages>
  </ThoughtDef>

  <!-- False accusation sympathy -->
  <ThoughtDef>
    <defName>InnocentlyAccused</defName>
    <thoughtClass>Thought_MemorySocial</thoughtClass>
    <stages>
      <li>
        <label>falsely accused</label>
        <baseOpinionOffset>+20</baseOpinionOffset>
      </li>
    </stages>
  </ThoughtDef>

  <!-- Executed innocent (severe) -->
  <ThoughtDef>
    <defName>ExecutedInnocent</defName>
    <thoughtClass>Thought_Memory</thoughtClass>
    <stages>
      <li>
        <label>innocent executed</label>
        <description>We executed an innocent person. I can't believe this happened.</description>
        <baseMoodEffect>-15</baseMoodEffect>
        <durationDays>60</durationDays>
      </li>
    </stages>
    <stackLimit>5</stackLimit>
    <stackedEffectMultiplier>0.8</stackedEffectMultiplier>
  </ThoughtDef>
</Defs>
```

**4.7.2: Social Impact Application**

```csharp
public static class SocialImpactSystem
{
    public static void ApplyCrimeImpact(Crime crime)
    {
        Pawn criminal = crime.perpetrator;
        Pawn victim = crime.victim;

        // Victim's opinion of criminal
        if (victim != null && !victim.Dead)
        {
            victim.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.VictimOfCrime,
                criminal
            );
        }

        // Witnesses' opinions
        foreach (var witness in crime.witnesses)
        {
            witness.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.WitnessedCrime,
                criminal
            );
        }

        // Friends of victim
        if (victim != null)
        {
            foreach (var friend in GetFriends(victim))
            {
                friend.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtDefOf.FriendVictimized,
                    criminal
                );
            }
        }

        // Friends of criminal (sympathy)
        foreach (var friend in GetFriends(criminal))
        {
            foreach (var accuser in crime.witnesses)
            {
                friend.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtDefOf.FriendAccused,
                    accuser
                );
            }
        }
    }

    public static void ApplyConvictionImpact(CriminalCase criminalCase, Punishment punishment)
    {
        Pawn convicted = criminalCase.accused;

        // Check if this is a false conviction
        bool isFalseConviction = criminalCase.crimes.Any(c => c.isFalseAccusation);

        if (isFalseConviction)
        {
            ApplyFalseConvictionImpact(convicted, punishment);
        }
        else
        {
            ApplyJustConvictionImpact(convicted, punishment);
        }
    }

    private static void ApplyFalseConvictionImpact(Pawn innocent, Punishment punishment)
    {
        // Colony-wide mood hit
        foreach (var colonist in innocent.Map.mapPawns.FreeColonists)
        {
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.UnjustConviction
            );

            // Extra severe if executed
            if (punishment.type == PunishmentType.Execution)
            {
                colonist.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtDefOf.ExecutedInnocent
                );
            }
        }

        // Friends get even worse mood
        foreach (var friend in GetFriends(innocent))
        {
            friend.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtDefOf.ExecutedInnocent
            );
        }
    }

    private static void ApplyJustConvictionImpact(Pawn criminal, Punishment punishment)
    {
        // Victim satisfaction (if alive)
        foreach (var crime in criminal.GetComp<CompCrimeRecord>().GetConvictedCrimes())
        {
            if (crime.victim != null && !crime.victim.Dead)
            {
                crime.victim.needs.mood.thoughts.memories.TryGainMemory(
                    ThoughtDefOf.JusticeServed
                );
            }
        }

        // Severity-appropriate thoughts for colony
        if (punishment.type == PunishmentType.Execution)
        {
            // Some colonists uncomfortable with execution
            foreach (var colonist in criminal.Map.mapPawns.FreeColonists)
            {
                if (colonist.story.traits.HasTrait(TraitDefOf.Kind))
                {
                    colonist.needs.mood.thoughts.memories.TryGainMemory(
                        ThoughtDefOf.ExecutionWitnessed
                    );
                }
            }
        }
    }

    private static List<Pawn> GetFriends(Pawn pawn)
    {
        return pawn.relations.PotentiallyRelatedPawns
            .Where(p => pawn.relations.OpinionOf(p) > 20)
            .ToList();
    }
}
```

---

## 5. Integration Points

### 5.1 Existing Phase 1-5 Integration

**Crime Class Extensions:**
```csharp
// Add to existing Crime.cs
public class Crime
{
    // Existing fields...
    public CrimeType crimeType;
    public int tickCommitted;
    public Pawn victim;
    public CrimeVisibilityState visibilityState;

    // NEW Phase 6 fields
    public bool isFalseAccusation;
    public Pawn actualPerpetrator;      // If false, who really did it?
    public Pawn falselyAccused;         // If false, who was blamed?
    public List<CrimeSceneClue> clues;  // Physical evidence
    public List<CrimeSceneClue> plantedEvidence; // Fake clues
    public IntVec3 location;            // Where crime occurred
}
```

**Evidence Class Extensions:**
```csharp
// Add to existing Evidence.cs
public class Evidence
{
    // Existing fields...
    public EvidenceType type;
    public float reliability;

    // NEW Phase 6 fields
    public bool isPlanted;
    public Pawn plantedBy;
    public List<ClueRevelation> revelations; // Progressive discovery
}
```

**CriminalCase Extensions:**
```csharp
// Add to existing CriminalCase.cs
public class CriminalCase
{
    // Existing fields...
    public Pawn accused;
    public List<Crime> crimes;
    public CaseStatus status;

    // NEW Phase 6 fields
    public bool falseAccusationRevealed;
    public Pawn realCulprit;                // If case was false
    public List<WitnessReliability> witnessReliability;
}
```

### 5.2 UI Integration Points

**MainTabWindow_Justice Enhancements:**
- Add "False Accusation" indicator badge on cases
- Display witness reliability scores
- Show clue evidence section
- Add "Investigate Identity" button for suspicious pawns

**ITab_Pawn_Judiciary Enhancements:**
- Show hidden identity progress bar (if applicable)
- Display accomplice status
- List gathered intelligence (dev mode only)

### 5.3 Settings Integration

```csharp
// Add to LawAndOrderSettings.cs
public class LawAndOrderSettings : ModSettings
{
    // Existing settings...

    // NEW Phase 6 tuning parameters (NO TOGGLES - core features)
    public float falseAccusationRate = 0.12f;        // 12% default (moderate drama)
    public float infiltratorSpawnChance = 0.05f;     // 5% of visitors
    public int maxAccomplicesPerInfiltrator = 2;     // Max colonists manipulated
    public float truthDiscoveryChance = 0.15f;       // Base chance to reveal false accusations
    public int cluesPerCrime_Min = 1;                // Minimum clues spawned
    public int cluesPerCrime_Max = 3;                // Maximum clues spawned
}
```

**Note:** Phase 6 features are **core and non-optional**. These settings allow tuning the intensity/frequency, but cannot disable the systems entirely.

---

## 6. Data Structures

### 6.1 New Classes Summary

| Class | Purpose | File Location |
|-------|---------|---------------|
| `HiddenIdentity` | Store fake identity data | `Infiltration/HiddenIdentity.cs` |
| `CompHiddenIdentity` | ThingComp for infiltrators | `Infiltration/CompHiddenIdentity.cs` |
| `CrimeSceneClue` | Physical evidence Thing | `Evidence/CrimeSceneClue.cs` |
| `ClueRevelation` | Progressive discovery data | `Evidence/ClueRevelation.cs` |
| `IntelligenceData` | Gathered spy intel | `Intelligence/IntelligenceData.cs` |
| `ColonyDefenseIntel` | Defense info category | `Intelligence/IntelData.cs` |
| `ColonyWealthIntel` | Wealth info category | `Intelligence/IntelData.cs` |
| `WitnessReliability` | Credibility tracking | `Witnesses/WitnessReliability.cs` |
| `CompAccomplice` | Manipulated colonist | `Infiltration/CompAccomplice.cs` |
| `FactionComp_RaidIntelligence` | Intel-based raids | `Intelligence/FactionComp_RaidIntelligence.cs` |

### 6.2 New WorldComponents

| Component | Purpose |
|-----------|---------|
| `WorldComponent_IntelligenceNetwork` | Track all gathered intelligence |
| `WorldComponent_InfiltrationManager` | Manage active infiltrators |
| `WorldComponent_AccompliceTracker` | Track manipulated colonists |

### 6.3 New MapComponents

| Component | Purpose |
|-----------|---------|
| `MapComponent_ClueTracker` | Track all crime scene clues |
| `MapComponent_InfiltrationActivity` | Monitor suspicious behavior |

---

## 7. Implementation Roadmap

### Phase 6.1: Foundation (Week 1)
**Goal:** Core data structures and basic systems

**Tasks:**
- [ ] Create `HiddenIdentity` class with save/load
- [ ] Implement `CompHiddenIdentity` ThingComp
- [ ] Create `CrimeSceneClue` Thing class
- [ ] Extend `Crime` class with Phase 6 fields
- [ ] Extend `Evidence` class with planting support
- [ ] Add Phase 6 settings to mod settings
- [ ] Create translation keys (50+ keys)

**Deliverables:**
- All Phase 6 data structures compile
- Settings UI shows Phase 6 options
- Save/load tested

### Phase 6.2: Clue & Evidence System (Week 2)
**Goal:** Physical evidence at crime scenes

**Tasks:**
- [ ] Implement `ClueGenerator` system
- [ ] Create clue ThingDefs (BloodStain, Footprint, etc.)
- [ ] Implement clue study/analysis jobs
- [ ] Create `JobDriver_AnalyzeClue`
- [ ] Implement progressive revelation system
- [ ] Add evidence planting mechanics
- [ ] Integrate clues with case UI

**Deliverables:**
- Clues spawn at crime scenes
- Colonists can study clues
- Clues link to criminal cases
- UI shows clue evidence

### Phase 6.3: False Accusation System (Week 2-3)
**Goal:** Grudge-based and sanguophage frame-ups

**Tasks:**
- [ ] Implement grudge-based false reporting
- [ ] Create sanguophage frame-up system
- [ ] Implement truth discovery mechanics
- [ ] Add false accusation UI indicators
- [ ] Create interrogation false accusation detection
- [ ] Implement exoneration system
- [ ] Add social impact for false convictions

**Deliverables:**
- Grudges trigger false reports (12% rate)
- Sanguophages frame innocents
- Investigation can reveal truth
- UI shows false accusation warnings

### Phase 6.4: Hidden Identity System (Week 3)
**Goal:** Infiltrator identity masking

**Tasks:**
- [ ] Implement fake name generation
- [ ] Create backstory fabrication system
- [ ] Implement gene masking (sanguophage)
- [ ] Create discovery progress system
- [ ] Implement behavioral clue detection
- [ ] Create dramatic reveal events
- [ ] Add infiltrator reaction system

**Deliverables:**
- Infiltrators spawn with fake identities
- UI shows fake information
- Investigation reveals true identity
- Dramatic reveals with varied reactions

### Phase 6.5: Intelligence Gathering (Week 4)
**Goal:** Espionage and raid buffs

**Tasks:**
- [ ] Create `IntelligenceData` structures
- [ ] Implement intelligence gathering jobs
- [ ] Create `WorldComponent_IntelligenceNetwork`
- [ ] Implement intel transmission on map exit
- [ ] Create `FactionComp_RaidIntelligence`
- [ ] Implement intel-based raid modifications
- [ ] Add player warnings for espionage

**Deliverables:**
- Infiltrators gather intel
- Intel transmits when leaving map
- Raids use intelligence for tactics
- Player receives espionage warnings

### Phase 6.6: Social Manipulation & Accomplices (Week 4-5)
**Goal:** Accomplice recruitment and sabotage

**Tasks:**
- [ ] Implement accomplice recruitment system
- [ ] Create `CompAccomplice` ThingComp
- [ ] Implement vulnerability calculation
- [ ] Create sabotage task system
- [ ] Implement raid-triggered sabotage
- [ ] Add accomplice discovery mechanics
- [ ] Create social impact for betrayal

**Deliverables:**
- Vulnerable colonists recruited
- Accomplices sabotage during raids
- Discovery reveals both accomplice and infiltrator
- Colony morale impacts

### Phase 6.7: Witness Reliability & Social Impact (Week 5)
**Goal:** Credibility system and relationship cascades

**Tasks:**
- [ ] Implement `WitnessReliability` calculations
- [ ] Add reliability UI to Justice tab
- [ ] Create relationship opinion modifiers
- [ ] Implement social network impact system
- [ ] Add unjust conviction thoughts
- [ ] Create false accusation sympathy system
- [ ] Implement friend/enemy cascades

**Deliverables:**
- Witnesses have reliability scores
- UI shows credibility ratings
- Crimes affect social networks
- Unjust convictions tank morale

### Phase 6.8: Polish & Testing (Week 6)
**Goal:** Bug fixes, balance, and integration testing

**Tasks:**
- [ ] Balance false accusation rates
- [ ] Test infiltrator spawn rates
- [ ] Verify all dramatic reveals working
- [ ] Test accomplice sabotage
- [ ] Balance intel-based raid difficulty
- [ ] Performance profiling
- [ ] Save/load stress testing
- [ ] Documentation updates

**Deliverables:**
- All systems working smoothly
- Balanced and engaging
- No performance issues
- Documentation complete

---

## 8. Testing Strategy

### 8.1 Unit Testing

**Hidden Identity:**
- [ ] Fake identity generation creates valid names/backstories
- [ ] Gene masking hides sanguophage status
- [ ] Discovery progress increments correctly
- [ ] Reveal triggers at 100% progress

**Clue System:**
- [ ] Clues spawn at crime locations
- [ ] Study progress advances based on intellectual skill
- [ ] Revelations trigger at correct thresholds
- [ ] Planted evidence points to scapegoat

**False Accusations:**
- [ ] Grudge chance calculates correctly
- [ ] False crimes create separate cases
- [ ] Truth discovery works
- [ ] Exoneration dismisses false charges

**Intelligence:**
- [ ] Intel categories track completeness
- [ ] Transmission triggers on map exit
- [ ] Raid modifications apply correctly
- [ ] Player warnings appear

### 8.2 Integration Testing

**Full Infiltration Cycle:**
1. Infiltrator spawns with hidden identity
2. Gathers intelligence over multiple visits
3. Recruits accomplice
4. Commits crime and frames innocent
5. Plants evidence
6. Transmits intel and leaves
7. Intelligence-buffed raid occurs
8. Accomplice sabotages
9. Investigation reveals truth
10. Infiltrator exposed

**False Conviction Chain:**
1. Sanguophage kills colonist
2. Frames innocent with planted evidence
3. Player convicts innocent
4. Colony morale tanks
5. Investigation reveals truth
6. Innocent exonerated
7. Real culprit exposed

### 8.3 Balance Testing

**False Accusation Rate:**
- Target: 10-15% of Suspected crimes
- Test: 100 crimes, count false accusations
- Adjust `GRUDGE_FALSE_REPORT_CHANCE` to hit target

**Infiltrator Spawn Rate:**
- Target: 5% of visitors/joiners
- Test: 100 visitor events
- Verify not too frequent or rare

**Accomplice Recruitment:**
- Target: 0-2 accomplices per infiltrator
- Test vulnerable colonists get recruited
- Verify not entire colony manipulated

**Intel-Based Raid Difficulty:**
- Test raids with 0%, 50%, 100% intel
- Verify noticeable but not impossible increase
- Balance raid points multipliers

---

## 9. Balance & Tuning

### 9.1 Difficulty Knobs

| Parameter | Default | Range | Effect |
|-----------|---------|-------|--------|
| `falseAccusationRate` | 12% | 0-30% | Frequency of false reports |
| `infiltratorSpawnChance` | 5% | 0-15% | % of visitors who are infiltrators |
| `accompliceMaxPerInfiltrator` | 2 | 0-5 | Max colonists manipulated |
| `intelPerGather` | 15% | 5-30% | Intel gained per observation |
| `truthDiscoveryChance` | 15% | 5-40% | Base chance to reveal false accusation |
| `cluesPerCrime` | 1-3 | 0-5 | Physical evidence left |
| `intelligenceDecayRate` | 5%/day | 0-20% | Accomplice manipulation decay |

### 9.2 Storyteller Integration

**Randy Random:**
- High infiltrator spawn rate (8%)
- High false accusation rate (18%)
- Chaos mode!

**Cassandra Classic:**
- Standard rates (5% infiltrators, 12% false accusations)
- Predictable escalation

**Phoebe Chillax:**
- Low rates (2% infiltrators, 8% false accusations)
- Reduced drama

### 9.3 Player Feedback Loops

**Too Easy (Players Crushing Infiltrators):**
- Increase intelligence stat range
- Reduce behavioral clue frequency
- Make accomplices harder to discover

**Too Hard (Colony Destroyed by Infiltration):**
- Increase discovery clue frequency
- Reduce accomplice sabotage impact
- Add more player tools (security scanning, etc.)

**Too Frustrating (False Accusations Annoying):**
- Lower false accusation rate
- Increase truth discovery chance
- Add more investigation options

---

## 10. Risk Mitigation

### 10.1 Technical Risks

**Risk:** Save/load corruption with complex nested data
**Mitigation:**
- Extensive `ExposeData()` testing
- Reference vs value type validation
- Null reference checks everywhere
- Save file compatibility tests

**Risk:** Performance issues with clue spawning
**Mitigation:**
- Limit clues per crime (max 3)
- Clue decay/cleanup after 10 days
- Efficient spawn location finding
- Profiling and optimization

**Risk:** UI becomes cluttered with new info
**Mitigation:**
- Collapsible sections for advanced features
- Tooltips for detailed information
- Clear visual hierarchy
- Optional display toggles

### 10.2 Design Risks

**Risk:** False accusations too frustrating
**Mitigation:**
- Clear UI indicators (warning colors)
- Truth discovery is achievable with effort
- Tunable rates (can reduce frequency, not disable)
- Tutorial/tooltips explaining system

**Risk:** Infiltration too subtle, players don't notice
**Mitigation:**
- Dramatic reveals are loud and clear
- Intel transmission warnings
- Accomplice sabotage is obvious
- Behavioral clues are discoverable

**Risk:** System too complex for casual players
**Mitigation:**
- Default tuning is moderate (12% false accusations)
- Rates can be adjusted lower for easier experience
- Tutorial messages explain mechanics
- God mode tools for debugging

### 10.3 Balance Risks

**Risk:** Infiltrators too weak, easily caught
**Mitigation:**
- Intelligence stat creates smart vs dumb infiltrators
- Multiple discovery methods required
- Fake identities are convincing
- Accomplices provide distraction

**Risk:** Infiltrators too strong, impossible to stop
**Mitigation:**
- Discovery progress is cumulative
- Behavioral clues are inevitable over time
- Interrogation can break cases
- Social network helps (friends defend, enemies accuse)

**Risk:** False accusations create tantrum spiral
**Mitigation:**
- Unjust conviction mood hit is significant but not catastrophic
- Truth discovery provides redemption arc
- Social impact has recovery period
- Friends provide support

---

## 11. Success Metrics

### 11.1 Quantitative Metrics

- **False Accusation Rate:** 10-15% of Suspected crimes
- **Infiltrator Detection Time:** 10-30 days average
- **Accomplice Recruitment Rate:** 20-40% of vulnerable colonists
- **Intel-Based Raid Difficulty:** +30-50% raid points
- **Performance:** <5ms per tick for all Phase 6 systems
- **Save File Size:** <10% increase from Phase 6

### 11.2 Qualitative Goals

**Player Should Feel:**
- Uncertainty (Can I trust this witness?)
- Detective excitement (Finding clues is rewarding)
- Paranoia (Is anyone an infiltrator?)
- Dramatic tension (Reveals are climactic)
- Agency (Investigation matters)

**Player Should Experience:**
- Memorable stories (innocent executed, infiltrator exposed)
- Moral dilemmas (convict on weak evidence or investigate further?)
- Social complexity (relationships matter in justice)
- Emergent drama (accomplice betrayals, false accusations)

**Player Should NOT Feel:**
- Helpless (no way to discover truth)
- Frustrated (system is incomprehensible)
- Bored (too predictable or deterministic)
- Cheated (deaths feel random/unfair)

---

## 12. Conclusion

Phase 6 transforms Law and Order from a crime tracking mod into a **deep investigative thriller** inspired by Dwarf Fortress's emergent storytelling. The combination of false accusations, hidden identities, espionage, and social manipulation creates a system where:

1. **No information is certain** - witnesses lie, evidence can be faked, identities are masks
2. **Investigation matters** - player effort uncovers truth, but requires skill and time
3. **Mistakes have consequences** - wrong convictions destroy colony morale permanently
4. **Stories emerge** - infiltrators, frame-ups, betrayals, and dramatic reveals create memorable narratives

### Implementation Priority

**Must Have (Core Drama):**
- False Accusation System (4.4)
- Witness Reliability System (4.6)
- Social Relationship Impact (4.7)

**Highly Desirable (Deep Intrigue):**
- Hidden Identity System (4.1)
- Clue & Evidence System (4.2)
- Truth Discovery mechanics

**Nice to Have (Advanced Gameplay):**
- Intelligence Gathering (4.3)
- Social Manipulation & Accomplices (4.5)
- Intel-based raids

This modular approach allows incremental implementation and testing while delivering value early.

**Total Estimated Effort:** 4-6 weeks
**Total Line Count:** ~15,000-20,000 lines
**Total New Files:** ~40 files

---

**END OF PHASE 6 DESIGN BLUEPRINT**

**Status:** Ready for Implementation
**Next Step:** User approval and implementation kickoff

---
