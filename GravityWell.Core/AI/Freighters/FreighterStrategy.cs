using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Freighters;

/// <summary>Freighters fly where they are sent: to the planet they were dispatched to, otherwise they hold.</summary>
internal sealed class FreighterStrategy(DockingSystem docking, PhysicsSystem physics) : IAutopilotStrategy<Freighter>
{
    private readonly PlanetApproach approach = new(docking, physics, ApproachTuning.ForFreighter);

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
            // A freighter only goes back to idle when it is recalled. Hold where it is, clear of planets,
            // rather than flying back to where it was first seen, which may be behind the planet it is next
            // to, and forget the old target so a new order to the same planet starts a fresh approach.
            if (plan.Phase != FlightPhase.Holding)
            {
                plan.HoldPosition = approach.ClearOfPlanets(world, ship);
                plan.Target = null;
            }
            plan.Phase = FlightPhase.Holding;
            return approach.HoldAt(world, ship, plan);
        }
        return approach.Steer(world, ship, plan, ship.TargetPlanet, deltaTime);
    }
}
