# Gravity Well clone

[![Tests](https://github.com/lin-stephen/gravity-well-clone/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/lin-stephen/gravity-well-clone/actions/workflows/tests.yml)
[![Core coverage](https://codecov.io/gh/lin-stephen/gravity-well-clone/branch/main/graph/badge.svg)](https://app.codecov.io/gh/lin-stephen/gravity-well-clone)

A C# WebAssembly recreation of the 1995 space strategy/action game, rendered with
[raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Project structure

- `GravityWell.Core/`: platform-independent simulation, state, and control types.
- `GravityWell/`: WebAssembly app, raylib input, rendering, and browser assets.
- `tests/`: NUnit tests and deterministic flight simulation checks, referencing `GravityWell.Core`.

Run the NUnit tests with `dotnet test tests/GravityWell.Tests/GravityWell.Tests.csproj`.

## Tests and coverage

The Tests workflow runs NUnit tests and flight simulation checks on pull requests
and pushes to `main`. Unit-test coverage measures `GravityWell.Core` only;
rendering and the separate flight simulation checks are not included. Line and
branch coverage appear in the workflow summary, and the `test-results` artifact
contains the TRX test results and Cobertura coverage report.

Generate the same coverage report locally:

```sh
dotnet test tests/GravityWell.Tests/GravityWell.Tests.csproj --configuration Release --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
```

Codecov provides the coverage badge and pull request coverage feedback. To enable
it, sign in to [Codecov](https://app.codecov.io/gh/lin-stephen/gravity-well-clone)
with GitHub and enable this repository, granting the Codecov GitHub App access
if prompted. Uploads authenticate using GitHub Actions OIDC, so no
`CODECOV_TOKEN` secret is needed. Coverage is informational, with no minimum
percentage enforced; Codecov upload failures do not fail the Tests workflow.
Fork pull requests still run tests and generate coverage artifacts and summaries,
but skip the OIDC upload. The coverage badge populates after the first successful
upload from `main`.

## Run locally

Requires the .NET 10 SDK and the `wasm-tools` workload.

```sh
dotnet workload install wasm-tools
dotnet run --project GravityWell/GravityWell.csproj
```

Use Left/Right to rotate, Up to thrust, and Home to reset the ship. The initial
prototype models gravity from a star and planet.

## Publish

```sh
dotnet publish GravityWell/GravityWell.csproj -c Release -o publish
```

The static site is written to `publish/wwwroot`. Pushes to `main` deploy that
directory with the GitHub Pages workflow. In the repository settings, set Pages
to use **GitHub Actions** as its source.

Original-game research lives under [`docs/reference`](docs/reference/README.md).

## Collision damage

Fighters, freighters, and planets use circular collision bounds. On first contact,
both bodies lose `ceil((closing speed - 100) / 25)` health if closing speed is
above 100; slower contacts are harmless. Closing speed is the relative velocity
projected toward the other body along the contact normal. Sideways motion does
not count, and separating bodies take no damage. Damage applies once per contact;
separating and touching again allows another hit. Health is clamped to zero.
The shared tuning constants live in `GravityWell.Core/Physics/CollisionSystem.cs`.

Ships bounce with equal mass and restitution 0.8; planets remain fixed. Contact
resolution separates overlaps and changes only inward velocity along the contact
normal, preserving sideways motion. Freighter autopilot recovers using the
resulting physical velocity. Damage uses impact velocity before the bounce.

Destruction is not yet implemented. Detection uses positions each frame, so
sufficiently fast bodies can pass through each other between frames.

## Landing and docking

Point the fighter's rear toward a planet or an operational spacedock and approach
below 60 speed units, within 30 degrees of rear-first alignment. Within 4 units
of contact, docking gently damps motion, rotates the ship perpendicular to the
surface with its rear facing inward, and settles it against the target,
without collision damage or bounce from that target. Planets can be landed on
regardless of ownership; a completed landing claims an unowned planet, or takes one
owned by another faction that has no base. A base, even one still being built, guards a
planet from takeover. Spacedock
bounds follow the dock glyph near the orbital starbase. Thrust releases the ship;
it must leave the capture zone before docking to that target again. Home resets
all fighter docking state. Docking does not interpret ship or target health;
destruction and respawning belong to a separate lifecycle system. Tune capture
and damping in `GravityWell.Core/Physics/DockingSystem.cs`.

Planet outlines and operational spacedock glyphs use thicker lines while the
fighter's approach speed toward that particular target is below the docking
threshold and the target lies within 30 degrees of the ship's rear direction.
Sideways motion does not count, and receding motion has zero approach
speed. Docking still requires safe total relative speed, rear-first alignment,
and proximity.

## Ship autopilot

Player input and freighter AI both produce turn/throttle controls. `Ship` holds
shared motion and docking state; `PhysicsSystem` applies acceleration, gravity,
rotation limits, and speed limits to every active ship. Collisions change the
actual velocity rather than adding a separate freighter drift effect.

`GravityWell.Core/AI/Freighters/FreighterAutopilot.cs` follows cruise, brake, align, approach, and dock phases.
It compensates for gravity, slows before waypoints, and makes a slow rear-first
final descent. `GravityWell.Core/AI/RoutePlanner.cs` plans detours around expanded planet and
operational spacedock bounds. Predictive local avoidance slows and steers around
other ships, with a consistent passing direction. Idle freighters hold near their
starting positions, and replacement freighters spawn clear of existing bodies.

Construction begins only after docking at the assigned planet completes. Current
freighter orders build surface facilities; spacedocks are obstacles on these
routes. Avoidance is a basic controller, not a guarantee against collisions in
arbitrarily dense traffic or an unrecoverable impact.

### AI fighters

Every faction with a homeworld, other than the player's, gets one peaceful AI fighter
(there are no weapons yet). `GravityWell.Core/AI/Fighters/FighterAutopilot.cs` sends it to the
nearest uncolonized planet and docks, which claims the planet. The claiming faction's own
freighters then build on it. Once the claim lands the fighter undocks by itself and heads
for the next nearest free planet. It re-picks if another faction claims its target first,
and holds position when no free planet is left or when it is wrecked.

AI fighters fly with the player fighter's own speed, thrust and turn limits. They share the
approach, align and dock logic in `PlanetApproach` with the freighters; each ship class
supplies its own `ApproachTuning`. If two fighters reach the same planet together, the first
in `GameWorld.Fighters` order (the player, then the AI fighters) keeps it and the other
re-picks. See [ADR 0007](docs/adr/0007-ai-fighters-share-the-planet-approach-logic.md).

Both autopilots are thin wrappers over `ShipAutopilot<TShip>`, which keeps a flight plan per ship
and asks an `IAutopilotStrategy<TShip>` what each ship should do. `FreighterStrategy` follows
dispatch orders and `PeacefulFighterStrategy` claims planets, so a new fighter behaviour is a new
strategy. A fighter's strategy is chosen from its `Behavior`, fixed at spawn and set per faction (see
`GameWorld.CreateAiFighters`). See [ADR 0008](docs/adr/0008-autopilots-are-strategies-over-a-shared-runner.md)
and [ADR 0010](docs/adr/0010-fighter-behaviour-is-an-enum-chosen-per-faction-at-spawn.md).

Run the deterministic simulation checks without browser or rendering dependencies:

```sh
dotnet run --project tests/FlightSimulation/FlightSimulation.csproj
```

They cover blocked routes, crossing traffic, drift and impact recovery, orbital
docks, multiple approach directions and frame rates, player docking, and fleet
construction with replacement production.

## Materials and docked repairs

Each planet has independent base and starbase stores, each capped at 20 raw and
20 finished materials. Homeworlds begin with 10 of each material in both stores;
new settlements begin empty. An operational colony on a habitable world supplies
one raw material every two seconds to each operational base/starbase. Each of
these facilities refines one raw material into one finished material every two
seconds. Production and refining pause when their required facilities are not
operational, inputs run out, or storage fills. Barren worlds cannot produce raw
materials locally.

Friendly fighters and freighters repair only after docking finishes. Surface
repairs require an operational base and restore one HP every two seconds from
its finished stores. Spacedock repairs require an operational starbase and dock,
restore one HP each second, and use the starbase's finished stores. Each restored
HP costs one finished material, charged only when that HP is restored. Repairs
stop at maximum health or when supplies run out; waiting for supplies does not
accumulate repair time. Undocking resets partial repair progress. Zero-health
ships cannot be revived by repairs.

The docking HUD shows repair status, health, and the relevant facility's raw and
finished materials. Rates live in `Economy/EconomySystem.cs` and
`Economy/RepairSystem.cs`; capacities live in `Entities/ResourceInventory.cs`.
These rates and capacities are tuning choices: the reference manual specifies
the material flow and faster orbital repairs but does not give numeric rates.

Ship construction costs and freighter cargo/resupply are not yet connected to
these stores. Barren outposts therefore need future resupply support for a
renewable repair service. The simulation checks also cover repair eligibility,
costs, depletion, separate inventories, production, and landing-to-repair flow.
