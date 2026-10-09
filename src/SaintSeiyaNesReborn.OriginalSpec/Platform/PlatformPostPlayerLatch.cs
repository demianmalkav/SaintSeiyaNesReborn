namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformPostPlayerLatchResult(
    byte Before76,
    byte After76,
    bool Decremented,
    bool SkippedBecausePlayerLoopExited);

/// <summary>
/// Semantic slice of bank 3 $B94B immediately after $AAE4.
///
/// The original loads $76, returns to the rest of $B94B unchanged when zero,
/// and otherwise decrements it exactly once before continuing animation/render
/// selection. The $80 reload branch is exceptional: when $AAE4 decides to
/// reload, it resets the CPU stack and JMPs away, so $B94B is not reached.
///
/// This type intentionally models only the confirmed timer side effect, not the
/// sprite/OAM work performed by the remainder of $B94B.
/// </summary>
public static class PlatformPostPlayerLatch
{
    public static PlatformPostPlayerLatchResult Step(byte hazardLatch76, bool playerLoopExited)
    {
        if (playerLoopExited)
        {
            return new PlatformPostPlayerLatchResult(
                hazardLatch76,
                hazardLatch76,
                Decremented: false,
                SkippedBecausePlayerLoopExited: true);
        }

        if (hazardLatch76 == 0)
        {
            return new PlatformPostPlayerLatchResult(
                0,
                0,
                Decremented: false,
                SkippedBecausePlayerLoopExited: false);
        }

        return new PlatformPostPlayerLatchResult(
            hazardLatch76,
            unchecked((byte)(hazardLatch76 - 1)),
            Decremented: true,
            SkippedBecausePlayerLoopExited: false);
    }

    /// <summary>
    /// Applies the post-player $76 side effect to an already-dispatched player
    /// result. Other $B94B rendering/animation mutations are deliberately out of
    /// scope until separately reconstructed.
    /// </summary>
    public static PlatformPlayerActionDispatchResult Apply(PlatformPlayerActionDispatchResult player)
    {
        var latch = Step(player.State.Special76, player.ExitsNormalPlayerLoop);
        if (latch.After76 == player.State.Special76)
            return player;

        return player with
        {
            State = player.State with { Special76 = latch.After76 },
        };
    }
}
