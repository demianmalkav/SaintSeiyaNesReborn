# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / normal reload interactive transition`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#98` — bounded narrative `$04=$8F` branch through `$E100` to stable engine state `$00`, substate `$00`.
- Merge commit: `97b0a495ce707e339b5b96f3ce36ea878f7f4a33`
- Exact final PR head: `af77331bd34c3d0f513e6fa65b2efa9f6ad93775`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#265`: `SUCCESS`
  - `Original Spec` run `#455`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Previous checkpoints: PR `#96` closed narrative `$80-$89`; PR `#94` closed the immediate `$3D`/special `$70->$80` post-exit boundary; PR `#92` closed primary-family reachability; PR `#90` closed the persistent normal platform frame.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Special platform/narrative chain

Closed sequence:

```text
special platform exit
 -> $70 -> $71 -> $72 -> $73 -> $74 -> $75
 -> $80 -> $81 -> $82 -> $83 -> $84 -> $85 -> $86 -> $87 -> $88 -> $89
 -> $04=$8F, $00/$01=$3D
 -> $E100
```

### Narrative `$04=$8F` reload destination

PR #98 proves that the narrative return is not a generic multibranch reload case.

Closed invariants:

```text
state $72 clears $03
$73-$89 do not write $03
$89 enters $E100 with $04=$8F, $00/$01=$3D, $03=$00
$E505[$03=$00] -> $0533=$00
$06AB=$FF from established lifecycle
$E13C -> $E257
$E263 sees $04=$8F -> $E20E
common commit -> $00/$01=$00, $03=$00, $05=$01, JMP $C180
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNarrative8FReload.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Narrative8FReloadChecks.cs`
- `docs/reverse-engineering/PLATFORM_RELOAD_MODE_8F.md`
- merged PR `#98`

Do not reopen the `$8F` branch unless a fixture fails or contradictory ROM evidence appears.

## EVIDENCE

### Normal platform exit differs structurally

A normal accepted platform exit does:

```text
$96CB  $04=$00
       snapshot Saints
       $00/$01=$3D
       JMP $E100
```

The established lifecycle still carries `$06AB=$FF`, so `$E100` reaches `$E257`; `$04=$00` bypasses both exceptional `$FF` and `$8F` selectors and continues at `$E26A`.

### `$03` is character-dependent at normal platform exits

The platform exit gate itself proves that RAM `$03` is the internal active-Saint index in this mode: `$96A8-$96AC` compares `$03` against internal index `1` to block Shun in substate `$10`.

Therefore `$E121-$E126` maps the active Saint through `$E505` into `$0533`. For internal Saint indices `$00-$04`, the relevant table entries are:

```text
$03:    00 01 02 03 04
$0533:  00 02 01 03 04
```

The normal reload cannot be reduced to the `$8F` path's fixed `$0533=0` assumption.

### First warm selector and reset

At `$E26A`, `$0670` is the first generic branch selector. `$ED57` calls bank-1 `$A973` unless `$0670` is `2` or `3`.

`$A973` clears:

```text
$066F
$0670
$0677
$0678
$064D/$064E
$067C
$068A
$068E
$0690
$06B8
```

and then returns A=`$050E` through `$BDCA`.

This means the ordinary `$0670=0` path re-enters the rest of `$E100` with `$0670=0` and `$06B8=0` after the reset.

### Reload contains an interactive mini-state, not a flat destination table

For the ordinary path, `$E33D+` eventually seeds:

```text
$0584 = 5
```

and calls `$F025`.

`$F025` is a six-way dispatcher using `$0584`; its pointer table is:

```text
case 0 -> $F03C
case 1 -> $F057
case 2 -> $F0B1
case 3 -> $F0D3
case 4 -> $F041
case 5 -> $F1D9
```

Case 5 prepares the interstitial/menu state. `$E34C` then switches to bank 5 and calls `$A275`. `$A275` waits for an input condition, updates `$0584` from `$0585/$0586`, and returns. The loop calls `$F025` again with the newly selected case.

Thus a normal platform reload can spend multiple iterations inside `$F025 <-> $A275` before `$0670` changes and `$E35A` is allowed to leave the loop.

`$FF11` is not the escape in this ROM: it would set `$0670=1` only if ROM byte `$FFDE` were nonzero, but canonical `$FFDE=$00`. Terminal `$0670` changes must therefore come from the selected case handlers / banked routines themselves.

This is a material architectural correction: the normal reload boundary must model an interactive intermediate state before its final engine-state commit.

## OPEN

1. The transition graph of `$F025` cases 0-5 and bank-5 `$A275` is not yet promoted as a semantic state machine.
2. We have not yet enumerated which selected cases can write terminal `$0670` values and which of those are reachable for each platform `$050E` / active-Saint combination.
3. Principal platform exits `$02=$00-$0B` should be closed first; special normal exits `$0C-$10` can follow once the interactive core is understood.
4. The final common `$E22C-$E254` destination outputs for normal exits remain unresolved until the interactive mini-state's terminal outcomes are known.
5. Cold `$06AB=0`, `$04=$FF`, renderer/audio/stack internals remain outside scope unless they alter the normal-exit logical transition.

## NEXT

**Reconstruct the `$F025 <-> $A275` interactive warm-reload mini-state for principal platform exits `$02=$00-$0B`.**

Completion criterion:

> For normal principal platform exits, every reachable `$0584` case transition is accounted for, every terminal write that can release the `$E35A` `$0670==0` loop is identified, and the resulting semantic outcomes are represented without emulating PPU drawing or controller-register plumbing.

Required sequence:

1. map the six `$F025` cases and identify which are pure presentation, which branch by `$050E/$0533`, and which call routines capable of changing `$0670`;
2. reconstruct `$A275` only as an input-to-case transition over `$0584/$0585/$0586`;
3. trace the called bank-5 handlers (notably `$9C91` and any descendants) only until their `$0670`/progression effects are closed;
4. use principal-stage `$067D -> $050E` mapping where confirmed, and retain active-Saint `$03 -> $0533` mapping as a separate dimension;
5. produce a transition table showing entry case, condition, next `$0584` or terminal `$0670` outcome;
6. implement the semantic mini-state plus discriminating fixtures once the graph is closed;
7. then resume `$E35A+` to derive the final normal reload commit.

## BLOCKERS

- None. The canonical ROM, fixed-bank dispatcher and bank-5 handlers are available.

## ANTI-LOOP

- `last_next_signature`: `warm-reload-f025-a275-interactive-state`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, new evidence, an evidence map, a discarded hypothesis, or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.

## CONTINUE SEMANTICS

When the user says `continúa` with no narrower instruction:

1. read this file first;
2. reconcile it with current `main` if repository history is newer;
3. execute the single `NEXT` in FAST mode until material progress or a real blocker;
4. run the smallest relevant VERIFY gate;
5. checkpoint only after verification;
6. update `DONE / EVIDENCE / OPEN / NEXT / BLOCKERS / ANTI-LOOP` before ending the cycle.
