# Technical Specification: Crime Visibility State System

## Overview

Implementation specification for the three-state crime visibility system: Hidden, Suspected, and Convicted.

**Reference**: See `Crime_Visibility_States.md` for design philosophy and detailed examples.

## Core Data Structures

### CrimeVisibilityState Enum

```csharp
namespace LawAndOrder
{
    /// <summary>
    /// Represents the visibility state of a crime within the justice system.
    /// </summary>
    public enum CrimeVisibilityState
    {
        /// <summary>
        /// Crime occurred but remains undetected. Not visible in UI.
        /// Can be discovered through interrogation or investigation.
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Crime has been witnessed or confessed. Case opened.
        /// Visible in UI, pending conviction.
        /// </summary>
        Suspected = 1,

        /// <summary>
        /// Pawn found guilty. Punishment assigned.
        /// Visible in UI, case closed or punishment in progress.
        /// </summary>
        Convicted = 2
    }
}
```

### CrimeInstance Class

```csharp
namespace LawAndOrder
{
    /// <summary>
    /// Represents a single criminal act committed by a pawn.
    /// Always tracked internally regardless of visibility state.
    /// </summary>
    public class CrimeInstance : IExposable
    {
        // ===== Core Crime Data =====

        /// <summary>
        /// The pawn who committed the crime (or is accused of it).
        /// May be innocent in case of false accusation.
        /// </summary>
        public Pawn perpetrator;

        /// <summary>
        /// The type of crime committed.
        /// </summary>
        public CrimeTypeDef crimeType;

        /// <summary>
        /// Game tick when the crime was committed.
        /// </summary>
        public int tickCommitted;

        /// <summary>
        /// Location where the crime occurred.
        /// </summary>
        public IntVec3 location;

        /// <summary>
        /// Map where the crime occurred.
        /// </summary>
        public Map map;

        /// <summary>
        /// The victim of the crime, if applicable.
        /// Null for victimless crimes (theft, vandalism, etc.)
        /// </summary>
        public Pawn victim;

        // ===== Visibility & State =====

        /// <summary>
        /// Current visibility state of the crime.
        /// Determines UI visibility and case status.
        /// </summary>
        public CrimeVisibilityState state = CrimeVisibilityState.Hidden;

        /// <summary>
        /// Tick when state last changed.
        /// Used for UI animation and tracking.
        /// </summary>
        public int stateChangeTickLast;

        // ===== Evidence & Detection =====

        /// <summary>
        /// List of pawns who witnessed the crime.
        /// Empty if crime is Hidden or discovered through other means.
        /// </summary>
        public List<Pawn> witnesses = new List<Pawn>();

        /// <summary>
        /// Strength of evidence against perpetrator (0.0 to 1.0).
        /// - 0.0-0.3: Weak (single unreliable witness)
        /// - 0.3-0.7: Moderate (multiple witnesses or confession)
        /// - 0.7-1.0: Strong (caught red-handed, multiple reliable witnesses)
        /// </summary>
        public float evidenceStrength = 0f;

        /// <summary>
        /// Whether the perpetrator confessed to this crime during interrogation.
        /// Increases evidenceStrength and reliability.
        /// </summary>
        public bool wasConfessed = false;

        /// <summary>
        /// Whether evidence was discovered through investigation.
        /// </summary>
        public bool wasInvestigated = false;

        // ===== Case Association =====

        /// <summary>
        /// The criminal case this crime is associated with.
        /// Null if state is Hidden.
        /// Set when transitioning to Suspected.
        /// </summary>
        public CriminalCase associatedCase;

        // ===== Conviction Data =====

        /// <summary>
        /// Punishment assigned upon conviction.
        /// Null until state becomes Convicted.
        /// </summary>
        public Punishment assignedPunishment;

        /// <summary>
        /// Tick when pawn was convicted.
        /// 0 if not yet convicted.
        /// </summary>
        public int convictionTick = 0;

        /// <summary>
        /// Whether this is a false conviction.
        /// True if convicted pawn is innocent (wrong person blamed).
        /// Used for storytelling and stat tracking.
        /// </summary>
        public bool isFalseConviction = false;

        // ===== Investigation State =====

        /// <summary>
        /// Whether this crime can still be investigated.
        /// False if statute of limitations expired or other conditions.
        /// </summary>
        public bool canBeInvestigated = true;

        /// <summary>
        /// Whether this is a cold case (old, unsolved).
        /// </summary>
        public bool isColdCase = false;

        // ===== Methods =====

        /// <summary>
        /// Transition crime from Hidden to Suspected.
        /// Opens or amends a criminal case.
        /// </summary>
        public void TransitionToSuspected(List<Pawn> newWitnesses = null)
        {
            if (state != CrimeVisibilityState.Hidden)
            {
                Log.Warning($"Cannot transition crime to Suspected from {state}");
                return;
            }

            state = CrimeVisibilityState.Suspected;
            stateChangeTickLast = Find.TickManager.TicksGame;

            if (newWitnesses != null)
                witnesses.AddRange(newWitnesses);

            // Create or amend case
            associatedCase = CriminalCase.GetOrCreateCase(perpetrator);
            associatedCase.AddCrime(this);

            // Notify player
            Messages.Message(
                $"{perpetrator.LabelShort} suspected of {crimeType.label}",
                new LookTargets(perpetrator),
                MessageTypeDefOf.NegativeEvent
            );
        }

        /// <summary>
        /// Transition crime from Suspected to Convicted.
        /// Assigns punishment and closes case.
        /// </summary>
        public void TransitionToConvicted(Punishment punishment, bool falseConviction = false)
        {
            if (state != CrimeVisibilityState.Suspected)
            {
                Log.Warning($"Cannot transition crime to Convicted from {state}");
                return;
            }

            state = CrimeVisibilityState.Convicted;
            stateChangeTickLast = Find.TickManager.TicksGame;
            convictionTick = Find.TickManager.TicksGame;
            assignedPunishment = punishment;
            isFalseConviction = falseConviction;

            // Update case
            if (associatedCase != null)
            {
                associatedCase.OnCrimeConvicted(this);
            }

            // Notify player
            Messages.Message(
                $"{perpetrator.LabelShort} convicted of {crimeType.label}",
                new LookTargets(perpetrator),
                MessageTypeDefOf.NegativeEvent
            );
        }

        /// <summary>
        /// Whether this crime should be shown in the UI.
        /// </summary>
        public bool IsVisibleInUI()
        {
            return state != CrimeVisibilityState.Hidden;
        }

        /// <summary>
        /// Get display label for this crime's state.
        /// </summary>
        public string GetStateLabel()
        {
            switch (state)
            {
                case CrimeVisibilityState.Hidden:
                    return "LAO_CrimeState_Hidden".Translate(); // Should never be shown
                case CrimeVisibilityState.Suspected:
                    return "LAO_CrimeState_Suspected".Translate();
                case CrimeVisibilityState.Convicted:
                    return "LAO_CrimeState_Convicted".Translate();
                default:
                    return "Unknown";
            }
        }

        // ===== Saving/Loading =====

        public void ExposeData()
        {
            Scribe_References.Look(ref perpetrator, "perpetrator");
            Scribe_Defs.Look(ref crimeType, "crimeType");
            Scribe_Values.Look(ref tickCommitted, "tickCommitted");
            Scribe_Values.Look(ref location, "location");
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref victim, "victim");

            Scribe_Values.Look(ref state, "state", CrimeVisibilityState.Hidden);
            Scribe_Values.Look(ref stateChangeTickLast, "stateChangeTickLast");

            Scribe_Collections.Look(ref witnesses, "witnesses", LookMode.Reference);
            Scribe_Values.Look(ref evidenceStrength, "evidenceStrength");
            Scribe_Values.Look(ref wasConfessed, "wasConfessed");
            Scribe_Values.Look(ref wasInvestigated, "wasInvestigated");

            Scribe_References.Look(ref associatedCase, "associatedCase");

            Scribe_Deep.Look(ref assignedPunishment, "assignedPunishment");
            Scribe_Values.Look(ref convictionTick, "convictionTick");
            Scribe_Values.Look(ref isFalseConviction, "isFalseConviction");

            Scribe_Values.Look(ref canBeInvestigated, "canBeInvestigated", true);
            Scribe_Values.Look(ref isColdCase, "isColdCase");
        }
    }
}
```

