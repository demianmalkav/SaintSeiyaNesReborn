# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / engine state family $11-$14`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#109` — fixed-bank global `$00/$01` bootstrap/main/NMI dispatcher map.
- Merge commit: `2f6b4b3bbb8a5754e5be229c62dfd7ec10a77ffa`
- Exact final PR head: `471213536186abc75db7bbb9df12685f528f5224`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#288`: `SUCCESS`
  - `Original Spec` run `#480`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#107` closed special-normal platform exits `$02=$0C-$10`.
- PR `#104` closed principal normal warm-reload destinations.
- PR `#102` closed the interactive `$F025 <-> $A275` warm-reload selector.
- PRs `#94/#96/#98` remain authoritative for the `$70-$89` narrative and `$04=$8F` reload path.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Complete platform exit/reload boundary

The platform-local exit family `$02=$00-$11` remains closed and must not be reopened without contradictory ROM evidence or fixture failure.

Promoted normal reload destinations include stable engine states:

```text
$00
$10
$90
```

plus explicit selector reentry where already documented.

### Global `$00/$01` dispatcher partition — PR #109

PR #109 closes the structural top-level map of the fixed-bank bootstrap, main-loop dispatcher and NMI companion dispatcher without emulating renderer/audio bodies.

Canonical bootstrap at `$C180`:

```text
reload $00 -> full bootstrap -> $20
reload $10 -> short bootstrap -> $D442 INC $00 -> $11
reload $90 -> short bootstrap -> $D442 INC $00 -> $91
```

Therefore `$10` and `$90` are bootstrap entry states, not long-lived ordinary frame states.

Main loop begins at `$C21E` and mirrors:

```text
$01 = $00
```

before dispatch.

Promoted main partition:

```text
$00          -> common tail only
$01-$10      -> bank-1 $9363
$11          -> dedicated $C246 body
$12-$14      -> bank-1 $9363
$15-$1F      -> common tail only / reachability unproved
$20          -> platform $C2F9
$21-$2F      -> common tail only / reachability unproved
$30-$4F      -> shared scene family $C346/$C659
$50-$5F      -> no dedicated main body at this level
$60-$6F      -> $C364 family
$70-$7F      -> $C538 family
$80-$8F      -> no dedicated main body at this level
$90          -> bootstrap input when returned through $C180
$91          -> bank-1 $9363
$92          -> dedicated $C3C3
$93-$98      -> bank-1 $9363
$99+         -> common tail at this dispatcher level
$3D          -> direct $E100 reload
```

NMI at `$D269` has a two-stage selector.

First it checks **mirror `$01`**:

```text
$01=$50 -> $DABC
$01=$3D -> $E000
```

Only then does it reload live `$00`.

Promoted live-NMI cases:

```text
$12 -> $D543, then $00/$01 increment to $13
$13 -> $D42D
$20 -> $D7F2/$D988
$34 -> $D73B
$40-$4F -> bank-1 $8C19 path
$60-$6F -> bank-1 $9D69 path
$70 -> $D3BF
$73 and $80-$8F -> bank-1 $8C19 path
$91 -> $D42D plus $07FC=$F0
$93 -> NMI increments to $94
$94-$96 -> NMI increments one state
$98 -> NMI increments to $99
```

All other values fall through the common NMI tail at this dispatcher level.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineStateDispatcherMap.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineStateDispatcherMapChecks.cs`
- `docs/reverse-engineering/ENGINE_STATE_DISPATCHER.md`
- PR `#109`

Do not reopen the top-level partition unless a later family trace demonstrates an omitted logical state branch.

## EVIDENCE

### Direct unresolved successors of promoted reload states

Two unresolved states are now proved reachable directly from already-closed reload outcomes:

```text
promoted reload $10
 -> $C180
 -> $D442 INC $00
 -> engine state $11

promoted reload $90
 -> $C180
 -> $D442 INC $00
 -> engine state $91
```

Both are real boundaries. `$11` is selected first because it is the lower and smaller directly reachable family and already exposes a bounded chain through `$12/$13`.

### Visible writers in the `$11-$14` family

Confirmed so far:

