using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Law_and_Order.Source.Hediffs;
using Law_and_Order.Source.Settings;
using Law_and_Order.Source.Components;

namespace Law_and_Order.Source.Investigation
{
    /// <summary>
    /// Generates false accusations based on:
    /// 1. Grudges (colonists with negative opinions framing enemies)
    /// 2. Sanguophages with hidden identities framing colonists
    /// 3. Intelligent criminals planting evidence
    /// </summary>
    public static class FalseAccusationGenerator
    {
        // Constants
        private const int GRUDGE_OPINION_THRESHOLD = -20;
        private const float SANGUOPHAGE_FRAME_UP_CHANCE_BASE = 0.25f;
        private const float INTELLIGENT_CRIMINAL_EVIDENCE_PLANT_CHANCE = 0.30f;

        /// <summary>
        /// Check if a grudge-based false accusation should occur
        /// Called periodically by WorldComponent
        /// </summary>
        public static void TryGenerateGrudgeFalseAccusation(Map map)
        {
            if (map == null)
                return;

            // Check if false accusations are enabled
            float falseAccusationRate = LawAndOrderSettings.FalseAccusationRate?.Value ?? 0.12f;
            if (falseAccusationRate <= 0f || !Rand.Chance(falseAccusationRate))
                return;

            // Find colonists with strong grudges
            var potentialAccusers = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.RaceProps.Humanlike && !p.Dead && !p.Downed)
                .ToList();

            if (!potentialAccusers.Any())
                return;

            foreach (var accuser in potentialAccusers)
            {
                // Find someone they hate
                var target = FindGrudgeTarget(accuser, map);
                if (target == null)
                    continue;

                // Small chance to file false accusation per check
                if (!Rand.Chance(0.05f)) // 5% chance when grudge exists
                    continue;

                // Generate the false accusation
                GenerateGrudgeBasedFalseAccusation(accuser, target, map);
                break; // Only one false accusation per check
            }
        }

        /// <summary>
        /// Find a pawn that the accuser hates enough to frame
        /// </summary>
        private static Pawn FindGrudgeTarget(Pawn accuser, Map map)
        {
            if (accuser?.relations == null)
                return null;

            // Get all colonists the accuser has strong negative opinions about
            var hatedPawns = map.mapPawns.FreeColonistsSpawned
                .Where(p => p != accuser &&
                           p.RaceProps.Humanlike &&
                           !p.Dead &&
                           accuser.relations.OpinionOf(p) <= GRUDGE_OPINION_THRESHOLD)
                .ToList();

            return hatedPawns.RandomElementWithFallback();
        }

        /// <summary>
        /// Generate a grudge-based false accusation
        /// </summary>
        private static void GenerateGrudgeBasedFalseAccusation(Pawn accuser, Pawn target, Map map)
        {
            if (accuser == null || target == null || map == null)
                return;

            // Choose a random crime type to accuse them of
            var crimeTypes = new List<CrimeType>
            {
                CrimeType.Assault,
                CrimeType.Theft,
                CrimeType.Vandalism,
                CrimeType.PropertyDestruction
            };

            var falseCrimeType = crimeTypes.RandomElement();

            // Create the false crime record
            var falseCrime = new Crime
            {
                crimeType = falseCrimeType,
                tickCommitted = Find.TickManager.TicksGame - Rand.Range(GenDate.TicksPerHour * 2, GenDate.TicksPerDay),
                victim = accuser, // Accuser claims to be the victim
                isFalseAccusation = true,
                actualPerpetrator = null, // No actual crime occurred
                falselyAccused = target,
                location = target.Position,
                visibilityState = CrimeVisibilityState.Suspected, // Immediately suspected
                witnesses = new List<Pawn> { accuser }, // Accuser is the "witness"
                evidenceStrength = 0.4f, // Moderate evidence (single witness testimony)
                additionalInfo = "False accusation based on grudge"
            };

            // Add to target's criminal record
            var criminalRecord = Utils.CrimeUtils.GetOrCreateCriminalRecord(target);
            criminalRecord?.AddCrime(falseCrime);

            // Create or amend criminal case
            var justiceManager = Find.World.GetComponent<WorldComponent_JusticeManager>();
            if (justiceManager != null)
            {
                var criminalCase = justiceManager.GetOrCreateCaseForPawn(target);
                criminalCase.AddCrime(falseCrime);

                Mod.Log?.Message($"[False Accusation] {accuser.LabelShort} falsely accused {target.LabelShort} of {falseCrimeType} (grudge-based)");
            }

            // Plant some fake evidence
            TryPlantFakeEvidence(accuser, target, falseCrime, map);

            // Notify player
            Messages.Message(
                "LawAndOrder_FalseAccusation_Grudge".Translate(accuser.LabelShort, target.LabelShort, falseCrimeType.ToString()),
                new LookTargets(new Pawn[] { accuser, target }),
                MessageTypeDefOf.NegativeEvent
            );
        }

