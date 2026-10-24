using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Fighters;

/// <summary>A fighter that does nothing: it never steers, thrusts, docks or claims, and has no target.</summary>
internal sealed class NullFighterStrategy : IAutopilotStrategy<Fighter>
{
    public Vector2 InitialHoldPosition(GameWorld world, Fighter ship) => ship.Position;

    public ShipControls Decide(GameWorld world, Fighter fighter, FlightPlan plan, float deltaTime) => default;
}
