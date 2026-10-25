using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.World;

namespace GravityWell.Core.Physics;

public sealed class PhysicsSystem
{
    private readonly PhysicsSettings settings;

    public PhysicsSystem(PhysicsSettings? settings = null)
    {
        this.settings = settings ?? new PhysicsSettings();
        this.settings.Validate();
    }

    public void Update(GameWorld world, PlayerControls controls, float deltaTime)
    {
        if (controls.Reset)
        {
            world.Player.Reset();
        }

        world.Player.Controls = new ShipControls(
            (controls.TurnRight ? 1f : 0f) - (controls.TurnLeft ? 1f : 0f),
            controls.Thrust ? 1f : 0f);
        foreach (var ship in world.Ships)
        {
            if (controls.Reset && ReferenceEquals(ship, world.Player))
            {
                continue;
            }

            Integrate(ship, world, deltaTime);
        }
    }

    private void Integrate(Ship ship, GameWorld world, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        var throttle = Math.Clamp(ship.Controls.Throttle, 0f, 1f);
        if (ship.DockingTarget is not null)
        {
            if (throttle <= 0f)
            {
                return;
            }

            ship.Undock();
        }
        ship.Rotation = MathF.IEEERemainder(ship.Rotation +
            Math.Clamp(ship.Controls.Turn, -1f, 1f) * ship.TurnRate * deltaTime, 360f);
        ship.Velocity += (ship.Forward * (throttle * ship.ThrustAcceleration) +
            GetGravity(world, ship.Position)) * deltaTime;
        if (ship.Velocity.LengthSquared() > ship.SpeedLimit * ship.SpeedLimit)
        {
            ship.Velocity = Vector2.Normalize(ship.Velocity) * ship.SpeedLimit;
        }

        ship.Position += ship.Velocity * deltaTime;
    }

    public Vector2 GetGravity(GameWorld world, Vector2 position)
    {
        var acceleration = Vector2.Zero;
        foreach (var planet in world.Planets)
        {
            var offset = planet.Position - position;
            var distanceSquared = offset.LengthSquared();
            if (distanceSquared > 0f)
            {
                acceleration += Vector2.Normalize(offset) *
                    (settings.PlanetGravityStrength / MathF.Max(distanceSquared, settings.MinimumGravityDistanceSquared));
            }
        }
        return acceleration;
    }
}
