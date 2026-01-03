# Export File Format Documentation

This document explains the structure and contents of the JSON and CSV files exported at the end of each game.

---

## File Naming Convention

Both files use the same `gameReferenceId` to ensure they can be matched:
- **Format**: `{FileHeaderName}_stats_{gameReferenceId}.json` and `{FileHeaderName}_playbyplay_{gameReferenceId}.csv`
- **gameReferenceId**: Generated at game start as `yyyyMMdd_HHmmss` (e.g., `20260103_100304`)
- **Location**: Both files are saved in the `stats/` folder in the server directory

---

## JSON File (`*_stats_*.json`)

The JSON file contains **aggregated game statistics** - summary data for players and teams.

### Structure

```json
{
  "players": [ ... ],           // Array of player stat objects
  "teamStats": { ... },          // Team-level statistics
  "stars": [ ... ],              // Star selections (1st, 2nd, 3rd)
  "gwg": "steamId",              // Game-winning goal scorer Steam ID
  "gameTimeMinutes": 0.0         // Total game time in minutes
}
```

### Player Stats (`players` array)

Each player object contains the following fields:

#### All Players
- **`steamId`** (string): Player's Steam ID
- **`name`** (string): Player's username
- **`team`** (string): "Blue", "Red", or "spectator"
- **`position`** (string): Player position (e.g., "C", "LW", "RW", "LD", "RD", "G")
- **`goals`** (int): Number of goals scored
- **`assists`** (int): Number of assists
- **`sog`** (int): Shots on goal
- **`passes`** (int): Number of passes completed
- **`exits`** (int): Defensive zone exits
- **`entries`** (int): Offensive zone entries
- **`hits`** (int): Number of hits
- **`turnovers`** (int): Number of turnovers
- **`takeaways`** (int): Number of takeaways
- **`puckTouches`** (int): Total puck touches
- **`possessionTimeSeconds`** (float): Total possession time in seconds (between consecutive touches)
- **`timeOnIce`** (float): Time on ice in seconds (calculated from roster data)
- **`faceoffWins`** (int): Faceoffs won
- **`faceoffLosses`** (int): Faceoffs lost

#### Goalies Only (additional fields)
- **`shotsFaced`** (int): Total shots faced
- **`saves`** (int): Total saves made
- **`saveperc`** (float): Save percentage (saves / shotsFaced)
- **`bodySaves`** (int): Saves made with body
- **`stickSaves`** (int): Saves made with stick

### Team Stats (`teamStats` object)

