# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the four stage-indexed dispatcher families are statically confirmed, the canonical `$050E=$00-$0B` namespace has been coverage-audited end-to-end, and dedicated executable contexts are now closed for `$00/$01/$02/$04/$05/$08/$09/$0A`. The first remaining material gap is `$03` Cancer / Death Mask.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the numeric denominator, `BOSS_CONTEXT_STAGE_00_MU.md` for Mu, and `BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md` for the composed stage-2 lifecycle.

## Stage-indexed architecture

PRG bank 5 repeatedly uses:

`LDA $050E -> JSR $E698 -> inline pointer table`

`$E698` is the shared indirect dispatcher.

## 1. `$97DB` — battle/stage initialization dispatcher

Pointer table at `$97E1`:

| Stage | Handler |
|---:|---:|
| 0 | `$97F7` |
| 1 | `$97F8` |
| 2 | `$981F` |
| 3 | `$9851` |
| 4 | `$989D` |
| 5 | `$9A28` |
| 6 | `$9ACE` |
| 7 | `$9ACF` |
| 8 | `$9B14` |
| 9 | `$9B5C` |
| 10 | `$9B5D` |
| 11 | raw pointer `$A960`; no canonical stable battle entry |
| 12 | table overrun -> raw `$8D00`; unreachable on canonical final-special entry |

Stage `$00` has a no-op initializer but material command/Talk state. Stage `$02` initializer `$981F` is material: it temporarily loads presentation index `$11`, grants `#$03` through `$F31E` (+300 Seventh Sense), then shared `$9C3D` writes `$0670=$03/$068E=1` and restores stage `$02`.

Stage `$0B` remains structural only: `$F016` never selects it as a stable battle stage, and pointer `$A960` lands inside the real instruction beginning at `$A95F` (`AD 6F 06`). Relevant immediate `$0B` uses call temporary loader `$F2ED`.

Saga stage `$0A` remains phase-dispatched through `$06CE`. Stage `$0C` remains the separately closed exception whose canonical entry bypasses this initializer table.

## 2. `$9C95` — Talk / interaction dispatcher

Pointer table at `$9C9B`:

| Stage | Handler |
|---:|---:|
| 0 | `$9CB7` |
| 1 | `$9D2C` |
| 2 | `$9D81` |
| 3 | `$9D96` |
| 4 | `$9DD8` |
| 5 | `$9E1B` |
| 6 | `$9E51` |
| 7 | `$9ED6` |
| 8 | `$9F00` |
| 9 | `$9F99` |
| 10 | `$9FF4` |
| 11 | `$9FF4` structural alias only |
| 12 | `$A1AD` |

### Stage `$00` Talk

Closed by `MuStage00Context`:

```text
$066F==0 -> first presentation -> $066F=1
$066F!=0 -> second/repeated presentation -> $0670=$01
```

### Stage `$02` Talk

Closed by `GeminiStage02Context`:

```text
$9D81 INC $DC
       message $45
       if $066F==0:
           INC $066F
           message $43
       RTS
```

Every stage-2 Talk therefore forces a Gold response through transient `$DC`. First Talk writes `$066F:0->1`; repeats preserve the nonzero value. If Talk occurs before the mandatory platform detour, `$066F` survives the ordinary release-`$02` return because `$ED8F` bypasses `$A973`.

Saga `$9FF4` and final-special `$A1AD` retain their already-closed semantics.

## 3. `$A361` — post-Bronze-action dispatcher

Pointer table at `$A367`:

| Stage | Handler |
|---:|---:|
| 0 | `$A3A1` |
| 1 | `$A3A2` |
| 2 | `$A444` |
| 3 | `$A50F` |
| 4 | `$A5B3` |
| 5 | `$A661` |
| 6 | `$A7FF` |
| 7 | `$A86B` |
| 8 | `$A8FC` |
| 9 | `$AA57` |
| 10 | `$AB18` |
| 11 | `$A3A1` structural only |
| 12 | `$A3A1` structural only; unreachable from canonical `$0C` Attack |

`$A3A1` is `RTS`.

### Stage `$02` phase split

`$A444` begins with `$067C`:

```text
$067C==0
  -> presentation
  -> $02=$0E
  -> release $0670=$02
  -> leave battle before generic opponent classification

$067C!=0
  -> JSR $ACD6
  -> consume $EB
```

Thus the first Bronze action is mandatorily diverted to platform substate `$0E`; the ordinary `$EB=$FF` victory branch is unreachable before that detour.

The already-closed platform `$0E` reload increments `$067C:0->1`. Seiya/Shun/Shiryu resume ordinary stage `$02`; Hyoga is redirected by fixed `$ED99+` to `$050E=$08/$06B8=$0A/$0690=$FF`, composing with the closed first-Camus Aquarius context.

Once `$067C!=0`, `$A444` owns:

```text
$EB=$FF -> release $01 victory
$EB=$01 -> low-opponent feedback
$EB=$00 + $06BC=0 -> miss feedback
$EB=$00 + hit -> continue
```

