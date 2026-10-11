# REBORN platform standing jump

Status: second bounded gameplay movement slice.

This document owns the REBORN decision boundary for ordinary standing-jump initiation and the table-controlled vertical trajectory. The frozen oracle remains `PlatformJumpInitiation`, `PlatformJumpProfile`, `PlatformAirborneVerticalMotion` and their ORIGINAL SPEC fixtures.

## Purpose

Preserve the original ordinary standing jump as a deterministic semantic trajectory while discarding the NES action-state bytes, phase bytes, table pointers and screen-coordinate convention.

The canonical behavior proves two facts that matter to REBORN:

1. a valid standing jump begins and consumes its first vertical sample in the same logical frame;
2. the ordinary jump is a fixed 30-sample trajectory rather than a generic gravity equation.

REBORN therefore models the bounded trajectory directly.

## Semantic contract

`Reborn.Core` owns:

- `RebornStandingJumpProfile`: ordered positive-up displacement samples;
- `RebornStandingJumpState`: semantic vertical position, consumed trajectory ticks, active/completed state;
- `RebornStandingJump.Initiate`: begins the jump and consumes sample 1 immediately;
- `RebornStandingJump.Step`: consumes exactly one further sample per logical tick.

`VerticalPosition` is presentation-independent and increases upward. It is not NES screen Y. A profile sample of `+8` means rise eight world-space pixels; a negative value means the descending side of the table moves downward.

The canonical ordinary standing profile has:

```text
trajectory samples    30
peak rise             58 px
first peak tick       14
net rise after table  35 px
```

After sample 30, `TableComplete=true` and `IsActive=false`. REBORN deliberately refuses another table step. Terminal falling, landing and terrain interaction are not silently appended to this slice.

## Canonical bridge

`OriginalSpecStandingJumpBridge` is the only layer that projects the frozen original trajectory into Core semantics.

It maps:

- `PlatformJumpKind.Standing` profile samples -> positive-up `RebornStandingJumpProfile`;
- canonical screen-Y delta -> semantic rise by sign inversion.

All five canonical Saint indices currently project to the same ordinary standing-jump curve, as proven by parity tests. This is oracle-derived behavior, not a new character-balance decision.

The bridge does not expose canonical action-state values, jump-phase storage or table addresses to Core.

## Same-frame initiation

The original standing-jump initiation establishes the jump state and immediately enters the airborne vertical routine in the same platform frame. REBORN preserves the observable semantic consequence rather than the storage mechanism:

```text
grounded Y
  -> Initiate(profile)
  -> first +8 rise already applied
  -> trajectory tick = 1
```

This prevents an accidental one-tick pause at takeoff.

## Parity coverage

The REBORN self-test runs the complete 30-sample ordinary standing-jump trajectory in parallel with the frozen ORIGINAL SPEC path using collision-free probes.

For every table tick it verifies:

1. per-tick semantic rise equals the sign-converted canonical screen delta;
2. cumulative vertical displacement from takeoff is identical;
3. REBORN trajectory tick matches the number of canonical table entries consumed;
4. first-frame initiation consumes the first sample immediately;
5. the profile has 30 samples, 58 px peak, apex tick 14 and 35 px net rise;
6. the same profile is projected for all five canonical Saint indices;
7. equal initial state/profile produces deterministic results;
8. the slice stops at table completion before the canonical terminal-fall continuation.

The oracle remains ORIGINAL SPEC. REBORN output is not evidence for the 1988 implementation.

## Deliberately excluded

This checkpoint does **not** model:

- landing or floor snapping;
- collision descriptors or ceiling interruption;
- terminal fixed fall after the table;
- directional jumps;
- high jumps;
- airborne horizontal control;
- attacks or hazards;
- camera behavior;
- animation or rendering.

These remain separate systems. In particular, the semantic jump trajectory must stay usable with redesigned stages, collision geometry, camera behavior and animation without importing NES storage conventions into gameplay state.

## Next composition boundary

Once this slice is merged and frozen, horizontal grounded locomotion and the ordinary standing vertical trajectory can be composed into a minimal semantic player-motion state. The next checkpoint should add only the smallest interaction needed to advance movement reconstruction—preferably ordinary standing-jump airborne horizontal control/parity or an equally bounded composition—without broad map/collision or presentation migration.
