# Star Points System Breakdown

## Overview
The star points system calculates player performance throughout the game to determine the "Stars of the Match" at the end of each game. Points are calculated in real-time and can be viewed in player tooltips during the game.

---

## Goalie Point System

### Base Stats
- **Goal Allowed**: -10 points per goal allowed
- **Shot Faced**: 10 points per shot faced
- **Shutout Bonus**: 100 points (awarded if goalie allows 0 goals and faces at least 1 shot)

### Additional Stats
- **Pass**: 2.5 points per pass
- **Goal Scored**: 175 points per goal
- **Assist**: 30 points per assist

### Example Calculation
**Goalie with 10 saves, 2 goals allowed, 12 shots faced:**
- Goals Allowed: 2 × (-10) = -20 points
- Shots Faced: 12 × 10 = 120 points
- **Subtotal**: -20 + 120 = **100 points**

**Goalie with 10 saves, 0 goals allowed (Shutout), 10 shots faced:**
- Goals Allowed: 0 × (-10) = 0 points
- Shots Faced: 10 × 10 = 100 points
- Shutout Bonus: 100 points
- **Subtotal**: 0 + 100 + 100 = **200 points**

---

## Skater Point System

### Base Stats
- **Shot (SOG)**: 7.5 points per shot
- **Goal Scored**: 70 points per goal
- **Assist**: 30 points per assist
- **Block**: 5 points per block

### Additional Stats
- **Pass**: 2.5 points per pass
- **Hit**: 2.5 points per hit
- **Takeaway**: 5 points per takeaway
- **Turnover**: -5 points per turnover (penalty)
- **DZ Exit**: 1 point per defensive zone exit
- **OZ Entry**: 1 point per offensive zone entry

### Example Calculation
**Skater with 10 shots, 3 goals, 2 assists, 5 passes, 3 hits, 2 takeaways, 1 turnover, 4 exits, 3 entries:**
- Shots: 10 × 7.5 = 75 points
- Goals: 3 × 70 = 210 points
- Assists: 2 × 30 = 60 points
- Passes: 5 × 2.5 = 12.5 points
- Hits: 3 × 2.5 = 7.5 points
- Takeaways: 2 × 5 = 10 points
- Turnovers: 1 × (-5) = -5 points
- Exits: 4 × 1 = 4 points
- Entries: 3 × 1 = 3 points
- **Subtotal**: 75 + 210 + 60 + 12.5 + 7.5 + 10 - 5 + 4 + 3 = **377 points**

---

## Modifiers

### Team Modifier
- **Winning Team**: 1.1× multiplier (10% bonus)
- **Losing Team**: 1.0× multiplier (no bonus)

### Game-Winning Goal (GWG) Bonus
- **GWG Scorer**: Receives 0.5× multiplier on goal points
  - Goalies: 175 × 0.5 = 87.5 bonus points
  - Skaters: 70 × 0.5 = 35 bonus points

---

## Final Point Calculation

**Final Points = (Base Points + GWG Bonus) × Team Modifier**

### Example: Winning Team Goalie (Shutout)
- Base Points: 200 (from shutout example above)
- GWG Bonus: 0 (no goal scored)
- Team Modifier: 1.1×
- **Final Points**: 200 × 1.1 = **220.0 points**

### Example: Winning Team Skater (3 Goals, GWG)
- Base Points: 285 (10 shots × 7.5 = 75, 3 goals × 70 = 210)
- GWG Bonus: 35 (70 × 0.5)
- Team Modifier: 1.1×
- **Final Points**: (285 + 35) × 1.1 = **352.0 points**

---

## Star Selection

At the end of each game, players are ranked by their final star points:
- **1st Star**: Highest star points
- **2nd Star**: Second highest star points
- **3rd Star**: Third highest star points

---

## Viewing Star Points

Star points are displayed:
- **During Game**: At the bottom of each player's tooltip (hover over player name on scoreboard)
- **After Game**: In the "Stars of the Match" announcement

---

## Quick Reference

### Goalie Points
| Stat | Points |
|------|--------|
| Goal Allowed | -10 |
| Shot Faced | +10 |
| Shutout (0 GA, 1+ shots) | +100 |
| Pass | +2.5 |
| Goal | +175 |
| Assist | +30 |

### Skater Points
| Stat | Points |
|------|--------|
| Shot (SOG) | +7.5 |
| Goal | +70 |
| Assist | +30 |
| Block | +5 |
| Pass | +2.5 |
| Hit | +2.5 |
| Takeaway | +5 |
| Turnover | -5 |
| DZ Exit | +1 |
| OZ Entry | +1 |

### Modifiers
| Condition | Effect |
|-----------|--------|
| Winning Team | ×1.1 (10% bonus) |
| Game-Winning Goal | +50% goal points bonus |

---

*Last Updated: December 26, 2025*

## Recent Changes

### Goalie Point System Updates (December 26, 2025)
- **Save Points**: Removed (saves no longer award points)
- **Shot Faced**: Increased from 1 point to 10 points per shot faced
- **Goal Allowed**: Changed from -40 points to -10 points per goal allowed
- **Shutout Bonus**: Remains 100 points (unchanged)




