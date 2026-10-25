using System.Numerics;
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
    /// <returns>True when the bolt is spent: it hit something, or its time ran out.</returns>
    private bool Advance(GameWorld world, Bolt bolt, float deltaTime)
    {
        var from = bolt.Position;
        bolt.Velocity += physics.GetGravity(world, bolt.Position) * deltaTime;
        bolt.Position += bolt.Velocity * deltaTime;
        bolt.TimeRemaining -= deltaTime;

        // The hit test runs before expiry, so a bolt gets a last look at what it reaches on its final frame.
        return ResolveHit(world, bolt, from, bolt.Position) || bolt.TimeRemaining <= 0f;
    }

    /// <summary>
    /// What the bolt's path this frame runs into. <see cref="T"/> is how far along the path (0 to 1) it happens.
    /// A <see cref="Ship"/> or <see cref="Structure"/> takes damage and flashes; with neither, the bolt is only absorbed.
    /// </summary>
    private readonly record struct HitEvent(float T, Ship? Ship = null, PlanetStructure? Structure = null);

    private bool ResolveHit(GameWorld world, Bolt bolt, Vector2 from, Vector2 to)
    {
        HitEvent? first = null;
        foreach (var candidate in ShipEvents(world, bolt, from, to))
        {
            if (first is null || candidate.T < first.Value.T)
            {
                first = candidate;
            }
        }

        if (first is not { } hit)
        {
            return false;
        }

        if (hit.Ship is { } ship)
        {
            ship.Health = Math.Max(0, ship.Health - settings.Damage);
            world.Impacts.Add(new Impact(Vector2.Lerp(from, to, hit.T)));
        }

        return true;
    }

    private IEnumerable<HitEvent> ShipEvents(GameWorld world, Bolt bolt, Vector2 from, Vector2 to)
    {
        foreach (var ship in world.Ships)
        {
            if (ReferenceEquals(ship, bolt.Shooter) || (!settings.FriendlyFire && ship.Faction == bolt.Faction))
            {
                continue;
            }

            if (Sweep.SegmentCircle(from, to, ship.Position, ship.CollisionRadius, out var enter, out _))
            {
                yield return new HitEvent(enter, ship);
            }
        }
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