### CriminalCase Class

```csharp
namespace LawAndOrder
{
    /// <summary>
    /// Represents a legal case against a pawn.
    /// Contains one or more crimes.
    /// Only one active case per pawn at a time.
    /// </summary>
    public class CriminalCase : IExposable
    {
        /// <summary>
        /// Unique identifier for this case.
        /// </summary>
        public int caseId;

        /// <summary>
        /// The accused pawn.
        /// </summary>
        public Pawn accused;

        /// <summary>
        /// Current status of the case.
        /// </summary>
        public CaseStatus status = CaseStatus.Open;

        /// <summary>
        /// All crimes associated with this case.
        /// Can have multiple crimes, added via amendment.
        /// </summary>
        public List<CrimeInstance> crimes = new List<CrimeInstance>();

        /// <summary>
        /// Tick when case was opened.
        /// </summary>
        public int openedTick;

        /// <summary>
        /// Tick when case was closed.
        /// 0 if still open.
        /// </summary>
        public int closedTick = 0;

        /// <summary>
        /// Add a crime to this case (amend the case).
        /// </summary>
        public void AddCrime(CrimeInstance crime)
        {
            if (!crimes.Contains(crime))
            {
                crimes.Add(crime);
                crime.associatedCase = this;

                Log.Message($"[Case #{caseId}] Added {crime.crimeType.label} to case against {accused.LabelShort}");
            }
        }

        /// <summary>
        /// Called when a crime in this case is convicted.
        /// Checks if all crimes convicted, closes case if so.
        /// </summary>
        public void OnCrimeConvicted(CrimeInstance crime)
        {
            // Check if all crimes are convicted
            bool allConvicted = crimes.All(c => c.state == CrimeVisibilityState.Convicted);

            if (allConvicted)
            {
                CloseCase();
            }
        }

        /// <summary>
        /// Close this case.
        /// </summary>
        public void CloseCase()
        {
            status = CaseStatus.Closed;
            closedTick = Find.TickManager.TicksGame;

            Log.Message($"[Case #{caseId}] Closed case against {accused.LabelShort}");
        }

        /// <summary>
        /// Get a summary string for this case.
        /// </summary>
        public string GetSummary()
        {
            int suspectedCount = crimes.Count(c => c.state == CrimeVisibilityState.Suspected);
            int convictedCount = crimes.Count(c => c.state == CrimeVisibilityState.Convicted);

            return $"Case #{caseId}: {accused.LabelShort} ({suspectedCount} suspected, {convictedCount} convicted)";
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref caseId, "caseId");
            Scribe_References.Look(ref accused, "accused");
            Scribe_Values.Look(ref status, "status");
            Scribe_Collections.Look(ref crimes, "crimes", LookMode.Deep);
            Scribe_Values.Look(ref openedTick, "openedTick");
            Scribe_Values.Look(ref closedTick, "closedTick");
        }
    }

    public enum CaseStatus
    {
        Open,              // Case active, crimes being investigated
        UnderInvestigation, // Active investigation in progress
        InTrial,           // Case in trial phase
        Closed             // Case concluded (convicted or dismissed)
    }
}
```

