# Player cannon: the first weapon

Date: 2026-10-25
Status: Draft, awaiting approval

## Intent

The game has no weapons. The original game's most basic weapon is the Type I Energy Cannon (see
`docs/reference/manual-md/technological developments.md`): standard on every ship and garrison, charged
by the gravity drive so it has unlimited ammunition, and one point of damage per hit. Give the player's
fighter that cannon, so the player can shoot and see what the shot does.

This is the first slice of a weapons system. It proves out the parts every later weapon needs: a
projectile that flies under gravity, hit detection against ships and planets, damage to the things hit,
and visible feedback. It is deliberately limited to the player firing the basic cannon. Destruction is
the next phase.

## Behavior (decided)

**Firing.** The player presses `F` or `Down` (the manual's fire keys) and a bolt leaves the fighter's
nose, travelling along the direction the fighter is facing. There is no aim assist and no auto-targeting.
The player lines up the ship and leads moving targets. The manual's only aim-assisted weapon is the guided
missile, which is out of scope.

**The bolt.** A bolt is a visible object that crosses the screen. It inherits the fighter's velocity and is
bent by planet gravity, the same pull that ships feel. The bolt is therefore harder to aim near planets.

**What a bolt hits.**

| Target | Result |
|--------|--------|
| Enemy fighter | Loses 1 health. Visible hit feedback. |
| Enemy freighter | Loses 1 health. Visible hit feedback. |
| A structure on an enemy planet | That structure loses health. Visible hit feedback. |
| A planet of the shooter's own faction | Absorbs the bolt. No damage. |
| An unclaimed planet, or a planet with no structures | Absorbs the bolt. No damage. |

No bolt ever passes through a planet.

**Friendly fire.** Bolts never hurt the shooter's own faction. This is a setting, off by default, so a
later harder mode can switch it on. The setting follows the existing settings pattern (see the
`CollisionSettings` and `PhysicsSettings` records).

**Planets and structures.** Each structure has its own health. The planet's resilience is not a stored
number: it comes from structures physically getting in the way of a bolt, so a planet with more structures,
placed around its centre, shelters its inner structures.

- The planet is drawn as an outline, so a bolt can visibly fly into the circle and hit a structure glyph.
  A bolt hits whichever structure it reaches first.
- The planet circle is a one-way door for bolts. A bolt that enters it keeps flying until it hits a
  structure, flies out the far side (where it is absorbed), or runs out its lifetime inside (it just
  disappears). It is not guaranteed to reach the centre, because it may enter off-centre, be bent by gravity
  or time out.
- A planet that cannot be damaged by this shooter (own planet, unclaimed, or no standing structures)
  absorbs the bolt at the edge on entry.
- The Starbase, Space Dock and Sensor Array are drawn above the planet, outside the circle. A bolt from
  above can hit them before it ever reaches the disc. They shield what is below them.
- A structure whose health reaches 0 becomes `Destroyed`. It no longer obstructs bolts and no longer counts
  as standing (`HasStructure` and `HasOperationalStructure` already ignore destroyed structures).
- There is no special case for the planet the shooter is docked on. The nose points outward when docked, so
  the bolt starts outside the circle. In an odd "drilling" pose where the nose is inside the circle, the bolt
  starts inside. On an enemy planet with a standing structure it can hit a structure or is absorbed at the far
  edge. On any other planet it is absorbed at once, because that planet absorbs bolts on entry.

**Ships keep their current limits in this phase.** A fighter or freighter can be reduced to 0 health but
nothing destroys, removes or disables it. Ship destruction is the next phase, for fighters and freighters
alike. AI fighters stay passive and do not fire.

## Goals

- Pressing fire visibly launches a bolt from the nose, and the bolt's path visibly bends near planets.
- Hitting an enemy fighter, freighter or planet structure gives clear visible feedback and reduces health.
- Which structure is hit is determined by where the bolt flies, using the positions the player sees.
- The rules for who can be hurt live in one place and the friendly-fire switch changes them.
- All of the simulation is testable without a graphics context, in the style of the existing Core tests.

## Non-goals

- Any weapon other than the Type I cannon: heavy cannon, lasers, plasma, rockets, missiles, bombs, rapid-fire.
- Shields and the fast-turn upgrade, or any tech-centre research.
- Ammunition or reloading: the cannon has unlimited ammo.
- AI fighters firing, targeting or fleeing. Garrison and freighter turrets firing.
- Destruction of ships, ship wrecks or respawn, and any change to what a zero-health ship does.
- Sound, and changes to the weapon-type HUD indicator.
- Rewarding the player for damage (score, kills).
- Recoil, heat or any other cost to firing.

## Anti-goals (a failure even if it technically works)

- Bolts that skip over a thin target between frames.
- A bolt that can leave a planet's far side. Shooting through a planet is the one explicit "never".
- The picture and the hit test disagree: a bolt visibly overlaps a structure glyph and misses, or hits where
  nothing is drawn.
- Friendly fire switched on by default, or hardcoded so it cannot be configured.
- The player's existing controls, camera, reset key or HUD changing in any way.
- Hit feedback so faint that the player cannot tell a hit from a miss.
- Weapon logic in `Application` or inside the renderers. Renderers draw only state (ADR 0004).

## Constraints from the current code

These are facts about the code at commit `c0bdd75`, found while writing this document.

- `ShipControls` is only `Turn` and `Throttle`. `PlayerControls` has no fire flag, and `InputSystem` reads
  raylib keys into it. `Down` is currently unused. `F` is not bound.
- `Ship.Forward` gives the facing direction. The renderer draws the fighter nose 13 units ahead of its centre,
  so the bolt should start there, not at the centre.
- `Ship.Health` is an `int`. Fighter and freighter have 6, collision damage already subtracts from it, and the
  HUD shows "n/6 HP". The radar colours fighters by health, so a hit is already visible in the radar.
- `PhysicsSystem.GetGravity(world, position)` is public and gives the acceleration at any point. Bolts can
  reuse it instead of copying the gravity maths. It clamps distance near a planet centre.
- Planets have `Planet.Health` (100) and it is changed by nothing today. `Planet` is an `ICollisionBody` and
  `SpaceDockBody` forwards its `Health` to the planet. `RepairSystem` refuses to repair at a planet with
  `Health <= 0`. A per-structure health model has to decide what happens to this number.
- `PlanetStructure` has `Type`, `State` and `ConstructionProgress`. `State` has a private setter and nothing sets
  `Destroyed`. There is no health on a structure.
- Structure positions exist only in `StructureRenderer.GetPosition` (private): Base at the planet centre,
  Colony `(0, +19)`, Tech Center `(-17, -15)`, Comm Center `(14, -11)`, and Starbase, Space Dock and Sensor
  Array sharing one anchor at `(0, -54)`. Glyph sizes are in `DrawGlyph`. The simulation cannot see any of this
  today. The Starbase, Space Dock and Sensor Array share an anchor, so they need separate hit shapes to be
  told apart.
- The planet radius is 34, the fighter radius 10 and the freighter radius 12.
- ADR 0009: the Base guards a planet from takeover. `Planet.HasStructure(Base)` ignores a destroyed one.
  When a Base is destroyed the planet loses its guard. Nothing could destroy one before this work.
- `GameWorld.Ships` yields the player, AI fighters and live freighters. Consumed freighters are not in it.
- `GravityWellGame.Tick` runs: autopilots, physics, docking, collisions, colonization, freighters, economy,
  repairs, draw. Systems are separate classes that take the world and `deltaTime`.
- Rendering draws world objects in `GameRenderer.DrawWorld` inside the world camera.
- Tests: NUnit in `tests/GravityWell.Tests`, one folder per Core area, with a deterministic flight simulation
  project beside it.

## Calls made by the author (not decided by the user)

These are the author's defaults. Please correct any that are wrong.

- **Numbers.** The manual gives damage (1) but not the bolt's speed, lifetime or cooldown. The builder picks
  starting values, expresses them as settings, and tunes them by playing. The only constraint is that the bolt
  is clearly faster than the fastest ship (260) so it can catch a moving target.
- **Fire rate.** One bolt per key press with a minimum interval between shots. Holding the key does not
  repeat. Rapid-fire is a separate upgrade in the manual.
- **Friendly ships.** With friendly fire off, a bolt flies through friendly ships, so a squadron or freighter
  cannot shelter the shooter or block a shot. Friendly planets still absorb. With friendly fire on, ships and
  structures are hurt like enemy ones.
- **What a ship hit looks like.** Bolt damage is 1 point on any ship, the same as the existing health scale.
  Health is clamped at 0 and nothing else happens at 0.
- **Which structures can be hit.** Structures that are `Building` or `Operational`. A `Planned` structure is
  not built yet and is not a target.
- **Structure health.** Each structure type has a health value in a table, a single starting value for all
  types if the builder prefers. The numbers are placeholders to tune.
- **Planet health is left alone.** `Planet.Health` (100) is already changed by ship-planet collisions and read
  by `RepairSystem` and `SpaceDockBody`. Bolts do not touch it. Structure health is a separate, new value on
  each structure, and the planet's resilience comes only from obstruction. No existing rule changes.
- **The drawn picture does not change.** Moving structure positions and sizes into Core is a pure extraction.
  The renderer reads them from Core and draws exactly what it draws today. The hit shapes follow the drawn
  glyphs. The Starbase, Space Dock and Sensor Array share an anchor and their glyphs can overlap. Where they
  do, the bolt hits whichever one it reaches first.
- **Faction.** Every ship has a faction. A planet's owner is a faction or none, and there is no neutral
  faction. "Enemy" means a different faction. An unowned planet is never an enemy and only absorbs bolts.
- **Structure state and damage.** A `Building` or `Operational` structure takes damage, and a hit does not
  change `ConstructionProgress`. Health starts full when a structure is created. A structure that is rebuilt
  after destruction is a new structure with full health.
- **Ownership after destruction.** Destroying structures never changes the owner, because only `Claim` changes
  it. A destroyed Base removes the takeover guard (ADR 0009), so the planet can then be taken by a fighter
  docking on it. This is accepted.
- **Friendly outer structures.** With friendly fire off, a friendly structure outside the circle (the
  Starbase and its attachments) absorbs the bolt without damage when the bolt reaches it, the same as the
  friendly planet circle does.
- **Destroyed structures.** Existing rules apply unchanged. A destroyed Base removes the takeover guard
  (ADR 0009). A destroyed structure can be rebuilt by freighters, because `HasStructure` ignores it. This is
  accepted for now and is not designed here.
- **Docked and overlapping ships.** A docked ship can be hit like any other. A bolt never hits the ship that
  fired it.
- **Bolts do not interact with each other.**
- **Hit feedback.** A short visual effect at the point of impact (a flash or small burst) in a colour that
  stands out from the faction colours, plus the damage already shown by the HUD and radar. The exact look is
  the builder's call, within the existing palette.
- **Per-frame hit test.** The bolt's movement over a frame is tested as a line segment, not just its end
  point, so a fast bolt cannot skip a thin target.
- **Bolt cap.** There is a limit on the number of bolts alive at once, high enough not to be noticed.

## Left to the builder

- The data model for bolts, and where they live in `GameWorld`.
- How the planet's structure layout moves into Core so the simulation and `StructureRenderer` share it. The
  renderer must read the same positions the simulation tests against.
- The system structure: a new system class and where it runs in `GravityWellGame.Tick`, so that bolts move
  after ships and hits use post-move positions.
- How hits are reported to the renderer, so feedback is drawn from state or events, not from gameplay logic
  in the renderer.
- Exact hit shapes for each structure glyph, within the sizes drawn now.
- The settings record, its defaults and validation, in the style of `CollisionSettings`.
- Bolt rendering: colour, length and trail, within the existing palette.
- Tests. At minimum: a bolt starts at the nose and moves along the facing direction with the fighter's
  velocity added; gravity bends its path; it hits an enemy fighter and a freighter and costs 1 health; it flies
  through friendly ships and is absorbed by friendly planets with friendly fire off; and the same setup hurts
  them with it on; it is absorbed by an unclaimed planet and by a planet with no standing structures; it hits
  the first structure along its path and a shielded structure survives; a destroyed structure stops obstructing
  and stops counting as standing; a bolt entering off-centre is absorbed at the far edge; a fast bolt cannot
  tunnel through a fighter; a bolt that outlives its lifetime disappears.
- Whether this earns an ADR. `docs/adr/` records decisions of this kind.

## Success

Starting a new game, the player flies out, presses `F`, and sees a bolt leave the nose and curve under planet
gravity. Shooting a Red freighter or fighter drops its health by one and flashes at the impact point. Shooting
Red's homeworld from different directions hits different structures, and a structure at 0 health turns to its
destroyed colour and stops blocking bolts. Shooting the player's own planet or an unclaimed planet just
absorbs the bolt. No bolt ever comes out of the far side of a planet. All existing tests still pass and the
player's controls, camera and HUD are unchanged apart from the added fire keys.
