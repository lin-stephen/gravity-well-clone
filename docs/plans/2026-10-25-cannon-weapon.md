# Player Cannon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The player's fighter fires the Type I cannon along its nose. The bolt bends under planet gravity, damages enemy ships and enemy planet structures, is absorbed by every other planet, and gives visible hit feedback.

**Architecture:** Bolts are state on `GameWorld`. A new `WeaponSystem` fires, moves and resolves bolts each tick, using swept segment tests so fast bolts cannot tunnel. Structure positions and hit boxes move out of `StructureRenderer` into a Core `StructureLayout`, so the simulation and the drawing share one layout. Each `PlanetStructure` gets its own health. Renderers only draw bolts and impact flashes from world state (ADR 0004).

**Tech Stack:** C# / .NET 10, NUnit with the constraint model (see `CollisionSystemTests`), Raylib-cs rendering (untested).

**Spec:** `docs/superpowers/specs/2026-10-25-cannon-weapon-design.md`

## Global Constraints

- Only the player fires, only the Type I cannon, unlimited ammo, 1 damage per hit. AI fighters stay passive.
- Fire keys are `F` and `Down`. One bolt per key press with a minimum interval, and holding the key does not repeat.
- The bolt starts at the fighter's nose, travels along `Ship.Forward` and inherits the fighter's velocity. It is bent by `PhysicsSystem.GetGravity`.
- Bolts never hurt the shooter's own faction unless `WeaponSettings.FriendlyFire` is true (default false). With friendly fire off a bolt flies through friendly ships; friendly planets, and friendly structures outside the circle, absorb it without damage.
- No bolt ever leaves a planet circle on the far side. An unowned planet, a friendly planet, and a planet with no standing structure absorb the bolt at the edge. An enemy planet with a standing structure lets the bolt in, and it is absorbed at the first structure hit, or at the far edge.
- A standing structure is `Building` or `Operational`. `Planned` and `Destroyed` structures are not targets and do not obstruct.
- Ships can be reduced to 0 health but nothing destroys, removes or disables them. Do not change `Planet.Health`.
- The drawn picture of planets and structures does not change.
- Hit detection tests the bolt's movement over a frame as a segment, never just its end point.
- Bolt speed, lifetime, interval and structure health are starting values in settings or constants, to tune by play. Bolt speed must be above the fastest ship (260).
- Test files mirror Core folders and use namespace `GravityWell.Tests.<Folder>`.
- After every task: `dotnet test tests/GravityWell.Tests` passes, `dotnet run --project tests/FlightSimulation` prints no failure, and `dotnet build GravityWell.slnx` succeeds. One commit per task, staged by explicit path. Run the code-reviewer agent before each commit.
- After each task, tick its checkboxes in this file and add a one-line note under "Progress" at the bottom.

## Review Focus

- A bolt fired with its nose already inside an enemy planet's circle is absorbed at the far edge, with no crash or NaN (Task 8).
- A zero or very large `deltaTime` (a hitch) neither divides by zero nor skips a target (Tasks 6 and 7).
- An enemy planet whose structures are all destroyed absorbs bolts at the edge (Task 8).
- Two key presses within the interval fire once, and the bolt cap holds (Task 5).
- A bolt never damages the ship that fired it, even with friendly fire on (Task 7).

---

### Task 1: Structure health

**Files:**
- Modify: `GravityWell.Core/Entities/PlanetStructure.cs`
- Test: `tests/GravityWell.Tests/Entities/PlanetStructureTests.cs` (create)

**Interfaces:**
- Produces:
  - `const int PlanetStructure.MaxHealth = 4` (placeholder)
  - `int PlanetStructure.Health { get; }`, full when created
  - `bool PlanetStructure.IsTargetable` (`State` is `Building` or `Operational`)
  - `void PlanetStructure.TakeDamage(int damage)`: does nothing unless targetable; clamps `Health` at 0; at 0 sets `State = Destroyed`; leaves `ConstructionProgress` alone.

