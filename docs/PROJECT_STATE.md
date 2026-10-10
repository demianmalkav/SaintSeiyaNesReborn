# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / engine state $50 modal subsystem`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#117` — reachable scene/battle engine-state graph `$30-$4F`, including entry from `$50`, exact reachable-state partition, Start handoff and terminal return to `$50`.
- Merge commit: `1ec9af023f4f579452613ff3eddeac45570ddbbf`
- Exact final PR head: `3cc0265e7916fae1b3fbc25b71a08972baa34f33`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#304`: `SUCCESS`
  - `Original Spec` run `#497`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#115` closed engine family `$91-$99`.
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

Closed from reload `$90` through text, countdown/input and NMI presentation states to absorbing `$99`.

### Scene/battle graph `$30-$4F` — PR #117

The global outer scaffold surrounding battle/presentation is now closed.

#### Entry

Canonical scene resume is produced by exact state `$50` main mode:

```text
$DA5C  LDY #$30
$DA5E  LDA $0200
$DA61  CMP #$06
$DA63  BEQ $DA9D
...
$DA9D  STY $00
$DA9F  STY $01
$DAA1  JMP $C1D0
```

`$C1D0->$D5DA` immediately increments live `$00` from `$30` to `$31`; `$C220` synchronizes `$01` before the ordinary dispatcher runs. Therefore `$30` is reachable only as a bootstrap transient and paired `$31/$31` is the first dispatch-ready scene state.

#### Reachable state set

```text
$30,
$31-$38,
$40-$4D
```

Canonical unreachable values inside the structural dispatcher range:

```text
$39-$3F
$4E-$4F
```

#### Main/NMI graph

```text
$50 resume ($0200=$06)
 -> transient $30
 -> $31
 -> $32
 -> $33
 -> $34
 -> NMI -> $35
 -> $36
 -> $37
 -> $38
 -> direct write $40
 -> $41 -> $42 -> $43 -> $44 -> $45 -> $46
 -> $47 -> $48 -> $49 -> $4A -> $4B -> $4C -> $4D
 -> $50
```

Exact promoted gates:

- `$31->$32`: post-decrement `$3F==0` at `$C6B0`;
- `$32->$33`: first scene-object field 1 zero after its step at `$C6DD`;
- `$33->$34`: second scene-object field 1 zero after its step at `$C70D`;
- `$34->$35`: first observing NMI via `$D2C7->$D73B->$D78B`;
- `$35->$36`: observed `$03BB==$65` at `$C8B7`;
- `$36->$37`: post-decrement `$3F==$44` at `$C940`, seeding `$57=$10`;
- `$37->$38`: `$57==0` and entering the frame with `$03CC >= $88` at `$C984`;
- `$38->$40`: incremented `$3F >= $60`; `$C9A9` writes `$40` directly, proving `$39-$3F` are skipped;
- `$40-$4C`: bank-1 `$8C19` text streams; terminal `$FF` reaches `$8DDB` and increments **both** `$00/$01`;
- `$4D->$50`: after shared `$57/$26` delay, `$8D41` writes paired `$50`, proving `$4E/$4F` are not reached.

Every `$3x/$4x` main frame checks Start first. Controller bit `$10` preempts local state logic and writes paired `$00/$01=$50` at `$C34F`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/SceneBattleEngineStateGraph.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SceneBattleEngineStateGraphChecks.cs`
- `docs/reverse-engineering/SCENE_BATTLE_ENGINE_STATE_30_4F.md`
- PR `#117`

Do not reopen `$30-$4F` absent contradictory ROM evidence or fixture failure.

## EVIDENCE

### Why exact state `$50` is selected next

PR #117 proved `$50` is a genuine modal hinge rather than a broad unresolved `$50-$5F` numeric family.

Confirmed producers into exact `$50` include:

```text
$3x/$4x Start escape
 -> $C34F writes $00/$01=$50
 -> A=$05
 -> JMP $DA15

$4D terminal NMI
 -> bank1 $8D41 writes $00/$01=$50
 -> fixed NMI notices live $50
 -> $D2E6 JSR $C154
 -> $D2E9 JMP $C14B
 -> $C14B writes paired $50 and enters $DA13
```

