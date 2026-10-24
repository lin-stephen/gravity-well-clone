# Gravity Well clone

A C# WebAssembly recreation of the 1995 space strategy/action game, rendered with
[raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Run locally

Requires the .NET 10 SDK and the `wasm-tools` workload.

```sh
dotnet workload install wasm-tools
dotnet run
```

Use Left/Right to rotate, Up to thrust, and Home to reset the ship. The initial
prototype models gravity from a star and planet.

## Publish

```sh
dotnet publish -c Release -o publish
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
The shared tuning constants live in `Physics/CollisionSystem.cs`.

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
regardless of ownership; a completed landing claims an unowned planet. Spacedock
bounds follow the dock glyph near the orbital starbase. Thrust releases the ship;
it must leave the capture zone before docking to that target again. Home resets
all fighter docking state. Tune capture and damping in `Physics/DockingSystem.cs`.

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

`AI/FreighterAutopilot.cs` follows cruise, brake, align, approach, and dock phases.
It compensates for gravity, slows before waypoints, and makes a slow rear-first
final descent. `AI/RoutePlanner.cs` plans detours around expanded planet and
operational spacedock bounds. Predictive local avoidance slows and steers around
other ships, with a consistent passing direction. Idle freighters hold near their
starting positions, and replacement freighters spawn clear of existing bodies.

Construction begins only after docking at the assigned planet completes. Current
freighter orders build surface facilities; spacedocks are obstacles on these
routes. Avoidance is a basic controller, not a guarantee against collisions in
arbitrarily dense traffic or an unrecoverable impact.

Run the deterministic simulation checks without browser or rendering dependencies:

```sh
dotnet run --project tests/FlightSimulation/FlightSimulation.csproj
```

They cover blocked routes, crossing traffic, drift and impact recovery, orbital
docks, multiple approach directions and frame rates, player docking, and fleet
construction with replacement production.