- [ ] **Step 1: Commit the spec and this plan first:** `git add docs/superpowers/specs/2026-10-25-cannon-weapon-design.md docs/plans/2026-10-25-cannon-weapon.md && git commit -m "Add cannon weapon design and plan"`
- [ ] **Step 2: Write the failing tests** in `PlanetStructureTests`:
  - `NewStructure_HasFullHealth`: `Health == PlanetStructure.MaxHealth` for each of `Planned`, `Building`, `Operational`.
  - `TakeDamage_OperationalStructure_LosesHealth`: after `TakeDamage(1)`, `Health == MaxHealth - 1` and `State == Operational`.
  - `TakeDamage_ToZero_DestroysStructureAndClamps`: `TakeDamage(MaxHealth + 3)` gives `Health == 0`, `State == Destroyed`.
  - `TakeDamage_BuildingStructure_KeepsConstructionProgress`: advance progress to 0.5, `TakeDamage(1)`: progress still 0.5, health reduced.
  - `TakeDamage_PlannedOrDestroyedStructure_IsUnchanged`.
  - `DestroyedStructure_NoLongerCountsOnPlanet`: an owned `Planet` with an operational Base: after `TakeDamage(MaxHealth)` on the Base, `HasStructure(Base)` and `HasOperationalStructure(Base)` are false.
- [ ] **Step 3: Run to verify they fail:** `dotnet test tests/GravityWell.Tests --filter "FullyQualifiedName~PlanetStructureTests"`. Expected: compile failure.
- [ ] **Step 4: Implement** the interface above in `PlanetStructure`.
- [ ] **Step 5: Run the full suite.** Expected: pass.
- [ ] **Step 6: Commit** `Give each planet structure its own health`.

### Task 2: Structure layout in Core

**Files:**
- Create: `GravityWell.Core/Entities/StructureLayout.cs`
- Modify: `GravityWell/Rendering/StructureRenderer.cs` (delete private `GetPosition`, call `StructureLayout.GetAnchor`)
- Test: `tests/GravityWell.Tests/Entities/StructureLayoutTests.cs` (create)

**Interfaces:**
- Produces:
  - `public readonly record struct Aabb(Vector2 Min, Vector2 Max)`
  - `public static class StructureLayout` with `Vector2 GetAnchor(Planet planet, StructureType type)` (the exact offsets in the current `StructureRenderer.GetPosition`) and `Aabb GetBounds(Planet planet, StructureType type)`: the box that encloses the glyph drawn by `DrawGlyph`, as offsets from the anchor.

- [ ] **Step 1: Write the failing tests** (planet at `(100, 200)`). `GetAnchor`: Base `(100,200)`, Colony `(100,219)`, TechCenter `(83,185)`, CommCenter `(114,189)`, and Starbase, SpaceDock and SensorArray each `(100,146)`. `GetBounds` as offsets from the anchor, one `TestCase` per row: Base `(-8,-8)..(8,8)`, Colony `(-15,-6)..(15,6)`, TechCenter `(-7,-12)..(7,6)`, CommCenter `(-6,-15)..(9,8)`, Starbase `(-25,-7)..(25,7)`, SpaceDock `(-15,-17)..(-5,0)`, SensorArray `(3,-14)..(17,0)`.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~StructureLayoutTests"`). Expected: compile failure.
- [ ] **Step 3: Implement `StructureLayout`**, then make `StructureRenderer` use `GetAnchor`. `DrawGlyph` and every drawn pixel stay unchanged.
- [ ] **Step 4: Run the full suite and build.** Expected: pass.
- [ ] **Step 5: Commit** `Move structure layout into Core`.

### Task 3: Bolt state, settings and the fire control

