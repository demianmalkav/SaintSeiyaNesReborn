# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / stage $03 Cancer + platform $0C detour`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#143` — complete canonical stage `$02` Gemini / first-Camus composite.
- Merge commit: `8f41ea2afd54e00f72066f07ee493936e92412c9`
- Exact final PR head: `38d3f1abb919f8dc380e06218d6dbe1d4c6cbdea`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #359: `SUCCESS`
  - `Original Spec` #562: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Coverage matrix PR #139 remains the frozen `$050E=$00-$0B` denominator; PR #143 promotes stage `$02` to `DedicatedContextClosed`.
- Dedicated battle/event contexts now closed: Mu `$00`, Taurus `$01`, Gemini/first-Camus `$02`, Leo `$04`, Virgo `$05`, Aquarius `$08`, Pisces `$09`, Saga `$0A`; final-special bridge `$0C` is separately closed.
- Remaining material stage-context gaps: `$03/$06/$07`. Stage `$0B` remains structural/transient with no canonical stable battle.

## DONE

### Stage `$02` Gemini / first-Camus composite — PR #143

Stage `$02` is now closed as a composed lifecycle rather than an isolated boss handler set.

Canonical seed:

```text
$067D = $02
$050E = $02
$E50B[$02] = $00
$06CD = $00
$0673 = $30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
Ikki excluded
```

Initializer `$981F`:

```text
temporary presentation $050E=$11
#$03 -> $F31E -> +300 Seventh Sense
shared $9C3D -> $0670=$03 / $068E=1 / restore $050E=$02
```

Talk `$9D81`:

```text
every Talk -> $DC++ -> forced Gold response
first Talk -> also $066F:0->1
repeated Talk -> preserves nonzero $066F
```

The first Bronze action is intercepted by `$A444` while `$067C=0` **before** generic opponent classification:

```text
$02 = $0E
$0670 = $02
```

Therefore phase zero cannot directly win even if hypothetical Bronze damage would otherwise produce `$EB=$FF`.

The already-closed special-normal platform `$0E` pipeline is reused:

```text
accepted gate: X >= $B4 / Y == $80 / jump phase 0
normal $3D/$E100 reload
release $02 special resume -> $067C:0->1
no $A973 reset
```

Because `$A973` is skipped, pre-detour `$066F` survives ordinary resume.

Resume split:

```text
Seiya/Shun/Shiryu
  -> $050E remains $02
  -> $067C=1
  -> ordinary stage-2 continuation

Hyoga
  -> $050E=$08
  -> $06B8=$0A
  -> $0690=$FF
  -> $067D remains $02
  -> compose existing redirected first-Camus Aquarius context
```

Once `$067C!=0`, ordinary `$A444` owns:

```text
$EB=$FF -> release $01 victory
$EB=$01 -> low-opponent feedback
$EB=$00 + hit -> continue
$EB=$00 + no hit -> miss feedback
```

Post-Gold `$A4CC`:

```text
$EA=$00 -> healthy feedback
$EA=$01 -> low feedback
$EA=$FF -> release $FF defeat
```

Gold selection remains generic parity slots `0/1`.

Generic stage-2 `$FF` does not advance story. On the next normal re-entry fixed `$ED57` calls `$A973`, clearing `$067C/$066F/$0670/$068E/$06B8`; therefore defeat **rearms the mandatory `$0E` detour**.

Both canonical success families converge on Cancer:

```text
ordinary stage-2 victory release $01
  -> $067D:02->03
  -> $050E=$03
  -> $06CD=$02
  -> $0673=$32
  -> active Saint preserved (Seiya/Shun/Shiryu)

Hyoga redirected first Camus
  -> scripted release $FE after three-Talk Bronze route OR actual Hyoga defeat
  -> existing $FE owner forces Seiya
  -> $067D:02->03
  -> $050E=$03
  -> $06CD=$02
  -> $0673=$32
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/GeminiStage02Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/GeminiStage02ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md`
- promoted `BattleStageContextCoverage` / coverage fixtures
- updated `BATTLE_STAGE_CONTEXT_COVERAGE.md`
- updated `BATTLE_EVENT_DISPATCH.md`
- PR #143

Do not reopen stage `$02`, platform `$0E`, or redirected first Camus without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Stage `$03` Cancer / Death Mask is the first remaining material gap

Canonical story entry after stage `$02` completion is:

```text
$067D = $03
$050E = $03
$E50B[$03] = $02
$06CD = $02
$0673 = $32
```

Fixed Saint-selection semantics therefore permit exactly:

```text
Seiya  bit $01 -> reachable
Hyoga  bit $02 -> blocked
Shun   bit $04 -> reachable
Shiryu bit $08 -> reachable
Ikki   bit $10 -> blocked
```

The stage-indexed owners are already fixed by the coverage audit:

```text
init          $9851
Talk          $9D96
post-Bronze   $A50F
post-Gold     $A560
Gold slots    0,1
```

#### Initialization `$9851`

Static ROM flow proves:

```text
temporary presentation index $0E through $F2ED
scripted presentations
#$04 -> $F31E -> +400 Seventh Sense
JMP $9C3D
```

Shared `$9C3D` supplies the same intro-handoff shape used elsewhere:

```text
$0670=$03
$068E=1
restore real $050E=$03
```

#### Talk `$9D96`: phase-zero platform detour

Talk first checks `$067C`.

With canonical `$067C=0` it executes the long scripted branch and then:

```text
$02 = $0C
$0670 = $02
PLA
PLA
RTS
```

The double-`PLA` is a caller unwind: the phase-zero Talk leaves the ordinary Talk command path immediately after creating the platform transition.