        /// <summary>
        /// Sanguophage with hidden identity attempts to frame a colonist
        /// Called when sanguophage commits a crime
        /// </summary>
        public static void TrySanguophageFrameUp(Pawn sanguophage, Crime realCrime, Map map)
        {
            if (sanguophage == null || realCrime == null || map == null)
                return;

            // Check if sanguophage has hidden identity
            var hiddenIdentityComp = sanguophage.TryGetComp<Infiltration.CompHiddenIdentity>();
            if (hiddenIdentityComp == null)
                return;

            // Intelligence affects success chance
            float intelligence = sanguophage.GetStatValue(StatDefOf.PawnBeauty); // Use beauty as proxy for cunning
            float frameUpChance = SANGUOPHAGE_FRAME_UP_CHANCE_BASE * (1f + intelligence * 0.5f);

            if (!Rand.Chance(frameUpChance))
                return;

            // Find a scapegoat
            var scapegoat = FindScapegoat(sanguophage, map);
            if (scapegoat == null)
            {
                Mod.Log?.Message($"[Sanguophage Frame-Up] No suitable scapegoat found for {sanguophage.LabelShort}");
                return;
            }

            // Mark the real crime as a false accusation
            realCrime.isFalseAccusation = true;
            realCrime.actualPerpetrator = sanguophage;
            realCrime.falselyAccused = scapegoat;

            // Transfer the crime to the scapegoat's record
            var scapegoatRecord = Utils.CrimeUtils.GetOrCreateCriminalRecord(scapegoat);
            scapegoatRecord?.AddCrime(realCrime);

            // Remove from sanguophage's record (if it exists)
            var sanguophageRecord = Utils.CrimeUtils.TryGetCriminalRecord(sanguophage);
            // Note: We can't easily remove from list, so we'll mark it instead

            // Plant fake bloodfeeder evidence
            TryPlantSanguophageEvidence(sanguophage, scapegoat, realCrime, map);

            Mod.Log?.Message($"[Sanguophage Frame-Up] {sanguophage.LabelShort} framed {scapegoat.LabelShort} for {realCrime.crimeType}");

            Messages.Message(
                "LawAndOrder_FalseAccusation_Sanguophage".Translate(scapegoat.LabelShort, realCrime.crimeType.ToString()),
                new LookTargets(scapegoat),
                MessageTypeDefOf.NeutralEvent
            );
        }

