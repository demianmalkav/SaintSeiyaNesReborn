# Battle-stage context coverage — `$050E=$00-$0B`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity used for this audit:

- size: `262160` bytes;
- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32: `F8D258A3`.

Status: the numeric battle-stage namespace `$00-$0B` is fully classified for canonical reachability and stage-local ownership. Dedicated executable contexts are now closed for `$00/$01/$02/$04/$05/$08/$09/$0A`; remaining material gaps are `$03/$06/$07`.

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
| `$03` | `$03` | `$9851` | `$9D96` | `$A50F` | `$A560` | `0,1` | **material context missing** |
| `$04` | `$04` | `$989D` | `$9DD8` | `$A5B3` | `$A63E` | `0,1` | dedicated context closed |
| `$05` | `$05` | `$9A28` | `$9E1B` | `$A661` | `$A7B3` | `0,1,2` | dedicated context closed |
| `$06` | `$07` | `$9ACE` | `$9E51` | `$A7FF` | `$A847` | `0,1` | **material context missing** |
| `$07` | `$09` | `$9ACF` | `$9ED6` | `$A86B` | `$A8D8` | `0,1` | **material context missing** |
| `$08` | `$0A` | `$9B14` | `$9F00` | `$A8FC` | `$A9D3` | `0,1,2` | dedicated context closed |
| `$09` | `$0B` | `$9B5C` | `$9F99` | `$AA57` | `0,1,2` | dedicated context closed |
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

`BOSS_CONTEXT_STAGE_02_GEMINI_FIRST_CAMUS.md` / `GeminiStage02Context` close stage `$02` as a **composed lifecycle**, not an isolated handler set.

Canonical seed:

```text
$067D=$02
$F016[$02]=$02
$E50B[$02]=$00
$06CD=$00
$0673=$30
```

Reachable entry Saints are Seiya/Hyoga/Shun/Shiryu; Ikki is masked.

### Initialization `$981F`

- temporary presentation index `$11` through `$F2ED`;
- `#$03 -> $F31E` = +300 Seventh Sense;
- shared `$9C3D` writes `$0670=$03`, `$068E=1` and restores stage `$02`;
- the internal `$03` handoff is consumed without `$A973` reset.

### Talk `$9D81`

- every Talk increments transient `$DC`, so every Talk forces a Gold response;
- first Talk alone writes `$066F:0->1`;
- repeated Talk keeps `$066F` nonzero.

### Mandatory first Bronze detour `$A444`

`$A444` tests `$067C` **before** calling the generic opponent classifier. With canonical phase zero, the first Bronze action always exits through:

```text
$02=$0E
$0670=$02
```

This occurs even before any `$EB=$FF` victory branch can be considered.

The already-closed `PlatformSpecialNormalExitPipeline` owns substate `$0E` and its exact gate:

```text
X >= $B4
Y == $80
jump phase == 0
```

Accepted reload reaches `$ED57/$ED8F` and increments `$067C:0->1` without calling `$A973`. Therefore pre-detour `$066F` survives ordinary resume.

### Resume split

For Seiya/Shun/Shiryu:

```text
$050E remains $02
$067C=1
$06B8=0
```

For Hyoga (`$0533=1`), existing fixed ownership redirects:

```text
$050E=$08
$06B8=$0A
$0690=$FF
$067D remains $02
$067C=1
```

That branch composes directly with `AquariusStage08Context.EnterRedirectedFirstCamus`; stage `$08` internals are not duplicated.

### Ordinary stage-2 completion

After `$067C!=0`, `$A444` uses the generic opponent classifier:

- `$EB=$FF` -> release `$01` victory;
- `$EB=$01` -> low-opponent feedback;
- `$EB=$00` + no hit -> miss feedback;
- `$EB=$00` + hit -> continue.

Post-Gold `$A4CC` consumes `$EA`:

- `$00` -> healthy feedback;
- `$01` -> low-player feedback;
- `$FF` -> generic defeat release `$FF`.

Gold selection is the generic parity selector, exactly slots `0,1`.

### Retry semantics

A stage-2 `$FF` defeat does not advance story progress. On normal re-entry `$ED57` takes the non-`02/03` path and calls `$A973`, clearing `$067C/$066F/$0670/$068E/$06B8`.

Therefore defeat **rearms phase zero** and the next first Bronze action repeats the mandatory `$0E` detour.

### Cancer convergence

Ordinary release `$01`:

```text
$067D:02->03
$F016[03]=$03
$E50B[03]=$02
$06CD=$02
$0673=$32
active Saint preserved (canonical ordinary winner is Seiya/Shun/Shiryu)
```

Hyoga's redirected first Camus has two already-closed `$FE` terminals: three-Talk scripted freezing after a Bronze action, or actual Hyoga defeat during first-Camus Gold response. Both use the existing `$FE` owner, force Seiya and advance to the same story progress/stage:

```text
$067D=$03
$050E=$03
$06CD=$02
$0673=$32
active Saint=Seiya
```

Stage `$02` is therefore frozen as `DedicatedContextClosed` with `nameof(GeminiStage02Context)`.

## 5. Why stage `$0B` is not a missing context

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

## 6. Remaining material gaps

### `$03` — Cancer / Death Mask — **next gap**

- init `$9851` is stage-specific;
- Talk `$9D96` branches on `$067C`; its zero branch writes `$02=$0C`, emits release `$02` and unwinds the caller, while the other branch raises transient `$DC`;
- post-Bronze `$A50F` owns opponent-low `$064A` and victory `$01`;
- post-Gold `$A560` owns first player-low `$064D` and defeat `$FF`;
- Gold selection reaches slots `0/1`.

### `$06` — Scorpio / Milo

- init `$9ACE` is `RTS`, but Talk/post-action handlers are stage-specific;
- Talk `$9E51` consumes dodge history through `$A1EC`, branches on active Saint, mutates `$068A/$066F`, invokes reward logic and can raise `$DC`;
- post-Bronze `$A7FF` owns victory `$01` and stage feedback;
- post-Gold `$A847` owns low-player response and defeat `$FF`;
- selector `$908C+` chooses slot `0` after at least two accumulated dodge events, otherwise slot `1`.

### `$07` — Capricorn / Shura

- init `$9ACF` owns Shiryu progression/technique growth unless re-entered with `$0670=$FE`;
- Talk `$9ED6` advances `$066F` and later raises `$DC`;
- post-Bronze `$A86B` owns `$0690` and a special defeat sequence ending in `$FE`;
- post-Gold `$A8D8` owns defeat `$FF`;
- Gold selection reaches slots `0/1`.

## 7. Gold-slot reachability result

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

## 8. Executable coverage artifact

`BattleStageContextCoverage.cs` records dispatcher ownership, story provenance, current coverage classification and Gold-slot reachability. `BattleStageContextCoverageChecks.cs` prevents silent regression of those classifications.

Current result:

```text
closed dedicated contexts : 00,01,02,04,05,08,09,0A
material uncovered        : 03,06,07
structural/no battle       : 0B
separate closed bridge     : 0C
```

The next canonical checkpoint is **stage `$03` Cancer / Death Mask**.
