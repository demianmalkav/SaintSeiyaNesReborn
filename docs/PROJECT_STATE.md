# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

Technical subsystem documents remain authoritative for evidence and semantics. This file records the accepted checkpoint, open boundaries and one executable `NEXT`.

## CURRENT

- Phase: `ORIGINAL SPEC / special normal platform exits`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#104` — principal normal warm-reload final destinations through common `$E22C-$E254` commit.
- Merge commit: `b58258f9f103ca22f7588b29b82cf230916bea40`
- Exact final PR head: `6c542baafeb4f47729678b52349e4627a7c85097`
- Verification gate on that exact head:
  - `ORIGINAL SPEC tests` run `#278`: `SUCCESS`
  - `Original Spec` run `#467`: `SUCCESS`
  - build, OriginalSpec self-test and password compatibility fixture: `SUCCESS`
- PR `#102` previously closed the interactive `$F025 <-> $A275` warm-reload selector.
- Recovery curation: PR `#101`, merge `c6b872bbc6ceee276e7454cead93ac989a9f866e`.
- Previous technical checkpoints: PR `#98` closed `$04=$8F -> $E100 -> $00/$00`; PR `#96` closed narrative `$80-$89`; PR `#94` closed immediate post-exit `$70->$80`; PR `#92` closed primary-family reachability; PR `#90` closed the persistent normal platform frame.
- Workflow hardening checkpoint: PR `#74` remains authoritative for continuation/anti-loop semantics.

## DONE

### Special `$11` platform/narrative chain

Closed sequence:

```text
special platform exit
 -> $70 -> $71 -> $72 -> $73 -> $74 -> $75
 -> $80 -> $81 -> $82 -> $83 -> $84 -> $85 -> $86 -> $87 -> $88 -> $89
 -> $04=$8F, $00/$01=$3D
 -> $E100
 -> stable $00/$00
```

Artifacts:

- `PlatformPostExitStateMachine.cs`
- `PlatformNarrative80To89StateMachine.cs`
- `PlatformNarrative8FReload.cs`
- corresponding self-tests and reverse-engineering documents
- PRs `#94`, `#96`, `#98`

Do not reopen this chain absent contradictory ROM evidence or fixture failure.

### Normal warm-reload interactive selector

PR #102 closes fixed `$E327-$E35D`, `$F025`, bank-5 `$A20B/$A275`.

Exact semantic selector:

```text
entry:
  $0670=$00
  $0584=$05
  $0585=$00
  $0586=$00
  $DB=$FF

case 5 = initialization
case 0 = idle/unlock -> $DB=0

$A20B accepts direction only while $DB=0
$0585 = 0 left / 2 right
$0586 = 0 up   / 1 down

confirm $A275:
  $0584 = $0585 + $0586 + 1

real choices:
  left/up    -> case 1 -> $DB=2
  left/down  -> case 2 -> $DB=3 -> Talk $9C91
  right/up   -> case 3 -> $DB=4
  right/down -> case 4 -> $DB=1
```

Selector-wide loop-release set:

```text
$0670 ∈ { $01, $02, $04, $DD, $FE, $FF }
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformWarmReloadInteractiveState.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/WarmReloadInteractiveStateChecks.cs`
- `docs/reverse-engineering/PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`
- PR `#102`

### Principal normal warm-reload destination

PR #104 closes the post-interactive principal path from `$E35F+` through `$E22C-$E254`.

Principal interactive releases are:

```text
{ $01, $02, $DD, $FE, $FF }
```

`$04` is excluded from **principal** progression because its interactive writer is Talk context `$050E=$0D`, while the principal `$F016` map contains no `$0D` entry.

Closed stable-result rules:

```text
$01:
  advance $067D
  rebuild $06CD/$0673, clear $06CC
  ordinary new indices -> state $10
  new $067D=$0D/$0E -> $0670=$05, state $00

$FE:
  force canonical $0533=0
  normalize to progression release $01
  then same advance rule

$02:
  no progression advance
  state $00

$DD:
  $0673=$3F
  $068F=$DD
  state $90

$FF with $06CE!=0:
  stage-$0A Saga phase only
  no stable commit
  return to $E327 and reseed interactive selector

$FF with $06CE=0:
  if (($0673 | $06CC) & $0F) == $0F -> $068F=$DD, state $90
  otherwise -> refresh $06CC Saint mask, state $00
```

