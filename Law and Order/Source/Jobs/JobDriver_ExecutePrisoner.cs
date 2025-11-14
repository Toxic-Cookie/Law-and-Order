using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;

namespace Law_and_Order.Source.Jobs
{
    /// <summary>
    /// JobDriver for executing a prisoner.
    /// Uses vanilla execution mechanics with Law and Order punishment tracking.
    /// Phase 4: Execution Punishment System
    /// </summary>
    public class JobDriver_ExecutePrisoner : JobDriver
    {
        private const int EXECUTION_DURATION_TICKS = 180; // ~3 seconds

        protected Pawn Victim => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOn(() => !Victim.IsPrisonerOfColony && !Victim.Downed);

            // Go to prisoner
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Execute
            Toil execution = new Toil();
            execution.initAction = () =>
            {
                execution.actor.pather.StopDead();

                // Start punishment
                var punishmentManager = WorldComponent_PunishmentManager.Instance;
                if (punishmentManager != null)
                {
                    var punishments = punishmentManager.GetPunishmentsForPawn(Victim);
                    var executionPunishment = punishments.Find(p =>
                        p.type == PunishmentType.Execution &&
                        p.status == PunishmentStatus.Pending
                    );

                    if (executionPunishment != null)
                    {
                        executionPunishment.Start();
                    }
                }
            };

            execution.tickAction = () =>
            {
                // Face the victim
                pawn.rotationTracker.FaceTarget(Victim);

                // Check if duration completed
                if (execution.actor.jobs.curDriver.ticksLeftThisToil <= 0)
                {
                    ExecuteVictim();
                    ReadyForNextToil();
                }
            };

            execution.defaultCompleteMode = ToilCompleteMode.Never;
            execution.defaultDuration = EXECUTION_DURATION_TICKS;
            execution.WithProgressBar(TargetIndex.A, () => 1f - ((float)execution.actor.jobs.curDriver.ticksLeftThisToil / EXECUTION_DURATION_TICKS));
            execution.socialMode = RandomSocialMode.Off;

            yield return execution;
        }

        private void ExecuteVictim()
        {
            if (Victim == null || Victim.Dead)
            {
                CompletePunishment(failed: true);
                return;
            }

            // Use vanilla execution method - instant kill via ExecutionUtility if available
            // Otherwise, deal lethal damage to a vital organ
            ExecuteVictimInternal();

            // Message
            Messages.Message(
                $"{pawn.NameShortColored} has executed {Victim.NameShortColored}.",
                new TargetInfo(Victim.Corpse),
                MessageTypeDefOf.NegativeEvent
            );

            Mod.Log?.Message($"Execution carried out by {pawn.NameShortColored} on {Victim.NameShortColored}");

            // Complete punishment
            CompletePunishment(failed: false);
        }

        private void ExecuteVictimInternal()
        {
            // Try to use vanilla execution utility if available
            // Otherwise fall back to direct damage

            // Get a vital body part (head, neck, or torso)
            BodyPartRecord vitalPart = GetVitalBodyPart();

            if (vitalPart != null)
            {
                // Deal lethal damage to vital part
                float damage = Victim.health.hediffSet.GetPartHealth(vitalPart) + 10f; // Enough to kill

                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Cut,
                    damage,
                    999f, // High armor penetration
                    -1f,
                    pawn,
                    vitalPart,
                    pawn.equipment?.Primary?.def,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    Victim
                );

                Victim.TakeDamage(damageInfo);
            }

            // Ensure death
            if (!Victim.Dead)
            {
                Victim.Kill(null);
            }
        }

        private BodyPartRecord GetVitalBodyPart()
        {
            // Try to get head/neck first
            BodyPartRecord head = Victim.health.hediffSet.GetBrain();
            if (head != null)
                return head;

            // Try neck
            BodyPartRecord neck = Victim.RaceProps.body.AllParts.Find(p => p.def.defName == "Neck");
            if (neck != null)
                return neck;

            // Fall back to torso
            return Victim.RaceProps.body.corePart;
        }

        private void CompletePunishment(bool failed)
        {
            // Find the punishment record and mark it complete
            var punishmentManager = WorldComponent_PunishmentManager.Instance;
            if (punishmentManager != null)
            {
                var punishments = punishmentManager.GetPunishmentsForPawn(Victim);
                var execution = punishments.Find(p =>
                    p.type == PunishmentType.Execution &&
                    (p.status == PunishmentStatus.Pending || p.status == PunishmentStatus.Active)
                );

                if (execution != null)
                {
                    if (failed)
                    {
                        execution.Fail("Victim died before execution could be completed");
                    }
                    else
                    {
                        execution.Complete();
                    }
                }
            }
        }
    }
}
