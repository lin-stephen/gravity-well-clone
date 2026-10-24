using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

public enum FlightPhase { Holding, Cruise, Brake, Align, Approach, Dock }

public sealed class FreighterAutopilot
{
    private const float HoldSpeed = 20f;

    private readonly PlanetApproach approach;
    private readonly Dictionary<Freighter, FlightPlan> plans = [];

    public FreighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null) =>
        approach = new PlanetApproach(docking ?? new DockingSystem(), physics ?? new PhysicsSystem(),
            ApproachTuning.ForFreighter);

    public FlightPhase GetPhase(Freighter ship) => plans.TryGetValue(ship, out var plan)
        ? plan.Phase : FlightPhase.Holding;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var removed in plans.Keys.Where(ship => ship.State == FreighterState.Consumed).ToArray())
            plans.Remove(removed);
        foreach (var ship in world.Freighters)
        {
            if (ship.State == FreighterState.Consumed) continue;
            if (!plans.TryGetValue(ship, out var plan))
            {
                plan = new FlightPlan { HoldPosition = ship.Position };
                plans.Add(ship, plan);
            }
            ship.Controls = Steer(world, ship, plan, deltaTime);
        }
    }

    private ShipControls Steer(GameWorld world, Freighter ship, FlightPlan plan, float deltaTime)
    {
        if (ship.DockingTarget is not null)
        {
            plan.Phase = FlightPhase.Dock;
            return default;
        }
        if (ship.State != FreighterState.Traveling || ship.TargetPlanet is null)
        {
            plan.Phase = FlightPhase.Holding;
            return approach.Seek(world, ship, plan.HoldPosition, HoldSpeed);
        }
        return approach.Steer(world, ship, plan, ship.TargetPlanet, deltaTime);
    }
}
