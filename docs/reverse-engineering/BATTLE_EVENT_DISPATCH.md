# Battle narrative dispatchers — per-stage event state machines

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the four stage-indexed dispatcher families are statically confirmed, the canonical `$050E=$00-$0B` namespace has been coverage-audited end-to-end, and dedicated executable contexts are now closed for `$00/$01/$02/$03/$04/$05/$08/$09/$0A`. The first remaining material gap is `$06` Scorpio / Milo.

See `BATTLE_STAGE_CONTEXT_COVERAGE.md` for the numeric denominator, `BOSS_CONTEXT_STAGE_00_MU.md` for Mu, `BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md` for the composed stage-2 lifecycle, and `BOSS_CONTEXT_STAGE_03_CANCER.md` for Cancer.

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

Stage `$00` has a no-op initializer but material command/Talk state.

Stage `$02` initializer `$981F` is closed: temporary presentation `$11`, +300 Seventh Sense through `#$03 -> $F31E`, then shared `$9C3D` writes `$0670=$03/$068E=1` and restores stage `$02`.

Stage `$03` initializer `$9851` is also closed:

```text
$9859  LDA #$0E
$985B  JSR $F2ED         ; temporary presentation index $0E
...
$9895  LDA #$04
$9897  JSR $F31E         ; +400 Seventh Sense
$989A  JMP $9C3D
```

Shared `$9C3D` produces the normal intro handoff `$0670=$03/$068E=1` and restores real stage `$03`.

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

Every stage-2 Talk forces a Gold response through transient `$DC`; first Talk writes `$066F:0->1`.

### Stage `$03` Talk

Closed by `CancerStage03Context` and split by `$067C`.

Phase zero:

```text
$9D96  LDA $067C
$9D99  BNE $9DCB
...
$9DBF  LDA #$0C
$9DC1  STA $02
$9DC3  LDA #$02
$9DC5  STA $0670
$9DC8  PLA
$9DC9  PLA
$9DCA  RTS
```

This creates special platform substate `$0C`, emits release `$02` and unwinds the ordinary Talk caller. It does not increment `$DC`, so no Gold response is forced on this branch.

The already-closed platform `$0C` exit requires `X >= $88 / Y=$20 / jump=0`. Its release-`$02` special resume increments `$067C:0->1` while skipping `$A973`.

Phase one:

```text
$9DCB  message $58
$9DD0  message $57
$9DD5  INC $DC
$9DD7  RTS
```

The fixed command owner therefore forces a Gold response after phase-one Talk.

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

`$A444` tests `$067C` before generic opponent classification. Phase zero therefore mandates platform `$0E/release $02`; only phase one can reach ordinary stage-2 post-Bronze victory/feedback.

### Stage `$03` ordinary post-Bronze

Cancer `$A50F` has **no** `$067C` gate:

```text
$A50F  JSR $ACD6
$A512  LDA $EB
$A514  CMP #$FF
...
$A52C  LDA #$01
$A52E  JMP $ACAA         ; release $01 victory

$A531  LDA $EB
$A533  CMP #$01
...
$A537  INC $064A
...
$A548  LDA $06BC
$A54B  BNE $A547         ; hit -> continue
...
$A55A  LDA #$3D          ; miss feedback
```

Semantic branches:

```text
$EB=$FF             -> release $01
$EB=$01             -> INC $064A + low-opponent feedback
$EB=$00 + hit       -> continue
$EB=$00 + no hit    -> miss feedback
```

Because `$067C` is not consulted, **phase-zero direct victory is canonical**. Cancer does not require Talk/platform `$0C` before Death Mask can be defeated.

`$064A` is classifier scratch; the stage handler increments it on each `$EB=$01` execution and does not use it as a one-shot predicate.

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

Stage `$02` `$A4CC` consumes `$EA=$00/$01/$FF` as healthy/low/defeat `$FF`.

Stage `$03` `$A560` is closed as:

```text
$EA=$FF                     -> release $FF
$EA=$01 and $064D==0         -> first-low presentation; INC $064D
$EA=$01 and $064D!=0         -> common/repeat feedback
$EA=$00                      -> common/healthy feedback
```

`$064D` is therefore a one-time Cancer low-player latch. Normal `$FF` retry returns through `$A973`, which clears `$064D` and `$067C`; retry starts in phase zero and Talk may create platform `$0C` again.

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

Ordinary stage-2 `$01` and redirected first-Camus `$FE` both converge on Cancer `$067D=$03/$050E=$03/$06CD=$02/$0673=$32`.

### Stage `$03` successor ownership

Cancer victory `$01` joins fixed `$E399/$E3B3`:

```text
$067D:03->04
$F016[04]=$04
$E50B[04]=$02
$06CD=$02
$0673=$32
$050E=$04
```

The Cancer roster excludes Ikki, so active Seiya/Shun/Shiryu is preserved. Both phase-zero direct victory and post-platform phase-one victory terminate at the same already-closed Leo boundary.

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

Stages `$02/$03` both use the generic parity fallthrough at `$913E`: `slot = $065F & 1`.

## Coverage classification

| Stage | Context | Coverage status |
|---:|---|---|
| `$00` | Mu / pre-battle repair | dedicated special context closed |
| `$01` | Taurus — Aldebaran | dedicated context closed |
| `$02` | Gemini / first Camus composite | dedicated context closed |
| `$03` | Cancer — Death Mask | dedicated context closed |
| `$04` | Leo — Aioria | dedicated context closed |
| `$05` | Virgo — Shaka | dedicated context closed |
| `$06` | Scorpio — Milo | **material context missing; next boundary** |
| `$07` | Capricorn — Shura | **material context missing** |
| `$08` | Aquarius — Camus | dedicated context closed |
| `$09` | Pisces — Aphrodite | dedicated context closed |
| `$0A` | Pope/Saga | dedicated context closed |
| `$0B` | transient/structural presentation index | no stable battle |
| `$0C` | final-special rose bridge | separately closed non-boss exception |

## Promoted contexts and next gap

Dedicated executable contexts are now closed for `$00/$01/$02/$03/$04/$05/$08/$09/$0A`; final-special `$0C` is closed separately. Remaining material gaps are `$06/$07`; `$0B` remains structural only.

The next checkpoint is **stage `$06` Scorpio / Milo**. Its exact owners remain `$9ACE/$9E51/$A7FF/$A847`, with stage-specific Talk/dodge-history behavior and a dedicated Gold selector branch at `$908C+`.
