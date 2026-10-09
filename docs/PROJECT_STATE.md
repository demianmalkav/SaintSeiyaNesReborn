# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / post-platform-exit state-machine boundary`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#92` — reachable live action-family closure for primary types `$0A/$0B`.
- Merge commit: `748c36a380226a6ed48994e2a435e9de88514293`
- Exact final PR head: `9a00d28d15d72411ef76c6b561fabedc9dfa0edf`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#253`: `SUCCESS`
  - `Original Spec` run `#443`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Previous structural checkpoint: PR `#90` closed the complete persistent normal late-object frame.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Persistent normal platform frame

Closed normal order:

```text
$B6D0 generic producer
 -> platform exit gate
 -> $8927 scheduled producer
 -> player phases
 -> $9B93 multisprite
 -> auxiliary A/B
 -> primary A/B
 -> $A22C
 -> one $3C increment
```

Shared attacks, `$76/$7F/$80`, Seventh Sense and persistent late-object state are threaded in ROM order. Exceptional player exits and platform exits have regression coverage.

### Primary entity family coverage

Normally reachable primary runtime families are now closed for all promoted types:

- `$00-$07`: full confirmed common family set;
- `$08/$09/$0C`: dedicated scheduled/direct special runtime;
- `$0D/$0E`: dedicated runtime;
- `$0F`: dedicated direct runtime;
- `$0A/$0B`: confirmed reachable **live** families exactly `{ $10, $50 }`.

For `$0A/$0B`, PR #92 proved:

```text
$B6D0 spawn -> $10
$10 -> $10 OR $50 OR retirement
$50 -> $50 OR $10 OR retirement
projectile -> action unchanged, +$03 impulse only
contact -> player/contact state only
$A845 -> +$03/X only
$A647 -> visual retirement; optional terminal logical $00 clear
```

The scheduled `$8925-$89D1` producer cannot create `$0A/$0B`; bank-3 `$A4A0/$A4A4` also bypasses the predispatch that can seed `$70`, and `$A970` bypasses the common decision path that can seed `$30`.

Defensive rejection of injected `$30/$40/$70/$D0/$E0` states remains intentional clean-room behavior rather than an assertion about externally corrupted ROM memory.

Do not reopen primary action-family coverage unless a fixture fails or contradictory ROM evidence appears.

## EVIDENCE

Latest checkpoint artifacts:

- `docs/reverse-engineering/ENTITY_TYPES_0A_0B.md`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformCommonEntityActiveDispatcher.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EntityTypes0A0BChecks.cs`
- merged PR `#92`

Persistent frame artifacts remain:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPersistentLateObjectFrame.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PersistentLateObjectFrameChecks.cs`
- `docs/reverse-engineering/PERSISTENT_LATE_OBJECT_FRAME.md`
- merged PR `#90`

Existing exit evidence:

- `docs/reverse-engineering/PLATFORM_EXIT_GATES.md`
- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformExitGate.cs`

The platform gate currently surfaces two semantic transitions:

- normal `State3DReload`;
- special substate `$11` `State70Special`.

## OPEN

1. The immediate engine state-machine behavior **after** semantic `$3D/$70` platform exits is not yet promoted as a clean logical boundary.
2. For `$3D`, the ROM jumps through `$E100`; the stage-dependent high-level narrative destination remains separate from the immediate reload handshake.
3. For `$70`, NMI/main-thread state progression around `$D3BF` / `$C538` is not yet represented.
4. Full renderer-owned tile/Y/attribute/X state remains outside logical runtimes except exact lifecycle writes already promoted.
5. NES-specific PPU/stack/audio plumbing should remain outside the semantic model unless needed to determine logical state transitions.

## NEXT

**Promote the immediate post-platform-exit state-machine boundary for `$3D` and `$70`.**

Completion criterion:

> Starting from an accepted `PlatformExitTransitionKind`, the clean-room model represents every directly confirmed logical engine-state mutation required to hand control to the next engine mode, while explicitly separating stage/narrative destination selection and renderer/audio/stack plumbing that are not needed for logical parity.

Required sequence:

1. trace main-loop and NMI dispatch for `$00/$01 == $3D` and `$00/$01 == $70`;
2. identify the minimum persistent logical fields changed before the next stable state is reached;
3. for `$3D`, separate the generic `$E100` reload handshake from later stage-dependent destination selection;
4. for `$70`, map the first confirmed state/timer progression through `$D3BF` and `$C538` and identify what event advances out of state `$70`;
5. implement semantic result/state types without emulating PPU, stack or sound internals;
6. add discriminating fixtures for normal `$3D` and special `$70` transitions;
7. update exit documentation and run both verification workflows.

## BLOCKERS

- None. The canonical ROM is available and the existing exit gate already provides deterministic entry conditions.

## ANTI-LOOP

- `last_next_signature`: `post-platform-exit-state-machine`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

Rules:

- A cycle counts as progress only with code, test, new evidence, evidence map, discarded hypothesis, or evidence-backed architectural decision.
- If two consecutive cycles finish with the same blocker, same `NEXT`, and no new evidence, a third identical attempt is forbidden.
- On anti-loop trigger: change source/technique or return to the last verified checkpoint.
- Closed primary/frame phases are not re-entered merely because uncertainty exists above them.

## CONTINUE SEMANTICS

When the user says `continúa` with no narrower instruction:

1. read this file first;
2. reconcile it with current `main` if repository history is newer;
3. execute the single `NEXT` in FAST mode until material progress or a real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.
