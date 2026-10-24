# Peaceful AI Fighter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every AI faction with a homeworld gets a fighter that repeatedly flies to the nearest uncolonized planet, docks to claim it, and moves on, so that faction's existing freighters colonize each claimed planet.

**Architecture:** `GameWorld` gains a list of AI fighters that join `Ships`, so physics, docking, repair and rendering see them. The planet-approach logic inside `FreighterAutopilot` is extracted into a shared `PlanetApproach` class parameterised by an `ApproachTuning` record; the freighter keeps its current values and a new `FighterAutopilot` uses fighter values. `ColonizationSystem` and `CollisionSystem` are widened from "the player" to "every fighter".

**Tech Stack:** C# / .NET 10, NUnit 5 with AwesomeAssertions, Raylib-cs rendering (untested), `tests/FlightSimulation` console harness.

**Spec:** `docs/superpowers/specs/2026-10-24-ai-fighter-design.md`

## Global Constraints

- The AI fighter flies with exactly the player fighter's speed, thrust and turn limits (`Fighter.MaxSpeed` 260, `ThrustAcceleration` 230, `TurnRate` 168). It is not slowed to freighter pace.
- `tests/GravityWell.Tests/AI/FreighterAutopilotTests.cs` and every `tests/FlightSimulation` check must keep passing **unchanged**. Freighter tuning values must not change.
- Every faction with a homeworld, other than the player's faction, gets exactly one AI fighter. No code names `Faction.Red`.
- Docking claims the planet. The claim rule stays in `ColonizationSystem`. Freighter dispatch is not changed.
- Peaceful AI: no weapons, targeting, chasing or fleeing.
- The player's controls, reset key, camera and HUD are unchanged.
- Test files go in folders that mirror `GravityWell.Core` (`AI/`, `Physics/`, `World/`, `Colonization/`) with namespace `GravityWell.Tests.<Folder>`.
- After every task: `dotnet test tests/GravityWell.Tests` passes, `dotnet run --project tests/FlightSimulation` prints no failure, and `dotnet build GravityWell.slnx` succeeds. One commit per task. Run the code-reviewer agent before each commit.
- Do not touch the unrelated uncommitted reformat in `GravityWell.Core/AI/RoutePlanner.cs`. Stage files by explicit path.

## Review Focus

- A fighter brakes to zero throttle near a planet that is not its target and docks there by accident: it must leave and carry on (pinned in Task 6).
- Two AI fighters pick the same planet: the first to dock claims it, the other re-picks, no deadlock (Task 6).
- An AI fighter spawns overlapping a planet or freighter and takes collision damage on the first tick (Task 1).
- A freighter crosses a fighter's path at fighter speed: no damaging contact (Task 5).
- The nearest planet cannot be reached (route blocked): motion stays finite and the fighter keeps retrying rather than crashing or spinning (Task 5).

---

### Task 1: AI fighters in the world

**Files:**
- Modify: `GravityWell.Core/World/GameWorld.cs`
- Test: `tests/GravityWell.Tests/World/GameWorldTests.cs` (create)

**Interfaces:**
- Produces:
  - `IReadOnlyList<Fighter> GameWorld.AiFighters`
  - `IEnumerable<Fighter> GameWorld.Fighters` (the `Player`, then `AiFighters`)
  - `GameWorld.Ships` yields `Player`, then `AiFighters`, then live freighters
  - `GameWorld(IEnumerable<Planet> planets, IEnumerable<Freighter> freighters, IEnumerable<Fighter>? aiFighters = null)`
  - `static List<Fighter> GameWorld.CreateAiFighters(IEnumerable<Planet> planets, Faction playerFaction)`: one fighter per owned planet with an operational `StructureType.TechCenter` (the existing "homeworld" test used by `FreighterSystem`), skipping `playerFaction` and any faction already given a fighter. The fighter has `Faction` set, `Velocity` zero, spawns on the homeworld's +Y side clear of the planet and of the faction's starting freighters (about `Position + new Vector2(0, 100)`), and `Rotation` 180 so it faces outward.

