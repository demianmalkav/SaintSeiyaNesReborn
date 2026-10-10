# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / post-Saga ending tail`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#135` — complete Saga stage `$067D=$0D / $050E=$0A` multiphase final-boss machine, including both inherited ingress variants, Ikki/Seiya phase re-entry, support/technique progression and exact victory/defeat boundaries.
- Merge commit: `36d5929502f8beb535b44ab3e54d3a1d1213b4a9`
- Exact final PR head: `bfc65e9d18848c7dec9d0b17d328ad0cebbbe254`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #341: `SUCCESS`
  - `Original Spec` #538: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- PR #133 closes final-special `$0C`; PR #131 closes Pisces/Aphrodite `$09`; PR #129 closes Aquarius/Camus `$08`; PR #127 closes Virgo/Shaka `$05`; PR #125 closes Leo/Aioria `$04`; PR #123 closes Taurus/Aldebaran `$01`.
- PR #121 closes the complete global `$00/$01` namespace.

## DONE

### Saga stage `$0A` — PR #135

Closed the complete reachable `$06CE` final-boss graph across:

```text
initialization       $9B5D -> {0:$9B69, 1:$9B9E, 2:$9C2C}
Talk                 $9FF4 -> {0:$A000, 1:$A0E0, 2:$A115}
post-Bronze action   $AB18 -> {0:$AB24, 1:$AB62, 2:$AB6F}
post-Gold response   $AC05 -> {0:$AC11, 1:$AC3E, 2:$AC76}
Gold selector        bank6 $90EC+ -> {0:$9104, 1:$911D, 2:$9135}
```

Promoted semantics:

- both final-special predecessor variants reach Saga at `$067D=$0D/$050E=$0A/$0673=$3E` with inherited active Seiya or Shun and canonical `$06CE=0`;
- global initialization is the proven seed for `$06CE=0`; common battle reset preserves `$06CE`;
- initial story ingress does **not** call Saga init `$9B5D`; the first fight starts in phase 0 with the inherited Saint;
- common entry reset arms scripted Bronze-hit block `$0690=$FF`, so phase 0 cannot connect normal Bronze damage;
- phase-0 first Bronze action increments `$0678`, performs the deliberate four-`PLA` unwind and suppresses the Gold response;
- first post-miss phase-0 Talk advances `$066F/$06CF/$06D0`; later Bronze actions can set `$06D0=$FF` from the player-condition classifier;
- phase-0 Gold selector exposes slot0 `35/23` or slot1 `30/30`; low/dead player after Gold response emits release `$FF`;
- release `$FF` owner `$E3ED->$F2E4->$970A` invokes init `$9B69`, saves the inherited Saint record, forces Ikki (`$0533=4`), advances `$06CE:0->1`, clears phase dialogue state and re-arms `$0690=$FF`;
- Ikki's first phase-1 Talk is the exact `$A1F4` event that clears `$0690`; skipping that Talk can carry `$0690=$FF` into the final Seiya phase and leave normal Bronze connections script-blocked;
- phase-1 post-Bronze consumes player condition; `$0649` is the selected Bronze technique slot; phase-1 Gold selection reaches slot2 `60/60` for `$0649=0`, slot0 `35/23` for nonzero `$0649`, and slot3 `60/60` when `$06D0=$FF`;
- phase-1 low/dead Gold response emits the second `$FF`; init `$9B9E` then forces Seiya, advances `$06CE:1->2`, clears `$06CF/$06D0/$066F/$064D/$0681`, reloads Seiya technique count and calls `$FDE0` exactly 1000 times, granting nominal +1000 Seventh Sense subject to the global cap;
- `$9B9E` does not rewrite `$0690`; therefore the Ikki Talk clear — or its absence — survives into phase 2;
- phase-2 first Talk runs the final scripted presentation and increments `$066F`;
- Saga command-3/Escape is blocked in phases 0/1 and becomes a one-shot support gate in phase 2: final Talk must occur before the first phase-2 Escape, otherwise `$06D0` is consumed without opening support;
- correct ordering opens `$068F=$55`, clears `$06D4` and enters the support overlay;
- support masks from `$F786` are recorded in `$06D4`; every newly confirmed bit grants +1000 Seventh Sense; the first new support also writes `$0587/$0696=3`, proving the exact Pegasus Rolling Crash unlock event; repeated use of an already-set support bit gives no reward;
- phase-2 post-Bronze alone consumes opponent condition: first `$EB=$01` latches `$064D`, `$EB=$FF` resets `$06CE` and emits victory `$01`;
- phase-2 Gold selector always uses slot3 `60/60`; `$EA=$01` is nonterminal feedback, while `$EA=$FF` resets `$06CE` and emits special defeat `$DD`;
- all four structural Saga Gold slots are reachable somewhere in the complete machine, but no single phase exposes all four;
- structural init phase 2 `$9C2C` is canonically unreachable because Saga init is entered through `$FF` re-entry and phase 2 has no reachable `$FF` terminal;
- victory `$01` advances `$067D:0D->0E`, produces `$06CD=00/$0673=30/$050E=00`, rewrites release to `$05`, commits engine bootstrap `$00`, and the already-closed dispatcher maps that bootstrap to immediate state `$20`;
- final defeat `$DD` leaves story progress `$0D`, writes `$0673=$3F/$068F=$DD`, runs the dedicated defeat overlay and commits bootstrap `$90->$91`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/SagaStage0AContext.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SagaStage0AContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_0A_SAGA.md`
- promoted Seiya progression in `docs/reverse-engineering/BATTLE_TECHNIQUES.md`
- promoted Saga phase dispatch in `docs/reverse-engineering/BATTLE_EVENT_DISPATCH.md`
- PR #135

Do not reopen Saga stage `$0A` without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Why the post-Saga ending tail is next

Saga victory does not prove an immediate title/credits terminal. It hands control back to the already-promoted platform engine:

```text
Saga phase-2 opponent defeat
  -> release $01
  -> $067D: $0D -> $0E
  -> $06CD=$00 / $0673=$30 / $050E=$00
  -> release rewritten to $05
  -> stable bootstrap $00
  -> global bootstrap successor $20
