# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / platform movement reconstruction`
- State: `READY_FOR_NEXT`
- Last merged technical checkpoint: PR `#175` — ordinary standing-jump initiation and bounded table-controlled vertical trajectory.
- Merge commit: `ea2064edcafc38fc1a82d4b8d2283238d39466de`.
- Exact final PR head: `a80436d93436f6029c5d1b890663ae30d1a6ec78`.
- Exact standing-jump code head before documentation-only state synchronization: `731a34ddf0a94aa730375b69b56bb86eea714078`.
- Verification on the exact final PR head:
  - `REBORN architecture` #29: `SUCCESS`;
  - `ORIGINAL SPEC tests` #454: `SUCCESS`;
  - frozen OriginalSpec build, REBORN Core build, OriginalBridge build, hardware-leak gate and complete REBORN self-test: `SUCCESS`.
- The exact standing-jump code head also passed `REBORN architecture` #26 and `ORIGINAL SPEC tests` #452 before documentation-only synchronization.
- ORIGINAL SPEC remains frozen at PR #169 / `MATERIAL_GAP=0`; no OriginalSpec source or oracle semantics changed.
- Initial REBORN architecture from PR #171 and grounded horizontal locomotion from PR #173 remain frozen.

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

Frozen grounded profiles currently include ordinary 1/1 cadence and Shun's distinct 1/2 cadence. Collision, camera and rendering remain separate layers.

### Ordinary standing jump — PR #175

REBORN now owns a second bounded gameplay primitive: the canonical ordinary standing-jump table as a pure semantic vertical trajectory.

`Reborn.Core` owns:

- `RebornStandingJumpProfile`: ordered positive-up displacement samples;
- `RebornStandingJumpState`: vertical position, consumed trajectory ticks and active/completed state;
- `RebornStandingJump.Initiate`: takeoff plus first sample in the same logical tick;
- `RebornStandingJump.Step`: exactly one further table sample per logical tick.

The semantic coordinate convention is **positive-up world-space vertical position**. It is deliberately not NES screen Y.

Frozen ordinary standing-jump metrics:

```text
samples                 30
first sample             +8 px
peak rise               +58 px
first apex tick          14
net rise after table    +35 px
```

`OriginalSpecStandingJumpBridge` projects the frozen `PlatformJumpProfile` into this profile and converts canonical screen-Y deltas into semantic rise by sign inversion.

Parity fixtures execute all 30 samples in parallel with the frozen `PlatformJumpInitiation` + `PlatformAirborneVerticalMotion` path using collision-free probes. Every tick verifies per-sample rise, cumulative displacement, trajectory phase and deterministic replay. All five canonical Saint indices project the same ordinary standing-jump table.

The slice ends explicitly after sample 30. It does **not** silently include the original terminal fixed fall, landing or collision behavior.

`docs/reborn/PLATFORM_STANDING_JUMP.md` owns this boundary and its exclusions.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations are REBORN design decisions and never rewrite ORIGINAL SPEC evidence.
3. Frozen fixtures/tables/state transitions remain oracle material.
4. Contradictory canonical evidence uses `ORACLE CHANGE` from `docs/VERIFY.md` before propagation.
5. ROM/audio/dialogue payloads remain private.

## OPEN

Grounded horizontal movement and the ordinary standing vertical trajectory are now independently semantic and deterministic. The next bounded step is to compose the **horizontal air-control behavior of an ordinary vertical jump in open space** without yet importing collision or landing.

The frozen `PlatformAirborneHorizontalMotion` proves that a standing/vertical jump with directional input uses a parity-driven drift while preserving the original Right-before-Left priority. This is separable from directional-jump-at-takeoff trajectories and from terrain response.

The next slice should answer only:

1. semantic airborne horizontal intent for a standing jump;
2. deterministic 0/1 parity drift in collision-free space;
3. world-space X evolution independent of the original player-X/scroll split;
4. composition order with the already-frozen standing vertical trajectory;
5. parity across a complete ordinary standing-jump table using controlled input sequences;
6. bridge mapping of canonical frame parity/input without NES storage leaking into Core.

Still exclude floor/ceiling/side collision, landing correction, terminal fall, directional/high jump profiles, attacks, camera, animation and rendering.

## NEXT

**Implement the next REBORN movement slice: ordinary standing-jump airborne horizontal control in collision-free space. Consume the frozen vertical-jump drift behavior through `Reborn.OriginalBridge`, compose it with the existing semantic standing-jump trajectory and world-space horizontal state, and prove tick-level parity for neutral/left/right input sequences while keeping camera, collision descriptors, landing and NES storage concepts outside `Reborn.Core`.**

Completion criterion:

> Given the same semantic takeoff state, logical-frame phase and neutral/left/right input sequence during an ordinary standing jump, REBORN must reproduce the frozen canonical open-space horizontal drift and facing/world-position semantics across the bounded standing-jump table. Composition with vertical trajectory must remain deterministic and explicitly ordered. Core stays independent of OriginalSpec and NES screen/camera/storage details. Stop before terrain collision, landing/terminal fall, directional/high jumps, attacks, camera, animation or rendering.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #171 architecture, PR #173 grounded horizontal locomotion and PR #175 standing-jump trajectory unless a failing fixture or explicit REBORN design decision requires a bounded change.
3. Read `docs/reborn/ARCHITECTURE.md`, `docs/reborn/PLATFORM_HORIZONTAL_LOCOMOTION.md` and `docs/reborn/PLATFORM_STANDING_JUMP.md` before expanding player movement.
4. Keep canonical-address/storage translation inside `Reborn.OriginalBridge`; gameplay/domain types remain semantic.
5. Do not use REBORN behavior as evidence for ORIGINAL SPEC.
6. Modern graphics, camera, animation and presentation remain explicit REBORN layers; behavioral parity does not preserve NES visual limitations.
7. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-standing-jump-airborne-horizontal-control`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
