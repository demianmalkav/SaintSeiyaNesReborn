# Auxiliary hazard interaction ordering

Status: **CONFIRMED by static ROM flow** for the `$976A -> $9887/$9896` paths described here.

This document complements `AUXILIARY_HAZARD_SLOTS.md`. The slot/spawn/motion subsystem is independent from the parent-bound `$A908` child/sprite record.

## Relative frame order

For each independent auxiliary slot, `$976A` first performs motion/animation/removal. If the slot survives, interaction order is:

```text
slot motion / animation
    -> $9887 / $989C / $98BA   auxiliary hazard -> player contact
    -> $9896 / $989C / $9915   player projectile -> auxiliary hazard
```

The projectile branch exists only for the ordinary auxiliary families. `$F4/$F5` executes the contact path but does not fall through to `$9896/$9915`.

This is intentionally different from the common movable-entity interaction path, where `$9915` projectile hit runs before `$98BA` contact.

## `$989C` logical-position synchronization

Immediately before either collision routine, `$989C` copies the current visual slot position into the logical metadata record selected by `$16`:

```text
logical +$02 = slot Y
logical +$01 = slot X
```

It then installs:

```text
$79 = $04
$7A = $04
$7B = $03
$7C = $03
```

Therefore both `$98BA` and `$9915` use the same reduced auxiliary geometry.

The clean model exposes this through:

- `PlatformContactHitboxParameters.AuxiliaryHazard = (4,4,3,3)` for `$98BA`;
- `PlatformAuxiliaryHazardInteractions.ProjectileHitbox = (4,4,3,3)` for `$9915`.

## Contact first

The auxiliary contact phase observes the shared physical bytes `$76/$7F/$80` exactly like the common-entity contact routine, but with the auxiliary geometry above.

When slot A triggers contact, it writes:

```text
$76 = $20
$7F = logical +$0E   Life drain ticks
$80 = logical +$0D   Cosmo drain ticks
```

Slot B runs later in `$9761`, so it sees slot A's newly written `$76=$20`. If B overlaps the player in the same frame, `$98BA` rejects it at the latch gate and B cannot overwrite `$7F/$80`.

## Projectile hit after contact

For ordinary auxiliary families, `$9896` invokes the existing `$9915` three-slot sequence only after the contact attempt.

The player attack slots are processed in the already reconstructed order:

```text
$0730 -> $0738 -> $0740
```

Mutations are immediately visible to later attacks and to slot B. In particular, Hyoga/Shiryu standard hit consumption can retire a projectile while processing slot A; slot B then sees that attack as inactive.

## Kind 1-4 hit routing

`$989C` copies `$03B5` into logical offset `+$09`; for the promoted stage hazards this is kind/type 1..4.

After an accepted `$9915` overlap, those types do **not** enter the ordinary HP subtraction path. The router reaches the type `< 5` response:

```text
logical Y += 6
logical action/state = $E0
```

Thus profile byte `+$0C` copied from `$9C92` must not be interpreted as effective ordinary HP for this subsystem merely because common entity records use the same offset as HP. The clean model currently retains the raw byte in metadata for layout fidelity, while the projectile router correctly takes the drop-reaction path for kind 1..4.

The `$E0/Y+6` mutation is a **logical-record mutation**. No reverse copy from `$03DA/$03EA` to `$07B0/$07B8` occurs in the `$976A` path reconstructed here, so the clean implementation does not invent one.

## `$F4/$F5` exception

The homing family still reaches `$9887/$98BA` after motion and therefore can seed `$76/$7F/$80`.

It does not reach `$9896/$9915`. An overlapping player projectile is therefore left untouched by this updater.

## Newly spawned hazards are live immediately

Frame order is `$96B4` spawn followed by `$9761` update. A hazard successfully created in an empty slot can therefore animate, move and interact in the same frame in which it is spawned.

For example, kind 1 with entropy 0 on a `$3C & 3 == 0` frame:

```text
spawn: sprite $80, X=$02
same $9761 pass: $80->$81, X += 2
```

before subsequent common-entity processing.

## Two-slot clean-room composition

`PlatformAuxiliaryHazardInteractions.StepPair` now mirrors the confirmed pair ordering:

1. run `$96B4` spawner;
2. update/interact slot A;
3. carry shared `$03A5/$03A6`, player attacks, `$76/$7F/$80`, and Seventh Sense forward;
4. update/interact slot B.

The next integration step is to place this pair composition into the higher-level platform-frame pipeline before `$A442` common entity A/B processing, while preserving a single pre-player resource/player simulation for the frame.
