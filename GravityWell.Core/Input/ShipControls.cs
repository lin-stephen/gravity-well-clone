namespace GravityWell.Core.Input;

// Turn is -1..1, throttle is 0..1. Controllers request motion; physics owns it.
public readonly record struct ShipControls(float Turn, float Throttle);
