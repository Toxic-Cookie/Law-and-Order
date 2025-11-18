using System.Collections.Generic;
using Verse;
using RimWorld;
using Law_and_Order.Source.Utils;

namespace Law_and_Order.Source.Infiltration
{
    /// <summary>
    /// ThingComp for pawns who have been recruited as accomplices by infiltrators.
    /// Tracks recruitment, sabotage tasks, and discovery.
    /// </summary>
    public class CompAccomplice : ThingComp
    {
        private Pawn recruiter;                         // The infiltrator who recruited this accomplice
        private int recruitedTick;                      // When this pawn was recruited
        private bool discovered;                        // Has the accomplice been caught?
        private int discoveredTick;                     // When they were discovered
        private List<SabotageTask> pendingSabotage;     // Sabotage tasks waiting to be executed
        private List<SabotageTask> completedSabotage;   // Sabotage tasks that have been performed
        private float loyaltyToRecruiter;               // 0.0-1.0, affects willingness to sabotage

        public Pawn Recruiter => recruiter;
        public bool IsRecruited => recruiter != null && !discovered;
        public bool Discovered => discovered;
        public int DaysSinceRecruitment => IsRecruited ? (Find.TickManager.TicksGame - recruitedTick) / GenDate.TicksPerDay : 0;
        public float LoyaltyToRecruiter => loyaltyToRecruiter;

        public CompProperties_Accomplice Props => (CompProperties_Accomplice)props;

        public CompAccomplice()
        {
            pendingSabotage = new List<SabotageTask>();
            completedSabotage = new List<SabotageTask>();
            loyaltyToRecruiter = 0.5f;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_References.Look(ref recruiter, "recruiter");
            Scribe_Values.Look(ref recruitedTick, "recruitedTick", 0);
            Scribe_Values.Look(ref discovered, "discovered", false);
            Scribe_Values.Look(ref discoveredTick, "discoveredTick", 0);
            Scribe_Values.Look(ref loyaltyToRecruiter, "loyaltyToRecruiter", 0.5f);
            Scribe_Collections.Look(ref pendingSabotage, "pendingSabotage", LookMode.Deep);
            Scribe_Collections.Look(ref completedSabotage, "completedSabotage", LookMode.Deep);

            // Post-load init
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (pendingSabotage == null)
                    pendingSabotage = new List<SabotageTask>();
                if (completedSabotage == null)
                    completedSabotage = new List<SabotageTask>();
            }
        }

