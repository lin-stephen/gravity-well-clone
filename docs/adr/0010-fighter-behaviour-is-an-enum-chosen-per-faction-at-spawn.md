# 0010: Fighter behaviour is an enum chosen per faction at spawn

- Status: Accepted
- Date: 2026-10-24

## Context

[ADR 0008](0008-autopilots-are-strategies-over-a-shared-runner.md) bound a strategy to a ship
class: `FighterAutopilot` built one `PeacefulFighterStrategy` and the strategy itself listed the
fighters to fly. That cannot express factions that behave differently, which is the point of
having strategies at all.

Three ways to give a ship its own behaviour were weighed:

- **A strategy object on the ship.** Rejected. It makes entities depend on the AI layer, against
  [ADR 0004](0004-separate-state-systems-and-renderers.md), and strategies hold a `PlanetApproach`
  that holds `DockingSystem` and `PhysicsSystem`.
- **Keeping `GameWorld` out of the strategies.** Rejected. The decisions need the nearest free
  planet, other fighters docking, planet clearance and traffic, and every other system here takes
  `GameWorld`. A narrower interface would carry the same information and be inconsistent with the
  rest of the code.
- **An enum on the ship, mapped to a strategy by the autopilot.** Chosen.

## Decision

- **`FighterBehavior` is an enum on `Fighter`.** It is `init`-only, so it is fixed when the fighter
  spawns. It is plain data like `Faction`, and it is ignored for the player's fighter. The only
  value today is `Peaceful`.
- **Chosen per faction.** `GameWorld.CreateAiFighters` takes a `Func<Faction, FighterBehavior>`
  that defaults to `Peaceful` for every faction, so game setup can supply the mapping later.
- **The runner takes its ships and its strategy lookup as arguments.** `ShipAutopilot<TShip>` is
  built with a ship source and a function that returns the strategy for a ship. It calls that
  function once, when it first sees a ship, and stores the strategy with the ship's flight plan, so
  a ship keeps its strategy for as long as it stays listed. `IAutopilotStrategy<TShip>` therefore
  loses `ShipsToPilot`, since which ships there are is no longer one strategy's decision. It keeps
  `InitialHoldPosition` and `Decide`.
- **The registry is in the wrapper.** `FighterAutopilot` maps each `FighterBehavior` to a strategy
  and throws `ArgumentOutOfRangeException` for a value with no strategy. A test runs every enum
  value through it, so adding a value without registering a strategy fails.
- **Fighters only.** Freighters keep their single strategy and have no behaviour enum until
  something needs one.

This amends ADR 0008, whose interface had three members and whose runner took one strategy.

## Consequences

- A new fighter behaviour is an enum value, a strategy class and one registration line. The runner
  and the existing strategies don't change.
- Two fighters of one faction always share a behaviour today, because the choice is per faction.
  Choosing per fighter would only need a different function at spawn.
- A behaviour cannot change after spawn, and the runner enforces it: the strategy is stored with the
  plan, so a lookup that later answered differently would be ignored. Allowing a change would be an
  explicit decision to reset the ship's flight plan, because plan state from one behaviour can
  confuse another.
- With one defined enum value, the dispatch cannot be told apart from the default by an outcome.
  Tests cover the plumbing instead: the mapping function is asked for each AI faction and its result
  is assigned (checked with a marker value outside the defined range), the runner uses a different
  strategy per ship and keeps it, and an unregistered value throws.
- Whether different behaviours are different logic or the same logic with other numbers is open.
  The original game's personalities look mostly like parameters, though the mechanics digest only
  lists their names. A second behaviour will show whether a class per behaviour or one
  parameterised strategy fits better.
