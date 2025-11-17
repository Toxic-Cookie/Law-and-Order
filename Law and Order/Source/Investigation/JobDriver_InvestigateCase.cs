using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// JobDriver for wardens to interrogate prisoners.
    /// Escorts the prisoner to a designated interrogation table, performs interrogation,
    /// then returns them to their bed.
    /// </summary>
    public class JobDriver_InvestigateCase : JobDriver
    {
        private const int INTERROGATION_BASE_DURATION_TICKS = 3000; // ~50 seconds base (much longer!)
        private const int DRAMATIC_ACTION_INTERVAL_TICKS = 300; // ~5 seconds between dramatic actions
        private const int WARMUP_TICKS = 120; // ~2 seconds to get started

        // Job targets
        protected Pawn Prisoner => (Pawn)job.targetA.Thing;
        protected Thing InterrogationTable => job.targetB.Thing;

        // Cached chairs
        private Thing prisonerChair;
        private Thing wardenChair;
        private IntVec3 prisonerPosition;
        private IntVec3 wardenPosition;

        private int dramaticActionsPerformed = 0;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the prisoner
            if (!pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            // Reserve the interrogation table
            if (!pawn.Reserve(InterrogationTable, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions
            this.FailOnDestroyedOrNull(TargetIndex.A); // Prisoner despawned
            this.FailOnDestroyedOrNull(TargetIndex.B); // Table despawned
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A); // Prisoner mental break
            this.FailOn(() => !Prisoner.IsPrisonerOfColony || !Prisoner.guest.PrisonerIsSecure);

            // Check if table is still designated
            this.FailOn(() =>
            {
                var comp = InterrogationTable.TryGetComp<Comp_InterrogationTable>();
                return comp == null || !comp.IsDesignated;
            });

            // Handle cleanup if job ends prematurely
            this.AddFinishAction(delegate
            {
                if (pawn.carryTracker.CarriedThing != null)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out Thing _, null);
                }
            });

            // Step 1: Go to prisoner
            Toil goToPrisoner = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !InterrogationSystem.CanInterrogate(Prisoner))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            // Skip to returning if already carrying
            yield return Toils_Jump.JumpIf(goToPrisoner, () => pawn.IsCarryingPawn(Prisoner));

            yield return goToPrisoner;

            // Step 2: Pick up the prisoner
            Toil ensureCount = new Toil();
            ensureCount.initAction = delegate
            {
                // Ensure count is set (defensive programming)
                if (job.count <= 0)
                {
                    job.count = 1;
                }
            };
            ensureCount.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return ensureCount;

            Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false, true, false);
            yield return startCarrying;

            // Step 3: Carry prisoner to interrogation table
            Toil goToTable = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell)
                .FailOn(() => !pawn.IsCarryingPawn(Prisoner));
            yield return goToTable;

            // Step 4: Find and setup interrogation positions/chairs
            yield return SetupInterrogationArea();

            // Step 5: Place prisoner in designated position/chair
            yield return PlacePrisonerForInterrogation();

            // Step 6: Warm up - warden prepares for interrogation
            yield return WarmupInterrogation();

            // Step 7-N: Perform multiple dramatic interrogation actions
            // We'll do 3-5 dramatic actions throughout the interrogation
            int numDramaticActions = Rand.RangeInclusive(3, 5);
            for (int i = 0; i < numDramaticActions; i++)
            {
                yield return PerformDramaticAction();
            }

            // Final: Conclude interrogation and get results
            yield return ConcludeInterrogation();

            // Step 7: Set last interaction time
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);

            // Step 8: Return prisoner to their bed
            yield return ReturnPrisonerToBed();
        }

        /// <summary>
        /// Toil to return the prisoner to their bed after interrogation
        /// </summary>
        private Toil ReturnPrisonerToBed()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Use the vanilla warden system to return them to bed
                WorkGiver_Warden_TakeToBed.TryTakePrisonerToBed(Prisoner, pawn);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        /// <summary>
        /// Setup the interrogation area - find chairs and positions
        /// </summary>
        private Toil SetupInterrogationArea()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Find interrogation chair for prisoner (closest to table)
                prisonerChair = Comp_InterrogationChair.FindNearestInterrogationChair(
                    InterrogationTable.Position,
                    pawn.Map,
                    pawn
                );

                // Determine prisoner position
                if (prisonerChair != null && prisonerChair is Building_Bed bed)
                {
                    prisonerPosition = bed.InteractionCell;
                }
                else if (prisonerChair != null)
                {
                    prisonerPosition = prisonerChair.InteractionCell;
                }
                else
                {
                    // No designated chair - just place near table
                    prisonerPosition = InterrogationTable.InteractionCell;
                }

                // Find a chair for the warden (any chair near the table, not designated for interrogation)
                wardenChair = FindWardenChair();
                wardenPosition = wardenChair != null ? wardenChair.InteractionCell : pawn.Position;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        /// <summary>
        /// Place the prisoner in their designated position/chair
        /// </summary>
        private Toil PlacePrisonerForInterrogation()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == Prisoner)
                {
                    pawn.carryTracker.TryDropCarriedThing(prisonerPosition, ThingPlaceMode.Direct, out Thing _, null);
                }

                // If there's a chair, make prisoner sit
                if (prisonerChair != null && prisonerChair is Building_Bed bed)
                {
                    Job sitJob = JobMaker.MakeJob(JobDefOf.LayDown, prisonerChair);
                    sitJob.forceSleep = false;
                    Prisoner.jobs.StartJob(sitJob, JobCondition.InterruptForced);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        /// <summary>
        /// Warmup phase - warden gets into position
        /// </summary>
        private Toil WarmupInterrogation()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                pawn.pather.StopDead();
                pawn.jobs.posture = PawnPosture.Standing;
            };
            toil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Prisoner);
                if (Prisoner.Spawned)
                {
                    Prisoner.rotationTracker.FaceTarget(pawn);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = WARMUP_TICKS;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.socialMode = RandomSocialMode.Off;
            return toil;
        }

        /// <summary>
        /// Perform one dramatic interrogation action
        /// </summary>
        private Toil PerformDramaticAction()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Choose a random dramatic action
                DoDramaticAction();
            };
            toil.tickAction = delegate
            {
                // Always face the prisoner
                pawn.rotationTracker.FaceTarget(Prisoner);
                if (Prisoner.Spawned && prisonerChair != null)
                {
                    Prisoner.rotationTracker.FaceTarget(pawn);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = DRAMATIC_ACTION_INTERVAL_TICKS;
            toil.socialMode = RandomSocialMode.Off;
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            return toil;
        }

        /// <summary>
        /// Conclude the interrogation and calculate results
        /// </summary>
        private Toil ConcludeInterrogation()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                // Perform the interrogation when toil completes
                InterrogationResult result = InterrogationSystem.PerformInterrogation(
                    pawn,
                    Prisoner,
                    InterrogationTable
                );

                // Show result message
                ShowInterrogationResult(result);

                // Phase 6: Check for truth discovery about false accusations
                TruthDiscoveryUtils.TruthDiscoveryResult truthResult =
                    TruthDiscoveryUtils.TryDiscoverTruthDuringInterrogation(pawn, Prisoner);

                if (truthResult != null && truthResult.truthRevealed)
                {
                    // Apply truth discovery
                    TruthDiscoveryUtils.ApplyTruthDiscovery(truthResult);

                    // Exonerate the falsely accused pawn
                    if (truthResult.falselyAccused != null)
                    {
                        ExonerationSystem.ExoneratePawn(
                            truthResult.falselyAccused,
                            truthResult.falseAccusationCrime,
                            truthResult.falseAccuser
                        );
                    }
                }

                // Grant social XP to warden
                pawn.skills?.Learn(SkillDefOf.Social, 200f); // More XP for longer process

                // Release prisoner from chair if sitting
                if (Prisoner.CurJob != null && Prisoner.CurJob.def == JobDefOf.LayDown)
                {
                    Prisoner.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        /// <summary>
        /// Display the interrogation result to the player
        /// </summary>
        private void ShowInterrogationResult(InterrogationResult result)
        {
            if (result.wasSuccessful && result.crimesConfessed.Count > 0)
            {
                // Success - crimes confessed
                string crimeList = string.Join(", ", result.crimesConfessed.ConvertAll(c => c.GetCrimeLabel()));

                Messages.Message(
                    "LawAndOrder_Interrogation_Success".Translate(
                        pawn.LabelShort,
                        Prisoner.LabelShort,
                        result.crimesConfessed.Count,
                        crimeList
                    ),
                    Prisoner,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else if (result.falseConfession)
            {
                // False confession
                Messages.Message(
                    "LawAndOrder_Interrogation_FalseConfession".Translate(
                        pawn.LabelShort,
                        Prisoner.LabelShort
                    ),
                    Prisoner,
                    MessageTypeDefOf.NeutralEvent
                );
            }
            else
            {
                // Failure
                Messages.Message(
                    result.failureReason.Translate(pawn.LabelShort, Prisoner.LabelShort),
                    Prisoner,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        /// <summary>
        /// Find a chair for the warden to use during interrogation
        /// </summary>
        private Thing FindWardenChair()
        {
            // Find any sittable thing near the interrogation table that's NOT designated as an interrogation chair
            List<Thing> nearbyThings = GenRadial.RadialDistinctThingsAround(
                InterrogationTable.Position,
                pawn.Map,
                8f, // 8 tile radius
                true
            ).ToList();

            foreach (Thing thing in nearbyThings)
            {
                // Check if it's a sittable building
                if (thing is Building_Bed bed && bed.def.building.isSittable)
                {
                    // Make sure it's NOT designated as an interrogation chair
                    Comp_InterrogationChair chairComp = thing.TryGetComp<Comp_InterrogationChair>();
                    if (chairComp == null || !chairComp.IsDesignated)
                    {
                        // This is a regular chair the warden can use
                        if (pawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Deadly))
                        {
                            return thing;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Execute a random dramatic action during interrogation
        /// </summary>
        private void DoDramaticAction()
        {
            dramaticActionsPerformed++;

            // List of possible dramatic actions
            List<System.Action> possibleActions = new List<System.Action>
            {
                () => ActionSitInChair(),
                () => ActionStandUp(),
                () => ActionPaceAround(),
                () => ActionSlamTable(),
                () => ActionDamageFurniture(),
                () => ActionYell(),
                () => ActionThreaten(),
                () => ActionSlap(),
                () => ActionInterrogate()
            };

            // Filter based on context
            List<System.Action> validActions = new List<System.Action>();

            // Can only sit if there's a warden chair and not already sitting
            if (wardenChair != null && pawn.jobs.posture != PawnPosture.LayingInBed)
            {
                validActions.Add(() => ActionSitInChair());
            }

            // Can only stand if currently sitting
            if (pawn.jobs.posture == PawnPosture.LayingInBed)
            {
                validActions.Add(() => ActionStandUp());
            }

            // Always available actions
            validActions.Add(() => ActionPaceAround());
            validActions.Add(() => ActionSlamTable());
            validActions.Add(() => ActionYell());
            validActions.Add(() => ActionThreaten());
            validActions.Add(() => ActionInterrogate());

            // Damaging actions - less frequent
            if (Rand.Chance(0.3f))
            {
                validActions.Add(() => ActionDamageFurniture());
                validActions.Add(() => ActionSlap());
            }

            // Execute a random valid action
            if (validActions.Count > 0)
            {
                validActions.RandomElement()();
            }
        }

        // ========== DRAMATIC ACTIONS ==========

        private void ActionSitInChair()
        {
            if (wardenChair != null && wardenChair is Building_Bed bed)
            {
                pawn.jobs.posture = PawnPosture.LayingInBed;
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "LawAndOrder_Action_Sit".Translate(), 3f);
            }
        }

        private void ActionStandUp()
        {
            pawn.jobs.posture = PawnPosture.Standing;
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "LawAndOrder_Action_StandUp".Translate(), 3f);
        }

        private void ActionPaceAround()
        {
            // Warden paces around the prisoner menacingly
            IntVec3 paceTarget = Prisoner.Position + GenRadial.RadialPattern[Rand.Range(1, 8)];
            if (paceTarget.InBounds(pawn.Map) && paceTarget.Standable(pawn.Map))
            {
                pawn.pather.StartPath(paceTarget, PathEndMode.OnCell);
            }
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "LawAndOrder_Action_Pace".Translate(), 3f);
        }

        private void ActionSlamTable()
        {
            // Slam fist on table for effect
            FleckMaker.ThrowDustPuffThick(InterrogationTable.DrawPos, pawn.Map, 1f, Color.gray);
            MoteMaker.ThrowText(InterrogationTable.DrawPos, pawn.Map, "*SLAM*", Color.red, 3f);

            // Add social interaction
            AddInterrogationSpeech("LawAndOrder_Speech_SlamTable");
        }

        private void ActionDamageFurniture()
        {
            // Slightly damage a random piece of furniture in the room
            Room room = InterrogationTable.GetRoom();
            if (room != null)
            {
                List<Thing> furniture = room.ContainedAndAdjacentThings.Where(t =>
                    t is Building &&
                    t.def.useHitPoints &&
                    t.HitPoints > 1
                ).ToList();

                if (furniture.Count > 0)
                {
                    Thing target = furniture.RandomElement();
                    int damage = Rand.Range(1, 5);
                    target.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage));
                    MoteMaker.ThrowText(target.DrawPos, pawn.Map, $"-{damage}", Color.yellow, 3f);
                }
            }
        }

        private void ActionYell()
        {
            // Yell at the prisoner
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
            AddInterrogationSpeech("LawAndOrder_Speech_Yell");
        }

        private void ActionThreaten()
        {
            // Threaten the prisoner
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.IncapIcon);
            AddInterrogationSpeech("LawAndOrder_Speech_Threaten");
        }

        private void ActionSlap()
        {
            // Slap the prisoner (minor damage)
            int damage = Rand.Range(1, 3);
            Prisoner.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage, 0, -1, pawn));
            FleckMaker.ThrowMicroSparks(Prisoner.DrawPos, pawn.Map);
            MoteMaker.ThrowText(Prisoner.DrawPos, pawn.Map, "*SLAP*", Color.red, 3f);

            AddInterrogationSpeech("LawAndOrder_Speech_Slap");
        }

        private void ActionInterrogate()
        {
            // Standard interrogation question
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
            AddInterrogationSpeech("LawAndOrder_Speech_Question");
        }

        /// <summary>
        /// Add a speech interaction that shows in the social log
        /// </summary>
        private void AddInterrogationSpeech(string translationKey)
        {
            string speech = translationKey.Translate(pawn.LabelShort, Prisoner.LabelShort);

            // Create social interaction for log (this will show in social tab)
            pawn.interactions.TryInteractWith(Prisoner, InteractionDefOf.Insult);
        }
    }
}
