using Raylib_cs;

namespace GravityWell.Input;

public sealed class InputSystem
{
    public PlayerControls ReadControls() => new(
        Raylib.IsKeyDown(KeyboardKey.Left),
        Raylib.IsKeyDown(KeyboardKey.Right),
        Raylib.IsKeyDown(KeyboardKey.Up),
        Raylib.IsKeyPressed(KeyboardKey.Home));
}
