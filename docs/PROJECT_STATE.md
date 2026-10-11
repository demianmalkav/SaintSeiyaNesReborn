# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / platform movement reconstruction`
- State: `READY_FOR_NEXT`
- Closing technical checkpoint: PR `#177` — ordinary standing-jump airborne horizontal control in collision-free space.
- Exact verified code head before documentation-only state synchronization: `d98a7c5d5739739ee7430c7f99abc1ea954e5ba6`.
- Verification on that exact code head:
  - `REBORN architecture` #35: `SUCCESS`;
  - `ORIGINAL SPEC tests` #458: `SUCCESS`;
  - frozen OriginalSpec build, REBORN Core build, OriginalBridge build, hardware-leak gate and complete REBORN self-test: `SUCCESS`.
- ORIGINAL SPEC remains frozen at PR #169 / `MATERIAL_GAP=0`; no OriginalSpec source or oracle semantics changed.
- Initial REBORN architecture from PR #171, grounded horizontal locomotion from PR #173 and ordinary standing vertical trajectory from PR #175 remain frozen.

## DONE

### Initial REBORN architecture — PR #171

Dependency direction remains:

```text
SaintSeiyaNesReborn.OriginalSpec
  frozen semantic oracle

SaintSeiyaNesReborn.Reborn.Core
  modern deterministic gameplay/domain
  NO OriginalSpec or graphics/audio-framework dependency

SaintSeiyaNesReborn.Reborn.OriginalBridge
  only ORIGINAL SPEC -> REBORN semantic translator
```

Raw CPU/RAM addresses, controller masks, banks and PPU/APU implementation details stay outside Core.

### Grounded horizontal locomotion/facing — PR #173

REBORN owns camera-independent `WorldX`, facing and deterministic movement cadence. The bridge proves parity with the canonical split `scroll + player_x` on both sides of the original camera handoff.

### Ordinary standing jump vertical trajectory — PR #175

REBORN owns the ordinary standing jump as a bounded 30-sample positive-up vertical trajectory. Same-tick takeoff consumes the first +8 sample immediately; peak rise is 58 px at tick 14 and the table ends 35 px above takeoff. Terminal fall, landing and collision remain outside this contract.

### Ordinary standing-jump airborne horizontal control — PR #177

REBORN now owns free-space steering during the ordinary standing jump without importing the original camera/storage split.

`Reborn.Core` owns:

- `RebornStandingJumpAirControlProfile`: logical-phase drift magnitudes;
- `RebornStandingJumpAirState`: semantic `WorldX`, preserved takeoff facing and logical phase;
- `RebornStandingJumpAirControl.Step`: deterministic neutral/left/right free-space drift;
- `RebornStandingJumpMotionState`: composed vertical + airborne-horizontal state;
- `RebornStandingJumpMotion.Initiate/Step`: explicit vertical-first then horizontal ordering.

Frozen standing-jump air-control semantics:

```text
even phase          0 px drift
odd phase           1 px drift
Right               +drift WorldX
Left                -drift WorldX
Neutral             0
facing              preserved from takeoff
phase               advances every active jump tick
```

A non-neutral takeoff is rejected by this ordinary-standing composition because it belongs to the directional-jump family and is intentionally deferred.

`OriginalSpecStandingJumpAirControlBridge` maps canonical input priority, frame parity, facing and `scroll + player_x` into the semantic contract. Simultaneous canonical Left+Right preserves Right-before-Left priority.

Parity fixtures run the complete 30-sample jump against `PlatformAirborneSession` with controlled neutral/left/right sequences. They verify tick-level horizontal delta, world X, preserved facing, logical parity, vertical-first ordering and deterministic replay. The fixture deliberately crosses the original player-X -> camera-scroll handoff and proves the semantic `WorldX` remains equivalent.

`docs/reborn/PLATFORM_STANDING_JUMP_AIR_CONTROL.md` owns this boundary and its exclusions.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations are REBORN design decisions and never rewrite ORIGINAL SPEC evidence.
3. Frozen fixtures/tables/state transitions remain oracle material.
4. Contradictory canonical evidence uses `ORACLE CHANGE` from `docs/VERIFY.md` before propagation.
5. ROM/audio/dialogue payloads remain private.

## OPEN

Grounded locomotion, ordinary standing vertical motion and ordinary standing-jump free-space steering are now independently semantic and deterministic.

The next smallest collision-free movement extension is the **high standing jump vertical family**. The frozen `PlatformJumpProfile` proves real per-Saint variation that is not present in the ordinary standing jump, while reusing the already-established trajectory abstraction.

The next slice should answer only:

1. semantic high-jump initiation from neutral horizontal takeoff plus Up intent;
2. high-jump vertical profiles projected into positive-up trajectory samples;
3. distinct canonical profile families across the five Saints;
4. same-tick first-sample consumption at takeoff;
5. complete table parity for each material profile family;
6. no landing/collision/terminal-fall composition yet.

Still exclude terrain collision, landing, terminal fall, directional jumps, forced directional trajectories, attacks, camera, animation and rendering.

## NEXT

**Implement the next REBORN movement slice: canonical high standing-jump initiation and complete table-controlled vertical profiles. Consume the frozen high-jump profiles through `Reborn.OriginalBridge`, reuse the positive-up semantic trajectory model in `Reborn.Core`, represent the material per-Saint profile variation explicitly, and prove complete tick-level parity for each distinct high-jump family without importing NES action bytes, selectors, table pointers, collision or presentation concepts into Core.**

Completion criterion:

> Given the same semantic grounded starting Y and valid neutral high-jump takeoff, REBORN must deterministically reproduce the complete frozen high-jump displacement sequence for every distinct canonical Saint profile family, including same-tick first-sample application and terminal table phase. The bridge/oracle mapping must be explicit and tested. Core remains independent of OriginalSpec and NES implementation details. Stop before landing/collision, terminal fixed fall, directional jumps/forced trajectories, attacks, camera, animation or rendering.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #171 architecture, PR #173 grounded locomotion, PR #175 standing vertical trajectory and PR #177 standing air-control contract after merge unless a failing fixture or explicit REBORN design decision requires a bounded change.
3. Read `docs/reborn/ARCHITECTURE.md`, `docs/reborn/PLATFORM_HORIZONTAL_LOCOMOTION.md`, `docs/reborn/PLATFORM_STANDING_JUMP.md` and `docs/reborn/PLATFORM_STANDING_JUMP_AIR_CONTROL.md` before expanding player movement.
4. Keep canonical-address/storage translation inside `Reborn.OriginalBridge`; gameplay/domain types remain semantic.
5. Do not use REBORN behavior as evidence for ORIGINAL SPEC.
6. Modern graphics, camera, animation and presentation remain explicit REBORN layers; behavioral parity does not preserve NES visual limitations.
7. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-high-standing-jump-vertical-profiles`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
