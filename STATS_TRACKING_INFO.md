# Stats Tracking Information

## Hits
**Location**: `PlayerBodyV2_OnCollisionEnter_Patch`
**Trigger**: When a player body collides with another player body
**Conditions**:
- Must be different teams (`collisionPlayerBody.Player.Team.Value != __instance.Player.Team.Value`)
- Target player must NOT be down (not fallen/slipped)
- Hitter must NOT be down (not fallen/slipped)
- Collision must be on "Player" layer

## Passes
**Location**: `ProcessPuckTouch` (called from `Puck_OnCollisionEnter_Patch`)
**Trigger**: When a new player on the same team touches the puck after another player on the same team
**How it works**:
- Passes are **converted** from Touch events in real-time
- When a player touches the puck, the system checks if the last event was a Touch from a different player on the same team
- If found within **6 seconds (6000ms)**, the previous Touch event is converted to a Pass
- The current touch remains as a Touch (not converted to Reception)
- Only the passer's Touch event is converted to Pass; the receiver's touch stays as Touch

**Conditions**:
- Last event must be a `Touch` event
- Last event must be from a different player (`_lastEvent.PlayerSteamId != currentPlayerSteamId`)
- Last event must be from the same team (`_lastEvent.PlayerTeam == currentPlayer.Team.Value`)
- Time between touches: **< 6000ms** (6 seconds)

## Takeaways/Turnovers
**Location**: `ValidatePendingTurnoversTakeaways` (called from `RecordPlayByPlayEventInternal` when `_currentPlayInPossession == 1`)
**Trigger**: When a new team reaches their 2nd event in a possession chain (indicating a successful possession change)

