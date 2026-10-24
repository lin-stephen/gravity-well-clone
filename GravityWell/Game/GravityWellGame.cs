using GravityWell.Core.AI;
using GravityWell.Core.Colonization;
using GravityWell.Core.Economy;
using GravityWell.Core.Physics;
using GravityWell.Core.World;
using GravityWell.Input;
using GravityWell.Rendering;

namespace GravityWell.Game;

public sealed class GravityWellGame
{
    private readonly GameWorld world = new();
    private readonly InputSystem input = new();
    private readonly PhysicsSystem physics = new();
    private readonly CollisionSystem collisions = new();
    private readonly DockingSystem docking = new();
    private readonly ColonizationSystem colonization = new();
    private readonly FreighterSystem freighters = new();
    private readonly FreighterAutopilot autopilot;
    private readonly EconomySystem economy = new();
    private readonly RepairSystem repairs = new();
    private readonly GameRenderer renderer;
    private bool showHelp = true;

    public GravityWellGame()
    {
        autopilot = new FreighterAutopilot(docking, physics);
        renderer = new GameRenderer(docking);
    }

    public void Initialize() => renderer.Initialize();
    public void Resize(int width, int height) => renderer.Resize(width, height);

    public void Tick(float deltaTime)
    {
        var controls = input.ReadControls();
        if (controls.ToggleHelp)
            showHelp = !showHelp;

        autopilot.Update(world, deltaTime);
        physics.Update(world, controls, deltaTime);
        if (controls.Reset) collisions.Reset();
        docking.Update(world, deltaTime);
        collisions.Update(world);
        colonization.Update(world);
        freighters.Update(world, deltaTime);
        economy.Update(world, deltaTime);
        repairs.Update(world, deltaTime);
        renderer.Draw(world, controls, showHelp);
    }
}