- [ ] **Step 1: Write the failing tests** in `GameWorldTests`:
  - `DefaultWorld_HasOneRedAiFighterAndNoBlueAiFighter`: `world.AiFighters` has one fighter, `Faction == Faction.Red`, `world.Player.Faction == Faction.Blue`, `world.AiFighters` does not contain `world.Player`.
  - `DefaultWorld_AiFighterSpawnsClearOfEveryPlanetAndFreighter`: for the AI fighter, distance to every planet `>= Planet.Radius + Fighter.Radius`, and to every freighter `>= Fighter.Radius + Freighter.Radius`; also within 200 of the Red homeworld.
  - `Ships_YieldsPlayerThenAiFightersThenFreighters`.
  - `CreateAiFighters_OneFighterPerAiHomeworldFaction`: planets with Purple and Red homeworlds (`BuildHomeworld()`), player Blue gives exactly one Purple and one Red fighter; a Blue homeworld and a non-homeworld owned planet give none.
- [ ] **Step 2: Run to verify they fail:** `dotnet test tests/GravityWell.Tests --filter "FullyQualifiedName~GameWorldTests"`. Expected: compile failure (members missing).
- [ ] **Step 3: Implement** the interface above. The default constructor calls `CreateAiFighters(Planets, Player.Faction)`.
- [ ] **Step 4: Run the full suite.** Expected: all pass.
- [ ] **Step 5: Commit** `git add GravityWell.Core/World/GameWorld.cs tests/GravityWell.Tests/World/GameWorldTests.cs && git commit -m "Add AI fighters to the world"`

### Task 2: AI fighters collide

**Files:**
- Modify: `GravityWell.Core/Physics/CollisionSystem.cs:23` (body list)
- Test: `tests/GravityWell.Tests/Physics/CollisionSystemTests.cs`

**Interfaces:**
- Consumes: `GameWorld.AiFighters`, the three-argument `GameWorld` constructor.

- [ ] **Step 1: Write the failing test** `Update_AiFighterHitsFreighter_BothTakeDamage`: world with one far-away planet, one Red `Fighter` and one `Freighter` overlapping and closing at 50 units/s, `CollisionSettings { DamageSpeedThreshold = 10f, SpeedPerDamagePoint = 5f }`, `Player.Position = (10000, 10000)`. After `Update`, both `Health` values are below their maximum.
- [ ] **Step 2: Run to verify it fails** (`--filter "FullyQualifiedName~CollisionSystemTests"`). Expected: health unchanged.
- [ ] **Step 3: Add `world.AiFighters` to the body list** in `CollisionSystem.Update`.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** (explicit paths) `Include AI fighters in collisions`.

### Task 3: Any fighter claims on docking

**Files:**
- Modify: `GravityWell.Core/Colonization/ColonizationSystem.cs`
- Test: `tests/GravityWell.Tests/Colonization/ColonizationSystemTests.cs` (create)

**Interfaces:**
- Consumes: `GameWorld.Fighters`.

- [ ] **Step 1: Write the failing tests:**
  - `Update_AiFighterDockedAtFreePlanet_ClaimsForItsFaction`: Red fighter, `BeginDocking(planet, Vector2.UnitY)` then `CompleteDocking()` on a free planet. After `Update`, `planet.Owner == Faction.Red`.
  - `Update_FighterDockedAtOwnedPlanet_KeepsOwner`: planet owned by Blue, Red fighter docked: owner stays Blue.
  - `Update_PlayerDockedAtFreePlanet_StillClaims`.
  - `Update_ClaimedByRed_DispatchesIdleRedFreighterOnly`: one idle Red and one idle Blue freighter, a free planet claimed by a docked Red fighter. After `ColonizationSystem.Update` then `FreighterSystem.Update(world, 0.1f)`, the Red freighter is `Traveling` with `TargetPlanet` equal to the planet and the Blue freighter is `Idle`.
