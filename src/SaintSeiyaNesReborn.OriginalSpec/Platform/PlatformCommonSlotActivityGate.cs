namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformCommonSlotActivityOutcome
{
    VisualActive,
    VisualFreeButReactionContinues,
    VisualFreeButDeathContinues,
    VisualFreeInactive,
}

public readonly record struct PlatformCommonSlotActivityResult(
    PlatformCommonSlotActivityOutcome Outcome,
    bool ProcessSlot)
{
    public bool Skipped => !ProcessSlot;
}

/// <summary>
/// Exact entry gate at bank-3 $A459-$A47A used by both common logical records
/// before the rest of the $A442 entity dispatcher.
///
/// Visual record byte +1 is the normal activity marker. Values other than $FE
/// always enter entity processing. A visually free slot ($FE) still processes
/// logical families $40 and $D0 so hit/death cleanup can finish after visual
/// retirement. All other visually free states return immediately.
/// </summary>
public static class PlatformCommonSlotActivityGate
{
    public const byte FreeVisualSprite = 0xFE;

    public static PlatformCommonSlotActivityResult Evaluate(
        byte visualSpritePlus1,
        byte logicalActionState)
    {
        if (visualSpritePlus1 != FreeVisualSprite)
        {
            return new(
                PlatformCommonSlotActivityOutcome.VisualActive,
                ProcessSlot: true);
        }

        return (logicalActionState & 0xF0) switch
        {
            0x40 => new(
                PlatformCommonSlotActivityOutcome.VisualFreeButReactionContinues,
                ProcessSlot: true),
            0xD0 => new(
                PlatformCommonSlotActivityOutcome.VisualFreeButDeathContinues,
                ProcessSlot: true),
            _ => new(
                PlatformCommonSlotActivityOutcome.VisualFreeInactive,
                ProcessSlot: false),
        };
    }
}
