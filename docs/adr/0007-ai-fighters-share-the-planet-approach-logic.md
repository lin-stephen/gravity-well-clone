# 0007: AI fighters share the planet approach logic

- Status: Accepted
- Date: 2026-10-24

## Context

Only the player's fighter could claim a planet. AI factions owned a homeworld and a
freighter fleet, but their freighters only build on planets the faction already owns, so
an AI faction never grew. The game has no weapons yet, so the first AI fighter is
peaceful: it claims planets and does nothing else. The design is in
`docs/superpowers/specs/2026-10-24-ai-fighter-design.md`.

Freighters already fly to a planet, align rear-first and dock. A fighter needs the same
manoeuvre at much higher speed (260, against the freighter's 90).

## Decision

- **Where AI fighters live.** `GameWorld.AiFighters` holds them next to `Freighters`, and
  they join `Ships`, so physics, docking, repair and avoidance already see them.
  `GameWorld.Fighters` yields the player first, then the AI fighters. A faction with an
  operational Tech Center, other than the player's, gets exactly one fighter, and no code
  names a faction.
- **One claim rule.** Docking claims a planet, in `ColonizationSystem`, which now looks at
  every fighter. Freighter dispatch is unchanged: it sends a freighter whose faction owns
  the planet, so the faction that claimed it colonizes it.
- **Shared approach logic.** The approach code moved out of `FreighterAutopilot` into
  `PlanetApproach`, parameterised by an `ApproachTuning` record. `FreighterAutopilot` uses
  `ApproachTuning.ForFreighter` (the previous constants, unchanged) and `FighterAutopilot`
  uses `ForFighter`. The fighter flies with the player fighter's own speed, thrust and turn
  limits, not at freighter pace.
- **Fighter tuning.** `ForFighter` raises three values: `CruiseSpeed` (260),
  `RouteWaypointSpeed` (180) and `BrakeDeceleration` (160). `Seek` caps speed at
  `sqrt(BrakeDeceleration * distance)`, which implies braking at half that figure, 80 u/s²
  against 230 thrust. The freighter's 55 waypoint cap is below freighter pace and its 40
  braking value holds a fighter near 150 on typical legs. `FighterApproachTests` pin both
  speed and safe docking.
- **Target selection.** A fighter picks the nearest planet with no owner, measured from its
  current position, and drops the target if anyone claims it first. With nothing to claim
  it holds where it stopped.
- **Contested planets.** Two ships docking side by side at one planet overlap and never
  finish, which left the planet unclaimed forever. The first fighter in `world.Fighters`
  order keeps the planet, the others release it by applying throttle and re-pick, and
  fighters skip planets another healthy fighter is already docking at. The player is first
  in that order, so an AI fighter always yields to the player.
- **Wrecked fighters.** A fighter at zero health holds position and never claims. One
  that was part-way through docking at a free planet releases it. Nothing repairs a ship
  at zero health and there are no replacement fighters, so a wrecked AI fighter stays
  wrecked and that faction stops expanding.

## Consequences

- AI factions expand without the player, and their existing freighters finish the job.
- Any future ship class that needs to fly to and dock at a planet can reuse
  `PlanetApproach` with its own `ApproachTuning`.
- A wrecked AI fighter ends its faction's expansion. This is accepted for the first version
  and should be revisited when weapons and fighter replacement exist.
- A target that cannot be reached is retried indefinitely: the fighter brakes and
  re-plans, as a freighter with no route does. There is no blacklist or give-up timer.
- Crossing traffic at fighter speed passes with little margin. In the test it clears a
  22-unit contact limit by under 4 units. Avoidance is the existing basic controller.
- The end-to-end test copies the system order from `GravityWellGame.Tick` by hand. A shared
  `GameSimulation.Step` in `GravityWell.Core` would remove that drift risk.
- Colonization is still triggered by completed docking, as `ColonizationSystem` already did
  before this change. ADR 0006's touch rule is out of date, but this decision does not
  change that.
