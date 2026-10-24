using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.AI.Fighters;
using GravityWell.Core.AI.Freighters;
using GravityWell.Core.Colonization;
using GravityWell.Core.Economy;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI;

/// <summary>The whole AI economy: a fighter claims planets and the claiming faction's freighters build on them.</summary>
public class AiExpansionTests
{
    private const float Dt = 1f / 60f;

    [Test]
    public void DefaultWorld_RedExpandsToEveryFreePlanet()
    {
        var world = new GameWorld();
        // The player gets no input here, so park it far away: left at its start it would drift under
        // gravity and could crash into a planet, which has nothing to do with what this test measures.
        world.Player.Position = new Vector2(10000f, 10000f);
        world.Player.Velocity = Vector2.Zero;

        // The systems GravityWellGame.Tick runs, in the same order, without input or rendering.
        var freighterAutopilot = new FreighterAutopilot();
        var fighterAutopilot = new FighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var colonization = new ColonizationSystem();
        var freighters = new FreighterSystem();
        var economy = new EconomySystem();
        var repairs = new RepairSystem();
        for (var step = 0; step < (int)(600f / Dt); step++)
        {
            freighterAutopilot.Update(world, Dt);
            fighterAutopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            colonization.Update(world);
            freighters.Update(world, Dt);
            economy.Update(world, Dt);
            repairs.Update(world, Dt);
        }

        world.Planets.Should().OnlyContain(planet => planet.IsColonized);
        world.Planets.Count(planet => planet.Owner == Faction.Blue).Should().Be(1);
        var redPlanets = world.Planets.Where(planet => planet.Owner == Faction.Red).ToList();
        redPlanets.Should().HaveCount(world.Planets.Count - 1);
        redPlanets.Should().OnlyContain(planet => planet.HasOperationalStructure(StructureType.Base));
        world.AiFighters.Should().OnlyContain(fighter => fighter.Health == Fighter.MaxHealth);
    }
}
