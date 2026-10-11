# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / platform movement reconstruction`
- State: `READY_FOR_NEXT`
- Last merged technical checkpoint: PR `#179` — canonical high standing-jump initiation and complete table-controlled vertical profiles.
- Merge commit: `d9ed0be3cdc95cbdac766ee488325666fc7e10a3`.
- Exact final PR head: `adff7cc1b107011b478c4afd0f035dd0e0f3d1b4`.
- Exact high-standing-jump code head before documentation-only state synchronization: `71452b23ec179ce975b6340c58913cf753b07771`.
- Verification on the exact final PR head:
  - `REBORN architecture` #47: `SUCCESS`;
  - `ORIGINAL SPEC tests` #466: `SUCCESS`;
  - frozen OriginalSpec build, REBORN Core build, OriginalBridge build, hardware-leak gate and complete REBORN self-test: `SUCCESS`.
- The exact code head also passed `REBORN architecture` #44 and `ORIGINAL SPEC tests` #464 before documentation-only synchronization.
- ORIGINAL SPEC remains frozen at PR #169 / `MATERIAL_GAP=0`; no OriginalSpec source or oracle semantics changed.
- PR #171 architecture, PR #173 grounded locomotion, PR #175 ordinary standing vertical and PR #177 standing air control remain frozen.

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

REBORN owns camera-independent `WorldX`, facing and deterministic two-phase grounded cadence. The bridge proves parity across the original player-X/camera-scroll handoff.

### Ordinary standing jump vertical trajectory — PR #175

REBORN owns the ordinary standing jump as a bounded 30-sample positive-up vertical trajectory. Same-tick takeoff consumes the first sample; peak rise is 58 px at tick 14 and the table ends 35 px above takeoff. Terminal fall, landing and collision remain separate.

### Ordinary standing-jump airborne horizontal control — PR #177

REBORN owns collision-free standing-jump steering as parity drift: even 0 px, odd 1 px, Right/Left apply signed world-space drift, neutral applies none, and takeoff facing is preserved. Composition order is vertical -> horizontal -> phase advance.

### High standing jump — PR #179

REBORN now owns canonical high-standing-jump takeoff semantics and all complete high-jump vertical profile families without importing original selectors/action bytes/table pointers.

`Reborn.Core` owns:

- `RebornHighStandingJumpTakeoffIntent`: semantic jump request + upward intent + horizontal takeoff intent;
- `RebornHighStandingJumpProfileFamily`: `Seiya`, `ShunIkki`, `HyogaShiryu`;
- `RebornHighStandingJumpProfile`: explicit family identity plus the frozen positive-up trajectory primitive;
- `RebornHighStandingJump.Initiate/Step`: same-tick first-sample application and bounded deterministic sample consumption.

Valid takeoff for this slice is jump + upward intent with neutral horizontal takeoff. Horizontal takeoff is rejected because it belongs to the directional-jump family.

Frozen high-jump families:

```text
family          ticks   first   peak    apex   net after table
Seiya             58      +9    +103      28       +51
Shun / Ikki       48      +9     +88      23       +51
Hyoga / Shiryu    38      +9     +71      18       +41
```

`OriginalSpecHighStandingJumpBridge` projects Saint identity, complete high profile and canonical takeoff input into semantic REBORN contracts.

Parity fixtures run every Saint through the complete table against `PlatformJumpInitiation` + `PlatformAirborneVerticalMotion` with collision-free probes. They verify per-tick rise, cumulative position, same-tick sample 1, trajectory phase, complete family sharing, deterministic replay and the final pre-terminal phase. The slice refuses implicit terminal fixed fall.

`docs/reborn/PLATFORM_HIGH_STANDING_JUMP.md` owns this boundary and its exclusions.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations are REBORN design decisions and never rewrite ORIGINAL SPEC evidence.
3. Frozen fixtures/tables/state transitions remain oracle material.
4. Contradictory canonical evidence uses `ORACLE CHANGE` from `docs/VERIFY.md` before propagation.
5. ROM/audio/dialogue payloads remain private.

## OPEN

Grounded locomotion, ordinary standing vertical motion, ordinary free-space air control and high-standing vertical families are now semantic and deterministic.

The next smallest movement extension is the **directional-jump takeoff plus its complete vertical profile families**, still isolated from the forced horizontal trajectory/collision layer.

The frozen oracle already proves:

1. horizontal takeoff direction selects the directional jump family before Up/high selection;
2. Right, Left and the both-held quirk have distinct takeoff identities but share the same Saint-specific vertical family selection;
3. directional vertical curves contain material per-Saint family differences;
4. same-frame initiation consumes the first vertical sample immediately;
5. forced airborne horizontal trajectory can remain a later composition layer.

Still exclude forced horizontal trajectory/counter-steer, terrain collision, landing, terminal fall, attacks, camera, animation and rendering.

## NEXT

**Implement the next REBORN movement slice: canonical directional-jump takeoff identity and complete table-controlled vertical profiles. Consume the frozen directional-jump profiles through `Reborn.OriginalBridge`, represent semantic Right/Left/both-held takeoff without NES action bytes, reuse the positive-up trajectory primitive in `Reborn.Core`, preserve the material per-Saint profile families, and prove complete tick-level vertical parity for every distinct family. Stop before forced horizontal trajectory/counter-steer, collision, landing, terminal fixed fall, attacks, camera, animation or rendering.**

Completion criterion:

> Given the same semantic grounded starting Y and directional takeoff intent, REBORN must select the correct directional takeoff identity and deterministically reproduce the complete frozen directional vertical displacement sequence for every distinct Saint profile family, including same-tick first-sample application and terminal table phase. The bridge/oracle mapping must be explicit and tested. Core remains independent of OriginalSpec and NES implementation details. Horizontal forced-trajectory behavior remains unimplemented in this slice.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #171, #173, #175, #177 and #179 after merge unless a failing fixture or explicit bounded REBORN design decision requires change.
3. Read `docs/reborn/ARCHITECTURE.md`, `PLATFORM_HORIZONTAL_LOCOMOTION.md`, `PLATFORM_STANDING_JUMP.md`, `PLATFORM_STANDING_JUMP_AIR_CONTROL.md` and `PLATFORM_HIGH_STANDING_JUMP.md` before expanding movement.
4. Keep canonical-address/storage/input-bit translation inside `Reborn.OriginalBridge`; gameplay/domain types remain semantic.
5. Do not use REBORN behavior as evidence for ORIGINAL SPEC.
6. Modern graphics, camera, animation and presentation remain explicit REBORN layers; behavioral parity does not preserve NES visual limitations.
7. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-directional-jump-takeoff-vertical-profiles`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