**Files:**
- Create: `GravityWell.Core/Entities/Bolt.cs`, `GravityWell.Core/Entities/Impact.cs`, `GravityWell.Core/Weapons/WeaponSettings.cs`
- Modify: `GravityWell.Core/World/GameWorld.cs`, `GravityWell.Core/Input/PlayerControls.cs`, `GravityWell.Core/Entities/Fighter.cs`, `GravityWell/Rendering/ShipRenderer.cs` (nose uses `Fighter.NoseDistance`)
- Test: `tests/GravityWell.Tests/Weapons/WeaponSettingsTests.cs` (create)

**Interfaces:**
- Produces:
  - `public sealed class Bolt(Ship shooter, Vector2 position, Vector2 velocity, float lifetime)` with `Ship Shooter`, `Faction Faction => Shooter.Faction`, settable `Position`, `Velocity`, `TimeRemaining`.
  - `public sealed class Impact(Vector2 position)` with `Position` and settable `float Age`.
  - `GameWorld.Bolts` and `GameWorld.Impacts`: public `List<Bolt>` and `List<Impact>`, empty by default.
  - `PlayerControls` gains a last parameter `bool Fire = false`.
  - `const float Fighter.NoseDistance = 13f`.
  - `public sealed record WeaponSettings` (validated like `CollisionSettings`, with `Validate()`), defaults: `BoltSpeed 520f` (> 0, added along the nose), `BoltLifetime 1.6f` (> 0), `FireInterval 0.25f` (>= 0), `Damage 1` (>= 1), `FriendlyFire false`, `MaxBolts 32` (>= 1), `ImpactDuration 0.3f` (> 0).

- [ ] **Step 1: Write the failing tests** in `WeaponSettingsTests`: `Defaults_AreValid` (`Validate()` does not throw); one `TestCase` each that throws `ValidationException`: `BoltSpeed = 0`, `BoltLifetime = 0`, `FireInterval = -1`, `Damage = 0`, `MaxBolts = 0`, `ImpactDuration = 0`; `Defaults_FriendlyFireIsOff`; `DefaultBoltSpeed_ExceedsFighterMaxSpeed`.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~WeaponSettingsTests"`). Expected: compile failure.
- [ ] **Step 3: Implement** the types above. Existing `PlayerControls` call sites still compile because the new parameter has a default. The renderer change replaces the literal `-13` with `-Fighter.NoseDistance`.
- [ ] **Step 4: Run the full suite and build.** Expected: pass.
- [ ] **Step 5: Commit** `Add bolt state, weapon settings and the fire control flag`.

### Task 4: Segment intersection helpers

**Files:**
- Create: `GravityWell.Core/Weapons/Sweep.cs`
- Test: `tests/GravityWell.Tests/Weapons/SweepTests.cs` (create)

**Interfaces:**
- Produces:
  - `public static class Sweep`
  - `bool SegmentCircle(Vector2 from, Vector2 to, Vector2 center, float radius, out float enter, out float exit)`: true when the segment `from..to` (parameter 0..1) overlaps the circle. `enter` is the first parameter inside the circle, clamped to at least 0 (so 0 when `from` is inside). `exit` is the unclamped parameter where the line leaves the circle and can exceed 1 when `to` is inside. Degenerate (zero-length) segments are handled without dividing by zero.
  - `bool SegmentBox(Vector2 from, Vector2 to, Aabb box, out float enter)`: true when the segment overlaps the box. `enter` is the first parameter inside the box, clamped to at least 0.

- [ ] **Step 1: Write the failing tests** in `SweepTests`, circle at origin radius 10:
  - `SegmentCircle_CrossingThrough`: from `(-20,0)` to `(20,0)`: true, `enter == 0.25`, `exit == 0.75`.
  - `SegmentCircle_StartsInside`: from `(0,0)` to `(20,0)`: true, `enter == 0`, `exit == 0.5`.
  - `SegmentCircle_EndsInside`: from `(-20,0)` to `(0,0)`: true, `enter == 0.5`, `exit > 1`.
  - `SegmentCircle_StopsShort`: from `(-30,0)` to `(-20,0)`: false.
  - `SegmentCircle_MissesAlongside`: from `(-20,15)` to `(20,15)`: false.
  - `SegmentCircle_ZeroLengthInside_TrueAndZeroLengthOutside_False`.
  - `SegmentBox_CrossingThrough`: box `(-5,-5)..(5,5)`, from `(-15,0)` to `(15,0)`: `enter == 1/3` (within 0.001).
  - `SegmentBox_StartsInside_EnterIsZero`, `SegmentBox_StopsShort_False`, `SegmentBox_MissesAbove_False`, `SegmentBox_AxisParallelOutsideSlab_False` (a horizontal segment at `y = 8`), `SegmentBox_ZeroLengthInside_True`.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~SweepTests"`). Expected: compile failure.
