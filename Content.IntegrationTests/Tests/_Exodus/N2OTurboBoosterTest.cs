using Content.Server._Exodus.Shuttles.Components;
using Content.Server._Exodus.Shuttles.Events;
using Content.Server._Exodus.Shuttles.Systems;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Exodus;

[TestFixture]
[TestOf(typeof(N2OTurboBoosterSystem))]
public sealed class N2OTurboBoosterTest
{
    [Test]
    public async Task BoosterNeedsFuelBoostesGridAndBurnsN2O()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var em = server.EntMan;
        var map = await pair.CreateTestMap();
        EntityUid boosterUid = default;

        await server.WaitAssertion(() =>
        {
            boosterUid = em.SpawnEntity("N2OTurboBooster", new EntityCoordinates(map.Grid, .5f, .5f));

            // Without N2O the booster refuses to start and the grid stays unboosted.
            Assert.That(SetActive(em, boosterUid, true), Is.False, "Empty booster must not start.");
            Assert.That(em.HasComponent<ActiveN2OTurboBoosterComponent>(map.Grid), Is.False);
        });

        await server.WaitAssertion(() =>
        {
            var comp = em.GetComponent<N2OTurboBoosterComponent>(boosterUid);
            var storage = comp.Storage;
            storage.AdjustMoles(Gas.NitrousOxide, 500f);

            Assert.That(SetActive(em, boosterUid, true), Is.True, "Fueled booster must start.");
            Assert.That(comp.Active, Is.True);

            // The grid now advertises the boost consumed by MoverController's thrust hook.
            var marker = em.GetComponent<ActiveN2OTurboBoosterComponent>(map.Grid);
            Assert.That(marker.Multiplier, Is.EqualTo(1.5f));

            var ev = new GetShuttleThrustBoostEvent(map.Grid);
            em.EventBus.RaiseLocalEvent(map.Grid, ref ev);
            Assert.That(ev.LinearMultiplier, Is.EqualTo(1.5f));
            Assert.That(ev.AngularMultiplier, Is.EqualTo(1.5f));
        });

        // Running the booster burns N2O through the atmos device update.
        await PoolManager.WaitUntil(server, () => FuelMoles(em, boosterUid) < 500f);

        await server.WaitAssertion(() =>
        {
            var comp = em.GetComponent<N2OTurboBoosterComponent>(boosterUid);
            var system = em.System<N2OTurboBoosterSystem>();

            Assert.That(system.Toggle((boosterUid, comp)), Is.True);
            Assert.That(comp.Active, Is.False);
            Assert.That(em.HasComponent<ActiveN2OTurboBoosterComponent>(map.Grid), Is.False);

            // Without the marker the boost hook no longer touches shuttle thrust.
            var ev = new GetShuttleThrustBoostEvent(map.Grid);
            em.EventBus.RaiseLocalEvent(map.Grid, ref ev);
            Assert.That(ev.LinearMultiplier, Is.EqualTo(1f));
            Assert.That(ev.AngularMultiplier, Is.EqualTo(1f));
        });
        await pair.CleanReturnAsync();
    }

    private static bool SetActive(IEntityManager em, EntityUid uid, bool active)
    {
        var comp = em.GetComponent<N2OTurboBoosterComponent>(uid);
        return em.System<N2OTurboBoosterSystem>().TrySetActive((uid, comp), active);
    }

    private static float FuelMoles(IEntityManager em, EntityUid uid)
    {
        var storage = em.GetComponent<N2OTurboBoosterComponent>(uid).Storage;
        return storage.GetMoles(Gas.NitrousOxide);
    }
}
