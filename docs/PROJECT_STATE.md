# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / reload destination selection`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#96` — NMI-driven narrative states `$80-$89` and final `$3D/$E100` handoff.
- Merge commit: `7a63447a9d3204ec13e11260192d6448a5a61262`
- Exact final PR head: `46d49e3bb732d9d6862120303b91b645a33b8983`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#261`: `SUCCESS`
  - `Original Spec` run `#451`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Previous checkpoints: PR `#94` closed the immediate `$3D`/special `$70->$80` post-exit boundary; PR `#92` closed primary-family reachability; PR `#90` closed the persistent normal platform frame.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Immediate post-platform exit

Normal exit is closed through `$3D/$E100`; special substate `$11` is closed through the split NMI/main sequence:

```text
$70 -> $71 -> $72 -> $73 -> $74 -> $75 -> $80
```

### NMI narrative chain `$80-$89`

PR #96 closes the continuation:

```text
$80 -> $81 -> $82 -> $83 -> $84 -> $85 -> $86 -> $87 -> $88 -> $89
```

Confirmed semantics:

- main thread contributes the global `$00->$01` mirror but no dedicated `$80-$89` transition;
- NMI routes the `$80` high-nibble family through bank-1 `$8C19`;
- `$57` is the pre-script countdown; the `$57->1` setup seeds `$26=1` before `$8D28` can cross the text gate;
- state `$73` uses script `$8F9C`;
- `$80-$88` map to scripts `$8FC4,$8FEF,$901A,$9047,$9070,$9081,$90A3,$90CA,$90F1`;
- script `$FF` terminator at `$8DDB` increments both `$00/$01` and seeds `$57/$26/$27=$80`;
- state `$89` selects no tenth script; after the same timing gate it writes `$04=$8F`, `$00/$01=$3D` and jumps to `$E100`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNarrative80To89StateMachine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Narrative80To89Checks.cs`
- `docs/reverse-engineering/PLATFORM_NARRATIVE_STATES_80_89.md`
- merged PR `#96`

Do not reopen `$70-$89` progression unless a fixture fails or contradictory ROM evidence appears.

## EVIDENCE

Initial `$E100` audit now shows an explicit branch connected to the just-closed narrative chain:

```text
$E100  clear $0527
$E105  LDA $04
$E107  CMP #$8F
$E109  BEQ $E10E
```

Thus the `$89` output `$04=$8F` is a first-class reload selector, not incidental state.

Later in the same routine, when the `$06AB` branch reaches `$E257`, `$04=$8F` is checked again:

```text
$E25A  LDA $04
$E25C  CMP #$FF
...
$E263  CMP #$8F
$E265  BNE ...
$E267  JMP $E20E
```

The common reload commit path at `$E22C-$E254` eventually writes the selected next engine state into both `$00/$01`, derives `$03` through the `$E505` table, sets `$05=1`, resets the stack and returns to the main loop at `$C180`.

This selector has not yet been reduced into a deterministic clean-room result for the `$04=$8F` case.

## OPEN

1. Exact `$E100` logical output for the special narrative return selector `$04=$8F` is not yet promoted.
2. `$E100` is a broad reload path and also handles generic platform/stage transitions; those other selectors should not be conflated with the `$8F` case.
3. The roles of persistent fields such as `$06AB`, `$0670`, `$067D`, `$050E/$0533` in choosing the stable output state need to be mapped only as far as required by the `$8F` path.
4. Generic normal `$3D` destination selection remains a later boundary after the `$8F` case is closed.
5. Renderer/audio/stack internals remain outside logical parity unless they change destination state.

## NEXT

**Close the `$04=$8F` branch through `$E100` to its next stable engine state.**

Completion criterion:

> Starting from the exact state produced by `$89` (`$04=$8F`, `$00/$01=$3D`), identify the persistent selector inputs that materially affect `$E100`, derive every reachable logical output of the `$8F` branch through the common commit at `$E22C-$E254`, and promote that handoff without modeling unrelated reload cases.

Required sequence:

1. trace the two `$04=$8F` checks at `$E105` and `$E263` and the conditions that determine whether `$E257` is reached;
2. map only the `$8F`-relevant selector fields (`$06AB`, `$0670`, `$067D`, `$050E/$0533`, and any directly required dependencies);
3. identify the value pushed into the common `$E22C` commit path and therefore written to `$00/$01`;
4. identify the corresponding `$03` value selected through `$0533 -> $E505`;
5. implement a semantic `$8F` reload result with explicit branches where the ROM genuinely has more than one reachable output;
6. add discriminating fixtures for each reachable `$8F` outcome;
7. document scope gaps and run both verification workflows.

## BLOCKERS

- None. The canonical ROM and fixed-bank `$E100` path are available.

## ANTI-LOOP

- `last_next_signature`: `e100-mode04-8f-destination`
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