- [ ] **Step 3: Implement** the quadratic for the circle and the slab method for the box.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Add segment intersection helpers for bolt hits`.

### Task 5: Firing

**Files:**
- Create: `GravityWell.Core/Weapons/WeaponSystem.cs`
- Test: `tests/GravityWell.Tests/Weapons/WeaponSystemFiringTests.cs` (create)

**Interfaces:**
- Consumes: Task 3's types, `PhysicsSystem`.
- Produces:
  - `public sealed class WeaponSystem(PhysicsSystem physics, WeaponSettings? settings = null)` (validates settings in the constructor, like `CollisionSystem`)
  - `void Update(GameWorld world, PlayerControls controls, float deltaTime)`. Each call, in this order: age impacts, count down the fire timer, move and resolve existing bolts (Tasks 6 to 8), then fire. A bolt fired in a call is not moved in that call.
  - Firing: when `controls.Fire`, the timer is at 0, and `world.Bolts.Count < MaxBolts`, add `new Bolt(world.Player, Player.Position + Player.Forward * Fighter.NoseDistance, Player.Velocity + Player.Forward * BoltSpeed, BoltLifetime)` and set the timer to `FireInterval`. A docked player can fire.

- [ ] **Step 1: Write the failing tests.** Helper builds a `GameWorld` with one far-away unowned planet, no freighters and no AI fighters, `PhysicsSystem(new PhysicsSettings { PlanetGravityStrength = 0f })`, `WeaponSettings { BoltSpeed = 500f, BoltLifetime = 2f, FireInterval = 0.25f }`, and `Player.Position = (0, 0)`.
  - `Fire_SpawnsBoltAtNoseAlongFacing`: `Rotation = 90`, `Velocity = (30, 0)`. One bolt, `Position == (13, 0)`, `Velocity == (530, 0)`, `Shooter == world.Player`, `TimeRemaining == 2`.
  - `Fire_FacingUp_InheritsSidewaysVelocity`: `Rotation = 0`, `Velocity = (40, 0)`: bolt velocity `(40, -500)` and position `(0, -13)`.
  - `NoFire_NoBolt`.
  - `Fire_TwiceWithinInterval_FiresOnce`: `Fire = true` at `deltaTime = 0.1` three times gives one bolt, a fourth gives two (at 0.3 s the timer has expired).
  - `Fire_AtMaxBolts_DoesNotExceedCap`: `MaxBolts = 2`, `FireInterval = 0`, four fire updates give `Bolts.Count == 2`.
  - `Fire_WhileDocked_StillFires`: `BeginDocking(planet, Vector2.UnitY)` then `CompleteDocking()` on the player, then fire: one bolt.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~WeaponSystemFiringTests"`). Expected: compile failure.
