# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / stage $07 Capricorn / Shura`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#147` — complete canonical stage `$06` Scorpio / Milo context plus progress-`$08` story-stage-`$10` principal-platform `$08` successor bridge.
- Merge commit: `62f474bd4e31cd4ca4f40006fd4e9b475bd950b6`
- Exact final PR head: `4148e0b178592126ee2587a4ca83753019580b42`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #375: `SUCCESS`
  - `Original Spec` #582: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Coverage matrix `$050E=$00-$0B`: dedicated contexts closed at `$00/$01/$02/$03/$04/$05/$06/$08/$09/$0A`; `$07` Capricorn is the **only remaining material stage-local gap**; `$0B` remains structural/transient.
- Final-special `$0C` remains separately closed.

## DONE

### Stage `$06` Scorpio / Milo — PR #147

Canonical seed:

```text
$067D=$07
$050E=$06
$06CD=$00
$0673=$30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
Ikki excluded
```

Initializer `$9ACE` is exactly `RTS`.

Talk `$9E51` consumes the 8-bit dodge-history sum `$0677+$0678`:

```text
total < 2, Hyoga
  -> messages $84/$85
  -> first time $068A:0->1 and +300 Seventh Sense
  -> later low-history Hyoga Talks: no second +300

total < 2, other Saints
  -> messages $F8/$3E; no reward/state mutation

total >= 2, $066F==0
  -> per-Saint $86/$86/$87/$86 + $A3
  -> $066F:0->1
  -> +200 Seventh Sense

total >= 2, $066F!=0
  -> repeat dialogue
  -> Hyoga returns
  -> Seiya/Shun/Shiryu raise $DC and force Gold response
```

Post-Bronze `$A7FF`:

```text
$EB=$FF -> release $01 victory
otherwise + $06BC!=0 -> message $A6 and continue
otherwise -> continue without Scorpio-local feedback
```

Post-Gold `$A847`:

```text
$EA=$00 -> continue
$EA=$01 -> repeatable $A4/$91 low-player feedback
$EA=$FF -> release $FF defeat
```

Gold selector `$908C+`:

```text
8-bit dodge sum < 2  -> slot 1
8-bit dodge sum >= 2 -> slot 0
```

Generic `$FF` retry calls `$A973`, clearing `$066F/$0677/$0678/$068A` and rearming both Talk reward gates.

Scorpio victory is a two-release successor chain, not a direct Capricorn handoff:

```text
release $01 at progress $07
  -> $067D=$08
  -> $050E=$10 story-stage
  -> $06CD=$00 / $0673=$30

fixed $E4D7
  -> principal platform $02=$08
  -> accepted gate X >= $D0 / Y == $40 / jump=0
  -> State3DReload

fixed $E2DD after reload sees $050E=$10
  -> synthesizes release $01
  -> $067D=$09
  -> $050E=$07
  -> $06CD=$00
  -> $0673=$30
  -> active Saint preserved
```

`$050E=$10` story-stage and platform `$02=$10` are distinct namespaces; Scorpio's bridge uses platform `$02=$08`.

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/ScorpioStage06Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/ScorpioStage06ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_06_SCORPIO.md`
- promoted `BattleStageContextCoverage` and fixtures
- updated battle coverage/dispatch docs
- PR #147

Do not reopen Scorpio, its progress-`$08` bridge, or principal platform `$08` without contradictory ROM evidence or a failing fixture.

## EVIDENCE FOR NEXT

### Stage `$07` Capricorn / Shura — final material stage-local gap

Canonical entry from the closed Scorpio bridge is:

```text
$067D=$09
$F016[$09]=$07 -> $050E=$07
$E50B[$09]=$00 -> $06CD=$00
$0673=$30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
Ikki excluded
```

Exact stage owners:

```text
init          $9ACF
Talk          $9ED6
post-Bronze   $A86B
post-Gold     $A8D8
Gold selector generic parity slots 0,1
```

### Initializer `$9ACF`

Static ROM flow:

```text
9ACF LDA $0670
9AD2 CMP #$FE
9AD4 BNE $9AD7
9AD6 RTS

9AD7 JSR $9C6D
...
9ADF LDA #$0F
9AE1 JSR $F2ED          ; temporary presentation $0F
...
9AFC LDA #$00
9AFE STA $0672
...
9B06 INC $058A
9B09 INC $0696
9B0C LDA #$06
9B0E JSR $F31E          ; +600 Seventh Sense
9B11 JMP $9C3D          ; shared intro handoff
```

The dedicated checkpoint must establish exact canonical-entry and retry semantics for this growth path, including the `$0670==$FE` early-return guard, `$058A/$0696` increments, `$0672=0`, +600 reward, and shared `$9C3D` release-`$03` handoff.

### Talk `$9ED6`

```text
message $AE
if $066F==0:
  per-Saint table $9EFC = 43 43 43 AF
  display first-form dialogue
  INC $066F
  return
else:
  same per-Saint table via repeat-form call
  INC $DC
  INC $066F
  return
```

Thus first Talk does not force Gold; every repeated Talk raises transient `$DC` and the fixed caller forces a Gold response. The counter continues incrementing rather than acting as a strict boolean.

### Post-Bronze `$A86B`

After shared opponent classifier `$ACD6`:

```text
$EB=$00
  -> if $06BC!=0: continue
  -> if $06BC==0: message $8B feedback; continue

