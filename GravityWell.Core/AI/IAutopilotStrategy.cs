using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

/// <summary>
/// How an AI-flown ship decides what to do. <see cref="ShipAutopilot{TShip}"/> supplies which ships there
/// are, which strategy each one uses, and the flight plan each keeps.
/// </summary>
public interface IAutopilotStrategy<TShip> where TShip : Ship
{
    /// <summary>Where a ship holds until it has something to do, fixed when it is first seen.</summary>
    Vector2 InitialHoldPosition(GameWorld world, TShip ship);

    ShipControls Decide(GameWorld world, TShip ship, FlightPlan plan, float deltaTime);
}
