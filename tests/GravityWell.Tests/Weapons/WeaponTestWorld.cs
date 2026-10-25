using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.Weapons;
using GravityWell.Core.World;

namespace GravityWell.Tests.Weapons;

/// <summary>Builds small worlds for weapon tests: the player at the origin facing +x, no gravity unless asked for.</summary>
internal static class WeaponTestWorld
{
    public static readonly PlayerControls Idle = default;
    public static readonly PlayerControls Fire = new(false, false, false, false, false, true);

    public static WeaponSettings Settings(Func<WeaponSettings, WeaponSettings>? tweak = null)
    {
        var settings = new WeaponSettings { BoltSpeed = 500f, BoltLifetime = 2f, FireInterval = 0.25f, ImpactDuration = 10f };
        return tweak?.Invoke(settings) ?? settings;
    }

    public static GameWorld CreateWorld(IEnumerable<Planet>? planets = null, IEnumerable<Freighter>? freighters = null,
        IEnumerable<Fighter>? aiFighters = null)
    {
        var world = new GameWorld(planets ?? [new Planet(new Vector2(5000f, 5000f))], freighters ?? [], aiFighters ?? []);
        world.Player.Position = Vector2.Zero;
        world.Player.Velocity = Vector2.Zero;
        world.Player.Rotation = 90f;
        return world;
    }

    public static WeaponSystem CreateSystem(WeaponSettings? settings = null, bool gravity = false) =>
        new(new PhysicsSystem(gravity ? null : new PhysicsSettings { PlanetGravityStrength = 0f }),
            settings ?? Settings());

    /// <summary>Steps the system, firing on the first step only when <paramref name="fire"/> is set.</summary>
    public static void Run(WeaponSystem system, GameWorld world, float seconds, bool fire = false, float step = 0.02f)
    {
        var steps = (int)MathF.Round(seconds / step);
        for (var i = 0; i < steps; i++)
        {
            system.Update(world, fire && i == 0 ? Fire : Idle, step);
        }
    }

    public static Fighter Enemy(Vector2 position, Faction faction = Faction.Red) =>
        new() { Faction = faction, Position = position, Velocity = Vector2.Zero };

    public static void ShouldBeNear(this Vector2 actual, Vector2 expected, float tolerance = 0.001f)
    {
        actual.X.Should().BeApproximately(expected.X, tolerance);
        actual.Y.Should().BeApproximately(expected.Y, tolerance);
    }
}
