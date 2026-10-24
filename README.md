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
normal, preserving sideways motion. Freighter autopilot continues while collision
motion decays over time. Damage uses impact velocity before the bounce.

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
threshold and the target lies within 30 degrees of the ship's rear direction. Sideways motion does not count, and receding motion has zero approach
speed. Docking still requires safe total relative speed, rear-first alignment,
and proximity.
