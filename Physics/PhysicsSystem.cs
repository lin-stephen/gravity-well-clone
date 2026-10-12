using System.Numerics;
using GravityWell.Entities;
using GravityWell.Input;
using GravityWell.World;

namespace GravityWell.Physics;

public sealed class PhysicsSystem
{
    private const float PlanetGravityStrength = 1_350_000f * 0.08f;
    private const float ThrustStrength = 230f;
    private const float TurnSpeed = 2.8f;

    public void Update(GameWorld world, PlayerControls controls, float deltaTime)
    {
        var fighter = world.Player;

        if (controls.TurnLeft) fighter.Rotation -= TurnSpeed * deltaTime * 60f;
        if (controls.TurnRight) fighter.Rotation += TurnSpeed * deltaTime * 60f;
        if (controls.Thrust) fighter.Velocity += fighter.Forward * ThrustStrength * deltaTime;

        foreach (var planet in world.Planets)
            ApplyGravity(fighter, planet.Position, PlanetGravityStrength, deltaTime);
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
