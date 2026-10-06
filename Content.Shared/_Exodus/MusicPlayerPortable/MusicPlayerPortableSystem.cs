using Content.Shared.Audio.Jukebox;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Exodus.MusicPlayerPortable;

public sealed partial class MusicPlayerPortableSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<MusicPlayerPortableComponent, BoundUIOpenedEvent>(OnBUIOpen);
        SubscribeLocalEvent<MusicPlayerPortableComponent, BoundUIClosedEvent>(OnBUIClose);
        SubscribeLocalEvent<MusicPlayerPortableComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(EntityUid uid, MusicPlayerPortableComponent MPPlayer, ComponentInit args)
    {
        MPPlayer.NextUpdate = _timing.CurTime + MPPlayer.UpdateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<MusicPlayerPortableComponent, JukeboxComponent, PowerCellDrawComponent>();
        while (query.MoveNext(out var ent, out var MPPlayer, out var jukebox, out var powerCellDrawComp))
        {
            if (MPPlayer.NextUpdate > currentTime)
                return;

            if (_ui.IsAnyUiOpen(ent))
            {
                MPPlayer.NextUpdate = currentTime + MPPlayer.UpdateInterval;
                return;
            }

            if (jukebox.AudioStream != null
                && Exists(jukebox.AudioStream.Value)
                && HasComp<MetaDataComponent>(jukebox.AudioStream.Value)
                && _audio.IsPlaying(jukebox.AudioStream.Value))
                powerCellDrawComp.DrawRate = MPPlayer.DrawRate;
            else
                powerCellDrawComp.DrawRate = 0f;

            MPPlayer.NextUpdate = currentTime + MPPlayer.UpdateInterval;
        }

    }

    private void OnBUIClose(EntityUid uid, MusicPlayerPortableComponent MPPlayer, BoundUIClosedEvent args)
    {
        if (!TryComp<PowerCellDrawComponent>(uid, out var powerDrawComp))
            return;

        if (!TryComp<JukeboxComponent>(uid, out var jukebox)
            || jukebox.AudioStream == null
            || !Exists(jukebox.AudioStream.Value)
            || !_audio.IsPlaying(jukebox.AudioStream.Value))
        {
            powerDrawComp.DrawRate = 0f;
        }

        powerDrawComp.DrawRate = MPPlayer.DrawRate;
    }

    private void OnBUIOpen(EntityUid uid, MusicPlayerPortableComponent MPPlayer, BoundUIOpenedEvent args)
    {
        if (!TryComp<PowerCellDrawComponent>(uid, out var powerDrawComp))
            return;

        powerDrawComp.DrawRate = MPPlayer.DrawRate;
    }

    private void OnPowerCellChanged(Entity<MusicPlayerPortableComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected || !TryComp<JukeboxComponent>(ent, out var jukebox))
            return;

        StopAudio(ent.Owner, jukebox);
        CloseUI(ent.Owner);
    }

    private void OnPowerCellSlotEmpty(Entity<MusicPlayerPortableComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (!TryComp<JukeboxComponent>(ent.Owner, out var jukebox))
            return;

        StopAudio(ent.Owner, jukebox);
        CloseUI(ent.Owner);
    }

    private void CloseUI(EntityUid ent)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var ui))
            return;

        _ui.CloseUis((ent, ui));
    }

    private void StopAudio(EntityUid ent, JukeboxComponent jukebox)
    {
        if (jukebox.AudioStream != null
            && Exists(jukebox.AudioStream.Value)
            && HasComp<MetaDataComponent>(jukebox.AudioStream.Value))
        {
            _audio.SetState(jukebox.AudioStream, AudioState.Stopped);
        }

        Dirty(ent, jukebox);
    }
}
