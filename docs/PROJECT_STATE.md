# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file only answers: what checkpoint is currently accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / platform runtime composition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#73` — hybrid common/special entity slot scheduler merged to `main`.
- Merge commit: `a71e2a7dccf3ae85513bf5e625248b8ff7a6518e`
- Verification gate:
  - `ORIGINAL SPEC tests` run `#210`: `SUCCESS`
  - `Original Spec` run `#385`: `SUCCESS`
- Last material result:
  - strict A -> B logical slot scheduling now composes common entities and special `$08/$09/$0C` entities;
  - attack state, `$76/$7F/$80`, Seventh Sense and global `$039A` propagate in original slot order;
  - visual-free inactive slots skip before route validation;
  - renderer-owned visual lifecycle remains intentionally outside the composed runtime.

## DONE

The following boundary is closed unless contradictory evidence appears:

`pre-player resources -> post-player latch -> slot A -> shared carry -> slot B -> late attack objects -> shared $3C increment`

Within that boundary:

- common admitted slots use `PlatformCommonEntitySlotRuntime`;
- `$08/$09/$0C` admitted slots use `PlatformSpecialEntityActive08090C`;
- `PlatformCommonSlotActivityGate` executes before semantic route validation;
- same-frame state carry between slot A and slot B is regression-tested.

Do **not** reopen this scheduler merely to re-check ordering. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect that crosses this boundary.

## EVIDENCE

Primary repository artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/HybridEntityCombatSliceChecks.cs`
- `docs/reverse-engineering/HYBRID_ENTITY_SLOT_SCHEDULER.md`
- merged PR `#73`

Acceptance criterion met: implementation builds and both repository verification workflows pass from the exact PR head before merge.

## OPEN

1. Exact visual-record lifecycle across entity removal, reaction/death cleanup, spawning and replacement is not yet composed into the hybrid multi-frame runtime.
2. Producer state (`PlatformPrimaryEncounterRefreshAndSpawn` / `PlatformPageEncounterSpawnState`) is not yet bridged into the hybrid logical scheduler as one persistent multi-frame state.
3. The bridge must preserve the NMI/main-thread boundary and must not infer renderer writes that have not been established.
4. Other entity families outside the promoted common set and `$08/$09/$0C` remain separate future work.

## NEXT

**Promote the minimum exact visual lifecycle needed for deterministic multi-frame entity-slot composition.**

Completion criterion:

> Starting from a persistent slot state, one composed frame can update logical entity state and the corresponding visual occupancy/free marker with evidence-backed rules, then feed that resulting state into the next frame without manually repairing `VisualSpritePlus1`.

Required sequence:

1. inventory every already-documented/read/write path that mutates the two entity visual `+1` markers or retires/replaces their visual records;
2. distinguish confirmed visual lifecycle writes from assumptions;
3. implement only the minimum closed lifecycle needed by the current common + `$08/$09/$0C` scheduler;
4. add discriminating multi-frame fixtures for removal/cleanup and subsequent slot reuse;
5. document scope gaps explicitly;
6. run both verification workflows before checkpointing.

Do not bridge producers into the scheduler until this criterion is met or until evidence proves that the producer bridge can remain correct without owning those visual writes.

## BLOCKERS

- None at this checkpoint.

## ANTI-LOOP

- `last_next_signature`: `visual-lifecycle-for-hybrid-multiframe`
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
