# Gravity Well: Game Mechanics Digest

A short summary of what the [original manual](manual-md/table%20of%20contents.md) says about how the game works. Every section links back to its source chapter. Anything the manual does **not** say is listed under [Open questions](#open-questions). Answer those from the screenshots, by playing the original, or by making our own design decision.

## Core loop

1. You start with a fully built **home world**: a base, colony, tech center and comm center on the planet, plus a starbase, space dock and sensor array in orbit, and a freighter. Your fighter starts parked on the starbase. The screenshots' ship list shows this as: Base, Colony, Lab, Comm Center, High Port, Space Dock-5, Sensor Array-5, Freighter.
2. You fly to an unclaimed planet and **land** on it, which claims it.
3. Up to 3 available **freighters** are automatically sent there to build a **Base → Colony → Starbase**, in that order. The extra facilities (Lab/Tech Center, Comm Center, Space Dock, Sensor Array) are then built from finished materials.
4. Meanwhile, you defend new claims, raid enemy freighters, and destroy enemy installations so you can retake their planets.
5. **Win**: claim the whole sector. An opponent is **defeated** when all of its colonies and freighters are destroyed, because it can no longer expand.

Sources: [Getting A Fast Start](manual-md/Getting%20A%20Fast%20Start.md), [Playing The Game](manual-md/Playing%20The%20Game.md)

## Sector & world

- **4 factions**, identified by colour: **Blue, Red, Purple, Yellow**. One of them is the human player.
- A sector has **stars** (the setup option sets how many it will *attempt* to place; 7 would be unusual) and **planets in orbits** around them (the setup option sets the chance of a planet in each orbit).
- Planets **orbit** their stars (an option draws blue orbit rings). **Gravity** pulls ships in.
- **Green planets**: habitable. They can build a Colony, the only source of raw materials. AI factions give undefended green worlds high priority.
- **Gray planets**: too close to the star or on the far edge of the system. They can't have a colony, so they need freighters to keep them supplied. They still get bases, tech centers and starbases, and serve as strategic outposts that add to tech research.

## Flight & landing

- Controls: rotate left/right, forward thrust only (there is no reverse). Acceleration and deceleration take "considerable distance".
- **Landing** on a planet surface or a starbase deck:
  - Your ship must be rotated so that **both thrusters fire against gravity** ("land thruster first like the lunar module").
  - Your speed must be below the **landing speed mark**; the velocity indicator turns bright green when it is.
  - Wrong rotation → the ship takes damage and usually settles into the correct orientation.
  - Too much speed → the ship is **destroyed instantly**.
- Planet landings are easier for beginners. Starbase landings repair you **faster**, because of zero-g.
- Repairs and shield recharges need the facility to have materials in stock.
- Landing on a facility with a Tech Center/Space Dock makes it **your fighter respawn point**.
- **In-flight repair**: match a friendly freighter's heading and speed, then drift slowly through it. This is dangerous: if the freighter changes course, both ships may be destroyed.

Sources: [Playing The Game](manual-md/Playing%20The%20Game.md), [Economic Model](manual-md/Economic%20Model.md)

## Units (9 types)

| Unit | Where | Role |
|------|-------|------|
| **Fighter** | Space | The only unit the player controls directly. **6 structural points.** Claims planets, attacks, defends. Squadron size is set at game start; AI wingmen use the squadron's personality. |
| **Freighter** | Space | Carries raw materials, builds Base/Colony/Starbase (it needs a **full hold** and is **used up** by the build), resupplies outposts. Has 2 single-barrel turrets that can take weapon upgrades, and passes those weapons on to the base or starbase it builds. Freighters show in one neutral colour on radar. |
| **Base** | Planet | Built first; the planet's HQ. Converts raw → finished materials. Has a garrison with **4 weapon emplacements** around the planet. |
| **Colony** | Planet (green only) | The **only producer of raw materials**. |
| **Tech Center** (a.k.a. "Lab") | Planet | Builds fighters and freighters; researches upgrades. More tech centers → better research odds. |
| **Comm Center** | Planet | Feeds the radar; improves garrison beam-laser accuracy. |
| **Starbase** (shown as "High Port" in-game) | Orbit | An orbiting base: converts raw → finished, landing deck, orbital garrison. |
| **Space Dock** | Orbit (on a starbase) | Builds fighters and freighters. |
| **Sensor Array** | Orbit (on a starbase) | Feeds the radar; improves starbase beam-laser accuracy. |

- If **all** Comm Centers and Sensor Arrays are lost, the radar goes blank.
- A constructor builds a new unit only when **no unit of that type is already present** (on the planet or in orbit), and only if it has enough finished materials. Example: as soon as a freighter leaves, a replacement gets built.
- Units under attack go on **Red Alert**: their name turns red in the ship list until your fighter visits them.

Source: [The Machines Of War](manual-md/The%20Machines%20Of%20War.md)

## Economy

Two resources: **raw materials** (made by Colonies, carried by Freighters) and **finished materials** (made from raw by a Base or Starbase, and used for repairs and construction). Everything runs automatically; the player never manages it.

```
Colony ──raw──► Base ──finished──► repairs, Tech Center/Comm Center builds,
   │                               Tech Center builds Fighters/Freighters
   ├──raw──► Starbase ──finished──► repairs, Space Dock/Sensor Array builds,
   │                                Space Dock builds Fighters/Freighters
   └──raw──► Freighter ──raw──► Starbase / colony-less Base elsewhere
                       └─(full hold, consumed)─► new Base / Colony / Starbase
```

Units with no colony on their planet slowly run out of materials, because battle repairs use them up. Freighters won't stay at gray planets; they go back to a green world to reload.

Source: [Economic Model](manual-md/Economic%20Model.md)

## Weapons, shields & upgrades

Tech Centers research upgrades at random; more tech centers give better odds. When one is ready, there's a sound, the tech centers flash their beacons, and the opponent-status box flashes. **Land at a base or starbase that has a tech center** to install it. If a second upgrade finishes before you collect the first, the first goes to another ship or facility instead (non-fighters only accept weapon upgrades). New fighters come with all available upgrades.

| Item | Type | Damage / capacity | Notes |
|------|------|-------------------|-------|
| Cannon (Type I) | Energy gun | 1 | Standard on everything; unlimited ammo |
| Heavy Cannon (Type II) | Energy gun | **2** | |
| Pulse Laser | Energy gun | 1 | Fast yellow bolts, so little lead needed |
| Beam Laser | Energy gun | 1 per target | Red beam that **pierces** and hits several targets; strong against planets |
| Plasma Bolt | Energy gun | 1 | **Ignores shields** |
| Rockets | Secondary (limited ammo) | ? | Unguided; drawn in the faction colour; purple HUD count |
| Guided Missiles | Secondary (limited ammo) | ? | Home in on the nearest enemy (shown on the HUD); turn poorly; white HUD count |
| Planet Buster Bomb | Secondary (limited ammo) | ? (cluster) | Unpowered; explodes on a timer into a cluster that hits ships and structures; yellow HUD count |
| Rapid-Fire | Upgrade | n/a | Fires continuously while the trigger is held |
| Fast Turn | Upgrade | n/a | Faster rotation; green HUD light |
| Ion Shield | Shield | absorbs **8** | White circle; the only shield available at game start |
| Singularity Shield | Shield | absorbs **12** | Cyan shell; **reflects** bullets (with their range reset), turns missiles into rockets, absorbs beams |

If a shield is drained **completely**, its module burns out and you must wait for a new one to be researched. While a shield is merely low, friendly bases and starbases recharge it.

Sources: [Technological Developments](manual-md/technological%20developments.md), [Playing The Game](manual-md/Playing%20The%20Game.md)

## AI personalities

Each squadron gets one personality, which also drives the AI wingmen on the human's team: Aggressive, Berserk, Cautious, **Cooperative** (best wingman), Cowardly, Defensive, Determined, Maniacal, Shrewd, Tenacious, **Trepidatious** (easiest, but its homeworld defends hard), Voracious (hard).

Source: [Game Setup](manual-md/game%20setup.md)

## New-game setup options

Per squadron: Human (yes/no), Personality, starting Weapon, starting Shield. Sector-wide: number of stars, chance of a planet in each orbit, fighters per squadron, pilot name (high scores are kept per name). Every column can be randomized. Menu items: New, Load, Save, Quit, Music (plays random `.mid` files from the game folder), Sound, Speed (11 steps), Options (orbit rings, solid-fill units, solid-fill planets and stars), License, About, Help.

Source: [Game Setup](manual-md/game%20setup.md)

## Controls

| Key | Action | Key | Action |
|-----|--------|-----|--------|
| ← / → | Rotate | ↑ | Thrust |
| ↓ or F | Fire gun | D | Fire rocket/missile/bomb |
| Home | Center on own fighter | End | Default zoom |
| PgUp/R | Zoom in | PgDn/E | Zoom out |
| Ins | Max zoom out | S | Zoom 1:1 |
| F1–F4 | Cycle Blue/Red/Purple/Yellow fighters | Enter / G | Summon all friendly fighters |
| Pause | Pause | Esc | Redraw |
| Ctrl-N / Shift-F2 | New game | Ctrl-O / Shift-F3 | Load |
| Ctrl-S / Shift-F4 | Save | | |

Mouse: click a unit in the view, radar or ship list to select it and follow it with the camera.

Source: [Control Summary](manual-md/Control%20Summary.md)

## HUD layout (left panel, top → bottom)

See [screenshots](screenshots/) and [Playing The Game](manual-md/Playing%20The%20Game.md).

1. **Radar**: the whole sector. Stars = large yellow dots; planets = medium green or gray dots, with a faction-coloured box once claimed; ships = small dots; the selected unit has a white box.
2. **Opponent status**: 4 coloured boxes that turn black when a faction is defeated, and flash when an upgrade is ready.
3. **Status indicators**: clock, weapon type (colour; a split bar means rapid-fire), fast-turn light, secondary ammo count, fighter orientation, shields, and pointers to the nearest star, planet and enemy; a velocity bar with the landing mark; a damage bar (6 segments).
4. **Ship list**: every friendly unit except your fighter. Each entry shows its name (red = Red Alert, white = selected), a damage bar scaled by unit strength, and a materials bar.
5. **Main view** (right): a scrolling, zoomable camera that follows the selected unit. Vector-style outlines, optionally filled.

## Open questions

The manual doesn't specify these. Decide them from the screenshots, by playing the original, or by tuning:

- Gravity strength, how far it reaches, whether it is inverse-square, and whether stars pull as well as planets
- Thrust, top speed, turn rate, landing speed limit
- Range, speed and fire rate of each weapon; secondary-weapon damage and ammo counts
- Hit points of all units other than the fighter; material capacities and production rates
- Odds and timing of research
- Fighter respawn timing, and what happens when the human's last fighter dies
- Whether planets orbit in real time and how fast
