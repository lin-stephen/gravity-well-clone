using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.Weapons;

/// <summary>Fires the player's cannon and flies, hits and expires its bolts.</summary>
public sealed class WeaponSystem
{
    private readonly PhysicsSystem physics;
    private readonly WeaponSettings settings;
    private float fireTimer;

    public WeaponSystem(PhysicsSystem physics, WeaponSettings? settings = null)
    {
        this.physics = physics;
        this.settings = settings ?? new WeaponSettings();
        this.settings.Validate();
    }

    /// <summary>
    /// Ages impacts, moves and resolves the bolts already in flight, then fires. A bolt fired in this call is not
    /// moved until the next one.
    /// </summary>
    public void Update(GameWorld world, PlayerControls controls, float deltaTime)
    {
        // A bad step (negative or not a number) must not stretch or switch off the cooldown.
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
        {
            deltaTime = 0f;
        }

        fireTimer = MathF.Max(0f, fireTimer - deltaTime);
        AgeImpacts(world, deltaTime);
        world.Bolts.RemoveAll(bolt => Advance(world, bolt, deltaTime));
        if (controls.Fire)
        {
            Fire(world);
        }
    }

    private void AgeImpacts(GameWorld world, float deltaTime)
    {
        foreach (var impact in world.Impacts)
        {
            impact.Age += deltaTime;
        }

        world.Impacts.RemoveAll(impact => impact.Age >= settings.ImpactDuration);
    }

    /// <summary>Flies the bolt one step, the way ships fly: gravity changes the velocity, then the velocity moves it.</summary>
    /// <returns>True when the bolt is spent and should be removed.</returns>
    private bool Advance(GameWorld world, Bolt bolt, float deltaTime)
    {
        bolt.Velocity += physics.GetGravity(world, bolt.Position) * deltaTime;
        bolt.Position += bolt.Velocity * deltaTime;
        bolt.TimeRemaining -= deltaTime;
        return bolt.TimeRemaining <= 0f;
    }

    private void Fire(GameWorld world)
    {
        if (fireTimer > 0f || world.Bolts.Count >= settings.MaxBolts)
        {
            return;
        }

        var player = world.Player;
        world.Bolts.Add(new Bolt(
            player,
            player.Position + player.Forward * Fighter.NoseDistance,
            player.Velocity + player.Forward * settings.BoltSpeed,
            settings.BoltLifetime));
        fireTimer = settings.FireInterval;
    }
}
