# 0009: A base guards a planet from takeover

- Status: Accepted
- Date: 2026-10-24

## Context

A claim was permanent: `Planet.Claim` only filled an empty owner, so once any faction owned a
planet nobody else could ever take it, even if it never built anything there. The intended rule is
that a planet stays safe only once it has a base. A fighter that lands on another faction's planet
that has no base takes it.

That affects freighters. `FreighterSystem` never checked the owner when a freighter began
building, so the previous owner's freighter, already on its way, would arrive and build a base for
the new owner. A freighter also had no way to cancel its order.

## Decision

- **The rule lives on the planet.** `Planet.Claim(faction)` takes an unowned planet, or a planet
  owned by another faction that has no base. The owner re-claiming its own planet changes nothing.
- **Any base guards, even an unfinished one.** The guard is `HasStructure(StructureType.Base)`,
  which is true for a base that is planned, being built or operational, and false only for a
  destroyed one. An enemy flip during construction would leave a half-built base with no builder
  and the planet stuck, so the guard switches on as soon as construction is ordered.
- **Every fighter, including the player.** `ColonizationSystem` no longer filters on "unowned"; it
  lets `Planet.Claim` decide for any docked fighter.
- **Freighters are recalled.** `Freighter.Recall()` cancels the order of a traveling freighter,
  releases its dock if it was already docked, and keeps its cargo so it can be sent elsewhere.
  `FreighterSystem` calls it for any traveling freighter whose target planet is owned by a
  *different* faction. A target with no owner, or owned by the freighter's own faction, is left
  alone. The new owner's idle freighters are dispatched in the same update. A recalled freighter
  holds where it is, clear of planets, and forgets its old target. It does not fly back to where it
  was first seen, which can be on the far side of the planet it is next to.
- **No health check in the colonization rule.** `ColonizationSystem` does not look at a fighter's
  health: the base is the guard, not the attacker's condition. Instead the fighter strategy keeps
  wrecks out of it. A wrecked fighter stays put only on a planet its own faction owns and releases
  every other planet, since a wreck that finished docking at an unbased enemy planet would take it
  and keep taking it every tick, because nothing repairs a wreck.

## Consequences

- A planet is exposed from the moment it is claimed until its first freighter starts building,
  which is the freighter's travel and docking time. Taking it then costs the previous owner the
  planet and its freighter's trip, and the planet can be taken back while it still has no base.
- Colonization still never reverts to unowned. This changes ADR 0006's "a colonized planet stays
  colonized" only in that the owner can now change.
- AI fighters still choose only unowned planets as targets, and a healthy AI fighter releases any
  owned planet the moment it is captured, before docking can complete. So in practice only the
  player takes planets from other factions. Deliberately attacking unbased enemy planets would be a
  new fighter strategy.
- Nothing destroys structures yet. When something does, a planet whose base is destroyed loses
  its guard, because destroyed structures do not count.
