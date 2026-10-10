# Boss context stage `$07` — Capricorn / Shura

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Canonical ROM identity:

- size `262160` bytes;
- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`;
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`;
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`;
- CRC32 `F8D258A3`.

Status: **closed dedicated executable context**. This is the final material stage-local gap in the canonical `$050E=$00-$0B` battle/event denominator.

## Canonical story entry

The already-closed Scorpio successor bridge terminates at:

```text
$067D=$09
$F016[$09]=$07 -> $050E=$07
$E50B[$09]=$00 -> $06CD=$00
$0673=$30
```

The `$30` selection marker permits Seiya/Hyoga/Shun/Shiryu and excludes Ikki.

Stage-local owners:

```text
initialization  $9ACF
Talk            $9ED6
post-Bronze     $A86B
post-Gold       $A8D8
Gold selector   generic parity $913E
```

## 1. Entry gate: `$9ACF` is Shiryu-only on a fresh canonical entry

The stage initializer is not unconditionally invoked for every active Saint. The outer bank-5 battle-entry owner checks `$068E` and the fixed designated-Saint table before falling into the stage dispatcher:

```text
9770  LDA $068E
9773  BNE $9780
9775  LDX $050E
9778  LDA $F36F,X
977B  CMP $0533
977E  BEQ $97B8
```

The exact table beginning at `$F36F` is:

```text
FF 00 02 03 00 FF FF 03 01 FF FF FF FF
```

Therefore:

```text
$F36F[$07] = $03 = Shiryu
```

A fresh Seiya/Hyoga/Shun entry bypasses `$9ACF`; it receives no Capricorn initializer reward or technique mutation. A fresh Shiryu entry dispatches `$9ACF`.

This outer gate is essential to the meaning of `$9B06/$9B09`: persistent Shiryu count `$058A` and active count `$0696` are incremented coherently from the canonical Shiryu value `1` to `2`.

## 2. Initializer `$9ACF`

Exact material flow:

```text
9ACF  LDA $0670
9AD2  CMP #$FE
9AD4  BNE $9AD7
9AD6  RTS

9AD7  JSR $9C6D
9ADA  LDA #$02
9ADC  STA $0514
9ADF  LDA #$0F
9AE1  JSR $F2ED          ; temporary presentation stage $0F
9AE4  LDA #$20
9AE6  JSR $E726
9AE9  LDA #$04
9AEB  STA $0514
9AEE  JSR $EBDB
9AF1  JSR $EB39
9AF4  JSR $E761
9AF7  LDA #$B1
9AF9  JSR $E7C7
9AFC  LDA #$00
9AFE  STA $0672
9B01  LDA #$B0
9B03  JSR $E7B7
9B06  INC $058A
9B09  INC $0696
9B0C  LDA #$06
9B0E  JSR $F31E          ; +600 Seventh Sense
9B11  JMP $9C3D
```

Shared `$9C3D` emits the internal intro handoff:

```text
$0670=$03
$068E=1
restore real stage $050E=$07
```

Canonical Shiryu effect:

```text
$058A: 1 -> 2
$0696: 1 -> 2
$0672 = 0
+600 Seventh Sense
release $03
```

The first `CMP #$FE` is a real handler guard: if `$9ACF` is reached while release `$FE` is inherited, it returns before presentation, reward or technique growth.

## 3. Talk `$9ED6`

Exact flow:

```text
9ED6  LDA #$AE
9ED8  JSR $E7B7
9EDB  LDA $066F
9EDE  BEQ $9EEF

; repeat
9EE0  LDX $0533
9EE3  LDA $9EFC,X
9EE6  JSR $E7C3
9EE9  INC $DC
9EEB  INC $066F
9EEE  RTS

; first
9EEF  LDX $0533
9EF2  LDA $9EFC,X
9EF5  JSR $E7C7
9EF8  INC $066F
9EFB  RTS

9EFC  43 43 43 AF
```

Semantics:

```text
first Talk ($066F=0)
  -> message $AE
  -> per-Saint $43/$43/$43/$AF
  -> $066F:0->1
  -> no forced Gold response

repeat Talk ($066F!=0)
  -> same semantic dialogue table through repeat display helper
  -> INC $DC
  -> INC $066F
  -> fixed caller forces Gold response
```

`$066F` is an incrementing byte counter, not a strict boolean latch.

## 4. Post-Bronze `$A86B`

Exact control:

```text
A86B  JSR $ACD6
A86E  LDA $EB
A870  CMP #$01
A872  BEQ $A886
A874  CMP #$FF
A876  BEQ $A893

; $EB=$00
A878  LDA $06BC
A87B  BNE $A885
A87D  JSR $ADC4
A880  LDA #$8B
A882  JSR $E7B3
A885  RTS

; $EB=$01
A886  LDA $0533
A889  CMP #$03
A88B  BEQ $A885
A88D  LDA #$FF
A88F  STA $0690
A892  RTS

; $EB=$FF
A893  ... scripted victory ...
A8C9  LDA #$FF
A8CB  STA $06B1
A8CE  LDA #$08
A8D0  JSR $F31E          ; +800 Seventh Sense
A8D3  LDA #$FE
A8D5  JMP $ACAA
```

Closed branches:

```text
$EB=$00 + $06BC!=0 -> continue
$EB=$00 + $06BC=0  -> message $8B; continue

$EB=$01 + Shiryu   -> continue; do not arm $0690
$EB=$01 + other    -> $0690=$FF; continue

$EB=$FF
  -> scripted victory
  -> $06B1=$FF
  -> +800 Seventh Sense
  -> release $FE
```

