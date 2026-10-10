# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / stage $02 Gemini + first-Camus detour`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#141` — complete canonical stage `$00` Mu / pre-battle repair context.
- Merge commit: `33ac4e3b3ec249b37424f4def9344e96a525654f`
- Exact final PR head: `fe4d5f92cb90aff67a43618c17b5b56a92e9b2de`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #353: `SUCCESS`
  - `Original Spec` #554: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Coverage matrix PR #139 remains the frozen `$050E=$00-$0B` denominator.
- Dedicated battle/event contexts now closed: Mu `$00`, Taurus `$01`, Leo `$04`, Virgo `$05`, Aquarius `$08`, Pisces `$09`, Saga `$0A`; final-special bridge `$0C` is separately closed.
- Remaining material stage-context gaps: `$02/$03/$06/$07`. Stage `$0B` remains structural/transient with no canonical stable battle.

## DONE

### Stage `$00` Mu / pre-battle repair — PR #141

Canonical seed and roster are now executable rather than inferred:

```text
global $AD4A-$AD54 clear -> $06BB=0
new-game $A100+          -> $0673=$30 / initial Seiya
common reset $A973+      -> $066F=0 / $0670=0, preserves $06BB
story progress $067D=00  -> $F016[00]=$00
```

`$0673=$30` permits Seiya/Hyoga/Shun/Shiryu and masks Ikki. Stage init `$97F7` is `RTS`.

Fixed command ownership proves stage `$00` is not an ordinary Gold battle:

```text
Resource Allocation  $F041 -> $F238
Attack               $F057 -> $F238
Talk                 $F0B1 -> $9C95 -> $9CB7
Escape               $F0D3 -> $F238
```

Blocked owner `$F238` owns a real local latch/counter:

```text
$06BB=0   -> first blocked presentation -> INC $06BB
$06BB!=0  -> extra selector $48         -> INC $06BB
all       -> shared selector $39 -> command loop
```

The canonical Talk machine is:

```text
first Talk ($066F=0)
  common selectors $32/$33
  Saint table $9D24 = 35 35 36 34
  $066F = 1

second/repeated Talk ($066F!=0)
  common selectors $37/$38
  Saint table $9D28 = 3B 3B 11 3B
  $0670 = $01
```

Active Saint changes presentation text only. The first-Talk helper `$AEDA` is presentation-only for this context.

Release `$01` composes through fixed `$E399/$E3B3` to the exact existing Taurus boundary:

```text
$067D: 00 -> 01
$050E = 01
$06CD = 00
$0673 = 30       ; before next battle-entry Saint bit commit
active Saint preserved (0..3)
```

No canonical stage-`$00` command reaches Bronze technique/damage, post-Bronze `$A361`, Gold selection/dodge/damage or post-Gold `$A381`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/MuStage00Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/MuStage00ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_00_MU.md`
- promoted `BattleStageContextCoverage` / coverage fixtures
- updated `BATTLE_STAGE_CONTEXT_COVERAGE.md`
- updated `BATTLE_EVENT_DISPATCH.md`
- PR #141

Do not reopen stage `$00` without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Stage `$02` is a composite Gemini / first-Camus context

Canonical story entry is:

```text
$067D = $02
$050E = $02
```

The coverage audit already fixes its dispatcher owners:

```text
init          $981F
Talk          $9D81
post-Bronze   $A444
post-Gold     $A4CC
Gold slots    0,1
```

#### Initialization `$981F`

The stage-specific initializer performs presentation setup, temporarily loads presentation index `$11`, grants `#$03` through fixed `$F31E` (**+300 Seventh Sense**), then joins shared `$9C3D`:

```text
$0670 = $03
$068E = 1
$050E restored to $02
```

This is an intro handoff, not a victory terminal.

#### Talk `$9D81`

Every Talk increments transient `$DC`, so its caller forces a Gold response. On the first Talk only:

```text
$066F: 0 -> 1
```

and an additional presentation branch is emitted. Repeated Talk leaves `$066F` nonzero but still raises `$DC`.

#### Mandatory first post-Bronze detour `$A444`

Before ordinary opponent classification, `$A444` checks `$067C`.

With canonical initial `$067C=0`:

```text
$02   = $0E
$0670 = $02
```

and control leaves battle for the already-closed special-normal platform substate `$0E`.

Existing `PLATFORM_SPECIAL_NORMAL_EXITS.md` proves the accepted `$0E` exit gate:

```text
X >= $B4
Y == $80
jump phase == 0
normal $3D / $E100 reload
```

The special resume at fixed `$ED57+` increments:

```text
$067C: 0 -> 1
```

without calling ordinary reset `$A973`.

For an ordinary active Saint, selector re-entry retains stage `$02`. For active Hyoga (`$0533=1`) the same already-closed owner instead writes:

```text
$050E = $08
$06B8 = $0A
$0690 = $FF
$067D remains $02
```

This is the redirected **first Camus** branch already represented by `AquariusStage08Context.EnterRedirectedFirstCamus`; it must be composed, not re-reversed.

#### Stage-2 ordinary post-action branch

Once `$067C!=0`, `$A444` uses the generic opponent classifier and owns stage-local feedback/victory. Opponent defeat emits release `$01`; low/miss/hit branches remain nonterminal.

Post-Gold `$A4CC` consumes the already-computed player condition:

- `$EA=$00`: healthy-player presentation and continue;
- `$EA=$01`: low-player feedback and continue;
- `$EA=$FF`: release `$FF` defeat.

Generic parity selector exposes only Gold slots `0/1`.

The stage `$02` checkpoint must therefore close the **composed lifecycle**, including the platform `$0E` phase transition and the Hyoga redirect, rather than modeling `$A444` in isolation.

## OPEN

1. Prove the exact canonical stage-`$02` story/roster seed, active-Saint reachability and fields carried into `$981F`.
2. Model `$981F` initialization: temporary presentation `$11`, +300 Seventh Sense, `$0670=$03`, `$068E=1`, and return to stage `$02`.
3. Model Talk `$9D81`, including unconditional `$DC++`, first-use `$066F++`, and the caller-owned forced Gold response.
4. Close `$A444` with the `$067C=0` mandatory post-Bronze detour to `$02=$0E / release $02`, before any opponent classification.
5. Compose the already-closed `$0E` platform exit and fixed special resume `$067C:0->1` without duplicating platform mechanics.
6. Split the resume correctly: ordinary Saint -> stage `$02`; Hyoga -> `$050E=$08/$06B8=$0A/$0690=$FF`, composed with the already-closed first-Camus phase in `AquariusStage08Context`.
7. Close stage-2 `$067C!=0` post-Bronze feedback/victory `$01`, Gold slots `0/1`, and post-Gold healthy/low/defeat `$FF` branches using existing generic arithmetic.
8. Prove defeat/retry ownership, especially whether `$FF` re-entry resets `$067C` and therefore makes the `$0E` detour repeat.
9. Compose every canonical victory path to the exact progress `$067D=$03 / stage $03` Cancer boundary, including the redirected first-Camus completion where applicable.
10. Implement a dedicated executable stage-`$02` context/compositor, discriminating fixtures and one focused context document. Stop at the already-identified Cancer boundary.

## NEXT

**Close canonical stage `$02` Gemini / first-Camus composite from story seed `$067D=$02/$050E=$02` through initializer `$981F`, Talk `$9D81`, the mandatory first post-Bronze `$067C=0 -> $02=$0E / release $02` platform detour, accepted `$0E` exit and `$067C:0->1` special resume, the ordinary-Saint stage-`$02` continuation versus Hyoga's redirected `$050E=$08/$06B8=$0A/$0690=$FF` first-Camus branch, and all reachable stage-2 victory/defeat terminals through the exact `$067D=$03/$050E=$03` Cancer successor.**

Completion criterion:

> Starting from the exact progress-`$02` entry, produce an executable composed model that proves the intro reward/handoff, Talk behavior, unavoidable first post-Bronze platform detour, phase increment on `$0E` exit, Hyoga-specific first-Camus redirection using the existing Aquarius model, ordinary stage-2 post-action/Gold behavior, retry semantics, and every canonical terminal through its fixed release owner. Generic damage/dodge/resource arithmetic and the already-closed platform/Aquarius internals must be reused rather than reimplemented.

## BLOCKERS

- None. Canonical ROM, stage-2 handlers, generic battle primitives, platform `$0E` pipeline, first-Camus Aquarius context and fixed release/story owners are all available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged Git history if present.
2. Treat PR #141 / `BOSS_CONTEXT_STAGE_00_MU.md` and PR #139 / `BATTLE_STAGE_CONTEXT_COVERAGE.md` as frozen.
3. Start stage `$02` from handlers `$981F/$9D81/$A444/$A4CC` and canonical progress `$067D=$02`.
4. Reuse `PLATFORM_SPECIAL_NORMAL_EXITS.md` for substate `$0E`; do not re-reverse its gate/reload machinery.
5. Reuse `AquariusStage08Context.EnterRedirectedFirstCamus` and the closed first-Camus phase for the Hyoga branch; do not duplicate stage `$08` logic.
6. Reuse generic damage/resources/dodge/Gold parity slots and fixed release/story ownership.
7. Keep stage `$03` Cancer as the successor boundary; do not begin Cancer internals in the same checkpoint.
8. Drive remains private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `stage-02-gemini-first-camus-detour`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