**How it works**:
- Detection occurs when `_currentPlayInPossession == 1` (new team's 2nd event)
- System looks **backwards** through play-by-play events to find when possession changed
- Searches up to 22 events backwards to find the previous team's last successful play
- Validates that possession change was recent and legitimate

**Conditions**:
1. Must be triggered when new team hits their 2nd event (`_currentPlayInPossession == 1`)
2. Previous team must have had **2+ consecutive successful events** before possession changed
3. Possession change must be **recent** (within 4 seconds of previous team's last successful play)
4. Previous team's last successful play must **NOT** be a Shot (shot attempts never lead to turnovers)
5. Turnover player is identified as the player from the previous team's last successful play
6. Takeaway player is identified as the player from the new team's first successful event

**Shot Protection**:
- System checks if a shot occurred within **5 seconds** (before or after) the turnover time
- If a shot is found near the turnover, the turnover/takeaway is **cancelled** (not recorded)
- This prevents turnovers from being recorded when players are attempting shots

**Cooldown**:
- Turnovers have a **1 second cooldown** per player to prevent duplicate counting
- Takeaways have a **1 second cooldown** per player to prevent duplicate counting

**Event Placement**:
- Turnover/Takeaway events are placed in the timeline between the previous team's last event and the new team's first event
- GameTime is calculated as slightly after the previous team's last event (or midpoint if needed)

## Zone Entries (OZEntry) / Zone Exits (DZExit)
**Location**: `AddZoneFlagsToPlayByPlayEvents` (runs periodically every 6 seconds)
**Trigger**: When a player advances from one zone to another (detected retroactively)

**How it works**:
- Zone flags are added **retroactively** to existing play-by-play events
- System scans events every 6 seconds to detect zone transitions
- Compares consecutive events from the same player (or Pass events between different players)
- Flags are added to the event that represents the zone transition

**Zone Transitions Detected**:
- **DZExit**: Defensive Zone → Neutral Zone
- **OZEntry**: Neutral Zone → Offensive Zone
- **DZExit,OZEntry**: Defensive Zone → Offensive Zone (both flags added)

**Conditions**:
1. Must compare events from the same player, OR
2. If different players, the previous event must be a **Pass** (to track zone transitions via passes)
3. Zone must advance forward (DZ→NZ, NZ→OZ, or DZ→OZ)
4. Previous event must be from the same team (to find the last possession event)

**Duplicate Prevention**:
- System checks if the team has already recorded this zone transition
- A new entry/exit is only allowed if **possession changed** between the last transition and the current event
- Possession change is detected by looking for **any event from a different team** (excluding Hits)
- If no possession change occurred, the duplicate transition is blocked

**Event Types**:
- Zone flags can be added to any event type (Touch, Pass, Shot, etc.)
- The flag is added to the event that represents the zone transition moment

## Shots (SOG - Shots On Goal)
**Location**: `ServerManager_Update_Patch` (raycast detection) and `Puck_OnCollisionStay_Patch` (shot attempt detection)
**Trigger**: When puck is determined to be going to net (via raycast) or when player releases puck with sufficient force

**How it works**:
- **Raycast Detection**: System uses `PuckRaycast` component to continuously check if puck is going to net
- Raycast must be true for at least **2 checks (12 frames)** to filter false positives
- When raycast confirms puck is going to net, a Shot event is recorded with outcome "on net"
- **Shot Attempt Detection**: When player releases puck with sufficient force, a Shot event is recorded with outcome "attempt"
- Shot outcomes can be: "attempt", "on net", "missed", "blocked", or "goal"

**Conditions**:
1. Raycast must confirm puck is going to net (for "on net" shots)
2. OR player must release puck with sufficient force (for "attempt" shots)
3. Shot must be from a skater (not goalie)
4. Shot cooldown: **1 second** between shot attempts per player

**Shot Flags**:
- **"HomePlate"**: Shot from within home plate area (close to net)
- **"Outside"**: Shot from outside home plate area
- Flag is determined by shot position relative to goal

**Outcome Updates**:
- "attempt" → "on net" when raycast confirms
- "on net" → "goal" when goal is scored
- "on net" → "blocked" when shot is blocked
- "on net" → "missed" if raycast becomes false without save/goal/block

## Saves
**Location**: `Puck_OnCollisionStay_Patch` (goalie collision) and `ServerManager_Update_Patch` (save confirmation)
**Trigger**: When goalie touches a shot that is going to net

**How it works**:
- System detects when goalie (stick or body) collides with puck while `PuckIsGoingToNet` is true
- A `SaveCheck` is created when goalie touches puck
- When raycast becomes false (puck no longer going to net) and no goal occurred, save is confirmed
- Shot On Goal (SOG) is recorded for shooter, then Save is recorded for goalie

**Conditions**:
1. Puck must be going to net (`PuckIsGoingToNet == true`) when goalie touches it
2. Goalie must be in defensive zone (z position > 13.5)
3. Raycast must become false (puck stopped going to net)
4. No goal must have occurred
5. Shot attempt must exist within last 3 seconds

**Save Flags**:
- **"Stick"**: Save made with goalie stick
- **"Body"**: Save made with goalie body
- **"HomePlate"**: Save on a home plate shot (combined with Stick/Body)

**Note**: Saves + Goals = Shots On Goal (SOG)

## Blocks
**Location**: `Puck_OnCollisionStay_Patch` (defender collision) and `ServerManager_Update_Patch` (block confirmation)
**Trigger**: When a defending player (non-goalie) touches a shot that is going to net

**How it works**:
- System detects when defending player touches puck while `PuckIsGoingToNet` is true
- A `BlockCheck` is created when defender touches puck
- When raycast becomes false and no save/goal occurred, block is confirmed
- Shot outcome is updated to "blocked" if block is successful

**Conditions**:
1. Puck must be going to net when defender touches it
2. Defender must NOT be a goalie
3. Raycast must become false (puck stopped going to net)
4. No save must have occurred (`_lastShotWasCounted == false`)
5. Shot attempt must exist within last 3 seconds

**Block Outcomes**:
- **"successful"**: Shot was blocked and did not reach goalie/goal
- **"failed"**: Shot was blocked but still reached goalie/goal (shot went on net)

**Shot Updates**:
- When block is successful, shot outcome is updated to "blocked"
- Home plate flag is cleared for blocked shots (they don't count as home plate SOGs)
- When block is failed, shot outcome remains "on net"

## Goals
**Location**: `Puck_OnTriggerEnter_Patch` (goal detection)
**Trigger**: When puck enters the goal

**How it works**:
- System detects when puck collides with goal trigger
- Goal event is recorded for the player who last touched the puck
- Shot On Goal (SOG) is recorded if not already counted
- Shot outcome is updated to "goal" if shot event exists
- Assists are determined by game logic (last 2 players on same team to touch puck before goal scorer)

**Conditions**:
1. Puck must enter goal trigger
2. Player who last touched puck is credited with goal
3. Shot must be recorded (SOG) if not already counted

**Goal Flags**:
- **"HomePlate"**: Goal scored from home plate shot
- Flag is determined by shot position when goal is scored

## Possession Time
**Location**: `Puck_OnCollisionEnter_Patch` (in `ProcessPuckTouch`)
**Trigger**: When player touches puck (consecutive touches)

**How it works**:
- Possession time is calculated **only between consecutive touches** by the same player
- Time is accumulated when player touches puck again within **5 seconds** of last touch
- If gap between touches is > 5 seconds, time is NOT accumulated (possession lost)

**Conditions**:
1. Player must touch puck
2. Previous touch by same player must be within **5 seconds**
3. Time between touches is added to player's possession time

**Note**: This is different from team possession time, which tracks continuous team control regardless of individual touches.

## Time On Ice (TOI)
**Location**: `CalculateTimeOnIce()` (called at game end)
**Trigger**: Calculated retroactively at end of game from play-by-play events

**How it works**:
- System scans all play-by-play events for roster data
- Tracks when players enter/exit ice based on roster changes between events
- Calculates total time player was on ice during Playing phase
- Uses roster data captured in each event to determine ice time

**Conditions**:
1. Roster data must be captured in events (done automatically)
2. Player must be in roster for an event to count as "on ice"
3. Time is calculated between consecutive events where player is in roster

**Game Time Handling**:
- For 3-period games: Uses `PeriodEnd` event's GameTime (900.0 seconds) if found
- For overtime games: Uses maximum GameTime from all events
- Ensures accurate TOI for both regular time and overtime scenarios

## Faceoffs
**Location**: `PuckManager_Server_SpawnPuck_Patch`
**Trigger**: When puck spawns at period start or after goals

**How it works**:
- Faceoff event is recorded when puck spawns in `FaceOff` or `Playing` phase
- Faceoff outcome events are recorded for both teams (placeholder, updated when outcome determined)
- Outcome is determined by which team gains possession first (2+ consecutive events)

**Conditions**:
1. Puck must spawn (not a replay)
2. Game phase must be `FaceOff` or `Playing`
3. Only one faceoff recorded per period (tracked by `_faceoffRecordedForCurrentPeriod`)

**Faceoff Outcomes**:
- Recorded as "successful" for team that wins (gains possession first)
- Recorded as "failed" for team that loses
- Outcome is determined by tracking possession chain after faceoff




