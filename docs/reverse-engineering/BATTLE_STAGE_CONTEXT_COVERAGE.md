# Battle-stage context coverage — `$050E=$00-$0B`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity used for this audit:

- size: `262160` bytes;
- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32: `F8D258A3`.

Status: the numeric battle-stage namespace `$00-$0B` is fully classified for canonical reachability and stage-local ownership. Dedicated executable contexts are now closed for `$00/$01/$02/$03/$04/$05/$08/$09/$0A`; remaining material gaps are `$06/$07`.

## 1. Canonical story provenance

Fixed-bank table `$F016` maps story progress `$067D=$00-$0E` to the current numeric stage/presentation index:

| `$067D` | `$F016[X]` | interpretation |
|---:|---:|---|
| `$00` | `$00` | Mu / pre-battle repair |
| `$01` | `$01` | Taurus |
| `$02` | `$02` | Gemini / first Camus composite |
| `$03` | `$03` | Cancer |
| `$04` | `$04` | Leo |
| `$05` | `$05` | Virgo |
| `$06` | `$0F` | non-ordinary story context |
| `$07` | `$06` | Scorpio |
| `$08` | `$10` | non-ordinary story context |
| `$09` | `$07` | Capricorn |
| `$0A` | `$08` | Aquarius |
| `$0B` | `$09` | Pisces |
| `$0C` | `$0C` | final-special rose bridge; closed separately |
| `$0D` | `$0A` | Saga |
| `$0E` | `$00` | post-Saga platform tail; not a Mu re-entry |

No story-progress value maps to stable stage `$0B`.

## 2. Dispatcher coverage matrix

The four exact bank-5 dispatcher families remain:

- initialization: `$97DB` / pointer table `$97E1`;
- Talk: `$9C95` / pointer table `$9C9B`;
- post-Bronze: `$A361` / pointer table `$A367`;
- post-Gold: `$A381` / pointer table `$A387`.

The Gold selector is bank 6 `$9074-$9146`; stages without a dedicated branch fall through `$913E` and choose `slot = $065F & 1`.

| stage | canonical progress | init | Talk | post-Bronze | post-Gold | Gold slots | coverage |
|---:|---:|---:|---:|---:|---:|---|---|
| `$00` | `$00` | `$97F7` | `$9CB7` | `$A3A1` | `$A3A1` | none | dedicated special context closed |
| `$01` | `$01` | `$97F8` | `$9D2C` | `$A3A2` | `$A415` | `0,1` | dedicated context closed |
| `$02` | `$02` | `$981F` | `$9D81` | `$A444` | `$A4CC` | `0,1` | dedicated composite context closed |
| `$03` | `$03` | `$9851` | `$9D96` | `$A50F` | `$A560` | `0,1` | dedicated context closed |
| `$04` | `$04` | `$989D` | `$9DD8` | `$A5B3` | `$A63E` | `0,1` | dedicated context closed |
| `$05` | `$05` | `$9A28` | `$9E1B` | `$A661` | `$A7B3` | `0,1,2` | dedicated context closed |
| `$06` | `$07` | `$9ACE` | `$9E51` | `$A7FF` | `$A847` | `0,1` | **material context missing** |
| `$07` | `$09` | `$9ACF` | `$9ED6` | `$A86B` | `$A8D8` | `0,1` | **material context missing** |
| `$08` | `$0A` | `$9B14` | `$9F00` | `$A8FC` | `$A9D3` | `0,1,2` | dedicated context closed |
| `$09` | `$0B` | `$9B5C` | `$9F99` | `$AA57` | `$AAF0` | `0,1,2` | dedicated context closed |
| `$0A` | `$0D` | `$9B5D` | `$9FF4` | `$AB18` | `$AC05` | `0,1,2,3` | dedicated context closed |
| `$0B` | none | `$A960`* | `$9FF4` | `$A3A1` | `$A3A1` | none | structural/transient; no canonical battle |

`*` `$A960` is not an intended instruction boundary: the real instruction begins at `$A95F` (`AD 6F 06`, `LDA $066F`). Canonical control never dispatches it.

