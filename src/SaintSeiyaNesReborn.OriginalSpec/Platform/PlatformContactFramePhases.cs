namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformContactPhaseState(
    byte HazardLatch76,
    ContactDrainState DrainState);

public readonly record struct PlatformContactAfterEntityResult(
    PlatformContactPhaseState State,
    PlatformEntityContactResult Contact);

/// <summary>
/// Composes the confirmed relative timing of contact-drain counters and ordinary
/// entity contact in active platform mode.
///
/// Bank-1 $8000 calls $927A/$930A before $AAE4, so existing $7F/$80 counters
/// drain before player simulation. Ordinary entity contact at bank-3 $98BA is
/// reached later through the $A442 entity pipeline and can seed/overwrite those
/// counters only after the current frame's drain phase has already passed.
/// </summary>
public static class PlatformContactFramePhases
{
    public static DrainStepResult AdvanceDrainBeforePlayer(
        PlatformSaintIndex saint,
        byte engineSubstate02,
        byte frameCounter3C,
        PlatformContactPhaseState state) =>
        ContactDrain.Step(saint, engineSubstate02, frameCounter3C, state.DrainState);

    /// <summary>
    /// Applies one ordinary entity-contact test after the player/post-player
    /// phases. If contact triggers, the original overwrites $7F/$80 with the
    /// entity's drain counts and seeds $76=$20. If it does not trigger, existing
    /// drain counters and latch are preserved.
    /// </summary>
    public static PlatformContactAfterEntityResult ApplyOrdinaryEntityContact(
        PlatformContactPhaseState state,
        byte frameStartAction4E,
        byte playerX,
        byte playerY,
        byte entityX,
        byte entityY,
        byte entityLifeDrainTicks,
        byte entityCosmoDrainTicks)
    {
        var contact = PlatformEntityContact.EvaluateOrdinary(
            frameStartAction4E,
            playerX,
            playerY,
            entityX,
            entityY,
            state.HazardLatch76,
            entityLifeDrainTicks,
            entityCosmoDrainTicks);

        if (!contact.Triggered)
            return new PlatformContactAfterEntityResult(state, contact);

        return new PlatformContactAfterEntityResult(
            new PlatformContactPhaseState(contact.HazardLatch76, contact.DrainState),
            contact);
    }

    /// <summary>
    /// Carries the post-drain counters forward into later frame phases. The loss
    /// values are semantic resource deltas; actual packed-BCD resource mutation
    /// remains the responsibility of the higher-level frame/resource model.
    /// </summary>
    public static PlatformContactPhaseState StateAfterDrain(
        PlatformContactPhaseState before,
        DrainStepResult drain) =>
        before with { DrainState = drain.State };
}
