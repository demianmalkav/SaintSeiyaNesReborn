# Battle-stage context coverage — `$050E=$00-$0B`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

Status: the canonical numeric `$050E=$00-$0B` battle/event namespace is fully classified **and every material stable battle context is now closed**. Dedicated executable contexts exist for `$00-$0A` except `$0B`, which is structural/transient and has no canonical stable battle. Final-special `$0C` remains separately closed outside this denominator.

## Canonical story provenance

Fixed `$F016` maps story progress `$067D=$00-$0E`:

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
| `$08` | `$10` | non-ordinary Scorpio successor bridge |
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
| `$06` | `$07` | `$9ACE` | `$9E51` | `$A7FF` | `$A847` | `0,1` | closed |
| `$07` | `$09` | `$9ACF` | `$9ED6` | `$A86B` | `$A8D8` | `0,1` | **closed** |
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
07  CapricornStage07Context
08  AquariusStage08Context
09  PiscesStage09Context
0A  SagaStage0AContext
```

Stage `$0C` final-special is closed separately and does not alter the `$00-$0B` denominator.

## Stage `$06` Scorpio successor bridge — frozen

Scorpio victory release `$01` does not enter Capricorn directly:

```text
progress $07 / stage $06
  -> release $01
  -> progress $08 / story-stage $10
  -> fixed $E4D7 writes principal platform $02=$08
  -> common exit X >= $D0 / Y=$40 / jump=0
  -> normal $3D/$E100 reload
  -> fixed $E2DD sees reconstructed story-stage $10
  -> synthesized release $01
  -> progress $09 / stage $07 Capricorn
```

Story-stage `$050E=$10` and platform substate `$02=$10` remain distinct namespaces; this bridge uses `$02=$08`.

## Stage `$07` closure — Capricorn / Shura

`BOSS_CONTEXT_STAGE_07_CAPRICORN.md` and `CapricornStage07Context` close the final material row.

Canonical seed:

```text
$067D=$09
$050E=$07
$06CD=$00
$0673=$30
reachable Saints = Seiya / Hyoga / Shun / Shiryu
```

### Fresh initializer reachability

The outer battle-entry owner at `$9770-$97B8` consults `$F36F[$050E]`. Exact table entry:

```text
$F36F[$07]=$03
```

Therefore only a fresh Shiryu entry dispatches `$9ACF`. Seiya/Hyoga/Shun bypass the initializer.

For Shiryu, ordinary `$9ACF`:

```text
temporary presentation $0F
$0672=0
$058A:1->2
$0696:1->2
+600 Seventh Sense
shared release $03 / $068E=1
```

Inbound `$0670=$FE` returns immediately before those mutations.

### Talk / post-action closure

Talk `$9ED6`:

```text
first $066F=0:
  $AE + per-Saint $43/$43/$43/$AF
  INC $066F
  no forced Gold
repeat $066F!=0:
  repeat-form same per-Saint table
  INC $DC
  INC $066F
  forced Gold
```

Post-Bronze `$A86B`:

```text
$EB=$00 + $06BC!=0 -> continue
$EB=$00 + $06BC=0  -> message $8B
$EB=$01 + Shiryu   -> continue, do not arm $0690
$EB=$01 + other    -> $0690=$FF
$EB=$FF             -> $06B1=$FF, +800 Seventh Sense, release $FE
```

Fixed `$FAB9+` gives `$0690` a concrete effect:

```text
$0690!=0 -> force $06BC=0
$0690==0 -> generic battle subsystem keeps ownership of $06BC
```

Post-Gold `$A8D8`:

```text
$EA=$00 -> continue
$EA=$01 -> repeatable $40/$91 feedback
$EA=$FF -> release $FF
```

Gold selection falls through generic parity `$913E`: `slot = $065F & 1`, exactly slots `0,1`.

### Defeat retry does not replay `$9ACF`

Defeat `$FF` keeps progress `$09`; `$E4D7` selects principal platform substate `$09`. Its common accepted exit reaches warm reload `$E100`. `$ED57` invokes `$A973`, clearing encounter-local fields including `$066F/$0670/$068E/$0690`, but the warm continuation then follows:

```text
$E2DD -> $E33D command loop
```

It does **not** pass through `$970A/$97DB/$9ACF` again. Thus `$058A/$0696/$0672` are preserved and there is no duplicate +600 or technique increment on retry.

### Victory release `$FE` -> Aquarius

Fixed `$E3ED-$E414` saves the winning record, forces active Seiya, rewrites `$FE->$01` and joins ordinary progression:

```text
$067D:09->0A
$F016[$0A]=$08
$E50B[$0A]=$08
$050E=$08
$06CD=$08
$0673=$38
$0533=$00 (Seiya)
```

This is the exact already-closed Aquarius story boundary; Aquarius internals remain owned by `AquariusStage08Context`.

## Stage `$0B` remains structural

Two facts exclude `$0B` as a missing battle context:

1. `$F016` never returns `$0B`.
2. Immediate `$0B` loads at `$9B31` and `$A16D` feed temporary presentation loader `$F2ED`.

Therefore no dedicated `$0B` battle context is required.

## Executable coverage result

`BattleStageContextCoverage.cs` and `BattleStageContextCoverageChecks.cs` now enforce:

```text
closed dedicated contexts : 00,01,02,03,04,05,06,07,08,09,0A
material uncovered        : NONE
structural/no battle       : 0B
separate closed bridge     : 0C
```

There is no further stage-local `$00-$0B` checkpoint. Subsequent ORIGINAL SPEC work must be selected from remaining global subsystem gaps rather than inventing another battle-stage gap.
