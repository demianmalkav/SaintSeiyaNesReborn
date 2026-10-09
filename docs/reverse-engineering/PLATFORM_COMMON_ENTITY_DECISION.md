# Common entity decision dispatcher — `$A970-$AA63`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the common type dispatch, decision-timer cadence, relative player-position decision and terrain-only branches are now represented literally in C# OriginalSpec.

## Type dispatch at `$A970`

The common path first reads entity type offset `+$09`.

Exact dispatch:

- type `$07` -> terrain-facing branch `$AA1F`;
- type `$0A` -> terrain-facing branch `$AA1F`;
- type `$0B` -> terrain-facing branch `$AA1F`;
- type `< $08` other than `$07` -> decision timer `$A98B`;
- all other types `>= $08` -> return from this common decision path.

This is narrower than the earlier broad description of a universal common terrain check. Ordinary types `$00-$06` do **not** execute `$AA1F` every update through this dispatcher; their path is timer-driven.

## Decision timer `$A98B`

For types `$00-$06`:

```text
timer = timer - 1   ; 8-bit SEC/SBC #1
if timer != 0:
    return
```

A raw zero therefore wraps to `$FF`; zero is not treated as a special idle value by this instruction sequence.

When `1 -> 0`, `$AA64` immediately reseeds before the relative-position decision:

```text
timer = ($48 & $3F) + $1F
```

Range 31..94.

## Relative player decision `$A999+`

The original uses unsigned screen-local bytes and preserves 8-bit wrap. The clean-room model therefore does the same rather than converting these comparisons to unconstrained modern coordinates.

### Entity left of player (`entity_x < player_x`)

- facing right (`$40` set): candidate jump `$31` immediately;
- facing left: enter the shared away-facing branch `$A9AC`.

### Entity at/right of player (`entity_x >= player_x`)

If facing right, enter `$A9AC`.

If facing left:

1. if `entity_x >= $C0`, return unchanged;
2. compute `distance = entity_x - player_x`;
3. distance `< $20` -> candidate jump `$32`;
4. distance `>= $20` -> candidate jump `$31`.

## Away-facing branch `$A9AC-$A9D9`

When player jump phase `$49 == 0`, the routine first checks:

```text
(entity_y - 1) >= player_y -> return unchanged
```

If player `$49 != 0`, this vertical gate is skipped.

Then the original performs a wrapping two-boundary comparison:

```text
upper = byte(player_x + $30)
if upper < entity_x:
    toggle facing
    return

lower = byte(upper - $60)
if lower < entity_x:
    return unchanged

toggle facing
```

This is intentionally represented literally. Replacing it with a simple absolute-distance test would not be guaranteed equivalent around byte wrap/boundary cases.

## Jump initialization `$A9FA`

Candidate action is `$31` or `$32`.

The common helper explicitly rejects types:

- `$08`;
- `$09`;
- `$0C`;
- `$0D`;
- `$0E`.

Otherwise it writes:

```text
action_state = candidate
state_phase  = 1
```

Those excluded types are not normally reachable through the `$A970` timer dispatch, but the checks are preserved in the reusable semantic model because they exist in the original helper.

## Terrain-only path `$AA1F-$AA63`

Types `$07/$0A/$0B` bypass decision-timer decrement entirely.

Facing left uses record offset `+$0B`; facing right uses `+$0A`. Exact descriptor families and the type `$07` `$E4+` exception are implemented by `PlatformCommonEntityMotion.TerrainForcesTurn`.

When terrain forces a turn:

```text
flags_facing ^= $40
```

Otherwise the record is unchanged.

## Clean-room implementation

`PlatformCommonEntityDecision.Step` returns both the resulting entity state and an explicit semantic outcome:

- `NoCommonDecisionPath`;
- `TimerPending`;
- `TimerExpiredNoChange`;
- `FacingToggled`;
- `JumpStartedRight`;
- `JumpStartedLeft`;
- `TerrainNoTurn`;
- `TerrainTurned`.

Tests cover the exact timer wrap/reseed, relative-X branches, `$C0` cutoff, player-jump vertical bypass, terrain-only types and 8-bit wrapped-window behavior.

No ROM payload is embedded.
