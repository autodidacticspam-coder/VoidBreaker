# VOIDBREAKER - Game Design Document
## Version 1.0

---

# TABLE OF CONTENTS
1. [Game Overview](#1-game-overview)
2. [Core Gameplay Loop](#2-core-gameplay-loop)
3. [Ship Systems](#3-ship-systems)
4. [Evolution System](#4-evolution-system)
5. [Crew System](#5-crew-system)
6. [Combat System](#6-combat-system)
7. [Sector & Event System](#7-sector--event-system)
8. [Progression Systems](#8-progression-systems)
9. [Final Encounters](#9-final-encounters)
10. [UI/UX Design](#10-uiux-design)

---

# 1. GAME OVERVIEW

## 1.1 High Concept
VoidBreaker is a roguelike space survival game where your ship physically evolves based on your playstyle, featuring pausable real-time combat, deep crew management, and multiple endings that validate different build paths.

## 1.2 Core Fantasy
*"I am the captain of a living ship, making impossible choices as we flee through a dying galaxy. Every battle changes us. Every sacrifice leaves a mark. By journey's end, we are transformed."*

## 1.3 Key Differentiators from FTL
| FTL | VoidBreaker |
|-----|-------------|
| Static ship appearance | Ship visually evolves |
| Single final boss | Three path-specific endings |
| Heavy RNG | Drafting + Pity Timers + Meta-progression |
| Crew are stat blocks | Crew have relationships |
| Builds can be "bricked" | Every path is viable |

---

# 2. CORE GAMEPLAY LOOP

```
┌─────────────────────────────────────────────────────────┐
│                    SECTOR MAP                           │
│  [Choose beacon] → [Jump] → [Encounter]                │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    ENCOUNTER                            │
│  Combat / Event / Shop / Distress / Empty              │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    REWARDS                              │
│  [Draft 1 of 3] → Scrap + Resources + Mutation Points  │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    SHIP MANAGEMENT                      │
│  Upgrade systems / Evolve ship / Manage crew           │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
        [Hegemony fleet advances] → Return to Sector Map
```

## 2.1 Session Structure
- **8 Sectors** to traverse
- **15-25 beacons** per sector
- **Exit beacon** leads to next sector
- **Hegemony pursuit** - fleet advances each jump, caught = dangerous fight
- **Average run:** 45-90 minutes

---

# 3. SHIP SYSTEMS

## 3.1 Core Systems (Require Power)

| System | Function | Max Level | Crew Bonus |
|--------|----------|-----------|------------|
| **Reactor** | Generates power | 8 | N/A |
| **Engines** | Evasion + FTL charge | 8 | +5% evasion per skill |
| **Shields** | Absorb damage layers | 8 (4 bubbles) | +10% recharge |
| **Weapons** | Powers weapon slots | 8 | +10% charge speed |
| **Piloting** | Base evasion | 3 | Required for evasion |
| **Sensors** | Enemy ship vision | 3 | See crew HP |
| **Doors** | Breach/fire control | 3 | Auto-close speed |
| **Medbay** | Heal crew | 3 | +25% heal rate |
| **Oxygen** | Life support | 3 | Regen rate |

## 3.2 Advanced Systems (Unlockable)

| System | Function | Evolution Path |
|--------|----------|----------------|
| **Cloak** | Temporary invisibility | Phantom |
| **Teleporter** | Crew boarding | Predator |
| **Drone Control** | Deploy drones | Any |
| **Hacking** | Disable enemy systems | Phantom |
| **Mind Control** | Turn enemy crew | Predator |
| **Comms Array** | Diplomacy options | Herald |
| **Regeneration Bay** | Clone dead crew | Herald |

## 3.3 Power Management
- Total reactor power distributed across systems
- Systems can be partially powered
- **Zoltan Crew:** Provide +1 power to manned system
- **Overcharge (NEW):** Spend mutation points for temporary +2 power burst

## 3.4 Room Layout
Ships have 12-20 rooms arranged in connected grid. Crew must walk between rooms. Breaches/fires spread to adjacent rooms.

---

# 4. EVOLUTION SYSTEM

## 4.1 Mutation Points
Earned from:
- Combat victories: 2-5 MP
- Event choices: 1-3 MP
- Crew milestones: 2 MP
- Boss defeats: 10 MP

## 4.2 Evolution Paths

### PREDATOR PATH (Combat)
*"We became the hunter."*

| Tier | Cost | Mutation | Effect |
|------|------|----------|--------|
| 1 | 10 MP | Reinforced Hull | +15 max hull |
| 1 | 10 MP | Weapon Ports | +1 weapon slot |
| 2 | 25 MP | Bio-Cannons | Weapons deal +1 damage |
| 2 | 25 MP | Armored Core | -1 damage from all sources |
| 3 | 50 MP | Predator's Maw | Ram enemy ships (boarding + damage) |
| 3 | 50 MP | Blood Frenzy | Kills grant temporary damage boost |
| 4 | 100 MP | APEX PREDATOR | Unlock Dreadnought boss, +25% damage |

**Visual Changes:** Ship grows spines, weapons fuse with hull, aggressive red/black coloring, forward-facing "jaws"

### PHANTOM PATH (Stealth)
*"We became the shadow."*

| Tier | Cost | Mutation | Effect |
|------|------|----------|--------|
| 1 | 10 MP | Silent Running | +10% evasion |
| 1 | 10 MP | Sensor Dampening | Enemies detect later |
| 2 | 25 MP | Phase Plating | 20% chance to phase through damage |
| 2 | 25 MP | Ghost Crew | Crew move faster, quieter boarding |
| 3 | 50 MP | Void Cloak | Cloak lasts 50% longer |
| 3 | 50 MP | System Vampire | Hacking drains power to you |
| 4 | 100 MP | PHANTOM COMPLETE | Unlock Station boss, start cloaked |

**Visual Changes:** Ship becomes angular, darker, matte black with blue accents, hard to see against space

### HERALD PATH (Diplomacy)
*"We became the voice."*

| Tier | Cost | Mutation | Effect |
|------|------|----------|--------|
| 1 | 10 MP | Broadcast Array | +1 diplomacy option per event |
| 1 | 10 MP | Welcoming Bays | +1 max crew |
| 2 | 25 MP | Empathic Field | Crew relationships improve faster |
| 2 | 25 MP | Trade Beacon | Better shop prices |
| 3 | 50 MP | Sanctuary | Some enemies won't attack |
| 3 | 50 MP | United Crew | Crew bonuses stack |
| 4 | 100 MP | HERALD ASCENDANT | Unlock Council boss, start with ally |

**Visual Changes:** Ship grows elegant, communication arrays bloom like flowers, white/gold coloring, inviting design

## 4.3 Hybrid Builds
Players can mix paths but reaching Tier 4 (100 MP unlock) requires 85+ MP in one path. Encourages specialization while allowing dabbling.

## 4.4 Visual Evolution
Ship model transitions through 5 visual stages per path:
- **Stage 0:** Base ship
- **Stage 1:** Subtle changes (10 MP spent in path)
- **Stage 2:** Noticeable transformation (35 MP)
- **Stage 3:** Dramatic appearance (85 MP)
- **Stage 4:** Final form (185 MP - full path)

---

# 5. CREW SYSTEM

## 5.1 Crew Races

| Race | HP | Special | Weakness |
|------|-----|---------|----------|
| **Human** | 100 | Fast skill learning | None |
| **Vex** (Mantis) | 100 | +50% combat damage | -50% repair |
| **Crystalline** | 125 | Immune to suffocation | Slow movement |
| **Zoltan** | 70 | +1 power when manning | Fragile |
| **Engi** | 100 | +50% repair speed | -50% combat |
| **Slug** | 100 | Telepathy (see adjacent rooms) | None |
| **Lanius** | 100 | Drains oxygen, immune to suffocation | Damages ally oxygen |
| **Synthetic** | 80 | Immune to mind control, no oxygen | Can't heal in medbay |

## 5.2 Crew Skills
Each skill has 3 levels (Novice → Trained → Expert)

| Skill | Effect per Level | Trained By |
|-------|------------------|------------|
| Piloting | +5% evasion | Dodging shots |
| Engines | +5% evasion, +10% FTL | Dodging shots |
| Shields | +10% recharge speed | Taking hits |
| Weapons | +10% charge speed | Firing weapons |
| Repair | +10% repair speed | Repairing |
| Combat | +10% damage dealt | Fighting |

## 5.3 Crew Relationships (NEW)
Crew develop bonds through shared experiences:

**Relationship Levels:**
- **Strangers** (0-20) - No bonus
- **Acquaintances** (21-50) - +5% efficiency when in same room
- **Friends** (51-80) - +10% efficiency, morale boost when together
- **Bonded** (81-100) - +15% efficiency, will rescue each other

**Relationship Events:**
- Surviving combat together: +5 bond
- One saves another from fire/breach: +15 bond
- One crew member dies: -20 morale to bonded crew
- Conflicting personalities: Can generate rivalry (-10 efficiency together)

## 5.4 Morale System (NEW)
Ship-wide morale affects performance:

| Morale Level | Effect |
|--------------|--------|
| Desperate (0-20) | -15% all stats, desertion risk |
| Low (21-40) | -10% all stats |
| Normal (41-60) | No modifier |
| High (61-80) | +5% all stats |
| Inspired (81-100) | +10% all stats, bonus events |

**Morale Modifiers:**
- Victory: +5
- Defeat (survived): -10
- Crew death: -15
- Good event outcome: +3
- Medbay/rest: +2 per jump
- Herald mutations: Various bonuses

---

# 6. COMBAT SYSTEM

## 6.1 Combat Flow
1. **Encounter begins** - Ships face off
2. **Real-time with pause** - Player can pause anytime
3. **Assign targets** - Weapons target enemy rooms
4. **Manage power** - Route power as needed
5. **Crew commands** - Position crew for repair/combat/manning
6. **Resolution** - Enemy destroyed, fled, or player defeated

## 6.2 Weapon Types

| Category | Examples | Characteristics |
|----------|----------|-----------------|
| **Lasers** | Burst Laser, Heavy Laser | Fast charge, blocked by shields |
| **Missiles** | Artemis, Breach | Ignore shields, limited ammo |
| **Beams** | Pike Beam, Halberd | Sweep damage, need shields down |
| **Ions** | Ion Blast, Ion Bomb | Disable systems temporarily |
| **Bombs** | Fire Bomb, Breach Bomb | Teleport to enemy ship |
| **Flak** | Flak I, II | Shotgun spread, shield breaker |

## 6.3 Damage System
- **Hull Points:** Ship destroyed at 0
- **System Damage:** Rooms can be damaged (3 bars each)
- **Breaches:** Drain oxygen, must be repaired
- **Fires:** Spread, damage crew and systems

## 6.4 Evasion Formula
```
Evasion = (Engine Level × 5) + Pilot Skill Bonus + Engine Skill Bonus + Mutations
```
Capped at 55% (can exceed with cloak)

## 6.5 Shield Mechanics
- Each shield bubble blocks one projectile
- Shields recharge when not being hit
- Beam weapons deal reduced damage based on shields
- Missiles bypass shields entirely

## 6.6 Retreat Mechanics
- FTL charges over ~20 seconds (modified by engines)
- Can jump away from combat
- Hegemony pursuit makes retreat costly (they gain ground)

---

# 7. SECTOR & EVENT SYSTEM

## 7.1 Sector Types

| Sector | Characteristics | Frequency |
|--------|-----------------|-----------|
| **Civilian** | Safe, good shops, events | Common early |
| **Hostile** | More combat, better loot | Common mid |
| **Nebula** | Sensors disabled, special events | Uncommon |
| **Abandoned** | Risk/reward, salvage | Uncommon |
| **Hegemony** | Enemy territory, hard combat | Late game |
| **The Breach** | Final sector, path-dependent | Always Sector 8 |

## 7.2 Beacon Types (Shown on Map)

| Icon | Type | Player Knows |
|------|------|--------------|
| ❓ | Unknown | Nothing - could be anything |
| ⚔️ | Combat | Guaranteed fight |
| 🏪 | Shop | Can buy/sell |
| 🆘 | Distress | Event (risk/reward) |
| ⭐ | Quest | Multi-stage reward |
| 🚪 | Exit | To next sector |
| ⚠️ | Danger | Hard fight, good loot |

## 7.3 Event Structure
Events present choices with outcomes. NEW: **Drafting applies to events!**

**Example Event:**
```
DISTRESS SIGNAL

A damaged merchant vessel broadcasts a plea for help.
Your sensors detect life signs... and something else.

[PREDATOR] Board and take what you need
  → Guaranteed loot, possible combat, -5 Herald reputation

[PHANTOM] Scan silently and assess
  → Reveal trap OR find hidden cache, no risk

[HERALD] Respond and offer assistance
  → Possible crew recruit, trade opportunity, +5 reputation

[IGNORE] Continue on your path
  → Nothing happens
```

## 7.4 Pity Timer System
To prevent "unwinnable" runs:

| Milestone | If Not Met By | Guarantee |
|-----------|---------------|-----------|
| First weapon | Sector 2 | Shop has affordable weapon |
| Shield upgrade | Sector 3 | Event grants shield component |
| Crew member | Sector 2 | Distress beacon has survivor |
| Path mutation | Sector 4 | Free Tier 1 mutation choice |

## 7.5 Pursuit Mechanic
- Hegemony fleet occupies beacons behind you
- Caught by fleet = dangerous ASB (Anti-Ship Battery) fight
- Exit beacon is pushed by fleet over time
- Creates tension between exploration and escape

---

# 8. PROGRESSION SYSTEMS

## 8.1 Run Rewards (Draft System)
After each combat/event, choose 1 of 3:
- **Scrap** (currency)
- **Weapon/Drone**
- **Augment**
- **Crew member**
- **Mutation points**
- **Resources** (fuel, missiles, drone parts)

Draft pool is weighted by:
- Current needs (low fuel = fuel more likely)
- Evolution path (Predator sees more weapons)
- Sector type
- Difficulty

## 8.2 Meta Progression: Gene Memories
Unlocked by achievements, provide starting bonuses:

| Memory | Unlock Condition | Bonus |
|--------|------------------|-------|
| Prepared | Win a run | Start with +25 scrap |
| Veteran Crew | Win with 6+ crew | Start with 1 trained crew |
| Weapons Expert | Deal 500+ damage in run | Start with random weapon |
| Evolved | Reach Tier 4 evolution | Start with 10 MP |
| Survivor | Win on Hard | +5 starting hull |
| Diplomat | Win via Herald path | +1 diplomacy option |
| Hunter | Win via Predator path | +10% damage for sector 1 |
| Ghost | Win via Phantom path | +10% evasion for sector 1 |

## 8.3 Ship Unlocks
8 ships total, each with 3 layouts (A, B, C):

1. **The Vagrant** (Starter) - Balanced
2. **The Fang** - Predator-focused, strong weapons
3. **The Whisper** - Phantom-focused, starts with cloak
4. **The Beacon** - Herald-focused, extra crew
5. **The Survivor** - High hull, weak weapons
6. **The Glass Cannon** - Powerful but fragile
7. **The Swarm** - Drone-focused
8. **The Hybrid** - Flexible evolution bonuses

---

# 9. FINAL ENCOUNTERS

## 9.1 Predator Ending: THE DREADNOUGHT
*"The Hegemony's ultimate warship. Destroy it, and their fleet crumbles."*

**Three Phases:**
1. **Phase 1:** Standard boss fight, AI learns your tactics
2. **Phase 2:** Dreadnought calls reinforcements, fight 2v1
3. **Phase 3:** Desperate ramming attack, board and destroy from within

**Victory Condition:** Destroy all 4 weapon systems then core
**Predator Bonus:** +25% damage, ram ability available

## 9.2 Phantom Ending: THE STATION
*"The Hegemony Command Station. Infiltrate it, destroy their coordination."*

**Three Phases:**
1. **Phase 1:** Stealth approach, avoid detection
2. **Phase 2:** Internal sabotage, disable defenses
3. **Phase 3:** Escape as station explodes

**Victory Condition:** Complete objectives without being caught, or fight through alarms
**Phantom Bonus:** Start cloaked, detection threshold +50%

## 9.3 Herald Ending: THE COUNCIL
*"The Hegemony Council. Convince them. Divide them. End this war without more death."*

**Three Phases:**
1. **Phase 1:** Opening arguments, build support
2. **Phase 2:** Counter opposition, expose corruption
3. **Phase 3:** Final vote OR emergency combat if failed

**Victory Condition:** Win council vote through dialogue/evidence
**Herald Bonus:** Start with ally council member, bonus dialogue options

---

# 10. UI/UX DESIGN

## 10.1 Main Screens
- **Title Screen** - New game, continue, settings, ship select
- **Sector Map** - Node-based navigation
- **Ship View** - Manage systems, crew, evolution
- **Combat View** - Real-time tactical combat
- **Event Screen** - Narrative choices
- **Shop Screen** - Buy/sell interface
- **Pause Menu** - Save & quit, settings

## 10.2 HUD Elements (Combat)
```
┌─────────────────────────────────────────────────────────────┐
│ [HULL: ████████░░ 24/30]  [EVASION: 35%]  [SECTOR: 4/8]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│     ┌─────────────────┐      ┌─────────────────┐           │
│     │   PLAYER SHIP   │      │   ENEMY SHIP    │           │
│     │   [Rooms/Crew]  │      │   [Rooms/Crew]  │           │
│     └─────────────────┘      └─────────────────┘           │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│ [WEAPONS]  [DRONES]  [CREW]  [SYSTEMS]    [PAUSE] [JUMP]  │
└─────────────────────────────────────────────────────────────┘
```

## 10.3 Accessibility Features
- Pause button always visible
- Color-blind modes
- Adjustable game speed
- Detailed tooltips
- Tutorial overlay (toggleable)

---

# APPENDIX A: BALANCE TARGETS

| Metric | Target |
|--------|--------|
| Win rate (Normal) | 15-20% |
| Win rate (Hard) | 5-10% |
| Average run length | 45-90 minutes |
| Runs before first win | 5-15 |
| "Felt fair" percentage | 85%+ |
| Path viability | All three within 5% win rate |

---

# APPENDIX B: CONTENT COUNTS

| Content Type | Count |
|--------------|-------|
| Ships | 8 (24 layouts) |
| Weapons | 40+ |
| Drones | 15+ |
| Augments | 30+ |
| Events | 200+ |
| Crew Races | 8 |
| Mutations | 21 (7 per path) |
| Sectors | 8 types |
| Final Bosses | 3 |

---

*Document Version 1.0 - Synthetic Studio*
*Lead Designer: The Visionary*
