# Platform narrative reload mode `$04=$8F`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED through the bank-0 ending diversion**.

This document covers only the `$04=$8F` path produced by the already-closed `$80-$89` narrative sequence. It is not a generic model of `$E100`.

## Entry state

State `$89` reaches bank-1 `$8D4A-$8D54` and writes:

```text
$04 = $8F
$00 = $3D
$01 = $3D
JMP $E100
```

The preceding `$70-$89` sequence also guarantees `$03=$00` because state `$72` clears it and no later state in that chain rewrites it.

Persistent `$06AB=$FF` is inherited from the established lifecycle; the `$70-$89` sequence does not write it.

## `$E100` warm branch

`$E100` first handles common reload setup. At `$E121`, `$03=$00` indexes `$E505`, producing:

```text
$0533 = $00
```

At `$E13C`, `$06AB=$FF` forces the warm branch at `$E257`.

The relevant dispatch is:

```text
$E257  JSR $DE00
$E25A  LDA $04
$E25C  CMP #$FF
$E263  CMP #$8F
$E265  BNE $E26A
$E267  JMP $E20E
```

Because `$04=$8F`, control jumps to `$E20E`.

## Critical correction: `$F381` does not return on `$8F`

The fixed path at `$E20E` is:

```text
$E20E  STA $068F       ; A is still $8F
$E211  JSR $F381
$E214  ...             ; common commit only if $F381 returns
```

A deeper trace of `$F381` resolves the previously bounded ambiguity.

When `$068F=$8F`:

```text
$F38D  LDA $068F
$F390  CMP #$55
$F394  CMP #$8F
...
$F398  LDA #$20
$F39A  STA $06CD
$F39D  STA $0673
$F3A0  LDA #$21
$F3A2  STA $06CC
...
$F3B0  LDA $068F
$F3B3  CMP #$8F
$F3B5  BNE $F3BF
$F3B7  LDA #$00
$F3B9  JSR $E589
$F3BC  JMP $BC39
```

`$E589` is the MMC1 PRG-bank writer. Input `A=$00` selects PRG bank 0 into the switchable `$8000-$BFFF` window. `$F3BC` then performs a **tail jump** to bank-0 CPU `$BC39`.

There is no `RTS` back to `$E214` on this path.

Therefore the following operations are **unreachable** for the `$8F` narrative return:

- `$E214-$E224` `$050E` normalization;
- `$E227` common destination setup;
- `$E22A-$E254` state-zero commit;
- `$00/$01=$00`;
- return to `$C180`;
- any supposed second bootstrap `$00->$20`.

The exact logical boundary is instead:

```text
$04   = $8F
$00   = $3D
$01   = $3D
$03   = $00
$0533 = $00
$068F = $8F
$06CD = $20
$0673 = $20
$06CC = $21
PRG bank = 0
PC -> $BC39
```

`$050E` remains whatever value entered this bounded `$8F` path because the former normalization code is never reached.

## Clean-room representation

`PlatformNarrative8FReload` now models this exact diversion. It reports:

- the still-live `$3D/$3D` engine state at diversion time;
- `$0533=$00`;
- `$068F=$8F`;
- ending setup `$06CD/$0673/$06CC=$20/$20/$21`;
- PRG bank 0;
- terminal continuation address `$BC39`;
- `ReturnsToMainLoopC180 = false`.

The bank-0 sequence itself is owned by `PostSagaEndingTail` and `POST_SAGA_ENDING_TAIL.md`.
