# Platform post-exit narrative states `$80-$89`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by static ROM flow** for NMI ownership, script selection, state progression and final `$3D/$E100` handoff.

This is the direct continuation of `PLATFORM_POST_EXIT_STATE_MACHINE.md`. The special platform sequence reaches `$80` from state `$75` with `$57=$20`; from that point progression is owned by NMI text machinery rather than by a dedicated main-thread state handler.

## Main-thread behavior

The fixed main loop begins with:

```text
$C21E  LDA $00
$C220  STA $01
```

States `$80-$89` do not match a dedicated main-thread branch in the confirmed dispatcher. They fall through to generic frame housekeeping.

Therefore the only narrative-state effect promoted for the main thread is the normal `$00->$01` mirror. State advancement itself belongs to NMI.

## NMI dispatch

After the earlier special cases, fixed NMI dispatch reaches:

```text
$D2F8  LDX $00
...
$D304  CPX #$73
$D306  BEQ $D30C
$D308  CMP #$80       ; high-nibble value from prior AND #$F0
$D30A  BNE ...
$D30C  ...
$D311  JSR $8C19      ; bank 1 text/NMI handler
```

Thus `$80-$8F` uses the same bank-1 text handler family as state `$73`.

## `$8C19` pre-script countdown

`$8C19` first inspects `$57`.

If `$57 != 0`, it decrements it. Most values simply return after setup/render work. A semantically relevant edge occurs when the decremented value becomes `$01`: the `$8CC4-$8CF2` setup path ends by writing:

```text
$26 = $01
```

When `$57` later reaches zero, the **following NMI** enters `$8D28`, whose first action is:

```text
DEC $26
```

Only when `$26` becomes zero does script/state processing continue.

This is why the clean model retains `$57` and the pre-script `$26` gate even though PPU/cursor setup is omitted.

## Script-pointer table

State `$73` uses the first pointer at `$911C`:

```text
$73 -> $8F9C
```

For states `$80-$88`, `$8D6E-$8D7B` computes `(state - $7F) * 2` and indexes the same table starting at `$911E`.

| engine state | script pointer |
|---:|---:|
| `$80` | `$8FC4` |
| `$81` | `$8FEF` |
| `$82` | `$901A` |
| `$83` | `$9047` |
| `$84` | `$9070` |
| `$85` | `$9081` |
| `$86` | `$90A3` |
| `$87` | `$90CA` |
| `$88` | `$90F1` |

All nine mapped scripts contain a terminating `$FF` token.

The clean-room runtime exposes these pointers as evidence metadata. It does **not** reproduce character/tile upload semantics.

## Script termination: `$80-$88`

When the active script reaches `$FF`, bank 1 `$8DDB-$8DE8` executes:

```text
INC $00
INC $01
LDA #$80
STA $57
STA $26
STA $27
```

So every completed narrative script performs exactly one state increment and seeds the next state's delay/pacing fields:

```text
$80 -> $81
$81 -> $82
$82 -> $83
$83 -> $84
$84 -> $85
$85 -> $86
$86 -> $87
$87 -> $88
$88 -> $89
```

with:

```text
$57 = $80
$26 = $80
$27 = $80
```

The model represents the renderer-owned token stream with one explicit semantic input: `scriptTerminatorReached`. Intermediate character pacing is intentionally outside the gameplay state model.

## State `$89`: no tenth script

Once `$57/$26` have crossed the same pre-script gate, `$8D28` checks the engine state before pointer selection:

```text
$8D35  CMP #$89
$8D37  BCS $8D4A
```

State `$89` therefore does not select another pointer. Instead `$8D4A-$8D54` performs:

```text
$04 = $8F
$00 = $3D
$01 = $3D
JMP $E100
```

This reconnects the narrative chain to the already-promoted normal reload boundary.

The complete closed chain is therefore:

```text
$75
 -> $80
 -> $81
 -> $82
 -> $83
 -> $84
 -> $85
 -> $86
 -> $87
 -> $88
 -> $89
 -> $04=$8F, $00/$01=$3D
 -> $E100
```

## Clean-room representation

`PlatformNarrative80To89StateMachine` provides:

- exact state `$80-$88` script-pointer mapping;
- `StepMainStateOnly(...)` for the confirmed main `$00->$01` mirror with no invented narrative transition;
- `StepNmi(...)` for `$57` countdown, `$26` gate, semantic script termination and state `$89` reload conversion;
- an explicit `ReloadE100` flag on the final `$89` result.

The model intentionally stops again at `$E100`. The broad reload routine's later selection of the next gameplay/narrative destination is a separate boundary and remains the next candidate for promotion.
