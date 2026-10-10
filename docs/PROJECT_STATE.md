# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / stage $06 Scorpio + progress-$08 stage-$10 platform bridge`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#145` — complete canonical stage `$03` Cancer / Death Mask context.
- Merge commit: `079e97cab384c53ef5fddf69f2e1c8eb6f065703`
- Exact final PR head: `4c686f90ca476a47ca2e7d2134f72a52609a9276`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #368: `SUCCESS`
  - `Original Spec` #573: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Coverage matrix PR #139 remains the frozen `$050E=$00-$0B` denominator; PR #145 promotes stage `$03` to `DedicatedContextClosed`.
- Dedicated battle/event contexts now closed: Mu `$00`, Taurus `$01`, Gemini/first-Camus `$02`, Cancer `$03`, Leo `$04`, Virgo `$05`, Aquarius `$08`, Pisces `$09`, Saga `$0A`; final-special bridge `$0C` is separately closed.
- Remaining material stage-context gaps: `$06/$07`. Stage `$0B` remains structural/transient with no canonical stable battle.

## DONE

### Stage `$03` Cancer / Death Mask — PR #145

Cancer is now closed as a dedicated executable context composed with the already-closed platform `$0C` machinery.

Canonical seed:

```text
$067D = $03
$050E = $03
$E50B[$03] = $02
$06CD = $02
$0673 = $32
reachable Saints = Seiya / Shun / Shiryu
Hyoga + Ikki blocked
```

Initializer `$9851`:

```text
temporary presentation $050E=$0E
#$04 -> $F31E -> +400 Seventh Sense
shared $9C3D -> $0670=$03 / $068E=1 / restore $050E=$03
```

Talk `$9D96` is phase-controlled:

```text
$067C=0
  -> $02=$0C
  -> $0670=$02
  -> PLA / PLA / RTS
  -> caller unwind; no forced Gold response

$067C!=0
  -> messages $58/$57
  -> INC $DC
  -> caller forces Gold response
```

The already-closed platform `$0C` exit is reused:

```text
accepted gate: X >= $88 / Y == $20 / jump phase 0
normal $3D/$E100 reload
release $02 special resume -> $067C:0->1
no $A973 reset
```

Because `$A973` is skipped, selected Saint and stage-local `$064D/$068E` survive the special resume.

Critical reachability result: the Talk/platform detour is **optional**, not a victory prerequisite. Post-Bronze `$A50F` never checks `$067C`, so Death Mask can be defeated directly in phase zero before Talk.

Post-Bronze `$A50F`:

```text
$EB=$FF             -> release $01 victory
$EB=$01             -> INC $064A + low-opponent feedback
$EB=$00 + hit       -> continue
$EB=$00 + no hit    -> miss feedback
```

`$064A` remains classifier scratch, not a Cancer one-shot latch; the stage-local handler increments it on every `$EB=$01` execution.

Post-Gold `$A560`:

```text
$EA=$FF                     -> release $FF defeat
$EA=$01 and $064D==0         -> first-low presentation; $064D=1
$EA=$01 and $064D!=0         -> common/repeat feedback
$EA=$00                      -> common/healthy feedback
```

Gold selection is generic parity slots `0/1`.

Generic `$FF` retry does not advance story; normal re-entry calls `$A973` and restores Cancer phase zero (`$067C=0`, `$064D=0`, `$0670=0`, `$068E=0`). Talk can therefore create platform `$0C` again.

Both canonical victory families converge on the frozen Leo boundary:

