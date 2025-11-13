using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LawAndOrder
{
    /// <summary>
    /// Manages crime penalty definitions globally across the game world.
    /// </summary>
    public class WorldComponent_CrimePenaltyManager : WorldComponent
    {
        // Lockout duration: 15 days (one quadrum)
        // RimWorld: 1 day = 60,000 ticks
        private const int LOCKOUT_DURATION_TICKS = 15 * 60000;

        // Penalty caps
        private const int MIN_PENALTY = 1;
        private const int MAX_PENALTY = 100000;

        private List<CrimeDefinition> crimeDefinitions = new List<CrimeDefinition>();
        private List<CrimePenaltyMultiplier> penaltyMultipliers = new List<CrimePenaltyMultiplier>();
        internal int lastCommitTick = -1; // -1 means never committed (internal for UI access)

        public WorldComponent_CrimePenaltyManager(World world) : base(world)
        {
            // Initialize defaults for new worlds
            InitializeDefaults();
        }

        /// <summary>
        /// Gets all crime definitions.
        /// </summary>
        public List<CrimeDefinition> CrimeDefinitions => crimeDefinitions;

        /// <summary>
        /// Gets all penalty multipliers.
        /// </summary>
        public List<CrimePenaltyMultiplier> PenaltyMultipliers => penaltyMultipliers;

        /// <summary>
        /// Gets a crime definition by defName.
        /// </summary>
        public CrimeDefinition GetCrimeDefinition(string defName)
        {
            return crimeDefinitions.FirstOrDefault(cd => cd.defName == defName);
        }

        /// <summary>
        /// Gets a penalty multiplier by type.
        /// </summary>
        public CrimePenaltyMultiplier GetMultiplier(MultiplierType type)
        {
            return penaltyMultipliers.FirstOrDefault(m => m.multiplierType == type);
        }

        /// <summary>
        /// Adds or updates a crime definition.
        /// </summary>
        public void SetCrimeDefinition(CrimeDefinition crime)
        {
            // Validate penalty range
            crime.minPenalty = Mathf.Clamp(crime.minPenalty, MIN_PENALTY, MAX_PENALTY);
            crime.maxPenalty = Mathf.Clamp(crime.maxPenalty, MIN_PENALTY, MAX_PENALTY);

            if (crime.minPenalty > crime.maxPenalty)
            {
                int temp = crime.minPenalty;
                crime.minPenalty = crime.maxPenalty;
                crime.maxPenalty = temp;
            }

            var existing = GetCrimeDefinition(crime.defName);
            if (existing != null)
            {
                // Update existing
                existing.label = crime.label;
                existing.description = crime.description;
                existing.severity = crime.severity;
                existing.minPenalty = crime.minPenalty;
                existing.maxPenalty = crime.maxPenalty;
                existing.category = crime.category;
            }
            else
            {
                // Add new
                crimeDefinitions.Add(crime);
            }
        }

        /// <summary>
        /// Removes a crime definition.
        /// </summary>
        public void RemoveCrimeDefinition(string defName)
        {
            crimeDefinitions.RemoveAll(cd => cd.defName == defName);
        }

        /// <summary>
        /// Sets or updates a penalty multiplier.
        /// </summary>
        public void SetMultiplier(CrimePenaltyMultiplier multiplier)
        {
            var existing = GetMultiplier(multiplier.multiplierType);
            if (existing != null)
            {
                existing.multiplierValue = multiplier.multiplierValue;
                existing.description = multiplier.description;
                existing.enabled = multiplier.enabled;
            }
            else
            {
                penaltyMultipliers.Add(multiplier);
            }
        }

        /// <summary>
        /// Records that crime penalty changes have been committed.
        /// </summary>
        public void CommitAllPendingChanges()
        {
            // Commit crime definition changes
            foreach (var crime in crimeDefinitions)
            {
                crime.CommitPendingChanges();
            }

            // Commit multiplier changes
            foreach (var multiplier in penaltyMultipliers)
            {
                multiplier.CommitPendingChanges();
            }

            // Record commit time
            lastCommitTick = Find.TickManager.TicksGame;
        }

        /// <summary>
        /// Cancels all pending changes.
        /// </summary>
        public void CancelAllPendingChanges()
        {
            foreach (var crime in crimeDefinitions)
            {
                crime.ClearPendingChanges();
            }

            foreach (var multiplier in penaltyMultipliers)
            {
                multiplier.ClearPendingChanges();
            }
        }

        /// <summary>
        /// Checks if there are any pending changes.
        /// </summary>
        public bool HasPendingChanges()
        {
            return crimeDefinitions.Any(cd => cd.hasPendingChanges) ||
                   penaltyMultipliers.Any(m => m.hasPendingChanges);
        }

        /// <summary>
        /// Checks if crime penalty changes are currently locked out.
        /// God mode bypasses the lockout.
        /// </summary>
        public bool IsInLockout()
        {
            // God mode bypasses lockout
            if (Verse.DebugSettings.godMode)
            {
                return false;
            }

            if (lastCommitTick < 0)
            {
                return false; // Never committed before
            }

            int ticksSinceCommit = Find.TickManager.TicksGame - lastCommitTick;
            return ticksSinceCommit < LOCKOUT_DURATION_TICKS;
        }

        /// <summary>
        /// Gets the number of days remaining in the lockout period.
        /// </summary>
        public float GetLockoutDaysRemaining()
        {
            if (!IsInLockout())
            {
                return 0f;
            }

            int ticksSinceCommit = Find.TickManager.TicksGame - lastCommitTick;
            int ticksRemaining = LOCKOUT_DURATION_TICKS - ticksSinceCommit;
            return ticksRemaining / 60000f; // Convert ticks to days
        }

        /// <summary>
        /// Gets the tick when the lockout will expire.
        /// </summary>
        public int GetLockoutExpiryTick()
        {
            if (lastCommitTick < 0)
            {
                return -1;
            }
            return lastCommitTick + LOCKOUT_DURATION_TICKS;
        }

        /// <summary>
        /// Initializes default crime definitions and multipliers if the lists are empty.
        /// </summary>
        public void InitializeDefaults()
        {
            if (crimeDefinitions.Count == 0)
            {
                InitializeDefaultCrimes();
            }

            if (penaltyMultipliers.Count == 0)
            {
                InitializeDefaultMultipliers();
            }
        }

        /// <summary>
        /// Initializes default crime definitions based on the comprehensive list.
        /// </summary>
        private void InitializeDefaultCrimes()
        {
            // CRIMES AGAINST PERSONS - LETHAL
            crimeDefinitions.Add(new CrimeDefinition("Execution", "LawAndOrder_Crime_Execution".Translate(), "LawAndOrder_Crime_Execution_Desc".Translate(), CrimeSeverity.Critical, 4000, 8000, CrimeCategory.Lethal));
            crimeDefinitions.Add(new CrimeDefinition("VitalOrganDestruction", "LawAndOrder_Crime_VitalOrganDestruction".Translate(), "LawAndOrder_Crime_VitalOrganDestruction_Desc".Translate(), CrimeSeverity.Critical, 3500, 7000, CrimeCategory.Lethal));
            crimeDefinitions.Add(new CrimeDefinition("Murder", "LawAndOrder_Crime_Murder".Translate(), "LawAndOrder_Crime_Murder_Desc".Translate(), CrimeSeverity.Critical, 2500, 5000, CrimeCategory.Lethal));
            crimeDefinitions.Add(new CrimeDefinition("Kidnapping", "LawAndOrder_Crime_Kidnapping".Translate(), "LawAndOrder_Crime_Kidnapping_Desc".Translate(), CrimeSeverity.Critical, 2000, 4000, CrimeCategory.Lethal, usesRangeCalculation: false)); // Always uses fixed 2000§
            crimeDefinitions.Add(new CrimeDefinition("Vaporization", "LawAndOrder_Crime_Vaporization".Translate(), "LawAndOrder_Crime_Vaporization_Desc".Translate(), CrimeSeverity.Critical, 2500, 5000, CrimeCategory.Lethal));

            // CRIMES AGAINST PERSONS - SEVERE INJURY
            crimeDefinitions.Add(new CrimeDefinition("LimbDestruction", "LawAndOrder_Crime_LimbDestruction".Translate(), "LawAndOrder_Crime_LimbDestruction_Desc".Translate(), CrimeSeverity.High, 800, 1800, CrimeCategory.SevereInjury));
            crimeDefinitions.Add(new CrimeDefinition("EyeDestruction", "LawAndOrder_Crime_EyeDestruction".Translate(), "LawAndOrder_Crime_EyeDestruction_Desc".Translate(), CrimeSeverity.High, 700, 1500, CrimeCategory.SevereInjury));
            crimeDefinitions.Add(new CrimeDefinition("MajorOrganDamage", "LawAndOrder_Crime_MajorOrganDamage".Translate(), "LawAndOrder_Crime_MajorOrganDamage_Desc".Translate(), CrimeSeverity.High, 600, 1200, CrimeCategory.SevereInjury));
            crimeDefinitions.Add(new CrimeDefinition("SevereBurns", "LawAndOrder_Crime_SevereBurns".Translate(), "LawAndOrder_Crime_SevereBurns_Desc".Translate(), CrimeSeverity.High, 400, 900, CrimeCategory.SevereInjury));
            crimeDefinitions.Add(new CrimeDefinition("MajorBloodLoss", "LawAndOrder_Crime_MajorBloodLoss".Translate(), "LawAndOrder_Crime_MajorBloodLoss_Desc".Translate(), CrimeSeverity.High, 400, 800, CrimeCategory.SevereInjury));
            crimeDefinitions.Add(new CrimeDefinition("SpineInjury", "LawAndOrder_Crime_SpineInjury".Translate(), "LawAndOrder_Crime_SpineInjury_Desc".Translate(), CrimeSeverity.High, 800, 1800, CrimeCategory.SevereInjury));

            // CRIMES AGAINST PERSONS - MODERATE INJURY
            crimeDefinitions.Add(new CrimeDefinition("GunshotWound", "LawAndOrder_Crime_GunshotWound".Translate(), "LawAndOrder_Crime_GunshotWound_Desc".Translate(), CrimeSeverity.Moderate, 150, 350, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("StabWound", "LawAndOrder_Crime_StabWound".Translate(), "LawAndOrder_Crime_StabWound_Desc".Translate(), CrimeSeverity.Moderate, 130, 300, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("SlashWound", "LawAndOrder_Crime_SlashWound".Translate(), "LawAndOrder_Crime_SlashWound_Desc".Translate(), CrimeSeverity.Moderate, 120, 280, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("BluntTrauma", "LawAndOrder_Crime_BluntTrauma".Translate(), "LawAndOrder_Crime_BluntTrauma_Desc".Translate(), CrimeSeverity.Moderate, 120, 280, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("BiteWound", "LawAndOrder_Crime_BiteWound".Translate(), "LawAndOrder_Crime_BiteWound_Desc".Translate(), CrimeSeverity.Moderate, 100, 220, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("ExplosiveInjury", "LawAndOrder_Crime_ExplosiveInjury".Translate(), "LawAndOrder_Crime_ExplosiveInjury_Desc".Translate(), CrimeSeverity.Moderate, 200, 450, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("ArrowWound", "LawAndOrder_Crime_ArrowWound".Translate(), "LawAndOrder_Crime_ArrowWound_Desc".Translate(), CrimeSeverity.Moderate, 120, 280, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("ModerateBurns", "LawAndOrder_Crime_ModerateBurns".Translate(), "LawAndOrder_Crime_ModerateBurns_Desc".Translate(), CrimeSeverity.Moderate, 150, 350, CrimeCategory.ModerateInjury));
            crimeDefinitions.Add(new CrimeDefinition("Frostbite", "LawAndOrder_Crime_Frostbite".Translate(), "LawAndOrder_Crime_Frostbite_Desc".Translate(), CrimeSeverity.Moderate, 120, 280, CrimeCategory.ModerateInjury));

            // CRIMES AGAINST PERSONS - MINOR INJURY
            crimeDefinitions.Add(new CrimeDefinition("Scratch", "LawAndOrder_Crime_Scratch".Translate(), "LawAndOrder_Crime_Scratch_Desc".Translate(), CrimeSeverity.Low, 30, 80, CrimeCategory.MinorInjury));
            crimeDefinitions.Add(new CrimeDefinition("Bruise", "LawAndOrder_Crime_Bruise".Translate(), "LawAndOrder_Crime_Bruise_Desc".Translate(), CrimeSeverity.Low, 20, 60, CrimeCategory.MinorInjury));
            crimeDefinitions.Add(new CrimeDefinition("MinorBurns", "LawAndOrder_Crime_MinorBurns".Translate(), "LawAndOrder_Crime_MinorBurns_Desc".Translate(), CrimeSeverity.Low, 40, 100, CrimeCategory.MinorInjury));
            crimeDefinitions.Add(new CrimeDefinition("SuperficialWound", "LawAndOrder_Crime_SuperficialWound".Translate(), "LawAndOrder_Crime_SuperficialWound_Desc".Translate(), CrimeSeverity.Low, 30, 80, CrimeCategory.MinorInjury));

            // ENVIRONMENTAL/SPECIAL ATTACKS
            crimeDefinitions.Add(new CrimeDefinition("Arson", "LawAndOrder_Crime_Arson".Translate(), "LawAndOrder_Crime_Arson_Desc".Translate(), CrimeSeverity.High, 500, 1200, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("ToxicGasDeployment", "LawAndOrder_Crime_ToxicGasDeployment".Translate(), "LawAndOrder_Crime_ToxicGasDeployment_Desc".Translate(), CrimeSeverity.High, 400, 900, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("PsychicAttack", "LawAndOrder_Crime_PsychicAttack".Translate(), "LawAndOrder_Crime_PsychicAttack_Desc".Translate(), CrimeSeverity.High, 300, 700, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("PsychicShock", "LawAndOrder_Crime_PsychicShock".Translate(), "LawAndOrder_Crime_PsychicShock_Desc".Translate(), CrimeSeverity.Moderate, 200, 450, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("EMPAttack", "LawAndOrder_Crime_EMPAttack".Translate(), "LawAndOrder_Crime_EMPAttack_Desc".Translate(), CrimeSeverity.Moderate, 150, 350, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("AcidBurns", "LawAndOrder_Crime_AcidBurns".Translate(), "LawAndOrder_Crime_AcidBurns_Desc".Translate(), CrimeSeverity.Moderate, 250, 550, CrimeCategory.Environmental));
            crimeDefinitions.Add(new CrimeDefinition("ElectricalBurns", "LawAndOrder_Crime_ElectricalBurns".Translate(), "LawAndOrder_Crime_ElectricalBurns_Desc".Translate(), CrimeSeverity.Moderate, 200, 450, CrimeCategory.Environmental));

            // DISEASE & INFECTION CRIMES
            crimeDefinitions.Add(new CrimeDefinition("IntentionalPlagueInfection", "LawAndOrder_Crime_IntentionalPlagueInfection".Translate(), "LawAndOrder_Crime_IntentionalPlagueInfection_Desc".Translate(), CrimeSeverity.High, 800, 1800, CrimeCategory.Disease));
            crimeDefinitions.Add(new CrimeDefinition("WoundInfection", "LawAndOrder_Crime_WoundInfection".Translate(), "LawAndOrder_Crime_WoundInfection_Desc".Translate(), CrimeSeverity.Moderate, 200, 450, CrimeCategory.Disease));
            crimeDefinitions.Add(new CrimeDefinition("ToxicBuildup", "LawAndOrder_Crime_ToxicBuildup".Translate(), "LawAndOrder_Crime_ToxicBuildup_Desc".Translate(), CrimeSeverity.Moderate, 250, 550, CrimeCategory.Disease));

            // PROPERTY CRIMES - DESTRUCTION
            crimeDefinitions.Add(new CrimeDefinition("Sapping", "LawAndOrder_Crime_Sapping".Translate(), "LawAndOrder_Crime_Sapping_Desc".Translate(), CrimeSeverity.High, 300, 800, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("Breaching", "LawAndOrder_Crime_Breaching".Translate(), "LawAndOrder_Crime_Breaching_Desc".Translate(), CrimeSeverity.High, 350, 900, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("DoorDestruction", "LawAndOrder_Crime_DoorDestruction".Translate(), "LawAndOrder_Crime_DoorDestruction_Desc".Translate(), CrimeSeverity.Moderate, 100, 300, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("PowerGeneratorDestruction", "LawAndOrder_Crime_PowerGeneratorDestruction".Translate(), "LawAndOrder_Crime_PowerGeneratorDestruction_Desc".Translate(), CrimeSeverity.High, 500, 1200, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("DefensiveStructureDestruction", "LawAndOrder_Crime_DefensiveStructureDestruction".Translate(), "LawAndOrder_Crime_DefensiveStructureDestruction_Desc".Translate(), CrimeSeverity.Moderate, 250, 600, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("BuildingDestruction", "LawAndOrder_Crime_BuildingDestruction".Translate(), "LawAndOrder_Crime_BuildingDestruction_Desc".Translate(), CrimeSeverity.Moderate, 200, 500, CrimeCategory.PropertyDestruction));
            crimeDefinitions.Add(new CrimeDefinition("CropDestruction", "LawAndOrder_Crime_CropDestruction".Translate(), "LawAndOrder_Crime_CropDestruction_Desc".Translate(), CrimeSeverity.Moderate, 150, 400, CrimeCategory.PropertyDestruction));

            // PROPERTY CRIMES - THEFT
            crimeDefinitions.Add(new CrimeDefinition("GrandTheft", "LawAndOrder_Crime_GrandTheft".Translate(), "LawAndOrder_Crime_GrandTheft_Desc".Translate(), CrimeSeverity.High, 700, 3000, CrimeCategory.Theft));
            crimeDefinitions.Add(new CrimeDefinition("Theft", "LawAndOrder_Crime_Theft".Translate(), "LawAndOrder_Crime_Theft_Desc".Translate(), CrimeSeverity.Moderate, 250, 700, CrimeCategory.Theft));
            crimeDefinitions.Add(new CrimeDefinition("PettyTheft", "LawAndOrder_Crime_PettyTheft".Translate(), "LawAndOrder_Crime_PettyTheft_Desc".Translate(), CrimeSeverity.Low, 80, 250, CrimeCategory.Theft));
            crimeDefinitions.Add(new CrimeDefinition("WeaponTheft", "LawAndOrder_Crime_WeaponTheft".Translate(), "LawAndOrder_Crime_WeaponTheft_Desc".Translate(), CrimeSeverity.High, 350, 1500, CrimeCategory.Theft));
            crimeDefinitions.Add(new CrimeDefinition("MedicineTheft", "LawAndOrder_Crime_MedicineTheft".Translate(), "LawAndOrder_Crime_MedicineTheft_Desc".Translate(), CrimeSeverity.High, 300, 1200, CrimeCategory.Theft));
            crimeDefinitions.Add(new CrimeDefinition("RelicTheft", "LawAndOrder_Crime_RelicTheft".Translate(), "LawAndOrder_Crime_RelicTheft_Desc".Translate(), CrimeSeverity.Critical, 2000, 6000, CrimeCategory.Theft));

            // SOCIAL/PSYCHOLOGICAL CRIMES
            crimeDefinitions.Add(new CrimeDefinition("Insult", "LawAndOrder_Crime_Insult".Translate(), "LawAndOrder_Crime_Insult_Desc".Translate(), CrimeSeverity.Low, 20, 60, CrimeCategory.Social));
            crimeDefinitions.Add(new CrimeDefinition("WitnessedExecution", "LawAndOrder_Crime_WitnessedExecution".Translate(), "LawAndOrder_Crime_WitnessedExecution_Desc".Translate(), CrimeSeverity.Moderate, 200, 450, CrimeCategory.Social));
            crimeDefinitions.Add(new CrimeDefinition("Terrorizing", "LawAndOrder_Crime_Terrorizing".Translate(), "LawAndOrder_Crime_Terrorizing_Desc".Translate(), CrimeSeverity.Moderate, 150, 350, CrimeCategory.Social));
            crimeDefinitions.Add(new CrimeDefinition("Trespassing", "LawAndOrder_Crime_Trespassing".Translate(), "LawAndOrder_Crime_Trespassing_Desc".Translate(), CrimeSeverity.Low, 30, 100, CrimeCategory.Social, usesRangeCalculation: false)); // Always uses fixed 100§

            // ANOMALY-SPECIFIC CRIMES (DLC)
            if (ModsConfig.AnomalyActive)
            {
                crimeDefinitions.Add(new CrimeDefinition("ShamblerInfection", "LawAndOrder_Crime_ShamblerInfection".Translate(), "LawAndOrder_Crime_ShamblerInfection_Desc".Translate(), CrimeSeverity.High, 1000, 2200, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("VoidCorruption", "LawAndOrder_Crime_VoidCorruption".Translate(), "LawAndOrder_Crime_VoidCorruption_Desc".Translate(), CrimeSeverity.High, 900, 1800, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("RevenantHypnosis", "LawAndOrder_Crime_RevenantHypnosis".Translate(), "LawAndOrder_Crime_RevenantHypnosis_Desc".Translate(), CrimeSeverity.High, 700, 1400, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("Inhumanization", "LawAndOrder_Crime_Inhumanization".Translate(), "LawAndOrder_Crime_Inhumanization_Desc".Translate(), CrimeSeverity.Critical, 1500, 3000, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("MetalhorrorImplant", "LawAndOrder_Crime_MetalhorrorImplant".Translate(), "LawAndOrder_Crime_MetalhorrorImplant_Desc".Translate(), CrimeSeverity.High, 1000, 2200, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("BloodRageInduction", "LawAndOrder_Crime_BloodRageInduction".Translate(), "LawAndOrder_Crime_BloodRageInduction_Desc".Translate(), CrimeSeverity.Moderate, 400, 900, CrimeCategory.Anomaly));
                crimeDefinitions.Add(new CrimeDefinition("FleshDevouring", "LawAndOrder_Crime_FleshDevouring".Translate(), "LawAndOrder_Crime_FleshDevouring_Desc".Translate(), CrimeSeverity.Critical, 2000, 4000, CrimeCategory.Anomaly));
            }

            // BIOTECH-SPECIFIC CRIMES (DLC)
            if (ModsConfig.BiotechActive)
            {
                crimeDefinitions.Add(new CrimeDefinition("BloodfeederAttack", "LawAndOrder_Crime_BloodfeederAttack".Translate(), "LawAndOrder_Crime_BloodfeederAttack_Desc".Translate(), CrimeSeverity.Moderate, 300, 600, CrimeCategory.Biotech));
                crimeDefinitions.Add(new CrimeDefinition("MechSwarmAttack", "LawAndOrder_Crime_MechSwarmAttack".Translate(), "LawAndOrder_Crime_MechSwarmAttack_Desc".Translate(), CrimeSeverity.High, 700, 1500, CrimeCategory.Biotech));
                crimeDefinitions.Add(new CrimeDefinition("WastpackMortar", "LawAndOrder_Crime_WastpackMortar".Translate(), "LawAndOrder_Crime_WastpackMortar_Desc".Translate(), CrimeSeverity.High, 550, 1200, CrimeCategory.Biotech));
            }
        }

        /// <summary>
        /// Initializes default penalty multipliers.
        /// </summary>
        private void InitializeDefaultMultipliers()
        {
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.RepeatOffender, 1.3f, "LawAndOrder_Multiplier_RepeatOffender_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.VictimNobility, 2.0f, "LawAndOrder_Multiplier_VictimNobility_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.VictimAge, 1.3f, "LawAndOrder_Multiplier_VictimAge_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.Wartime, 0.75f, "LawAndOrder_Multiplier_Wartime_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.Premeditated, 1.4f, "LawAndOrder_Multiplier_Premeditated_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.VictimRelationship, 1.25f, "LawAndOrder_Multiplier_VictimRelationship_Desc".Translate(), true));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.RaiderWealth, 1.0f, "LawAndOrder_Multiplier_RaiderWealth_Desc".Translate(), false));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.ColonyWealth, 1.0f, "LawAndOrder_Multiplier_ColonyWealth_Desc".Translate(), false));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.DifficultySetting, 1.0f, "LawAndOrder_Multiplier_DifficultySetting_Desc".Translate(), false));
            penaltyMultipliers.Add(new CrimePenaltyMultiplier(MultiplierType.FactionRelations, 1.0f, "LawAndOrder_Multiplier_FactionRelations_Desc".Translate(), true));
        }

        /// <summary>
        /// Gets a static reference to the crime penalty manager.
        /// </summary>
        public static WorldComponent_CrimePenaltyManager Instance
        {
            get
            {
                return Find.World.GetComponent<WorldComponent_CrimePenaltyManager>();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref crimeDefinitions, "crimeDefinitions", LookMode.Deep);
            Scribe_Collections.Look(ref penaltyMultipliers, "penaltyMultipliers", LookMode.Deep);
            Scribe_Values.Look(ref lastCommitTick, "lastCommitTick", -1);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (crimeDefinitions == null)
                {
                    crimeDefinitions = new List<CrimeDefinition>();
                }

                if (penaltyMultipliers == null)
                {
                    penaltyMultipliers = new List<CrimePenaltyMultiplier>();
                }
            }

            // Initialize defaults after loading
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                InitializeDefaults();
            }
        }
    }
}
