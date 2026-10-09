# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for their own evidence and semantics. This file answers only: what checkpoint is accepted, what remains open, and what exact action must run next.

## CURRENT

- Phase: `ORIGINAL SPEC / primary action-space closure`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#90` — complete persistent normal platform late-object frame merged to `main`.
- Merge commit: `a1d5ac06b3507be62b0bca0ea1b2207440d0f9ac`
- Exact final PR head: `328861152e930e4c35069eca896f2fc30631a213`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#249`: `SUCCESS`
  - `Original Spec` run `#438`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Structural prerequisite: PR `#88` extracted the already-closed primary A -> B body as a reusable after-player primitive without changing semantics.
- Superseded attempt: PR `#89` was closed without merge after CI exposed contract/test compilation issues; those issues were corrected on the same implementation line before the exact #90 head passed all verification.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

Closed unless contradictory evidence appears:

### Complete persistent normal late-object frame

The continuing normal platform main-thread path is now composed as one persistent state boundary:

```text
$B6D0 generic primary producer
 -> platform exit gate
 -> $8927 scheduled producer
 -> pre-player resources / player / post-player latch
 -> $9B93 multisprite
 -> $96B4 auxiliary spawn
 -> $9761 auxiliary A
 -> $9761 auxiliary B
 -> $A442 primary A
 -> $A442 primary B
 -> $A22C attack-object update
 -> one $3C increment
```

One physical mutable stream is preserved across late object classes for:

- player attack objects;
- `$76` contact latch;
- `$7F/$80` Life/Cosmo drain state;
- Seventh Sense.

Persistent state now carries the promoted primary entity pair, `$9B93` state and auxiliary-hazard state directly into the next frame.

Fixtures prove:

- a projectile consumed by `$9B93` is invisible to auxiliary and primary classes later that frame;
- a projectile consumed by auxiliary A is invisible to primary A/B;
- auxiliary contact seeds `$76/$7F/$80` and suppresses later primary overwrite;
- freshly spawned auxiliary hazards update in the same frame;
- `$A22C` executes exactly once after all promoted late object classes;
- normal `$3C` advances exactly once;
- exceptional player-loop exits preserve completed producer mutations while skipping `$9B93`, auxiliaries, primary A/B, `$A22C` and the normal `$3C` advance;
- accepted platform exits retain the already-confirmed earlier producer/exit split.

### Primary-family runtime coverage already promoted

- common types `$00-$07`;
- scheduled/direct special types `$08/$09/$0C`;
- `$0D/$0E`;
- direct `$0F`;
- `$0A/$0B` executable behavior on `$10/$50`, including their special projectile response and `+$03` post-hit impulse.

The remaining `$0A/$0B` question is not an unimplemented observed route; it is whether `$10/$50` form the complete **reachable** action-family set for normally produced instances.

Do **not** reopen the persistent late-object order merely to re-check sequencing. Reopen only for a failing fixture, contradictory ROM evidence, or a newly promoted side effect crossing its boundary.

## EVIDENCE

Primary artifacts for the latest checkpoint:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentLateObjectFrame.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformHybridEntityCombatSlice.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PersistentLateObjectFrameChecks.cs`
- `docs/reverse-engineering/PERSISTENT_LATE_OBJECT_FRAME.md`
- merged PR `#90`

Relevant `$0A/$0B` artifacts already promoted:

- `docs/reverse-engineering/ENTITY_TYPES_0A_0B.md`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEdgeSpawner.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEntityPreparation.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEntityFall50.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEntityLanding.cs`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEntityActiveDispatcher.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EntityTypes0A0BChecks.cs`

Current confirmed transition evidence already establishes:

```text
$B6D0 spawn -> $10
$10 ordinary -> remain $10 OR start $50 OR remove
$50 -> remain $50 OR land to $10 OR remove
projectile response -> action unchanged, +$03 impulse only
post-hit $A845 -> +$03/X mutation only
```

The next task must audit the remaining entity-action writers before promoting that chain from strong static evidence to explicit reachable-state closure.

## OPEN

1. `$0A/$0B` are defensively admitted by the active dispatcher only on `$10/$50`; the repository still lacks one explicit writer-audit statement proving no normal producer/interaction path can generate `$30/$40/$70/$D0/$E0` for those types.
2. Full renderer-owned animation/tile/Y/attribute/X state remains outside logical runtimes except where exact lifecycle writes have been promoted.
3. Higher-level native destination/state-machine behavior after semantic `$3D/$70` platform exits remains above the persistent entity-frame layer.
4. NES-specific snapshot/PPU/stack/reload plumbing remains intentionally outside the clean logical frame boundary.

## NEXT

**Close reachable action-family state for primary types `$0A/$0B`.**

Completion criterion:

> Starting from every confirmed normal producer of `$0A/$0B`, every promoted action-state writer reachable during their lifecycle is accounted for, and the reachable action-family set is either proven to be exactly `{ $10, $50 }` or expanded only where direct evidence requires it.

Required sequence:

1. audit the real producer(s) of `$0A/$0B` and record their initial action family;
2. audit all action-state writers reached from `$10/$50`, including decision/proximity, landing, projectile interaction, player contact, post-hit `+$03`, removal and any type-aware shared helpers;
3. explicitly distinguish action-byte writes from `+$03` motion/impulse writes;
4. check whether the scheduled producer can ever create `$0A/$0B` or whether `$B6D0` is their only normal source;
5. add only missing discriminating fixtures, preferably producer -> `$10` -> `$50` -> `$10` plus projectile/contact preservation, without duplicating existing coverage;
6. update `ENTITY_TYPES_0A_0B.md` and dispatcher wording to state the evidence-backed closure while retaining defensive rejection of injected impossible states;
7. run both verification workflows before checkpointing.

## BLOCKERS

- None. The required producer, ordinary, fall, landing, projectile/contact and post-hit primitives are already promoted.

## ANTI-LOOP

- `last_next_signature`: `entity-0a0b-reachable-action-closure`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only if it produces code, a fixture, new evidence, an evidence map/table, a discarded hypothesis, or an evidence-backed architectural decision.
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
