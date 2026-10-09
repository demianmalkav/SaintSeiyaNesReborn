using System.Runtime.CompilerServices;
using SaintSeiyaNesReborn.OriginalSpec;
using SaintSeiyaNesReborn.OriginalSpec.Platform;

internal static class PersistentLateObjectFrameChecks
{
    [ModuleInitializer]
    internal static void Run()
    {
        CheckMultispriteProjectileConsumptionPrecedesAuxiliaryAndPrimary();
        CheckAuxiliaryProjectileConsumptionPrecedesPrimary();
        CheckAuxiliaryContactSuppressesPrimaryContact();
        CheckFreshAuxiliarySpawnUpdatesBeforeSingleLateAttackStep();
        CheckExceptionalPlayerExitSkipsAllLateObjectsAndCounterAdvance();
    }

    private static void CheckMultispriteProjectileConsumptionPrecedesAuxiliaryAndPrimary()
    {
        var attack = HyogaProjectile(x: 0x4E, y: 0x50);
        var player = Player(
            PlatformSaintIndex.Hyoga,
            x: 0x10,
            y: 0x20,
            attack: PlatformAttackState.Empty with { Slot0 = attack });
        var primaryA = PrimarySlot(type: 0x05, action: 0x10, x: 0x4E, y: 0x50, hp: 30);
        var state = State(
            primaryA,
            FreePrimary(),
            MultispriteNormal(hp: 30),
            AuxiliaryPair(
                AuxiliaryAt(x: 0x4C, y: 0x50, sprite: 0x80),
                PlatformAuxiliaryHazardSlot.Empty,
                Metadata(kind: 1, cosmo: 2, life: 2),
                default),
            frameCounter: 1);

        var frame = Step(state, player, contact: new PlatformContactPhaseState(1, default));

        var multiHit = frame.Multisprite!.Normal!.Value.ProjectileHits!.Results[0].Result;
        Require(multiHit.Outcome == PlatformProjectileHitOutcome.HpSurvived
            && multiHit.StandardConsumptionApplied,
            "$9B93 normal route consumes the Hyoga projectile first");
        Require(frame.Multisprite.State.Runtime.Logical.Profile0C == 20,
            "$9B93 receives the shared platform damage");

        Require(frame.Auxiliary!.SlotA.ProjectileHits is not null
            && frame.Auxiliary.SlotA.ProjectileHits.Results[0].Result.Outcome == PlatformProjectileHitOutcome.NoOverlap,
            "auxiliary A receives the already-retired projectile from $9B93");
        Require(frame.PrimaryPair!.SlotA.State.Entity.HitPoints == 30,
            "primary A cannot be hit by a projectile consumed by earlier $9B93");
        Require(frame.PlayerAfterLatePhases!.State.AttackState.Slot0.Object.Type == 0xFE,
            "retired projectile remains retired through all later classes and A22C");
    }

    private static void CheckAuxiliaryProjectileConsumptionPrecedesPrimary()
    {
        var attack = HyogaProjectile(x: 0x40, y: 0x40);
        var player = Player(
            PlatformSaintIndex.Hyoga,
            x: 0x70,
            y: 0x70,
            attack: PlatformAttackState.Empty with { Slot0 = attack });
        var primaryA = PrimarySlot(type: 0x05, action: 0x10, x: 0x40, y: 0x40, hp: 30);
        var state = State(
            primaryA,
            FreePrimary(),
            PlatformMultisprite9B93PersistentState.Empty,
            AuxiliaryPair(
                AuxiliaryAt(x: 0x3E, y: 0x40, sprite: 0x80),
                PlatformAuxiliaryHazardSlot.Empty,
                Metadata(kind: 1, cosmo: 2, life: 2),
                default),
            frameCounter: 1);

        var frame = Step(state, player, contact: new PlatformContactPhaseState(1, default));
        var auxHit = frame.Auxiliary!.SlotA.ProjectileHits!.Results[0].Result;

        Require(frame.Multisprite!.Route == PlatformMultisprite9B93Route.BootstrapSelectorZero,
            "empty selector-zero $9B93 leaves the projectile untouched before auxiliaries");
        Require(auxHit.Outcome == PlatformProjectileHitOutcome.DropReaction
            && auxHit.StandardConsumptionApplied,
            "auxiliary A consumes the projectile on its $9915 path");
        Require(frame.PrimaryPair!.SlotA.State.Entity.HitPoints == 30,
            "primary A receives the retired attack state from auxiliary A");
    }

