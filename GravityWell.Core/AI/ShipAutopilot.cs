using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>
/// Flies every ship it is given, keeping one flight plan per ship and asking the strategy chosen for that ship
/// what to do. A ship keeps the strategy it was first given for as long as it stays listed; a ship that is no
/// longer listed loses both its flight plan and its strategy.
/// </summary>
/// <param name="shipsToPilot">The ships to fly this tick.</param>
/// <param name="strategyFor">The strategy a ship follows. It is consulted once, when the ship is first seen.</param>
public sealed class ShipAutopilot<TShip>(
    Func<GameWorld, IEnumerable<TShip>> shipsToPilot,
    Func<TShip, IAutopilotStrategy<TShip>> strategyFor) where TShip : Ship
{
    private readonly Dictionary<TShip, (FlightPlan Plan, IAutopilotStrategy<TShip> Strategy)> pilots = [];

    public FlightPlan? PlanOf(TShip ship) => pilots.TryGetValue(ship, out var pilot) ? pilot.Plan : null;

    public void Update(GameWorld world, float deltaTime)
    {
        var ships = shipsToPilot(world).ToList();
        foreach (var gone in pilots.Keys.Except(ships).ToArray())
        {
            pilots.Remove(gone);
        }

        foreach (var ship in ships)
        {
            if (!pilots.TryGetValue(ship, out var pilot))
            {
                var strategy = strategyFor(ship);
                pilot = (new FlightPlan { HoldPosition = strategy.InitialHoldPosition(world, ship) }, strategy);
                pilots.Add(ship, pilot);
            }
            ship.Controls = pilot.Strategy.Decide(world, ship, pilot.Plan, deltaTime);
        }
    }
}
