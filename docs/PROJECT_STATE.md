# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / platform movement reconstruction`
- State: `READY_FOR_NEXT`
- Last merged technical checkpoint: PR `#173` — first gameplay slice, grounded horizontal locomotion/facing.
- Merge commit: `a1f39f4ca079d977376e8efb177adec40c9e4c91`.
- Exact final PR head: `d90699dc950243bd2329398a188e1582f47d099d`.
- Exact locomotion code head before documentation-only state synchronization: `14498821d7dbe2e9473108a2bf61ce66c17aafbc`.
- Verification on the exact final PR head:
  - `REBORN architecture` #20: `SUCCESS`;
  - `ORIGINAL SPEC tests` #448: `SUCCESS`;
  - frozen OriginalSpec build, REBORN Core build, OriginalBridge build, hardware-leak gate and REBORN self-test: `SUCCESS`.
- The exact locomotion code head also passed `REBORN architecture` #17 and `ORIGINAL SPEC tests` #446 before documentation-only synchronization.
- ORIGINAL SPEC remains frozen at PR #169 / `MATERIAL_GAP=0`; no OriginalSpec source or oracle semantics changed.
- Initial REBORN architecture from PR #171 remains frozen.

## DONE

### Initial REBORN architecture — PR #171

REBORN is split into a modern deterministic domain and an anti-corruption bridge:

```text
SaintSeiyaNesReborn.OriginalSpec
  frozen semantic oracle

SaintSeiyaNesReborn.Reborn.Core
  modern deterministic gameplay/domain
  NO OriginalSpec or graphics/audio-framework dependency

SaintSeiyaNesReborn.Reborn.OriginalBridge
  only ORIGINAL SPEC -> REBORN semantic translator

future host/adapters
  input/render/audio/filesystem integration
```

`docs/reborn/ARCHITECTURE.md` owns this dependency contract. Raw NES addresses, controller masks, banks, PPU/APU state and other hardware-facing concepts do not belong in Core.

### Grounded horizontal locomotion/facing — PR #173

The first gameplay behavior now exists in REBORN as a pure semantic model.

`Reborn.Core` owns:

- `RebornHorizontalInput`: Neutral / Left / Right;
- `RebornFacing`: Left / Right;
- `RebornMotionPhase`: deterministic even/odd logical cadence;
- `RebornGroundedHorizontalProfile`: per-phase displacement;
- `RebornPlatformPlayerHorizontalState`: world X, facing, phase, locomotion flag;
- `RebornPlatformPlayerHorizontalLocomotion.Step`: deterministic free-space grounded state evolution.

The key modernization decision is that Core owns **world-space horizontal position**, not the NES split between screen-local player X and camera scroll. The frozen original proves `world_x = scroll_x + player_x`; parity fixtures compare REBORN against the canonical path both before and during the original camera handoff.

`OriginalSpecPlatformHorizontalBridge` maps canonical semantics into the modern contract and is the only layer that sees the original controller/state representations.

Canonical grounded cadence preserved by the bridge:

```text
ordinary / Seiya profile  even 1, odd 1
Shun distinct profile     even 1, odd 2
```

Bridge input projection also preserves the original Right priority when Left+Right are simultaneously present.

Parity fixtures cover:

1. Right before canonical camera handoff;
2. Right during canonical camera handoff;
3. Left with non-zero canonical scroll;
4. Neutral position/facing preservation;
5. Shun even/odd 1/2 cadence;
6. simultaneous Left+Right -> Right projection;
7. repeated equal state/input sequences -> identical semantic output.

`docs/reborn/PLATFORM_HORIZONTAL_LOCOMOTION.md` owns this slice and its exclusions.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations are REBORN design decisions and never rewrite ORIGINAL SPEC evidence.
3. Frozen fixtures/tables/state transitions remain oracle material.
4. Contradictory canonical evidence uses `ORACLE CHANGE` from `docs/VERIFY.md` before propagation.
5. ROM/audio/dialogue payloads remain private.

## OPEN

Horizontal free-space locomotion/facing is now sufficient to compose with a first vertical-motion primitive.

Do **not** collapse camera, collision/map descriptors, rendering or animation into the locomotion state. They remain separate systems so the remake can adopt modern camera behavior and completely redrawn pixel-art presentation without changing the semantic movement baseline.

The next bounded movement slice should reconstruct only the canonical standing jump trajectory:

1. semantic jump state and phase required by the deterministic curve;
2. initiation from grounded state;
3. canonical table-shaped vertical displacement per logical tick;
4. bridge projection from the frozen standing-jump oracle into modern semantic Y/delta state;
5. parity from takeoff through the end of the table-controlled trajectory;
6. no collision/landing resolution yet.

Directional/high jumps, horizontal air control and terrain interaction remain later checkpoints.

## NEXT

**Implement the next REBORN gameplay slice: canonical ordinary standing-jump initiation and table-controlled vertical trajectory. Consume the frozen standing-jump oracle through `Reborn.OriginalBridge`, model only semantic vertical position/phase in `Reborn.Core`, and prove deterministic parity for the complete canonical standing-jump curve without importing NES coordinates, addresses, jump-table pointers or hardware concepts into Core.**

Completion criterion:

> Given the same semantic grounded starting Y and standing-jump initiation, REBORN must deterministically reproduce the complete frozen ordinary standing-jump displacement sequence and terminal table phase. The bridge/oracle mapping must be explicit and tested. Core remains independent of OriginalSpec and NES implementation details. Stop before landing/collision resolution, directional/high jumps, airborne horizontal control, attacks, camera, animation or rendering.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #171 architecture and PR #173 horizontal-locomotion contract unless a failing fixture or explicit REBORN design decision requires a bounded change.
3. Read `docs/reborn/ARCHITECTURE.md` and `docs/reborn/PLATFORM_HORIZONTAL_LOCOMOTION.md` before expanding player movement.
4. Keep canonical-address/storage translation inside `Reborn.OriginalBridge`; gameplay/domain types remain semantic.
5. Do not use REBORN behavior as evidence for ORIGINAL SPEC.
6. Modern graphics, camera, animation and presentation remain explicit REBORN layers; preserving the oracle does not require preserving NES visual limitations.
7. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-standing-jump-vertical-trajectory`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