    private static void CheckAuxiliaryContactSuppressesPrimaryContact()
    {
        var player = Player(PlatformSaintIndex.Seiya, x: 0x40, y: 0x40);
        var primaryA = PrimarySlot(
            type: 0x05,
            action: 0x10,
            x: 0x40,
            y: 0x40,
            hp: 30,
            life: 9,
            cosmo: 8);
        var state = State(
            primaryA,
            FreePrimary(),
            PlatformMultisprite9B93PersistentState.Empty,
            AuxiliaryPair(
                AuxiliaryAt(x: 0x3E, y: 0x40, sprite: 0x80),
                PlatformAuxiliaryHazardSlot.Empty,
                Metadata(kind: 1, cosmo: 2, life: 3),
                default),
            frameCounter: 1);

        var frame = Step(state, player, contact: new PlatformContactPhaseState(0, default));

        Require(frame.Auxiliary!.SlotA.Contact?.Outcome == PlatformEntityContactOutcome.ContactTriggered,
            "auxiliary A seeds contact before primary A");
        Require(frame.PrimaryPair!.SlotA.Common!.Interaction!.ContactPhase.Contact.Outcome ==
                PlatformEntityContactOutcome.ContactLatchActive,
            "primary A sees the auxiliary-seeded $76 latch and cannot overwrite it");
        Require(frame.ContactState.HazardLatch76 == 0x20
            && frame.ContactState.DrainState == new ContactDrainState(LifeTicks: 3, CosmoTicks: 2),
            "final $76/$7F/$80 state remains owned by the earliest auxiliary contact");
        Require(frame.PlayerAfterLatePhases!.State.Special76 == 0x20,
            "player view of physical $76 remains synchronized after all late classes");
    }

    private static void CheckFreshAuxiliarySpawnUpdatesBeforeSingleLateAttackStep()
    {
        var attack = new PlatformAttackSlot(
            new PlatformAttackObject(
                Y: 0x80,
                Type: 0x64,
                Facing: 0x40,
                X: 0x40,
                Field4: 0,
                Field5: 0,
                Field6: 0,
                AuxiliaryX: 0),
            RangeCounter: 3);
        var player = Player(
            PlatformSaintIndex.Seiya,
            x: 0x60,
            y: 0x50,
            attack: PlatformAttackState.Empty with { Slot0 = attack });
        var aux = new PlatformAuxiliaryHazardSpawnerState(
            CurrentKind03B4: 1,
            ActiveKind03B5: 1,
            Cooldown03B6: 0,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: PlatformAuxiliaryHazardSlot.Empty,
            SlotB: PlatformAuxiliaryHazardSlot.Empty,
            MetadataA: default,
            MetadataB: default);
        var state = State(
            FreePrimary(),
            FreePrimary(),
            PlatformMultisprite9B93PersistentState.Empty,
            aux,
            frameCounter: 0,
            encounter: PlatformPrimaryEncounterLatchState.Empty,
            staged03B7: 0);

        var frame = Step(state, player, contact: new PlatformContactPhaseState(1, default));

        Require(frame.Auxiliary!.Spawn.SlotAOutcome == PlatformAuxiliaryHazardSpawnAttemptOutcome.Spawned,
            "$96B4 creates auxiliary A before its updater");
        Require(frame.Auxiliary.SlotA.Update.Slot.SpriteType1 == 0x81
            && frame.Auxiliary.SlotA.Update.Slot.X3 == 0x04,
            "fresh $80 auxiliary immediately animates and moves in the same frame");
        Require(frame.AttackObjectPhase!.UpdatedAttackObjects,
            "$A22C executes on the normal path after all object classes");
        Require(frame.PlayerAfterLatePhases!.State.AttackState.Slot0.RangeCounter == 2
            && frame.PlayerAfterLatePhases.State.AttackState.Slot0.Object.X == 0x45,
            "one and only one late A22C step changes range 3->2 and X $40->$45");
        Require(frame.State.Primary.FrameCounter3C == 1,
            "shared $3C increments exactly once after A22C");
    }