### CriminalCase Static Methods

```csharp
namespace LawAndOrder
{
    public partial class CriminalCase
    {
        /// <summary>
        /// Get or create a case for the given pawn.
        /// Enforces "one case per pawn" rule.
        /// </summary>
        public static CriminalCase GetOrCreateCase(Pawn pawn)
        {
            var justiceManager = Find.World.GetComponent<JusticeManager>();

            // Check for existing open case
            var existingCase = justiceManager.GetActiveCase(pawn);
            if (existingCase != null)
            {
                Log.Message($"[Case] Amending existing case #{existingCase.caseId} for {pawn.LabelShort}");
                return existingCase;
            }

            // Create new case
            var newCase = new CriminalCase
            {
                caseId = justiceManager.GetNextCaseId(),
                accused = pawn,
                status = CaseStatus.Open,
                openedTick = Find.TickManager.TicksGame
            };

            justiceManager.RegisterCase(newCase);

            Log.Message($"[Case] Opened new case #{newCase.caseId} for {pawn.LabelShort}");

            return newCase;
        }
    }
}
```

## Crime Detection System

### OnCrimeCommitted Event

```csharp
namespace LawAndOrder
{
    public static class CrimeDetectionSystem
    {
        /// <summary>
        /// Called whenever a crime is committed.
        /// ALWAYS records crime internally, determines visibility based on witnesses.
        /// </summary>
        public static CrimeInstance OnCrimeCommitted(
            Pawn perpetrator,
            CrimeTypeDef crimeType,
            IntVec3 location,
            Map map,
            Pawn victim = null)
        {
            // STEP 1: Always record internally
            var crime = new CrimeInstance
            {
                perpetrator = perpetrator,
                crimeType = crimeType,
                tickCommitted = Find.TickManager.TicksGame,
                location = location,
                map = map,
                victim = victim,
                state = CrimeVisibilityState.Hidden // Start as Hidden
            };

            // Store in perpetrator's crime record
            var crimeRecord = perpetrator.GetCrimeRecord();
            crimeRecord.AddCrime(crime);

            // Store in global registry
            var justiceManager = Find.World.GetComponent<JusticeManager>();
            justiceManager.RegisterCrime(crime);

            Log.Message($"[Crime] {perpetrator.LabelShort} committed {crimeType.label} at {location}");

            // STEP 2: Check for witnesses using Fog of War
            var witnesses = GetWitnesses(location, map, perpetrator);

            if (witnesses.Count > 0)
            {
                // Crime was witnessed - transition to Suspected
                float evidence = CalculateEvidenceStrength(witnesses, location, crime);

                crime.witnesses = witnesses;
                crime.evidenceStrength = evidence;
                crime.TransitionToSuspected(witnesses);

                Log.Message($"[Crime] Witnessed by {witnesses.Count} colonist(s)");
            }
            else
            {
                // No witnesses - stays Hidden
                Log.Message($"[Crime] No witnesses - crime remains hidden");
            }

            return crime;
        }

        /// <summary>
        /// Get list of pawns who witnessed the crime using Fog of War.
        /// </summary>
        private static List<Pawn> GetWitnesses(IntVec3 location, Map map, Pawn perpetrator)
        {
            var witnesses = new List<Pawn>();

            // Check if Real Fog of War is active
            var fogComp = map.GetComponent<MapComponentSeenFog>();
            if (fogComp == null)
            {
                Log.Warning("[Crime] Real Fog of War not found - assuming all crimes witnessed");
                // Fallback: add all nearby colonists
                return map.mapPawns.FreeColonistsSpawned
                    .Where(p => p.Position.InHorDistOf(location, 20f))
                    .ToList();
            }

            // Check each colonist for line of sight
            foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
            {
                // Skip the perpetrator themselves
                if (colonist == perpetrator)
                    continue;

                // Must be able to see
                if (!colonist.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                    continue;

                // Check if location is visible to player faction
                if (!fogComp.IsShown(Faction.OfPlayer, location))
                    continue;

                // Check if within colonist's sight range
                var watcher = colonist.TryGetComp<CompFieldOfViewWatcher>();
                if (watcher != null)
                {
                    float sightRange = watcher.CalcPawnSightRange(colonist.Position, false, false);
                    float distance = colonist.Position.DistanceTo(location);

                    if (distance <= sightRange)
                    {
                        witnesses.Add(colonist);
                    }
                }
            }

            return witnesses;
        }

        /// <summary>
        /// Calculate evidence strength based on witnesses and circumstances.
        /// </summary>
        private static float CalculateEvidenceStrength(List<Pawn> witnesses, IntVec3 location, CrimeInstance crime)
        {
            if (witnesses.Count == 0)
                return 0f;

            float strength = 0f;

            // Base strength from number of witnesses
            strength += Mathf.Min(witnesses.Count * 0.2f, 0.6f); // Max 0.6 from witnesses

            // Bonus for multiple independent witnesses
            if (witnesses.Count >= 3)
                strength += 0.2f;

            // Reduce for low light conditions
            var map = crime.map;
            if (map != null)
            {
                float light = map.glowGrid.GroundGlowAt(location);
                if (light < 0.3f)
                    strength *= 0.5f; // Half strength in darkness
            }

            // Bonus if caught red-handed (within 3 cells)
            if (witnesses.Any(w => w.Position.InHorDistOf(location, 3f)))
                strength += 0.3f;

            return Mathf.Clamp01(strength);
        }
    }
}
```

