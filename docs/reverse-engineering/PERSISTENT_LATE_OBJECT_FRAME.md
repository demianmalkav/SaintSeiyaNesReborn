# Persistent platform late-object frame

Status: **clean-room composition of confirmed main-thread ordering and individually promoted object classes**.

This layer closes the currently reconstructed normal platform-frame ordering from the primary producers through the late attack-object update, while preserving the existing NMI boundary as a separate call.

## Confirmed main-thread order

The continuing path is composed as:

```text
bank-0 $B6D0 generic primary producer
platform exit gate $969D-$9713
bank-1 $8927 scheduled primary producer
pre-player resource phases
player $AAE4
post-player $B94B latch
bank-3 $9B93 multisprite
bank-3 $96B4 auxiliary spawn
bank-3 $9761 auxiliary slot A
bank-3 $9761 auxiliary slot B
bank-3 $A442 primary slot A
bank-3 $A442 primary slot B
bank-3 $A22C player attack-object update
fixed-bank $C402 shared $3C increment
```

The order is not represented as independent per-class updates over copied inputs. Attack objects, physical contact latch/drain state and Seventh Sense are threaded as one mutable stream from each earlier class into every later class.

## Persistent state

`PlatformPersistentLateObjectFrameState` groups three already-distinct ownership domains:

```text
Primary    -> encounter latch, $03B7, primary A/B, $03B8, $03A2,
              Seventh Sense, $039A, $3C
Multisprite -> $07E0/$03FB runtime, $81, $03FA, $03A9
Auxiliary  -> $03B4/$03B5/$03B6, $03A5/$03A6,
              $07B0/$07B8 slots and metadata
```

The grouping is a frame-level composition boundary, not a claim that the original stores these structures contiguously.

## Producer and exit semantics

The generic `$B6D0` producer remains before the platform exit gate. Therefore its mutations persist even if the gate accepts a `$3D` reload or `$70` special transition.

On an accepted platform exit:

- scheduled `$8927` does not run;
- player processing does not run;
- `$9B93`, auxiliaries and primary A/B do not run;
- `$A22C` does not run;
- normal `$3C` does not advance.

This matches the earlier persistent-primary checkpoint.

## Player exceptional exit

After the continuing producer path, pre-player/player processing still owns its own exceptional return routes. If `PlatformPlayerActionDispatchResult.ExitsNormalPlayerLoop` is true:

- completed producer mutations persist;
- post-player late object classes are skipped;
- `$A22C` is represented by its existing explicit skipped result;
- persistent `$9B93` and auxiliary states are unchanged;
- primary A/B are unchanged after the producers;
- `$3C` remains unchanged.

This is distinct from the platform exit gate because the scheduled producer has already run before the player exceptional path is known.

## Shared-state carry

On a normal continuing frame:

### `$9B93 -> auxiliary`

`PlatformMultisprite9B93Runtime` receives the player's current attack objects and post-player contact state. Its resulting:

- `AttackState`;
- `ContactState` (`$76/$7F/$80` semantics);
- `SeventhSense`;

are written back into the player/shared view before auxiliary processing.

### Auxiliary A -> B -> primary

`PlatformAuxiliaryHazardInteractions.StepPair` already composes spawn followed by slot A and slot B, including same-frame update of a freshly spawned hazard. Its final attacks/contact/Seventh Sense become the inputs to the primary pair.

### Primary A -> B

`PlatformHybridEntityCombatSlice.StepPairAfterPlayer` is the extracted reusable primary primitive. It deliberately owns no player simulation, no `$A22C`, and no `$3C` increment.

### `$A22C`

`PlatformAttackFramePhases.UpdateObjectsAfterPlayer` is called exactly once after the primary pair. Newly created or surviving player attack objects therefore receive exactly one late update at the original point in the frame.

The shared frame counter then increments exactly once.

## Discriminating fixtures

The complete frame self-tests require causal ordering rather than only final non-null results:

1. a Hyoga projectile overlapping `$9B93`, an auxiliary hazard and primary A is consumed by `$9B93`; auxiliary and primary classes receive the retired attack state;
2. with `$9B93` inactive, auxiliary A consumes an overlapping projectile and primary A cannot receive it;
3. auxiliary contact seeds `$76/$7F/$80`, and the later primary contact sees `ContactLatchActive` instead of overwriting the earlier drain profile;
4. a fresh `$96B4` auxiliary spawn animates/moves through `$9761` in the same frame, while an unrelated player projectile receives one and only one `$A22C` step and `$3C` increments once;
5. exceptional player exit leaves `$9B93`, auxiliary and primary late-object state untouched and skips `$A22C/$3C`.

## Scope boundary

This compositor covers the promoted gameplay semantics of the current late-object chain. It does not promote full renderer-owned sprite/tile transfer state, higher native state-machine transitions after semantic platform exits, or any still-unresolved primary type/action reachability outside the promoted closures.

No ROM payload is committed.
