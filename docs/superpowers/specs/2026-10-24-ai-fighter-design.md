# Peaceful AI fighter that claims planets

Date: 2026-10-24
Status: Draft, awaiting approval

## Intent

Today only the player's fighter can claim a planet. AI factions own a homeworld and a
freighter fleet, but their freighters can only expand planets the faction already owns,
so AI factions never grow. Give each AI faction a fighter that finds and claims free
planets, so the freighters that already exist have new planets to colonize.

The game has no weapons yet, so this is a *peaceful* AI: it claims planets and does
nothing else. It must not be written in a way that blocks adding combat behavior later.

## Behavior (decided)

The AI fighter flies with exactly the player fighter's speed, thrust and turn limits. It is
not slowed down to freighter pace.

Every faction that owns a homeworld, other than the player's faction, gets one AI fighter.
The player's faction is already represented by `GameWorld.Player`. Today that makes Red the
only AI fighter, but the rule is data-driven: a faction with a homeworld gets a fighter, and
no code names Red.

Each AI fighter repeats this loop:

1. Pick the nearest planet that is not colonized.
2. Fly to it and dock. Docking claims the planet for the fighter's faction.
3. Once the planet is claimed, undock without being told to and go back to step 1.
4. Freighter colonization needs no change. The planet's owner becomes the fighter's
   faction, and the existing dispatch only sends freighters whose faction equals the owner.
   The claiming faction's own freighters therefore do the colonizing.

Edge cases (decided):

- If the player or another faction claims the target before the fighter docks, the fighter
  drops that target and picks again.
- If no uncolonized planet is left, the fighter holds position, the way idle freighters do.
- A disabled fighter holds position. See "Left to the builder" for what disabled means.

## Goals

- AI factions expand over time with no player involvement.
- The claim rule stays in one place: the fighter docks, and docking claims.
- The new autopilot reuses the proven approach and docking logic rather than copying it.
- Behavior is testable without rendering, using the same simulation style as
  `FreighterAutopilotTests`.

## Non-goals

- Weapons, targeting, chasing or fleeing from the player or other AI ships.
- Multiple fighters per faction, or building replacement fighters.
- Difficulty levels, personalities, or player-facing tuning options for the AI.
- Changes to freighter behavior or to the construction chain.

## Anti-goals (a failure even if it technically works)

- An AI fighter that docks and claims but then sits on the planet forever.
- AI fighters that pass through ships or planets, or damage other ships by ramming.
- The player's controls, reset key, camera or HUD changing in any way.
- A duplicated copy of the freighter approach code that must be kept in sync by hand.

## Constraints from the current code

These are facts about the code as of this commit, found while writing this document.

- `GameWorld` has one `Player` fighter, and `Ships` yields it plus live freighters. AI
  fighters need to be part of the world and part of `Ships`.
- `PhysicsSystem` integrates every ship in `Ships` and only reads each ship's `Controls`.
  The autopilot already works by writing `Controls`, so an AI fighter fits with no
  physics changes. Only `Player` gets keyboard controls and the reset key.
- `DockingSystem` and `RepairSystem` already iterate `Ships`. `CollisionSystem` does not:
  it starts its body list from `world.Player`, so AI fighters would not collide until it
  is changed.
- `ColonizationSystem` only examines `world.Player`. It has to claim a planet when any
  fighter, player or AI, is docked at an unclaimed planet.
- The renderers draw only `world.Player` as a fighter. Freighters have their own renderer.
  AI fighters need to be drawn, in their faction colour, and shown on the radar.
- `Fighter.Reset()` hard-codes the player's start position, and `Fighter` takes no faction
  or position in its constructor (`Ship.Faction` is `init`-only and defaults to Blue).
- `FreighterAutopilot` holds its approach logic as private methods. The recent refactor
  split `Steer` into `BeginApproach`, `SteerTerminal`, `FollowRoute` and `SteerCruise`, and
  its behavior is pinned by `tests/GravityWell.Tests/AI/FreighterAutopilotTests.cs`.
  Those tests must keep passing unchanged.