- [ ] **Step 2: Run to verify** the first test fails (owner null), the others pass or fail only on the first.
- [ ] **Step 3: Iterate `world.Fighters`** in `ColonizationSystem.Update`; the claim condition is unchanged.
- [ ] **Step 4: Run the full suite.** Expected: pass.
- [ ] **Step 5: Commit** `Let any fighter claim planets by docking`.

### Task 4: Extract the shared approach logic (pure refactor)

**Files:**
- Create: `GravityWell.Core/AI/PlanetApproach.cs`, `GravityWell.Core/AI/ApproachTuning.cs`
- Modify: `GravityWell.Core/AI/FreighterAutopilot.cs`
- Test: none new. `tests/GravityWell.Tests/AI/FreighterAutopilotTests.cs` is the guard and must not be edited.

**Interfaces:**
- Produces:
  - `public sealed record ApproachTuning` with init properties: `CruiseSpeed`, `RouteWaypointSpeed`, `ApproachClearance`, `CorridorBreakDistance`, `CorridorBreakSpeed`, `AlignedSidewaysSpeed`, `HoverSpeed`, `TouchdownSpeed`, `WaypointReachedDistance`, `AlignTriggerDistance`, `AlignTriggerSpeed`, `BrakeDeceleration` (the `40f` in `Seek` and the `80f` divisor in the braking distance, which is `2 * BrakeDeceleration`), `BrakeTurnSeconds` (the `1.2f`). `static ApproachTuning ForFreighter` carries today's constant values exactly (`CruiseSpeed = Freighter.Speed`).
  - `public sealed class FlightPlan` (moved out of `FreighterAutopilot`, same fields).
  - `public sealed class PlanetApproach(DockingSystem docking, PhysicsSystem physics, ApproachTuning tuning)` with `ShipControls Steer(GameWorld world, Ship ship, FlightPlan plan, Planet target, float deltaTime)` (everything from `BeginApproach` through `SteerCruise`, now over `Ship`) and `ShipControls Seek(GameWorld world, Ship ship, Vector2 destination, float maxSpeed)`.
  - `FreighterAutopilot` keeps its public API (`Update`, `GetPhase`, constructor) and its Dock/Holding handling, and delegates the rest.

- [ ] **Step 1: Run the baseline** `dotnet test tests/GravityWell.Tests` and `dotnet run --project tests/FlightSimulation`. Expected: all pass. Record the output.
- [ ] **Step 2: Move the logic** into `PlanetApproach`, replacing the private constants with `tuning` properties. Behaviour must be bit-for-bit identical for `ApproachTuning.ForFreighter`.
- [ ] **Step 3: Re-run both commands.** Expected: identical pass results; any golden-value failure in `FreighterAutopilotTests` means the refactor changed behaviour, so fix the refactor and never the test.
- [ ] **Step 4: Commit** `Extract planet approach logic from FreighterAutopilot`.

### Task 5: FighterAutopilot flies one target at fighter speed

**Files:**
- Create: `GravityWell.Core/AI/FighterAutopilot.cs`
- Modify: `GravityWell.Core/AI/ApproachTuning.cs` (add `static ApproachTuning ForFighter`)
- Test: `tests/GravityWell.Tests/AI/FighterApproachTests.cs` (create)

**Interfaces:**
- Consumes: `PlanetApproach`, `FlightPlan`, `ApproachTuning`.
- Produces:
  - `public sealed class FighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null)`
  - `void Update(GameWorld world, float deltaTime)`: for each `world.AiFighters` entry whose health is above zero, targets the nearest planet with `Owner == null` (distance from the fighter, planet order breaks ties), steers with `PlanetApproach` using `ApproachTuning.ForFighter`, and writes `fighter.Controls`. With no free planet it holds position through `PlanetApproach.Seek` at a hold speed. It never touches `world.Player`.
  - `Planet? GetTarget(Fighter fighter)`
  - `ApproachTuning.ForFighter`: `CruiseSpeed = Fighter.MaxSpeed`. The other values are found in Step 3.

