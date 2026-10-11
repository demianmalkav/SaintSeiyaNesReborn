# REBORN standing-jump airborne horizontal control

Status: third bounded gameplay movement slice.

This document owns the REBORN decision boundary for horizontal air control during an ordinary standing jump in collision-free space. The frozen oracle remains `PlatformAirborneHorizontalMotion`, `PlatformAirborneSession` and their ORIGINAL SPEC fixtures.

## Purpose

Preserve the original ordinary standing jump's **semantic mid-air steering identity** while discarding the NES split between player-local X and camera scroll, controller bitmasks and frame-counter storage.

For a vertical/standing jump in open space, the canonical path proves:

- horizontal input does not convert the jump into a directional trajectory after takeoff;
- Right is tested before Left when both canonical direction bits are present;
- horizontal drift is parity-shaped: `0 px` on even logical phase and `1 px` on odd logical phase;
- airborne directional input does **not** rewrite facing; the takeoff facing is preserved;
- the active-frame ordering is vertical displacement first, horizontal air control second, logical-frame parity advance last.

REBORN models exactly those semantics and nothing from collision/landing yet.

## Semantic contract

`Reborn.Core` owns:

- `RebornStandingJumpAirControlProfile`: phase-shaped drift magnitude;
- `RebornStandingJumpAirState`: world X, preserved takeoff facing and logical parity;
- `RebornStandingJumpAirControl.Step`: one deterministic free-space steering update;
- `RebornStandingJumpMotionState`: composition of the already-frozen vertical trajectory and airborne horizontal state;
- `RebornStandingJumpMotion.Initiate`: ordinary neutral takeoff, vertical sample first, horizontal control second;
- `RebornStandingJumpMotion.Step`: one further vertical sample followed by one horizontal steering update.

The frozen ordinary standing-jump air-control profile is:

```text
even logical phase   0 px drift
odd logical phase    1 px drift
```

`RebornHorizontalInput.Right` adds that drift to `WorldX`; `Left` subtracts it; `Neutral` leaves X unchanged. Every active jump tick advances the logical phase even when the drift magnitude is zero or input is neutral.

Facing is intentionally **not** changed by airborne steering in this slice.

## Standing takeoff boundary

A non-neutral horizontal input at takeoff belongs to the canonical directional-jump family. This slice therefore rejects non-neutral `RebornStandingJumpMotion.Initiate` input rather than silently widening scope.

The ordinary takeoff sequence is:

```text
neutral standing takeoff
  -> consume vertical sample 1
  -> apply standing-jump air control for the same logical tick
  -> advance logical parity
```

Directional-jump-at-takeoff behavior remains a separate checkpoint.

## Canonical bridge

`OriginalSpecStandingJumpAirControlBridge` terminates the original storage/presentation model.

It maps:

- canonical `scroll + player_x` -> semantic `WorldX`;
- canonical facing byte -> `RebornFacing`;
- canonical frame parity -> `RebornMotionPhase`;
- canonical direction bits -> `RebornHorizontalInput`, preserving Right-before-Left priority;
- frozen vertical-jump drift law -> `RebornStandingJumpAirControlProfile(0, 1)`.

No controller masks, player-X/scroll storage, RAM addresses, camera thresholds or collision descriptors enter `Reborn.Core`.

## Composition order

`PlatformAirborneSession` establishes the observable order used as the oracle:

1. vertical jump step;
2. if still eligible, horizontal air-control step using the current frame parity;
3. frame parity advances at the end of the frame.

`RebornStandingJumpMotion` preserves that order explicitly. This matters because moving parity advancement before air control would invert the 0/1 drift cadence and create a one-tick mismatch.

## Parity coverage

The REBORN self-test runs one complete 30-sample ordinary standing jump against `PlatformAirborneSession` in an open synthetic stage.

Coverage verifies:

1. the semantic drift profile is even `0` / odd `1`;
2. neutral/left/right steering produces identical tick-level world-space delta;
3. takeoff facing remains unchanged despite later opposite-direction input;
4. Right-before-Left canonical priority is preserved by the bridge;
5. vertical-first then horizontal composition matches the frozen frame order;
6. semantic `WorldX` remains correct while the canonical path transitions from player-local movement into camera scroll;
7. logical phase matches the canonical end-of-frame counter progression;
8. equal initial state plus equal input sequence is deterministic;
9. the composed slice stops at standing-table completion before terminal fall;
10. no collision, edge block or landing-side correction occurs in the parity fixture.

The oracle remains ORIGINAL SPEC. REBORN behavior is not evidence for the 1988 implementation.

## Deliberately excluded

This checkpoint does **not** model:

- floor, ceiling or side collision;
- landing or landing-side correction;
- terminal fixed fall after the standing table;
- directional jump at takeoff;
- high-jump vertical profiles;
- directional-jump forced horizontal trajectories;
- attacks or hazards;
- camera policy as gameplay state;
- animation or rendering.

The camera remains free to be completely redesigned. The only preserved invariant is world-space movement produced by the selected canonical behavior.

## Next bounded movement candidate

With grounded locomotion, ordinary standing vertical motion and standing-jump free-space steering now independently semantic, the next smallest movement extension should remain collision-free. Prefer introducing the canonical **high standing-jump vertical profiles** as semantic trajectories before terrain/landing composition, because this reuses the proven vertical abstraction while exposing the first material per-Saint airborne variation without importing collision or presentation constraints.
