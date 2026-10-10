using System.Numerics;
using GravityWell.Entities;
using GravityWell.Input;
using GravityWell.World;

namespace GravityWell.Physics;

public sealed class PhysicsSystem
{
    private const float GravityStrength = 1_350_000f;
    private const float ThrustStrength = 230f;
    private const float TurnSpeed = 2.8f;

    public void Update(GameWorld world, PlayerControls controls, float deltaTime)
    {
        var fighter = world.Player;

        if (controls.TurnLeft) fighter.Rotation -= TurnSpeed * deltaTime * 60f;
        if (controls.TurnRight) fighter.Rotation += TurnSpeed * deltaTime * 60f;
        if (controls.Thrust) fighter.Velocity += fighter.Forward * ThrustStrength * deltaTime;

        ApplyGravity(fighter, GameWorld.StarPosition, GravityStrength, deltaTime);
        ApplyGravity(fighter, GameWorld.PlanetPosition, GravityStrength * 0.08f, deltaTime);
        fighter.Position += fighter.Velocity * deltaTime;

        if (controls.Reset) fighter.Reset();
    }

    private static void ApplyGravity(
        Fighter fighter,
        Vector2 bodyPosition,
        float strength,
        float deltaTime)
    {
        var offset = bodyPosition - fighter.Position;
        var distanceSquared = MathF.Max(offset.LengthSquared(), 2_500f);
        fighter.Velocity += Vector2.Normalize(offset) * (strength / distanceSquared) * deltaTime;
    }
}