## Interrogation System

### Revealing Hidden Crimes

```csharp
namespace LawAndOrder
{
    public static class InterrogationSystem
    {
        /// <summary>
        /// Perform interrogation of suspect.
        /// Can reveal Hidden crimes through confession.
        /// </summary>
        public static InterrogationResult PerformInterrogation(
            Pawn interrogator,
            Pawn suspect)
        {
            var result = new InterrogationResult { suspect = suspect };

            // Get all Hidden crimes by suspect
            var crimeRecord = suspect.GetCrimeRecord();
            var hiddenCrimes = crimeRecord.GetCrimes()
                .Where(c => c.state == CrimeVisibilityState.Hidden &&
                           c.canBeInvestigated)
                .ToList();

            if (hiddenCrimes.Count == 0)
            {
                result.success = false;
                result.reason = "No hidden crimes to confess";
                return result;
            }

            // Social skill check
            float interrogatorSkill = interrogator.skills.GetSkill(SkillDefOf.Social).Level;
            float suspectResistance = suspect.skills.GetSkill(SkillDefOf.Social).Level;

            // Base chance: 50%
            float confessionChance = 0.5f;

            // Modify by skill difference
            confessionChance += (interrogatorSkill - suspectResistance) * 0.05f;

            // Modify by relationship
            if (suspect.relations.OpinionOf(interrogator) < 0)
                confessionChance -= 0.2f; // Harder if suspect dislikes interrogator

            // Roll for confession
            if (Rand.Chance(confessionChance))
            {
                // Confess to a random hidden crime
                var crimeToConfess = hiddenCrimes.RandomElement();

                crimeToConfess.wasConfessed = true;
                crimeToConfess.evidenceStrength = Mathf.Max(crimeToConfess.evidenceStrength, 0.7f);
                crimeToConfess.TransitionToSuspected();

                result.success = true;
                result.confessedCrime = crimeToConfess;
                result.reason = $"{suspect.LabelShort} confessed to {crimeToConfess.crimeType.label}";

                return result;
            }
            else
            {
                result.success = false;
                result.reason = $"{suspect.LabelShort} refused to confess";
                return result;
            }
        }
    }

    public class InterrogationResult
    {
        public Pawn suspect;
        public bool success;
        public CrimeInstance confessedCrime;
        public string reason;
    }
}
```

