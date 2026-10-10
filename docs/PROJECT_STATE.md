# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / engine state family $91-$99`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#113` — fatal active-platform resource exhaustion, engine state `$60`, reload mode `$04=$FF`, and direct stable destinations.
- Merge commit: `d60c76acb12bef0fa6f59c13e387da0c5b58d55e`
- Exact final PR head: `888058e5702edbdca95cae2f15da1524a93b17f4`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#296`: `SUCCESS`
  - `Original Spec` run `#489`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#111` closed engine family `$11-$14`.
- PR `#109` closed the fixed-bank global `$00/$01` bootstrap/main/NMI dispatcher map.
- PR `#107` closed special-normal platform exits `$02=$0C-$10`.
- PR `#104` closed principal normal warm-reload destinations.
- PR `#102` closed the interactive `$F025 <-> $A275` warm-reload selector.
- PRs `#94/#96/#98` remain authoritative for `$70-$89` narrative and `$04=$8F` reload.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Platform / reload foundations

The platform-local exit family `$02=$00-$11`, warm-reload selector, stable normal reload destinations `$00/$10/$90`, special narrative `$70-$89`, and narrative `$04=$8F` reload are closed at the semantic level required by ORIGINAL SPEC.

### Global dispatcher — PR #109

The fixed-bank bootstrap/main/NMI partition is closed by `EngineStateDispatcherMap` and `ENGINE_STATE_DISPATCHER.md`.

Key bootstrap successors:

```text
reload $00 -> $20
reload $10 -> $11
reload $90 -> $91
```

### Engine family `$11-$14` — PR #111

Closed end-to-end:

```text
$10 bootstrap -> $11

$11 upper/default Start
 -> $3D -> already-promoted reload

$11 lower Start
 -> bank0 $AE18 password output builder
 -> $12
 -> first observing NMI -> $13
 -> text stream terminator $FF at bank1 $8DDB
 -> $14
 -> absorbing normal-engine terminal state
```

`$14` does not advance to another engine family under normal main/NMI execution.

### Resource failure family `$60-$6F` — PR #113

The fatal platform resource boundary is closed.

#### Entry

Bank-1 `$8000` executes Life before Cosmo:

```text
$8012 JSR $927A
$8015 JSR $930A
```

Life subtracts 2. Fatal underflow occurs only when semantic Life is `0` or `1`; Life exactly `2` becomes zero without failure until another Life drain attempt.

Substate `$02=$10` also has a periodic subtract-two Life route that does not consume `$7F`:

- internal Shun: `$3C & $07 == 0`;
- other Saints: `$3C & $1F == 0`.

Cosmo subtracts 1 when `$80!=0`; fatal underflow occurs when Cosmo is already zero. Cosmo still executes on the same frame after a fatal Life subtraction.

Both failures converge at:

```text
$92E3 LDA #$60
$92E5 STA $00
$92E7 STA $01
$92E9 LDA #$D0
$92EB STA $4D
```

#### Reachable family set

For this subgraph:

```text
reachable engine-state set = { $60 }
```

`$61-$6F` are structurally dispatchable by the high-nibble router but have no reachable producer/advance in this failure path.

#### State `$60` lifetime

Main `$C364-$C3AB` advances `$4D/$4E`, not `$00/$01`.

On frames where `($3C & $0F)==0`, `$4D` increments. Starting at `$D0`, it progresses through `$DF`. When the next increment would reach `$E0`:

```text
$C389 LDA #$FF
$C38B JMP $C2BA
```

The `$E0` value is not stored back to `$4D/$4E`.

NMI `$D2F0 -> bank1 $9D69` is presentation/resource display work and does not change global engine state.

#### Reload mode `$04=$FF`

`$C2BA` performs:

```text
$04=$FF
refresh Saint snapshot
$00/$01=$3D
JMP $E100
```

`$E257` recognizes `$04=$FF`, reaches `$E3ED`, switches to bank 5, and calls `$970A`.

The mode-$FF prelude:

1. forces `$0670=$FF`;
2. ORs the current canonical Saint mask from `$FFC0` into `$0673`:
   - Seiya `$01`
   - Hyoga `$02`
   - Shun `$04`
   - Shiryu `$08`
   - Ikki `$10`;
3. stage `$05` stores `$067E=$050E`, clears `$0525`, runs its local presentation and clears `$04=0` before returning;
4. stage `$0A` with `$06B8!=0` redirects `$067D=$02`, swaps active work state to canonical Seiya, and sets `$0533=0`;
5. otherwise returns with current Saint/progression intact.

