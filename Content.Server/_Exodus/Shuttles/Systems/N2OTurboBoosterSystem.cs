using Content.Server._Exodus.Shuttles.Components;
using Content.Server._Exodus.Shuttles.Events;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared._Exodus.Shuttles;
using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Exodus.Shuttles.Systems;

/// <summary>
/// Runs N2O turbo boosters: pulls N2O out of the connected pipe network into the internal
/// reserve, burns it while the booster is active and applies the resulting thrust multiplier
/// to every engine of the shuttle the booster is installed on.
/// </summary>
public sealed partial class N2OTurboBoosterSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<N2OTurboBoosterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<N2OTurboBoosterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<N2OTurboBoosterComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<N2OTurboBoosterComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<N2OTurboBoosterComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<N2OTurboBoosterComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<N2OTurboBoosterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<N2OTurboBoosterComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ActiveN2OTurboBoosterComponent, GetShuttleThrustBoostEvent>(OnGetThrustBoost);
    }

    private void OnInit(Entity<N2OTurboBoosterComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort, ent.Comp.OffPort, ent.Comp.TogglePort);
    }

    private void OnMapInit(Entity<N2OTurboBoosterComponent> ent, ref MapInitEvent args)
    {
        // Run a mapped-in active state through the regular path so fuel and anchor checks apply.
        if (!ent.Comp.Active)
            return;

        ent.Comp.Active = false;
        TrySetActive(ent, true);
    }

    private void OnShutdown(Entity<N2OTurboBoosterComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.Active)
            return;

        ent.Comp.Active = false;
        RefreshGridBoost(ent.Owner);
    }

    private void OnAnchorChanged(Entity<N2OTurboBoosterComponent> ent, ref AnchorStateChangedEvent args)
    {
        // An unanchored booster is neither piped into the network nor bolted to the grid.
        if (args.Anchored || !ent.Comp.Active)
            return;

        TrySetActive(ent, false);
        PopupAt(ent, null, "n2o-turbo-booster-unanchored-shutdown");
    }

    /// <summary>
    /// Refuels the internal reserve from the pipe network and burns N2O while active.
    /// </summary>
    private void OnAtmosUpdate(Entity<N2OTurboBoosterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        Refill(ent);

        if (!ent.Comp.Active)
            return;

        var stored = ent.Comp.Storage.GetMoles(Gas.NitrousOxide);
        var consumed = MathF.Min(stored, ent.Comp.FuelConsumption * args.dt);
        if (consumed <= 0f)
        {
            TrySetActive(ent, false);
            PopupAt(ent, null, "n2o-turbo-booster-out-of-fuel");
            return;
        }

        ent.Comp.Storage.AdjustMoles(Gas.NitrousOxide, -consumed);
    }

    private void OnSignalReceived(Entity<N2OTurboBoosterComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.TogglePort)
            Toggle(ent);
        else if (args.Port == ent.Comp.OnPort)
            TrySetActive(ent, true);
        else if (args.Port == ent.Comp.OffPort)
            TrySetActive(ent, false);
    }

    private void OnGetVerbs(Entity<N2OTurboBoosterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        var active = ent.Comp.Active;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString(active
                ? "n2o-turbo-booster-verb-deactivate"
                : "n2o-turbo-booster-verb-activate"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () =>
            {
                if (!Deleted(ent.Owner))
                    TrySetActive(ent, !active, user);
            },
            Priority = 5,
        };

        args.Verbs.Add(verb);
    }

    private void OnExamined(Entity<N2OTurboBoosterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var comp = ent.Comp;
        var moles = comp.Storage.GetMoles(Gas.NitrousOxide);
        var percent = comp.Capacity > 0f
            ? (int) MathF.Round(moles / comp.Capacity * 100f)
            : 0;

        args.PushMarkup(Loc.GetString("n2o-turbo-booster-examine-fuel",
            ("moles", MathF.Round(moles, 1)),
            ("percent", percent)));
        args.PushMarkup(Loc.GetString(comp.Active
            ? "n2o-turbo-booster-examine-active"
            : "n2o-turbo-booster-examine-inactive"));
    }

    /// <summary>
    /// Applies the shuttle's combined turbo booster multiplier, raised by MoverController.
    /// </summary>
    private void OnGetThrustBoost(Entity<ActiveN2OTurboBoosterComponent> ent, ref GetShuttleThrustBoostEvent args)
    {
        args.LinearMultiplier *= ent.Comp.Multiplier;
        args.AngularMultiplier *= ent.Comp.Multiplier;
    }

    /// <summary>
    /// Tries to activate or deactivate the booster.
    /// </summary>
    /// <param name="user">Entity to report failures and a manual state change to, if any.</param>
    /// <returns>True if the booster ended up in the requested state.</returns>
    public bool TrySetActive(Entity<N2OTurboBoosterComponent> ent, bool active, EntityUid? user = null)
    {
        if (Deleted(ent.Owner))
            return false;

        var comp = ent.Comp;
        if (comp.Active == active)
            return true;

        if (active)
        {
            if (!Transform(ent.Owner).Anchored)
            {
                PopupAt(ent, user, "n2o-turbo-booster-not-anchored");
                return false;
            }

            if (comp.Storage.GetMoles(Gas.NitrousOxide) <= 0f)
            {
                PopupAt(ent, user, "n2o-turbo-booster-no-fuel");
                return false;
            }
        }

        comp.Active = active;

        RefreshGridBoost(ent.Owner);
        UpdateVisuals(ent);

        if (TryComp<DeviceLinkSourceComponent>(ent.Owner, out var source))
            _deviceLink.SendSignal(ent.Owner, comp.StatusPort, active, source);

        // Silent for automatic state changes without an actor; those report themselves.
        if (user != null)
        {
            PopupAt(ent, user, active
                ? "n2o-turbo-booster-activated"
                : "n2o-turbo-booster-deactivated");
        }

        return true;
    }

    /// <summary>
    /// Toggles the booster.
    /// </summary>
    public bool Toggle(Entity<N2OTurboBoosterComponent> ent, EntityUid? user = null)
        => TrySetActive(ent, !ent.Comp.Active, user);

    /// <summary>
    /// Pulls N2O out of the connected pipe net into the internal reserve, up to <see cref="N2OTurboBoosterComponent.Capacity"/>.
    /// Other gases are left in the network untouched.
    /// </summary>
    private void Refill(Entity<N2OTurboBoosterComponent> ent)
    {
        var comp = ent.Comp;
        var free = comp.Capacity - comp.Storage.TotalMoles;
        if (free <= 0f)
            return;

        if (!_nodeContainer.TryGetNode(ent.Owner, comp.InletName, out PipeNode? inlet))
            return;

        if (inlet.NodeGroup is not PipeNet { NodeCount: > 1 } net)
            return;

        var available = net.Air.GetMoles(Gas.NitrousOxide);
        if (available <= 0f)
            return;

        var transfer = MathF.Min(available, free);
        comp.Storage.AdjustMoles(Gas.NitrousOxide, transfer);
        net.Air.AdjustMoles(Gas.NitrousOxide, -transfer);
    }

    /// <summary>
    /// Recalculates the combined boost multiplier of the grid the booster is installed on and
    /// adds or removes the marker component consumed by the thrust boost hook.
    /// </summary>
    private void RefreshGridBoost(EntityUid boosterUid)
    {
        if (!TryComp(boosterUid, out TransformComponent? xform) ||
            xform.GridUid is not { } grid ||
            Deleted(grid))
        {
            return;
        }

        var active = false;
        var multiplier = 1f;

        var query = EntityQueryEnumerator<N2OTurboBoosterComponent, TransformComponent>();
        while (query.MoveNext(out _, out var comp, out var otherXform))
        {
            if (!comp.Active || otherXform.GridUid != grid)
                continue;

            active = true;
            multiplier += comp.BoostMultiplier - 1f;
        }

        if (!active)
        {
            RemComp<ActiveN2OTurboBoosterComponent>(grid);
            return;
        }

        var marker = EnsureComp<ActiveN2OTurboBoosterComponent>(grid);
        marker.Multiplier = multiplier;
    }

    private void UpdateVisuals(Entity<N2OTurboBoosterComponent> ent)
    {
        var active = ent.Comp.Active;

        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, N2OTurboBoosterVisuals.Active, active, appearance);

        if (_light.TryGetLight(ent.Owner, out var light))
            _light.SetEnabled(ent.Owner, active, light);
    }

    private void PopupAt(Entity<N2OTurboBoosterComponent> ent, EntityUid? recipient, string message)
    {
        if (recipient is { } target)
            _popup.PopupEntity(Loc.GetString(message), ent.Owner, target);
        else
            _popup.PopupEntity(Loc.GetString(message), ent.Owner);
    }
}