```text
phase-zero direct victory OR post-platform victory
  -> release $01
  -> $067D:03->04
  -> $050E=$04
  -> $06CD=$02
  -> $0673=$32
  -> active Seiya/Shun/Shiryu preserved
```

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/CancerStage03Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CancerStage03ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_03_CANCER.md`
- promoted `BattleStageContextCoverage` / coverage fixtures
- updated `BATTLE_STAGE_CONTEXT_COVERAGE.md`
- updated `BATTLE_EVENT_DISPATCH.md`
- PR #145

Do not reopen stage `$03`, platform `$0C`, or Leo `$04` without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Stage `$06` Scorpio / Milo is the first remaining material battle-stage gap

Canonical story entry is progress `$07`:

```text
$067D = $07
$F016[$07] = $06 -> $050E=$06
$E50B[$07] = $00 -> $06CD=$00
$0673 = $30
```

The `$30` story marker permits Seiya/Hyoga/Shun/Shiryu and masks Ikki, matching the established fixed Saint-selection semantics.

Coverage already fixes Scorpio's owners:

```text
init          $9ACE
Talk          $9E51
post-Bronze   $A7FF
post-Gold     $A847
Gold selector $908C+
Gold slots    0,1
```

#### Initialization `$9ACE`

`$9ACE` is exactly `RTS`; Scorpio has no dedicated intro handoff/reward in the initializer. Material stage state begins in Talk/post-action/selector logic.

#### Talk `$9E51`: dodge-history and Saint-specific reward state

Helper `$A1EC` returns byte-sum dodge history:

```text
A = $0677 + $0678
```

For total `< 2`:

```text
Hyoga ($0533=1)
  -> messages $84/$85
  -> if $068A==0:
       $068A:0->1
       scripted presentation
       #$03 -> $A1FF -> $F31E -> +300 Seventh Sense
  -> repeat while $068A!=0 gives no second +300 in the same runtime

Seiya/Shun/Shiryu
  -> messages $F8/$3E
  -> no persistent stage-local mutation
