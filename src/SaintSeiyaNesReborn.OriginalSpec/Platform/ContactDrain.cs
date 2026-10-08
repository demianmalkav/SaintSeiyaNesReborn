using SaintSeiyaNesReborn.OriginalSpec;

namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct ContactDrainState(byte LifeTicks, byte CosmoTicks)
{
    public bool IsEmpty => LifeTicks == 0 && CosmoTicks == 0;
}

public readonly record struct DrainStepResult(
    ContactDrainState State,
    int LifeLoss,
    int CosmoLoss,
    bool PeriodicEnvironmentLifeLoss);

public static class ContactDrain
{
    /// <summary>
    /// Reproduces the effective platform drain scheduling of bank 1 $927A/$930A.
    /// This returns semantic integer losses; packed-BCD storage is an implementation detail
    /// handled elsewhere in the original engine.
    /// </summary>
    public static DrainStepResult Step(
        PlatformSaintIndex saint,
        byte engineSubstate02,
        byte frameCounter3C,
        ContactDrainState state)
    {
        var lifeTicks = state.LifeTicks;
        var cosmoTicks = state.CosmoTicks;
        var lifeLoss = 0;
        var cosmoLoss = 0;
        var periodic = false;

        // $927A: in substate $10 a frame-mask branch reaches the same
        // subtract-two-Life helper without consuming $7F.
        if (engineSubstate02 == 0x10 && IsPeriodicEnvironmentDrainFrame(saint, frameCounter3C))
        {
            lifeLoss = 2;
            periodic = true;
        }
        else if (lifeTicks != 0)
        {
            lifeTicks--;
            lifeLoss = 2;
        }

        // $930A: Cosmo contact drain is one point per remaining tick.
        if (cosmoTicks != 0)
        {
            cosmoTicks--;
            cosmoLoss = 1;
        }

        return new DrainStepResult(
            new ContactDrainState(lifeTicks, cosmoTicks),
            lifeLoss,
            cosmoLoss,
            periodic);
    }

    public static bool IsPeriodicEnvironmentDrainFrame(PlatformSaintIndex saint, byte frameCounter3C)
    {
        // Internal index 1 is Shun. The ROM uses mask $07 for him and $1F for everyone else.
        var mask = saint == PlatformSaintIndex.Shun ? 0x07 : 0x1F;
        return (frameCounter3C & mask) == 0;
    }
}
