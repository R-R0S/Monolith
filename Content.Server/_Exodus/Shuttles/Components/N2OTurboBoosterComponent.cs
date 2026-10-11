using Content.Server._Exodus.Shuttles.Systems;
using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Exodus.Shuttles.Components;

/// <summary>
/// Nitrous oxide turbo booster: a large shuttle installation that stores N2O piped in from the
/// gas network and, while active, supercharges every engine on the grid it is bolted to.
/// Controlled either by a context menu verb or by a device link signal (e.g. a signal button).
/// </summary>
[RegisterComponent, Access(typeof(N2OTurboBoosterSystem))]
public sealed partial class N2OTurboBoosterComponent : Component
{
    /// <summary>
    /// Internal N2O reserve. Refilled from the pipe network through <see cref="InletName"/>.
    /// </summary>
    [DataField]
    public GasMixture Storage = new();

    /// <summary>
    /// How many moles of gas the internal storage can hold at most.
    /// </summary>
    [DataField]
    public float Capacity = 1000f;

    /// <summary>
    /// Moles of N2O consumed per second while the booster is active.
    /// </summary>
    [DataField]
    public float FuelConsumption = 8f;

    /// <summary>
    /// Multiplier applied to the thrust of every engine on the grid while the booster is active.
    /// </summary>
    [DataField]
    public float BoostMultiplier = 1.5f;

    /// <summary>
    /// Whether the booster is currently boosting its grid.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// Identifier of the pipe node that receives gas from the pipe network.
    /// </summary>
    [DataField]
    public string InletName = "inlet";

    /// <summary>
    /// Device link sink ports used to control the booster remotely.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <inheritdoc cref="OnPort"/>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <inheritdoc cref="OnPort"/>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Device link source port reporting whether the booster is running.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> StatusPort = "Status";
}
