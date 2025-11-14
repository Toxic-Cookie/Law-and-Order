using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Components;

namespace Law_and_Order.Source.Jobs
{
    /// <summary>
    /// JobDriver for administering a beating punishment.
    /// The colonist will go to the prisoner and inflict non-lethal damage.
    /// Phase 4: Beating Punishment System
    /// </summary>
    public class JobDriver_AdministerBeating : JobDriver
    {
        private const int BEATING_DURATION_TICKS = 180; // ~3 seconds
        private const float BASE_DAMAGE = 15f; // Base damage per beating

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
            this.FailOn(() => !Victim.IsPrisonerOfColony);

            // Go to prisoner
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Administer beating
            Toil beating = new Toil();
            beating.initAction = () =>
            {
                beating.actor.pather.StopDead();
            };
            beating.tickAction = () =>
            {
                // Face the victim
                pawn.rotationTracker.FaceTarget(Victim);

                // Check if duration completed
                if (beating.actor.jobs.curDriver.ticksLeftThisToil <= 0)
                {
                    ApplyBeatingDamage();
                    ReadyForNextToil();
                }
            };
            beating.defaultCompleteMode = ToilCompleteMode.Never;
            beating.defaultDuration = BEATING_DURATION_TICKS;
            beating.WithProgressBar(TargetIndex.A, () => 1f - ((float)beating.actor.jobs.curDriver.ticksLeftThisToil / BEATING_DURATION_TICKS));
            beating.socialMode = RandomSocialMode.Off;

            yield return beating;

            // Complete punishment
            Toil complete = new Toil();
            complete.initAction = () =>
            {
                CompletePunishment();
            };
            complete.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return complete;
        }

        private void ApplyBeatingDamage()
        {
            if (Victim == null || Victim.Dead)
                return;

            // Apply multiple strikes to different body parts
            int strikeCount = Rand.RangeInclusive(3, 5);

            for (int i = 0; i < strikeCount; i++)
            {
                // Random body part
                BodyPartRecord bodyPart = Victim.health.hediffSet.GetRandomNotMissingPart(
                    DamageDefOf.Blunt,
                    BodyPartHeight.Undefined,
                    BodyPartDepth.Outside
                );

                if (bodyPart != null)
                {
                    // Calculate damage (non-lethal, scaled down for non-vital parts)
                    float damage = BASE_DAMAGE * Rand.Range(0.5f, 1.2f);

                    // Reduce damage if hitting vital parts to prevent accidental death
                    if (bodyPart.def.tags?.Contains(BodyPartTagDefOf.ConsciousnessSource) == true)
                    {
                        damage *= 0.3f;
                    }

                    // Apply blunt damage
                    DamageInfo damageInfo = new DamageInfo(
                        DamageDefOf.Blunt,
                        damage,
                        0f,
                        -1f,
                        pawn,
                        bodyPart,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        Victim
                    );

                    Victim.TakeDamage(damageInfo);
                }
            }

            // Add pain
            Hediff painHediff = HediffMaker.MakeHediff(HediffDefOf.WoundInfection, Victim);
            Victim.health.AddHediff(painHediff);

            // Message
            Messages.Message(
                $"{pawn.NameShortColored} has administered a beating to {Victim.NameShortColored}.",
                new TargetInfo(Victim),
                MessageTypeDefOf.NeutralEvent
            );

            Mod.Log?.Message($"Beating administered by {pawn.NameShortColored} to {Victim.NameShortColored}");
        }

        private void CompletePunishment()
        {
            // Find the punishment record and mark it complete
            var punishmentManager = WorldComponent_PunishmentManager.Instance;
            if (punishmentManager != null)
            {
                var punishments = punishmentManager.GetPunishmentsForPawn(Victim);
                var beating = punishments.Find(p =>
                    p.type == PunishmentType.Beating &&
                    (p.status == PunishmentStatus.Pending || p.status == PunishmentStatus.Active)
                );

                if (beating != null)
                {
                    if (beating.status == PunishmentStatus.Pending)
                        beating.Start();

                    beating.Complete();

                    Messages.Message(
                        $"{Victim.NameShortColored}'s beating punishment has been completed.",
                        new TargetInfo(Victim),
                        MessageTypeDefOf.TaskCompletion
                    );
                }
            }
        }
    }
}