- [ ] **Step 3: Implement** the firing path and the fire timer in `WeaponSystem`. Leave the move and hit step as an empty private method for Task 6.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Add WeaponSystem firing`.

### Task 6: Bolt flight under gravity

**Files:**
- Modify: `GravityWell.Core/Weapons/WeaponSystem.cs`
- Test: `tests/GravityWell.Tests/Weapons/WeaponSystemFlightTests.cs` (create)

**Interfaces:**
- Consumes: `PhysicsSystem.GetGravity(GameWorld, Vector2)`.
- Produces: moving a bolt over `deltaTime`: `Velocity += GetGravity(world, Position) * deltaTime`, then `Position += Velocity * deltaTime` (the same order the ships use), `TimeRemaining -= deltaTime`. A bolt whose `TimeRemaining` is at or below 0 after moving is removed. Impacts get `Age += deltaTime` and are removed once `Age >= ImpactDuration`. Move code must return the segment `(from, to)` for the hit step in Tasks 7 and 8. A `deltaTime <= 0` moves nothing.

- [ ] **Step 1: Write the failing tests** (same helper as Task 5):
  - `Bolt_WithoutGravity_TravelsStraight`: fire, then one update at `0.1`: `Position == (63, 0)` (within 0.001).
  - `Bolt_WithGravity_BendsTowardPlanet`: default `PhysicsSettings`, an unowned planet at `(0, -100)` (clear of the path), 30 updates at `0.02`: `Velocity.Y < 0` and `Position.Y < 0`.
  - `Bolt_PastLifetime_IsRemoved`: `BoltLifetime = 0.5`, fire, then two updates at `0.3`: `Bolts` is empty.
  - `Update_ZeroDeltaTime_DoesNotMoveBolts`: position unchanged and finite, bolt kept.
  - `Update_VeryLargeDeltaTime_RemovesExpiredBoltWithoutNaN`: `deltaTime = 5`: no bolts and no exception.
  - `Impact_AgesAndExpires`: add `new Impact(Vector2.Zero)` to the world with `ImpactDuration = 0.3`: after an update at `0.2` it exists with `Age == 0.2`, after another `0.2` it is gone.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~WeaponSystemFlightTests"`). Expected: fail.
- [ ] **Step 3: Implement** movement, expiry and impact ageing.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Move bolts under gravity and expire them`.

### Task 7: Bolts hit ships

**Files:**
- Modify: `GravityWell.Core/Weapons/WeaponSystem.cs`
- Test: `tests/GravityWell.Tests/Weapons/WeaponSystemShipHitTests.cs` (create)

**Interfaces:**
- Consumes: `Sweep.SegmentCircle`, `world.Ships`.
- Produces: for each bolt's segment this frame, every candidate ship is a circle of `CollisionRadius`. Skipped: the bolt's own `Shooter`; ships of the bolt's faction unless `FriendlyFire`. The earliest hit by segment parameter wins: that ship loses `Damage` health (`Math.Max(0, ...)`), an `Impact` is added at the hit point, and the bolt is removed. Docked ships are hit like any other. No ship is removed or disabled.

- [ ] **Step 1: Write the failing tests** (Task 5 helper; the player at `(0,0)` faces `+x`; a Red `Fighter { Faction = Red, Position = (200,0), Velocity = Zero }` added through the three-argument `GameWorld` constructor; a helper `Run(seconds)` steps at `0.02`):
  - `Bolt_HitsEnemyFighter`: after 0.6 s the fighter has `Health == 5`, `Bolts` is empty, `Impacts.Count == 1`, `Impacts[0].Position.X` is `190` within 1.
  - `Bolt_HitsEnemyFreighter`: a Red `Freighter` at `(200,0)`: `Health == Freighter.MaxHealth - 1`.
  - `Bolt_FliesThroughFriendlyShip`: a Blue AI fighter at `(200,0)` and a Red fighter at `(400,0)`: the Blue fighter keeps full health and the Red one takes 1.
  - `FriendlyFireOn_HurtsFriendlyShip`: `FriendlyFire = true`: the Blue fighter takes 1.
  - `Bolt_NeverHurtsItsShooter`: `FriendlyFire = true`, a bolt created by hand with `Shooter = world.Player` at the player's position: after an update the player has full health.
  - `FastBolt_DoesNotTunnelThroughShip`: `BoltSpeed = 20000`, `BoltLifetime = 1`, one update at `0.1`: the Red fighter at `(1000,0)` takes 1 (the bolt moves 2000 units in the frame).
  - `Bolt_HitsNearerOfTwoEnemiesOnly`: Red fighters at `(200,0)` and `(300,0)`: the first takes 1, the second none.
  - `Hit_ClampsHealthAtZero_AndKeepsShipInWorld`: an enemy with `Health = 0` is hit: `Health == 0`, still in `world.AiFighters`.
  - `Bolt_HitsDockedEnemyShip`: a Red fighter with `BeginDocking(farPlanet, Vector2.UnitY)` and `CompleteDocking()` takes 1.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~WeaponSystemShipHitTests"`). Expected: health unchanged.
