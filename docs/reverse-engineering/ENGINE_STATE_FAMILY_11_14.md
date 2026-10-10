# Engine state family `$11-$14` — choice, password output and terminal display

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED / executable semantic model**.

This document closes the low engine-state family entered from the already-promoted normal-reload stable state `$10`. The family is not a single timer chain. It is a cooperative main/NMI state machine with a user-choice fork:

```text
promoted reload stable $10
  -> $C180 short bootstrap
  -> $D442 INC $00
  -> $11

$11 choice
  upper/default route -> $3D -> already-promoted $E100 reload
  lower route         -> bank-0 $AE18 password generation -> $12

$12 --next NMI--> $13
$13 --password text stream--> $14
$14 --normal engine--> $14  (absorbing)
```

The model deliberately omits PPU tile writes, audio commands and the internals of the password codec. Those already belong to `PASSWORD_SYSTEM.md` and other renderer/audio boundaries.

## 1. Entry from verified reload `$10`

PR #109 established the fixed-bank bootstrap:

```asm
C190  LDA $00
C192  CMP #$10
C194  BEQ $C20E
...
C20E  JSR $C458
C211  JSR $D442
```

`$C458` stages durable state at `$0110+`, including the four serializable Saint records and progression fields used by the password encoder.

`$D442` begins:

```asm
D442  INC $00             ; $10 -> $11
D444  LDA $02
D446  CMP #$0C
D448  BCC $D44C
D44A  LDA #$0C
D44C  STA $06             ; $06 = min($02,$0C)
...
D480  STA $03AA           ; text offset = 0
D483  LDA #$BE
D485  STA $58             ; default/upper cursor row
```

The main dispatcher later executes `$C220 STA $01`, so the dispatch-ready state is `$00/$01=$11`.

The normal reload common commit at `$E24D-$E24F` left `$05=$01`. State `$11` uses that byte as a **Start-release latch**: a held Start from the previous context cannot immediately activate a menu choice.

## 2. State `$11`: input-driven two-row choice

The dedicated main body is `$C246-$C2A8`.

Controller bits used by the logical state are:

```text
$3D bit $08 = Up
$3D bit $04 = Down
$3D bit $10 = Start
```

`$C249-$C273` permits vertical selection only when `$02!=0`:

```text
upper row = $58=$BE
lower row = $58=$CE
```

Up has priority if Up and Down are simultaneously set because the Down test is reached only after the Up test fails.

### Start-release latch `$05`

At `$C275` the state tests Start.

If Start is **not** pressed, execution reaches:

```asm
C2A9  LDA #$00
C2AB  STA $05
```

Therefore one Start-released frame clears the inherited `$05=1`.

If Start remains pressed while `$05!=0`:

```asm
C27B  LDA $05
C27D  BNE $C2AD
```

the state does not advance. This is a clean edge guard against a held Start.

## 3. `$11` upper/default choice -> `$3D` reload

After the latch is clear, Start branches to the reload route when either:

- `$02==0`; or
- `$58<$C8`.

Because the reachable cursor values are `$BE/$CE`, the second condition means the upper row `$BE`.

The route is:

```asm
C2B8  LDA #$00
C2BA  STA $04
C2BC  JSR $CA94
C2BF  LDA #$3D
C2C1  STA $00
C2C3  STA $01
...
C2CB  JMP $E100
```

`$CA94` maps bank 1 and calls `$951F`, refreshing the Saint snapshot before the reload.

This branch therefore leaves the `$11-$14` family at an **already promoted destination**:

```text
$11 -> $3D -> $E100
```

No new reload semantics are required here.

## 4. `$11` lower choice -> password generator -> `$12`

With `$02!=0`, lower row `$58=$CE`, Start released previously and then pressed:

```asm
C289  STA $05             ; latch selected cursor ($CE)
C28B  LDA #$6B
C28D  JSR $DBB6
C290  LDA #$00
C292  JSR $C0B4           ; map bank 0
C295  JSR $AE18
C298  LDA #$12
C29A  STA $00
C29C  STA $01
C29E  LDA #$23
C2A0  STA $15
C2A2  LDA #$08
C2A4  STA $14
```

Bank-0 `$AE18` is the already-documented password encoder/output preparation routine. `PASSWORD_SYSTEM.md` proves that it:

1. packs the durable snapshot staged at `$0110+`;
2. produces the 31-symbol password including checksum and obfuscation;
3. maps symbols to display glyphs;
4. writes a dynamic display stream at `$0600`;
5. terminates that stream with `$FF` at `$AEB1`.

