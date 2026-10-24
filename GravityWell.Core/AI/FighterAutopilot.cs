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
            if (fighter.Health <= 0) continue;
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
        if (fighter.DockingTarget is not null)
        {
            plan.Phase = FlightPhase.Dock;
            return default;
        }
        var target = NearestUncolonizedPlanet(world, fighter.Position);
        if (target is null)
        {
            plan.Target = null;
            plan.Phase = FlightPhase.Holding;
            return approach.Seek(world, fighter, plan.HoldPosition, HoldSpeed);
        }
        return approach.Steer(world, fighter, plan, target, deltaTime);
    }

    private static Planet? NearestUncolonizedPlanet(GameWorld world, Vector2 position) =>
        world.Planets.Where(planet => !planet.IsColonized)
            .MinBy(planet => Vector2.DistanceSquared(planet.Position, position));
}