```text
$10 -> $11:
  $D442 INC $00

$11 -> $12:
  dedicated main body at $C246+
  $C298 LDA #$12
  $C29A STA $00
  $C29C STA $01

$12 -> $13:
  NMI $D2A2+
  $D2A5 INC $00
  $D2A7 INC $01

candidate paired increment relevant to $13/$14:
  bank-1 $8DDB INC $00
  bank-1 $8DDD INC $01
```

The existence of `$8DDB` is confirmed. Its exact call-path reachability from state `$13`, and therefore the actual `$13->$14` transition, is **not yet promoted**.

### Deferred high family

The parallel high family is structurally bounded but deliberately deferred:

```text
$90 -> $91
$92 candidate via bank-1 paired state increment
$92 -> $93 via main $C3E8
$93-$96 -> successive NMI increments
$97 -> $98 via bank-1 $9381 after local terminal counter
$98 -> $99 via NMI $D365
```

Do not switch to `$91-$99` until `$11-$14` is closed or evidence proves the lower family is unreachable beyond its already-confirmed `$11` entry.

## OPEN

1. Exact semantics and duration of engine state `$11` are not yet promoted beyond the dispatcher/writer skeleton.
2. The dedicated `$11` body at `$C246-$C2A8` must be reduced to logical conditions for remaining in `$11` versus committing `$12`.
3. State `$12` is known to advance to `$13` from NMI, but its main/NMI cooperation and frame lifetime need to be characterized.
4. State `$13` must be traced into the bank-1 text/state machinery far enough to prove or disprove reachability of `$8DDB` and therefore `$14`.
5. State `$14` must be closed through its next stable state/family; do not assume that a syntactic increment writer is actually reached.
6. Persistent logical writes that survive the family — especially `$02`, `$04`, `$05`, `$06`, `$14/$15`, `$57`, and any state-selection fields — must be recorded only where they alter later control flow.
7. Renderer/PPU/audio bodies remain out of scope unless they gate a logical state transition.
8. `$91-$99`, `$30-$6F`, RNG, renderer, audio and broader boss progression remain later fronts.

## NEXT

**Close engine-state family `$11-$14` from promoted reload `$10` to its next already-known or newly-promoted destination.**

Completion criterion:

> Starting from the confirmed `$10->$11` bootstrap, account for every reachable logical transition through `$11`, `$12`, `$13` and `$14`, prove which of those states are one-frame/transitional versus input/text-driven, identify the exact condition and writer for each state advance, and terminate at the first state outside `$11-$14` without modeling unrelated rendering/audio internals.

Required sequence:

1. trace `$C246-$C2A8` for engine state `$11`, separating input/UI plumbing from the exact condition that writes `$12`;
2. prove the full `$12` lifetime across main `$9363` and NMI `$D2A2-$D2A9`, including whether any logical writes precede the forced `$13` transition;
3. trace `$13` through its main `$9363` path, NMI `$D42D`, and the relevant bank-1 text/state call chain until `$8DDB` is either proved reachable or excluded;
4. if `$13->$14` is proved, close state `$14` to its first state outside the family and record all persistent control fields; if not, document the real alternate transition;
5. implement the smallest semantic `$11-$14` state machine and discriminating fixtures only after the transition graph is closed;
6. reuse `EngineStateDispatcherMap` for top-level ownership rather than duplicating dispatcher logic;
7. document the family and run both verification workflows.

## BLOCKERS

- None. The canonical ROM and the global dispatcher map are available.

## RECOVERY CONTRACT

A new session must be able to resume this project without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `ENGINE_STATE_DISPATCHER.md` plus only the code/docs/tests required by the active `$11-$14` boundary;
4. use `docs/REVERSE_ENGINEERING_STATUS.md` only as a global navigation/maturity map;
5. use `docs/WORK_PROTOCOL.md` for execution rules;
6. when private assets are required, use the private Drive `PRIVATE_WORKSPACE_MANIFEST — Saint Seiya Reborn`; private evidence is indexed under `04_REVERSE_ENGINEERING/EVIDENCE_INDEX`;
7. Drive never overrides this file and never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `engine-state-family-11-14`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, new evidence, an evidence map, a discarded hypothesis, or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

## CONTINUE SEMANTICS

When the user says `continúa` or `next` with no narrower instruction:

1. read this file first;
2. reconcile it with current `main` if repository history is newer;
3. execute the single `NEXT` in FAST mode until material progress or a real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.