$EB=$01
  -> if active Shiryu ($0533=$03): continue without arming $0690
  -> otherwise: $0690=$FF; continue

$EB=$FF
  -> scripted victory sequence
  -> $06B1=$FF
  -> #$08 -> $F31E -> +800 Seventh Sense
  -> release $FE
```

`$0690` is consumed by fixed battle setup at `$FAB9+`: when nonzero it forces `$06BC=0`. The Capricorn model must compose this effect rather than treating `$0690` as an inert flag.

### Post-Gold `$A8D8`

```text
$EA=$00 -> continue
$EA=$01 -> messages $40/$91; continue
$EA=$FF -> release $FF defeat
```

The low-player feedback is repeatable; no stage-local one-shot latch is consulted.

### Victory release `$FE` successor

Fixed release owner `$E3ED-$E414` handles `$FE` specially:

```text
save active record
force $0533=$00 (Seiya)
rewrite release to $01
join ordinary story increment at $E3B3
```

From canonical progress `$09`, this yields:

```text
$067D:09->0A
$F016[$0A]=$08
$E50B[$0A]=$08
$050E=$08
$06CD=$08
$0673=$38
active Saint = Seiya
```

This is the exact already-closed final-Camus/Aquarius story boundary. The Capricorn checkpoint must stop there and reuse `AquariusStage08Context`; do not reopen Aquarius internals.

## OPEN

1. Model exact Capricorn seed `$067D=$09/$050E=$07/$06CD=$00/$0673=$30` and reachable 0..3 roster.
2. Close initializer `$9ACF`: `$FE` early return versus ordinary temp `$0F`, `$0672=0`, `$058A++/$0696++`, +600 Seventh Sense and shared release `$03` handoff.
3. Prove whether generic `$FF` retry re-executes the ordinary initializer growth/reward path and encode the ROM-exact behavior rather than assuming one-time story ownership.
4. Close Talk `$9ED6`: first `$066F` increment, per-Saint `$43/$43/$43/$AF`, repeated `$DC++` forced Gold response and continuing `$066F++`.
5. Close post-Bronze `$A86B` for `$EB=$00/$01/$FF`, including Shiryu exception to `$0690`, miss feedback `$8B`, scripted victory, `$06B1=$FF`, +800 reward and release `$FE`.
6. Compose `$0690` with fixed `$FAB9+` so its forced `$06BC=0` effect is executable/tested.
7. Close post-Gold `$A8D8`: healthy, repeatable low feedback `$40/$91`, defeat `$FF`.
8. Encode generic parity Gold slots `0/1`.
9. Prove `$FF` retry reset/re-entry semantics for `$066F/$0690` and initializer-owned technique/reward fields.
10. Compose release `$FE` through fixed `$E3ED-$E414` to forced Seiya and exact Aquarius boundary `$067D=$0A/$050E=$08/$06CD=$08/$0673=$38`; reuse existing Aquarius context and stop.
11. Promote `$07` to `DedicatedContextClosed`; the `$00-$0B` material stage-local gap set must become empty.

## NEXT

**Close canonical stage `$07` Capricorn / Shura from story seed `$067D=$09/$050E=$07/$06CD=$00/$0673=$30` through initializer `$9ACF`, Talk `$9ED6`, post-Bronze `$A86B`, post-Gold `$A8D8`, generic parity Gold slots `0/1`, `$0690->$FAB9` composition, generic `$FF` retry/re-entry, and scripted victory release `$FE`; then compose fixed `$E3ED-$E414` to forced Seiya and exact already-closed Aquarius boundary `$067D=$0A/$050E=$08/$06CD=$08/$0673=$38`.**

Completion criterion:

> Produce an executable dedicated Capricorn model proving ordinary versus `$FE`-guarded init, exact +600 technique/reward growth, first/repeat Talk behavior, `$0690` Shiryu/non-Shiryu split and its fixed consumer, all post-Bronze/post-Gold terminals, retry semantics, +800 scripted victory and `$FE` forced-Seiya handoff to Aquarius. Promote stage `$07` so no material `$00-$0B` battle-stage gaps remain. Do not reopen Aquarius.

## BLOCKERS

- None. Canonical ROM, exact stage-7 handlers, common reset/battle primitives, fixed `$0690` consumer, fixed `$FE` release owner and already-closed Aquarius successor are available.

## RECOVERY CONTRACT

1. Read this file from `main` first and reconcile it with newer merged history if present.
2. Freeze PR #147 / `BOSS_CONTEXT_STAGE_06_SCORPIO.md` and all earlier closed stage contexts.
3. Start only from Capricorn `$9ACF/$9ED6/$A86B/$A8D8` at progress `$09`.
4. Reuse generic damage/resources/classifiers and fixed release/story owners; do not duplicate them.
5. Reuse `AquariusStage08Context` only as the terminal successor boundary.
6. Stop after stage `$07` closure and coverage promotion; the next phase must be chosen from the remaining global ORIGINAL SPEC gaps, not by inventing another battle-stage gap.
7. Drive remains private asset/evidence storage only; it never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `stage-07-capricorn-fe-aquarius-boundary`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
