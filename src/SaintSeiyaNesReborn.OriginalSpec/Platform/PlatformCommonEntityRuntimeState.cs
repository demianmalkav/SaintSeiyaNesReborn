namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

/// <summary>
/// Semantic view of one common 16-byte entity record across both the motion/AI
/// and combat/contact subsystems.
///
/// The original aliases record offset +$03 across movement state and projectile
/// reaction writes. Keeping one runtime aggregate prevents the clean-room model
/// from letting those views silently diverge.
/// </summary>
public readonly record struct PlatformCommonEntityRuntimeState(
    PlatformCommonEntityMotionState Motion,
    byte HitPoints,
    byte LifeDrainTicks,
    byte CosmoDrainTicks,
    byte SeventhSenseRewardBcd)
{
    public PlatformCombatEntity ToCombatEntity() => new(
        State: Motion.ActionState,
        X: Motion.X,
        Y: Motion.Y,
        Type: Motion.Type,
        HitPoints: HitPoints,
        SeventhSenseRewardBcd: SeventhSenseRewardBcd,
        Motion3: Motion.StatePhase,
        RightTerrain0A: Motion.TerrainProbeRight,
        LeftTerrain0B: Motion.TerrainProbeLeft);

    /// <summary>
    /// Rejoins mutations made by projectile-hit routing into the shared logical
    /// entity record while preserving motion fields that combat code does not
    /// own (ground descriptor, decision timer and facing flags).
    /// </summary>
    public PlatformCommonEntityRuntimeState WithCombatEntity(PlatformCombatEntity combat)
    {
        if (combat.Type != Motion.Type)
            throw new InvalidOperationException(
                $"Combat entity type ${combat.Type:X2} does not match runtime motion type ${Motion.Type:X2}.");

        return this with
        {
            Motion = Motion with
            {
                ActionState = combat.State,
                X = combat.X,
                Y = combat.Y,
                StatePhase = combat.Motion3,
                TerrainProbeRight = combat.RightTerrain0A,
                TerrainProbeLeft = combat.LeftTerrain0B,
            },
            HitPoints = combat.HitPoints,
            SeventhSenseRewardBcd = combat.SeventhSenseRewardBcd,
        };
    }
}
