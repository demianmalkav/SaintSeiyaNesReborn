using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class ContactFramePhaseChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        var empty = new PlatformContactPhaseState(0, new ContactDrainState(0, 0));

        // Frame N begins with no old contact counters, so the pre-player drain
        // phase produces no resource loss.
        var frameNDrain = PlatformContactFramePhases.AdvanceDrainBeforePlayer(
            PlatformSaintIndex.Seiya,
            engineSubstate02: 0,
            frameCounter3C: 1,
            empty);
        Require(frameNDrain.LifeLoss == 0 && frameNDrain.CosmoLoss == 0, "new contact frame has no pre-existing drain loss");
        var afterFrameNDrain = PlatformContactFramePhases.StateAfterDrain(empty, frameNDrain);

        // The entity contacts later in A442/$98BA and seeds the counters only now.
        var frameNContact = PlatformContactFramePhases.ApplyOrdinaryEntityContact(
            afterFrameNDrain,
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x50,
            entityX: 0x50,
            entityY: 0x50,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 4);
        Require(frameNContact.Contact.Triggered, "frame N later entity phase triggers contact");
        Require(frameNContact.State.HazardLatch76 == 0x20, "contact seeds latch after pre-player drain phase");
        Require(frameNContact.State.DrainState == new ContactDrainState(2, 4), "contact seeds future Life/Cosmo counters");

        // Frame N+1 is the first time those newly-seeded counters are consumed.
        var frameNPlus1Drain = PlatformContactFramePhases.AdvanceDrainBeforePlayer(
            PlatformSaintIndex.Seiya,
            engineSubstate02: 0,
            frameCounter3C: 2,
            frameNContact.State);
        Require(frameNPlus1Drain.LifeLoss == 2, "new contact Life loss begins on next frame");
        Require(frameNPlus1Drain.CosmoLoss == 1, "new contact Cosmo loss begins on next frame");
        Require(frameNPlus1Drain.State == new ContactDrainState(1, 3), "next frame consumes one tick from each counter");

        // Non-triggering contact must not erase already-running drain counters.
        var existing = new PlatformContactPhaseState(0x10, new ContactDrainState(5, 6));
        var suppressed = PlatformContactFramePhases.ApplyOrdinaryEntityContact(
            existing,
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x50,
            entityX: 0x50,
            entityY: 0x50,
            entityLifeDrainTicks: 1,
            entityCosmoDrainTicks: 1);
        Require(!suppressed.Contact.Triggered, "active latch suppresses refresh contact");
        Require(suppressed.State == existing, "suppressed contact preserves current latch and drain counters");

        // Once a contact does trigger, $98BA overwrites rather than accumulates
        // $7F/$80 with the current entity profile.
        var previousCountersButFreeLatch = new PlatformContactPhaseState(0, new ContactDrainState(5, 6));
        var overwrite = PlatformContactFramePhases.ApplyOrdinaryEntityContact(
            previousCountersButFreeLatch,
            frameStartAction4E: 0,
            playerX: 0x50,
            playerY: 0x50,
            entityX: 0x50,
            entityY: 0x50,
            entityLifeDrainTicks: 2,
            entityCosmoDrainTicks: 1);
        Require(overwrite.Contact.Triggered, "zero latch permits a fresh contact even with residual counters");
        Require(overwrite.State.DrainState == new ContactDrainState(2, 1), "fresh contact overwrites old drain counters");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Contact frame phase self-test failed: {label}");
    }
}
