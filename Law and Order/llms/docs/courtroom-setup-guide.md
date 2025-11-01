# Courtroom Setup Guide - Law and Order Mod

## Overview

The Law and Order mod uses a **two-component system** for courtroom setup. Understanding both components and how they work together is essential for creating functional courtrooms.

## Component System Architecture

### Component 1: Comp_JudgesBench (Table/Bench Designation)

**Purpose:** Marks a building (usually a table) as the Judge's Bench, which serves as the ritual target for court hearings.

**Applied to:** Tables, desks, or similar furniture

**How it works:**
- Players right-click on a table and select "Designate as Judge's Bench"
- This toggles a boolean flag `isDesignatedAsJudgesBench`
- The ritual system searches for rooms containing a designated Judge's Bench
- The Judge's Bench becomes the target location for court hearing rituals

**Code Location:** `Source/Buildings/Comp_JudgesBench.cs`

**Why it's needed:** RimWorld's ritual system requires a specific building as a target. The Judge's Bench serves as this focal point for court rituals, similar to how weddings target marriage spots.

### Component 2: CompCourtroomChair (Seat Role Assignment)

**Purpose:** Designates individual chairs/seats for specific courtroom roles.

**Applied to:** Chairs, stools, thrones, or other sittable furniture

**How it works:**
- Players can assign chairs to specific roles:
  - **Judge** - The adjudicator's seat
  - **Jury** - Seats for jury members (if used)
  - **Defendant** - Seat for the accused
  - **Victim** - Seat for crime victims (if participating)
  - **Spectator** - General audience seating
  - **Unassigned** - Default state, no specific role

**Code Location:** `Source/Rituals/CourtroomChairRole.cs`

**Why it's needed:** The Custom Ritual Framework (CRF) needs to know where each participant should sit during the hearing ritual. Without role-designated chairs, participants would choose random seats.

## How the Two Systems Work Together

```
COURTROOM SETUP FLOW:

1. Player builds a room
2. Player places a table and designates it as Judge's Bench (Comp_JudgesBench)
   → This makes the room a valid courtroom
   → The bench becomes the ritual target

3. Player places chairs around the room
4. Player assigns roles to chairs (CompCourtroomChair)
   → Judge chair near the bench
   → Defendant chair facing the bench
   → Optional: Victim, jury, and spectator chairs

5. Ritual system validates the courtroom:
   ✓ Has a designated Judge's Bench (Comp_JudgesBench)
   ✓ Has required role chairs (CompCourtroomChair for judge, defendant)
   ✓ Room is indoors and accessible

6. Court hearing ritual can now be performed
```

## Why Two Separate Components?

**Design Rationale:**

1. **Different Furniture Types:**
   - Comp_JudgesBench applies to tables/desks
   - CompCourtroomChair applies to chairs/seats
   - These are fundamentally different furniture categories

2. **Different Purposes:**
   - Comp_JudgesBench: Ritual targeting (where the ritual happens)
   - CompCourtroomChair: Participant positioning (where people sit)

3. **Separation of Concerns:**
   - The bench is about the room/ritual
   - The chairs are about individual participants

4. **Flexibility:**
   - A courtroom could have multiple benches (though only one designated)
   - A courtroom can have varying numbers of chairs for each role
   - Chairs can be reassigned without affecting the bench designation

## Setting Up a Courtroom (Step-by-Step)

### Minimum Requirements

1. **Build an enclosed room** (needs walls and door)

2. **Place a table:**
   - Any table type works (RimWorld vanilla or modded)
   - Place it where you want the judge to sit
   - Right-click → "Designate as Judge's Bench"

3. **Place chairs with roles:**
   - **Required:** At least one Judge chair
   - **Required:** At least one Defendant chair
   - **Optional:** Victim, jury, and spectator chairs

### Recommended Layout

```
    +-----------------+
    |                 |
    |  [S] [S] [S]   |  S = Spectator chairs
    |                 |
    |  [J] [J] [J]   |  J = Jury chairs (optional)
    |                 |
    |     [D]         |  D = Defendant chair
    |                 |
    |  [===JB===]     |  JB = Judge's Bench (table)
    |     [Jg]        |  Jg = Judge chair
    |                 |
    |     [Door]      |
    +-----------------+
```

## Common Issues and Solutions

### Issue: "No suitable courtroom available"

**Causes:**
- No table designated as Judge's Bench
- Room has no judge chair
- Room has no defendant chair
- Room is outdoors
- Room is not accessible

**Solution:**
1. Check that a table is designated as Judge's Bench (inspect the table)
2. Verify at least one chair is assigned to "Judge" role
3. Verify at least one chair is assigned to "Defendant" role
4. Ensure the room has a roof and walls

### Issue: Participants not sitting in correct spots

**Cause:** Chairs not properly assigned roles

**Solution:**
- Right-click each chair and verify role assignment
- Reassign roles as needed
- Ensure chairs are accessible (not blocked)

### Issue: Gizmo not appearing on chairs

**Cause:** The chair doesn't have CompCourtroomChair component

**Solution:**
- This is normal for vanilla chairs
- The component needs to be added via XML patch or custom chair defs
- Check `Defs/` folder for examples of adding the component

## Technical Details for Modders

### Adding Components to Vanilla Furniture

**To add Comp_JudgesBench to vanilla tables:**
```xml
<Operation Class="PatchOperationAdd">
    <xpath>/Defs/ThingDef[defName="TableShort"]/comps</xpath>
    <value>
        <li Class="Law_and_Order.Source.Buildings.CompProperties_JudgesBench" />
    </value>
</Operation>
```

**To add CompCourtroomChair to vanilla chairs:**
```xml
<Operation Class="PatchOperationAdd">
    <xpath>/Defs/ThingDef[defName="DiningChair"]/comps</xpath>
    <value>
        <li Class="Law_and_Order.Source.Rituals.CompProperties_CourtroomChair" />
    </value>
</Operation>
```

### Validation Code

The courtroom validation happens in:
- `Source/Utils/CourtroomUtils.cs` - Room validation
- `Source/Hearings/HearingUtils.cs` - Ritual setup

**Key validation checks:**
1. Room has a building with `Comp_JudgesBench.IsDesignatedAsJudgesBench == true`
2. Room has chairs with `CompCourtroomChair.Role == CourtroomChairRole.Judge`
3. Room has chairs with `CompCourtroomChair.Role == CourtroomChairRole.Defendant`
4. Room is indoors (`room.TouchesMapEdge == false`)

## Summary

The courtroom chair system uses **two complementary components**:

| Component | Applied To | Purpose | Required? |
|-----------|-----------|---------|-----------|
| **Comp_JudgesBench** | Tables/Desks | Marks the ritual target | ✓ Yes (1 per room) |
| **CompCourtroomChair** | Chairs/Seats | Assigns seating roles | ✓ Yes (Judge + Defendant minimum) |

Both components are necessary because they serve different purposes:
- The **bench** defines where the ritual takes place
- The **chairs** define where participants sit

This separation allows for flexible courtroom layouts while maintaining clear ritual structure and participant positioning.
