namespace Content.Server._Exodus.Shuttles.Events;

/// <summary>
/// Raised directed on a shuttle grid by MoverController every tick while the shuttle is piloted,
/// right before the averaged pilot multipliers are applied to the shuttle.
/// Subscribers can scale the power of every engine on that shuttle; default multipliers are 1.
/// </summary>
[ByRefEvent]
public record struct GetShuttleThrustBoostEvent(EntityUid ShuttleUid)
{
    /// <summary>
    /// Multiplier applied to the shuttle's linear engine thrust.
    /// </summary>
    public float LinearMultiplier = 1f;

    /// <summary>
    /// Multiplier applied to the shuttle's angular thrust (gyroscopes).
    /// </summary>
    public float AngularMultiplier = 1f;
}