#### Blue Team Stats
- **`blueTeamSogs`** (int): Blue team shots on goal
- **`blueTeamSogsAg`** (int): Blue team shots on goal allowed (Red team's SOG)
- **`blueTeamPasses`** (int): Blue team passes
- **`blueTeamPassesAg`** (int): Blue team passes allowed (Red team's passes)
- **`blueFaceoffsWon`** (int): Blue team faceoffs won
- **`blueFaceoffsLost`** (int): Blue team faceoffs lost
- **`blueTakeaways`** (int): Blue team takeaways
- **`blueTurnovers`** (int): Blue team turnovers
- **`blueDZExits`** (int): Blue team defensive zone exits
- **`blueDZExitsAllowed`** (int): Blue team DZ exits allowed (Red team's exits)
- **`blueOZEntries`** (int): Blue team offensive zone entries
- **`blueOZEntriesAllowed`** (int): Blue team OZ entries allowed (Red team's entries)
- **`blueTeamPossessionTime`** (float): Blue team total possession time in seconds
- **`blueTeamPossessionTimeAg`** (float): Blue team possession time allowed (Red team's possession time)

#### Red Team Stats
- Same structure as Blue team stats, with `red` prefix instead of `blue`

#### Goal Tracking
- **`bluegoals`** (array of strings): Array of Steam IDs who scored each Blue goal (in order)
- **`redgoals`** (array of strings): Array of Steam IDs who scored each Red goal (in order)
- **`blueassists`** (array of strings): Array of Steam IDs who assisted each Blue goal (in order)
- **`redassists`** (array of strings): Array of Steam IDs who assisted each Red goal (in order)

### Other Fields
- **`stars`** (array of strings): Steam IDs of 1st, 2nd, and 3rd star selections (in order)
- **`gwg`** (string): Steam ID of the game-winning goal scorer (empty if no GWG)
- **`gameTimeMinutes`** (float): Total game time in minutes (actual duration from events)
  - Typically ~15.0 minutes for regulation games (3 periods × 5 minutes each)
  - Will be longer for overtime games
  - Will be shorter for early endings (forfeits)
  - Calculated from the maximum `gameTime` value in all play-by-play events

---

## CSV File (`*_playbyplay_*.csv`)

The CSV file contains **event-by-event play-by-play data** - a chronological record of every event that occurred during the game.

### Structure

The CSV file has a header row and one row per event. Events are ordered chronologically by `gameTime`.

### Columns

1. **`gameReferenceId`** (string): Game reference ID (matches JSON filename)
2. **`id`** (int): Sequential event ID (0, 1, 2, ...)
3. **`period`** (int): Period number (1, 2, 3, 4+ for overtime)
4. **`gameTime`** (float): Game time in seconds (0.000 to 900.000+)
5. **`team`** (string): Team of the player who performed the event ("Blue", "Red", or empty)
6. **`teamInPossession`** (string): Team that has possession of the puck ("Blue", "Red", or empty)
7. **`currentPlayInPossession`** (string): Possession chain counter (increments for consecutive events by same team)
8. **`scoreState`** (string): Score at time of event (e.g., "0-0", "1-0", "2-1")
9. **`name`** (string): Event type name (see Event Types below)
10. **`zone`** (string): Zone where event occurred ("dz", "nz", "oz")
11. **`outcome`** (string): Event outcome (see Outcomes below)
12. **`flags`** (string): Event flags (comma-separated, e.g., "OZEntry", "HomePlate", "Stick")
13. **`xCoord`** (float): X coordinate of event position
14. **`yCoord`** (float): Y coordinate of event position
15. **`zCoord`** (float): Z coordinate of event position
16. **`playerJersey`** (int): Player's jersey number
17. **`playerPosition`** (string): Player's position ("C", "LW", "RW", "LD", "RD", "G")
18. **`playerName`** (string): Player's username
19. **`forcemagnitude`** (float): Force magnitude of the event (for shots/passes)
20. **`PlayerSpeed`** (float): Player speed in km/h at time of event
21. **`PuckVelocity`** (float): Puck velocity magnitude in m/s
22. **`playerReferenceSteamID`** (string): Player's Steam ID
23. **`teamForwardsSteamID`** (string): Comma-separated Steam IDs of forwards on player's team
24. **`teamDefencemenSteamID`** (string): Comma-separated Steam IDs of defencemen on player's team
25. **`teamGoalieSteamID`** (string): Steam ID of goalie on player's team
26. **`opposingTeamForwardsSteamID`** (string): Comma-separated Steam IDs of forwards on opposing team
27. **`opposingTeamDefencemenSteamID`** (string): Comma-separated Steam IDs of defencemen on opposing team
28. **`opposingTeamGoalieSteamID`** (string): Steam ID of goalie on opposing team

### Event Types (`name` column)

- **`faceoff`**: Faceoff at start of period/after goal
- **`faceoffoutcome`**: Faceoff win/loss outcome
- **`touch`**: Player touched the puck
- **`pass`**: Pass completed (converted from touch)
- **`shot`**: Shot attempt or shot on goal
- **`goal`**: Goal scored
- **`save`**: Goalie save
- **`block`**: Shot blocked by defender
- **`hit`**: Player hit another player
- **`takeaway`**: Takeaway (possession gained)
- **`turnover`**: Turnover (possession lost)
- **`puckbattle`**: Puck battle (multiple players contesting)
- **`periodend`**: End of period

### Event Outcomes (`outcome` column)

- **`successful`**: Event was successful (default for most events)
- **`failed`**: Event failed (e.g., failed block, failed faceoff)
- **`neutral`**: Neutral outcome (e.g., puck battle)
- **`attempt`**: Shot attempt (not yet confirmed as on net)
- **`on net`**: Shot on goal (confirmed by raycast)
- **`missed`**: Shot missed the net
- **`blocked`**: Shot was blocked
- **`goal`**: Shot resulted in goal
- **`""`** (empty): No outcome specified (e.g., faceoff)

### Event Flags (`flags` column)

Flags are comma-separated and indicate additional event properties:

- **`OZEntry`**: Offensive zone entry
- **`DZExit`**: Defensive zone exit
- **`HomePlate`**: Shot/save from home plate area (close to net)
- **`Outside`**: Shot from outside home plate area
- **`Stick`**: Save made with stick
- **`Body`**: Save made with body
- **`defending`**: Player defending in puck battle
- **`contesting`**: Player contesting in puck battle

### Roster Data (Columns 23-28)

The last 6 columns contain roster snapshots at the time of each event:
- **`teamForwardsSteamID`**: Forwards on the event player's team (comma-separated)
- **`teamDefencemenSteamID`**: Defencemen on the event player's team (comma-separated)
- **`teamGoalieSteamID`**: Goalie on the event player's team
- **`opposingTeamForwardsSteamID`**: Forwards on the opposing team (comma-separated)
- **`opposingTeamDefencemenSteamID`**: Defencemen on the opposing team (comma-separated)
- **`opposingTeamGoalieSteamID`**: Goalie on the opposing team

This roster data is used to calculate Time On Ice (TOI) by tracking when players enter/exit the ice.

---

## Key Differences

| Aspect | JSON File | CSV File |
|--------|-----------|----------|
| **Purpose** | Aggregated summary statistics | Event-by-event chronological data |
| **Data Type** | Summary totals and averages | Individual event records |
| **Use Case** | Quick stats lookup, leaderboards | Detailed analysis, replay reconstruction |
| **Size** | Small (one object per player) | Large (one row per event, 300+ events) |
| **Time Data** | Total possession time, TOI | Exact gameTime for each event |
| **Position Data** | Single position per player | Position at time of each event |
| **Roster Data** | Not included | Full roster snapshot per event |

---

## Export Conditions

Both files are only exported if:
- **8+ unique players** participated in the game
- **300+ events** were recorded

If these conditions are not met, neither file is exported (prevents exporting incomplete/warmup games).

---

## Usage Examples

### JSON File
- **Leaderboards**: Sort players by goals, assists, SOG, etc.
- **Team Stats**: Compare team possession time, faceoff wins, etc.
- **Player Profiles**: Display career stats aggregated across games
- **Star Selections**: Identify top performers

### CSV File
- **Event Analysis**: Find all shots in a specific time period
- **Zone Transition Analysis**: Track zone entries/exits over time
- **Possession Chains**: Analyze possession sequences using `currentPlayInPossession`
- **Replay Reconstruction**: Recreate game flow from event data
- **TOI Calculation**: Calculate time on ice from roster changes
- **Shot Location Analysis**: Analyze shot positions (xCoord, yCoord, zCoord)

---

*Last Updated: January 2025*
