# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / normal reload destination selection`
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
```

A static PRG writer audit found the established lifecycle writer of `$06AB` at `$E165`, storing `$FF`; the closed `$70-$89` sequence does not write that field. Therefore the returning narrative path necessarily follows:

```text
$E13C: $06AB != 0 -> $E257
$E263: $04 == $8F -> $E20E
```

This bypasses the broad `$0670/$067D` generic destination branch.

At `$E20E`, A still contains `$8F`, so `$068F=$8F` is selected for shared initialization. `$E214` can normalize `$050E=$0F` to `$0D` because `$0533=$00`; that side effect does not change destination state.

The common commit closes as:

```text
$E22A A=$00
$E241/$E243 -> $01/$00=$00
$E245-$E24B -> $03=$E505[$0533=$00]=$00
$E24F -> $05=$01
$E254 -> $C180
```

Thus the next stable logical handoff is:

```text
engine state    $00
engine mirror   $00
engine substate $00
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNarrative8FReload.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Narrative8FReloadChecks.cs`
- `docs/reverse-engineering/PLATFORM_RELOAD_MODE_8F.md`
- merged PR `#98`

Do not reopen the `$8F` branch unless a fixture fails or contradictory ROM evidence appears.

## EVIDENCE

The normal platform exit remains distinct from the narrative return:

```text
normal accepted platform exit
 -> clear $04
 -> snapshot Saints
 -> $00/$01=$3D
 -> $E100
```

For this normal path, the same established lifecycle value `$06AB=$FF` means `$E100` reaches `$E257`, but `$04=$00` does **not** take either exceptional `$FF` or `$8F` branch. Control continues into the generic destination logic beginning at `$E26A`, where `$0670` and later persistent progression fields can matter.

This generic warm-reload path has not yet been reduced to the set of outputs reachable specifically from normal platform exits.

## OPEN

1. Normal platform exits enter `$E100` with `$04=$00`; their reachable stable destination states are not yet promoted.
2. The generic warm branch at `$E26A+` reads `$0670` and can later inspect additional persistent progression fields. Their meanings and reachable combinations need to be traced only for normal platform-exit inputs.
3. Cold/generic `$06AB=0` initialization remains outside the next boundary unless normal platform exits can actually reach it; current lifecycle evidence says they cannot.
4. `$04=$FF` reload behavior is a separate selector and should not be conflated with normal `$04=$00` exits.
5. Renderer/audio/stack/banked initialization remains outside logical parity unless it changes destination selection.

## NEXT

**Close the warm normal-platform `$04=$00` branch through `$E100` to its reachable stable engine-state outputs.**

Completion criterion:

> Starting from a confirmed normal `State3DReload` platform exit (`$04=$00`, `$00/$01=$3D`) in the established lifecycle (`$06AB=$FF`), enumerate only the persistent selector combinations that are actually reachable from platform play, derive every resulting value committed at `$E22C-$E254`, and promote the normal-exit handoff without modeling unrelated `$04=$FF/$8F` or cold `$06AB=0` cases.

Required sequence:

1. trace `$E257->$E26A+` for `$04=$00` and identify every field that can affect the value eventually pushed into the common commit;
2. map `$0670` writes/meaning along promoted platform gameplay and determine its reachable values at accepted exits;
3. follow only additional selectors reachable from those `$0670` cases (`$06B8`, `$050E`, `$0533`, `$067D`, `$0673/$06CC`, or others if direct evidence requires them);
4. derive the destination `$00/$01` and final `$03` for each reachable normal-exit case;
5. keep cold `$06AB=0`, `$04=$FF`, renderer and bank-loading internals outside scope unless they alter those outputs;
6. add discriminating fixtures for each genuinely reachable normal-exit outcome;
7. document the selector map and run both verification workflows.

## BLOCKERS

- None. Canonical ROM and the complete fixed-bank `$E100` path are available.

## ANTI-LOOP

- `last_next_signature`: `e100-normal-platform-warm-destination`
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
