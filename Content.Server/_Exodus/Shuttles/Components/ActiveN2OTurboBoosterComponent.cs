namespace Content.Server._Exodus.Shuttles.Components;

/// <summary>
/// Marker placed on a shuttle grid while at least one of its N2O turbo boosters is active.
/// Holds the combined thrust multiplier consumed by the MoverController thrust boost hook.
/// Rebuilt by <see cref="Systems.N2OTurboBoosterSystem"/> whenever a booster starts or stops,
/// so the hot path only has to dispatch a directed event when the grid is actually boosted.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveN2OTurboBoosterComponent : Component
{
    /// <summary>
    /// Combined thrust multiplier for every engine of this shuttle.
    /// </summary>
    [ViewVariables]
    public float Multiplier = 1f;
}