The already-closed `PlatformSpecialNormalExitPipeline` owns substate `$0C`:

```text
provenance = stage-3 Talk
progress $067D=$03
reload field $050E=$03
inherited release $02
accepted gate: X >= $88 / Y == $20 / jump phase 0
```

Accepted `$3D/$E100` reload reaches fixed `$ED57/$ED8F`, increments:

```text
$067C:0->1
```

and, because release is `$02`, skips common reset `$A973`. Unlike stage `$02`, there is no Hyoga character redirect here; the selected reachable Saint returns to stage `$03`.

With `$067C!=0`, Talk `$9D96` instead emits the phase-1 dialogue (`$58/$57`) and increments transient `$DC`, so the caller forces a Gold response.

#### Post-Bronze `$A50F`

The handler immediately uses the existing opponent classifier `$ACD6`.

```text
$EB=$FF -> stage victory presentation -> release $01
$EB=$01 -> INC $064A -> low-opponent feedback
$EB=$00 + hit -> continue
$EB=$00 + no hit -> miss feedback
```

The `$064A` increment is stage-local and occurs on each `$EB=$01` execution; do not silently turn it into a one-shot latch unless additional evidence proves a bounded caller state.

#### Post-Gold `$A560`

After existing player classifier `$AD4D`:

```text
$EA=$FF -> release $FF defeat
$EA=$01 and $064D==0 -> first-low presentation -> INC $064D -> return
otherwise -> common healthy/repeat feedback -> return
```

Thus `$064D` is a one-time first-low latch for the special low-player presentation.

Generic parity Gold selection exposes only slots `0/1`.

#### Retry and successor contracts

A generic `$FF` defeat leaves story progress at `$03`; on subsequent normal stage re-entry `$ED57` calls `$A973`, which clears `$067C`. Therefore retry returns to phase zero and the **first later Talk can create platform `$0C` again**. Cancer's detour is Talk-triggered, not a mandatory first-Bronze detour.

Ordinary victory release `$01` joins fixed `$E399/$E3B3`:

```text
$067D:03->04
$F016[$04]=$04
$E50B[$04]=$02
$050E=$04
$06CD=$02
$0673=$32
```

The reachable Cancer roster excludes Ikki, so ordinary release `$01` preserves the active Seiya/Shun/Shiryu into the exact Leo `$04` boundary. Leo internals are already closed by PR #125 and must not be reopened.

## OPEN

1. Model the exact canonical Cancer entry seed and reachable roster from `$067D=$03/$050E=$03/$0673=$32`.
2. Model init `$9851`: temporary `$0E` presentation, +400 Seventh Sense, `$0670=$03/$068E=1`, and command-loop handoff.
3. Close Talk `$9D96` in both phases: phase-zero `$02=$0C/release $02` with caller unwind, and phase-one `$DC++` forced Gold response.
4. Compose the already-closed platform `$0C` gate/reload and special resume `$067C:0->1`; do not duplicate platform mechanics.
5. Prove what stage-local Talk/presentation state survives release `$02` special resume and what is reset only on generic retry.
6. Close post-Bronze `$A50F`, including repeated `$064A++` low-opponent behavior, miss feedback and victory `$01`, using existing classifier/damage arithmetic.
7. Close post-Gold `$A560`, including first-low `$064D` latch, repeat/healthy feedback and defeat `$FF`.
8. Prove `$FF` retry semantics and phase-zero re-entry, including renewed eligibility for the Talk-created `$0C` detour.
9. Compose victory `$01` to exact Leo boundary `$067D=$04/$050E=$04/$06CD=$02/$0673=$32`, preserving the reachable active Saint.
10. Implement dedicated `CancerStage03Context`, discriminating fixtures and one focused context document; stop at Leo boundary.

## NEXT

**Close canonical stage `$03` Cancer / Death Mask from story seed `$067D=$03/$050E=$03/$06CD=$02/$0673=$32` through initializer `$9851`, both phases of Talk `$9D96`, the phase-zero Talk-created platform `$02=$0C / release $02` detour and accepted `$0C` special resume `$067C:0->1`, post-Bronze `$A50F`, post-Gold `$A560`, Gold slots `0/1`, generic `$FF` retry semantics, and ordinary victory `$01` to the exact already-closed Leo boundary `$067D=$04/$050E=$04/$06CD=$02/$0673=$32`.**

Completion criterion:

> Starting from the exact progress-`$03` roster/state, produce an executable composed Cancer model that proves the +400 intro handoff, phase-zero Talk platform transition and caller unwind, phase-one Talk counterattack trigger, `$064A`/`$064D` stage-local behavior, all ordinary post-Bronze/post-Gold terminals, retry phase reset, and exact release-`$01` handoff to Leo. Reuse generic battle arithmetic, fixed release/story ownership and the already-closed platform `$0C` pipeline; do not reopen Leo.

## BLOCKERS

- None. Canonical ROM, stage-3 handlers, generic battle primitives, platform `$0C` pipeline, fixed release/story ownership and closed Leo successor are available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged Git history if present.
2. Treat PR #143 / `BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md`, PR #141 / Mu and PR #139 / coverage matrix as frozen.
3. Start stage `$03` only from `$9851/$9D96/$A50F/$A560` and canonical progress `$067D=$03`.
4. Reuse `PlatformSpecialNormalExitPipeline` substate `$0C`; do not re-reverse the gate/reload machinery.
5. Reuse generic damage/resources/dodge/Gold parity slots and fixed release/story ownership.
6. Keep Leo `$04` as the terminal successor boundary; PR #125 already owns its internals.
7. Drive remains private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `stage-03-cancer-platform-0c-detour`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
