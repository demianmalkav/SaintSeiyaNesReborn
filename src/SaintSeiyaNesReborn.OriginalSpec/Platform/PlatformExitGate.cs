namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformExitTransitionKind
{
    /// <summary>
    /// Original path snapshots Saint state, sets $00/$01=$3D, disables rendering,
    /// resets the stack and jumps through the engine reload path at $E100.
    /// </summary>
    State3DReload,

    /// <summary>
    /// Special substate $11 path sets $00=$70 and enters the final special
    /// transition instead of the normal $3D reload path.
    /// </summary>
    State70Special,
}

public readonly record struct PlatformExitGate(
    int MinimumPlayerX,
    int RequiredPlayerY,
    PlatformExitTransitionKind Transition)
{
    /// <summary>
    /// Exit coordinates reconstructed from PRG bank 1 $969D-$9713.
    /// For substates $00-$0B the gate is the common ($D0,$40) endpoint.
    /// Substates $0C-$11 use the six coordinate pairs at $9714.
    /// </summary>
    public static PlatformExitGate ForSubstate(int substate) => substate switch
    {
        >= 0x00 and <= 0x0B => new(0xD0, 0x40, PlatformExitTransitionKind.State3DReload),
        0x0C => new(0x88, 0x20, PlatformExitTransitionKind.State3DReload),
        0x0D => new(0xB4, 0x30, PlatformExitTransitionKind.State3DReload),
        0x0E => new(0xB4, 0x80, PlatformExitTransitionKind.State3DReload),
        0x0F => new(0xB4, 0x40, PlatformExitTransitionKind.State3DReload),
        0x10 => new(0xB4, 0x70, PlatformExitTransitionKind.State3DReload),
        0x11 => new(0xD0, 0x50, PlatformExitTransitionKind.State70Special),
        _ => throw new ArgumentOutOfRangeException(nameof(substate), substate, "Platform substate must be $00-$11."),
    };

    /// <summary>
    /// Mirrors the gate predicates before the original mode transition.
    /// jumpPhase is RAM $49. A nonzero phase prevents leaving the platform area.
    /// Substate $10 explicitly rejects internal Saint index 1 (Shun).
    /// </summary>
    public static PlatformExitTransitionKind? Evaluate(
        int substate,
        PlatformSaintIndex saint,
        int playerX,
        int playerY,
        int jumpPhase)
    {
        if (jumpPhase != 0)
            return null;

        if (substate == 0x10 && saint == PlatformSaintIndex.Shun)
            return null;

        var gate = ForSubstate(substate);
        if (playerX < gate.MinimumPlayerX || playerY != gate.RequiredPlayerY)
            return null;

        return gate.Transition;
    }
}
