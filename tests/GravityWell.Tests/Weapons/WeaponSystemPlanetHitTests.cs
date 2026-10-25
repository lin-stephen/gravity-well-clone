using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Weapons;
using GravityWell.Core.World;

namespace GravityWell.Tests.Weapons;

public class WeaponSystemPlanetHitTests
{
    private static readonly Vector2 PlanetPosition = new(300f, 0f);

    private static Planet CreatePlanet(Faction? owner) => new(PlanetPosition, owner: owner);

    private static PlanetStructure Add(Planet planet, StructureType type, StructureState state = StructureState.Operational) =>
        planet.AddStructure(type, state);

    /// <summary>The planet at (300, 0) and a Red fighter behind it at (500, y), the witness for "nothing passes through".</summary>
    private static (GameWorld World, Fighter Witness) CreateWorld(Planet planet, float witnessY = 0f)
    {
        var witness = WeaponTestWorld.Enemy(new Vector2(500f, witnessY));
        return (WeaponTestWorld.CreateWorld([planet], aiFighters: [witness]), witness);
    }

    private static void ShootAlongX(GameWorld world, WeaponSystem? system = null, float seconds = 1.2f)
    {
        WeaponTestWorld.Run(system ?? WeaponTestWorld.CreateSystem(), world, seconds, fire: true);
    }