        /// <summary>
        /// Recruit this pawn as an accomplice
        /// </summary>
        public void RecruitAsAccomplice(Pawn recruitingInfiltrator, float initialLoyalty)
        {
            if (recruiter != null)
            {
                ModLog.Warning($"Attempting to recruit {parent.LabelShort} who is already an accomplice!");
                return;
            }

            recruiter = recruitingInfiltrator;
            recruitedTick = Find.TickManager.TicksGame;
            loyaltyToRecruiter = initialLoyalty;
            discovered = false;

            ModLog.Info($"{recruitingInfiltrator.LabelShort} recruited {parent.LabelShort} as accomplice (loyalty: {loyaltyToRecruiter:F2})");

            // Send message to player if in dev mode
            if (Prefs.DevMode)
            {
                Messages.Message(
                    $"DEBUG: {parent.LabelShort} recruited as accomplice by {recruitingInfiltrator.LabelShort}",
                    parent,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        /// <summary>
        /// Add a sabotage task for this accomplice to perform
        /// </summary>
        public void AddSabotageTask(SabotageTask task)
        {
            if (!IsRecruited)
            {
                ModLog.Warning($"Attempting to add sabotage task to non-accomplice {parent.LabelShort}");
                return;
            }

            if (pendingSabotage == null)
                pendingSabotage = new List<SabotageTask>();

            pendingSabotage.Add(task);

            ModLog.Debug($"Added sabotage task {task.type} for accomplice {parent.LabelShort}");
        }

        /// <summary>
        /// Get all pending sabotage tasks
        /// </summary>
        public List<SabotageTask> GetPendingSabotage()
        {
            if (pendingSabotage == null)
                pendingSabotage = new List<SabotageTask>();

            return pendingSabotage;
        }

        /// <summary>
        /// Mark a sabotage task as completed
        /// </summary>
        public void CompleteSabotageTask(SabotageTask task)
        {
            if (pendingSabotage == null || completedSabotage == null)
                return;

            pendingSabotage.Remove(task);
            completedSabotage.Add(task);

            ModLog.Info($"Accomplice {parent.LabelShort} completed sabotage: {task.type}");
        }

        /// <summary>
        /// Clear all pending sabotage tasks
        /// </summary>
        public void ClearPendingSabotage()
        {
            if (pendingSabotage == null)
                return;

            pendingSabotage.Clear();
        }

        /// <summary>
        /// Discover this accomplice (they've been caught)
        /// </summary>
        public void DiscoverAccomplice()
        {
            if (discovered)
                return;

            discovered = true;
            discoveredTick = Find.TickManager.TicksGame;

            Pawn accomplice = parent as Pawn;
            if (accomplice == null)
                return;

            ModLog.Info($"Accomplice {accomplice.LabelShort} has been discovered!");

            // Clear pending sabotage
            ClearPendingSabotage();

            // Send dramatic letter to player
            Find.LetterStack.ReceiveLetter(
                "LawAndOrder_LetterAccompliceDiscoveredTitle".Translate(),
                "LawAndOrder_LetterAccompliceDiscoveredDesc".Translate(
                    accomplice.LabelShortCap,
                    recruiter?.LabelShort ?? "unknown infiltrator"
                ),
                LetterDefOf.ThreatBig,
                accomplice
            );

            // Apply social impact
            AccompliceUtils.ApplyBetrayalThoughts(accomplice, recruiter);
        }

        /// <summary>
        /// Update loyalty based on mood and time
        /// Loyalty degrades if mood improves, increases if mood stays low
        /// </summary>
        public void UpdateLoyalty()
        {
            if (!IsRecruited)
                return;

            Pawn accomplice = parent as Pawn;
            if (accomplice == null || accomplice.needs?.mood == null)
                return;

            float currentMood = accomplice.needs.mood.CurLevelPercentage;

            // If mood improved significantly, loyalty decreases
            if (currentMood > 0.5f)
            {
                loyaltyToRecruiter -= 0.01f; // Slow decay when happy
            }
            // If mood is still low, loyalty increases slightly
            else if (currentMood < 0.3f)
            {
                loyaltyToRecruiter += 0.005f; // Slow increase when miserable
            }

            // Clamp loyalty
            loyaltyToRecruiter = UnityEngine.Mathf.Clamp01(loyaltyToRecruiter);

            // If loyalty drops too low, accomplice might refuse to sabotage
            if (loyaltyToRecruiter < 0.2f)
            {
                // Chance to abandon the infiltrator
                if (Rand.Chance(0.05f)) // 5% chance per update
                {
                    AbandonInfiltrator();
                }
            }
        }

        /// <summary>
        /// Accomplice has second thoughts and abandons the infiltrator
        /// </summary>
        private void AbandonInfiltrator()
        {
            if (!IsRecruited)
                return;

            Pawn accomplice = parent as Pawn;
            if (accomplice == null)
                return;

            ModLog.Info($"Accomplice {accomplice.LabelShort} abandoned infiltrator {recruiter?.LabelShort}");

            // Clear recruitment
            Pawn oldRecruiter = recruiter;
            recruiter = null;
            ClearPendingSabotage();

            // No discovery consequence - they just quietly stopped helping
            // This is different from being discovered

            if (Prefs.DevMode)
            {
                Messages.Message(
                    $"DEBUG: {accomplice.LabelShort} abandoned {oldRecruiter?.LabelShort ?? "infiltrator"} (loyalty too low)",
                    accomplice,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        /// <summary>
        /// Check if this accomplice should perform sabotage now
        /// Called when a raid starts
        /// </summary>
        public bool ShouldPerformSabotage()
        {
            if (!IsRecruited || discovered)
                return false;

            // Check loyalty - low loyalty = might refuse
            if (loyaltyToRecruiter < 0.3f)
            {
                return Rand.Chance(loyaltyToRecruiter); // Loyalty-based chance
            }

            return true; // High loyalty = always performs sabotage
        }

        public override string CompInspectStringExtra()
        {
            if (!Prefs.DevMode)
                return null;

            // Debug information (god mode only)
            if (IsRecruited)
            {
                return $"Accomplice: Recruited by {recruiter?.LabelShort ?? "unknown"}\n" +
                       $"Loyalty: {loyaltyToRecruiter:P0}\n" +
                       $"Days since recruitment: {DaysSinceRecruitment}\n" +
                       $"Pending sabotage: {pendingSabotage?.Count ?? 0}";
            }

            return null;
        }
    }

    /// <summary>
    /// Properties for CompAccomplice
    /// </summary>
    public class CompProperties_Accomplice : CompProperties
    {
        public CompProperties_Accomplice()
        {
            compClass = typeof(CompAccomplice);
        }
    }

    /// <summary>
    /// Represents a sabotage task assigned to an accomplice
    /// </summary>
    public class SabotageTask : IExposable
    {
        public SabotageType type;           // Type of sabotage
        public IntVec3 targetLocation;      // Where to perform sabotage
        public Thing targetThing;           // What to sabotage (if applicable)
        public int assignedTick;            // When this was assigned
        public bool requiresHighIntel;      // Whether this requires smart infiltrator

        public SabotageTask()
        {
        }

        public SabotageTask(SabotageType type, IntVec3 location, Thing target = null, bool requiresHighIntel = false)
        {
            this.type = type;
            this.targetLocation = location;
            this.targetThing = target;
            this.assignedTick = Find.TickManager.TicksGame;
            this.requiresHighIntel = requiresHighIntel;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref targetLocation, "targetLocation");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_Values.Look(ref assignedTick, "assignedTick", 0);
            Scribe_Values.Look(ref requiresHighIntel, "requiresHighIntel", false);
        }
    }

    /// <summary>
    /// Types of sabotage an accomplice can perform
    /// </summary>
    public enum SabotageType
    {
        DisableTurret,      // Damage or disable a turret
        OpenDoor,           // Open a defensive door/gate
        StartFire,          // Start a fire in the colony
        BreakEquipment,     // Damage critical equipment (power, production)
        CauseDistraction,   // Create a disturbance
        SabotageDefenses    // Generic defense sabotage
    }
}
