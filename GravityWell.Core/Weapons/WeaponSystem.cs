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
        foreach (var candidate in ShipEvents(world, bolt, from, to).Concat(PlanetEvents(world, bolt, from, to)))
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
        else if (hit.Structure is { } structure)
        {
            structure.TakeDamage(settings.Damage);
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

    /// <summary>
    /// A planet is a one-way door for bolts. One this shooter can hurt (an enemy's, or any with friendly fire on,
    /// with a structure still standing) lets the bolt in: each standing structure can be hit, and the bolt is absorbed
    /// where it would come out the far side. Any other planet absorbs the bolt on entry, and a friendly planet's own
    /// structures, such as a Starbase outside the circle, absorb it too.
    /// </summary>
    private IEnumerable<HitEvent> PlanetEvents(GameWorld world, Bolt bolt, Vector2 from, Vector2 to)
    {
        foreach (var planet in world.Planets)
        {
            var friendly = planet.Owner == bolt.Faction;
            var hostile = planet.Owner is not null && (!friendly || settings.FriendlyFire) &&
                planet.Structures.Any(structure => structure.IsTargetable);
            if (hostile || friendly)
            {
                // A hostile structure takes the hit. A friendly one only absorbs the bolt.
                foreach (var structure in planet.Structures)
                {
                    if (structure.IsTargetable &&
                        Sweep.SegmentBox(from, to, StructureLayout.GetBounds(planet, structure.Type), out var enter))
                    {
                        yield return new HitEvent(enter, Structure: hostile ? structure : null);
                    }
                }
            }

            if (!hostile)
            {
                if (Sweep.SegmentCircle(from, to, planet.Position, Planet.Radius, out var entry, out _))
                {
                    yield return new HitEvent(entry);
                }

                continue;
            }

            // The bolt is out when the float the next frame will read says so, not when a rounded parameter
            // says so: a bolt left just inside the edge is still inside next frame, so it is caught then.
            if (Sweep.SegmentCircle(from, to, planet.Position, Planet.Radius, out _, out var exit) &&
                (exit <= 1f || Vector2.DistanceSquared(to, planet.Position) >= Planet.Radius * Planet.Radius))
            {
                yield return new HitEvent(MathF.Min(exit, 1f));
            }
        }
    }
}