- [ ] **Step 1: Write the failing tests** in `FighterApproachTests`. Each flies a Red AI fighter, via `FighterAutopilot`, `PhysicsSystem`, `DockingSystem` and `CollisionSystem` at 60 Hz for up to 120 s, to a free planet at the origin, with the player at (10000, 10000). Each asserts: the fighter ends `IsDocked` at that planet, `Health == Fighter.MaxHealth`, `IsRearAligned`, and every position and velocity stayed finite. Scenarios (one `TestCase` each, named): start (-900, 0) at rest; start (-900, 0) velocity (260, 0) toward the planet; start (0, 800) velocity (200, -150) sideways drift; start (-1200, 0) with a blocking planet at (-600, 0); start (-900, 0) with a bump of (-60, 65) at 4 s; `Fighter_AvoidsFreighterCrossingItsPath` (a freighter crossing the line at the midpoint, closest distance stays at least `Fighter.Radius + Freighter.Radius`); `Fighter_BlockedByEnclosingPlanets_KeepsRetryingWithFiniteMotion` (nearest planet ringed so no route exists: after 30 s motion is finite, health is full, and the fighter is not docked).
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~FighterApproachTests"`). Expected: compile failure.
- [ ] **Step 3: Implement `FighterAutopilot`, then tune.** Start from the freighter values with `CruiseSpeed = Fighter.MaxSpeed` and a higher `BrakeDeceleration`, then adjust `ApproachTuning.ForFighter` until every scenario passes. Braking from 260 needs several hundred units, so expect `BrakeDeceleration`, `BrakeTurnSeconds`, `RouteWaypointSpeed` and `CorridorBreakSpeed` to differ from the freighter. Only `ApproachTuning.ForFighter` may change; the `ApproachTuning.ForFreighter` values must not.
- [ ] **Step 4: Run the full suite and the simulation harness.** Expected: pass, including the unchanged freighter tests.
- [ ] **Step 5: Commit** `Add FighterAutopilot with fighter approach tuning`.

### Task 6: The claim-and-move-on loop

**Files:**
- Modify: `GravityWell.Core/AI/FighterAutopilot.cs`
- Test: `tests/GravityWell.Tests/AI/FighterAutopilotTests.cs` (create)

**Interfaces:**
- Consumes: Task 5's `FighterAutopilot`, `GetTarget`; `ColonizationSystem`.
- Produces: no new public members. Behaviour added to `Update`:
  - Docking in progress at a free planet: output `default` so the claim can land.
  - Docked or docking at any planet or space dock that already has an owner: output `new ShipControls(0f, 1f)` so physics undocks it and it flies away rear-first, then it re-picks its target.
  - A target that gains an owner is dropped and re-picked on the next update.
  - A fighter with `Health <= 0` holds position and never claims.

- [ ] **Step 1: Write the failing tests** in `FighterAutopilotTests` (loop: `FighterAutopilot`, physics, docking, collisions, `ColonizationSystem`, 60 Hz):
  - `Update_PicksNearestUncolonizedPlanet`: `GetTarget` equals the nearer of two free planets; owned planets are never chosen.
  - `Update_ClaimsPlanetThenFliesToNext`: two free planets, 180 s: the first has `Owner == Faction.Red`, then the second has `Owner == Faction.Red`, and the fighter ends docked at the second.
  - `Update_TargetClaimedByAnotherFaction_RePicksNext`: mid-flight `nearer.Claim(Faction.Purple)`; `GetTarget` becomes the other planet and the fighter does not dock at the claimed one.
  - `Update_NoFreePlanet_HoldsPosition`: all planets owned: after 20 s the fighter is within 25 units of its start and slower than 1 unit/s.
  - `Update_ZeroHealthFighter_HoldsAndNeverClaims`: `Health = 0` beside a free planet, 30 s: `Owner` still null and speed under 1.
  - `Update_DockedAtOwnedNonTargetPlanet_LeavesAndCarriesOn` (accidental docking): fighter docked at a Red-owned planet with a free planet elsewhere: within 5 s `DockingTarget` is null, and within 120 s it docks at the free planet.
  - `Update_TwoFightersSamePlanet_FirstClaimsOtherMovesOn`: Red and Purple fighters, one equidistant free planet and one farther: exactly one faction owns the near planet and the other ends up owning the far one; neither fighter ends stuck.
  - `Update_DoesNotTouchPlayerControls`: `world.Player.Controls` is unchanged after `Update`.