```

Fixed platform-substate mapping `$E4D7/$E4E0` gives:

```text
progress $067D=$0E -> $02=$11
```

This is the special two-page platform map already reconstructed in `PLATFORM_MAP_KITS.md`.

Its accepted exit is also already known:

```text
substate $11
player_x >= $D0
player_y == $50
jump_phase == 0
  -> $00=$70
  -> $26/$27=0
  -> $57=$C0
```

Unlike all normal platform exits, this path does not use the `$3D` reload and does not snapshot Saints.

The existing platform state specifications then close the following *individual* layers:

```text
$70 -> $71 -> $72 -> $73 -> $74 -> $75 -> $80
$80 -> $81 -> $82 -> $83 -> $84 -> $85 -> $86 -> $87 -> $88 -> $89
$89 -> $04=$8F / $00=$01=$3D -> $E100
```

States `$80-$88` are NMI-owned narrative scripts with confirmed pointer/terminator progression; state `$89` performs the `$8F` reload handoff.

`PLATFORM_RELOAD_MODE_8F.md` proves the bounded reload result:

- entry `$04=$8F/$03=0/$06AB=$FF` takes the warm `$E257` branch;
- `$E263-$E267` jumps to `$E20E`, writes `$068F=$8F`, invokes `$F381`, and bypasses the generic `$0670/$067D` destination selectors;
- the common commit produces stable engine state `$00`, internal Seiya `$03=0`, and returns through `$C180`, whose already-closed bootstrap successor is `$20`.

Fresh canonical-ROM inspection adds two discriminating anchors for the unresolved tail:

```text
$F38D-$F3A2 when $068F==$8F:
  $06CD=$20
  $0673=$20
  $06CC=$21

