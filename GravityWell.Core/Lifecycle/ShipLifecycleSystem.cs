using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Lifecycle;

/// <summary>Removes destroyed ships and returns fighters to their last friendly shipyard.</summary>
public sealed class ShipLifecycleSystem
{
    public const float RespawnDelaySeconds = 5f;
    private readonly Dictionary<Fighter, ICollisionBody> respawnPoints = [];
    private readonly Dictionary<Fighter, float> respawnTimers = [];

    public void Update(GameWorld world, float deltaTime)
    {
        var elapsed = float.IsFinite(deltaTime) ? Math.Max(0f, deltaTime) : 0f;
        foreach (var explosion in world.Explosions)
        {
            explosion.Age += elapsed;
        }
        world.Explosions.RemoveAll(explosion => explosion.Age >= Explosion.DurationSeconds);

        foreach (var fighter in world.AiFighters.Prepend(world.Player))
        {
            if (!fighter.IsDestroyed)
            {
                respawnTimers.Remove(fighter);
                if (fighter.IsDocked && fighter.DockingTarget is { } dock && CanRespawn(fighter, dock))
                {
                    respawnPoints[fighter] = dock;
                }
                else if (!respawnPoints.ContainsKey(fighter))
                {
                    var home = world.Planets.FirstOrDefault(planet =>
                        planet.Owner == fighter.Faction && CanRespawn(fighter, planet));
                    if (home is not null)
                    {
                        respawnPoints[fighter] = home;
                    }
                }
                continue;
            }

            respawnTimers.TryGetValue(fighter, out var timer);
            timer += elapsed;
            respawnTimers[fighter] = timer;
            if (timer < RespawnDelaySeconds || !respawnPoints.TryGetValue(fighter, out var target) ||
                !CanRespawn(fighter, target))
            {
                continue;
            }

            fighter.Reset();
            fighter.Position = target.Position - Vector2.UnitY * (target.CollisionRadius + Fighter.Radius);
            fighter.Velocity = target.Velocity;
            fighter.Rotation = 0f;
            fighter.BeginDocking(target, -Vector2.UnitY);
            fighter.CompleteDocking();
            respawnTimers.Remove(fighter);
        }

        ResolveDestruction(world);
    }

    public void ResolveDestruction(GameWorld world)
    {
        // Docking can finish on the same tick as a lethal hit, after Update recorded the previous landing.
        foreach (var fighter in world.Fighters)
        {
            if (fighter.IsDocked && fighter.DockingTarget is { } target && CanRespawn(fighter, target))
            {
                respawnPoints[fighter] = target;
            }
        }

        foreach (var ship in world.Ships.Where(ship => ship.Health <= 0).ToArray())
        {
            world.Explosions.Add(new Explosion(ship.Position));
            if (ship is Freighter freighter)
            {
                freighter.BuildSite?.Cancel();
                freighter.Abandon();
            }
            ship.Destroy();
            if (ship is Fighter fighter)
            {
                respawnTimers[fighter] = 0f;
            }
        }
        world.RemoveDestroyedFreighters();
    }

    private static bool CanRespawn(Fighter fighter, ICollisionBody target) => target switch
    {
        Planet planet => planet.Health > 0 && planet.Owner == fighter.Faction &&
                         planet.HasOperationalStructure(StructureType.TechCenter),
        SpaceDockBody dock => dock.Planet.Health > 0 && dock.Planet.Owner == fighter.Faction &&
                              dock.Planet.HasOperationalStructure(StructureType.SpaceDock),
        _ => false
    };
}