        /// <summary>
        /// Find a suitable scapegoat for the sanguophage to frame
        /// </summary>
        private static Pawn FindScapegoat(Pawn sanguophage, Map map)
        {
            if (sanguophage == null || map == null)
                return null;

            // Prefer pawns that:
            // 1. Are not well-liked
            // 2. Have prior criminal records
            // 3. Are not close friends of other colonists

            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(p => p != sanguophage &&
                           p.RaceProps.Humanlike &&
                           !p.Dead &&
                           !p.Downed)
                .ToList();

            if (!candidates.Any())
                return null;

            // Score each candidate
            Pawn bestCandidate = null;
            float bestScore = float.MinValue;

            foreach (var candidate in candidates)
            {
                float score = 0f;

                // Check social standing
                var avgOpinion = map.mapPawns.FreeColonistsSpawned
                    .Where(p => p != candidate && p.relations != null)
                    .Average(p => (float)p.relations.OpinionOf(candidate));

                score -= avgOpinion * 0.1f; // Lower opinion = better scapegoat

                // Check existing crimes
                var criminalRecord = Utils.CrimeUtils.TryGetCriminalRecord(candidate);
                if (criminalRecord != null && criminalRecord.TotalCrimeCount > 0)
                {
                    score += 20f; // Prefer pawns with prior records
                }

                // Random factor
                score += Rand.Range(-10f, 10f);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            return bestCandidate;
        }

        /// <summary>
        /// Try to plant fake evidence at the crime scene
        /// </summary>
        private static void TryPlantFakeEvidence(Pawn planter, Pawn target, Crime falseCrime, Map map)
        {
            if (planter == null || target == null || falseCrime == null || map == null)
                return;

            // Use the evidence planter system
            IntVec3 location = falseCrime.location.IsValid ? falseCrime.location : target.Position;
            EvidencePlanter.PlantFalseEvidence(planter, falseCrime, target, location, map);
        }

        /// <summary>
        /// Plant sanguophage-specific false evidence (fake bloodfeeder signs)
        /// </summary>
        private static void TryPlantSanguophageEvidence(Pawn sanguophage, Pawn scapegoat, Crime crime, Map map)
        {
            if (sanguophage == null || scapegoat == null || crime == null || map == null)
                return;

            // Plant bloodstains that point to scapegoat
            IntVec3 clueLocation = crime.location.IsValid ? crime.location : scapegoat.Position;

            ThingDef clueDef = ClueGenerator.GetClueThingDef(ClueType.BloodStain);
            if (clueDef == null)
            {
                Mod.Log?.Warning("[Sanguophage Evidence] No ThingDef found for BloodStain clue");
                return;
            }

            var clue = (CrimeSceneClue)ThingMaker.MakeThing(clueDef);
            clue.clueType = ClueType.BloodStain;
            clue.linkedCriminal = scapegoat; // Points to scapegoat, not real criminal
            clue.linkedCrime = crime;
            clue.crimeType = crime.crimeType;
            clue.isPlanted = true;
            clue.plantedBy = sanguophage;
            clue.clueQuality = 0.7f; // High quality fake evidence

            // Create revelation that points to scapegoat
            clue.revelations = new List<ClueRevelation>
            {
                new ClueRevelation
                {
                    threshold = 0.5f,
                    label = "Blood Type Analysis",
                    description = $"The blood matches {scapegoat.LabelShort}...",
                    accuracyChance = 0.85f, // Appears accurate
                    pointsTo = scapegoat,
                    revealed = false
                }
            };

            // Try to spawn on map
            if (CellFinder.TryFindRandomCellNear(clueLocation, map, 3, c => c.Standable(map) && !c.Fogged(map), out IntVec3 spawnPos))
            {
                GenSpawn.Spawn(clue, spawnPos, map);
                Mod.Log?.Message($"[Sanguophage Evidence] Planted fake blood evidence at {spawnPos}");
            }
        }

        /// <summary>
        /// Check if intelligent criminal should plant false evidence when committing crime
        /// Called when a crime is recorded
        /// </summary>
        public static void TryIntelligentCriminalFrameUp(Pawn criminal, Crime crime, Map map)
        {
            if (criminal == null || crime == null || map == null)
                return;

            // Only intelligent criminals attempt this
            float intelligence = criminal.GetStatValue(StatDefOf.PsychicSensitivity);
            if (intelligence < 0.6f) // Require at least 60% intelligence
                return;

            if (!Rand.Chance(INTELLIGENT_CRIMINAL_EVIDENCE_PLANT_CHANCE * intelligence))
                return;

            // Find someone to frame
            var scapegoat = FindScapegoat(criminal, map);
            if (scapegoat == null)
                return;

            // Mark as false accusation
            crime.isFalseAccusation = true;
            crime.actualPerpetrator = criminal;
            crime.falselyAccused = scapegoat;

            // Plant evidence pointing to scapegoat
            IntVec3 location = crime.location.IsValid ? crime.location : scapegoat.Position;
            EvidencePlanter.PlantFalseEvidence(criminal, crime, scapegoat, location, map);

            Mod.Log?.Message($"[Intelligent Criminal] {criminal.LabelShort} planted evidence to frame {scapegoat.LabelShort}");
        }
    }
}
