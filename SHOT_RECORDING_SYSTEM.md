# Shot Recording System - Detailed Documentation

This document explains in detail how shots are detected, recorded, and tracked in the Stats mod.

---

## Overview

The shot recording system uses a **two-phase approach**:
1. **Shot Attempt Detection**: Detects when a player releases the puck with sufficient force and direction
2. **Raycast Confirmation**: Uses physics raycasts to confirm if the puck is actually going to net

This dual-system approach ensures accurate shot tracking while filtering out passes and other puck movements.

---

## Phase 1: Shot Attempt Detection

**Location**: `Puck_OnCollisionExit_Patch` (when puck leaves player's stick)

### Trigger Conditions

A shot attempt is recorded when **ALL** of the following conditions are met:

1. **Puck leaves stick**: `!__instance.IsTouchingStick`
2. **Player is not a goalie**: `!PlayerFunc.IsGoalie(player)`
3. **Sufficient velocity**: Puck speed >= minimum threshold
4. **Moving towards net**: Puck velocity in correct Z direction
5. **Moving towards opponent's net**: Not going towards own net
6. **Velocity aligned with net**: Angle check ensures puck is actually heading towards goal
7. **Player releasing possession**: Player was last to touch the puck
8. **Cooldown expired**: At least 1 second since last shot attempt by this player

### Velocity Requirements

The system uses **distance-based velocity thresholds**:

- **Standard shots** (within 50 units of net): Minimum **12 m/s**
- **Distance shots** (beyond 50 units): Minimum **25 m/s**

This prevents weak passes from being counted as shots while allowing long-range shots.

### Direction Checks

#### 1. Moving Towards Net (Z Direction)
- **Blue team**: Puck velocity Z < 0 (moving towards negative Z, Red's goal at z=-40)
- **Red team**: Puck velocity Z > 0 (moving towards positive Z, Blue's goal at z=40)

#### 2. Moving Towards Opponent's Net
- **Blue team**: `puckVel.z < 0 && puckPos.z > -40` (moving negative, not past goal)
- **Red team**: `puckVel.z > 0 && puckPos.z < 40` (moving positive, not past goal)

This prevents own-goal attempts from being counted as shots.

#### 3. Velocity Alignment Check (Angle-Based)

The system verifies that the puck's velocity vector is aligned with the direction to the net center:

- **Net centers**: 
  - Blue shoots at: `(0, 1.0, -40)` (Red's goal)
  - Red shoots at: `(0, 1.0, 40)` (Blue's goal)

- **Angle calculation**: Uses weighted dot product between velocity and direction-to-net
  - Y component is weighted at **0.5** (less important than X/Z)
  - This filters out cross-ice passes that happen to be moving towards net initially

- **Angle thresholds**:
  - **Standard shots** (within 50 units): **18-degree** threshold (cos(18°) ≈ 0.951)
  - **Distance shots** (beyond 50 units): **12-degree** threshold (cos(12°) ≈ 0.978)

Tighter angle requirement for distance shots prevents wide-angle passes from being counted.

### Shot Attempt Recording

When all conditions are met:

1. **Shot event created** with outcome `"attempt"`
2. **Shot flag determined** (`"HomePlate"` or `"Outside"`) based on puck position
3. **Shot attempt stats tracked** (separate from SOG)
4. **Release info stored** in `_pendingShotReleases` for potential retroactive recording
5. **Cooldown updated** (prevents duplicate attempts within 1 second)

**Shot Flag Determination**:
- Uses `DetermineShotFlag()` function
- Checks if puck position is within "home plate" region (close to net)
- Home plate region: Square (18-30 units from goal) + Triangle (30-40 units from goal)
- Returns `"HomePlate"` if in region, `"Outside"` otherwise

---

## Phase 2: Raycast Confirmation System

**Location**: `ServerManager_Update_Patch` (runs every frame, checks every 6 frames)

### Raycast System (`PuckRaycast` Component)

The raycast system uses **4 rays** cast from the puck to detect if it's going to hit the goal:

1. **Bottom Left Ray**: Left side of puck, slightly above ground
2. **Bottom Right Ray**: Right side of puck, slightly above ground
3. **Far Bottom Left Ray**: Left side of puck, lower position
4. **Far Bottom Right Ray**: Right side of puck, lower position

**Raycast Parameters**:
- **Check frequency**: Every **6 frames** (`CHECK_EVERY_X_FRAMES = 6`)
- **Max distance**: **26 units**
- **Layer mask**: "Goal Trigger" layer (layer 15)

**How it works**:
- Rays are cast in the direction of puck movement
- If **any** of the 4 rays hits the goal trigger, `PuckIsGoingToNet[team] = true`
- The team stored is the **defending team** (team whose goal is being shot at)

### Raycast Confirmation Logic

**Confirmation Requirements**:
- Raycast must be `true` for at least **2 consecutive checks** (12 frames total)
- This filters out false positives from puck bouncing near goal

**When Raycast Confirms**:
1. **Mark shot as confirmed**: `_shotRecordedForRaycast[defendingTeam] = true`
2. **Do NOT immediately mark as "on net"**: Outcome stays as `"attempt"` until save/goal/block occurs
3. **Keep pending release info**: Used for retroactive shot recording if needed

**Important**: Raycast confirmation does **NOT** immediately create a shot event. The shot attempt from Phase 1 is what creates the event. Raycast only confirms it's going to net.

---

## Shot Outcome Updates

Shot outcomes can be: `"attempt"`, `"on net"`, `"missed"`, `"blocked"`, or `"goal"`

### Outcome Transitions

#### "attempt" → "on net"
- **When**: Save or goal is recorded for the shot
- **Location**: `ServerManager_Update_Patch` (save/goal logic)
- **Process**: 
  - Finds existing shot event with outcome `"attempt"`
  - Updates outcome to `"on net"`
  - Sets shot flag (HomePlate/Outside) if not already set
  - Records SOG for shooter

#### "attempt" → "missed"
- **When**: Shot attempt times out without save/goal/block
- **Location**: `ServerManager_Update_Patch` (missed shot detection)
- **Timeout by zone**:
  - **Offensive Zone**: 2 seconds
  - **Neutral Zone**: 4 seconds
  - **Defensive Zone**: 6 seconds
- **Process**:
  - Checks all `"attempt"` shots older than timeout
  - Verifies no save/goal/block occurred within 3 seconds of shot
  - Updates outcome to `"missed"`
  - Clears HomePlate flag if present (missed shots don't count as home plate SOGs)

#### "on net" → "missed"
- **When**: Raycast becomes false (puck stopped going to net) without save/goal/block
- **Location**: `ServerManager_Update_Patch` (raycast state change)
- **Process**:
  - Finds recent `"on net"` shots (within last 2 seconds)
  - Checks if save/goal/block occurred within 4 seconds
  - If not, updates to `"missed"` and clears HomePlate flag

#### "on net" → "blocked"
- **When**: Shot is successfully blocked by defender
- **Location**: `ProcessBlock()` function
- **Process**:
  - Finds corresponding shot event
  - Updates outcome to `"blocked"`
  - Clears HomePlate flag (blocked shots don't count as home plate SOGs)

#### "on net" → "goal"
- **When**: Shot results in a goal
- **Location**: `SendSOGDuringGoal()` function
- **Process**:
  - Finds corresponding shot event
  - Updates outcome to `"goal"`
  - Preserves HomePlate flag if present

---

## Shots On Goal (SOG) Recording

**SOG is only recorded when a save or goal occurs** - not when raycast confirms.

### SOG Recording Locations

#### 1. During Save (`ServerManager_Update_Patch`)
- **Trigger**: When save is confirmed (raycast false, no goal)
- **Process**:
  1. Record SOG for shooter
  2. Update shot event outcome to `"on net"` (if still `"attempt"`)
  3. Set shot flag (HomePlate/Outside)
  4. Track home plate SOGs if applicable
  5. Record Save event for goalie

#### 2. During Goal (`SendSOGDuringGoal()`)
- **Trigger**: When goal is scored
- **Process**:
  1. Record SOG for shooter (if not already counted)
  2. Find or create shot event
  3. Update outcome to `"goal"`
  4. Set shot flag
  5. Track home plate SOGs if applicable
  6. Record Goal event

### Retroactive Shot Recording

If a save/goal occurs but no shot event exists (edge case), the system can create one retroactively:

- **Uses**: `_pendingShotReleases` data stored during shot attempt
- **Location**: Save/goal confirmation logic
- **Process**:
  - Looks up pending release info for the shooter
  - Creates new shot event with `"on net"` or `"goal"` outcome
  - Uses release position/velocity for accurate data
  - Records SOG and tracks stats

---

## Shot Flags: HomePlate vs Outside

### HomePlate Flag

**Purpose**: Identifies shots taken from close range (home plate area)

**Region Definition**:
- **Square region**: 18-30 units from goal, X: -11 to +11
- **Triangle region**: 30-40 units from goal, X: -11 to +11 (tapering to -2 to +2 at goal line)

**Calculation**: `DetermineShotFlag()` function checks puck position against these boundaries

**Usage**:
- Home plate SOGs are tracked separately (`_homePlateSogs`)
- Home plate saves are tracked separately (`_homePlateSaves`)
- Home plate shots faced are tracked separately (`_homePlateShots`)
- Used for advanced goalie statistics

**Flag Clearing**:
- Cleared for `"missed"` shots (don't count as home plate SOGs)
- Cleared for `"blocked"` shots (don't count as home plate SOGs)
- Preserved for `"goal"` shots

### Outside Flag

**Purpose**: Identifies shots taken from outside the home plate area

**Default**: If not HomePlate, flag is `"Outside"` (or empty string)

---

## Shot Cooldown System

**Purpose**: Prevents duplicate shot attempts from rapid puck releases

**Cooldown Duration**: **1 second** per player

**Implementation**:
- Tracks `_lastShotAttemptGameTime[playerSteamId]`
- Only records new shot attempt if `currentGameTime - lastShotGameTime >= 1.0`
- Cooldown is **per player** (different players can shoot simultaneously)

**Note**: Cooldown only applies to shot **attempts**. If raycast confirms a shot that was blocked by cooldown, it can still be recorded retroactively when save/goal occurs.

---

## Shot Timeout System

**Purpose**: Automatically marks unconfirmed shot attempts as "missed" after a timeout period

**Timeout by Zone**:
- **Offensive Zone (OZ)**: **2 seconds**
- **Neutral Zone (NZ)**: **4 seconds**
- **Defensive Zone (DZ)**: **6 seconds**

**Logic**:
- Checks all `"attempt"` shots periodically
- If shot is older than timeout AND no save/goal/block occurred within 3 seconds, mark as `"missed"`
- Uses longer 3-second window to account for delayed saves

**Why zone-based?**:
- Shots from offensive zone should reach goal quickly (2s)
- Shots from defensive zone take longer to travel (6s)
- Prevents premature "missed" classification

---

## Shot Event Data

Each shot event contains:

- **EventType**: `PlayByPlayEventType.Shot`
- **Outcome**: `"attempt"`, `"on net"`, `"missed"`, `"blocked"`, or `"goal"`
- **Flags**: `"HomePlate"` or `"Outside"` (or empty)
- **Position**: Puck position when shot was taken
- **Velocity**: Puck velocity when shot was taken
- **PlayerSpeed**: Shooter's speed at time of shot
- **GameTime**: Exact game time when shot occurred
- **Zone**: Zone where shot was taken (DZ, NZ, or OZ)

---

## Flow Diagram

```
Player Releases Puck
    ↓
[Phase 1: Shot Attempt Detection]
    ├─ Velocity Check (12 m/s or 25 m/s)
    ├─ Direction Check (towards net)
    ├─ Angle Check (aligned with net)
    ├─ Possession Check (player releasing)
    └─ Cooldown Check (1 second)
    ↓
Record Shot Event (outcome: "attempt")
Store Release Info in _pendingShotReleases
    ↓
[Phase 2: Raycast Confirmation]
    ├─ Raycast checks every 6 frames
    ├─ 4 rays cast from puck
    └─ If any ray hits goal trigger → confirmed
    ↓
Raycast Confirms (2 consecutive checks)
    ↓
Mark _shotRecordedForRaycast = true
(Outcome still "attempt" - not "on net" yet)
    ↓
[Outcome Updates]
    ├─ Save occurs → "attempt" → "on net" + Record SOG
    ├─ Goal occurs → "attempt"/"on net" → "goal" + Record SOG
    ├─ Block occurs → "on net" → "blocked"
    ├─ Timeout expires → "attempt" → "missed"
    └─ Raycast false → "on net" → "missed" (if no save/goal/block)
```

---

## Key Design Decisions

### Why Two-Phase System?

1. **Shot Attempt Detection** (Phase 1): Captures player intent immediately when puck is released
2. **Raycast Confirmation** (Phase 2): Validates that puck is actually going to net

This prevents:
- Missing shots that are confirmed by raycast but didn't meet velocity thresholds
- Counting passes as shots (angle check + raycast validation)

### Why Not Mark "on net" Immediately?

SOG is only recorded when save/goal occurs, not when raycast confirms. This keeps play-by-play in sync with tooltip stats (SOG = Saves + Goals).

### Why Zone-Based Timeouts?

Shots from different zones take different amounts of time to reach goal. Zone-based timeouts prevent premature "missed" classification.

### Why HomePlate Flag Clearing?

Home plate SOGs should only count when save/goal occurs. Missed or blocked shots shouldn't count as home plate SOGs, even if they were taken from home plate.

---

## Edge Cases Handled

1. **Shot attempt blocked by cooldown but raycast confirms**: Retroactive recording when save/goal occurs
2. **Save occurs before shot event exists**: Creates retroactive shot event from `_pendingShotReleases`
3. **Raycast confirms but no save/goal**: Timeout system marks as "missed"
4. **Shot marked "on net" but raycast becomes false**: Re-evaluates and marks as "missed" if no save/goal/block
5. **Multiple shots in quick succession**: Cooldown prevents duplicates, but each valid shot is tracked separately

---

## Special Scenarios

### Puck Battle → Shot on Goalie

**Scenario**: Two players fight for the puck, puck squirts out and goes towards the goalie.

**Will a shot be recorded?**

**Yes, potentially** - if all shot criteria are met:

1. **Player releasing possession**: The last player whose stick was touching the puck (from `_lastPlayerOnPuckTipIncludedSteamId`) is considered "releasing possession"
2. **Direction check**: The puck must be moving towards the **opponent's net** (not own net)
3. **Velocity/angle checks**: Must meet velocity and angle requirements

**Key Point**: The shot is attributed to the player who was last touching the puck when it exited, **not** necessarily the player who "won" the battle. If the puck squirts out with sufficient velocity towards the opponent's net, it will be recorded as a shot attempt by that player.

**Example**:
- Blue and Red players battle for puck
- Blue player was last to touch (`_lastPlayerOnPuckTipIncludedSteamId[Blue] = BluePlayer`)
- Puck squirts out with 15 m/s velocity towards Red's goal (z=-40)
- **Result**: Shot recorded for Blue player (even though it was a battle)

### Own-Goal Shot Prevention

**Scenario**: Team accidentally shoots towards their own goalie/net.

**Will a shot be recorded?**

**No** - the `movingTowardsOpponentNet` check prevents this:

**Blue Team Shooting at Own Net (z=40)**:
- Check: `puckVel.z < 0 && puckPos.z > -40`
- If shooting at own net: `puckVel.z` would be **positive** (moving towards z=40)
- **Result**: Check fails, no shot recorded

**Red Team Shooting at Own Net (z=-40)**:
- Check: `puckVel.z > 0 && puckPos.z < 40`
- If shooting at own net: `puckVel.z` would be **negative** (moving towards z=-40)
- **Result**: Check fails, no shot recorded

**Key Protection**: The direction check ensures shots are only recorded when moving towards the **opponent's** net, not own net.

### Difference Between Battle Shot and Own-Goal Attempt

| Aspect | Puck Battle Shot | Own-Goal Attempt |
|--------|------------------|-----------------|
| **Direction** | Towards opponent's net | Towards own net |
| **Velocity Check** | Must pass (12/25 m/s) | Must pass (but irrelevant) |
| **Direction Check** | ✅ Passes (`movingTowardsOpponentNet = true`) | ❌ Fails (`movingTowardsOpponentNet = false`) |
| **Result** | Shot recorded | No shot recorded |
| **Attribution** | Last player to touch puck | N/A (not recorded) |

**Important**: During a puck battle, if the puck squirts out:
- **Towards opponent's net** → Shot recorded (if velocity/angle criteria met)
- **Towards own net** → No shot recorded (direction check fails)

The system cannot distinguish between an "intentional shot" and a "battle squirt" - it only checks if the puck meets shot criteria when released. This is by design, as the outcome (puck going towards opponent's net with sufficient velocity) is what matters for statistics.

---

*Last Updated: January 2025*
