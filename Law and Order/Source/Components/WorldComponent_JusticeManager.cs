using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Law_and_Order.Source.Justice;
using Law_and_Order.Source.Hediffs;

namespace Law_and_Order.Source.Components
{
    /// <summary>
    /// Global manager for criminal cases and justice system.
    /// WorldComponent persists across saves and manages all active/closed cases.
    /// </summary>
    public class WorldComponent_JusticeManager : WorldComponent
    {
        // Global case registry
        private List<CriminalCase> allCases = new List<CriminalCase>();
        private int nextCaseId = 1;

        // Static accessor for convenience
        public static WorldComponent_JusticeManager Instance => Find.World.GetComponent<WorldComponent_JusticeManager>();

        public WorldComponent_JusticeManager(World world) : base(world)
        {
        }

        /// <summary>
        /// Create a new criminal case for a suspected criminal.
        /// </summary>
        public CriminalCase CreateCase(Pawn accused)
        {
            var newCase = new CriminalCase(nextCaseId++, accused);
            allCases.Add(newCase);

            Mod.Log?.Message($"Created case #{newCase.caseId} for {accused.NameShortColored}");

            return newCase;
        }

        /// <summary>
        /// Get the active (open) case for a pawn, if one exists.
        /// </summary>
        public CriminalCase GetActiveCase(Pawn pawn)
        {
            return allCases.FirstOrDefault(c => c.accused == pawn && c.status == CaseStatus.Open);
        }

        /// <summary>
        /// Get or create a case for a pawn. If they already have an open case, return it.
        /// Otherwise create a new case.
        /// </summary>
        public CriminalCase GetOrCreateCase(Pawn pawn)
        {
            var existing = GetActiveCase(pawn);
            if (existing != null)
                return existing;

            return CreateCase(pawn);
        }

        /// <summary>
        /// Get all open cases across all pawns.
        /// </summary>
        public List<CriminalCase> GetOpenCases()
        {
            return allCases.Where(c => c.status == CaseStatus.Open).ToList();
        }

        /// <summary>
        /// Get all convicted cases across all pawns.
        /// </summary>
        public List<CriminalCase> GetConvictedCases()
        {
            return allCases.Where(c => c.status == CaseStatus.Convicted).ToList();
        }

        /// <summary>
        /// Get all dismissed cases across all pawns.
        /// </summary>
        public List<CriminalCase> GetDismissedCases()
        {
            return allCases.Where(c => c.status == CaseStatus.Dismissed).ToList();
        }

        /// <summary>
        /// Get all cases for a specific pawn (any status).
        /// </summary>
        public List<CriminalCase> GetCasesForPawn(Pawn pawn)
        {
            return allCases.Where(c => c.accused == pawn).ToList();
        }

        /// <summary>
        /// Get a case by its ID.
        /// </summary>
        public CriminalCase GetCaseById(int id)
        {
            return allCases.FirstOrDefault(c => c.caseId == id);
        }

        /// <summary>
        /// Remove old closed cases to prevent save file bloat.
        /// Called periodically (e.g., once per day).
        /// </summary>
        public void ArchiveOldCases()
        {
            int currentTick = Find.TickManager.TicksGame;
            int archiveThreshold = GenDate.TicksPerDay * 60; // 60 days

            var toArchive = allCases.Where(c =>
                c.status != CaseStatus.Open &&
                (currentTick - c.tickClosed) > archiveThreshold
            ).ToList();

            foreach (var caseToArchive in toArchive)
            {
                allCases.Remove(caseToArchive);
                Mod.Log?.Message($"Archived old case #{caseToArchive.caseId} ({caseToArchive.status})");
            }

            if (toArchive.Count > 0)
            {
                Mod.Log?.Message($"Archived {toArchive.Count} old cases");
            }
        }

        /// <summary>
        /// WorldComponent tick - called periodically for maintenance tasks.
        /// </summary>
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            // Archive old cases once per day
            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
            {
                ArchiveOldCases();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref allCases, "allCases", LookMode.Deep);
            Scribe_Values.Look(ref nextCaseId, "nextCaseId", 1);

            // Ensure list is initialized after load
            if (Scribe.mode == LoadSaveMode.PostLoadInit && allCases == null)
            {
                allCases = new List<CriminalCase>();
            }
        }

        /// <summary>
        /// Get statistics about the justice system for debugging/UI.
        /// </summary>
        public (int open, int convicted, int dismissed) GetCaseStats()
        {
            return (
                allCases.Count(c => c.status == CaseStatus.Open),
                allCases.Count(c => c.status == CaseStatus.Convicted),
                allCases.Count(c => c.status == CaseStatus.Dismissed)
            );
        }
    }
}
