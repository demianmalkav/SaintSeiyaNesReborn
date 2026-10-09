# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / normal reload post-interactive destination`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#102` — normal warm-reload interactive state machine around `$F025 <-> $A275`.
- Merge commit: `f14581d6ea6431b380c7f23d814273ab10e25865`
- Exact final PR head: `a65528b6aa0b8ead320890d20f4c7d57d892018b`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#273`: `SUCCESS`
  - `Original Spec` run `#459`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- Recovery curation: PR `#101`, merge `c6b872bbc6ceee276e7454cead93ac989a9f866e`.
- Previous technical checkpoints: PR `#98` closed `$04=$8F -> $E100 -> $00/$00`; PR `#96` closed narrative `$80-$89`; PR `#94` closed immediate post-exit `$70->$80`; PR `#92` closed primary-family reachability; PR `#90` closed the persistent normal platform frame.
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
 -> stable $00/$00
```

Artifacts for the bounded `$8F` return:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNarrative8FReload.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/Narrative8FReloadChecks.cs`
- `docs/reverse-engineering/PLATFORM_RELOAD_MODE_8F.md`
- merged PR `#98`

Do not reopen the `$8F` branch unless a fixture fails or contradictory ROM evidence appears.

### Normal warm-reload interactive mini-state

PR #102 closes the selector around fixed `$E327-$E35D`, `$F025`, NMI-side bank-5 `$A20B` and main-side bank-5 `$A275`.

Closed invariants:

```text
entry seeds:
  $0670=$00
  $0584=$05
  $0585=$00
  $0586=$00
  $DB=$FF

case 5 = initialization/interstitial setup
case 0 = idle/unlock; writes $DB=0

$A20B accepts direction only while $DB=0
$0585 = 0 left / 2 right
$0586 = 0 up   / 1 down

confirm at $A275:
  $0584 = $0585 + $0586 + 1

therefore real user choices are exactly cases 1..4:
  left/up    -> 1 -> $DB=2
  left/down  -> 2 -> $DB=3 -> Talk $9C91
  right/up   -> 3 -> $DB=4
  right/down -> 4 -> $DB=1
```

The selector subgraph can release the `$E35A` loop with exactly:

```text
$0670 ∈ { $01, $02, $04, $DD, $FE, $FF }
```

Sources:

- `$01`: fixed `$F0A1`, Talk `$9D20`, or downstream post-action sink `$ACAA`;
- `$02`: Talk/stage-3 `$9DC5` or downstream `$ACAA`;
- `$04`: Talk stage `$0D` at `$A1CC`;
- `$DD/$FE/$FF`: downstream stage scripts through `$ACAA`.

`$0670=$03` is not reachable from this selector subgraph; its bank-5 writer belongs to another initialization/event family.

The larger Bronze/Gold round at `$F813` remains delegated to the already isolated battle dispatchers `$A361/$A381`; it is not duplicated in the warm-reload model.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformWarmReloadInteractiveState.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/WarmReloadInteractiveStateChecks.cs`
- `docs/reverse-engineering/PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`
- merged PR `#102`

Do not reopen the selector geometry or the release set unless contradictory ROM evidence or a fixture failure appears.

## EVIDENCE

### Normal platform exit entry remains character-dependent

A normal accepted platform exit does:

```text
$04=$00
snapshot Saints
$00/$01=$3D
JMP $E100
```

The established lifecycle carries `$06AB=$FF`, so `$E100` reaches the warm branch through `$E257` and then `$E26A`.

RAM `$03` is the internal active-Saint index in platform mode. `$E121-$E126` maps it through `$E505` into battle/UI index `$0533`:

```text
$03:    00 01 02 03 04
$0533:  00 02 01 03 04
```

This character dimension remains live for the post-loop destination work.

### Principal progression index is not identical to `$050E`

The table at `$F016` begins:

```text
$067D: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D
$050E: 00 01 02 03 04 05 0F 06 10 07 08 09 0C 0A
```

`$050E=$0F` and `$10` are intercepted earlier in `$E100` and assign progression before the ordinary interactive selector. They are not additional unknown selector cases.

### The interactive loop is now a closed boundary

Once `$E35A` observes one of the six proved nonzero `$0670` release values, control falls through beyond `$E35D`. No further input/menu semantics need to be reconstructed before following the final reload destination path.

## OPEN

1. The fixed-bank logic beginning immediately after the `$E35A/$E35D` loop has not yet been reduced into semantic branches for each reachable `$0670` outcome.
2. We have not yet mapped those post-loop outcomes, together with `$050E`, `$0533`, `$067D` and any directly required progression fields, into the value eventually pushed into common commit `$E22C`.
3. The corresponding final `$03` produced by `$0533 -> $E505` at `$E245-$E24B` remains to be stated for every reachable normal principal-exit outcome.
4. Principal normal exits remain the priority. Cold `$06AB=0`, `$04=$FF`, renderer/audio/stack internals and unrelated reload families stay out of scope unless they materially alter those outcomes.

## NEXT

**Close the post-interactive normal warm-reload path from `$E35F+` to the common `$E22C-$E254` commit for principal platform exits.**

Completion criterion:

> Starting from the six confirmed nonzero `$0670` values that release `$E35A`, derive every reachable final logical destination for principal normal platform exits: value committed to `$00/$01`, resulting `$03`, and any persistent progression field that materially distinguishes the outcome. Represent the transition semantically without reproducing presentation-only work.

Required sequence:

1. disassemble the fixed-bank path beginning immediately after `$E35D` through every branch that can rejoin `$E22C` or another stable engine-state handoff;
2. partition behavior by the confirmed release set `{01,02,04,DD,FE,FF}` and discard branches proved unreachable from the warm selector;
3. trace only selectors that materially affect the destination, including `$050E`, `$0533`, `$067D` and directly required dependencies;
4. identify the exact A value entering `$E22C` (or any alternative stable-state handoff) for each reachable principal outcome;
5. derive the final `$03` at `$E245-$E24B` through `$0533 -> $E505`;
6. implement the bounded semantic normal-reload result and discriminating fixtures;
7. document the complete principal-exit transition table and run both verification workflows.

## BLOCKERS

- None. The canonical ROM and the closed interactive release set provide a finite entry space for `$E35F+`.

## RECOVERY CONTRACT

A new session must be able to resume this project without chat history.

Recovery order:

1. read this file from `main`;
2. reconcile `CURRENT` with newer merged Git history if any exists;
3. inspect only the technical documents/code/tests required by `NEXT`;
4. use `docs/REVERSE_ENGINEERING_STATUS.md` only as a global navigation/maturity map;
5. use `docs/WORK_PROTOCOL.md` for execution rules;
6. when private assets are required, use the private Drive `PRIVATE_WORKSPACE_MANIFEST — Saint Seiya Reborn`; when a result depends on private traces/save states/captures, consult `04_REVERSE_ENGINEERING/EVIDENCE_INDEX`;
7. Drive never overrides this file and never owns a separate `NEXT`.

Private Drive IDs/URLs are intentionally not stored in this public repository. The manifest records them privately. Historical private trace/save-state packs were not present at curation time and must not be assumed to exist.

Recovery is healthy when a fresh session can identify from durable artifacts alone: target ROM revision, last verified technical checkpoint, closed boundaries, active boundary, one executable `NEXT`, and whether private evidence is required.

## ANTI-LOOP

- `last_next_signature`: `warm-reload-post-loop-final-destination`
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
