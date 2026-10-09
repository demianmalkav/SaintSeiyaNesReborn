namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public readonly record struct PlatformSpecial40State(
    byte PlayerY,
    byte ActionState4D);

public readonly record struct PlatformSpecial40StepResult(
    PlatformSpecial40State State,
    byte FrameStartAction4E,
    bool CompletedCycle,
    int ScreenYDelta);

/// <summary>
/// Exact fixed-bank $C5CC behavior used when frame-start action $4E is in
/// family $40-$4F.
///
/// For $40-$4E the routine increments current action and player Y by one.
/// For $4F it resets current action to zero and subtracts $0F from player Y.
/// No high-byte/page adjustment is performed by the original routine.
/// </summary>
public static class PlatformSpecial40Motion
{
    public static PlatformSpecial40StepResult Step(PlatformSpecial40State state)
    {
        var frameStartAction4E = state.ActionState4D;
        if (PlatformActionState.Family(frameStartAction4E) != (byte)PlatformActionFamily.Special40)
            throw new ArgumentException("Special40 step requires frame-start action family $40-$4F.", nameof(state));

        var incrementedAction = unchecked((byte)(frameStartAction4E + 1));
        if (incrementedAction == 0x50)
        {
            return new PlatformSpecial40StepResult(
                state with
                {
                    PlayerY = unchecked((byte)(state.PlayerY - 0x0F)),
                    ActionState4D = 0,
                },
                frameStartAction4E,
                CompletedCycle: true,
                ScreenYDelta: -15);
        }

        return new PlatformSpecial40StepResult(
            state with
            {
                PlayerY = unchecked((byte)(state.PlayerY + 1)),
                ActionState4D = incrementedAction,
            },
            frameStartAction4E,
            CompletedCycle: false,
            ScreenYDelta: 1);
    }
}
