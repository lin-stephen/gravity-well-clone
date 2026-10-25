# 0011: Bolts are swept segments and planets are one-way doors

- Status: Accepted
- Date: 2026-10-25

## Context

The player's fighter now fires the Type I cannon. Its bolts are fast, small and bent by planet gravity,
and they must hit ships and planet structures that are drawn smaller than one frame's travel. The game
had no projectile, no way to damage a structure, and the positions of structures lived only in the
renderer.

## Decision

- **A bolt's frame is a segment.** `WeaponSystem` tests the line from where the bolt was to where it is
  against every candidate (`Sweep.SegmentCircle`, `Sweep.SegmentBox`) and applies the earliest event.
  Testing only the end point would let a fast bolt skip a thin target. The hit test runs before the
  expiry check so a bolt gets a last look on its final frame.
- **Structures carry their own health.** `PlanetStructure` has `Health`, and `TakeDamage` destroys it at 0.
  `Planet.Health` is untouched: collisions and repairs already use it. A planet's resilience comes from
  structures blocking bolts, not from a bigger number.
- **Structure layout lives in Core.** `StructureLayout` gives each structure's anchor and a box around its
  drawn glyph. The renderer and the weapon system read the same values, so what the player sees is what
  gets hit.
- **A planet is a one-way door.** A planet the shooter can hurt (an enemy's, with a standing structure)
  lets the bolt in: it hits the first structure on its way or is absorbed where it would come out. Every
  other planet absorbs the bolt on entry, and a friendly planet's own structures absorb it too. No bolt
  leaves the far side of a planet.
- **Friendly fire is a setting.** `WeaponSettings.FriendlyFire` is off by default. With it off a bolt flies
  through friendly ships and is absorbed, undamaging, by friendly planets.
- **Hit feedback is state.** `WeaponSystem` adds an `Impact` to `GameWorld` for each damaging hit and ages
  it. `BoltRenderer` only draws it. A bolt absorbed by a planet makes no flash, so a hit is distinct from a miss.

## Consequences

- Structures can now be destroyed, so a destroyed Base removes the takeover guard from
  [ADR 0009](0009-a-base-guards-a-planet-from-takeover.md). The existing freighter logic then rebuilds a
  destroyed structure. `Planet.AddStructure` replaces a destroyed entry of the same type, so a type never
  appears twice and the destroyed glyph is not left under the new one. A freighter whose build site is
  destroyed under it calls `Freighter.Abandon()`, lets go of its dock and is sent again with its cargo.
  Neither behaviour is designed further here.
- Ships can drop to 0 health but nothing removes or disables them yet. That is the next phase.
- Hit boxes cover a glyph's nominal geometry, not its stroke width, so a highlighted dock can poke about a
  pixel outside its box.
- Each frame builds a few small collections per bolt. At 32 bolts and ten planets that is cheap, and
  caching which planets have a standing structure is the first thing to do if it ever is not.