The complete stable engine-state set for principal normal warm reload is:

```text
{ $00, $10, $90 }
```

plus explicit Saga phase reentry.

At every stable common commit:

```text
$00/$01 = committed engine state
$03 = $E505[$0533]
```

Canonical-to-internal Saint mapping remains:

```text
$0533: 0 1 2 3 4
$03:   0 2 1 3 4
```

Persistent progression details closed by #104:

- `$06CD` is preserved on non-advancing `$02/$DD/$FF` branches;
- `$01/$FE` recompute it only after `$067D` advances;
- new `$067D=$0C` is special and can produce `$06CD=$0E`/canonical Saint 0 or `$06CD=$0B`/canonical Saint 2 from old `$0673` bit 0;
- ordinary `$FF` maps `$050E=$0F` to `$0D` when canonical `$0533!=3`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/Platform/PlatformNormalWarmReloadDestination.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/NormalWarmReloadDestinationChecks.cs`
- `docs/reverse-engineering/PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`
- PR `#104`

The principal normal platform reload chain is now closed end-to-end. Do not reopen it without contradictory evidence or failing fixtures.

## EVIDENCE

### Normal platform exit entry

A normal accepted platform exit does:

```text
$04=$00
snapshot Saints
$00/$01=$3D
JMP $E100
```

The established lifecycle carries `$06AB=$FF`, so warm reload reaches `$E257/$E26A`.

RAM `$03` is internal active-Saint index; `$E121-$E126` maps it through `$E505` into canonical/battle index `$0533`:

```text
$03:    00 01 02 03 04
$0533:  00 02 01 03 04
```

### Principal progression map

Confirmed `$F016` mapping:

```text
$067D: 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E
$050E: 00 01 02 03 04 05 0F 06 10 07 08 09 0C 0A 00
```

The `$0F/$10` entries are intercepted earlier in `$E100`; `$0D` is absent from the principal map and belongs to a special interaction context.

### Remaining nearby boundary

The platform exit gate still has ordinary/special-normal substates beyond the principal `$02=$00-$0B` family. Earlier state discovery intentionally deferred normal exits `$02=$0C-$10` until the common interactive and destination machinery was understood. That prerequisite is now satisfied.

Substate `$11` is **not** part of this open boundary; it is the already closed special `$70->$89` chain.

## OPEN

1. Normal platform exits with platform substate `$02=$0C-$10` have not yet been enumerated end-to-end against the now-closed reload machinery.
2. For each of `$0C-$10`, we need to determine whether the exit is reachable, whether it uses the generic `$04=$00/$3D->$E100` handoff unchanged, and which already-modeled selector/destination branch it reaches.
3. Any special persistent writes performed before the common exit gate must be identified only if they materially alter the reload result.
4. Cold `$06AB=0`, `$04=$FF`, renderer/audio/stack internals and unrelated global reload families remain outside scope unless one of these special normal exits proves dependent on them.

## NEXT

**Close normal platform exits `$02=$0C-$10` through the existing exit/reload pipeline.**

Completion criterion:

> For each platform substate `$0C`, `$0D`, `$0E`, `$0F` and `$10`, prove reachability and exact exit semantics, then map every reachable normal exit to an already-closed reload outcome or promote the smallest genuinely new branch. Substate `$11` must remain excluded as the previously closed special narrative path.

Required sequence:

1. trace the platform dispatcher/gates for `$02=$0C-$10` and identify the exact condition under which each can reach the accepted normal exit;
2. record any pre-exit writes that survive into `$E100` and can alter `$03/$0533`, `$067D/$050E`, `$0670`, `$06CE`, `$0673/$06CC` or the exit mode `$04`;
3. prove which substates reuse the generic normal snapshot + `$04=$00`, `$00/$01=$3D`, `JMP $E100` path;
4. route those states through the verified #102/#104 selector and destination models rather than duplicating them;
5. implement only special-normal semantics not already represented by existing models;
6. add discriminating fixtures for every reachable `$0C-$10` exit and explicit unreachable cases;
7. document the complete special-normal exit table and run both verification workflows.

## BLOCKERS

- None. The common normal reload machinery is now closed and the canonical ROM is available.

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

- `last_next_signature`: `platform-special-normal-exits-0c-10`
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