Stage `$0C` remains a separately closed non-boss bridge outside this ordinary `$00-$0B` denominator.

## 3. Stage `$00` closure

`BOSS_CONTEXT_STAGE_00_MU.md` / `MuStage00Context` close the Mu/pre-battle repair context:

- `$0673=$30` permits Seiya/Hyoga/Shun/Shiryu and masks Ikki;
- Resource Allocation, Attack and Escape are diverted to `$F238`;
- `$F238` owns accumulating `$06BB` and repeated blocked-command presentation;
- Talk `$9CB7` is the sole progression command;
- first Talk writes `$066F=1`;
- second/repeated Talk emits release `$01`;
- fixed `$E399/$E3B3` advances `$067D:00->01` and `$F016[01]=01` selects Taurus;
- no Bronze/Gold arithmetic is reachable from stage zero.

Stage `$00` is frozen as `DedicatedContextClosed` with `nameof(MuStage00Context)`.

## 4. Stage `$02` closure — Gemini + first Camus composite

`BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md` / `GeminiStage02Context` close stage `$02` as a composed lifecycle.

Canonical seed:

```text
$067D=$02
$F016[$02]=$02
$E50B[$02]=$00
$06CD=$00
$0673=$30
```

Reachable entry Saints are Seiya/Hyoga/Shun/Shiryu; Ikki is masked.

Key closed facts:

- init `$981F` temporarily presents `$11`, grants +300 Seventh Sense and exits through shared release `$03`;
- every Talk `$9D81` raises transient `$DC`; first Talk also writes `$066F:0->1`;
- `$A444` checks `$067C` before classifier, so phase-zero first Bronze action must emit `$02=$0E / release $02`;
- platform `$0E` accepted exit is `X >= $B4 / Y=$80 / jump=0`, then `$067C:0->1` without `$A973`;
- ordinary Seiya/Shun/Shiryu resume stage `$02`; Hyoga redirects to `$050E=$08/$06B8=$0A/$0690=$FF` first Camus;
- ordinary post-detour victory uses release `$01`; generic defeat uses `$FF` and retry re-arms phase zero;
- both ordinary victory and redirected first-Camus `$FE` completion converge on Cancer `$067D=$03/$050E=$03/$06CD=$02/$0673=$32`.

Stage `$02` is frozen as `DedicatedContextClosed` with `nameof(GeminiStage02Context)`.

## 5. Stage `$03` closure — Cancer / Death Mask

`BOSS_CONTEXT_STAGE_03_CANCER.md` / `CancerStage03Context` close the first gap left after Gemini.

Canonical seed:

```text
$067D=$03
$F016[$03]=$03
$E50B[$03]=$02
$06CD=$02
$0673=$32
```

Reachable entry Saints are exactly Seiya, Shun and Shiryu. Hyoga and Ikki are blocked.

### Initialization `$9851`

The initializer:

```text
temporary presentation $050E=$0E
#$04 -> $F31E -> +400 Seventh Sense
shared $9C3D -> $0670=$03 / $068E=1 / restore $050E=$03
```

The internal `$03` handoff is consumed before ordinary command selection.

### Talk `$9D96`

Cancer Talk is phase-controlled by `$067C`.

Phase zero:

```text
$02=$0C
$0670=$02
PLA
PLA
RTS
```

The double-`PLA` unwinds the ordinary Talk caller. This branch does not raise `$DC` and therefore does not force a Gold response.

The already-closed platform `$0C` pipeline owns the exact exit gate:

```text
X >= $88
Y == $20
jump phase == 0
```

Accepted release `$02` special resume increments `$067C:0->1` while skipping `$A973`, so active Saint and stage-local `$064D/$068E` survive.

Phase one Talk instead displays `$58/$57` and increments transient `$DC`; the fixed caller then forces the Gold-response path.

### The platform detour is optional

Unlike Gemini, `$A50F` does not test `$067C`. Ordinary Bronze action remains reachable in phase zero.

Therefore a canonical phase-zero Attack can defeat Death Mask before Talk ever creates platform `$0C`:

```text
phase0 -> Bronze action -> $EB=$FF -> release $01 -> Leo
```

