using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// WorldComponent that manages all active punishments across the game.
    /// Handles punishment tracking, execution, and completion.
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    public class WorldComponent_PunishmentManager : WorldComponent
    {
        private List<Punishment> activePunishments = new List<Punishment>();
        private int lastCheckTick = 0;
        private const int CHECK_INTERVAL = 2500; // Check every ~1 in-game hour

        public static WorldComponent_PunishmentManager Instance => Find.World.GetComponent<WorldComponent_PunishmentManager>();

        public WorldComponent_PunishmentManager(World world) : base(world)
        {
        }

        /// <summary>
        /// Add a new punishment to the tracking system.
        /// </summary>
        public void AddPunishment(Punishment punishment)
        {
            if (punishment == null || activePunishments.Contains(punishment))
                return;

            activePunishments.Add(punishment);
            Mod.Log?.Message($"Punishment added: {punishment.type} for {punishment.criminal?.NameShortColored} (Case #{punishment.caseId})");

            // Apply social impact (mood effects, opinions)
            Utils.SocialImpactUtils.ApplyPunishmentImpact(punishment);
            Utils.SocialImpactUtils.ApplyRelationshipImpact(punishment);

            // Auto-start certain punishment types immediately
            switch (punishment.type)
            {
                case PunishmentType.Beating:
                    // Queue beating job for a colonist
                    QueueBeatingJob(punishment);
                    break;

                case PunishmentType.Execution:
                    // Queue execution job for a colonist
                    QueueExecutionJob(punishment);
                    break;

                case PunishmentType.Imprisonment:
                    // Start imprisonment tracking immediately if already imprisoned
                    if (punishment.criminal?.IsPrisoner == true)
                    {
                        punishment.Start();
                    }
                    break;

                case PunishmentType.Fine:
                    // Fines start immediately
                    punishment.Start();
                    ApplyFineHediff(punishment);
                    break;

                case PunishmentType.Exile:
                    // Exile is executed immediately
                    punishment.Start();
                    ExecuteExile(punishment);
                    break;
            }
        }

        /// <summary>
        /// Get all punishments for a specific pawn.
        /// </summary>
        public List<Punishment> GetPunishmentsForPawn(Pawn pawn)
        {
            return activePunishments.Where(p => p.criminal == pawn).ToList();
        }

        /// <summary>
        /// Get all punishments for a specific case.
        /// </summary>
        public List<Punishment> GetPunishmentsForCase(int caseId)
        {
            return activePunishments.Where(p => p.caseId == caseId).ToList();
        }

        /// <summary>
        /// Get all active (not completed/failed) punishments.
        /// </summary>
        public List<Punishment> GetActivePunishments()
        {
            return activePunishments.Where(p =>
                p.status == PunishmentStatus.Pending ||
                p.status == PunishmentStatus.Active).ToList();
        }

        /// <summary>
        /// Get all completed punishments.
        /// </summary>
        public List<Punishment> GetCompletedPunishments()
        {
            return activePunishments.Where(p => p.status == PunishmentStatus.Completed).ToList();
        }

        /// <summary>
        /// Remove old completed/failed punishments to prevent list bloat.
        /// </summary>
        private void CleanupOldPunishments()
        {
            int currentTick = Find.TickManager.TicksGame;
            int cutoffTick = currentTick - (60 * GenDate.TicksPerDay); // 60 days

            activePunishments.RemoveAll(p =>
                (p.status == PunishmentStatus.Completed || p.status == PunishmentStatus.Failed) &&
                p.tickCompleted > 0 &&
                p.tickCompleted < cutoffTick
            );
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            int currentTick = Find.TickManager.TicksGame;

            // Only check periodically for performance
            if (currentTick - lastCheckTick < CHECK_INTERVAL)
                return;

            lastCheckTick = currentTick;

            // Process all active punishments
            var punishmentsToCheck = GetActivePunishments().ToList();

            foreach (var punishment in punishmentsToCheck)
            {
                ProcessPunishment(punishment);
            }

            // Cleanup old punishments periodically (once per day)
            if (currentTick % GenDate.TicksPerDay == 0)
            {
                CleanupOldPunishments();
            }
        }

        /// <summary>
        /// Process a single punishment, checking for completion conditions.
        /// </summary>
        private void ProcessPunishment(Punishment punishment)
        {
            // Check if criminal is still valid
            if (punishment.criminal == null || punishment.criminal.Dead)
            {
                if (punishment.status != PunishmentStatus.Failed)
                {
                    punishment.Fail("Criminal died");
                    Mod.Log?.Message($"Punishment failed: {punishment.type} - Criminal died");
                }
                return;
            }

            switch (punishment.type)
            {
                case PunishmentType.Imprisonment:
                    ProcessImprisonment(punishment);
                    break;

                case PunishmentType.Fine:
                    ProcessFine(punishment);
                    break;

                case PunishmentType.Beating:
                case PunishmentType.Execution:
                    // These are handled by jobs, just check if pawn is still valid
                    break;

                case PunishmentType.Exile:
                    // Exile completes immediately
                    break;
            }
        }

        /// <summary>
        /// Process imprisonment punishment, checking for release conditions.
        /// </summary>
        private void ProcessImprisonment(Punishment punishment)
        {
            // Start imprisonment if pawn just became a prisoner
            if (punishment.status == PunishmentStatus.Pending && punishment.criminal.IsPrisoner)
            {
                punishment.Start();
                Mod.Log?.Message($"Imprisonment started for {punishment.criminal.NameShortColored} - Duration: {punishment.durationDays} days");
            }

            // Check if imprisonment duration is complete
            if (punishment.status == PunishmentStatus.Active && punishment.IsImprisonmentComplete())
            {
                punishment.Complete();

                // Release the prisoner
                if (punishment.criminal.IsPrisoner && punishment.criminal.guest != null)
                {
                    punishment.criminal.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Guest);
                    Messages.Message(
                        $"{punishment.criminal.NameShortColored} has completed their imprisonment sentence and has been released.",
                        punishment.criminal,
                        MessageTypeDefOf.PositiveEvent
                    );
                }

                Mod.Log?.Message($"Imprisonment completed for {punishment.criminal.NameShortColored}");
            }
        }

        /// <summary>
        /// Process fine punishment, checking for payment completion.
        /// </summary>
        private void ProcessFine(Punishment punishment)
        {
            // Check if fine is paid
            if (punishment.status == PunishmentStatus.Active && punishment.IsFinePaid())
            {
                punishment.Complete();
                RemoveFineHediff(punishment);

                Messages.Message(
                    $"{punishment.criminal.NameShortColored} has fully paid their fine of {punishment.fineAmount} silver.",
                    punishment.criminal,
                    MessageTypeDefOf.PositiveEvent
                );

                Mod.Log?.Message($"Fine paid in full for {punishment.criminal.NameShortColored}: {punishment.fineAmount} silver");
            }
        }

        /// <summary>
        /// Apply the Fine hediff to track the debt.
        /// </summary>
        private void ApplyFineHediff(Punishment punishment)
        {
            if (punishment.criminal == null)
                return;

            // Check if pawn already has a fine hediff
            Hediff_Fine existingFine = punishment.criminal.health?.hediffSet?.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamed("LawAndOrder_Fine")) as Hediff_Fine;

            if (existingFine != null)
            {
                // Add to existing fine
                existingFine.fineAmount += punishment.fineAmount;
                Mod.Log?.Message($"Added {punishment.fineAmount} to existing fine for {punishment.criminal.NameShortColored}. Total: {existingFine.fineAmount}");
            }
            else
            {
                // Create new fine hediff
                HediffDef fineDef = DefDatabase<HediffDef>.GetNamed("LawAndOrder_Fine");
                Hediff_Fine fine = HediffMaker.MakeHediff(fineDef, punishment.criminal) as Hediff_Fine;

                if (fine != null)
                {
                    fine.fineAmount = punishment.fineAmount;
                    fine.finePaid = 0;

                    // Set severity based on amount for mood effect stages
                    if (punishment.fineAmount < 500)
                        fine.Severity = 0.5f; // Small fine stage
                    else if (punishment.fineAmount < 1500)
                        fine.Severity = 1.5f; // Medium fine stage
                    else
                        fine.Severity = 2.5f; // Large fine stage

                    punishment.criminal.health.AddHediff(fine);

                    Mod.Log?.Message($"Fine hediff applied to {punishment.criminal.NameShortColored}: {punishment.fineAmount} silver");
                }
            }
        }

        /// <summary>
        /// Remove the Fine hediff when paid.
        /// </summary>
        private void RemoveFineHediff(Punishment punishment)
        {
            if (punishment.criminal == null)
                return;

            Hediff_Fine fine = punishment.criminal.health?.hediffSet?.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamed("LawAndOrder_Fine")) as Hediff_Fine;

            if (fine != null)
            {
                punishment.criminal.health.RemoveHediff(fine);
                Mod.Log?.Message($"Fine hediff removed for {punishment.criminal.NameShortColored}");
            }
        }

        /// <summary>
        /// Execute exile punishment by banishing the pawn.
        /// </summary>
        private void ExecuteExile(Punishment punishment)
        {
            if (punishment.criminal == null || !punishment.criminal.Spawned)
            {
                punishment.Fail("Criminal not on map");
                return;
            }

            // Use vanilla banishment mechanics
            Faction playerFaction = Faction.OfPlayer;

            // If prisoner, release them first
            if (punishment.criminal.IsPrisoner && punishment.criminal.guest != null)
            {
                punishment.criminal.guest.SetGuestStatus(null, GuestStatus.Guest);
            }

            // Make them leave the map
            punishment.criminal.DeSpawn();

            Messages.Message(
                $"{punishment.criminal.NameShortColored} has been exiled from the colony.",
                MessageTypeDefOf.NeutralEvent
            );

            punishment.Complete();
            Mod.Log?.Message($"Exile executed for {punishment.criminal.NameShortColored}");
        }

        /// <summary>
        /// Queue a beating job for a colonist to carry out.
        /// </summary>
        private void QueueBeatingJob(Punishment punishment)
        {
            if (punishment.criminal == null || !punishment.criminal.Spawned)
            {
                punishment.Fail("Criminal not on map");
                return;
            }

            if (!punishment.criminal.IsPrisoner)
            {
                punishment.Fail("Criminal must be imprisoned");
                return;
            }

            // Find a colonist capable of violence to carry out the beating
            Pawn executioner = FindExecutioner(punishment.criminal.Map);

            if (executioner == null)
            {
                Messages.Message(
                    $"No colonist available to administer beating to {punishment.criminal.NameShortColored}. Punishment will be attempted later.",
                    punishment.criminal,
                    MessageTypeDefOf.NegativeEvent
                );
                return;
            }

            // Create and assign job
            JobDef beatJob = DefDatabase<JobDef>.GetNamed("LawAndOrder_AdministerBeating");
            Job job = JobMaker.MakeJob(beatJob, punishment.criminal);
            executioner.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            Mod.Log?.Message($"Beating job assigned to {executioner.NameShortColored} for {punishment.criminal.NameShortColored}");
        }

        /// <summary>
        /// Queue an execution job for a colonist to carry out.
        /// </summary>
        private void QueueExecutionJob(Punishment punishment)
        {
            if (punishment.criminal == null || !punishment.criminal.Spawned)
            {
                punishment.Fail("Criminal not on map");
                return;
            }

            // Find a colonist capable of violence to carry out the execution
            Pawn executioner = FindExecutioner(punishment.criminal.Map);

            if (executioner == null)
            {
                Messages.Message(
                    $"No colonist available to execute {punishment.criminal.NameShortColored}. Punishment will be attempted later.",
                    punishment.criminal,
                    MessageTypeDefOf.NegativeEvent
                );
                return;
            }

            // Create and assign job
            JobDef execJob = DefDatabase<JobDef>.GetNamed("LawAndOrder_ExecutePrisoner");
            Job job = JobMaker.MakeJob(execJob, punishment.criminal);
            executioner.jobs.TryTakeOrderedJob(job, JobTag.Misc);

            Messages.Message(
                $"{executioner.NameShortColored} has been ordered to execute {punishment.criminal.NameShortColored}.",
                punishment.criminal,
                MessageTypeDefOf.NegativeEvent
            );

            Mod.Log?.Message($"Execution job assigned to {executioner.NameShortColored} for {punishment.criminal.NameShortColored}");
        }

        /// <summary>
        /// Find a suitable colonist to carry out violent punishments.
        /// Prefers colonists with good shooting/melee skills and who are not incapable of violence.
        /// </summary>
        private Pawn FindExecutioner(Map map)
        {
            if (map == null)
                return null;

            var colonists = map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.WorkTagIsDisabled(WorkTags.Violent) && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Shooting).Level + p.skills.GetSkill(SkillDefOf.Melee).Level);

            return colonists.FirstOrDefault();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref activePunishments, "activePunishments", LookMode.Deep);
            Scribe_Values.Look(ref lastCheckTick, "lastCheckTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && activePunishments == null)
            {
                activePunishments = new List<Punishment>();
            }
        }
    }
}
