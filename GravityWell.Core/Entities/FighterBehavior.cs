namespace GravityWell.Core.Entities;

/// <summary>
/// How the AI flies a fighter. It is fixed when the fighter spawns and has no effect on the player's fighter.
/// Each value needs a strategy registered in <c>FighterAutopilot</c>.
/// </summary>
public enum FighterBehavior
{
    /// <summary>No weapons to use: it claims free planets and holds when none are left.</summary>
    Peaceful,

    /// <summary>Does nothing: it gives no controls and never claims a planet.</summary>
    Null
}
