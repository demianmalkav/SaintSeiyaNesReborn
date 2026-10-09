namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformDamage80Outcome
{
    WaitingForHazardLatch,
    ReloadPlatform,
}

public readonly record struct PlatformDamage80StepResult(
    byte FrameStartAction4E,
    byte HazardLatch76,
    PlatformDamage80Outcome Outcome)
{
    public bool ExitsNormalPlayerLoop => Outcome == PlatformDamage80Outcome.ReloadPlatform;
    public bool RequiresPersistentResourceSnapshot => ExitsNormalPlayerLoop;
    public bool ClearsMainModeBytes00And01 => ExitsNormalPlayerLoop;
    public bool DisablesRendering => ExitsNormalPlayerLoop;
    public bool ResetsCpuStack => ExitsNormalPlayerLoop;
    public ushort RestartAddress => ExitsNormalPlayerLoop ? (ushort)0xC180 : (ushort)0;
}

/// <summary>
/// Exact $AAE4 behavior for frame-start action family $80-$8F.
///
/// If $76 is non-zero, the dispatcher returns immediately with no player/input
/// processing. If $76 is zero, the original does not return to the ordinary
/// platform frame: it calls fixed $CA94 (bank-1 $951F resource snapshot), clears
/// main mode bytes $00/$01, disables rendering through $C154, resets the CPU
/// stack, and jumps to fixed $C180 for platform/main-loop reinitialization.
///
/// The countdown/update of $76 happens outside this routine; this model only
/// reproduces the gate observed by $AAE4.
/// </summary>
public static class PlatformDamage80Transition
{
    public static PlatformDamage80StepResult Step(byte frameStartAction4E, byte hazardLatch76)
    {
        if (PlatformActionState.Family(frameStartAction4E) != (byte)PlatformActionFamily.DamageOrHazard)
            throw new ArgumentException("Damage80 transition requires frame-start action family $80-$8F.", nameof(frameStartAction4E));

        return new PlatformDamage80StepResult(
            frameStartAction4E,
            hazardLatch76,
            hazardLatch76 != 0
                ? PlatformDamage80Outcome.WaitingForHazardLatch
                : PlatformDamage80Outcome.ReloadPlatform);
    }
}
