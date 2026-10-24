# 0008: Autopilots are strategies over a shared runner

- Status: Accepted
- Date: 2026-10-24

## Context

[ADR 0007](0007-ai-fighters-share-the-planet-approach-logic.md) extracted the planet approach,
align and dock manoeuvre into `PlanetApproach`, which freighters and fighters share. That left
two autopilot classes, `FreighterAutopilot` and `FighterAutopilot`, with the same skeleton: a
`Dictionary<TShip, FlightPlan>`, a get-or-create step, a loop that writes each ship's
`Controls`, and a duplicated hold-speed constant. About 15 lines were shared. What differed was
the decision itself:

- A freighter flies where `FreighterSystem` sends it and otherwise holds.
- A fighter chooses its own target, yields a contested planet, releases a planet that already
  has an owner, and handles wrecks.
- Which ships there are, and whether finished ships are forgotten, also differed.

Combat will make the fighter's decision much larger, and the original game lists a dozen AI
personalities (Aggressive, Cautious, Cooperative and so on), so how a fighter decides is the part
that will vary.

## Decision

- **A strategy for the part that varies.** `IAutopilotStrategy<TShip>` has three members:
  `ShipsToPilot(world)`, `InitialHoldPosition(world, ship)` and
  `Decide(world, ship, plan, deltaTime)`.
- **One runner for the rest.** `ShipAutopilot<TShip>` owns the plan dictionary, creates a plan
  when it first sees a ship, calls `Decide`, and writes the result to `ship.Controls`. It
  forgets the plan of any ship the strategy stops yielding. That replaces the freighter's
  special case for consumed ships with a rule that holds for every ship class.
- **Two strategies today.** `FreighterStrategy` and `PeacefulFighterStrategy` hold the logic
  that used to live in the two autopilot classes, moved verbatim. Both are `internal`.
- **Composition, not a base class.** `FreighterAutopilot` and `FighterAutopilot` stay public,
  with the same constructors, `Update`, `GetPhase` and `GetTarget`. Each is a thin wrapper that
  builds its strategy and a runner. No caller or existing test changed.
- **Smaller cleanups on the way.** `PlanetApproach.HoldAt` and `ApproachTuning.HoldSpeed`
  replace the duplicated hold call and constant, and `FlightPhase` moved to its own file. The
  fighter's hold standoff now reads its own tuning, and `FighterAutopilot` accepts an optional
  `ApproachTuning`.

A base class was rejected. It would have needed about four hooks (ships, pruning, choosing a
target, handling docked or disabled ships) that re-slice two methods that already read top to
bottom, to save about 15 lines.

## Consequences

- A new fighter behaviour, such as an aggressive or cautious one, is a new strategy class. The
  runner and the existing two strategies don't change.
- There are more types than before (an interface, a runner, two strategies and two wrappers,
  against two classes), and one extension point with a single fighter implementation so far.
  This is a bet that fighter behaviour will vary, and it is cheap to unwind if it doesn't.
- Strategy logic can be tested without the runner. The runner has its own tests with a fake
  strategy.
- `ApproachTuning` properties default to zero. A tuning built from scratch rather than copied
  from `ForFreighter` or `ForFighter` would break approach flight, including the new hold
  speed. Marking the properties `required` would catch that at compile time and was left for a
  separate decision.
- The runner copies the strategy's ship list and prunes each tick. At these ship counts the
  cost is negligible.
