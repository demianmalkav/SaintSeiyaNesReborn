namespace SaintSeiyaNesReborn.OriginalSpec.Platform;

public enum PlatformMultisprite9B93Route
{
    BootstrapSelectorZero,
    BootstrapCooldown,
    BootstrapInitialized,
    Substate0D,
    DeathDrop,
    Normal,
    Flag08Bit04,
    Flag08Clear04,
}

public readonly record struct PlatformMultisprite9B93PersistentState(
    PlatformMultisprite9B93RuntimeState Runtime,
    byte Cooldown03FA,
    byte Global03A9)
{
    public static PlatformMultisprite9B93PersistentState Empty =>
        new(
            new PlatformMultisprite9B93RuntimeState(
                PlatformMultisprite9B93VisualState.Empty,
                default,
                Mode81: 0),
            Cooldown03FA: 0,
            Global03A9: 0);
}

public sealed record PlatformMultisprite9B93FrameResult(
    PlatformMultisprite9B93PersistentState State,
    PlatformMultisprite9B93Route Route,
    PlatformMultisprite9B93BootstrapResult Bootstrap,
    PlatformMultisprite9B93NormalUpdateResult? Normal,
    PlatformMultisprite9B93Flag08Bit04Result? Flag08Bit04,
    PlatformMultisprite9B93Flag08Clear04Result? Flag08Clear04,
    PlatformMultisprite9B93DeathDropResult? DeathDrop,
    PlatformMultisprite9B93Substate0DResult? Substate0D,
    PlatformAttackState AttackState,
    PlatformContactPhaseState ContactState,
    int SeventhSense);

/// <summary>
/// Persistent top-level composition of bank-3 `$9B93-$A22B` for the independent
/// multisprite class rooted at visual `$07E0` and logical `$03FB`.
///
/// The call always begins with the bootstrap/active gate. A newly initialized
/// object returns from the bootstrap path and is NOT updated again in the same
/// call. Existing active visuals continue into the exact `$9CAC+` dispatcher:
///
/// `$02==$0D` -> dedicated A12A route;
/// otherwise logical D0/E0 -> A06E death/drop;
/// otherwise flag08 clear -> normal 9D05 route;
/// otherwise flag04 set -> 9ED1 route;
/// otherwise -> 9F79 clear04 route.
/// </summary>
public static class PlatformMultisprite9B93Runtime
{
    public static PlatformMultisprite9B93FrameResult Step(
        PlatformMultisprite9B93PersistentState state,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        PlatformSaintIndex saint,
        int platformDamage,
        int seventhSense,
        byte frameCounter3C,
        byte cameraDelta43,
        byte playerX3F,
        byte playerY40,
        byte frameStartPlayerAction4E,
        byte engineSubstate02,
        byte flag74,
        byte stageDerivedSelector,
        byte entropy48)
    {
        var bootstrap = PlatformMultisprite9B93Bootstrap.Step(
            state.Runtime.Visual,
            engineSubstate02,
            flag74,
            stageDerivedSelector,
            state.Cooldown03FA,
            playerX3F,
            entropy48);

        if (bootstrap.Outcome != PlatformMultisprite9B93BootstrapOutcome.ExistingActive)
        {
            var next = ApplyBootstrap(state, bootstrap);
            var route = bootstrap.Outcome switch
            {
                PlatformMultisprite9B93BootstrapOutcome.SelectorZero =>
                    PlatformMultisprite9B93Route.BootstrapSelectorZero,
                PlatformMultisprite9B93BootstrapOutcome.CooldownDecremented =>
                    PlatformMultisprite9B93Route.BootstrapCooldown,
                PlatformMultisprite9B93BootstrapOutcome.Initialized =>
                    PlatformMultisprite9B93Route.BootstrapInitialized,
                _ => throw new InvalidOperationException("ExistingActive is handled by active dispatcher."),
            };

            return Result(
                next,
                route,
                bootstrap,
                attacks,
                contactState,
                seventhSense);
        }

        // `$9CBB`: substate $0D diverts before the common flag08 dispatcher.
        if (engineSubstate02 == 0x0D)
        {
            var step = PlatformMultisprite9B93Substate0D.Step(
                state.Runtime,
                attacks,
                contactState,
                saint,
                platformDamage,
                seventhSense,
                frameCounter3C,
                cameraDelta43,
                playerX3F,
                playerY40,
                frameStartPlayerAction4E);

            var next = state with { Runtime = step.State };
            return new(
                next,
                PlatformMultisprite9B93Route.Substate0D,
                bootstrap,
                Normal: null,
                Flag08Bit04: null,
                Flag08Clear04: null,
                DeathDrop: null,
                Substate0D: step,
                step.AttackState,
                step.ContactState,
                step.SeventhSense);
        }

        // Both non-$0D active dispatchers divert D0/E0 before ordinary flag
        // handling. This precedence matters when part0 still carries flag08/04.
        var family = state.Runtime.Logical.Action00 & 0xF0;
        if (family is 0xD0 or 0xE0)
        {
            var step = PlatformMultisprite9B93DeathDrop.Step(
                state.Runtime,
                engineSubstate02,
                cameraDelta43);
            var next = state with { Runtime = step.State };
            return new(
                next,
                PlatformMultisprite9B93Route.DeathDrop,
                bootstrap,
                Normal: null,
                Flag08Bit04: null,
                Flag08Clear04: null,
                DeathDrop: step,
                Substate0D: null,
                attacks,
                contactState,
                seventhSense);
        }

        var flags = state.Runtime.Visual.Part0.Flags;
        if ((flags & 0x08) == 0)
        {
            var step = PlatformMultisprite9B93NormalUpdate.Step(
                state.Runtime,
                attacks,
                contactState,
                saint,
                platformDamage,
                seventhSense,
                frameCounter3C,
                cameraDelta43,
                playerX3F,
                playerY40,
                frameStartPlayerAction4E,
                engineSubstate02);
            var next = state with { Runtime = step.State };
            return new(
                next,
                PlatformMultisprite9B93Route.Normal,
                bootstrap,
                step,
                Flag08Bit04: null,
                Flag08Clear04: null,
                DeathDrop: null,
                Substate0D: null,
                step.AttackState,
                step.ContactState,
                step.SeventhSense);
        }

        if ((flags & 0x04) != 0)
        {
            var step = PlatformMultisprite9B93Flag08Bit04.Step(
                state.Runtime,
                attacks,
                contactState,
                saint,
                platformDamage,
                seventhSense,
                frameCounter3C,
                cameraDelta43,
                playerX3F,
                playerY40,
                frameStartPlayerAction4E,
                engineSubstate02);
            var next = state with { Runtime = step.State };
            return new(
                next,
                PlatformMultisprite9B93Route.Flag08Bit04,
                bootstrap,
                Normal: null,
                Flag08Bit04: step,
                Flag08Clear04: null,
                DeathDrop: null,
                Substate0D: null,
                step.AttackState,
                step.ContactState,
                step.SeventhSense);
        }

        var clear04 = PlatformMultisprite9B93Flag08Clear04.Step(
            state.Runtime,
            contactState,
            cameraDelta43,
            playerX3F,
            playerY40,
            frameStartPlayerAction4E,
            engineSubstate02);
        var clearNext = state with { Runtime = clear04.State };
        return new(
            clearNext,
            PlatformMultisprite9B93Route.Flag08Clear04,
            bootstrap,
            Normal: null,
            Flag08Bit04: null,
            Flag08Clear04: clear04,
            DeathDrop: null,
            Substate0D: null,
            attacks,
            clear04.Contacts.ContactState,
            seventhSense);
    }