## UI Integration

### Justice Tab Main Window

```csharp
namespace LawAndOrder.UI
{
    public class MainTabWindow_Justice : MainTabWindow
    {
        private enum Tab
        {
            OpenCases,
            Convictions,
            Settings
        }

        private Tab currentTab = Tab.OpenCases;
        private Vector2 scrollPosition;

        public override void DoWindowContents(Rect inRect)
        {
            // Tab buttons
            Rect tabRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            DrawTabs(tabRect);

            // Content area
            Rect contentRect = new Rect(
                inRect.x,
                inRect.y + 45f,
                inRect.width,
                inRect.height - 45f
            );

            switch (currentTab)
            {
                case Tab.OpenCases:
                    DrawOpenCases(contentRect);
                    break;
                case Tab.Convictions:
                    DrawConvictions(contentRect);
                    break;
                case Tab.Settings:
                    DrawSettings(contentRect);
                    break;
            }
        }

        private void DrawOpenCases(Rect rect)
        {
            var justiceManager = Find.World.GetComponent<JusticeManager>();

            // Get all open cases (only Suspected crimes)
            var openCases = justiceManager.GetOpenCases();

            if (openCases.Count == 0)
            {
                Widgets.Label(rect, "LAO_NoOpenCases".Translate());
                return;
            }

            // Draw list of cases
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, openCases.Count * 120f);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (var caseInstance in openCases)
            {
                DrawCaseEntry(new Rect(0f, y, viewRect.width, 110f), caseInstance);
                y += 120f;
            }

            Widgets.EndScrollView();
        }

        private void DrawCaseEntry(Rect rect, CriminalCase caseInstance)
        {
            Widgets.DrawBox(rect);

            // Case header
            Rect headerRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, caseInstance.GetSummary());
            Text.Font = GameFont.Small;

            // Crime list
            float y = rect.y + 35f;
            foreach (var crime in caseInstance.crimes)
            {
                if (crime.IsVisibleInUI())
                {
                    Rect crimeRect = new Rect(rect.x + 20f, y, rect.width - 40f, 20f);
                    DrawCrimeEntry(crimeRect, crime);
                    y += 22f;
                }
            }

            // Action buttons
            Rect buttonRect = new Rect(rect.x + 10f, rect.yMax - 30f, 100f, 25f);

            if (Widgets.ButtonText(buttonRect, "LAO_Convict".Translate()))
            {
                ConvictPawn(caseInstance);
            }

            buttonRect.x += 110f;
            if (Widgets.ButtonText(buttonRect, "LAO_Dismiss".Translate()))
            {
                DismissCase(caseInstance);
            }

            buttonRect.x += 110f;
            if (Widgets.ButtonText(buttonRect, "LAO_Investigate".Translate()))
            {
                StartInvestigation(caseInstance);
            }
        }

        private void DrawCrimeEntry(Rect rect, CrimeInstance crime)
        {
            // State label
            string stateLabel = $"[{crime.GetStateLabel()}]";
            Widgets.Label(rect, $"{stateLabel} {crime.crimeType.label}");

            // Details on hover
            if (Mouse.IsOver(rect))
            {
                string tooltip = $"Evidence: {crime.evidenceStrength:P0}\n" +
                                $"Witnesses: {crime.witnesses.Count}\n" +
                                $"Confessed: {(crime.wasConfessed ? "Yes" : "No")}";
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void ConvictPawn(CriminalCase caseInstance)
        {
            // Open punishment selection dialog
            Find.WindowStack.Add(new Dialog_SelectPunishment(caseInstance));
        }

        private void DismissCase(CriminalCase caseInstance)
        {
            // Confirm dismissal
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "LAO_ConfirmDismissCase".Translate(caseInstance.accused.LabelShort),
                delegate
                {
                    caseInstance.CloseCase();
                    Messages.Message(
                        "LAO_CaseDismissed".Translate(caseInstance.accused.LabelShort),
                        MessageTypeDefOf.NeutralEvent
                    );
                }
            ));
        }

        private void StartInvestigation(CriminalCase caseInstance)
        {
            // Open investigation dialog
            Find.WindowStack.Add(new Dialog_Investigation(caseInstance));
        }

        private void DrawConvictions(Rect rect)
        {
            var justiceManager = Find.World.GetComponent<JusticeManager>();

            // Get all convicted crimes
            var convictedCases = justiceManager.GetConvictedCases();

            if (convictedCases.Count == 0)
            {
                Widgets.Label(rect, "LAO_NoConvictions".Translate());
                return;
            }

            // Similar to open cases, but showing convicted status
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, convictedCases.Count * 100f);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (var caseInstance in convictedCases)
            {
                DrawConvictedCaseEntry(new Rect(0f, y, viewRect.width, 90f), caseInstance);
                y += 100f;
            }

            Widgets.EndScrollView();
        }

        private void DrawConvictedCaseEntry(Rect rect, CriminalCase caseInstance)
        {
            // Similar to DrawCaseEntry but showing punishment status
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.1f, 0.1f, 0.5f));

            Rect headerRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 30f);
            Widgets.Label(headerRect, caseInstance.GetSummary());

            // Show punishment details
            float y = rect.y + 35f;
            foreach (var crime in caseInstance.crimes.Where(c => c.state == CrimeVisibilityState.Convicted))
            {
                Rect punishmentRect = new Rect(rect.x + 20f, y, rect.width - 40f, 20f);
                string punishmentText = crime.assignedPunishment?.GetDescription() ?? "None";
                Widgets.Label(punishmentRect, $"{crime.crimeType.label}: {punishmentText}");
                y += 22f;
            }
        }

        private void DrawSettings(Rect rect)
        {
            // Mod settings and configuration
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            listing.CheckboxLabeled("LAO_ShowHiddenCrimes".Translate(), ref DebugSettings.showHiddenCrimes);
            listing.CheckboxLabeled("LAO_AutoConvictCaughtRedHanded".Translate(), ref Settings.autoConvictRedHanded);

            listing.End();
        }

        private void DrawTabs(Rect rect)
        {
            float tabWidth = rect.width / 3f;

            if (Widgets.ButtonText(new Rect(rect.x, rect.y, tabWidth, rect.height), "LAO_OpenCases".Translate()))
                currentTab = Tab.OpenCases;

            if (Widgets.ButtonText(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), "LAO_Convictions".Translate()))
                currentTab = Tab.Convictions;

            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), "LAO_Settings".Translate()))
                currentTab = Tab.Settings;
        }
    }
}
```

