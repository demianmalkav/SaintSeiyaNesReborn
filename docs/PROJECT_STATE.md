# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `REBORN / first gameplay vertical slice`
- State: `READY_FOR_NEXT`
- Closing checkpoint in PR `#171` — initial REBORN architecture boundary and minimal text vertical slice.
- Exact architecture code head verified before documentation-only state synchronization: `6104ae5815b1612834e4008418024b385a77c8f7`.
- Verification on that exact head:
  - `REBORN architecture` #2: `SUCCESS`.
  - `ORIGINAL SPEC tests` #438: `SUCCESS`.
  - independent OriginalSpec build, REBORN Core build, OriginalBridge build, hardware-leak gate and REBORN architecture self-test: `SUCCESS`.
- ORIGINAL SPEC remains frozen at PR #169 / `MATERIAL_GAP=0` and was not modified semantically by this checkpoint.

## DONE

### Initial REBORN architecture — PR #171

REBORN now has an explicit clean-room implementation boundary instead of sharing implementation concepts with the NES reconstruction.

#### Project/module boundary

```text
SaintSeiyaNesReborn.OriginalSpec
  frozen semantic oracle

SaintSeiyaNesReborn.Reborn.Core
  deterministic modern domain/runtime
  NO dependency on OriginalSpec or a graphics/audio framework

SaintSeiyaNesReborn.Reborn.OriginalBridge
  anti-corruption layer
  depends on OriginalSpec + Reborn.Core
  owns canonical-NES -> REBORN semantic translation

future host/adapters
  platform/render/audio/input/filesystem integration
  consume Reborn.Core semantic output

SaintSeiyaNesReborn.Reborn.SelfTest
  parity/architecture boundary fixtures
```

Dependency direction is frozen: ORIGINAL SPEC never depends on REBORN; Core never depends on OriginalSpec; only bridge/test infrastructure may see both.

#### Deterministic runtime

`RebornRuntime.Step` is the initial domain-time primitive:

- one call = one logical tick;
- equal state + equal input = equal semantic output;
- wall-clock scheduling belongs outside Core;
- rendering/audio consume output rather than mutating gameplay state;
- future randomness must be deterministic/injected and covered by parity fixtures.

#### Content/localization boundary

The first vertical slice uses the already-closed text contract because it is stable, bounded and can be tested without copyrighted payloads.

`OriginalSpecLocalizationBridge` maps canonical requests into REBORN-owned semantics:

```text
canonical message id -> RebornMessageId / MSG_xxx
$066A/$066B          -> opaque Lane0/Lane1 inside bridge only
$0672 $00/$FF        -> opaque Variant0/Variant1 inside bridge only
JP/ES catalog        -> IRebornLocalizationPort
```

REBORN Core contains no `$066A`, `$066B`, `$0672`, `$E7Bx`, PRG/CHR bank, PPU/APU or other NES-facing contract.

#### Presentation/audio and persistence boundaries

- `IRebornPresentationAdapter` and `IRebornAudioAdapter` define platform-independent output ports; no backend has been selected yet.
- `RebornSaveSnapshot` is REBORN-owned and schema-versioned. It does not serialize NES RAM/emulator state.
- schema v1 currently carries deterministic tick only because gameplay progression has not yet been migrated.
- canonical password behavior remains an ORIGINAL SPEC compatibility oracle, not the native REBORN save format.

#### Architecture fixtures

The public synthetic fixture proves:

- all four canonical text entrypoint combinations preserve message identity and map to the expected two opaque lanes/two opaque variants;
- JP/ES selection and explicit Japanese fallback survive the bridge;
- lane/variant metadata is stable through runtime resolution;
- two runtimes with identical state/input emit identical semantic frame output;
- Core has no assembly dependency on OriginalSpec;
- CI rejects raw NES address/entrypoint leakage into Core;
- save capture/restore preserves deterministic tick and rejects unknown schema versions.

`docs/reborn/ARCHITECTURE.md` is the architecture contract for this boundary.

Do not broaden or collapse these layers without an explicit REBORN architecture decision and regression update.

## ORIGINAL SPEC FREEZE CONTRACT

1. ORIGINAL SPEC remains authoritative for proven 1988 semantics.
2. REBORN may deliberately deviate, but deviations are REBORN design decisions and never rewrite ORIGINAL SPEC evidence.
3. Frozen fixtures/tables/state transitions remain oracle material.
4. New contradictory canonical evidence uses the `ORACLE CHANGE` process in `docs/VERIFY.md` before propagation.
5. ROM/audio/dialogue payloads remain private.

## OPEN

The architecture boundary is sufficient to begin one bounded gameplay migration. No renderer, battle system, map stack, hazard layer or narrative system should be added in the same checkpoint.

The first gameplay slice should use already-closed platform player control/motion evidence and answer only:

1. which semantic player state is minimally required for horizontal locomotion and facing;
2. how neutral/left/right input maps to the frozen canonical horizontal-motion behavior;
3. how deterministic logical ticks advance that state;
4. which ORIGINAL SPEC fixtures/formulas are the oracle for parity;
5. how the bridge exposes those semantics without CPU/RAM addresses in Core;
6. how output is represented as semantic player/frame state before collision or rendering.

Explicitly exclude collision resolution, hazards, maps/exits, attacks, jumping/vertical motion, animation assets and rendering unless a tiny dependency is proven necessary for the horizontal slice.

## NEXT

**Implement the first REBORN gameplay vertical slice: canonical platform-player horizontal locomotion and facing. Reconcile the frozen player-control/horizontal-motion evidence and fixtures, expose only the minimum semantic contract through the OriginalBridge, implement deterministic neutral/left/right state evolution in Reborn.Core, and add parity tests proving the selected original behavior without leaking NES addresses or hardware concepts into Core.**

Completion criterion:

> Given the same semantic initial player state and neutral/left/right input sequence, REBORN must deterministically produce the horizontal motion/facing state required by the frozen ORIGINAL SPEC fixtures. The oracle mapping must be explicit and tested. Reborn.Core must remain free of NES addresses/registers/bank concepts. OriginalSpec must remain semantically unchanged. The checkpoint stops before collision, hazards, maps, attacks, vertical motion, animation/rendering or broad gameplay migration.

## BLOCKERS

- None known.

## RECOVERY CONTRACT

1. Refresh `main`, then read this file before executing `NEXT`.
2. Freeze PR #171 architecture after merge unless a failing architecture fixture or explicit design decision requires a bounded change.
3. Read `docs/reborn/ARCHITECTURE.md` before adding a new REBORN dependency or project.
4. Keep canonical-address translation inside `Reborn.OriginalBridge`; gameplay/domain types stay semantic.
5. Do not use REBORN behavior as evidence for ORIGINAL SPEC.
6. Drive remains private evidence/content storage only and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `reborn-platform-horizontal-locomotion-facing`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
