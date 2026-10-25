using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Fighters;

/// <summary>
/// A fighter with no weapons to use: it claims planets. It picks the nearest uncolonized planet, docks
/// there (docking claims it), then leaves for the next one, and holds when nothing is left to claim.
/// </summary>
internal sealed class PeacefulFighterStrategy : IAutopilotStrategy<Fighter>
{
    private readonly PlanetApproach approach;

    public PeacefulFighterStrategy(DockingSystem docking, PhysicsSystem physics, ApproachTuning tuning) =>
        approach = new PlanetApproach(docking, physics, tuning);

    // A new plan already reads as Holding, so Hold will not recompute this.
    public Vector2 InitialHoldPosition(GameWorld world, Fighter ship) => approach.ClearOfPlanets(world, ship);

    public ShipControls Decide(GameWorld world, Fighter fighter, FlightPlan plan, float deltaTime)
    {
        // A wrecked fighter never claims: it holds where it is, stays put on a planet its own faction owns,
        // and releases any other planet. That includes an enemy planet without a base, which it would
        // otherwise take on finishing the dock and keep taking, since nothing repairs a wreck.
        if (fighter.Health <= 0)
        {
            if (fighter.DockingTarget is not { } held)
            {
                return Hold(world, fighter, plan);
            }

            return IsOwnedBy(held, fighter.Faction) ? default : new ShipControls(0f, 1f);
        }

        if (fighter.DockingTarget is { } docked)
        {
            if (!IsOwned(docked))
            {
                // Two ships docking side by side at one planet overlap and never finish, so the first
                // fighter in world order keeps the planet and the other releases it and re-picks.
                if (!ReferenceEquals(FirstDockingAt(world, docked), fighter))
                {
                    return new ShipControls(0f, 1f);
                }

                plan.Phase = FlightPhase.Dock; // Let the claim land.
                return default;
            }
            // Throttle is what releases a docked ship. It leaves rear-first and then picks its next target.
            plan.Phase = FlightPhase.Cruise;
            return new ShipControls(0f, 1f);
        }

        var target = NearestUncolonizedPlanet(world, fighter);
        return target is null ? Hold(world, fighter, plan) : approach.Steer(world, fighter, plan, target, deltaTime);
    }

    private ShipControls Hold(GameWorld world, Fighter fighter, FlightPlan plan)
    {
        // Hold where the fighter was when it stopped having something to do, but never pressed
        // against a planet it has just left.
        if (plan.Phase != FlightPhase.Holding)
        {
            plan.HoldPosition = approach.ClearOfPlanets(world, fighter);
        }

        plan.Target = null;
        plan.Phase = FlightPhase.Holding;
        return approach.HoldAt(world, fighter, plan);
    }

    private static bool IsOwned(ICollisionBody body) => body switch
    {
        Planet planet => planet.IsColonized,
        SpaceDockBody dock => dock.Planet.IsColonized,
        _ => true,
    };

    private static bool IsOwnedBy(ICollisionBody body, Faction faction) => body switch
    {
        Planet planet => planet.Owner == faction,
        SpaceDockBody dock => dock.Planet.Owner == faction,
        _ => false,
    };

    private static Fighter? FirstDockingAt(GameWorld world, ICollisionBody body) =>
        world.Fighters.FirstOrDefault(other => other.Health > 0 && ReferenceEquals(other.DockingTarget, body));

    /// <summary>The nearest free planet that no other fighter is already docking at.</summary>
    private static Planet? NearestUncolonizedPlanet(GameWorld world, Fighter fighter) =>
        world.Planets
            .Where(planet => !planet.IsColonized &&
                             !world.Fighters.Any(other => other.Health > 0 && !ReferenceEquals(other, fighter) &&
                                                          ReferenceEquals(other.DockingTarget, planet)))
            .MinBy(planet => Vector2.DistanceSquared(planet.Position, fighter.Position));
}