    private static PlatformMultisprite9B93PersistentState ApplyBootstrap(
        PlatformMultisprite9B93PersistentState state,
        PlatformMultisprite9B93BootstrapResult bootstrap)
    {
        return bootstrap.Outcome switch
        {
            PlatformMultisprite9B93BootstrapOutcome.SelectorZero => state,

            PlatformMultisprite9B93BootstrapOutcome.CooldownDecremented =>
                state with
                {
                    Runtime = state.Runtime with { Mode81 = bootstrap.Mode81 },
                    Cooldown03FA = bootstrap.Cooldown03FA,
                },

            PlatformMultisprite9B93BootstrapOutcome.Initialized =>
                state with
                {
                    Runtime = new PlatformMultisprite9B93RuntimeState(
                        bootstrap.Visual,
                        MergeBootstrapLogical(state.Runtime.Logical, bootstrap.Logical),
                        bootstrap.Mode81),
                    Cooldown03FA = bootstrap.Cooldown03FA,
                    Global03A9 = bootstrap.Global03A9WasWritten
                        ? bootstrap.Global03A9
                        : state.Global03A9,
                },

            _ => throw new InvalidOperationException("Active bootstrap outcome cannot be applied as an initialization result."),
        };
    }

    private static PlatformMultisprite9B93LogicalState MergeBootstrapLogical(
        PlatformMultisprite9B93LogicalState existing,
        PlatformMultisprite9B93LogicalBootstrap bootstrap) =>
        existing with
        {
            Action00 = bootstrap.Action00WasCleared ? (byte)0 : existing.Action00,
            Phase03 = bootstrap.Phase03,
            Profile0C = bootstrap.Profile0C,
            CosmoDrain0D = bootstrap.Profile0D,
            LifeDrain0E = bootstrap.Profile0E,
            SeventhSenseReward0F = bootstrap.Profile0F,
        };

    private static PlatformMultisprite9B93FrameResult Result(
        PlatformMultisprite9B93PersistentState state,
        PlatformMultisprite9B93Route route,
        PlatformMultisprite9B93BootstrapResult bootstrap,
        PlatformAttackState attacks,
        PlatformContactPhaseState contactState,
        int seventhSense) =>
        new(
            state,
            route,
            bootstrap,
            Normal: null,
            Flag08Bit04: null,
            Flag08Clear04: null,
            DeathDrop: null,
            Substate0D: null,
            attacks,
            contactState,
            seventhSense);
}
