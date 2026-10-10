# 0004: Separate state, systems, and renderers

- Status: Accepted
- Date: 2026-10-10

## Context

The initial prototype kept input, simulation, state, viewport handling, and all
drawing in `Application`. That structure becomes difficult to test and extend as
collision, weapons, factions, facilities, and AI are added.

## Decision

Use a pragmatic object-oriented architecture:

- Entity and world classes own game state.
- Systems perform input and simulation behavior across that state.
- Renderers draw state but don't own gameplay behavior.
- `GravityWellGame` coordinates each frame.
- `Application` remains only the browser/raylib lifecycle bridge.

Prefer composition for capabilities such as health, weapons, and shields. Add
inheritance only where entities have a genuine substitutable relationship.

## Consequences

- Simulation code can be tested without a raylib graphics context.
- Rendering changes don't need to mutate domain objects.
- New features require choosing an explicit owning module instead of adding
  convenience logic to the application entry point.
- More files and dependency boundaries exist than in the prototype monolith.