    private static void CheckExceptionalPlayerExitSkipsAllLateObjectsAndCounterAdvance()
    {
        var existingAttack = new PlatformAttackSlot(
            new PlatformAttackObject(0x60, 0x64, 0x40, 0x40, 0, 0, 0, 0),
            RangeCounter: 3);
        var player = Player(
            PlatformSaintIndex.Seiya,
            x: 0x40,
            y: 0x60,
            action4D: 0x80,
            special76: 0,
            attack: PlatformAttackState.Empty with
            {
                ActionState4D = 0x80,
                Slot0 = existingAttack,
            });
        var multi = MultispriteNormal(hp: 30);
        var aux = AuxiliaryPair(
            AuxiliaryAt(x: 0x3E, y: 0x40, sprite: 0x80),
            PlatformAuxiliaryHazardSlot.Empty,
            Metadata(kind: 1, cosmo: 2, life: 2),
            default);
        var primaryA = PrimarySlot(type: 0x05, action: 0x10, x: 0x50, y: 0x50, hp: 30);
        var state = State(primaryA, FreePrimary(), multi, aux, frameCounter: 7);

        var frame = Step(state, player, contact: new PlatformContactPhaseState(0, default));

        Require(frame.PlayerExitedBeforeLateObjects,
            "$80 reload is recognized as an exceptional player-loop exit");
        Require(frame.Multisprite is null && frame.Auxiliary is null && frame.PrimaryPair is null,
            "$9B93, auxiliaries and primary A/B are all skipped after exceptional player exit");
        Require(frame.AttackObjectPhase?.SkippedBecausePlayerLoopExited == true,
            "$A22C remains explicitly skipped on the exceptional route");
        Require(frame.PlayerAfterLatePhases!.State.AttackState.Slot0 == existingAttack,
            "existing attack object is not moved or aged when A22C is unreachable");
        Require(frame.State.Multisprite == multi && frame.State.Auxiliary == aux,
            "persistent earlier object classes remain byte-for-byte unchanged");
        Require(frame.State.Primary.SlotA == primaryA
            && frame.State.Primary.FrameCounter3C == 7,
            "primary slots and normal $3C cadence do not advance on exceptional exit");
    }

    private static PlatformPersistentLateObjectMainThreadResult Step(
        PlatformPersistentLateObjectFrameState state,
        PlatformPlayerActionState player,
        PlatformContactPhaseState contact) =>
        PlatformPersistentLateObjectFrame.StepMainThread(
            Stage(Page(0, raw: state.Primary.EncounterLatch.ActiveEngine58, hp: 30)),
            cameraLow44: 0x02,
            cameraHigh45: 0,
            scrollX: 0,
            player,
            PlatformInput.None,
            new PlatformFrameResources(99, 50),
            contact,
            state,
            scheduledEntries: Array.Empty<PlatformSpecialSpawnEntry>(),
            PlatformHitboxParameters.Common,
            PlatformHitboxParameters.Common,
            entropy48: 0,
            cameraDelta43: 0,
            engineSubstate02: 1,
            engineState00: 0x20,
            alternateParent08_03AB: 0x66,
            multispriteFlag74: 1,
            multispriteStageSelector: 0);

    private static PlatformPersistentLateObjectFrameState State(
        PlatformHybridEntitySlotState slotA,
        PlatformHybridEntitySlotState slotB,
        PlatformMultisprite9B93PersistentState multisprite,
        PlatformAuxiliaryHazardSpawnerState auxiliary,
        byte frameCounter,
        PlatformPrimaryEncounterLatchState? encounter = null,
        byte? staged03B7 = null)
    {
        var active = encounter ?? ActiveEncounter(raw: 0x05, hp: 30);
        var primary = new PlatformPersistentPrimaryEntityFrameState(
            active,
            staged03B7 ?? active.ActiveEngine58,
            slotA,
            slotB,
            Cooldown03B8: 0,
            LastTriggerLow03A2: 0,
            SeventhSense: 0,
            GlobalCounter039A: 0,
            FrameCounter3C: frameCounter);
        return new(primary, multisprite, auxiliary);
    }

    private static PlatformPrimaryEncounterLatchState ActiveEncounter(byte raw, int hp)
    {
        var config = Config(raw, hp);
        return new PlatformPrimaryEncounterLatchState(raw, config);
    }

    private static PlatformMultisprite9B93PersistentState MultispriteNormal(byte hp)
    {
        var visual = new PlatformMultisprite9B93VisualState(
            new PlatformMultisprite9B93Part(0x50, 0xB4, 0x02, 0x50),
            new PlatformMultisprite9B93Part(0x50, 0xB5, 0x02, 0x58),
            new PlatformMultisprite9B93Part(0x58, 0xB6, 0x02, 0x50),
            new PlatformMultisprite9B93Part(0x58, 0xB7, 0x02, 0x58));
        var logical = new PlatformMultisprite9B93LogicalState(
            Action00: 0x10,
            X01: 0x50,
            Y02: 0x50,
            Phase03: 0,
            Field05: 0,
            Type09: 0x05,
            Profile0C: hp,
            CosmoDrain0D: 2,
            LifeDrain0E: 3,
            SeventhSenseReward0F: 0x10);
        return new(
            new PlatformMultisprite9B93RuntimeState(visual, logical, Mode81: 0),
            Cooldown03FA: 7,
            Global03A9: 0x55);
    }

