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
both bodies lose `ceil((relative speed - 100) / 25)` health if relative speed is
above 100; slower contacts are harmless. Relative speed is the magnitude of the
velocity difference, with planets stationary. Damage applies once per contact;
separating and touching again allows another hit. Health is clamped to zero.
The shared tuning constants live in `Physics/CollisionSystem.cs`.

Ships bounce with equal mass and restitution 0.8; planets remain fixed. Contact
resolution separates overlaps and changes only inward velocity along the contact
normal, preserving sideways motion. Freighter autopilot continues while collision
motion decays over time. Damage uses impact velocity before the bounce.

Destruction is not yet implemented. Detection uses positions each frame, so
sufficiently fast bodies can pass through each other between frames.
