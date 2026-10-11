# REBORN high standing jump

Status: bounded platform-movement slice.

This document owns the REBORN decision boundary for canonical high standing-jump initiation and complete table-controlled vertical profiles. The frozen oracle remains `PlatformJumpInitiation`, `PlatformJumpProfile`, `PlatformAirborneVerticalMotion` and their ORIGINAL SPEC fixtures.

## Purpose

Preserve the original `Up + jump` high-standing-jump identity, including its real Saint-specific curve variation, while keeping original action bytes, selector storage, table pointers, screen coordinates, collision and presentation mechanics outside `Reborn.Core`.

The high jump reuses the already-frozen positive-up table-trajectory primitive rather than introducing a gravity equation or a second timing model.

## Semantic takeoff contract

`Reborn.Core` owns `RebornHighStandingJumpTakeoffIntent`:

- `JumpRequested` must be true;
- `UpwardIntent` must be true;
- horizontal takeoff input must be `Neutral`.

This matches the selected observable behavior: a purely vertical jump request plus upward intent selects the high-standing family. A horizontal takeoff direction belongs to the directional-jump family and is deliberately rejected here rather than silently reclassified.

Ground/support eligibility, input latching and collision are separate concerns and are not copied into this semantic takeoff DTO.

A valid high standing jump consumes its first vertical sample in the same logical tick as initiation.

## Semantic profile families

REBORN makes the material curve-sharing families explicit:

```text
Seiya          unique
Shun / Ikki    shared
Hyoga / Shiryu shared
```

They are represented by `RebornHighStandingJumpProfileFamily` and `RebornHighStandingJumpProfile`.

Frozen metrics:

| Family | Table ticks | First rise | Peak rise | First apex tick | Net rise after table |
|---|---:|---:|---:|---:|---:|
| Seiya | 58 | +9 px | +103 px | 28 | +51 px |
| Shun / Ikki | 48 | +9 px | +88 px | 23 | +51 px |
| Hyoga / Shiryu | 38 | +9 px | +71 px | 18 | +41 px |

The complete ordered sample sequences are supplied by `Reborn.OriginalBridge` from the frozen semantic oracle. Core owns no original table address or selector value.

## Trajectory reuse

`RebornHighStandingJumpProfile` wraps the existing `RebornStandingJumpProfile` table primitive. `RebornHighStandingJump.Initiate/Step` delegate bounded sample consumption to the already-frozen deterministic trajectory behavior:

```text
valid high-standing takeoff
  -> Initiate
  -> consume sample 1 immediately
  -> one sample per logical tick
  -> stop at table completion
```

The historical class name `RebornStandingJumpProfile` therefore acts here as the frozen positive-up table-trajectory primitive. This slice does not broaden or rewrite the ordinary-standing contract.

## Canonical bridge

`OriginalSpecHighStandingJumpBridge` is the only layer that knows canonical Saint identity and original input representation.

It maps:

- each canonical Saint to one of the three semantic high-jump families;
- the frozen high-jump displacement table to positive-up REBORN samples;
- canonical takeoff input to semantic `JumpRequested`, `UpwardIntent` and horizontal intent.

A canonical `Up + jump` neutral takeoff projects to a valid high-standing request. Adding Left or Right makes the semantic request invalid for this slice because directional jump owns that takeoff family.

## Parity coverage

The REBORN self-test runs every Saint's complete high-standing table in parallel with `PlatformJumpInitiation` plus `PlatformAirborneVerticalMotion`, using collision-free probes.

For every table tick it verifies:

1. canonical initiation selects high standing and requests same-frame airborne execution;
2. semantic takeoff is valid only for jump + upward intent with neutral horizontal takeoff;
3. per-tick positive-up displacement matches the frozen oracle;
4. cumulative semantic vertical position matches canonical movement;
5. the first +9 sample is applied on the takeoff tick;
6. trajectory tick count and final pre-terminal phase agree;
7. Seiya, Shun/Ikki and Hyoga/Shiryu remain three distinct curve families;
8. shared-family Saints have identical complete sample sequences;
9. equal initial state/profile replays deterministically;
10. the bounded REBORN path refuses to continue implicitly into terminal fixed fall.

The oracle remains ORIGINAL SPEC. REBORN output is not evidence for the original implementation.

## Deliberately excluded

This checkpoint does **not** model:

- floor, ceiling or side collision;
- landing or floor snapping;
- terminal fixed fall after table completion;
- directional-jump takeoff or forced horizontal trajectories;
- high-jump horizontal/collision composition beyond the already-frozen ordinary free-space concepts;
- attacks or hazards;
- camera behavior;
- animation or rendering.

## Next movement boundary

After this slice, ordinary and high vertical standing families are both semantic and deterministic. The next bounded extension should consume the directional-jump family separately, beginning with directional takeoff plus its Saint-specific vertical profiles before forced horizontal trajectory/collision behavior is composed.