## 5. `$0690` has an executable fixed-bank consequence

`$0690` is not an inert narrative flag. Fixed battle setup `$FAB9-$FAE5` begins:

```text
FAB9  LDA $0690
FABC  BEQ $FAC2
FABE  LDA #$00
FAC0  BEQ $FAE2
...
FAE2  STA $06BC
FAE5  RTS
```

Therefore nonzero `$0690` **forces `$06BC=0`** before the generic hit-token path can supply a nonzero value.

The dedicated model composes this as an override only:

```text
$0690!=0 -> $06BC=0
$0690==0 -> preserve generic battle subsystem result
```

No generic hit calculation is duplicated in the Capricorn context.

## 6. Post-Gold `$A8D8`

Exact branches:

```text
A8D8  JSR $AD4D
A8DB  LDA $EA
A8DD  CMP #$01
A8DF  BEQ $A8E6
A8E1  CMP #$FF
A8E3  BEQ $A8F7
A8E5  RTS

A8E6  ... message $40 ... message $91 ... RTS

A8F7  LDA #$FF
A8F9  JMP $ACAA
```

Semantics:

```text
$EA=$00 -> continue
$EA=$01 -> repeatable low-player feedback $40/$91
$EA=$FF -> release $FF defeat
```

No stage-local first-low latch is consulted.

## 7. Gold selector

Capricorn has no dedicated branch. It reaches generic parity at `$913E`:

```text
913E  LDA $065F
9141  AND #$01
9143  STA $0680
```

Reachable slots are exactly `0,1`.

## 8. Defeat retry does **not** repeat the +600/unlock

This was the main unresolved lifecycle question.

Defeat `$FF` does not advance progress `$09`. Fixed `$E4D7` therefore selects:

```text
$E4E0[$09] = $09
-> principal platform substate $02=$09
```

Principal substate `$09` uses the established common exit gate:

```text
X >= $D0
Y == $40
jump phase == 0
-> State3DReload / $E100
```

The warm-reload path is materially different from a fresh battle entry:

```text
E100 ...
E13C  LDA $06AB
E13F  BEQ $E144
E141  JMP $E257          ; warm path skips fresh story setup
...
E279  JSR $DFCB
E27C  JSR $ED57
```

`$ED57` sees inherited release `$FF`, clears `$0690`, and calls common reset `$A973`:

```text
A973 clears:
$DC/$066F/$0670/$0677/$0678/$064D/$064E/$067C/$068A/$068E/$0690/$06B8/$DD
```

Crucially, after reset stage `$07` is not story-stage `$10` and release is zero:

```text
E2DD  LDA $050E
E2E0  CMP #$10           ; false for $07
...
E2E4  LDA $0670
E2E7  BEQ $E33D          ; direct command-loop resume
```

The retry path **never calls `$970A/$97DB/$9ACF` again**.

Consequences:

```text
$066F reset to 0
$0690 reset to 0
$068E reset to 0
$0670 reset to 0
$058A preserved
$0696 preserved
$0672 preserved
no second +600 reward
no second technique increment
```

Thus the canonical Shiryu unlock remains `1 -> 2`; intentional defeats cannot grow it to 3/4.

## 9. Scripted victory release `$FE` -> Aquarius boundary

Fixed terminal owner `$E3ED-$E414` handles `$FE` specially:

```text
E3ED  JSR $F2E4
...
E3F7  LDA $0670
E3FE  CMP #$FE
E400  BNE $E3EA
E402  LDX #$00
E404  LDY $0533
E407  JSR $FB9F          ; save winning Saint record
E40A  LDA #$00
E40C  STA $0533          ; force Seiya
E40F  LDA #$01
E411  STA $0670          ; rewrite FE -> ordinary progression release 01
E414  JMP $E3B3
```

From progress `$09`:

```text
$067D:09->0A
$F016[$0A]=$08
$E50B[$0A]=$08
$050E=$08
$06CD=$08
$0673=$38
$0533=$00 (Seiya)
```

This is the exact already-closed Aquarius/final-Camus story boundary. `CapricornStage07Context` stops here and references `AquariusStage08Context` only as the successor identity; Aquarius internals are not reopened.

## 10. Executable artifact and closure

Artifacts:

- `src/SaintSeiyaNesReborn.OriginalSpec/CapricornStage07Context.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CapricornStage07ContextChecks.cs`
- `docs/reverse-engineering/BOSS_CONTEXT_STAGE_07_CAPRICORN.md`
- promoted `BattleStageContextCoverage` and coverage fixtures

Closed assertions include:

- exact progress/stage/descriptor/roster seed;
- `$F36F[$07]=$03` Shiryu-only fresh initializer dispatch;
- `$FE` initializer guard;
- +600 and Shiryu technique `1->2`;
- first/repeat Talk counter behavior;
- every `$EB/$EA` stage-local branch;
- Shiryu exception versus non-Shiryu `$0690` arming;
- fixed `$FAB9` forced `$06BC=0` consequence;
- parity Gold slots `0/1`;
- `$FF` principal-platform `$09` retry and no initializer replay;
- +800 victory, `$06B1=$FF`, release `$FE`;
- forced-Seiya exact Aquarius boundary.

After this promotion the canonical `$050E=$00-$0B` denominator contains **zero material stage-local gaps**. `$0B` remains structural/transient rather than becoming a fictitious missing battle.