Thus state `$12` is specifically the presentation entry for a freshly generated password; this conclusion does not rely on UI-string guessing.

## 5. State `$12`: one-NMI transition

Main `$12` belongs to the generic low-state `$9363` route and has no logical state writer.

NMI has an exact `$12` case:

```asm
D29E  CMP #$12
D2A0  BNE ...
D2A2  JSR $D543
D2A5  INC $00
D2A7  INC $01
D2A9  JMP $D367
```

`$D543` performs presentation-only PPU writes. No persistent logical control field is changed before the paired increment.

Therefore:

```text
$12 -> $13
```

on the first NMI that observes `$12`. It is an NMI-transitional state rather than an input-driven frame state.

## 6. State `$13`: dynamic password-text stream

Main `$13` again uses bank-1 `$9363`; that routine has no `$13` transition.

NMI `$13` reaches `$D42D`:

```asm
D42D  LDA #$F0
D42F  STA $07FC
D432  LDA #$01
D434  JSR $C0B4           ; map bank 1
D437  LDA #$0D
D439  JSR $8D5A
D43C  LDA #$03
D43E  JSR $C0B4
D441  RTS
```

At `$8D5A`, selector `$0D` resolves through the bank-1 pointer table to `$0600`. `$03AA`, initialized to zero during `$D442`, is the stream cursor.

Ordinary/control symbols keep engine state `$13`; the exact rendering/token grammar remains delegated to the existing text-engine boundary.

When the generated `$0600` stream reaches its `$FF` terminator:

```asm
8D93  CMP #$FF
8D95  BEQ $8DDB
...
8DDB  INC $00             ; $13 -> $14
8DDD  INC $01             ; mirror -> $14
8DDF  LDA #$80
8DE1  STA $57
8DE3  LDX #$01
8DE5  STA $26             ; $80
8DE7  DEX
8DE8  STA $27             ; $80
```

Therefore `$13` is **text-driven**, not a fixed-frame timer state.

## 7. State `$14` is absorbing in the normal engine

State `$14` is real and reachable, but there is no subsequent normal state transition.

Main dispatcher:

- `$14 < $15`, so it enters the low family;
- it is not `$11`, so `$C2A9` clears `$05`;
- it calls bank-1 `$9363`;
- `$9363` changes `$00` only for exact state `$97`, never `$14`;
- main then returns through the common tail.

NMI dispatcher:

- `$14` matches none of the dedicated cases;
- it falls directly to the common NMI tail `$D367`;
- that tail does not write `$00/$01`.

A bounded scan of executable global state writers likewise finds no state-$14-specific escape path.

Consequently:

```text
$14 -> $14
```

for every normal main/NMI iteration. Controller input is not even sampled by the `$14` main route. Escape therefore requires reset/power-cycle or another external restart path outside this normal engine family.

This is the correct closure of the password branch: the completion criterion does **not** require inventing an out-of-family state when the ROM implements an absorbing terminal screen.

## Persistent logical writes

| Event | Persistent logical effects relevant after the branch |
|---|---|
| `$10->$11` bootstrap | `$00=$11`, `$06=min($02,$0C)`, `$03AA=0`, `$58=$BE`; main mirrors `$01=$11` |
| Start released in `$11` | `$05=0` |
| upper/default Start | `$04=0`, refresh Saint snapshot, `$00/$01=$3D` |
| lower Start | `$05=$CE`, generate `$0600` password stream, `$00/$01=$12`, `$15=$23`, `$14=$08` |
| `$12` NMI | `$00/$01=$13` |
| `$13` stream terminator | `$00/$01=$14`, `$57=$80`, `$26=$80`, `$27=$80` |
| ordinary `$14` main | `$05=0`; no state transition |

`$14/$15` after password entry are text/PPU cursor fields. Their later token-by-token mutations do not select another engine state and are therefore not modeled as global control state.

## Executable model

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineState11To14Machine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineState11To14MachineChecks.cs`

The model composes with `EngineStateDispatcherMap`; it does not duplicate the top-level dispatcher.

## Closed graph

```text
                               Start upper / $02=0
                          +--------------------------> $3D / $E100
                          |
$10 -> $11 --release Start--+
          |               |
          | Up/Down       +--Start lower--> $AE18 -> $12 -> $13 -> $14
          |                                                password    ^
          +---------------- remain $11                    stream ------+
                                                                      |
                                                         normal loop --+
```

The `$11-$14` family is therefore closed semantically for the path reachable from promoted reload `$10`.