- [ ] **Step 3: Implement** the ship part of the hit step. Structure the hit step as "collect candidate events with a parameter, a kind and a payload, apply the earliest", because Task 8 adds planet events to the same list.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Let bolts damage enemy ships`.

### Task 8: Bolts meet planets and structures

**Files:**
- Modify: `GravityWell.Core/Weapons/WeaponSystem.cs`
- Test: `tests/GravityWell.Tests/Weapons/WeaponSystemPlanetHitTests.cs` (create)

**Interfaces:**
- Consumes: `StructureLayout.GetBounds`, `PlanetStructure.IsTargetable` and `TakeDamage`, `Sweep.SegmentCircle` and `SegmentBox`.
- Produces: extra candidate events per planet, evaluated against the same frame segment, earliest wins. A planet is *hostile* to the bolt when its owner is a faction and (`owner != bolt.Faction` or `FriendlyFire`) and it has at least one targetable structure.
  - Hostile planet: each targetable structure's box is a damaging event (structure loses `Damage`, an `Impact` is added at the hit point, the bolt is removed). The circle's exit (the unclamped `exit` parameter, when `exit <= 1`) is an absorbing event.
  - Any other planet: the circle entry (`enter`) is an absorbing event with no damage and no impact. If the owner is the bolt's own faction (friendly fire off), each of its targetable structures' boxes is also an absorbing event, which covers a friendly Starbase outside the circle.
  - A bolt that starts inside a circle is covered by `enter == 0`. There is no per-bolt "inside" state.

- [ ] **Step 1: Write the failing tests.** Planet at `(300, 0)`. Gravity off. The player at `(0,0)` faces `+x` unless stated. A Red `Fighter` at `(500, 0)` behind the planet, `Health 6`, serves as the "bolt did not pass through" witness. Helpers build an owned planet with `AddStructure(StructureType.Base, StructureState.Operational)` and others as needed.
  - `UnownedPlanet_AbsorbsBolt`: `Bolts` empty, witness at full health, `Impacts` empty.
  - `OwnPlanet_AbsorbsBoltWithoutDamage`: Blue planet with a Base: `Base.Health == MaxHealth`, witness unhurt, no impact.
  - `EnemyPlanetWithNoStructures_AbsorbsBolt`.
  - `EnemyPlanetWithOnlyPlannedStructure_AbsorbsBolt`: Base added as `Planned`: its health is unchanged and the witness is unhurt.
  - `EnemyBase_TakesDamage_AndBoltIsGone`: Red planet with a Base: `Base.Health == MaxHealth - 1`, one impact, `Bolts` empty, witness unhurt.
  - `StructureInFront_ShieldsStructureBehind`: Red planet with Base and Colony, the player at `(300, 200)` facing up (`Rotation = 0`): the Colony takes 1, the Base none.
  - `DestroyedStructure_NoLongerObstructs`: same setup after `Colony.TakeDamage(PlanetStructure.MaxHealth)`: the Base takes 1.
  - `FromAbove_StarbaseShieldsPlanet`: Red planet with Base and Starbase (`Operational`), the player at `(300, -300)` facing down (`Rotation = 180`): the Starbase takes 1, the Base none.
  - `OffCentreBolt_ThroughGap_IsAbsorbedAtFarEdge`: Red planet with only a Base, the player at `(0, 20)` facing `+x`: the Base is unhurt, `Bolts` is empty, the witness at `(500, 20)` is unhurt.
  - `BoltStartingInsidePlanet_IsAbsorbedAtFarEdge`: the same planet, the player at `(280, 20)` facing `+x` (the nose is inside the circle): no exception, the Base is unhurt, the witness at `(500, 20)` is unhurt.
  - `AllStructuresDestroyed_PlanetAbsorbsAtEdge`: a Base reduced to 0 health: the witness is unhurt, no impact.
  - `FriendlyStarbase_AbsorbsBoltFromAbove`: Blue planet with Base and Starbase, the player at `(300,-300)` facing down: `Starbase.Health` unchanged, no impact, `Bolts` empty.
  - `FriendlyFireOn_DamagesOwnPlanetStructures`: `FriendlyFire = true`, Blue planet with a Base: `Base.Health == MaxHealth - 1`.
  - `ShipInFrontOfPlanet_IsHitBeforePlanet`: a Red fighter at `(200, 0)` and a Red planet behind it: the fighter takes 1, the planet's Base is unhurt.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~WeaponSystemPlanetHitTests"`). Expected: bolts pass through planets.
