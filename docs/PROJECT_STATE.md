# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file only answers: what checkpoint is currently accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform runtime composition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#75` — exact primary `$A647` retirement composed into the hybrid entity scheduler and merged to `main`.
- Merge commit: `fbc1539a10ceec850be36aac615d7b0e7be1085c`
- Verification gate on exact PR head `eb2a89fb06e2d2e0795cfd7772ac65f7c9708887`:
  - `ORIGINAL SPEC tests` run `#214`: `SUCCESS`
  - `Original Spec` run `#391`: `SUCCESS`
- Workflow hardening checkpoint: PR `#74` is also merged; this file and `docs/WORK_PROTOCOL.md` define the authoritative continuation/anti-loop protocol.
- Last material result:
  - canonical-ROM inspection established exact bank-3 `$A647` primary removal semantics;
  - primary visual occupancy now transitions to `$FE` on confirmed removal paths;
  - logical action `+$00` clears only while engine state `$00 < $30`;
  - `$00 >= $30` can therefore leave logical `$40/$D0` cleanup behind a visual-free slot, matching the `$A459` gate exceptions;
  - hybrid common and `$08/$09/$0C` routes now persist this retirement state across frames;
  - a retired slot can pass directly into a confirmed producer and be reused `$FE -> $FD` without manual state repair.

## DONE

The following boundaries are closed unless contradictory evidence appears:

### Hybrid A -> B scheduler

`pre-player resources -> post-player latch -> slot A -> shared carry -> slot B -> late attack objects -> shared $3C increment`

Within that boundary:

- common admitted slots use `PlatformCommonEntitySlotRuntime`;
- `$08/$09/$0C` admitted slots use `PlatformSpecialEntityActive08090C`;
- `PlatformCommonSlotActivityGate` executes before semantic route validation;
- attack state, `$76/$7F/$80`, Seventh Sense and `$039A` carry A -> B in-order.

### Primary removal / occupancy lifecycle

For currently promoted common and `$08/$09/$0C` removal paths:

- `$A647` retirement is evidence-backed from the canonical ROM;
- tracked primary visual occupancy becomes `$FE`;
- eleven base visual records are retired by the original helper; type `$0D` has the documented extra `+$2C/+2D` retirement;
- logical `+$00` clear obeys the original `$00 < $30` threshold;
- frame N post-removal state can feed frame N+1 directly;
- confirmed producer reuse of a freed slot is regression-tested.

Do **not** reopen scheduler order or `$A647` semantics merely to re-check them. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect that crosses these boundaries.

## EVIDENCE

Primary repository artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformEntityRemovalA647.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/HybridEntityCombatSliceChecks.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EntityRemovalA647Checks.cs`
- `docs/reverse-engineering/HYBRID_ENTITY_SLOT_SCHEDULER.md`
- `docs/reverse-engineering/PRIMARY_ENTITY_REMOVAL_A647.md`
- merged PRs `#73`, `#74`, `#75`

Direct ROM evidence used for `$A647` came from the canonical Japanese ROM stored privately in the project Drive; no ROM payload or extracted binary asset is committed to GitHub.

Acceptance criterion met: the implementation builds and both repository verification workflows passed from the exact PR #75 head before merge.

## OPEN

1. Producer state (`PlatformPrimaryEncounterRefreshAndSpawnState` / `PlatformPageEncounterSpawnState`) is not yet unified with `PlatformHybridEntitySlotState` as one persistent multi-frame machine state.
2. The bridge must preserve the confirmed NMI-side encounter refresh/acceptance boundary versus later main-thread producer execution.
3. The confirmed main-thread producer order is generic `$B6D0` first, scheduled `$8927` second, before platform damage/player/entity update; composition must preserve that order rather than merely sharing data structures.
4. Special-only persistent fields (`+$04` control, attached hazard, parent `+$08`, shared `$039A`) need explicit initialization/preservation rules when producer output becomes a hybrid slot.
5. Other entity families outside the promoted common set and `$08/$09/$0C` remain separate future work.
6. Full renderer-owned animation/tile/Y/attribute/X state remains outside this logical/occupancy runtime except for evidence-backed lifecycle writes already promoted.

## NEXT

**Compose a persistent producer -> hybrid scheduler frame bridge without collapsing the NMI/main-thread timing boundary.**

Completion criterion:

> A persistent platform state can accept an encounter at the NMI boundary, run the confirmed main-thread producer order into the two primary slots, execute the hybrid A -> B entity scheduler, and carry the resulting logical/visual slot state into a following frame without manual translation or reconstruction.

Required sequence:

1. inventory the exact state-field correspondence between `PlatformPageEncounterSpawnState` and `PlatformHybridEntitySlotState`;
2. establish evidence-backed initialization rules for special-only slot fields when a producer creates/replaces an entity;
3. keep `StepNmi(...)` and main-thread execution as distinct callable boundaries;
4. compose main-thread order as `producer B6D0 -> producer 8927 -> existing pre-player/player/hybrid entity frame slice` only where already confirmed;
5. add multi-frame fixtures covering at least:
   - free slot -> producer spawn -> same-frame/next-confirmed entity processing according to original order;
   - entity removal -> later producer reuse with no manual state repair;
   - NMI descriptor acceptance followed by deferred main-thread consumption;
   - special-only state reset/preservation across replacement;
6. document any execution-order gap rather than inventing it;
7. run both verification workflows before checkpointing.

## BLOCKERS

- None at this checkpoint.

## ANTI-LOOP

- `last_next_signature`: `producer-hybrid-persistent-frame-bridge`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only if it produces at least one of: code, test, evidence map, discarded hypothesis, or explicit architectural decision backed by evidence.
- If two consecutive cycles finish with the same blocker, same `NEXT`, and no new evidence, the third identical attempt is forbidden.
- On anti-loop trigger: change technique, move to a different evidence source, or roll back to the last verified checkpoint.
- Closed phases are not re-entered merely because uncertainty remains elsewhere.

## CONTINUE SEMANTICS

When the user says `continúa` with no narrower instruction:

1. read this file first;
2. verify that `Last verified checkpoint` still matches current `main` or reconcile newer merged work;
3. execute `NEXT` in FAST mode until a material result or real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.

If repository history is newer than this file, repository history wins temporarily: reconstruct the real checkpoint, update this file, and only then continue.