Mu `$00`, Saga `$0A` and final-special `$0C` retain their previously closed reachability constraints.

## 4. `$A381` — post-Gold-response dispatcher

Pointer table at `$A387`:

| Stage | Handler |
|---:|---:|
| 0 | `$A3A1` |
| 1 | `$A415` |
| 2 | `$A4CC` |
| 3 | `$A560` |
| 4 | `$A63E` |
| 5 | `$A7B3` |
| 6 | `$A847` |
| 7 | `$A8D8` |
| 8 | `$A9D3` |
| 9 | `$AAF0` |
| 10 | `$AC05` |
| 11 | `$A3A1` structural only |
| 12 | `$A3A1` structural only; unreachable at canonical `$0C` |

Stage `$02` `$A4CC` consumes the already-computed player condition:

```text
$EA=$00 -> healthy-player feedback, continue
$EA=$01 -> low-player feedback, continue
$EA=$FF -> release $FF
```

Generic `$FF` does not advance story. On the next normal re-entry, `$ED57` takes the non-`$02/$03` branch and calls `$A973`, clearing `$067C`; therefore a stage-2 retry rearms the mandatory platform detour.

Stage `$00` cannot reach Gold response, `$0B` has no stable battle, and `$0C` bypasses ordinary Gold response entirely.

## Canonical story-stage provenance

Fixed `$F016` maps `$067D=$00-$0E`:

```text
00->00  01->01  02->02  03->03  04->04
05->05  06->0F  07->06  08->10  09->07
0A->08  0B->09  0C->0C  0D->0A  0E->00
```

Canonical ordinary entries:

```text
stage 00 <- progress 00
stage 01 <- progress 01
stage 02 <- progress 02
stage 03 <- progress 03
stage 04 <- progress 04
stage 05 <- progress 05
stage 06 <- progress 07
stage 07 <- progress 09
stage 08 <- progress 0A
stage 09 <- progress 0B
stage 0A <- progress 0D
stage 0B <- none
```

Progress `$0E` reuses numeric `$00` only in the already-closed post-Saga platform tail.

### Stage `$02` successor ownership

Ordinary stage-2 release `$01` joins fixed `$E399/$E3B3`:

```text
$067D:02->03
$F016[03]=$03
$E50B[03]=$02
$06CD=$02
$0673=$32
```

The reachable ordinary winners are Seiya/Shun/Shiryu; Hyoga's `$0E` exit redirects to first Camus instead.

First-Camus completion uses existing release `$FE` ownership. Both the three-Talk scripted-freezing route and actual Hyoga defeat during first-Camus Gold response emit `$FE`, force Seiya and advance the same `$067D:02->03 / $050E=$03` boundary. Adding the fixed progress-`$03` descriptor yields the same `$06CD=$02/$0673=$32` Cancer state.

## Gold-selector coverage

Bank 6 `$9074-$9146` proves canonical reachable opponent slots:

| Stage | Reachable `$0680` slots |
|---:|---|
| `$00` | none |
| `$01` | `0,1` |
| `$02` | `0,1` |
| `$03` | `0,1` |
| `$04` | `0,1` |
| `$05` | `0,1,2` |
| `$06` | `0,1` |
| `$07` | `0,1` |
| `$08` | `0,1,2` |
| `$09` | `0,1,2` |
| `$0A` | `0,1,2,3` |
| `$0B` | none |

Stage `$02` uses the generic parity fallthrough at `$913E`: `slot = $065F & 1`.

## Coverage classification

| Stage | Context | Coverage status |
|---:|---|---|
| `$00` | Mu / pre-battle repair | dedicated special context closed |
| `$01` | Taurus — Aldebaran | dedicated context closed |
| `$02` | Gemini / first Camus composite | dedicated context closed |
| `$03` | Cancer — Death Mask | **material context missing; next boundary** |
| `$04` | Leo — Aioria | dedicated context closed |
| `$05` | Virgo — Shaka | dedicated context closed |
| `$06` | Scorpio — Milo | **material context missing** |
| `$07` | Capricorn — Shura | **material context missing** |
| `$08` | Aquarius — Camus | dedicated context closed |
| `$09` | Pisces — Aphrodite | dedicated context closed |
| `$0A` | Pope/Saga | dedicated context closed |
| `$0B` | transient/structural presentation index | no stable battle |
| `$0C` | final-special rose bridge | separately closed non-boss exception |

## Promoted contexts and next gap

Dedicated executable contexts are now closed for `$00/$01/$02/$04/$05/$08/$09/$0A`; final-special `$0C` is closed separately. Remaining material gaps are `$03/$06/$07`; `$0B` remains structural only.

The next checkpoint is **stage `$03` Cancer / Death Mask**. Its exact handlers are already bounded at `$9851/$9D96/$A50F/$A560`, and the coverage audit has already identified its `$067C=0` Talk-created platform `$0C` detour as the first special surface to compose.