$F3B0-$F3BC when $068F==$8F:
  invokes $E589 with A=0
  transfers into the banked continuation at $BC39
```

The bounded `$8F` reload does not call progression-to-substate mapper `$E4D7`; therefore the high-level destination after this second bootstrap cannot be inferred by simply reapplying `$067D=$0E -> $02=$11`. It must be traced through the actual `$068F=$8F` setup and subsequent state-$20` control.

This is now the only unresolved canonical control tail directly downstream of the proven Saga victory. The already-promoted `$70-$89` layers must be **composed**, not reopened.

## OPEN

1. Join the exact Saga victory state to platform substate `$11` entry and prove all relevant carried fields at the first state `$20` frame.
2. Compose the already-closed substate-`$11` physical exit with `$70-$75` and `$80-$89` without re-reversing those state machines.
3. Trace the `$8F` reload beyond its current bounded commit, including `$068F=$8F`, `$06CD=$20`, `$0673=$20`, `$06CC=$21`, `$F381/$F5B6` setup and the banked continuation reached from `$F3BC`.
4. Prove what state `$20` actually owns after the `$8F` reload: normal platform replay, ending-specific interactive state, loop, title/front-end handoff, or another bounded subsystem.
5. Resolve any post-`$8F` writes to `$02/$04/$0670/$067D/$068F` before assigning high-level semantics; do not assume that the preserved progress byte alone chooses the destination.
6. Identify the true terminal/restart boundary of the original game and join it to an already-closed global/front-end owner, or isolate the smallest still-unclosed state family if the tail continues beyond current coverage.
7. Implement only the missing composition/context and discriminating fixtures; reuse `PlatformExitGate`, `PlatformPostExitStateMachine`, `PlatformNarrative80To89StateMachine`, `PlatformNarrative8FReload` and global `$00/$01` contracts.
8. Stop when the Saga victory path has one evidence-backed successor chain all the way to a true terminal, restart/title boundary, or an explicitly isolated next subsystem with no ambiguous intermediate ownership.

## NEXT

**Close the complete post-Saga ending tail from the proven `$067D=$0E / release $05 / bootstrap $00->$20` victory boundary through platform substate `$11`, the special `$70->$89` narrative chain, reload mode `$8F`, and the true terminal/restart boundary of the original game.**

Completion criterion:

> Starting from the exact `SagaStage0AContext` victory boundary, compose the existing platform/narrative specifications to prove entry into `$02=$11`, traverse the accepted `$11 -> $70 -> ... -> $89 -> $E100` path, then close the currently unresolved `$068F=$8F` return far enough to determine whether control loops, resumes interactive play, enters a final scene, or returns to the front-end. Every reachable post-Saga transition must have a known owner; already-closed `$70-$89` internals must be reused rather than duplicated.

## BLOCKERS

- None. Canonical ROM, Saga victory boundary, progress-to-platform map, substate `$11` exit, `$70-$89` state machines, bounded `$8F` reload and global dispatcher are all available.

## RECOVERY CONTRACT

1. Read this file from `main`.
2. Reconcile it with newer merged Git history if present.
3. Read `BOSS_CONTEXT_STAGE_0A_SAGA.md` only for the exact victory boundary.
4. Reuse `PLATFORM_MAP_KITS.md`, `PLATFORM_EXIT_GATES.md`, `PLATFORM_POST_EXIT_STATE_MACHINE.md`, `PLATFORM_NARRATIVE_STATES_80_89.md`, `PLATFORM_RELOAD_MODE_8F.md` and `ENGINE_STATE_DISPATCHER.md` as frozen predecessors.
5. Inspect only the missing composition around progress `$0E` entry and post-`$8F` state-$20` ownership, especially fixed `$F381+` and its banked continuation; follow callers/writers before assigning ending semantics.
6. Do not call `$20` an ending/title state: it is the generic platform body unless the `$8F` mode proves a specialized reachable branch.
7. Drive is private ROM/evidence storage only; it never owns a separate `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `post-saga-ending-tail-8f`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
