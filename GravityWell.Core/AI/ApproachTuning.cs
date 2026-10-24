using GravityWell.Core.Entities;

namespace GravityWell.Core.AI;

/// <summary>Per-ship-class values that shape how <see cref="PlanetApproach"/> flies to a planet and docks.</summary>
public sealed record ApproachTuning
{
    public float CruiseSpeed { get; init; }              // Speed cap on the final leg of the route.
    public float RouteWaypointSpeed { get; init; }       // Speed cap while intermediate waypoints remain.
    public float ApproachClearance { get; init; }        // Gap between the hull and the approach point.
    public float CorridorBreakDistance { get; init; }    // Farther than this from the surface, abandon Align/Approach.
    public float CorridorBreakSpeed { get; init; }       // Faster than this, abandon Align/Approach.
    public float AlignedSidewaysSpeed { get; init; }     // Sideways drift allowed to start the final approach.
    public float HoverSpeed { get; init; }               // Outward speed used to hold off the surface while aligning.
    public float TouchdownSpeed { get; init; }           // Closing speed during the final approach.
    public float WaypointReachedDistance { get; init; }
    public float AlignTriggerDistance { get; init; }     // Distance to the last waypoint that starts Align...
    public float AlignTriggerSpeed { get; init; }        // ...provided the ship is slower than this.
    public float BrakeDeceleration { get; init; }        // Deceleration assumed when slowing for a destination.
    public float BrakeTurnSeconds { get; init; }         // Time reserved to turn rear-first before braking.

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
    };
}