The new death mark then composes with the already-promoted terminal-`$FF` branch from #104.

Direct stable results when `$06CE=0`:

```text
(($0673 | $06CC) & $0F) == $0F -> stable $90, $068F=$DD
otherwise                         -> stable $00
```

Ikki's `$10` bit is outside the four-persistent-Saint completion low nibble.

When reachable Saga story phase `$06CE!=0`, the fixed gate checks story phase first and hands control back to the already-promoted interactive selector rather than committing a new stable state.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformResourceFailureTransition.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PlatformResourceFailureTransitionChecks.cs`
- `docs/reverse-engineering/PLATFORM_RESOURCE_FAILURE_STATE_60.md`
- PR `#113`

Do not reopen `$60/$04=$FF` absent contradictory ROM evidence or fixture failure.

## EVIDENCE

### Why `$91-$99` is selected next

The other direct unresolved successor of an already-promoted reload remains the high family:

```text
stable reload $90
 -> $C180 short bootstrap
 -> $D442 INC $00
 -> engine state $91
```

The global dispatcher already bounded a finite state chain and its candidate writers:

```text
$91 -> candidate $92 through bank1 text terminator $8DDB
$92 -> $93 through main $C3E8 INC $00
$93 -> $94 through NMI $D55E
$94 -> $95 through NMI
$95 -> $96 through NMI
$96 -> $97 through NMI
$97 -> $98 through bank1 $9381 after local terminal counter
$98 -> $99 through NMI $D365
```

These writer addresses are structural anchors only. Exact call-path reachability, frame lifetime and semantic effects still need family-specific proof.

Known top-level ownership from #109:

- `$91`: main bank-1 `$9363`; NMI `$D42D` plus `$07FC=$F0`.
- `$92`: dedicated main `$C3C3`.
- `$93-$98`: main bank-1 `$9363`.
- `$93-$96`: NMI-owned state increments.
- `$98`: dedicated NMI transition toward `$99`.
- `$99`: no dedicated top-level dispatcher body was promoted; terminal/next behavior remains open.

This is now the most direct closed-to-open control-flow boundary. Wider `$30-$4F`, renderer, RNG and audio remain later fronts.

## OPEN

1. Prove exact `$91->$92` reachability through the bank-1 text/state machinery and identify the terminator/source that reaches `$8DDB`.
2. Reduce dedicated state `$92` main `$C3C3-$C3xx` to logical effects and prove exact `$92->$93` condition at `$C3E8`.
3. Characterize `$93-$97` NMI/main cooperation: which states are one-NMI transitional and which wait on presentation/text/local counters.
4. Prove the state-$97 terminal condition that reaches bank-1 `$9381` and writes `$98`.
5. Close `$98->$99` through NMI and determine whether `$99` is absorbing, redirects to reload, or enters another already-known family.
6. Record persistent control fields only where they select later state transitions; ignore renderer/audio bodies unless logically gating the graph.
7. `$30-$4F`, RNG, renderer/metasprites, audio and broader boss progression remain outside this checkpoint.

## NEXT

**Close engine-state family `$91-$99` from promoted reload `$90` through the first stable or already-promoted destination after `$99`.**

Completion criterion:

> Starting from confirmed `$90->$91`, prove every reachable logical transition through `$91-$99`, distinguish text/input/timer-driven states from one-NMI transitional states, identify exact writers/conditions for each advance, and terminate at the first stable state or existing promoted family without modeling unrelated rendering/audio internals.

Required sequence:

1. trace `$91` through main bank-1 `$9363`, NMI `$D42D`, and the relevant text source until `$91->$92` is proved or disproved;
2. trace dedicated main state `$92` at `$C3C3+` through exact `$93` writer;
3. close NMI `$93->$94->$95->$96->$97`, recording any logical writes before each increment;
4. trace state `$97` main/bank-1 logic to the `$9381` increment and prove the terminal counter/condition;
5. trace NMI state `$98` to `$99` and classify `$99` under both main and NMI;
6. implement the smallest semantic `$91-$99` machine plus discriminating fixtures only after the graph is closed;
7. document the family and run both verification workflows.

## BLOCKERS

- None. Canonical ROM, dispatcher map, verified reload `$90` entry and writer anchors are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `ENGINE_STATE_DISPATCHER.md` and only code/docs/tests required by `$91-$99`;
4. use `PLATFORM_RESOURCE_FAILURE_STATE_60.md` only if reviewing the immediately previous checkpoint;
5. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
6. use `docs/WORK_PROTOCOL.md` for execution rules;
7. use the private Drive manifest only to locate private assets; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `engine-state-family-91-99`
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