## Justice Manager (WorldComponent)

```csharp
namespace LawAndOrder
{
    /// <summary>
    /// Global manager for the justice system.
    /// Tracks all crimes and cases across all maps.
    /// </summary>
    public class JusticeManager : WorldComponent
    {
        private List<CrimeInstance> allCrimes = new List<CrimeInstance>();
        private List<CriminalCase> allCases = new List<CriminalCase>();
        private int nextCaseId = 1;

        public JusticeManager(World world) : base(world)
        {
        }

        /// <summary>
        /// Register a crime in the global registry.
        /// </summary>
        public void RegisterCrime(CrimeInstance crime)
        {
            if (!allCrimes.Contains(crime))
            {
                allCrimes.Add(crime);
            }
        }

        /// <summary>
        /// Register a case in the global registry.
        /// </summary>
        public void RegisterCase(CriminalCase caseInstance)
        {
            if (!allCases.Contains(caseInstance))
            {
                allCases.Add(caseInstance);
            }
        }

        /// <summary>
        /// Get next case ID (auto-incrementing).
        /// </summary>
        public int GetNextCaseId()
        {
            return nextCaseId++;
        }

        /// <summary>
        /// Get active (open) case for a pawn, if any.
        /// </summary>
        public CriminalCase GetActiveCase(Pawn pawn)
        {
            return allCases.FirstOrDefault(c =>
                c.accused == pawn &&
                c.status != CaseStatus.Closed);
        }

        /// <summary>
        /// Get all open cases (Suspected crimes).
        /// </summary>
        public List<CriminalCase> GetOpenCases()
        {
            return allCases.Where(c => c.status != CaseStatus.Closed).ToList();
        }

        /// <summary>
        /// Get all convicted cases.
        /// </summary>
        public List<CriminalCase> GetConvictedCases()
        {
            return allCases.Where(c =>
                c.status == CaseStatus.Closed &&
                c.crimes.Any(crime => crime.state == CrimeVisibilityState.Convicted)
            ).ToList();
        }

        /// <summary>
        /// Get all crimes (for debugging/stats).
        /// </summary>
        public List<CrimeInstance> GetAllCrimes()
        {
            return allCrimes;
        }

        /// <summary>
        /// Get crimes visible in UI (Suspected + Convicted).
        /// </summary>
        public List<CrimeInstance> GetVisibleCrimes()
        {
            return allCrimes.Where(c => c.IsVisibleInUI()).ToList();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref allCrimes, "allCrimes", LookMode.Deep);
            Scribe_Collections.Look(ref allCases, "allCases", LookMode.Deep);
            Scribe_Values.Look(ref nextCaseId, "nextCaseId", 1);
        }
    }
}
```

