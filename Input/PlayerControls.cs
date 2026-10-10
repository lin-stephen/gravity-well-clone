namespace GravityWell.Input;

public readonly record struct PlayerControls(
    bool TurnLeft,
    bool TurnRight,
    bool Thrust,
    bool Reset);
