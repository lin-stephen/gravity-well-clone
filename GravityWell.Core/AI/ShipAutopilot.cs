using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>Flies every ship an <see cref="IAutopilotStrategy{TShip}"/> names, keeping one flight plan per ship.</summary>
public sealed class ShipAutopilot<TShip>(IAutopilotStrategy<TShip> strategy) where TShip : Ship
{
    private readonly Dictionary<TShip, FlightPlan> plans = [];

    public FlightPlan? PlanOf(TShip ship) => plans.GetValueOrDefault(ship);

    public void Update(GameWorld world, float deltaTime)
    {
        var ships = strategy.ShipsToPilot(world).ToList();
        foreach (var gone in plans.Keys.Except(ships).ToArray())
            plans.Remove(gone);
        foreach (var ship in ships)
        {
            if (!plans.TryGetValue(ship, out var plan))
            {
                plan = new FlightPlan { HoldPosition = strategy.InitialHoldPosition(world, ship) };
                plans.Add(ship, plan);
            }
            ship.Controls = strategy.Decide(world, ship, plan, deltaTime);
        }
    }
}