## Extension Methods

```csharp
namespace LawAndOrder
{
    public static class PawnExtensions
    {
        /// <summary>
        /// Get the crime record for this pawn.
        /// Creates one if it doesn't exist.
        /// </summary>
        public static CrimeRecord GetCrimeRecord(this Pawn pawn)
        {
            var comp = pawn.GetComp<CompCrimeRecord>();
            if (comp == null)
            {
                Log.Error($"Pawn {pawn.LabelShort} missing CompCrimeRecord");
                return null;
            }

            return comp.crimeRecord;
        }

        /// <summary>
        /// Check if this pawn has an active criminal case against them.
        /// </summary>
        public static bool HasActiveCase(this Pawn pawn)
        {
            return Find.World.GetComponent<JusticeManager>().GetActiveCase(pawn) != null;
        }
    }
}
```

## Summary

**Key Components**:
1. **CrimeVisibilityState** enum - Three states (Hidden/Suspected/Convicted)
2. **CrimeInstance** class - Individual crime tracking
3. **CriminalCase** class - Case management (one per pawn)
4. **CrimeDetectionSystem** - Witness checking via FoW
5. **InterrogationSystem** - Revealing hidden crimes
6. **JusticeManager** - Global crime/case registry
7. **MainTabWindow_Justice** - UI presentation

**State Flow**:
- All crimes start as Hidden
- Witnesses → transition to Suspected → case opened
- Player/trial → transition to Convicted → punishment assigned
- No backward transitions (one-way progression)

**UI Visibility**:
- Hidden crimes: NOT shown anywhere
- Suspected crimes: Shown in "Open Cases" tab
- Convicted crimes: Shown in "Convictions" tab

This creates a robust, DF-faithful crime tracking system with clean separation between internal simulation and player-facing UI!