```

For total `>= 2` and `$066F==0`:

```text
per-Saint table $9ED2 = 86 86 87 86 for Saints 0..3
message $A3
$066F:0->1
#$02 -> $A1FF -> $F31E -> +200 Seventh Sense
```

For total `>= 2` and `$066F!=0`:

```text
same per-Saint text + $A3
Hyoga -> returns without $DC
Seiya/Shun/Shiryu -> INC $DC -> caller forces Gold response
```

Common reset `$A973` clears `$066F/$0677/$0678/$068A`; therefore a generic defeat/retry re-arms the Scorpio Talk/dodge-history gates. The dedicated checkpoint must preserve that reset ownership rather than treating either reward latch as permanent story state.

#### Post-Bronze `$A7FF`

After shared helper/classifier work:

```text
$EB=$FF -> scripted victory presentation -> release $01
$EB!=FF and $06BC!=0 -> message $A6 feedback -> continue
$EB!=FF and $06BC==0 -> continue
```

Generic Bronze damage/classification remains outside the stage-local model.

#### Post-Gold `$A847`

After shared player classifier:

```text
$EA=$00 -> continue
$EA=$01 -> stage-specific low-player feedback ($A4/$91) -> continue
$EA=$FF -> release $FF defeat
```

No stage-local one-shot low-player latch is tested here.

#### Scorpio Gold selector `$908C+`

Scorpio owns a dedicated dodge-history selector:

```text
$0677 + $0678 < 2  -> slot $01
$0677 + $0678 >=2 -> slot $00
```

No other slot is canonically reachable.

### Scorpio success does not hand off directly to Capricorn

This bridge is part of the required checkpoint because ordinary Scorpio victory at progress `$07` first enters a non-battle story/platform phase.

Scorpio victory release `$01` advances:

```text
$067D:07->08
$F016[$08]=$10
$E50B[$08]=$00
$050E=$10
$06CD=$00
$0673=$30
```

Here `$050E=$10` is a **story-stage/presentation value**. It must not be confused with special-normal platform substate RAM `$02=$10`, which belongs to the already-closed final-special path.

With progress `$08` and inherited release `$01`, fixed `$E1CF+` calls `$E4D7`. The exact `$E4E0` table entry for progress `$08` is `$08`, so it writes:

```text
$02 = $08
```

This is a principal platform substate (`$00-$0B`). Existing `PlatformExitGate` owns its standard exit:

```text
X >= $D0
Y == $40
jump phase == 0
transition = normal State3DReload
```

After accepted `$3D/$E100` reload, story progress is still `$08`, so `$F016[$08]` reconstructs `$050E=$10`. Fixed `$E2DD` recognizes exactly that value:

```text
$E2DD LDA $050E
$E2E0 CMP #$10
$E2E2 BEQ $E2D5
$E2D5 LDA #$01
$E2D7 STA $0670
$E2DA JMP $E386
```

Thus the engine automatically synthesizes another release `$01`. Fixed `$E399/$E3B3` then advances:

```text
$067D:08->09
$F016[$09]=$07
$E50B[$09]=$00
$050E=$07
$06CD=$00
$0673=$30
```

This is the exact Capricorn stage `$07` boundary. For the reachable `$30` roster, active Saint is preserved through the ordinary release/platform chain; Capricorn internals remain outside the Scorpio checkpoint.

The authoritative Scorpio checkpoint must therefore close **stage `$06` plus its progress-`$08` / stage-`$10` standard-platform bridge**. It must not stop prematurely at `$067D=$08/$050E=$10`, and it must not misidentify that stage value as platform substate `$10`.

## OPEN

1. Encode the exact canonical Scorpio seed `$067D=$07/$050E=$06/$06CD=$00/$0673=$30` and reachable Seiya/Hyoga/Shun/Shiryu roster.
2. Freeze initializer `$9ACE=RTS` and prove no stage-specific intro handoff is required.
3. Model Talk `$9E51` across dodge-history `<2` versus `>=2`, Hyoga's `$068A` +300 branch, first high-history `$066F` +200 branch, and repeat forced-Gold behavior only for non-Hyoga.
4. Close post-Bronze `$A7FF` victory/feedback using existing generic opponent arithmetic.
5. Close post-Gold `$A847` low feedback and defeat `$FF` using existing generic player arithmetic.
6. Encode Scorpio selector `$908C+`: dodge total `<2` -> slot1, `>=2` -> slot0.
7. Prove `$FF` retry semantics through `$A973`, including reset of `$066F/$0677/$0678/$068A` and renewed eligibility of Talk reward branches.
8. Compose Scorpio release `$01` from progress `$07` to progress `$08/$050E=$10`, then fixed `$E4D7` to principal platform substate `$02=$08`.
9. Reuse the established common platform gate for substate `$08` (`X>=$D0/Y=$40/jump=0`) and normal `$3D/$E100` reload; do not duplicate platform physics.
10. Prove fixed `$E2DD` stage-`$10` auto-release `$01` and compose it to exact Capricorn boundary `$067D=$09/$050E=$07/$06CD=$00/$0673=$30` without opening Capricorn internals.
11. Implement dedicated executable `ScorpioStage06Context`/compositor, discriminating fixtures and one focused context document. Stop at Capricorn boundary.

## NEXT

**Close canonical stage `$06` Scorpio / Milo from story seed `$067D=$07/$050E=$06/$06CD=$00/$0673=$30` through no-op init `$9ACE`, Talk `$9E51` with dodge-history/Hyoga reward state, post-Bronze `$A7FF`, post-Gold `$A847`, selector `$908C+`, generic `$FF` retry, and victory release `$01`; then compose the mandatory successor bridge `$067D=$08/$050E=$10 -> $02=$08` principal platform, accepted common exit `X>=$D0/Y=$40/jump=0`, fixed `$E2DD` auto-release `$01`, and exact Capricorn boundary `$067D=$09/$050E=$07/$06CD=$00/$0673=$30`.**

Completion criterion:

> Starting from the exact progress-`$07` roster/state, produce an executable composed Scorpio model that proves all Talk/reward/dodge-history branches, stage-local post-Bronze/post-Gold terminals, two-slot Gold selection, retry reset ownership, and the complete two-step success handoff through progress `$08` / stage `$10` / platform substate `$08` to Capricorn progress `$09`. Reuse generic battle arithmetic and existing platform gate/reload machinery. Keep `$050E=$10` story-stage and `$02=$10` special-normal substate namespaces distinct, and do not reopen Capricorn.

## BLOCKERS

- None. Canonical ROM, Scorpio handlers, generic battle primitives, fixed progression/release owners, principal platform exit gate and closed Capricorn successor boundary are available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged Git history if present.
2. Treat PR #145 / `BOSS_CONTEXT_STAGE_03_CANCER.md`, PR #143 / Gemini, PR #141 / Mu and PR #139 / coverage as frozen.
3. Start Scorpio only from canonical progress `$067D=$07` and handlers `$9ACE/$9E51/$A7FF/$A847/$908C+`.
4. Reuse generic battle damage/resources/dodge/classifiers and `PlatformExitGate` for principal platform substate `$08`; do not duplicate their mechanics.
5. Preserve the namespace distinction between story-stage `$050E=$10` and platform substate `$02=$10`.
6. Stop at exact Capricorn `$067D=$09/$050E=$07/$06CD=$00/$0673=$30`; do not begin `$9ACF/$9ED6/$A86B/$A8D8` in the same checkpoint.
7. Drive remains private ROM/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `stage-06-scorpio-progress08-stage10-platform08-bridge`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
