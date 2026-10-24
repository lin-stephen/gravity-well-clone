using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>Freighters fly where they are sent: to the planet they were dispatched to, otherwise they hold.</summary>
internal sealed class FreighterStrategy(DockingSystem docking, PhysicsSystem physics) : IAutopilotStrategy<Freighter>
{
    private readonly PlanetApproach approach = new(docking, physics, ApproachTuning.ForFreighter);

    public IEnumerable<Freighter> ShipsToPilot(GameWorld world) =>
        world.Freighters.Where(ship => ship.State != FreighterState.Consumed);

    public Vector2 InitialHoldPosition(GameWorld world, Freighter ship) => ship.Position;

    public ShipControls Decide(GameWorld world, Freighter ship, FlightPlan plan, float deltaTime)
    {
        if (ship.DockingTarget is not null)
        {
            plan.Phase = FlightPhase.Dock;
            return default;
        }
        if (ship.State != FreighterState.Traveling || ship.TargetPlanet is null)
        {
            plan.Phase = FlightPhase.Holding;
            return approach.HoldAt(world, ship, plan);
        }
        return approach.Steer(world, ship, plan, ship.TargetPlanet, deltaTime);
    }
}
