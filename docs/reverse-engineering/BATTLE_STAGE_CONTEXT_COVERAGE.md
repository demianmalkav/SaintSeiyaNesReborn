# Battle-stage context coverage — `$050E=$00-$0B`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

Status: the numeric `$050E=$00-$0B` battle/event namespace is fully classified. Dedicated executable contexts are closed for `$00/$01/$02/$03/$04/$05/$06/$08/$09/$0A`; **Capricorn `$07` is the only remaining material stage-local gap**. `$0B` is structural/transient and has no canonical stable battle. Final-special `$0C` remains a separately closed bridge outside this denominator.

## Canonical story provenance

Fixed `$F016` maps story progress `$067D=$00-$0E` to stage/presentation index:

| `$067D` | `$F016[X]` | interpretation |
|---:|---:|---|
| `$00` | `$00` | Mu / repair |
| `$01` | `$01` | Taurus |
| `$02` | `$02` | Gemini / first Camus |
| `$03` | `$03` | Cancer |
| `$04` | `$04` | Leo |
| `$05` | `$05` | Virgo |
| `$06` | `$0F` | non-ordinary story context |
| `$07` | `$06` | Scorpio |
| `$08` | `$10` | non-ordinary story context / Scorpio successor bridge |
| `$09` | `$07` | Capricorn |
| `$0A` | `$08` | Aquarius |
| `$0B` | `$09` | Pisces |
| `$0C` | `$0C` | final-special rose bridge |
| `$0D` | `$0A` | Saga |
| `$0E` | `$00` | post-Saga platform tail; not Mu re-entry |

No progress value selects stable stage `$0B`.

## Dispatcher coverage matrix

Exact bank-5 dispatcher families:

```text
initialization  $97DB / table $97E1
Talk            $9C95 / table $9C9B
post-Bronze     $A361 / table $A367
post-Gold       $A381 / table $A387
```

Gold selection is bank 6 `$9074-$9146`.

| stage | progress | init | Talk | post-Bronze | post-Gold | Gold slots | coverage |
|---:|---:|---:|---:|---:|---:|---|---|
| `$00` | `$00` | `$97F7` | `$9CB7` | `$A3A1` | `$A3A1` | none | closed special context |
| `$01` | `$01` | `$97F8` | `$9D2C` | `$A3A2` | `$A415` | `0,1` | closed |
| `$02` | `$02` | `$981F` | `$9D81` | `$A444` | `$A4CC` | `0,1` | closed composite |
| `$03` | `$03` | `$9851` | `$9D96` | `$A50F` | `$A560` | `0,1` | closed |
| `$04` | `$04` | `$989D` | `$9DD8` | `$A5B3` | `$A63E` | `0,1` | closed |
| `$05` | `$05` | `$9A28` | `$9E1B` | `$A661` | `$A7B3` | `0,1,2` | closed |
| `$06` | `$07` | `$9ACE` | `$9E51` | `$A7FF` | `$A847` | `0,1` | **closed** |
| `$07` | `$09` | `$9ACF` | `$9ED6` | `$A86B` | `$A8D8` | `0,1` | **material context missing** |
| `$08` | `$0A` | `$9B14` | `$9F00` | `$A8FC` | `$A9D3` | `0,1,2` | closed |
| `$09` | `$0B` | `$9B5C` | `$9F99` | `$AA57` | `$AAF0` | `0,1,2` | closed |
| `$0A` | `$0D` | `$9B5D` | `$9FF4` | `$AB18` | `$AC05` | `0,1,2,3` | closed |
| `$0B` | none | `$A960`* | `$9FF4` | `$A3A1` | `$A3A1` | none | structural/transient |

`*` `$A960` is byte two of the real instruction beginning at `$A95F` (`AD 6F 06`) and is never canonically dispatched as a battle initializer.

## Closed dedicated artifacts

```text
00  MuStage00Context
01  TaurusStage01Context
02  GeminiStage02Context
03  CancerStage03Context
04  LeoStage04Context
05  VirgoStage05Context
06  ScorpioStage06Context
08  AquariusStage08Context
09  PiscesStage09Context
0A  SagaStage0AContext
```

Focused documents preserve each context's detailed evidence. Stage `$0C` final-special is closed separately and does not alter the `$00-$0B` denominator.

## Stage `$06` closure — Scorpio / Milo

`BOSS_CONTEXT_STAGE_06_SCORPIO.md` and `ScorpioStage06Context` close the former first gap.

Canonical entry:

```text
$067D=$07
$050E=$06
$06CD=$00
$0673=$30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
```

Initializer `$9ACE` is `RTS`.

Talk `$9E51` consumes the byte-sum `$0677+$0678` via `$A1EC`:

```text
total < 2, Hyoga:
  $068A==0 -> messages $84/$85; INC $068A; +300 Seventh Sense
  $068A!=0 -> same branch, no second reward

total < 2, other Saints:
  messages $F8/$3E; no reward

total >= 2, $066F==0:
  per-Saint $86/$86/$87/$86 + $A3
  INC $066F
  +200 Seventh Sense

total >= 2, $066F!=0:
  repeat high-history dialogue
  Hyoga returns
  Seiya/Shun/Shiryu -> INC $DC -> forced Gold response
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

Dedicated selector `$908C+`:

```text
8-bit dodge sum < 2  -> slot 1
8-bit dodge sum >= 2 -> slot 0
```

Generic `$FF` retry invokes `$A973`, clearing `$066F/$0677/$0678/$068A` and rearming both Talk reward gates.

### Scorpio victory bridge

Scorpio release `$01` first advances to:

```text
$067D=$08
$050E=$10
$06CD=$00
$0673=$30
```

Fixed `$E4D7` maps progress `$08` to **principal platform substate `$02=$08`**. The principal `$00-$0B` platform exit gate is:

```text
X >= $D0
Y == $40
jump phase == 0
State3DReload
```

After reload, fixed `$E2DD` recognizes reconstructed story-stage `$050E=$10` and synthesizes a second release `$01`. Fixed story progression then reaches exact Capricorn:

```text
$067D=$09
$050E=$07
$06CD=$00
$0673=$30
active Saint preserved
```

Important namespace rule: story-stage `$050E=$10` is **not** special-normal platform substate `$02=$10`; the Scorpio bridge uses `$02=$08`.

## Stage `$0B` remains structural

Two facts exclude `$0B` as a missing battle context:

1. `$F016` never returns `$0B`.
2. Immediate `$0B` loads at `$9B31` and `$A16D` feed temporary presentation loader `$F2ED`.

Therefore no dedicated `$0B` battle context is required.

## Remaining material gap

Only stage `$07` Capricorn / Shura remains:

```text
canonical progress $09
init          $9ACF
Talk          $9ED6
post-Bronze   $A86B
post-Gold     $A8D8
Gold slots    0,1
```

Known material surfaces include Shiryu-specific initializer/progression behavior, Talk `$066F` state, `$0690`, a special opponent-defeat sequence ending in release `$FE`, and generic `$FF` defeat. Those internals remain the next checkpoint and are not reopened by the Scorpio closure.

## Executable coverage result

`BattleStageContextCoverage.cs` and `BattleStageContextCoverageChecks.cs` now enforce:

```text
closed dedicated contexts : 00,01,02,03,04,05,06,08,09,0A
material uncovered        : 07
structural/no battle       : 0B
separate closed bridge     : 0C
first material gap         : 07
```

The next canonical stage-local checkpoint is **Capricorn / Shura `$07`**.
