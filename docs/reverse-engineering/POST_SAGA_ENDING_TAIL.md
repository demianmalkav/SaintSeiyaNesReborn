# Post-Saga ending tail and hard terminal

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED end-to-end from the Saga victory boundary to the original game's hard terminal**.

This checkpoint composes already-closed subsystems rather than reopening them. The only newly reversed control surface is the `$8F` diversion inside `$F381` and the bank-0 final presentation at `$BC39-$BD2F`.

## 1. Exact Saga victory boundary

`SagaStage0AContext` already proves the final phase-2 victory path:

```text
opponent defeated
 -> release $01
 -> $067D: $0D -> $0E
 -> $050E=$00
 -> $06CD=$00
 -> $0673=$30
 -> release rewritten to $05
 -> bootstrap $00
 -> global bootstrap successor $20
```

The winning active Saint is Seiya (`$0533=$00`).

This remains the **first and only** post-Saga `$00->$20` bootstrap in the canonical ending path.

## 2. Progress `$0E` selects final platform substate `$11`

The fixed progress-to-platform table at `$E4D7/$E4E0` maps:

```text
$067D=$0E -> $02=$11
```

`PLATFORM_MAP_KITS.md` already identifies `$11` as the special two-page platform map.

The physical exit is already owned by `PlatformExitGate`:

```text
substate $11
player X >= $D0
player Y == $50
jump phase $49 == 0
```

When accepted, bank 1 `$96FD-$9713` does **not** use the ordinary `$3D` reload. It seeds:

```text
$00 = $70
$26 = $00
$27 = $00
$57 = $C0
```

and does not request the normal Saint snapshot.

## 3. Reused special narrative chain

No new semantics are introduced here. The existing executable specifications remain authoritative:

```text
PlatformPostExitStateMachine
  $70 -> $71 -> $72 -> $73 -> $74 -> $75 -> $80

PlatformNarrative80To89StateMachine
  $80 -> $81 -> $82 -> $83 -> $84
      -> $85 -> $86 -> $87 -> $88 -> $89
```

State `$89` produces the exact reload handoff:

```text
$04 = $8F
$00 = $3D
$01 = $3D
$03 = $00
JMP $E100
```

The post-Saga fixture walks these existing machines in order; it does not duplicate their countdown, text or renderer logic.

## 4. Correct `$8F` reload destination

The earlier bounded `$8F` model stopped too early and incorrectly assumed that `$F381` returned to `$E214`, allowing the common `$00/$01=$00 -> $C180 -> $20` commit.

The deeper control-flow trace proves that assumption false.

`$E257` recognizes `$04=$8F` and jumps to `$E20E`:

```text
$E20E  STA $068F       ; A=$8F
$E211  JSR $F381
```

Inside `$F381`, selector `$068F=$8F` installs ending-specific fields:

```text
$06CD = $20
$0673 = $20
$06CC = $21
```

Then:

```text
$F3B7  LDA #$00
$F3B9  JSR $E589       ; MMC1 PRG bank writer
$F3BC  JMP $BC39
```

`A=$00` selects PRG bank 0 in `$8000-$BFFF`, and `$F3BC` is a tail jump. It never returns to `$E214`.

Consequences:

- there is **no second bootstrap** `$00->$20`;
- `$E214-$E224` cannot normalize `$050E`;
- `$E22A-$E254` cannot write `$00/$01=$00`;
- `$C180` is not reached again;
- the `$8F` path enters the ending directly at bank-0 `$BC39`.

This correction is encoded in `PlatformNarrative8FReload`.

## 5. Bank-0 final presentation `$BC39-$BD2F`

The ending routine is a linear presentation driver. Its relevant semantic state is a ten-entry stream sequence selected through `$BDA0`.

`$BDA0` starts from the base pointer stored at `$BDDC/$BDDD`:

```text
$BDDC-$BDDD = $BDDE
```

It adds `2 * index`, loads a 16-bit stream pointer and sets `$0641=$01` to mark the stream active.

The ten pointers are:

| index | pointer |
|---:|---:|
| 0 | `$BDF2` |
| 1 | `$BE14` |
| 2 | `$BE48` |
| 3 | `$BE81` |
| 4 | `$BEB1` |
| 5 | `$BEE9` |
| 6 | `$BEFD` |
| 7 | `$BF3B` |
| 8 | `$BF71` |
| 9 | `$BF87` |

`$BC39-$BD2D` invokes these indices in strict ascending order `0..9`.

For streams 0 through 7, `$BD32` enables the presentation update flag `$E7=1` and repeatedly calls the existing presentation helper while polling `$0641` until the stream completes. Streams 8 and 9 also wait for `$0641=0` before advancing, but use their local polling loops.

PPU writes, palette timing and stream-token interpretation are presentation internals and are deliberately not promoted as gameplay state.

## 6. True original terminal

After stream 9 completes:

```text
$BD2A  LDA $0641
$BD2D  BNE $BD2A
$BD2F  JMP $BD2F
```

The main thread therefore enters a self-loop at `$BD2F`.

There is no ROM control-flow edge from this terminal back to:

- gameplay;
- platform state `$20`;
- `$E100` reload;
- `$C180` bootstrap;
- state `$50` front-end/title;
- any software restart path.

Normal NMI activity may still occur according to console interrupt semantics, but the main-thread continuation remains `$BD2F -> $BD2F`. The canonical game does not automatically return to its title/front-end after the ending; leaving this terminal requires external reset/power semantics.

## 7. Complete canonical chain

```text
Saga phase-2 victory
 -> $067D=$0E / release $05
 -> bootstrap $00->$20
 -> progress map selects platform $02=$11
 -> accepted gate ($D0+,$50,jump=0)
 -> $70->$71->$72->$73->$74->$75
 -> $80->$81->$82->$83->$84->$85->$86->$87->$88->$89
 -> $04=$8F / $00=$01=$3D / $E100
 -> $068F=$8F
 -> $F381 ending branch
 -> $06CD/$0673/$06CC=$20/$20/$21
 -> PRG bank 0
 -> $BC39
 -> presentation streams 0..9
 -> $BD2F: JMP $BD2F
 -> hard terminal until external reset/power
```

Every reachable transition downstream of the proven Saga victory now has a known owner.

## Clean-room representation

`PostSagaEndingTail` provides:

- validation/composition of the exact `SagaVictoryBoundary`;
- progress `$0E` -> platform substate `$11` join;
- reuse of `PlatformExitGate` and `PlatformPostExitStateMachine` for the special exit;
- reuse of `PlatformNarrative80To89StateMachine` and corrected `PlatformNarrative8FReload`;
- exact ten-stream pointer order;
- explicit bank-0 `$BC39` entry and `$BD2F` hard-terminal boundary.

The model intentionally does not reproduce the NES renderer, palette choreography or ending stream bytecode. Those are audiovisual implementation detail unless later ORIGINAL SPEC coverage explicitly requires them.
