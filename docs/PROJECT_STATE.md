# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file answers only: what checkpoint is accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform frame + entity-family promotion`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#79` — exact platform-exit gate composed into the persistent main-thread frame and merged to `main`.
- Merge commit: `d3b291f7fc2df330c073fa1209afd8b0b592c8f5`
- Verification gate on exact final PR head `f8891c7912ce671452e721e8c785311ecbf30a58`:
  - `ORIGINAL SPEC tests` run `#224`: `SUCCESS`
  - `Original Spec` run `#404`: `SUCCESS`
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.
- Last material result:
  - direct canonical-ROM inspection fixed the main-thread order as `$B6D0 -> $969D exit gate -> continuing-path $8000/$8927`;
  - `PlatformLatchedCommonProducerPhase` now exposes that boundary as `StepCommon(...)` / `StepScheduled(...)` while retaining the old non-exit composition;
  - `PlatformPersistentPrimaryEntityFrame.StepMainThread(...)` now returns explicit `Continued`, `State3DReload`, or `State70Special` outcomes;
  - accepted exits preserve already-completed `$B6D0` mutations but suppress `$8927`, later player/entity processing and `$3C` advancement;
  - the `$10` Shun rejection and `$11` special `$70` transition are regression-tested through the persistent frame;
  - the continuing full path is semantically parity-checked against `StepMainThreadNonExit(...)`.

## DONE

The following boundaries are closed unless contradictory evidence appears.

### Hybrid A -> B scheduler

- activity gate precedes semantic route selection;
- common admitted slots use `PlatformCommonEntitySlotRuntime`;
- `$08/$09/$0C` admitted slots use `PlatformSpecialEntityActive08090C`;
- attack state, `$76/$7F/$80`, Seventh Sense and `$039A` carry A -> B in-order.

### Primary removal / occupancy lifecycle

- confirmed removal paths compose exact `$A647` primary retirement;
- tracked visual occupancy becomes `$FE`;
- logical `+$00` clear obeys engine `$00 < $30`;
- frame N retirement can feed frame N+1 gating and later producer reuse directly.

### Persistent producer -> hybrid state bridge

- NMI acceptance remains separate from main-thread producers;
- encounter latch / `$03B7`, richer slots, `$03B8`, `$03A2`, Seventh Sense, `$039A` and `$3C` persist together;
- both promoted producers reset logical `+$04`, skip `+$08`, and do not own the attached `$A908/$AA70` hazard record;
- producer output feeds the hybrid runtime without manual slot reconstruction.

### Main-thread exit split

Direct ROM order is now closed as:

```text
$C30A JSR $B6D0
$C30F bank-1 select
$C312 JSR $969D
if continuing: $C319 JSR $8000
bank-1 $800C JSR $8927
```

Closed invariants:

- `$B6D0` executes before the exit gate;
- accepted `$3D/$70` exits preserve pre-gate mutations;
- accepted exits do not execute `$8927`, player/entities or shared `$3C` advance;
- rejected exits continue through the existing verified path;
- `PlatformExitGate` remains the single source for coordinates, jump-phase, Shun and transition-kind predicates.

Do **not** reopen these boundaries merely to re-check them. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect crossing one of them.

## EVIDENCE

Primary artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentPrimaryEntityFrame.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformLatchedCommonProducerPhase.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformExitGate.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformEntityRemovalA647.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PersistentPrimaryEntityFrameChecks.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PersistentPrimaryEntityExitGateChecks.cs`
- `docs/reverse-engineering/PERSISTENT_PRIMARY_ENTITY_FRAME.md`
- `docs/reverse-engineering/PLATFORM_EXIT_GATES.md`
- merged PRs `#73` through `#79` relevant to this composition chain.

VERIFY note for PR #79: the first .NET run reached the new parity fixture and failed only because it compared the entire `Hybrid` record object, whose nested structure does not define the intended semantic equality. Persistent state and producer equality had already passed. The fixture was changed to compare observable hybrid invariants; the runtime was not changed. The exact final head then passed both workflows.

## OPEN

1. `$8927` can produce scheduled types `$0D/$0E`, but `PlatformHybridEntityCombatSlice` currently promotes only `$08/$09/$0C` as scheduled-special active routes; `$0D/$0E` therefore remain an immediate runtime integration gap.
2. Direct ROM dispatch shows `$0D/$0E` are **not** variants of the `$08/$09/$0C` predispatch: type checks at `$A489-$A48F` branch to `$A4BF -> $A55E`, bypassing `$A4A7-$A55B` (`+$04` / `$039A` predispatch logic).
3. The action-family and interaction semantics reached from `$A55E` for `$0D/$0E` are not yet promoted as a closed runtime.
4. Type `$0F` has its own jump to `$A74C` and remains separate future work.
5. Full renderer-owned animation/tile/Y/attribute/X state remains outside this logical/occupancy runtime except for evidence-backed lifecycle writes already promoted.
6. Higher-level native destination/state-machine behavior after semantic `$3D/$70` exit remains above this entity-frame layer.

## NEXT

**Promote the scheduled `$0D/$0E` active runtime from their real `$A55E` entry path and integrate it into the hybrid scheduler without reusing the `$08/$09/$0C` predispatch.**

Completion criterion:

> A type `$0D` or `$0E` produced by `$8927` can enter the persistent/hybrid frame, execute its evidence-backed active action/interaction/removal path, carry shared combat/contact state correctly, and persist into the next frame without falling into the unsupported common dispatcher or the wrong `$08/$09/$0C` predispatch.

Required sequence:

1. map the exact control flow for `$0D/$0E` from `$A48B/$A48F -> $A4BF -> $A55E` through action-family dispatch and the later interaction/removal boundary;
2. identify which existing promoted common family primitives are genuinely shared and which type-specific branches require new code;
3. prove that `$0D/$0E` bypass `$A4A7-$A55B` predispatch and therefore do not inherit `$08/$09/$0C` `+$04/$039A` behavior by assumption;
4. create a dedicated semantic route/runtime for `$0D/$0E` rather than widening `PlatformSpecialEntityActive08090C`;
5. integrate that route into `PlatformHybridEntityCombatSlice` behind the existing activity gate;
6. add fixtures beginning from actual `$8927` spawn output for both `$0D` and `$0E`, including at least one interaction path and one removal/next-frame path;
7. document unresolved type-specific renderer or auxiliary-object effects instead of inventing them;
8. run both verification workflows before checkpointing.

## BLOCKERS

- None for the first reverse-engineering pass. The canonical ROM is available privately and the dispatcher entry split for `$0D/$0E` is already identified.

## ANTI-LOOP

- `last_next_signature`: `scheduled-0d0e-active-route`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only if it produces code, a test/fixture, new evidence, an evidence map/table, a discarded hypothesis, or an explicit evidence-backed architectural decision.
- If two consecutive cycles finish with the same blocker, same `NEXT`, and no new evidence, a third identical attempt is forbidden.
- On anti-loop trigger: change technique/source or roll back to the last verified checkpoint.
- Closed phases are not re-entered merely because uncertainty remains elsewhere.

## CONTINUE SEMANTICS

When the user says `continúa` with no narrower instruction:

1. read this file first;
2. verify that the latest checkpoint still matches current `main` or reconcile newer merged work;
3. execute `NEXT` in FAST mode until a material result or real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.

If repository history is newer than this file, repository history wins temporarily: reconstruct the real checkpoint, update this file, and only then continue.