    private static PlatformAuxiliaryHazardSpawnerState AuxiliaryPair(
        PlatformAuxiliaryHazardSlot a,
        PlatformAuxiliaryHazardSlot b,
        PlatformAuxiliaryHazardMetadata ma,
        PlatformAuxiliaryHazardMetadata mb) =>
        new(
            CurrentKind03B4: 0,
            ActiveKind03B5: 1,
            Cooldown03B6: 0,
            HorizontalStep03A5: 0,
            VerticalStep03A6: 0,
            SlotA: a,
            SlotB: b,
            MetadataA: ma,
            MetadataB: mb);

    private static PlatformAuxiliaryHazardSlot AuxiliaryAt(byte x, byte y, byte sprite) =>
        PlatformAuxiliaryHazardSlot.Empty with
        {
            X3 = x,
            Y0 = y,
            SpriteType1 = sprite,
            Flags2 = 0x40,
            MirrorFlags6 = 0x40,
        };

    private static PlatformAuxiliaryHazardMetadata Metadata(byte kind, byte cosmo, byte life) =>
        new(
            Kind09: kind,
            HitPoints0C: 0x1E,
            CosmoDrainTicks0D: cosmo,
            LifeDrainTicks0E: life,
            SeventhSenseReward0F: 0x32);

    private static PlatformHybridEntitySlotState PrimarySlot(
        byte type,
        byte action,
        byte x,
        byte y,
        byte hp,
        byte life = 1,
        byte cosmo = 1) =>
        PlatformHybridEntitySlotState.Common(
            new PlatformCommonEntityRuntimeState(
                new PlatformCommonEntityMotionState(
                    ActionState: action,
                    X: x,
                    Y: y,
                    StatePhase: 0,
                    GroundDescriptor: 0xE0,
                    DecisionTimer: 5,
                    FlagsFacing: 0x40,
                    Type: type,
                    TerrainProbeRight: 0,
                    TerrainProbeLeft: 0),
                HitPoints: hp,
                LifeDrainTicks: life,
                CosmoDrainTicks: cosmo,
                SeventhSenseRewardBcd: 0x10),
            visualSpritePlus1: 0xFD);

    private static PlatformHybridEntitySlotState FreePrimary() =>
        PlatformHybridEntitySlotState.Common(
            new PlatformCommonEntityRuntimeState(
                new PlatformCommonEntityMotionState(0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                0, 0, 0, 0),
            visualSpritePlus1: 0xFE);

    private static PlatformAttackSlot HyogaProjectile(byte x, byte y) =>
        new(
            new PlatformAttackObject(y, 0x64, 0x40, x, 0, 0, 0, 0),
            RangeCounter: 3);

    private static PlatformPlayerActionState Player(
        PlatformSaintIndex saint,
        byte x,
        byte y,
        byte action4D = 0,
        byte special76 = 0,
        PlatformAttackState? attack = null) =>
        new(
            saint,
            new PlatformHorizontalState(x, 0, 0, 0x40),
            y,
            0,
            action4D,
            0,
            0,
            0,
            0,
            0,
            special76,
            0,
            attack ?? PlatformAttackState.Empty);

    private static PlatformPrimaryEncounterSpawnConfig Config(byte raw, int hp) =>
        PlatformPrimaryEncounterSpawnConfig.FromEncounter(Encounter(raw, hp));

    private static PlatformStagePage Page(int index, byte raw, int hp) =>
        new(index, GroundDescriptors(), Encounter(raw, hp));

    private static PlatformPrimaryEncounter Encounter(byte raw, int hp) =>
        raw == 0
            ? new PlatformPrimaryEncounter(0, 0, 0, false, false, null)
            : new PlatformPrimaryEncounter(
                Raw: raw,
                TypeId: (byte)(raw & 0x0F),
                Tier: (byte)((raw >> 4) & 0x03),
                SecondCommonSlotEnabled: (raw & 0x80) != 0,
                Bit6Unknown: (raw & 0x40) != 0,
                Stats: new PlatformEncounterStats(hp, 1, 1, 4));

    private static PlatformStageMap Stage(params PlatformStagePage[] pages) => new(0, pages);

    private static byte[] GroundDescriptors()
    {
        var descriptors = new byte[PlatformStagePage.DescriptorCount];
        descriptors[4 * PlatformStagePage.Columns] = 0xA8;
        descriptors[4 * PlatformStagePage.Columns + 15] = 0xA8;
        return descriptors;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition)
            throw new InvalidOperationException($"Persistent late-object frame self-test failed: {label}");
    }
}