    [Test]
    public void UnownedPlanet_AbsorbsBolt()
    {
        var (world, witness) = CreateWorld(CreatePlanet(null));

        ShootAlongX(world);

        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void OwnPlanet_AbsorbsBoltWithoutDamage()
    {
        var planet = CreatePlanet(Faction.Blue);
        var planetBase = Add(planet, StructureType.Base);
        var (world, witness) = CreateWorld(planet);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void EnemyPlanetWithNoStructures_AbsorbsBolt()
    {
        var (world, witness) = CreateWorld(CreatePlanet(Faction.Red));

        ShootAlongX(world);

        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void EnemyPlanetWithOnlyAPlannedStructure_AbsorbsBolt()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base, StructureState.Planned);
        var (world, witness) = CreateWorld(planet);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void EnemyBase_TakesDamage_AndBoltIsGone()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var (world, witness) = CreateWorld(planet);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().ContainSingle();
    }

    [Test]
    public void EnemyBuildingStructure_TakesDamage()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base, StructureState.Building);
        var (world, _) = CreateWorld(planet);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
    }

    [Test]
    public void StructureInFront_ShieldsStructureBehind()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var colony = Add(planet, StructureType.Colony);
        var world = WeaponTestWorld.CreateWorld([planet]);
        world.Player.Position = new Vector2(300f, 200f);
        world.Player.Rotation = 0f;

        ShootAlongX(world, seconds: 0.6f);

        colony.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
    }

    [Test]
    public void DestroyedStructure_NoLongerObstructs()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var colony = Add(planet, StructureType.Colony);
        colony.TakeDamage(PlanetStructure.MaxHealth);
        var world = WeaponTestWorld.CreateWorld([planet]);
        world.Player.Position = new Vector2(300f, 200f);
        world.Player.Rotation = 0f;

        ShootAlongX(world, seconds: 0.6f);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        colony.Health.Should().Be(0);
    }

    [Test]
    public void FromAbove_StarbaseShieldsPlanet()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var starbase = Add(planet, StructureType.Starbase);
        var world = WeaponTestWorld.CreateWorld([planet]);
        world.Player.Position = new Vector2(300f, -300f);
        world.Player.Rotation = 180f;

        ShootAlongX(world, seconds: 0.6f);

        starbase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
    }

    [Test]
    public void OffCentreBolt_ThroughGap_IsAbsorbedAtFarEdge()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var (world, witness) = CreateWorld(planet, witnessY: 20f);
        world.Player.Position = new Vector2(0f, 20f);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void BoltStartingInsidePlanet_IsAbsorbedAtFarEdge()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var (world, witness) = CreateWorld(planet, witnessY: 20f);
        world.Player.Position = new Vector2(280f, 20f);

        ShootAlongX(world);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void AllStructuresDestroyed_PlanetAbsorbsAtEdge()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        planetBase.TakeDamage(PlanetStructure.MaxHealth);
        var (world, witness) = CreateWorld(planet);

        ShootAlongX(world);

        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void FriendlyStarbase_AbsorbsBoltFromAbove()
    {
        var planet = CreatePlanet(Faction.Blue);
        var planetBase = Add(planet, StructureType.Base);
        var starbase = Add(planet, StructureType.Starbase);
        var world = WeaponTestWorld.CreateWorld([planet]);
        world.Player.Position = new Vector2(300f, -300f);
        world.Player.Rotation = 180f;

        ShootAlongX(world, seconds: 0.6f);

        starbase.Health.Should().Be(PlanetStructure.MaxHealth);
        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
        world.Bolts.Should().BeEmpty();
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void FriendlyFireOn_DamagesOwnPlanetStructures()
    {
        var planet = CreatePlanet(Faction.Blue);
        var planetBase = Add(planet, StructureType.Base);
        var (world, _) = CreateWorld(planet);
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { FriendlyFire = true }));

        ShootAlongX(world, system);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
    }

    [Test]
    public void ShipInFrontOfPlanet_IsHitBeforePlanet()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var blocker = WeaponTestWorld.Enemy(new Vector2(200f, 0f));
        var world = WeaponTestWorld.CreateWorld([planet], aiFighters: [blocker]);

        ShootAlongX(world);

        blocker.Health.Should().Be(Fighter.MaxHealth - 1);
        planetBase.Health.Should().Be(PlanetStructure.MaxHealth);
    }

    [Test]
    public void FastBolt_CannotSkipAThinStructure()
    {
        var planet = CreatePlanet(Faction.Red);
        var planetBase = Add(planet, StructureType.Base);
        var world = WeaponTestWorld.CreateWorld([planet]);
        var system = WeaponTestWorld.CreateSystem(
            WeaponTestWorld.Settings(s => s with { BoltSpeed = 20000f, BoltLifetime = 1f }));
        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        planetBase.Health.Should().Be(PlanetStructure.MaxHealth - 1);
    }

    [Test]
    public void FastBolt_IsStillAbsorbedByAnUnownedPlanet()
    {
        var (world, witness) = CreateWorld(CreatePlanet(null));
        var system = WeaponTestWorld.CreateSystem(
            WeaponTestWorld.Settings(s => s with { BoltSpeed = 20000f, BoltLifetime = 1f }));
        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        witness.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Bolt_NeverComesOutTheFarSide_AcrossStepSizesAndOffsets()
    {
        float[] steps = [0.004f, 0.007f, 0.0133f, 0.016f, 0.02f, 0.033f, 0.05f];
        float[] offsets = [12f, 15f, 18f, 21f, 24f, 27f, 30f, 33f];
        foreach (var owner in new Faction?[] { null, Faction.Red })
        {
            foreach (var step in steps)
            {
                foreach (var offset in offsets)
                {
                    var planet = CreatePlanet(owner);
                    if (owner is not null)
                    {
                        Add(planet, StructureType.Base);
                    }

                    var (world, witness) = CreateWorld(planet, witnessY: offset);
                    world.Player.Position = new Vector2(0f, offset);
                    var system = WeaponTestWorld.CreateSystem();

                    WeaponTestWorld.Run(system, world, 1.2f, fire: true, step: step);

                    witness.Health.Should().Be(Fighter.MaxHealth, $"owner {owner}, step {step}, offset {offset}");
                }
            }
        }
    }

    [Test]
    public void FriendlyStarbase_AbsorbsBoltFlyingSidewaysThroughIt()
    {
        var planet = CreatePlanet(Faction.Blue);
        Add(planet, StructureType.Base);
        var starbase = Add(planet, StructureType.Starbase);
        var (world, witness) = CreateWorld(planet, witnessY: -54f);
        world.Player.Position = new Vector2(0f, -54f);

        ShootAlongX(world);

        witness.Health.Should().Be(Fighter.MaxHealth);
        starbase.Health.Should().Be(PlanetStructure.MaxHealth);
        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void FriendlyDestroyedStarbase_NoLongerAbsorbs()
    {
        var planet = CreatePlanet(Faction.Blue);
        Add(planet, StructureType.Base);
        var starbase = Add(planet, StructureType.Starbase);
        starbase.TakeDamage(PlanetStructure.MaxHealth);
        var (world, witness) = CreateWorld(planet, witnessY: -54f);
        world.Player.Position = new Vector2(0f, -54f);

        ShootAlongX(world);

        witness.Health.Should().Be(Fighter.MaxHealth - 1);
    }

    [Test]
    public void EnemyPlanetWithOnlyAPlannedStructure_AbsorbsAtEntryNotExit()
    {
        var planet = CreatePlanet(Faction.Red);
        Add(planet, StructureType.Base, StructureState.Planned);
        var (world, _) = CreateWorld(planet);

        ShootAlongX(world, seconds: 0.54f);

        world.Bolts.Should().BeEmpty("the bolt is absorbed at x = 266, before it is deep inside the circle");
    }

    [Test]
    public void EnemyPlanetWithEveryStructureDestroyed_AbsorbsAtEntryNotExit()
    {
        var planet = CreatePlanet(Faction.Red);
        Add(planet, StructureType.Base).TakeDamage(PlanetStructure.MaxHealth);
        var (world, _) = CreateWorld(planet);

        ShootAlongX(world, seconds: 0.54f);

        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void EnemyPlanetWithAStandingStructure_LetsTheBoltInPastTheEdge()
    {
        var planet = CreatePlanet(Faction.Red);
        Add(planet, StructureType.Base);
        var (world, _) = CreateWorld(planet, witnessY: 20f);
        world.Player.Position = new Vector2(0f, 20f);

        ShootAlongX(world, seconds: 0.54f);

        world.Bolts.Should().ContainSingle("a gap shot flies inside the circle until it comes to the far edge");
    }

    [Test]
    public void Bolt_NeverComesOutTheFarSide_OfAPlanetFarFromTheOrigin()
    {
        var random = new Random(12345);
        var farPlanetPosition = new Vector2(1900f, 900f);
        for (var trial = 0; trial < 3000; trial++)
        {
            var offset = 1f + (float)random.NextDouble() * 32f;
            var step = 0.002f + (float)random.NextDouble() * 0.018f;
            var planet = new Planet(farPlanetPosition, owner: Faction.Red);
            Add(planet, StructureType.Base);
            var witness = WeaponTestWorld.Enemy(farPlanetPosition + new Vector2(200f, offset));
            var world = WeaponTestWorld.CreateWorld([planet], aiFighters: [witness]);
            world.Player.Position = farPlanetPosition + new Vector2(-300f, offset);
            var system = WeaponTestWorld.CreateSystem();

            WeaponTestWorld.Run(system, world, 1.5f, fire: true, step: step);

            witness.Health.Should().Be(Fighter.MaxHealth, $"trial {trial}, offset {offset}, step {step}");
        }
    }
}