Global bootstrap/reset paths can also enter `$C14B`, so the modal subsystem is not scene-exclusive.

Main modal engine:

```text
$DA13/$DA15 seeds $0200
$DA33 reads $0201
 -> inline dispatch selects $DA3D or $DA5C
```

NMI modal engine:

```text
$D282 sees mirror $01=$50 before all live-state dispatch
 -> JMP $DABC
$DABC dispatches on $0200 through a finite inline table
```

The NMI `$0200` table visibly contains only a small finite set of handlers:

```text
$00-$03 -> $DAEB
$04     -> $DB04
$05     -> $DB27
$06-$07 -> $DB7C
$08     -> $DB84
$09     -> $DB8C
```

Known exits from the main modal path:

- `$0200=$06` -> paired `$30` -> scene bootstrap `$31`;
- `$0200=$08` -> paired `$10` -> already-promoted `$11-$14` bootstrap path;
- another `$DA90+` fallback with `$0202==0` also selects `$10`.

This is a bounded, directly reachable subsystem whose closure will explain the modal/menu bridge between scene/battle, password/low-family flows and global startup behavior.

## OPEN

1. Identify every executable producer entering exact state `$50`, distinguishing scene Start, `$4D` completion and global/bootstrap entry.
2. Map `$DA13-$DAA4` main semantics around `$0200/$0201/$0202`, including both inline `$8960` dispatch branches.
3. Map NMI `$DABC` dispatch states `$0200=$00-$09` and prove which values are actually reachable in each entry mode.
4. Determine exact transitions that mutate `$0200`, `$0201` and `$0202`; avoid interpreting renderer/input helpers unless they gate modal progression.
5. Prove all stable exits: `$50->$30`, `$50->$10`, and any additional real exit if present.
6. Classify whether `$50` is pause/menu, boot/menu, or a shared modal shell only after the control graph is proven; labels must follow evidence.
7. Implement one executable modal state machine plus discriminating fixtures and documentation.
8. Renderer, audio, RNG and deeper boss mechanics remain outside this checkpoint unless they directly gate `$0200/$0201/$0202` progression.

## NEXT

**Close exact engine state `$50` as a modal subsystem: map its `$0200/$0201/$0202` main/NMI state machine from every confirmed entry to every confirmed exit.**

Completion criterion:

> Produce an evidence-backed state graph for exact global engine state `$50`, including entry mode, all reachable `$0200` substates, `$0201/$0202` control roles, main/NMI ownership, and all exits back to already-promoted engine families, without reverse-engineering unrelated rendering/audio internals.

Required sequence:

1. enumerate `$50` producers and normalize their initial `$0200/$0201/$0202` conditions;
2. trace main `$DA13-$DAA4`, including the `$0201` inline dispatcher at `$DA33`;
3. trace NMI `$DABC` inline `$0200` dispatcher and every handler that mutates modal control state;
4. build a finite transition table for reachable modal substates;
5. prove exits `$30/$10` and check for any additional real global-state writer;
6. implement the smallest executable `$50` modal model + fixtures;
7. document and run both verification workflows.

## BLOCKERS

- None. Canonical ROM, fixed dispatcher, scene/battle graph, low-family/password graph and all relevant fixed-bank anchors are available.

## RECOVERY CONTRACT

A new session must be able to resume without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect `SCENE_BATTLE_ENGINE_STATE_30_4F.md` and `ENGINE_STATE_DISPATCHER.md`;
4. inspect only fixed/bank routines required by exact state `$50` (`$C14B`, `$DA13-$DB9C`, and helpers only where they gate modal control);
5. use `docs/REVERSE_ENGINEERING_STATUS.md` as navigation only;
6. use `docs/WORK_PROTOCOL.md` for execution rules;
7. use the private Drive manifest only to locate private assets; Drive never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `engine-state-50-modal-subsystem`
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
