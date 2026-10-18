using GravityWell.Colonization;
using GravityWell.Input;
using GravityWell.Physics;
using GravityWell.Rendering;
using GravityWell.World;

namespace GravityWell.Game;

public sealed class GravityWellGame
{
    private readonly GameWorld world = new();
    private readonly InputSystem input = new();
    private readonly PhysicsSystem physics = new();
    private readonly ColonizationSystem colonization = new();
    private readonly FreighterSystem freighters = new();
    private readonly GameRenderer renderer = new();
    private bool showHelp = true;

    public void Initialize() => renderer.Initialize();
    public void Resize(int width, int height) => renderer.Resize(width, height);

    public void Tick(float deltaTime)
    {
        var controls = input.ReadControls();
        if (controls.ToggleHelp)
            showHelp = !showHelp;

        physics.Update(world, controls, deltaTime);
        colonization.Update(world);
        freighters.Update(world, deltaTime);
        renderer.Draw(world, controls, showHelp);
    }
}