The Talk/platform path is a second canonical route, not a prerequisite for victory.

### Post-Bronze `$A50F`

After generic classifier `$ACD6`:

```text
$EB=$FF             -> release $01 victory
$EB=$01             -> INC $064A; low-opponent feedback
$EB=$00 + hit       -> continue
$EB=$00 + no hit    -> miss feedback
```

`$064A` is classifier scratch. The stage-local handler increments it on **every** `$EB=$01` execution; it is not promoted to a one-shot Cancer latch.

### Post-Gold `$A560`

After generic classifier `$AD4D`:

```text
$EA=$FF                     -> release $FF defeat
$EA=$01 and $064D==0         -> special first-low feedback; INC $064D
$EA=$01 and $064D!=0         -> common/repeat feedback
$EA=$00                      -> common/healthy feedback
```

`$064D` is the real one-time Cancer low-player latch.

Gold selection is generic parity, exactly slots `0,1`.

### Retry and Leo convergence

Generic `$FF` defeat leaves progress `$03`; normal re-entry calls `$A973`, restoring `$067C=0/$064D=0/$0670=0/$068E=0`. Talk can therefore create platform `$0C` again on retry.

Victory release `$01` advances:

```text
$067D:03->04
$F016[04]=$04
$E50B[04]=$02
$050E=$04
$06CD=$02
$0673=$32
active Saint preserved (Seiya/Shun/Shiryu)
```

Both phase-zero direct victory and post-platform victory converge on the already-closed Leo boundary.

Stage `$03` is frozen as `DedicatedContextClosed` with `nameof(CancerStage03Context)`.

## 6. Why stage `$0B` is not a missing context

Two independent facts eliminate `$0B` as a canonical stable battle entry.

First, `$F016` contains no `$0B` value.

Second, the only immediate `$0B` assignments in the relevant event code are temporary presentation loads:

```text
$9B31  LDA #$0B
$9B33  JSR $F2ED

$A16D  LDA #$0B
$A16F  JSR $F2ED
```

The malformed init table pointer reinforces the conclusion:

```text
$A95F: AD 6F 06    LDA $066F
        ^
$A960 is byte 2 of that instruction
```

No dedicated `$0B` battle context is required.

## 7. Remaining material gaps

### `$06` — Scorpio / Milo — **next gap**

- init `$9ACE` is `RTS`, but Talk/post-action handlers are stage-specific;
- Talk `$9E51` consumes dodge history through `$A1EC`, branches on active Saint, mutates `$068A/$066F`, invokes reward logic and can raise `$DC`;
- post-Bronze `$A7FF` owns victory `$01` and stage feedback;
- post-Gold `$A847` owns low-player response and defeat `$FF`;
- selector `$908C+` chooses slot `0` after at least two accumulated dodge events, otherwise slot `1`.

### `$07` — Capricorn / Shura

- init `$9ACF` owns Shiryu progression/technique growth unless re-entered with `$0670=$FE`;
- Talk `$9ED6` advances `$066F` and later raises `$DC`;
- post-Bronze `$A86B` owns `$0690` and a special opponent-defeat sequence ending in `$FE`;
- post-Gold `$A8D8` owns defeat `$FF`;
- Gold selection reaches slots `0/1`.

## 8. Gold-slot reachability result

Canonical reachable `$0680` masks remain:

```text
stage 00  none
stage 01  0,1
stage 02  0,1
stage 03  0,1
stage 04  0,1
stage 05  0,1,2
stage 06  0,1
stage 07  0,1
stage 08  0,1,2
stage 09  0,1,2
stage 0A  0,1,2,3
stage 0B  none
```

## 9. Executable coverage artifact

`BattleStageContextCoverage.cs` records dispatcher ownership, story provenance, current coverage classification and Gold-slot reachability. `BattleStageContextCoverageChecks.cs` prevents silent regression of those classifications.

Current result:

```text
closed dedicated contexts : 00,01,02,03,04,05,08,09,0A
material uncovered        : 06,07
structural/no battle       : 0B
separate closed bridge     : 0C
```

The next canonical checkpoint is **stage `$06` Scorpio / Milo**.
