using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Settings;
using LawAndOrder;
using System.Linq;

namespace Law_and_Order.Source.Utils
{
    /// <summary>
    /// Utility class for managing prisoner debt
    /// </summary>
    public static class DebtUtils
    {
        // Crime debt multipliers
        private const float KIDNAPPING_DEBT_MULTIPLIER = 1.5f; // Kidnapping is 150% of downing debt

        public static HediffDef DebtDef => LawAndOrder_HediffDefOf.LawAndOrder_Debt;

        /// <summary>
        /// Get or create the debt hediff for a pawn
        /// </summary>
        public static Hediff_Debt GetOrCreateDebtRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            // Try to find existing debt record
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DebtDef) as Hediff_Debt;

            if (existingHediff != null)
            {
                return existingHediff;
            }

            // Create new debt record
            var newHediff = (Hediff_Debt)HediffMaker.MakeHediff(DebtDef, pawn);
            pawn.health.AddHediff(newHediff);
            return newHediff;
        }

        /// <summary>
        /// Try to get existing debt record (returns null if pawn has no debt)
        /// </summary>
        public static Hediff_Debt TryGetDebtRecord(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetFirstHediffOfDef(DebtDef) as Hediff_Debt;
        }

        /// <summary>
        /// Add debt for a crime using the pre-calculated debt amount stored in the Crime object.
        /// The debt is calculated by CrimeUtils.RecordCrime() using the new penalty management system.
        /// </summary>
        public static void AddDebtForCrime(Pawn criminal, Crime crime)
        {
            if (criminal == null || crime == null)
            {
                return;
            }

            // Use the pre-calculated debt amount from the Crime object
            // This was calculated by CrimeUtils.RecordCrime() using CalculateDebtForCrimeNew()
            float totalDebt = crime.debtAmount;

            if (totalDebt <= 0)
            {
                return;
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord != null)
            {
                string reason = $"{crime.crimeType}";
                if (crime.victim != null)
                {
                    reason += $" vs {crime.victim.LabelShort}";
                }

                debtRecord.AddDebt(totalDebt, reason);
            }
        }

        /// <summary>
        /// Check if a pawn has any debt
        /// </summary>
        public static bool HasDebt(Pawn pawn)
        {
            var record = TryGetDebtRecord(pawn);
            return record != null && record.CurrentDebt > 0;
        }

        /// <summary>
        /// Get current debt amount for a pawn
        /// </summary>
        public static float GetCurrentDebt(Pawn pawn)
        {
            var record = TryGetDebtRecord(pawn);
            return record?.CurrentDebt ?? 0f;
        }

        /// <summary>
        /// Pay off some of a pawn's debt
        /// </summary>
        public static float PayDebt(Pawn pawn, float amount, string reason = null)
        {
            var record = TryGetDebtRecord(pawn);
            if (record == null)
            {
                return 0f;
            }

            return record.PayDebt(amount, reason ?? "Labor");
        }

        /// <summary>
        /// Calculate total debt for all crimes in a criminal record.
        /// Uses the pre-calculated debt amounts stored in each Crime object.
        /// </summary>
        public static float CalculateTotalDebtForCrimes(Hediff_Crimes criminalRecord)
        {
            if (criminalRecord == null || criminalRecord.Crimes == null)
            {
                return 0f;
            }

            float totalDebt = 0f;

            foreach (var crime in criminalRecord.Crimes)
            {
                // Use the pre-calculated debt amount from the Crime object
                totalDebt += crime.debtAmount;
            }

            return totalDebt;
        }

        /// <summary>
        /// Apply debt for all crimes in a criminal record
        /// This is useful when a raider is first captured
        /// </summary>
        public static void ApplyDebtForAllCrimes(Pawn criminal)
        {
            if (criminal == null)
            {
                return;
            }

            var criminalRecord = CrimeUtils.TryGetCriminalRecord(criminal);
            if (criminalRecord == null || criminalRecord.TotalCrimeCount == 0)
            {
                return;
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord == null)
            {
                return;
            }

            // Process each crime using pre-calculated debt amounts
            foreach (var crime in criminalRecord.Crimes)
            {
                float crimeDebt = crime.debtAmount;
                if (crimeDebt > 0)
                {
                    string reason = crime.crimeType.ToString();
                    if (crime.victim != null)
                    {
                        reason += $" vs {crime.victim.LabelShort}";
                    }
                    else if (crime.targetThing != null)
                    {
                        reason += $" ({crime.targetThing.Label})";
                    }

                    debtRecord.AddDebt(crimeDebt, reason);
                }
            }

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Applied {debtRecord.CurrentDebt:F0} silver debt to {criminal.NameShortColored} for {criminalRecord.TotalCrimeCount} crimes");
            }
        }

        /// <summary>
        /// Add debt for contraband items found on a pawn.
        /// </summary>
        public static void AddDebtForContraband(Pawn criminal, float totalPenalty, System.Collections.Generic.Dictionary<ThingDef, int> contrabandItems)
        {
            if (criminal == null || totalPenalty <= 0)
            {
                return;
            }

            var debtRecord = GetOrCreateDebtRecord(criminal);
            if (debtRecord == null)
            {
                return;
            }

            // Build a description of the contraband
            string itemList = string.Join(", ", System.Linq.Enumerable.Select(contrabandItems, kvp => $"{kvp.Value}x {kvp.Key.LabelCap}"));
            string reason = $"Contraband: {itemList}";

            debtRecord.AddDebt(totalPenalty, reason);

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"Applied {totalPenalty:F0} silver contraband debt to {criminal.LabelShort}");
            }
        }

        /// <summary>
        /// Get the number of days overdue for release (for UI display)
        /// </summary>
        public static int GetDebtDaysOverdue(this Pawn pawn)
        {
            var debtRecord = TryGetDebtRecord(pawn);
            if (debtRecord == null)
                return 0;

            return debtRecord.TicksSinceDebtPaid / GenDate.TicksPerDay;
        }

        #region Phase 3: Crime Penalty Management System Integration

        /// <summary>
        /// Calculate penalty within the crime's min-max range based on damage context.
        /// Returns a contextual penalty (not random) based on severity factors.
        /// </summary>
        private static float CalculatePenaltyInRange(CrimeDefinition crimeDef, DamageInfo? damageInfo, Pawn victim)
        {
            if (crimeDef == null)
            {
                return 0f;
            }

            // Default to middle of range (0.5 = 50% severity)
            float severity = 0.5f;

            // Analyze damage severity if we have damage info
            if (damageInfo != null && damageInfo.HasValue && victim != null)
            {
                DamageInfo dinfo = damageInfo.Value;

                // Factor 1: Body part importance (0.0 to 1.0)
                float bodyPartImportance = GetBodyPartImportance(dinfo.HitPart);

                // Factor 2: Damage amount relative to victim health (0.0 to 1.0)
                float damageRatio = 0.5f;
                if (victim.health?.summaryHealth?.SummaryHealthPercent > 0)
                {
                    // Calculate how much of victim's health was damaged
                    float healthPercent = victim.health.summaryHealth.SummaryHealthPercent;
                    damageRatio = 1f - healthPercent; // Lower health = higher severity
                    damageRatio = UnityEngine.Mathf.Clamp01(damageRatio);
                }

                // Factor 3: Permanent effects (0.0 or 1.0)
                float permanentEffect = IsPermanentInjury(dinfo, victim) ? 1.0f : 0.0f;

                // Combine factors (weighted average)
                // Body part: 40%, Damage ratio: 40%, Permanent: 20%
                severity = (bodyPartImportance * 0.4f) + (damageRatio * 0.4f) + (permanentEffect * 0.2f);
                severity = UnityEngine.Mathf.Clamp01(severity);
            }

            // Interpolate between min and max penalty
            int basePenalty = UnityEngine.Mathf.RoundToInt(
                UnityEngine.Mathf.Lerp(crimeDef.minPenalty, crimeDef.maxPenalty, severity)
            );

            return basePenalty;
        }

        /// <summary>
        /// Score body part importance from 0.0 (least important) to 1.0 (most important).
        /// Used to adjust penalties based on which body part was damaged.
        /// </summary>
        private static float GetBodyPartImportance(BodyPartRecord part)
        {
            if (part == null || part.def == null)
            {
                return 0.5f; // Default to medium importance
            }

            string defName = part.def.defName;

            // Critical organs - instant death or severe disability
            if (defName == "Brain" || part.def == BodyPartDefOf.Heart)
            {
                return 1.0f;
            }

            // Important organs - major impact on survival
            if (defName == "Liver" ||
                defName == "Kidney" ||
                part.def == BodyPartDefOf.Lung ||
                defName == "Stomach")
            {
                return 0.8f;
            }

            // Eyes and spine - major impact on quality of life
            if (part.def == BodyPartDefOf.Eye || defName == "Spine")
            {
                return 0.75f;
            }

            // Limbs - significant disability
            if (part.def == BodyPartDefOf.Arm || part.def == BodyPartDefOf.Leg)
            {
                return 0.6f;
            }

            // Hands and feet - moderate impact
            if (part.def == BodyPartDefOf.Hand || defName == "Foot")
            {
                return 0.5f;
            }

            // Neck, jaw - moderate importance
            if (part.def == BodyPartDefOf.Neck || defName == "Jaw")
            {
                return 0.5f;
            }

            // Fingers, toes, ears, nose - minor impact
            return 0.3f;
        }

        /// <summary>
        /// Check if damage caused permanent injury (limb loss, organ destruction, etc.)
        /// </summary>
        private static bool IsPermanentInjury(DamageInfo dinfo, Pawn victim)
        {
            if (victim?.health?.hediffSet == null)
            {
                return false;
            }

            // Check if the damaged part is now missing
            if (dinfo.HitPart != null && victim.health.hediffSet.PartIsMissing(dinfo.HitPart))
            {
                return true;
            }

            // Check for permanent injuries added recently (within last 60 ticks)
            int recentTicks = 60;
            foreach (Hediff hediff in victim.health.hediffSet.hediffs)
            {
                // Check if it's a recent injury
                if (hediff.ageTicks > recentTicks)
                {
                    continue;
                }

                // Check if it's permanent
                if (hediff.IsPermanent())
                {
                    return true;
                }

                // Check for missing body parts
                if (hediff is Hediff_MissingPart)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a limb (arm, leg, hand, foot) was destroyed
        /// </summary>
        private static bool IsLimbDestroyed(DamageInfo dinfo, Pawn victim)
        {
            if (dinfo.HitPart == null || dinfo.HitPart.def == null || victim?.health?.hediffSet == null)
            {
                return false;
            }

            // Check if it's a limb
            string defName = dinfo.HitPart.def.defName;
            if (dinfo.HitPart.def != BodyPartDefOf.Arm &&
                dinfo.HitPart.def != BodyPartDefOf.Leg &&
                dinfo.HitPart.def != BodyPartDefOf.Hand &&
                defName != "Foot")
            {
                return false;
            }

            // Check if the limb is now missing
            return victim.health.hediffSet.PartIsMissing(dinfo.HitPart);
        }

        /// <summary>
        /// Check if an eye was destroyed
        /// </summary>
        private static bool IsEyeDestroyed(DamageInfo dinfo, Pawn victim)
        {
            if (dinfo.HitPart == null || victim?.health?.hediffSet == null)
            {
                return false;
            }

            if (dinfo.HitPart.def != BodyPartDefOf.Eye)
            {
                return false;
            }

            return victim.health.hediffSet.PartIsMissing(dinfo.HitPart);
        }

        /// <summary>
        /// Check if a vital organ (brain, heart) was destroyed
        /// </summary>
        private static bool IsVitalOrganDestroyed(DamageInfo dinfo, Pawn victim)
        {
            if (dinfo.HitPart == null || dinfo.HitPart.def == null || victim?.health?.hediffSet == null)
            {
                return false;
            }

            string defName = dinfo.HitPart.def.defName;
            if (defName != "Brain" && dinfo.HitPart.def != BodyPartDefOf.Heart)
            {
                return false;
            }

            return victim.health.hediffSet.PartIsMissing(dinfo.HitPart);
        }

        /// <summary>
        /// Apply all enabled penalty multipliers based on crime context.
        /// Multipliers are applied multiplicatively (not additively).
        /// </summary>
        private static float ApplyMultipliers(
            float basePenalty,
            WorldComponent_CrimePenaltyManager manager,
            Pawn criminal,
            Pawn victim,
            DamageInfo? damageInfo)
        {
            if (manager == null || manager.PenaltyMultipliers == null)
            {
                return basePenalty;
            }

            float penalty = basePenalty;

            // Apply each enabled multiplier
            foreach (var multiplier in manager.PenaltyMultipliers)
            {
                if (!multiplier.enabled)
                {
                    continue;
                }

                bool shouldApply = false;
                float customFactor = 1.0f;

                switch (multiplier.multiplierType)
                {
                    case MultiplierType.RepeatOffender:
                        shouldApply = HasPriorOffenses(criminal);
                        break;

                    case MultiplierType.VictimNobility:
                        shouldApply = IsNoble(victim);
                        break;

                    case MultiplierType.VictimAge:
                        shouldApply = IsChild(victim);
                        break;

                    case MultiplierType.Wartime:
                        shouldApply = IsWartime(criminal?.Faction);
                        break;

                    case MultiplierType.Premeditated:
                        shouldApply = WasPremeditated(damageInfo);
                        break;

                    case MultiplierType.VictimRelationship:
                        shouldApply = IsFamily(criminal, victim);
                        break;

                    case MultiplierType.RaiderWealth:
                        // Custom factor based on faction wealth
                        customFactor = GetFactionWealthFactor(criminal?.Faction);
                        penalty *= customFactor;
                        continue; // Skip standard multiplier application

                    case MultiplierType.ColonyWealth:
                        // Custom factor based on colony wealth
                        customFactor = GetColonyWealthFactor();
                        penalty *= customFactor;
                        continue;

                    case MultiplierType.DifficultySetting:
                        // Custom factor based on difficulty
                        customFactor = GetDifficultyFactor();
                        penalty *= customFactor;
                        continue;

                    case MultiplierType.FactionRelations:
                        // Custom factor based on faction relations
                        customFactor = GetFactionRelationFactor(criminal?.Faction);
                        penalty *= customFactor;
                        continue;
                }

                // Apply standard multiplier if conditions are met
                if (shouldApply)
                {
                    penalty *= multiplier.multiplierValue;
                }
            }

            // Cap total multiplier at 4x base penalty to prevent extreme outliers
            float totalMultiplier = penalty / basePenalty;
            if (totalMultiplier > 4.0f)
            {
                penalty = basePenalty * 4.0f;

                if (Prefs.DevMode)
                {
                    Mod.Log?.Message($"[Law & Order] Penalty capped: {totalMultiplier:F2}x reduced to 4.0x (from {penalty:F0} to {basePenalty * 4.0f:F0} silver)");
                }
            }

            return penalty;
        }

        /// <summary>
        /// Check if criminal has prior offenses (has been captured before)
        /// </summary>
        private static bool HasPriorOffenses(Pawn criminal)
        {
            if (criminal == null)
            {
                return false;
            }

            // Check if criminal has a crime record
            var crimeRecord = CrimeUtils.TryGetCriminalRecord(criminal);
            if (crimeRecord == null)
            {
                return false;
            }

            // Check if they have archived crimes (meaning they were processed before)
            if (crimeRecord.ArchivedCrimeCount > 0)
            {
                return true;
            }

            // TODO: Could add more sophisticated prior offense detection here
            // For now, archived crimes are sufficient

            return false;
        }

        /// <summary>
        /// Check if victim has a noble title (Royalty DLC)
        /// </summary>
        private static bool IsNoble(Pawn victim)
        {
            if (victim?.royalty == null)
            {
                return false;
            }

            // Check if they have any royal title in the Empire faction
            return victim.royalty.HasAnyTitleIn(Faction.OfEmpire);
        }

        /// <summary>
        /// Check if victim is a child
        /// </summary>
        private static bool IsChild(Pawn victim)
        {
            if (victim == null)
            {
                return false;
            }

            return victim.DevelopmentalStage == DevelopmentalStage.Child ||
                   victim.DevelopmentalStage == DevelopmentalStage.Baby;
        }

        /// <summary>
        /// Check if the colony is currently at war with the criminal's faction
        /// </summary>
        private static bool IsWartime(Faction faction)
        {
            if (faction == null)
            {
                return false;
            }

            // Check if faction is hostile to player
            return faction.HostileTo(Faction.OfPlayer);
        }

        /// <summary>
        /// Check if the attack was premeditated (execution, planned attack)
        /// </summary>
        private static bool WasPremeditated(DamageInfo? damageInfo)
        {
            if (damageInfo == null || !damageInfo.HasValue)
            {
                return false;
            }

            DamageInfo dinfo = damageInfo.Value;

            // Execution damage type indicates premeditation
            if (dinfo.Def == DamageDefOf.ExecutionCut)
            {
                return true;
            }

            // Could add more logic here based on weapon types, attack patterns, etc.
            return false;
        }

        /// <summary>
        /// Check if victim is family (spouse, parent, child, sibling) of a colonist
        /// </summary>
        private static bool IsFamily(Pawn criminal, Pawn victim)
        {
            if (criminal == null || victim == null)
            {
                return false;
            }

            // Check if victim has direct family relations
            if (victim.relations == null)
            {
                return false;
            }

            // Check for common family relations
            return victim.relations.DirectRelationExists(PawnRelationDefOf.Spouse, victim) ||
                   victim.relations.DirectRelationExists(PawnRelationDefOf.Parent, victim) ||
                   victim.relations.DirectRelationExists(PawnRelationDefOf.Child, victim) ||
                   victim.relations.DirectRelationExists(PawnRelationDefOf.Sibling, victim);
        }

        /// <summary>
        /// Get faction wealth multiplier factor (0.5x to 2.0x)
        /// Placeholder - needs actual faction wealth calculation
        /// </summary>
        private static float GetFactionWealthFactor(Faction faction)
        {
            if (faction == null)
            {
                return 1.0f;
            }

            // TODO: Implement actual faction wealth calculation
            // For now, return 1.0 (no adjustment)
            // Could potentially use faction.def.techLevel or other metrics
            return 1.0f;
        }

        /// <summary>
        /// Get colony wealth multiplier factor
        /// Scales penalties based on how wealthy the colony is
        /// </summary>
        private static float GetColonyWealthFactor()
        {
            // Get player home map
            Map map = Find.Maps?.FirstOrDefault(m => m.IsPlayerHome);
            if (map?.wealthWatcher == null)
            {
                return 1.0f; // Default if no map found
            }

            float wealth = map.wealthWatcher.WealthTotal;

            // Scale penalties based on wealth brackets:
            // < 10k wealth = 0.5x (poor colony)
            // 10k-50k = 1.0x (normal)
            // 50k-100k = 1.5x (wealthy)
            // > 100k = 2.0x (very wealthy)

            if (wealth < 10000f)
            {
                return 0.5f;
            }
            else if (wealth < 50000f)
            {
                return 1.0f;
            }
            else if (wealth < 100000f)
            {
                return 1.5f;
            }
            else
            {
                return 2.0f;
            }
        }

        /// <summary>
        /// Get difficulty-based multiplier factor
        /// Placeholder - could scale based on storyteller difficulty
        /// </summary>
        private static float GetDifficultyFactor()
        {
            // TODO: Could implement actual difficulty scaling
            // For now, return 1.0 (no adjustment)
            // Could use Find.Storyteller.difficulty or similar
            return 1.0f;
        }

        /// <summary>
        /// Get faction relations multiplier factor
        /// Hostile factions get higher penalties, allies get lower
        /// </summary>
        private static float GetFactionRelationFactor(Faction faction)
        {
            if (faction == null)
            {
                return 1.0f;
            }

            int goodwill = faction.GoodwillWith(Faction.OfPlayer);

            // Hostile factions (-100 to -50) = 2.0x penalty
            // Neutral factions (-50 to 50) = 1.0x penalty
            // Allied factions (50 to 100) = 0.5x penalty

            if (goodwill < -50)
            {
                return 2.0f;
            }
            else if (goodwill > 50)
            {
                return 0.5f;
            }
            else
            {
                return 1.0f;
            }
        }

        /// <summary>
        /// Determine which crime type (defName) to use based on damage info and context.
        /// Maps RimWorld damage to specific crime definitions in the penalty manager.
        /// </summary>
        public static string DetermineCrimeType(DamageInfo? damageInfo, Pawn victim)
        {
            if (damageInfo == null || !damageInfo.HasValue || victim == null)
            {
                return "SuperficialWound"; // Default fallback
            }

            DamageInfo dinfo = damageInfo.Value;

            // Death crimes (highest priority)
            if (victim.Dead)
            {
                // Execution
                if (dinfo.Def == DamageDefOf.ExecutionCut)
                {
                    return "Execution";
                }

                // Vital organ destruction (brain/heart)
                if (IsVitalOrganDestroyed(dinfo, victim))
                {
                    return "VitalOrganDestruction";
                }

                // Default to murder for any death
                return "Murder";
            }

            // Severe injury crimes (limbs, organs, major damage)
            if (IsLimbDestroyed(dinfo, victim))
            {
                return "LimbDestruction";
            }

            if (IsEyeDestroyed(dinfo, victim))
            {
                return "EyeDestruction";
            }

            // Check for major organ damage (not destroyed, but damaged)
            if (dinfo.HitPart != null && dinfo.HitPart.def != null)
            {
                string partName = dinfo.HitPart.def.defName;
                if (partName == "Liver" ||
                    partName == "Kidney" ||
                    dinfo.HitPart.def == BodyPartDefOf.Lung ||
                    partName == "Stomach")
                {
                    return "MajorOrganDamage";
                }

                // Spine injury
                if (partName == "Spine")
                {
                    return "SpineInjury";
                }
            }

            // Check for severe burns
            if (dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn)
            {
                if (dinfo.Amount > 20) // High damage burn
                {
                    return "SevereBurns";
                }
                else if (dinfo.Amount > 10)
                {
                    return "ModerateBurns";
                }
                else
                {
                    return "MinorBurns";
                }
            }

            // Moderate injury crimes (specific weapon/damage types)
            if (dinfo.Def == DamageDefOf.Bullet)
            {
                return "GunshotWound";
            }

            if (dinfo.Def == DamageDefOf.Stab)
            {
                return "StabWound";
            }

            if (dinfo.Def == DamageDefOf.Cut || dinfo.Def == DamageDefOf.Scratch)
            {
                if (dinfo.Amount > 10)
                {
                    return "SlashWound";
                }
                else
                {
                    return "Scratch";
                }
            }

            if (dinfo.Def == DamageDefOf.Blunt || dinfo.Def == DamageDefOf.Crush)
            {
                if (dinfo.Amount > 10)
                {
                    return "BluntTrauma";
                }
                else
                {
                    return "Bruise";
                }
            }

            if (dinfo.Def == DamageDefOf.Bite)
            {
                return "BiteWound";
            }

            if (dinfo.Def == DamageDefOf.Bomb)
            {
                return "ExplosiveInjury";
            }

            if (dinfo.Def.defName == "Arrow") // Arrow damage type
            {
                return "ArrowWound";
            }

            // Frostbite
            if (dinfo.Def == DamageDefOf.Frostbite)
            {
                return "Frostbite";
            }

            // Environmental attacks
            if (dinfo.Def.defName == "ToxGas")
            {
                return "ToxicGasDeployment";
            }

            if (dinfo.Def.defName == "EMP")
            {
                return "EMPAttack";
            }

            // Default to superficial wound for unrecognized damage
            return "SuperficialWound";
        }

        /// <summary>
        /// Calculate debt for a crime using the new penalty management system.
        /// This is the primary method for Phase 3 integration.
        /// </summary>
        public static float CalculateDebtForCrimeNew(Pawn criminal, Pawn victim, DamageInfo? damageInfo, string crimeDefName = null)
        {
            // Get the crime penalty manager
            var manager = WorldComponent_CrimePenaltyManager.Instance;
            if (manager == null)
            {
                Mod.Log?.Error("[Law & Order] CrimePenaltyManager not found, cannot calculate penalty. This should never happen.");
                return 0f;
            }

            // Determine crime type if not provided
            if (string.IsNullOrEmpty(crimeDefName))
            {
                crimeDefName = DetermineCrimeType(damageInfo, victim);
            }

            // Get crime definition
            var crimeDef = manager.GetCrimeDefinition(crimeDefName);
            if (crimeDef == null)
            {
                Mod.Log?.Error($"[Law & Order] Crime definition '{crimeDefName}' not found in manager. Crime will not be penalized.");
                return 0f;
            }

            // Calculate base penalty within range
            float basePenalty = CalculatePenaltyInRange(crimeDef, damageInfo, victim);

            // Apply multipliers
            float finalPenalty = ApplyMultipliers(basePenalty, manager, criminal, victim, damageInfo);

            // Clamp to valid range (1 to 100,000 silver)
            int penalty = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(finalPenalty), 1, 100000);

            if (Prefs.DevMode)
            {
                Mod.Log?.Message($"[Law & Order] Calculated penalty: {penalty} silver for {crimeDefName} (base: {basePenalty:F0}, final: {finalPenalty:F0})");
            }

            return penalty;
        }

        /// <summary>
        /// Calculate debt for crimes that don't involve direct victim damage
        /// (Theft, Trespassing, Contraband, Property Destruction, etc.)
        /// </summary>
        public static float CalculateDebtForNonVictimCrime(Pawn criminal, CrimeType crimeType, Thing targetThing = null)
        {
            // Base penalties for non-victim crimes
            switch (crimeType)
            {
                case CrimeType.Theft:
                    // Theft penalty based on item value
                    if (targetThing != null)
                    {
                        float itemValue = targetThing.MarketValue * targetThing.stackCount;
                        return itemValue * 1.5f; // 150% of item value
                    }
                    return 100f; // Minimum for unspecified theft

                case CrimeType.PropertyDestruction:
                    // Property destruction based on thing value
                    if (targetThing != null)
                    {
                        return targetThing.MarketValue * 2.0f; // 200% of property value
                    }
                    return 500f; // Default for severe property damage

                case CrimeType.Vandalism:
                    // Vandalism is minor property damage
                    if (targetThing != null)
                    {
                        return targetThing.MarketValue * 1.0f; // 100% of property value
                    }
                    return 200f; // Default for minor damage

                case CrimeType.Arson:
                    // Arson - scales with property value destroyed, like property destruction but more severe
                    if (targetThing != null)
                    {
                        // Use 3x multiplier for arson (more severe than property destruction's 2x)
                        return targetThing.MarketValue * 3.0f;
                    }

                    // No target thing provided - use crime definition system for base penalty
                    var arsonManager = WorldComponent_CrimePenaltyManager.Instance;
                    if (arsonManager != null)
                    {
                        var arsonCrimeDef = arsonManager.GetCrimeDefinition("Arson");
                        if (arsonCrimeDef != null)
                        {
                            // Use average of min and max penalty
                            return (arsonCrimeDef.minPenalty + arsonCrimeDef.maxPenalty) / 2f;
                        }
                    }
                    return 1500f; // Fallback if crime definition not found

                case CrimeType.Trespassing:
                    // Trespassing - use crime definition system
                    var manager = WorldComponent_CrimePenaltyManager.Instance;
                    if (manager != null)
                    {
                        var crimeDef = manager.GetCrimeDefinition("Trespassing");
                        if (crimeDef != null)
                        {
                            // Use average of min and max penalty
                            return (crimeDef.minPenalty + crimeDef.maxPenalty) / 2f;
                        }
                    }
                    return 100f; // Fallback if crime definition not found

                case CrimeType.ContrabandPossession:
                    // Contraband penalty based on item
                    if (targetThing != null)
                    {
                        return targetThing.MarketValue * 2.0f; // 200% of contraband value
                    }
                    return 100f; // Default contraband penalty

                case CrimeType.Kidnapping:
                    // Kidnapping is a serious crime even without immediate damage
                    return 2000f;

                default:
                    Mod.Log?.Warning($"CalculateDebtForNonVictimCrime called with unsupported crime type: {crimeType}");
                    return 100f; // Fallback penalty
            }
        }

        #endregion
    }
}
