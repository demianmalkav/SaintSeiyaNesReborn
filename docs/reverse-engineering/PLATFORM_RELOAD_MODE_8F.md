# Platform reload selector `$04=$8F`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED for the narrative return produced by state `$89`**.

This document intentionally closes only the `$04=$8F` path reached after the already-promoted special platform/narrative sequence. It is not a general model of `$E100`.

## Entry state

State `$89` reaches bank-1 `$8D4A-$8D54` and writes:

```text
$04 = $8F
$00 = $3D
$01 = $3D
JMP $E100
```

The preceding special sequence also gives one important invariant: state `$72` clears `$03`, and states `$73-$89` do not write it again. Therefore the reload enters with:

```text
$03 = $00
```

## First `$8F` check

Fixed-bank `$E100` begins:

```text
$E100  LDA #$00
$E102  STA $0527
$E105  LDA $04
$E107  CMP #$8F
$E109  BEQ $E10E
$E10B  JSR $C00D
```

Thus the narrative `$8F` return skips the otherwise immediate `$C00D` call and continues through the shared reload initialization.

At `$E121`, the current `$03` indexes table `$E505`:

```text
LDX $03
LDA $E505,X
STA $0533
```

For the confirmed narrative input `$03=$00`, table entry `$E505[0]` is `$00`, so:

```text
$0533 = $00
```

## `$06AB` makes the narrative path deterministic

At `$E13C`:

```text
LDA $06AB
BEQ $E144
JMP $E257
```

A static PRG writer audit finds one direct writer of `$06AB`: `$E165`, which stores `$FF` during the earlier reload initialization. The closed `$70-$89` state sequence does not write `$06AB`.

Therefore the returning narrative path reaches `$E100` with:

```text
$06AB = $FF
```

and necessarily jumps to `$E257`.

This matters because it bypasses the broad cold/generic branch beginning at `$E144`, including the later selectors that inspect `$0670` and `$067D`. Those fields do not choose the destination of the narrative `$8F` return.

## Second `$8F` check

The warm branch starts:

```text
$E257  JSR $DE00
$E25A  LDA $04
$E25C  CMP #$FF
...
$E263  CMP #$8F
$E265  BNE $E26A
$E267  JMP $E20E
```

Because A still contains `$8F`, jumping to `$E20E` stores:

```text
$068F = $8F
```

before calling the shared initialization helper `$F381`.

Renderer/banked initialization performed below `$F381` is outside this bounded destination model. The relevant fact is that control returns to `$E214-$E22C` without consulting `$0670/$067D` for the `$8F` destination.

## `$050E` side effect

At `$E214-$E224`:

```text
LDA $050E
CMP #$0F
BNE $E227
LDA $0533
CMP #$03
BEQ $E227
LDA #$0D
STA $050E
```

The narrative path has `$0533=$00`, so an incoming `$050E=$0F` is normalized to `$0D`. Any other `$050E` value passes through this bounded branch unchanged.

This is a side effect, not a destination-state branch.

## Common commit

After `$E227` the routine reaches:

```text
$E22A  LDA #$00
$E22C  PHA
...
$E235  PLA
...
$E241  STA $01
$E243  STA $00
$E245  LDX $0533
$E248  LDA $E505,X
$E24B  STA $03
$E24D  LDA #$01
$E24F  STA $05
$E251  LDX #$FF
$E253  TXS
$E254  JMP $C180
```

For the narrative return:

```text
A at commit = $00
$0533 = $00
$E505[$00] = $00
```

so the next stable logical handoff is:

```text
$00 = $00
$01 = $00
$03 = $00
$05 = $01
main loop -> $C180
```

The clean-room model promotes the destination state/substate and the `$050E` normalization. PPU disable, stack reset, bank/render initialization and unrelated reload fields remain outside this semantic boundary.

## Clean-room representation

`PlatformNarrative8FReload` accepts only the exact closed narrative-return state:

```text
$04=$8F
$00/$01=$3D
$03=$00
$06AB=$FF
```

and returns the confirmed destination:

```text
engine state     $00
engine mirror    $00
engine substate  $00
intermediate     $0533=$00
selector         $068F=$8F
handoff          $C180
```

It deliberately rejects `$06AB=0` and nonzero entry `$03` rather than pretending those generic `$E100` cases are part of this closed narrative path.
