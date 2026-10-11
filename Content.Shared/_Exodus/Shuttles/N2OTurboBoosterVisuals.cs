using Robust.Shared.Serialization;

namespace Content.Shared._Exodus.Shuttles;

/// <summary>
/// Appearance keys for the N2O turbo booster.
/// </summary>
[Serializable, NetSerializable]
public enum N2OTurboBoosterVisuals : byte
{
    /// <summary>
    /// True while the booster is active.
    /// </summary>
    Active,
}

/// <summary>
/// Sprite layer keys for the N2O turbo booster.
/// </summary>
public enum N2OTurboBoosterVisualLayers : byte
{
    Base,
    Thrust,
}
