using GravityWell.Core.Entities;

namespace GravityWell.Core.AI;

/// <summary>Per-ship-class values that shape how <see cref="PlanetApproach"/> flies to a planet and docks.</summary>
public sealed record ApproachTuning
{
    public required float CruiseSpeed { get; init; }              // Speed cap on the final leg of the route.
    public required float RouteWaypointSpeed { get; init; }       // Speed cap while intermediate waypoints remain.
    public required float ApproachClearance { get; init; }        // Gap between the hull and the approach point.
    public required float CorridorBreakDistance { get; init; }    // Farther than this from the surface, abandon Align/Approach.
    public required float CorridorBreakSpeed { get; init; }       // Faster than this, abandon Align/Approach.
    public required float AlignedSidewaysSpeed { get; init; }     // Sideways drift allowed to start the final approach.
    public required float HoverSpeed { get; init; }               // Outward speed used to hold off the surface while aligning.
    public required float TouchdownSpeed { get; init; }           // Closing speed during the final approach.
    public required float WaypointReachedDistance { get; init; }
    public required float AlignTriggerDistance { get; init; }     // Distance to the last waypoint that starts Align...
    public required float AlignTriggerSpeed { get; init; }        // ...provided the ship is slower than this.
    public required float BrakeDeceleration { get; init; }        // Deceleration assumed when slowing for a destination.
    public required float BrakeTurnSeconds { get; init; }         // Time reserved to turn rear-first before braking.
    public required float HoldSpeed { get; init; }                // Speed cap when returning to a hold position.

    public static ApproachTuning ForFreighter { get; } = new()
    {
        CruiseSpeed = Freighter.Speed,
        RouteWaypointSpeed = 55f,
        ApproachClearance = 90f,
        CorridorBreakDistance = 150f,
        CorridorBreakSpeed = 65f,
        AlignedSidewaysSpeed = 5f,
        HoverSpeed = 20f,
        TouchdownSpeed = 12f,
        WaypointReachedDistance = 20f,
        AlignTriggerDistance = 18f,
        AlignTriggerSpeed = 15f,
        BrakeDeceleration = 40f,
        BrakeTurnSeconds = 1.2f,
        HoldSpeed = 20f,
    };

    // The freighter's tuning, raised to the fighter's limits. Seek caps speed at sqrt(BrakeDeceleration * d),
    // which implies a braking rate of BrakeDeceleration / 2, so 160 means 80 u/s^2 against the fighter's
    // 230 thrust. The freighter's 40 would hold a fighter near 150 on typical legs, and its 55 waypoint cap
    // is below freighter pace. FighterApproachTests fly the fighter to prove both speed and safety.
    public static ApproachTuning ForFighter { get; } = ForFreighter with
    {
        CruiseSpeed = Fighter.MaxSpeed,
        RouteWaypointSpeed = 180f,
        BrakeDeceleration = 160f,
    };
}