- [ ] **Step 2: Run to verify they fail** (`--filter "FullyQualifiedName~FighterAutopilotTests"`). Expected: the loop tests fail.
- [ ] **Step 3: Implement the behaviours** above in `FighterAutopilot`. Per-fighter state is the `FlightPlan` plus a hold position, as in `FreighterAutopilot`.
- [ ] **Step 4: Run the full suite and the simulation harness.** Expected: pass.
- [ ] **Step 5: Commit** `Make AI fighter claim planets in a loop`.

### Task 7: Wire into the game and draw it

**Files:**
- Modify: `GravityWell/Game/GravityWellGame.cs`, `GravityWell/Rendering/GameRenderer.cs`, `GravityWell/Rendering/RadarRenderer.cs`
- Test: `tests/GravityWell.Tests/AI/AiExpansionTests.cs` (create)

**Interfaces:**
- Consumes: `FighterAutopilot`, `GameWorld.AiFighters`, `ShipRenderer.Draw(Fighter, bool)`, `Palette.ForFaction`.

- [ ] **Step 1: Write the end-to-end test** `DefaultWorld_RedExpandsToEveryFreePlanet`: a default `GameWorld`, and the systems in `GravityWellGame.Tick` order without input or rendering (both autopilots, `PhysicsSystem` with default controls, docking, collisions, colonization, freighters, economy, repairs) for 600 simulated seconds at 60 Hz. Assert: every planet `IsColonized`; Blue owns exactly its homeworld; Red owns the other four; each Red-claimed planet has an operational `StructureType.Base`; the player's fighter took no damage.
- [ ] **Step 2: Run it.** This is an integration check on Tasks 1 to 6, not a red-first test: it should pass once it compiles. If it fails, a real integration bug exists in an earlier task, so fix that task's code and add the missing unit test there. The wiring and rendering changes in Step 3 have no automated test (Raylib), so Step 4 verifies them by hand.
- [ ] **Step 3: Wire it.** In `GravityWellGame` construct `FighterAutopilot(docking, physics)` next to `FreighterAutopilot` and call its `Update` straight after the freighter autopilot. In `GameRenderer.DrawWorld` draw each `world.AiFighters` entry with `ships.Draw(fighter, fighter.Controls.Throttle > 0f)`. In `RadarRenderer.Draw` add a marker per AI fighter (`DrawObjectMarker`, radius 3, `Palette.ForFaction(fighter.Faction)`), drawn after the freighters.
- [ ] **Step 4: Verify.** `dotnet test tests/GravityWell.Tests` passes; `dotnet build GravityWell.slnx` succeeds. Launch the game (use the `run` skill) and confirm by eye that the Red fighter appears, flies to a planet, the planet turns red, and it moves on. Report exactly what you saw, or that you could not run it.
- [ ] **Step 5: Commit** `Run and draw AI fighters in the game`.

### Task 8: Document the decision

**Files:**
- Create: `docs/adr/0007-ai-fighters-share-the-planet-approach-logic.md` (use `docs/adr/template.md`)
- Modify: `docs/reference/game-mechanics.md`, `docs/adr/README.md` (index, if it lists ADRs)

- [ ] **Step 1: Write the ADR**: AI fighters live on `GameWorld`, approach logic shared via `PlanetApproach` with per-ship `ApproachTuning`, docking is the only claim rule, a wrecked AI fighter is permanent in v1, and unreachable targets are retried rather than blacklisted.
- [ ] **Step 2: Add the AI fighter behaviour** to `game-mechanics.md` (who gets one, the claim loop, edge cases).
- [ ] **Step 3: Commit** `Document AI fighter behaviour`. Then run the doc-updater agent for codemaps if `docs/CODEMAPS` exists.
