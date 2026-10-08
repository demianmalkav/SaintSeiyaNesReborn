namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public static class SeventhSense
{
    /// <summary>
    /// Reproduces fixed-bank $D1E0+ for the ordinary platform enemy-death path.
    /// The reward is one packed-BCD byte (00..99), current value is four digits,
    /// and the original clamps overflow to 9999. Substate $02 == 0 suppresses it.
    /// </summary>
    public static int AddPlatformReward(int current, byte rewardPackedBcd, byte engineSubstate02)
    {
        if ((uint)current > 9999)
            throw new ArgumentOutOfRangeException(nameof(current), current, "Seventh Sense must be in 0..9999.");

        if (engineSubstate02 == 0)
            return current;

        var reward = PackedBcd.DecodeByte(rewardPackedBcd);
        return Math.Min(9999, current + reward);
    }
}
