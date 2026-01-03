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
**Location**: `Puck_OnCollisionStay_Patch`
**Trigger**: When puck moves from one player to another on the same team
**Conditions**:
- Same team (`_lastTeamOnPuckTipIncluded == player.Team.Value`)
- Different players (`playerSteamId != lastPlayerOnPuckTipIncluded`)
- Time between touches: **80ms to 5000ms**
- Uses `_lastPlayerOnPuckTipIncludedSteamId` to track last player who touched puck

## Takeaways/Turnovers
**Location**: `Puck_OnCollisionStay_Patch`
**Trigger**: When possession changes from one team to another
**Parameters** (from ServerConfig):
- `MinPossessionMilliseconds = 300` - Minimum time to be considered "in possession"
- `MaxPossessionMilliseconds = 700` - Maximum time before possession expires
- `MaxTippedMilliseconds = 91` - Time before puck is considered "tipped" vs "possessed"

**Conditions**:
1. Uses `GetPlayerSteamIdInPossession()` to determine current possession
2. Must have previous possession (`_lastPossession.Team != PlayerTeam.None`)
3. Teams must be different (`player.Team.Value != _lastPossession.Team`)
4. Different players (`currentPossessionSteamId != _lastPossession.SteamId`)
5. Must happen within **500ms** of last possession (`(DateTime.UtcNow - _lastPossession.Date).TotalMilliseconds < 500`)

**How it works**:
- `GetPlayerSteamIdInPossession` checks:
  - Player must have touched puck within `MinPossessionMilliseconds` (300ms)
  - Player must currently be touching puck (`playersCurrentPuckTouch` contains player)
  - Puck must not be "tipped" (touch duration > `MaxTippedMilliseconds` = 91ms)
  - If multiple players meet criteria, possession is "challenged" and returns empty string

**Potential Issues**:
- If `GetPlayerSteamIdInPossession` returns empty (challenged possession), takeaways won't be counted
- The 500ms window might be too short for some situations
- Possession detection requires specific timing windows that might not always be met