- [ ] **Step 3: Implement** the planet events in the shared candidate list from Task 7.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Let bolts hit planet structures and be absorbed by planets`.

### Task 9: Wire it into the game and draw it

**Files:**
- Modify: `GravityWell/Input/InputSystem.cs`, `GravityWell/Game/GravityWellGame.cs`, `GravityWell/Rendering/GameRenderer.cs`, `GravityWell/Rendering/Palette.cs`, `GravityWell/Rendering/HudRenderer.cs` (help text), `README.md` (controls line)
- Create: `GravityWell/Rendering/BoltRenderer.cs`, `docs/adr/0011-bolts-are-swept-segments-and-planets-are-one-way-doors.md`
- Test: none (rendering is untested). The simulation is covered by Tasks 1 to 8.

**Interfaces:**
- Consumes: `WeaponSystem.Update`, `GameWorld.Bolts`, `GameWorld.Impacts`, `WeaponSettings.ImpactDuration`.
- Produces:
  - `InputSystem.ReadControls` sets `Fire` from `IsKeyPressed(KeyboardKey.F) || IsKeyPressed(KeyboardKey.Down)`.
  - `GravityWellGame` owns a `WeaponSystem` built from the shared `PhysicsSystem` and calls `weapons.Update(world, controls, deltaTime)` right after `collisions.Update(world)`.
  - `BoltRenderer.DrawBolt(Bolt)` draws a short line along the velocity in `Palette.ForFaction(bolt.Faction)`. `DrawImpact(Impact, float duration)` draws an expanding, fading burst at `Impact.Position` in a new `Palette.Impact` colour (a light colour that stands out from all four faction colours). `GameRenderer.DrawWorld` draws bolts and impacts after ships.
  - The ADR follows `docs/adr/template.md` and records: swept-segment hits, per-structure health, the planet circle as a one-way door, and the friendly-fire setting.

- [ ] **Step 1: Implement** the input, loop, renderer, palette, help text and README changes. The help overlay lists `F` or `Down` for fire.
- [ ] **Step 2: Write the ADR** and add it to `docs/adr/README.md` if that file keeps an index.
- [ ] **Step 3: Verify** `dotnet test tests/GravityWell.Tests`, `dotnet run --project tests/FlightSimulation` and `dotnet build GravityWell.slnx`. Expected: all clean.
- [ ] **Step 4: Play-test** with `dotnet run --project GravityWell/GravityWell.csproj`. Confirm each Success line in the spec: the bolt leaves the nose and curves near a planet; a Red freighter or fighter flashes and loses 1 health in the HUD or radar; shooting Red's homeworld from different directions hits different structures; your own and unclaimed planets absorb bolts; no bolt comes out of the far side of a planet. Report tuning notes for bolt speed, lifetime and interval.
- [ ] **Step 5: Commit** `Wire the cannon into the game and draw bolts`.

## Progress

(Updated after each task.)
