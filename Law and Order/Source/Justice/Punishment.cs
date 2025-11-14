using System;
using Verse;
using RimWorld;

namespace Law_and_Order.Source.Justice
{
    /// <summary>
    /// Represents an assigned punishment for a convicted criminal.
    /// Tracks punishment status, duration, and completion.
    /// Phase 4: Conviction & Punishment System
    /// </summary>
    public class Punishment : IExposable
    {
        // Core punishment data
        public PunishmentType type = PunishmentType.None;
        public Pawn criminal;
        public int caseId = -1;

        // Timing
        public int tickAssigned; // When the punishment was assigned
        public int tickStarted = -1; // When the punishment execution began (-1 = not started)
        public int tickCompleted = -1; // When the punishment was completed (-1 = not completed)

        // Type-specific data
        public int durationDays = 0; // For imprisonment
        public int fineAmount = 0; // For fines
        public int finePaid = 0; // Amount of fine paid so far

        // Status tracking
        public PunishmentStatus status = PunishmentStatus.Pending;

        // Notes and context
        public string notes = "";

        public Punishment()
        {
            // Required for IExposable
        }

        public Punishment(PunishmentType type, Pawn criminal, int caseId)
        {
            this.type = type;
            this.criminal = criminal;
            this.caseId = caseId;
            this.tickAssigned = Find.TickManager.TicksGame;
            this.status = PunishmentStatus.Pending;
        }

        /// <summary>
        /// Mark the punishment as started.
        /// </summary>
        public void Start()
        {
            if (status == PunishmentStatus.Pending)
            {
                status = PunishmentStatus.Active;
                tickStarted = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// Mark the punishment as completed.
        /// </summary>
        public void Complete()
        {
            if (status == PunishmentStatus.Active || status == PunishmentStatus.Pending)
            {
                status = PunishmentStatus.Completed;
                tickCompleted = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// Mark the punishment as failed (criminal died, escaped, etc.).
        /// </summary>
        public void Fail(string reason)
        {
            status = PunishmentStatus.Failed;
            notes = reason;
            tickCompleted = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Get the target release tick for imprisonment punishments.
        /// </summary>
        public int GetReleaseTick()
        {
            if (type != PunishmentType.Imprisonment || tickStarted < 0)
                return -1;

            return tickStarted + (durationDays * GenDate.TicksPerDay);
        }

        /// <summary>
        /// Check if imprisonment duration has elapsed.
        /// </summary>
        public bool IsImprisonmentComplete()
        {
            if (type != PunishmentType.Imprisonment || status != PunishmentStatus.Active)
                return false;

            return Find.TickManager.TicksGame >= GetReleaseTick();
        }

        /// <summary>
        /// Get remaining days for imprisonment.
        /// </summary>
        public int GetRemainingDays()
        {
            if (type != PunishmentType.Imprisonment || status != PunishmentStatus.Active)
                return 0;

            int releaseTick = GetReleaseTick();
            int remainingTicks = releaseTick - Find.TickManager.TicksGame;
            return Math.Max(0, remainingTicks / GenDate.TicksPerDay);
        }

        /// <summary>
        /// Check if fine is fully paid.
        /// </summary>
        public bool IsFinePaid()
        {
            if (type != PunishmentType.Fine)
                return false;

            return finePaid >= fineAmount;
        }

        /// <summary>
        /// Get remaining fine amount.
        /// </summary>
        public int GetRemainingFine()
        {
            if (type != PunishmentType.Fine)
                return 0;

            return Math.Max(0, fineAmount - finePaid);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type", PunishmentType.None);
            Scribe_References.Look(ref criminal, "criminal");
            Scribe_Values.Look(ref caseId, "caseId", -1);
            Scribe_Values.Look(ref tickAssigned, "tickAssigned", 0);
            Scribe_Values.Look(ref tickStarted, "tickStarted", -1);
            Scribe_Values.Look(ref tickCompleted, "tickCompleted", -1);
            Scribe_Values.Look(ref durationDays, "durationDays", 0);
            Scribe_Values.Look(ref fineAmount, "fineAmount", 0);
            Scribe_Values.Look(ref finePaid, "finePaid", 0);
            Scribe_Values.Look(ref status, "status", PunishmentStatus.Pending);
            Scribe_Values.Look(ref notes, "notes", "");
        }

        public override string ToString()
        {
            string durationInfo = type switch
            {
                PunishmentType.Imprisonment => $"{durationDays} days",
                PunishmentType.Fine => $"{fineAmount} silver",
                _ => ""
            };

            return $"{type} ({status}) {durationInfo}";
        }
    }

    /// <summary>
    /// Status of an assigned punishment.
    /// </summary>
    public enum PunishmentStatus
    {
        /// <summary>
        /// Punishment assigned but not yet started.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Punishment is currently being carried out.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Punishment has been completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Punishment could not be completed (criminal died, escaped, etc.).
        /// </summary>
        Failed = 3
    }
}
