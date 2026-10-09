# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / post-exit narrative-state closure`
- State: `READY_FOR_NEXT`
- Last verified checkpoint: PR `#94` — immediate post-platform-exit state-machine boundary.
- Merge commit: `9f0b2e67727b7da73ba39df12a165633b7660f2d`
- Exact final PR head: `d7056e904cb7b242b2bc5b176c85f54f3639393c`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#257`: `SUCCESS`
  - `Original Spec` run `#447`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Previous checkpoints: PR `#92` closed primary-family reachability; PR `#90` closed the persistent normal late-object frame.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Immediate normal `$3D` post-exit boundary

The clean logical result of a normal platform exit is now closed as:

```text
clear $04
request five-Saint snapshot
$00 = $3D
$01 = $3D
handoff -> $E100 reload
NMI mirror $01=$3D -> $E000
```

PPU disable, stack reset and sound helper internals remain implementation plumbing. Stage/narrative destination selection inside the broad `$E100` reload remains a separate boundary.

### Special substate `$11` transition `$70->$80`

The split NMI/main sequence is now promoted with state-mirror asymmetry preserved:

```text
accepted exit
 -> $70 ($57=$C0)
 -> NMI even-frame countdown
 -> $71 ($57=$80)
 -> main countdown
 -> $72 scripted X movement
 -> $73 NMI text-stream gate
 -> $74 ($57=$80)
 -> main countdown
 -> $75 main delay
 -> $80 ($57=$20)
```

Confirmed ownership:

```text
$70 : NMI
$71 : main
$72 : main
$73 : NMI text terminator
$74 : main
$75 : main
```

Transitions that modify only `$00` deliberately leave `$01` on the previous mirrored state until the next global main-thread `$00->$01` copy.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformPostExitStateMachine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/PostExitStateMachineChecks.cs`
- `docs/reverse-engineering/PLATFORM_POST_EXIT_STATE_MACHINE.md`
- merged PR `#94`

Do not reopen this boundary unless a fixture fails or contradictory ROM evidence appears.

## EVIDENCE

Direct control-flow facts already established beyond the `$80` handoff:

- NMI dispatcher routes state high nibble `$80` through bank-1 `$8C19`.
- `$8C19` decrements `$57`; at zero it reaches `$8D28`.
- For states `$80-$88`, `$8D39-$8D73` selects state-specific script pointers from table `$911C`.
- Script terminator `$FF` reaches `$8DDB`, which increments both `$00/$01`, seeds `$57=$80`, `$26=$80`, `$27=$80`.
- Therefore a completed script advances `$80->$81`, `$81->$82`, and so on.
- `$8D35` treats state `$89` and above differently: it writes `$04=$8F`, `$00/$01=$3D` and jumps to `$E100`.

These facts are not yet promoted into a clean runtime/fixture set.

## OPEN

1. NMI-driven state chain `$80-$89` after the special `$70-$75` sequence is not yet modeled.
2. Exact semantic role/content of each `$80-$88` script is not required for state parity, but script-pointer mapping and terminator progression should be documented.
3. State `$89` final transition back to `$3D/$E100` is not yet represented.
4. Generic stage/narrative destination selection inside `$E100` remains unresolved.
5. Full renderer-owned tile/Y/attribute/X state remains outside logical runtimes except exact lifecycle writes already promoted.

## NEXT

**Promote the NMI-driven `$80-$89` narrative-state chain and its final `$3D/$E100` reload handoff.**

Completion criterion:

> Starting from state `$80` with the timer seeded by the closed `$75->$80` transition, the semantic model reproduces NMI countdown/script selection, `$FF`-terminator progression through `$80-$88`, and the confirmed state `$89` conversion to `$04=$8F`, `$00/$01=$3D`, reload `$E100`, without emulating PPU text rendering.

Required sequence:

1. extract the `$911C` pointer mapping for states `$80-$88` and distinguish the special `$73` pointer;
2. verify timer/scratch seeding on every `$8DDB` terminator;
3. establish whether main-thread logic mutates `$80-$89` or only mirrors/runs generic housekeeping;
4. model text progress as a semantic terminator event, not renderer internals;
5. model state `$89` final `$3D/$E100` conversion;
6. add fixtures for `$80->$81`, an interior state, `$88->$89`, and `$89->$3D`;
7. document the boundary and run both verification workflows.

## BLOCKERS

- None. The canonical ROM and the relevant NMI routines are available.

## ANTI-LOOP

- `last_next_signature`: `state-80-89-nmi-narrative-chain`
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
