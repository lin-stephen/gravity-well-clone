# 0006: Colonize planets on touch until collision exists

- Status: Accepted
- Date: 2026-10-12

## Context

The prototype has a single planet and a star, and no collision detection. Colonization
is the next gameplay loop, but the intended interaction is landing, and landing needs
collision and landing rules (rotation and speed limits) that don't exist yet. The
prototype needs a working colonization loop now, and the temporary rule must be easy
to replace.

The star is removed from the game, so planets are the only gravity sources and the
only colonizable bodies. Planets start as uncolonized (green) except for one home
planet, which starts colonized in the player's ship colour. Colonization state is
owned by the planet, so it persists when the ship is reset.

## Decision

- Model each planet as a `Planet` entity with a `Position` and an `IsColonized` flag.
  Colonization is one-way: a colonized planet stays colonized.
- A colonization system in `Colonization/` colonizes an uncolonized planet when the
  ship touches it, meaning the distance between centres is at most the fighter radius
  plus the planet radius. The rule is a temporary stand-in for landing.
- Run the colonization system after physics each frame, via `GravityWellGame`.
- Draw colonized planets in the ship colour and uncolonized planets in green. Both the
  world view and the radar read these colours from a shared `Palette`.
- Planets remain gravity sources whether or not they are colonized.

## Consequences

- Colonization can be played and tested before collision exists.
- When collision and landing arrive, only `ColonizationSystem` needs to change. It
  should check that the ship has landed (rotation, speed, and contact), not just touched
  the planet. Callers and the renderer don't depend on how colonization is triggered.
- Touching is too permissive. A ship can colonize a planet while flying past at speed.
  This is accepted for the prototype and will be superseded by the landing rule.
- The planet state lives on the world, not on the ship, so the reset key doesn't undo
  colonization. This matches the expectation that colonization is progress, but it is a
  design choice that could change.
- With the star removed, the ship no longer orbits anything. Planets are the only
  gravity sources, so the ship drifts in straight lines unless the player steers.
