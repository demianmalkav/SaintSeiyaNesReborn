# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / global engine state dispatcher`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#107` — special normal platform exits `$02=$0C-$10` closed through the existing exit/reload pipeline.
- Merge commit: `e89ecb64b429a9a0b4b035e47b8a90c77fe7047f`
- Exact final PR head: `63c441ccf5b0b890074d8b44eeedaab6e0d959b2`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#284`: `SUCCESS`
  - `Original Spec` run `#476`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#104` closed principal normal warm-reload destinations.
- PR `#102` closed the interactive `$F025 <-> $A275` warm-reload selector.
- Recovery curation: PR `#101`, merge `c6b872bbc6ceee276e7454cead93ac989a9f866e`.
- Previous platform-exit checkpoints: PR `#98` closed `$04=$8F -> $E100 -> $00/$00`; PR `#96` closed narrative `$80-$89`; PR `#94` closed immediate post-exit `$70->$80`.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Platform exit/reload boundary `$02=$00-$11`

The complete platform-exit family is now partitioned and promoted.

#### Principal normal exits `$00-$0B`

PRs #102/#104 close the warm-reload selector and every principal stable destination:

```text
normal accepted platform exit
 -> $04=$00
 -> snapshot Saints
 -> $00/$01=$3D
 -> $E100
 -> warm selector $F025 <-> $A275
 -> release $0670
 -> common destination logic
```

Principal selector-wide release set:

```text
$0670 ∈ { $01, $02, $04, $DD, $FE, $FF }
```

Principal stable engine-state set:

```text
{ $00, $10, $90 }
```

plus explicit stage-$0A Saga selector reentry on `$FF + $06CE!=0`.

Key artifacts:

- `PlatformWarmReloadInteractiveState.cs`
- `PlatformNormalWarmReloadDestination.cs`
- `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`
- `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`
- PRs `#102`, `#104`

#### Special-normal exits `$0C-$10`

PR #107 proves that these states reuse the common `$96CB` normal exit gate but carry different persistent reload inputs.

Closed results:

```text
$0C:
  provenance = stage-3 Talk $9D96+
  inherited $0670=$02, creation $067C=0
  $ED57 increments $067C -> 1
  reenter selector at progression/stage $03

$0D:
  provenance = stage-5 $A361 special action $A6A8+
  inherited $0670=$02, creation $067C=0
  $ED57 increments $067C -> 1
  reenter selector at progression/stage $05

$0E:
  provenance = stage-2 $A361 special action $A444+
  inherited $0670=$02, creation $067C=0
  $ED57 increments $067C -> 1
  ordinary Saint -> selector stage $02
  canonical Hyoga -> temporary $050E=$08, $06B8=$0A

$0F:
  provenance = $E4D7 progression map at new $067D=$0D
  inherited $0670=$05
  $ED57 -> $A973 reset
  reenter clean selector at Saga stage $0A

$10:
  provenance = $E4D7 progression map at new $067D=$0C
  inherited $0670=$01
  $E26A intercepts before $ED57
  no selector reentry
  accepted exit is Seiya-only
  direct stable commit -> engine state $00
```

For accepted `$10`/Seiya exit, the stable result is:

```text
$00/$01=$00
$03=$00
$0670=$05
$067D=$0C
$0533=$00
$050E=$0C
$0673=$3E
$06CC=$01
$06CD=$0E
$068F=$00
$05=$01
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformSpecialNormalExitPipeline.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/SpecialNormalPlatformExitChecks.cs`
- `docs/reverse-engineering/PLATFORM_SPECIAL_NORMAL_EXITS.md`
- PR `#107`

#### Special narrative exit `$11`

Already closed by PRs #94/#96/#98:

```text
$11 special platform exit
 -> $70 -> $71 -> $72 -> $73 -> $74 -> $75
 -> $80 -> $81 -> ... -> $89
 -> $04=$8F, $00/$01=$3D
 -> $E100
 -> stable $00/$00
```

Therefore the platform-exit/reload boundary `$02=$00-$11` is closed at the semantic level required by ORIGINAL SPEC. Do not reopen it absent contradictory ROM evidence or fixture failure.

### Persistent normal platform frame

The persistent platform frame, primary/auxiliary entities, hazards, player interaction ordering and late-object frame are already promoted through the earlier platform checkpoints. This remains a closed dependency for the global dispatcher investigation.