- `RoutePlanner.Plan` and `IsClear` already accept any `Ship`.
- The game loop order is autopilot, physics, docking, collisions, colonization, freighters,
  economy, repairs, draw (`GravityWellGame.Tick`).
- Fighter limits differ from freighter limits (fighter max speed 260, thrust 230; freighter
  speed 90, thrust 180). The freighter approach constants were tuned for the freighter.

## Calls made by the author (not decided by the user)

These are the author's defaults. Please correct any that are wrong.

- AI fighters are stored on the world in a collection of their own, next to `Freighters`,
  and join `Ships`. `Player` stays a separate property.
- An AI fighter spawns next to its faction's homeworld, facing outward, the way freighters
  do, rather than at the player's start position.
- "Nearest" means nearest to the fighter's current position, measured to the planet centre.
- There is no reservation between AI factions. If two fighters pick the same planet,
  whichever docks first wins and the other re-picks, which the claimed-first rule covers.
- The speed difference is not an AI-only setting. The approach constants (braking distance,
  hover and touchdown speeds, corridor limits) were tuned for a ship with speed 90 and thrust
  180. At 260 they may overshoot or fail the docking speed and alignment checks, so tuning
  them for the fighter is part of this work, not a later pass. They should become parameters
  of the shared logic, with the freighter's values unchanged.
- The shared approach logic is extracted from `FreighterAutopilot` and used by both
  autopilots, rather than the fighter autopilot inheriting from `FreighterAutopilot`.
- After claiming, the fighter undocks by applying throttle, which is how docking already
  releases a ship. `ShipControls` is only `Turn` and `Throttle`, so the AI needs no new
  input path. The claim lands in the same tick the dock completes (docking runs before
  colonization), and the autopilot sees the planet is owned on its next update.
- Collision damage needs no new rules. `CollisionSystem` already damages both bodies in a
  collision, so the only change is including AI fighters in its body list. The "no ramming"
  anti-goal is about the AI's steering, which reuses the freighter's traffic avoidance.
- "Disabled" means `Health <= 0`. Nothing repairs a ship at zero health (`RepairSystem`
  skips it), and there are no replacement fighters, so a wrecked AI fighter holds position
  for the rest of the game. That halts that faction's expansion. It should be rare, because
  the autopilot avoids collisions, and it is accepted for this first version.
- Target selection looks only at `Owner == null`. It does not filter on planet health. There
  is no blacklist or give-up timer: if the nearest planet cannot be reached, the fighter
  brakes and retries, like a freighter with no route. This is a known limitation of v1.
- The default `GameWorld` keeps Blue (player) and Red (AI). Spawning is written for any
  homeworld faction, and one test builds a world with two AI homeworlds to prove it.

## Left to the builder

- Exactly how to share the approach logic (a shared helper type, an interface, or
  parameters on the existing class), provided the freighter tests pass unchanged.
- Where the world builds AI fighters (the default `GameWorld` constructor).
- What "disabled" means. Candidates are zero health or being unable to fly. Check how
  repairs treat zero-health ships and follow that.
- How the fighter autopilot is organized and tested. Its tests must include the approach at
  full fighter speed from several start positions and velocities (like the freighter's
  clear, blocked, drift and bounce scenarios) so a fast fighter that overshoots or fails
  to dock is caught. At minimum also: one planet claimed and the next one chosen, a target
  claimed by someone else, and no planets left.
- The new values for the fighter's approach tuning, found by running those scenarios.
- Rendering details for AI fighters and radar markers, within the existing palette.
- Whether this earns an ADR. `docs/adr/` records decisions of this kind.

## Success

Starting a new game and leaving the player alone, Red's fighter claims the nearest free
planet, Red's freighters then build on it, and the fighter moves on to the next nearest one,
until every planet is claimed. Nothing about the player's own ship, controls or HUD has
changed, and all existing tests still pass.
