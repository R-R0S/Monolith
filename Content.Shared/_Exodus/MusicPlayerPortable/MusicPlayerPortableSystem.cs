using Content.Shared.Audio.Jukebox;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Exodus.MusicPlayerPortable;

public sealed partial class MusicPlayerPortableSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellChangedEvent>(OnPowerCellChanged);
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