## EVIDENCE

### Canonical normal gate

Accepted normal platform exits use bank-1 `$96CB`:

```text
$04=$00
JSR $951F       ; snapshot all five Saints
$00=$3D
$01=$3D
...
JMP $E100
```

Substate `$11` instead takes the already-promoted `$70` special branch.

### Special-normal provenance matters

The special-normal distinction is not a different physical gate. It is the state carried into `$E100`:

- `$0C/$0D/$0E` retain `$0670=$02`; `$ED57` increments one-shot phase `$067C` before selector reentry.
- `$0E` additionally remaps canonical Hyoga to temporary `$050E=$08/$06B8=$0A`.
- `$0F` retains `$0670=$05`; `$A973` clears warm phase state and reopens stage `$0A`.
- `$10` retains `$0670=$01`; `$E26A` consumes it before `$ED57` and commits state `$00` directly.

### Late progression map

Fixed `$E4D7` maps progression into platform substate:

```text
new $067D=$0C -> $02=$10
new $067D=$0D -> $02=$0F
new $067D=$0E -> $02=$11
```

PR #104 already proved the `$0B->$0C` progression choice can create only Seiya or Shun; the `$10` physical exit gate rejects Shun, so the accepted `$10` exit is Seiya-only.

### Why the next boundary moves upward

Platform movement/combat/frame semantics and platform exit/reload semantics are now substantially promoted. The remaining uncertainty immediately above them is no longer a platform-local branch but the fixed-bank global engine dispatcher that coordinates `$00/$01` between main loop and NMI.

Known anchors already established by previous checkpoints include:

- main loop mirrors `$00 -> $01` before dispatch;
- `$00=$3D` dispatches into `$E100` reload;
- NMI reads mirrored `$01` and has corresponding special handling;
- platform active/init states, `$70-$89`, `$3D`, and stable reload destinations `$00/$10/$90` are now known locally.

What is missing is one authoritative top-level map showing how those known families coexist with the remaining engine states and which reachable family is the next genuinely unmodeled transition.

## OPEN

1. There is no promoted semantic map of the complete fixed-bank main `$00` dispatcher and NMI `$01` companion dispatcher.
2. We have local models for several state families, but no single partition proving which engine-state values/ranges are reachable, which are aliases/transitional states, and which are still unresolved.
3. Writers that introduce unresolved engine-state families have not been globally indexed.
4. Renderer/PPU/audio internals must remain outside scope unless they change logical `$00/$01` transitions.
5. RNG, global boss progression, renderer/metasprites and audio remain later global fronts; they should not be selected before the state dispatcher tells us which unresolved engine family is actually next in control flow.

## NEXT

**Build the top-level `$00/$01` engine-state dispatcher map and select the first unresolved reachable state family.**

Completion criterion:

> Enumerate the logical state families dispatched by the fixed-bank main loop and NMI, reconcile them with all already-promoted platform/reload/narrative states, identify every writer that can enter an unresolved family, and reduce the result to one concrete next state-family boundary without modeling unrelated renderer/audio bodies.

Required sequence:

1. disassemble the fixed-bank main dispatcher around `$C180-$C2xx` and NMI dispatcher around `$D269-$D3xx` only far enough to enumerate state comparisons/ranges and branch targets;
2. record the `$00->$01` mirror semantics and identify which transitions are main-owned, NMI-owned or cooperative;
3. classify every discovered state/range as:
   - already promoted and represented by existing ORIGINAL SPEC artifacts;
   - transitional/alias with no independent semantic body;
   - reachable but unresolved;
   - statically present but reachability not yet proven;
4. search writers of `$00/$01` for each unresolved family and determine which unresolved family is reachable directly from an already-closed checkpoint;
5. add a dispatcher/state-family document and a minimal executable semantic map/fixtures if the partition can be represented without emulating subsystem internals;
6. set the next checkpoint to exactly one unresolved reachable family, not to the whole global engine.

## BLOCKERS

- None. The canonical ROM, fixed-bank dispatchers and existing local state-machine artifacts are available.

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

Historical private trace/save-state packs were not present at curation time and must not be assumed to exist.

## ANTI-LOOP

- `last_next_signature`: `global-engine-state-dispatcher-map`
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
