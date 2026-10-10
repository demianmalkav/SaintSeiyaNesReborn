# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / scene-battle engine state graph $30-$4F`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#115` — reachable engine-state family `$91-$99` from promoted reload `$90` through absorbing terminal state `$99`.
- Merge commit: `07361dfebef8bd803d7a21950ebc865c570fcdce`
- Exact final PR head: `8f43e2c6ead7ff4082d2431ac65af9b220ab37d2`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#300`: `SUCCESS`
  - `Original Spec` run `#493`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#113` closed fatal resource state `$60` and reload `$04=$FF`.
- PR `#111` closed engine family `$11-$14`.
- PR `#109` closed the fixed-bank global `$00/$01` bootstrap/main/NMI dispatcher map.
- PR `#107` closed special-normal platform exits `$02=$0C-$10`.
- PR `#104` closed principal normal warm-reload destinations.
- PR `#102` closed the interactive `$F025 <-> $A275` warm-reload selector.
- PRs `#94/#96/#98` remain authoritative for `$70-$89` narrative and `$04=$8F` reload.

## DONE

### Platform / reload foundations

Platform state `$20`, exit family `$02=$00-$11`, warm-reload selector, stable normal reload destinations `$00/$10/$90`, narrative `$70-$89`, narrative reload `$8F`, and fatal reload `$FF` are closed at the semantic level required by ORIGINAL SPEC.

### Global dispatcher — PR #109

The fixed-bank bootstrap/main/NMI partition is closed. Key promoted bootstrap successors are:

```text
reload $00 -> $20
reload $10 -> $11
reload $90 -> $91
```

### Engine family `$11-$14` — PR #111

Closed end-to-end. `$11` either returns to normal `$3D` reload or generates/displays password text through `$12->$13->$14`; `$14` is absorbing under normal main/NMI execution.

### Resource failure state `$60` — PR #113

Fatal Life/Cosmo drain from platform `$20` converges on the sole reachable state `$60`, advances failure timer `$4D=$D0..DF`, exits through `$04=$FF`, marks the defeated Saint in `$0673`, and composes with promoted reload logic to stable `$00/$90` or the already-known Saga selector.

### Engine family `$91-$99` — PR #115

The high family entered from stable reload `$90` is closed.

Entry/bootstrap:

```text
$90
 -> $D442 INC $00
 -> $91
 -> bank0 $AE18 builds $FF-terminated stream at $0600
 -> $14/$15=$08/$23
 -> $06=min($02,$0C)
```

State graph:

```text
$91 --$0600 terminator $FF--> $92

$92 --count $57:$92 -> 0--> wait for A ($3D bit $80)
$92 --A---------------------> $93, $57=$10

$93 --next NMI--> $94
$94 --next NMI--> $95
$95 --next NMI--> $96
$96 --next NMI--> $97

$97 --each $57 expiry--> $57=$30, $06++
$97 --new $06=$0D-----> $98

$98 --next NMI--> $99
$99 -------------> $99 ...
```

Important mirror behavior:

- `$91->$92` increments both `$00/$01` through bank-1 `$8DDB`.
- `$92->$93`, `$93->$94->$95->$96->$97`, `$97->$98`, and `$98->$99` increment only live `$00`; `$01` catches up at the next main `$C220` mirror synchronization.
- `$99` has no dedicated main/NMI writer and is absorbing until reset/external restart.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineState91To99Machine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineState91To99MachineChecks.cs`
- `docs/reverse-engineering/ENGINE_STATE_FAMILY_91_99.md`
- PR `#115`

Do not reopen `$91-$99` absent contradictory ROM evidence or fixture failure.

## EVIDENCE

### Why `$30-$4F` is selected next

The three direct reload successor families are now accounted for:

```text
$00 -> platform $20
$10 -> $11-$14
$90 -> $91-$99
```

The next large reachable gap in the global dispatcher is the shared scene/battle family `$30-$4F`.

Top-level main structure:

```text
($00 & $F0) == $30 or $40
 -> $C346 controller poll
 -> Start ($3D bit $10) writes $00/$01=$50
 -> otherwise JSR $C659
```

The body already exposes a finite progression skeleton rather than an opaque renderer:

```text
$31 -> dedicated $C659 branch; reachable INC $00 at $C6B0
$32 -> dedicated branch; reachable INC $00 at $C6DD
$33 -> dedicated branch; reachable INC $00 at $C70D
...
$37 -> timed/counter branch; $C984 INC $00 -> $38
$38 -> phase counter $3F; terminal gate $C9A9 writes $00=$40
$40-$4F -> same main family; NMI uses bank-1 $8C19
```

Existing boss-battle documentation already describes substantial mechanics behind stage-indexed dispatchers (`BATTLE_EVENT_DISPATCH.md`, battle resources/damage/dodge/techniques). The missing layer is the **global engine-state graph that composes those mechanics**. This checkpoint should map that layer without re-reversing already-promoted combat formulas.

## OPEN

1. Identify every executable producer that enters the reachable `$30-$4F` family and reject static/data false positives.
2. Determine the exact reachable state set inside `$30-$4F`; do not assume every numeric state in the range is used.
3. Close main/NMI transition writers for the early `$31/$32/$33...` sequence, including the conditions at `$C6B0/$C6DD/$C70D`.
4. Trace the confirmed late chain `$37->$38->$40`, including `$57`, `$03CC`, `$3F`, `$4D/$4E` only insofar as they gate global state.
5. Classify the `$40-$4F` subgraph and its NMI bank-1 `$8C19` cooperation.
6. Prove the `$50` handoff(s), including the top-level Start escape at `$C34F`, but do not open the entire `$50-$5F` family until the scene graph identifies a real continuation requirement.
7. Tie global states to existing battle/event semantic documents where evidence permits; do not duplicate boss mechanics.
8. Renderer, audio and RNG remain outside scope unless one of them directly gates `$00/$01` progression.

## NEXT

**Map the reachable scene/battle engine-state graph `$30-$4F`, from its real entry producer(s) through the confirmed `$37->$38->$40` progression and `$50` handoff boundary.**

Completion criterion:

> Produce an evidence-backed transition graph containing every reachable `$30-$4F` engine state, its main/NMI ownership, every writer that advances or leaves the family, and the first already-known or newly bounded destination, while composing with existing boss-battle semantics instead of reimplementing them.

Required sequence:

1. index executable `$00/$01` producers/advancers that can enter or move within `$30-$4F`;
2. trace `$C346->$C659` as control-flow only and build a state-transition table;
3. reconcile `$31/$32/$33...` transitions with state-local counters and existing battle/event docs;
4. close `$37->$38->$40` and the `$40-$4F` NMI cooperation;
5. isolate every `$50` exit/handoff condition;
6. implement a structural scene-state graph artifact plus discriminating fixtures once reachability is closed;
7. document and run both verification workflows.

## BLOCKERS

- None. Canonical ROM, dispatcher map, `$91-$99` checkpoint and existing battle subsystem documentation are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `ENGINE_STATE_DISPATCHER.md`, `ENGINE_STATE_FAMILY_91_99.md`, and only existing battle docs relevant to the active `$30-$4F` boundary;
4. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
5. use `docs/WORK_PROTOCOL.md` for execution rules;
6. use the private Drive manifest only to locate private assets; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `scene-battle-engine-state-30-4f-graph`
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
