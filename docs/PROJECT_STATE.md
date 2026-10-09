# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file only answers: what checkpoint is currently accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / persistent platform frame composition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#77` — persistent encounter/producer state bridged into the hybrid primary-entity frame and merged to `main`.
- Merge commit: `0493905a74da7ea12dd445ae4d64414f6d71e17e`
- Verification gate on exact PR head `9dfb81ad484e74191d9335c7f866a53b28ac071b`:
  - `ORIGINAL SPEC tests` run `#219`: `SUCCESS`
  - `Original Spec` run `#397`: `SUCCESS`
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.
- Last material result:
  - NMI encounter acceptance remains a distinct callable boundary;
  - accepted/staged encounter state persists into later main-thread work;
  - generic `$B6D0` and scheduled `$8927` producer outputs now reconcile directly into the richer hybrid slot representation;
  - producer write-map evidence establishes that both producers reset logical `+$04`, skip `+$08`, and do not own the separate attached `$A908/$AA70` hazard record;
  - the normal non-exit path composes producers -> player/resources -> hybrid slot A -> slot B -> late attack objects -> `$3C` advance;
  - `$A647` retirement output can feed later producer reuse with no manual visual-state reconstruction.

## DONE

The following boundaries are closed unless contradictory evidence appears.

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
- logical `+$00` clear obeys the original `$00 < $30` threshold;
- frame N post-removal state can feed frame N+1 directly;
- confirmed producer reuse of a freed slot is regression-tested.

### Persistent producer -> hybrid bridge

The normal active/non-exit path now owns one persistent state containing:

- encounter latch and staged `$03B7`;
- richer hybrid slots A/B;
- producer cooldown `$03B8` and scheduled-trigger latch `$03A2`;
- Seventh Sense, `$039A` and `$3C`.

Closed invariants:

- `StepNmi(...)` updates only the NMI-side encounter boundary and does not synthesize main-thread producer/entity work;
- main-thread order preserves `$B6D0 -> $8927 -> later player/entity phases` on the already-confirmed non-exit path;
- successful producer replacement resets `+$04` but preserves producer-unwritten `+$08` and the separately-owned attached hazard record;
- producer mutations persist even if later player processing exits before the entity pipeline;
- removal in one frame can feed producer reuse in a later frame without external slot translation.

Do **not** reopen these three boundaries merely to re-check them. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect crossing one of them.

## EVIDENCE

Primary repository artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformEntityRemovalA647.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentPrimaryEntityFrame.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/HybridEntityCombatSliceChecks.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EntityRemovalA647Checks.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PersistentPrimaryEntityFrameChecks.cs`
- `docs/reverse-engineering/HYBRID_ENTITY_SLOT_SCHEDULER.md`
- `docs/reverse-engineering/PRIMARY_ENTITY_REMOVAL_A647.md`
- `docs/reverse-engineering/PERSISTENT_PRIMARY_ENTITY_FRAME.md`
- merged PRs `#73`, `#74`, `#75`, `#76`, `#77`

Direct canonical-ROM evidence additionally established the `$B6D0` / `$8927` spawn write maps used by the bridge; no ROM payload or extracted binary asset is committed.

VERIFY note: the first PR #77 fixture run exposed a test-setup interaction with the shared `$03B8` cooldown when a `$86` encounter enabled slot B. The fixture was corrected to the intended one-slot `$06` case on the same branch. The exact final head then passed both verification workflows. No architectural rollback or repeated blind attempt was required.

## OPEN

1. The fixed-bank platform-exit gate is modeled independently but is not yet inserted into the persistent main-thread frame at its exact position between early `$B6D0` work and later `$8927` / player / entity work.
2. Accepted exit transitions need a persistent result shape that exposes normal `$3D` reload versus special `$70` transition without pretending the subsequent NES engine reload is part of the native logical runtime.
3. On an accepted exit, already-completed earlier producer mutations must persist while later scheduled producer/player/entity phases must not execute.
4. Scheduled entity families `$0D/$0E` can be produced by `$8927` but are not yet promoted into the hybrid active runtime.
5. Full renderer-owned animation/tile/Y/attribute/X state remains outside this logical/occupancy runtime except for evidence-backed lifecycle writes already promoted.

## NEXT

**Compose the exact platform-exit boundary into the persistent main-thread frame and replace the external `NonExit` precondition with an explicit normal/exit path split.**

Completion criterion:

> One persistent main-thread frame API executes all confirmed work up to the exit gate in original order, evaluates `PlatformExitGate` at the evidence-backed boundary, and either (a) returns an explicit `$3D`/`$70` exit result while preserving earlier state and suppressing all later phases, or (b) continues through the already-verified scheduled-producer/player/hybrid path with behavior equivalent to `StepMainThreadNonExit(...)`.

Required sequence:

1. inventory the exact call/order boundary surrounding `$B6D0`, bank-1 exit evaluation `$969D-$9713`, and scheduled producer `$8927`;
2. reuse `PlatformExitGate` rather than duplicating coordinate/substate logic;
3. split the current main-thread bridge so the early generic producer can run before the gate without also forcing `$8927` to run;
4. define an explicit frame outcome distinguishing `Continued`, `State3DReload`, and `State70Special`;
5. on accepted exit, preserve state mutations that occur before the gate and prove scheduled producer, player/entity processing and `$3C` advancement do not occur;
6. on non-exit, prove parity with the existing `StepMainThreadNonExit(...)` path;
7. add discriminating fixtures for:
   - a normal `$00-$0B` `$D0/$40` exit;
   - the `$10` Shun rejection;
   - the `$11` special `$70` transition;
   - early-producer mutation surviving an accepted exit;
   - non-exit equivalence;
8. document the engine-transition side-effect boundary rather than emulating stack/PPU/reload plumbing in the logical runtime;
9. run both verification workflows before checkpointing.

## BLOCKERS

- None at this checkpoint. `PlatformExitGate` and its ROM-backed coordinate/transition semantics are already modeled and documented.

## ANTI-LOOP

- `last_next_signature`: `persistent-frame-exit-gate-split`
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
