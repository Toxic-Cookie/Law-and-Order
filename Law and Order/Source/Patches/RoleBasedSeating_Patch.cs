using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Law_and_Order.Source.Rituals;

namespace Law_and_Order.Source.Patches
{
    /// <summary>
    /// Patches ritual seating to make pawns prefer seats designated for their role
    /// during court hearings
    /// </summary>
    [HarmonyPatch]
    public static class RoleBasedSeating_Patch
    {
        // We'll patch the TryFindSpot method in JobGiver_SpectateDutySpectateRect
        // This is where pawns look for spots during rituals
        [HarmonyPatch(typeof(RimWorld.JobGiver_SpectateDutySpectateRect), "TryFindSpot")]
        [HarmonyPrefix]
        public static bool Prefix_TryFindSpot(
            Pawn pawn,
            PawnDuty duty,
            ref IntVec3 spot,
            ref bool __result)
        {
            // Only handle court hearing rituals
            if (pawn?.GetLord()?.LordJob is LordJob_Ritual ritual)
            {
                // Check if this is a court hearing ritual
                if (ritual.Ritual?.def?.defName == "LawAndOrder_CourtHearing")
                {
                    // Try to find a role-appropriate seat
                    if (TryFindRoleBasedSeat(pawn, ritual, duty, out spot))
                    {
                        __result = true;
                        return false; // Skip original method
                    }
                }
            }

            // Fall back to original method
            return true;
        }

        /// <summary>
        /// Try to find a seat that matches the pawn's role in the ritual
        /// </summary>
        private static bool TryFindRoleBasedSeat(
            Pawn pawn,
            LordJob_Ritual ritual,
            PawnDuty duty,
            out IntVec3 spot)
        {
            spot = IntVec3.Invalid;

            if (duty == null || duty.spectateRect.Area == 0)
            {
                return false;
            }

            // Determine the pawn's role in the ritual
            CourtroomChairRole roleNeeded = GetPawnRitualRole(pawn, ritual);
            if (roleNeeded == CourtroomChairRole.Unassigned)
            {
                return false; // No specific role, use normal seating
            }

            Map map = pawn.Map;
            CellRect spectateRect = duty.spectateRect;

            // Find all buildings with courtroom chair component in the vicinity
            List<Building> potentialSeats = new List<Building>();

            // Search in a reasonable radius around the spectate rect
            CellRect searchRect = spectateRect.ExpandedBy(15).ClipInsideMap(map);

            foreach (IntVec3 cell in searchRect)
            {
                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.def.building.isSittable)
                {
                    // Check if this building has a courtroom chair component
                    CompCourtroomChair comp = edifice.TryGetComp<CompCourtroomChair>();
                    if (comp != null && comp.Role == roleNeeded)
                    {
                        potentialSeats.Add(edifice);
                    }
                }
            }

            if (potentialSeats.Count == 0)
            {
                return false; // No seats for this role
            }

            // Sort by distance to spectate rect center
            potentialSeats = potentialSeats
                .OrderBy(s => s.Position.DistanceToSquared(spectateRect.CenterCell))
                .ToList();

            // Try to find a valid seat
            foreach (Building seat in potentialSeats)
            {
                IntVec3 seatPos = seat.Position;

                // Validate the seat position
                if (!seatPos.InBounds(map))
                    continue;

                // Check if pawn can reach and reserve it
                if (!pawn.CanReserveSittableOrSpot(seatPos, false))
                    continue;

                if (!pawn.CanReach(seatPos, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
                    continue;

                // Check if seat is correctly oriented toward spectate rect
                if (!IsCorrectlyRotatedChair(seatPos, seat.Rotation, seat.def, spectateRect))
                    continue;

                // Check line of sight to spectate rect
                IntVec3 closestSpectateCell = spectateRect.ClosestCellTo(seatPos);
                if (!GenSight.LineOfSight(closestSpectateCell, seatPos, map, true, null, 0, 0))
                    continue;

                // Check distance constraint
                if (closestSpectateCell.DistanceToSquared(seatPos) > 210) // ~14.5 squared
                    continue;

                // This seat is valid!
                spot = seatPos;

                // Logging disabled to prevent spam - role-based seating is working correctly
                // Law_and_Order.Source.Mod.Log?.Message(
                //     $"[RoleBasedSeating] Found {roleNeeded} seat for {pawn.LabelShort} at {seatPos}");

                return true;
            }

            // No valid seat found for this role
            return false;
        }

        /// <summary>
        /// Determine what role the pawn has in the ritual
        /// </summary>
        private static CourtroomChairRole GetPawnRitualRole(Pawn pawn, LordJob_Ritual ritual)
        {
            if (ritual?.assignments == null)
            {
                return CourtroomChairRole.Unassigned;
            }

            // Get the role this pawn is assigned to
            var assignment = ritual.assignments.RoleForPawn(pawn);
            if (assignment == null)
            {
                // Pawn has no assigned role, they're a spectator
                return CourtroomChairRole.Spectator;
            }

            // Map ritual role ID to courtroom chair role
            switch (assignment.id)
            {
                case "judge":
                    return CourtroomChairRole.Judge;

                case "defendant":
                    return CourtroomChairRole.Defendant;

                case "victim":
                    return CourtroomChairRole.Victim;

                case "jury":
                    return CourtroomChairRole.Jury;

                default:
                    // Unknown role, treat as spectator
                    return CourtroomChairRole.Spectator;
            }
        }

        /// <summary>
        /// Check if a chair is correctly rotated toward the spectate rect
        /// Copied from SpectatorCellFinder.IsCorrectlyRotatedChair
        /// </summary>
        private static bool IsCorrectlyRotatedChair(IntVec3 chairPos, Rot4 chairRot, ThingDef chairDef, CellRect spectateRect)
        {
            return chairDef.building.sitIgnoreOrientation ||
                   GenGeo.AngleDifferenceBetween(
                       chairRot.AsAngle,
                       (spectateRect.ClosestCellTo(chairPos) - chairPos).AngleFlat) <= 75f;
        }
    }
}
