namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformFrameResources(int Life, int Cosmo);

public sealed record PlatformPrePlayerResourcePhaseResult(
    PlatformFrameResources ResourcesBefore,
    PlatformFrameResources ResourcesAfterDrain,
    PlatformContactPhaseState ContactStateAfterDrain,
    DrainStepResult Drain,
    int PlatformDamage,
    PlatformPlayerActionDispatchResult Player);

/// <summary>
/// Composes the confirmed non-fatal resource/attack ordering before and through
/// the player action phase of an active platform frame.
///
/// Existing $7F/$80 drain counters are consumed in bank 1 before $AAE4. The
/// later $C52F -> bank-1 $8616 damage calculation and BBCA projectile-range
/// selection therefore observe the post-drain Cosmo value.
///
/// Resource-exhaustion frames take the original failure transition and are not
/// routed through this non-fatal helper.
/// </summary>
public static class PlatformPrePlayerResourcePhases
{
    public static PlatformPrePlayerResourcePhaseResult StepNonFatal(
        PlatformStageMap stage,
        PlatformPlayerActionState playerState,
        PlatformInput input,
        PlatformFrameResources resources,
        PlatformContactPhaseState contactState,
        byte frameCounter3C,
        byte engineSubstate02,
        byte engineSubstate01 = 0,
        byte dynamicFloorY039B = 0)
    {
        ValidateResources(resources);

        // Bank-1 $8000 also advances the attack busy byte before player input.
        // Its exact relative position versus the two drain helpers is immaterial
        // here because neither operation aliases the other's state, but both are
        // completed before $AAE4 sees B.
        playerState = PlatformAttackFramePhases.AdvanceBusyBeforePlayer(playerState);

        var drain = PlatformContactFramePhases.AdvanceDrainBeforePlayer(
            playerState.Saint,
            engineSubstate02,
            frameCounter3C,
            contactState);

        var after = new PlatformFrameResources(
            resources.Life - drain.LifeLoss,
            resources.Cosmo - drain.CosmoLoss);

        if (after.Life <= 0 || after.Cosmo <= 0)
        {
            throw new InvalidOperationException(
                "Resource exhaustion takes the original platform failure transition; use a failure-path model instead of StepNonFatal.");
        }

        var afterContact = PlatformContactFramePhases.StateAfterDrain(contactState, drain);

        // Fixed $C52F computes $72 after bank-1 frame work, so it sees the same
        // post-drain Cosmo that BBCA later uses for projectile range/lifetime.
        var platformDamage = PlatformDamage.FromCosmo(playerState.Saint, after.Cosmo);

        var player = PlatformPlayerActionDispatcher.Step(
            stage,
            playerState,
            input,
            frameCounter3C,
            after.Cosmo,
            engineSubstate01,
            dynamicFloorY039B);

        return new PlatformPrePlayerResourcePhaseResult(
            resources,
            after,
            afterContact,
            drain,
            platformDamage,
            player);
    }

    private static void ValidateResources(PlatformFrameResources resources)
    {
        if (resources.Life is < 1 or > 999)
            throw new ArgumentOutOfRangeException(nameof(resources), resources.Life, "Life must be in 1..999 for an active non-fatal platform frame.");
        if (resources.Cosmo is < 1 or > 999)
            throw new ArgumentOutOfRangeException(nameof(resources), resources.Cosmo, "Cosmo must be in 1..999 for an active non-fatal platform frame.");
    }
}
