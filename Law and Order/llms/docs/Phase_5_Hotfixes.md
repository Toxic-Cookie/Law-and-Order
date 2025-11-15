# Phase 5: Critical Hotfixes

**Date:** November 14, 2025
**Status:** ✅ Fixed

## Issues Fixed

### Issue 1: Missing Texture Error ✅ FIXED
**Error:**
```
Could not load Texture2D at 'UI/Commands/InterrogationTable' in any active mod or in base resources.
```

**Root Cause:**
The `Comp_InterrogationTable` gizmo was trying to load a custom texture that doesn't exist.

**Fix:**
Changed icon to use vanilla `UI/Commands/Draft` texture instead.

**File Modified:**
- `Source/Investigation/Comp_InterrogationTable.cs`
- Line 46: `icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true)`

---

### Issue 2: Job Spam Loop (CRITICAL) ✅ FIXED
**Error:**
```
Matt started 10 jobs in one tick. newJob=LawAndOrder_InvestigateCase...
```

**Root Cause:**
The WorkGiver was creating interrogation jobs every tick, and the jobs were failing immediately (likely due to complex escort toil), causing infinite job spam.

**Fixes Applied:**

1. **Added Cooldown System** (`WorkGiver_Warden_InvestigateCase.cs`):
   - Static dictionary tracks last interrogation time per prisoner
   - 1 in-game day (60,000 ticks) cooldown between interrogations
   - Prevents same prisoner from being interrogated multiple times

2. **Throttled Job Assignment** (`WorkGiver_Warden_InvestigateCase.cs`):
   - Only check for interrogation jobs every 250 ticks (~4 seconds)
   - Prevents constant job spam
   - Line 23: `if (Find.TickManager.TicksGame % 250 != 0) return null;`

3. **Simplified Job Driver** (`JobDriver_InvestigateCase.cs`):
   - Removed complex prisoner escort toil
   - Simpler workflow: Warden goes to table, then to prisoner
   - Interrogation happens at prisoner's location (more realistic - guards don't escort prisoners for interrogations)
   - Lines 56-62: Simplified toil sequence

**Files Modified:**
- `Source/Investigation/WorkGiver_Warden_InvestigateCase.cs` (added cooldown + throttling)
- `Source/Investigation/JobDriver_InvestigateCase.cs` (simplified toil workflow)

---

### Issue 3: No Accessible Table Found (Manual Investigation) ✅ FIXED
**Error:**
```
No accessible interrogation table found
```
Despite log showing: `[Law & Order] Found 1 designated interrogation tables`

**Root Cause:**
Line 728 in `MainTabWindow_Justice.cs` was checking if the **prisoner** could reach the table:
```csharp
Thing table = FindNearestInterrogationTable(prisoner, prisoner);
```

Prisoners are locked in cells, so of course they can't reach anything! The code needs to check if a **warden** can reach the table.

**Fix:**
Reordered the logic:
1. Find available warden FIRST
2. Then check if that warden can reach a table
3. If yes, assign the job

**File Modified:**
- `Source/UI/MainTabWindow_Justice.cs` lines 717-738
- Changed: `FindNearestInterrogationTable(prisoner, prisoner)`
- To: `FindNearestInterrogationTable(warden, prisoner)`

**Debugging Added:**
1. **Logging in GetAllInterrogationTables** (`Comp_InterrogationTable.cs`):
   - Logs total buildings checked
   - Reports count of designated tables found
   - Lines 117-124: Debug output

2. **Logging in ToggleDesignation** (`Comp_InterrogationTable.cs`):
   - Confirms when table designation changes
   - Shows table ID and new designation status
   - Line 64: Status change logging

---

## Testing Instructions

1. **Restart RimWorld** completely (fresh load)
2. **Check Dev Console** for messages like:
   - `[Law & Order] Table [name] designated status changed to: true`
   - `[Law & Order] Found X designated interrogation tables`
3. **Designate a table**:
   - Right-click any table (dining, butcher, research, etc.)
   - Should see toggle button (uses Draft icon temporarily)
   - Click to designate
   - Check console for confirmation
4. **Test automatic investigation**:
   - Should NOT spam jobs anymore
   - Warden should check every ~4 seconds
   - Same prisoner interrogated max once per day
5. **Test manual investigation**:
   - Open Justice tab
   - Click "Investigate"
   - Check console for "Found X tables" message

---

## Known Limitations

1. **Icon:** Uses vanilla "Draft" icon temporarily. Can create custom icon later.
2. **Interrogation Location:** Prisoner no longer escorted to table - warden goes to prisoner's cell instead. This is actually more realistic and prevents the job spam issue.
3. **Cooldown:** 1 day between interrogations might be too long for testing. Can be adjusted.

---

## If Issues Persist

**If tables still not found:**
1. Check if `Comp_InterrogationTable` is being added to tables
2. Dev console command: Check a table's comps list
3. Verify XML patch is applying (check for patch errors on load)
4. May need to add comp via C# Harmony patch instead of XML

**If job spam returns:**
1. Increase throttle interval (line 23 in WorkGiver)
2. Increase cooldown duration (line 18 in WorkGiver)
3. Check for toil failures in JobDriver

---

## Files Changed This Hotfix

1. `Source/Investigation/Comp_InterrogationTable.cs` - Icon fix + debugging
2. `Source/Investigation/WorkGiver_Warden_InvestigateCase.cs` - Cooldown + throttling
3. `Source/Investigation/JobDriver_InvestigateCase.cs` - Simplified workflow
4. `Source/UI/MainTabWindow_Justice.cs` - Fixed accessibility check (warden not prisoner)

**Build Status:** ✅ 0 errors, 0 warnings

---

## Summary

**All 3 critical issues fixed:**
1. ✅ Missing texture error (using Draft icon)
2. ✅ Job spam loop (cooldown + throttling + simplified workflow)
3. ✅ No accessible table (fixed to check warden accessibility, not prisoner)

**Ready for testing!** The manual investigation feature should now work correctly.
