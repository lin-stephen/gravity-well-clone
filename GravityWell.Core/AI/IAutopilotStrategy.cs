using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>
/// What varies between kinds of AI-flown ship: which ships there are and how each one decides
/// what to do. <see cref="ShipAutopilot{TShip}"/> supplies everything else.
/// </summary>
public interface IAutopilotStrategy<TShip> where TShip : Ship
{
    /// <summary>The ships to fly this tick. A ship that is no longer listed loses its flight plan.</summary>
    IEnumerable<TShip> ShipsToPilot(GameWorld world);

    /// <summary>Where a ship holds until it has something to do, fixed when it is first seen.</summary>
    Vector2 InitialHoldPosition(GameWorld world, TShip ship);

    ShipControls Decide(GameWorld world, TShip ship, FlightPlan plan, float deltaTime);
}
