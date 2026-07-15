using Content.Shared.Audio.Jukebox;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Exodus.MusicPlayerPortable;

public sealed class MusicPlayerPortableSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MusicPlayerPortableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        SubscribeLocalEvent<MusicPlayerPortableComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnPowerCellChanged(Entity<MusicPlayerPortableComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected && TryComp<JukeboxComponent>(ent, out var jukebox))
        {
            StopAudio(ent.Owner, jukebox);
        }
    }

    private void OnPowerCellSlotEmpty(Entity<MusicPlayerPortableComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (TryComp<JukeboxComponent>(ent.Owner, out var jukebox))
        {
            StopAudio(ent.Owner, jukebox);
        }
    }

    private void StopAudio(EntityUid entity, JukeboxComponent jukebox)
    {
        if (jukebox.AudioStream != null && Exists(jukebox.AudioStream.Value) && HasComp<MetaDataComponent>(jukebox.AudioStream.Value))
        {
            _audio.SetState(jukebox.AudioStream, AudioState.Stopped);
        }

        Dirty(entity, jukebox);
    }

    private void OnInit(EntityUid uid, MusicPlayerPortableComponent component, ComponentInit args)
    {
        // UpdatePlayerState(uid);
    }

    // public override void Update(float frameTime)
    // {
    //     base.Update(frameTime);
    //     _nextUpdate -= frameTime;
    //     if (_nextUpdate > 0)
    //         return;
    //     _nextUpdate = UpdateInterval;
    //
    //     // Перебираем все сущности с нашим компонентом и JukeboxComponent
    //     var query = EntityQueryEnumerator<MusicPlayerPortableComponent, JukeboxComponent>();
    //     while (query.MoveNext(out var uid, out _, out var jukebox))
    //     {
    //         UpdatePlayerState(uid, jukebox);
    //     }
    // }

    // private void UpdatePlayerState(EntityUid uid, JukeboxComponent? jukebox = null)
    // {
    //     if (!Resolve(uid, ref jukebox))
    //         return;
    //
    //     bool hasPower = HasSufficientBatteryPower(uid);
    //
    //     // Обновляем визуальное состояние слоя, который определён в прототипе как PowerDeviceVisualLayers.Powered
    //     _appearanceSystem.SetData(uid, PowerDeviceVisualLayers.Powered, hasPower);
    //
    //     // Если питание пропало, а музыка играет – останавливаем через стандартное сообщение
    //     if (!hasPower && jukebox.Playing)
    //     {
    //         // Отправляем локальное сообщение, которое JukeboxSystem обработает
    //         var stopMsg = new JukeboxStopMessage();
    //         RaiseLocalEvent(uid, stopMsg);
    //     }
    // }

    // private bool HasSufficientBatteryPower(EntityUid uid)
    // {
    //     if (!TryComp<PowerCellSlotComponent>(uid, out var slot))
    //         return false;
    //
    //     var cell = GetCellFromSlot(slot);
    //     if (cell == null)
    //         return false;
    //
    //     if (!TryComp<BatteryComponent>(cell.Value, out var battery))
    //         return false;
    //
    //     // Считаем, что питание есть, если заряд строго больше нуля
    //     return battery.CurrentCharge > 0;
    // }
    //
    // private EntityUid? GetCellFromSlot(PowerCellSlotComponent slot)
    // {
    //     // Попытка получить батарейку из слота разными способами
    //     // (универсальный подход, т.к. точное поле может отличаться в вашей версии)
    //
    //     // Способ 1: прямое поле Cell
    //     var cellProp = slot.GetType().GetProperty("Cell");
    //     if (cellProp != null)
    //         return (EntityUid?)cellProp.GetValue(slot);
    //
    //     // Способ 2: контейнер Slot
    //     var slotProp = slot.GetType().GetProperty("Slot");
    //     if (slotProp != null)
    //     {
    //         var container = (ContainerSlot?)slotProp.GetValue(slot);
    //         if (container != null && container.ContainedEntity != null)
    //             return container.ContainedEntity.Value;
    //     }
    //
    //     // Способ 3: свойство ContainedEntity
    //     var containedProp = slot.GetType().GetProperty("ContainedEntity");
    //     if (containedProp != null)
    //         return (EntityUid?)containedProp.GetValue(slot);
    //
    //     return null;
    // }
}
