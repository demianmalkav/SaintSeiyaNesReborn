# Boss context — stage `$06` Scorpio / Milo

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

This document closes canonical battle stage `$050E=$06` and composes its victory into the already-closed principal platform machinery until the exact Capricorn boundary. It does not reopen generic battle arithmetic, platform physics or Capricorn internals.

## 1. Canonical entry

Fixed story tables select Scorpio at progress `$07`:

```text
$067D=$07
$F016[$07]=$06 -> $050E=$06
$E50B[$07]=$00 -> $06CD=$00
$0673=$30
```

The fixed Saint mask therefore permits exactly:

```text
$0533=0 Seiya
$0533=1 Hyoga
$0533=2 Shun
$0533=3 Shiryu
```

Ikki is excluded.

The stage-local owners are:

```text
init          $9ACE
Talk          $9E51
post-Bronze   $A7FF
post-Gold     $A847
Gold selector $908C+
```

`$9ACE` is exactly `RTS`; Scorpio has no stage-specific initializer reward or presentation handoff.

## 2. Dodge-history helper `$A1EC`

```text
A1EC LDA $0678
A1EF CLC
A1F0 ADC $0677
A1F3 RTS
```

Both Talk and the Scorpio Gold selector consume this 8-bit sum. As on the 6502, overflow wraps modulo 256.

## 3. Talk `$9E51`

### Total dodge history below two

With `$0677+$0678 < 2`, Hyoga has a dedicated branch:

```text
messages $84 / $85
if $068A==0:
    INC $068A
    #$03 -> $A1FF -> +300 Seventh Sense
else:
    no second reward
```

This branch neither sets `$066F` nor raises transient `$DC`.

For Seiya/Shun/Shiryu at the same low history:

```text
messages $F8 / $3E
no persistent mutation
no reward
no forced Gold response
```

### Total dodge history at least two

First high-history Talk (`$066F==0`) uses the per-Saint table at `$9ED2`:

```text
Seiya  $86
Hyoga  $86
Shun   $87
Shiryu $86
```

followed by message `$A3`, then:

```text
INC $066F
#$02 -> $A1FF -> +200 Seventh Sense
```

It does not force a Gold response.

Repeated high-history Talk (`$066F!=0`) repeats the Saint-specific text plus `$A3`. Hyoga returns directly. Seiya/Shun/Shiryu execute `INC $DC`, so the fixed Talk caller forces the Gold-response path.

The two rewards are independent gates: `$068A` owns Hyoga's low-history +300; `$066F` owns the first high-history +200.

## 4. Post-Bronze `$A7FF`

After generic battle helpers `$ADC4/$ACD6`:

```text
$EB=$FF
  -> victory presentation
  -> release $01

otherwise
  $06BC!=0 -> message $A6; continue
  $06BC==0 -> continue without Scorpio-local feedback
```

Unlike several earlier stages, `$EB=$01` has no distinct Scorpio terminal or latch. Only `$FF` is terminal here.

## 5. Post-Gold `$A847`

After generic player classifier `$AD4D`:

```text
$EA=$00 -> continue
$EA=$01 -> messages $A4 / $91; continue
$EA=$FF -> release $FF defeat
```

The low-player branch is repeatable. Scorpio does not own a Cancer-style one-shot `$064D` latch.

## 6. Gold selector `$908C-$90A5`

The dedicated branch reuses the same 8-bit dodge-history sum:

```text
total < 2  -> Gold slot 1
total >= 2 -> Gold slot 0
```

Exactly slots `0,1` are reachable.

## 7. Generic defeat retry

A `$FF` defeat does not advance progress `$07`. On normal re-entry, common reset `$A973` clears:

```text
$066F
$0670
$0677
$0678
$064D
$064E
$067C
$068A
$068E
$0690
$06B8
```

Therefore Scorpio retry starts again with dodge history zero, `$066F=0` and `$068A=0`. Both Talk reward gates are rearmed, including Hyoga's +300 branch.

## 8. Victory is a two-release bridge to Capricorn

Scorpio's `$A7FF` victory release `$01` does **not** enter Capricorn directly.

Fixed story progression first advances:

```text
$067D:07->08
$F016[08]=$10
$E50B[08]=$00
$050E=$10
$06CD=$00
$0673=$30
active Saint preserved
```

Here `$050E=$10` is a **story-stage/presentation value**. It must not be confused with special-normal platform substate `$02=$10`.

Fixed `$E4D7` indexes the progress-to-platform table with `$067D=$08` and writes:

```text
$02=$08
```

Thus this bridge uses principal platform substate `$08`.

The already-closed principal platform exit gate for `$02=$08` is the common `$00-$0B` endpoint:

```text
X >= $D0
Y == $40
jump phase == 0
transition = State3DReload
```

After the normal `$3D/$E100` reload, progress `$08` reconstructs story-stage `$050E=$10`. Fixed `$E2DD` tests that stage and synthesizes another release `$01`:

```text
E2DD LDA $050E
E2E0 CMP #$10
E2E2 BEQ $E2D5
...
E2D5 LDA #$01
E2D7 STA $0670
E2DA JMP $E386
```

The second fixed release progression then reaches:

```text
$067D:08->09
$F016[09]=$07
$E50B[09]=$00
$050E=$07
$06CD=$00
$0673=$30
active Saint preserved
```

This is the exact Capricorn / Shura boundary. Capricorn internals `$9ACF/$9ED6/$A86B/$A8D8` remain outside this checkpoint.

## 9. Executable artifact

`ScorpioStage06Context` models:

- exact progress `$07` seed and reachable roster;
- no-op `$9ACE` initializer;
- low/high dodge-history Talk split;
- independent `$068A` +300 Hyoga reward and `$066F` +200 milestone;
- repeated high-history non-Hyoga forced Gold response;
- post-Bronze/post-Gold terminals;
- dedicated dodge-history Gold slot selection;
- `$FF` retry reset/rearm;
- victory release `$01` into progress `$08/$050E=$10`;
- fixed `$E4D7` platform `$08` mapping;
- principal platform gate composition;
- `$E2DD` auto-release `$01`;
- exact progress `$09/$050E=$07` Capricorn handoff.

`ScorpioStage06ContextChecks` supplies discriminating fixtures for all of those branches, including 8-bit dodge-sum wrap and rejected/accepted platform gate coordinates.

Stage `$06` is therefore `DedicatedContextClosed`. The only remaining material battle-stage context in `$00-$0B` is stage `$07` Capricorn / Shura.
