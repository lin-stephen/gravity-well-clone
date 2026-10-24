using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>Flies each AI fighter to the nearest uncolonized planet so that docking there claims it.</summary>
public sealed class FighterAutopilot
{
    private const float HoldSpeed = 20f;

    private readonly PlanetApproach approach;
    private readonly Dictionary<Fighter, FlightPlan> plans = [];

    public FighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null) =>
        approach = new PlanetApproach(docking ?? new DockingSystem(), physics ?? new PhysicsSystem(),
            ApproachTuning.ForFighter);

    public Planet? GetTarget(Fighter fighter) => plans.TryGetValue(fighter, out var plan) ? plan.Target : null;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var fighter in world.AiFighters)
        {
            if (!plans.TryGetValue(fighter, out var plan))
            {
                plan = new FlightPlan { HoldPosition = fighter.Position };
                plans.Add(fighter, plan);
            }
            fighter.Controls = Steer(world, fighter, plan, deltaTime);
        }
    }

    private ShipControls Steer(GameWorld world, Fighter fighter, FlightPlan plan, float deltaTime)
    {
        // A wrecked fighter never claims: it holds where it is, stays put on a planet that is already
        // owned, and releases a free planet it was part-way through docking at.
        if (fighter.Health <= 0)
        {
            if (fighter.DockingTarget is not { } held) return Hold(world, fighter, plan);
            return IsOwned(held) ? default : new ShipControls(0f, 1f);
        }

        if (fighter.DockingTarget is { } docked)
        {
            if (!IsOwned(docked))
            {
                // Two ships docking side by side at one planet overlap and never finish, so the first
                // fighter in world order keeps the planet and the other releases it and re-picks.
                if (!ReferenceEquals(FirstDockingAt(world, docked), fighter))
                    return new ShipControls(0f, 1f);
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
        // Hold wherever the fighter was when it stopped having something to do.
        if (plan.Phase != FlightPhase.Holding)
            plan.HoldPosition = fighter.Position;
        plan.Target = null;
        plan.Phase = FlightPhase.Holding;
        return approach.Seek(world, fighter, plan.HoldPosition, HoldSpeed);
    }

    private static bool IsOwned(ICollisionBody body) => body switch
    {
        Planet planet => planet.IsColonized,
        SpaceDockBody dock => dock.Planet.IsColonized,
        _ => true,
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
