# Engine state family `$91-$99`

Status: **CONFIRMED** for the reachable high family entered from the promoted stable reload state `$90` in the canonical Japanese ROM.

Scope: logical `$00/$01` progression, the family timer/index fields that gate that progression, and the dynamic text source that proves entry. PPU/OAM presentation, audio and unrelated renderer internals are excluded.

## Entry from promoted reload `$90`

PR #109 established the fixed bootstrap:

```text
stable reload $90
 -> $C180 short bootstrap
 -> $D442 INC $00
 -> live state $91
```

`$D442` also clamps platform substate `$02` into family field `$06`:

```text
$06 = min($02,$0C)
```

When the incremented state is `$91`, `$D454-$D462` maps bank 0, calls `$AE18`, and seeds text coordinates:

```text
bank0 $AE18 -> generate $FF-terminated stream at $0600
$15=$23
$14=$08
```

`$AE18` is the same already-documented password/text output builder used by the lower `$11-$14` family. No second codec is introduced here.

For semantic fixtures, entry is treated as dispatch-ready `$00/$01=$91`; ordinary main `$C21E-$C220` synchronizes mirror `$01` from live `$00` before dispatch.

## `$91 -> $92`: generated text terminator

Main state `$91` uses bank-1 `$9363` only for presentation and contains no family-state writer.

NMI `$D31C-$D328` detects live `$91` and calls `$D42D`:

```text
D42D  LDA #$F0
D42F  STA $07FC
D432  map bank 1
D437  LDA #$0D
D439  JSR $8D5A
```

Bank-1 pointer table entry `$911A/$911B` for source selector `$0D` is exactly `$0600`. Therefore state `$91` consumes the stream generated at bootstrap.

Bank-1 `$8D5A` processes the stream. Its terminal `$FF` reaches:

```text
8DDB  INC $00
8DDD  INC $01
8DDF  LDA #$80
8DE1  STA $57
8DE5  STA $26
8DE8  STA $27
8DEA  LDA $00
8DEC  CMP #$92
8DEE  BNE $8DF2
8DF0  STA $57
```

Starting from `$91`, the terminator therefore commits:

```text
$00/$01 = $92
$57      = $92
$26/$27  = $80
```

The `$57=$92` value is a state-$92-specific override after the generic `$80` seed.

## State `$92`: countdown, then A-button gate

Main dispatcher routes `$92` to dedicated `$C3C3`.

If `$57!=0`:

```text
C3C3  LDA $57
C3C5  BEQ $C3CC
C3C7  DEC $57
C3C9  JMP common-main
```

A frame that decrements `$57` from `1` to `0` still returns immediately. Input is not polled until the following main frame.

When `$57==0`, `$C3CC` calls `$C4E4`, which serializes controller 1 into `$3D`. The established NES serial order makes bit `$80` the A button; the previously promoted Start bit is `$10`.

The gate is:

```text
C3CF  LDA #$80
C3D1  BIT $3D
C3D3  BEQ $C3EE
```

Thus state `$92` waits at timer zero until A is pressed. On A:

```text
C3D5-C3E7 presentation-only writes
C3E8  INC $00
C3EA  LDA #$10
C3EC  STA $57
```

Main had already mirrored `$00=$92` into `$01` at `$C220`, so the immediate result is:

```text
live $00 = $93
mirror $01 = $92
$57 = $10
```

The mirror catches up on the next main frame.

## `$93-$96`: four one-NMI presentation states

The main dispatcher routes `$93-$98` through bank-1 `$9363`; for `$93-$96` that body performs presentation only and contains no state writer.

NMI owns the progression:

```text
$93: D32F -> D55E -> D56E INC $00 -> $94
$94: D33D -> D571 -> D55E -> D56E -> $95
$95: D345 -> D571 -> D55E -> D56E -> $96
$96: D34D -> D571 -> D55E -> D56E -> $97
```

`$D56E` increments only live `$00`; it does not update mirror `$01`. Therefore each immediate post-NMI transition retains the previous frame mirror until main `$C220` synchronizes it.

No logical timer gates these four states. They are one-observing-NMI transitions.

## State `$97`: family-index countdown

Bank-1 `$9363` carves out exactly `$97`:

```text
9369  CMP #$97
936B  BCC presentation
936D  CMP #$98
936F  BCS presentation
9371  DEC $57
9373  BNE presentation
9375  LDA #$30
9377  STA $57
9379  INC $06
937B  LDA $06
937D  CMP #$0D
937F  BCC presentation
9381  INC $00
```

`$57` entered this later phase as `$10`, seeded by state `$92`. While nonzero it decrements once per main frame. On expiry it reloads to `$30` and increments `$06`.

The bootstrap had established `$06=min($02,$0C)`, and no reachable `$91-$96` body writes zero-page `$06`. Therefore state `$97` walks `$06` upward until the new value reaches `$0D`.

Terminal expiry from `$06=$0C` gives:

```text
$06=$0D
$57=$30
live $00=$98
mirror $01=$97
```

Again, `$9381` increments only live state; the next main frame synchronizes the mirror.

## `$98 -> $99`: one NMI

Main `$98` again uses `$9363` presentation only.

NMI has a dedicated `$98` case:

```text
D359-D362 presentation-only
D365  INC $00
```

So the immediate transition is:

```text
live $00=$99
mirror $01=$98
```

No `$01` write occurs in that NMI.

## State `$99`: absorbing terminal state

Main high-family dispatcher explicitly excludes `$99` from the `$93-$98` bank-1 path:

```text
C3F5  CPX #$99
C3F7  BCS $C3FC
```

Thus `$99` falls to the common main tail. NMI has no `$99` case and falls to `$D367`.

The family-specific writers are exhausted at `$D365`; neither common top-level path contains a `$99` progression writer. The next main frame mirrors live `$99` into `$01`, after which normal execution remains:

```text
$99 -> $99 -> ...
```

Escape requires reset/external restart rather than another normal engine-state transition. This mirrors the terminal behavior already established for `$14`, although the presentation leading to it is different.

## Complete reachable graph

```text
promoted reload $90
  -> bootstrap $91 + build $0600 stream

$91 --$0600 terminator $FF--> $92

$92 --count $57:$92 -> $00--> wait for A
$92 --A ($3D bit $80)-------> $93, $57=$10

$93 --next NMI--> $94
$94 --next NMI--> $95
$95 --next NMI--> $96
$96 --next NMI--> $97

$97 --each $57 expiry--> $06++, $57=$30
$97 --new $06=$0D-----> $98

$98 --next NMI--> $99
$99 -------------> $99 ...
```

No reachable alternate member outside `$91-$99` is produced by this family after entry.

## Persistent logical fields

| field | role in this family |
|---|---|
| `$00` | live engine state |
| `$01` | main-frame mirror; intentionally lags transitions that increment only `$00` |
| `$02` | bootstrap source for `$06` |
| `$06` | clamped family index; state `$97` increments it until `$0D` |
| `$14/$15` | text cursor seeded `$08/$23` at `$91` entry |
| `$26/$27` | text-engine fields seeded `$80` on `$91->$92` |
| `$3D` | controller byte; state `$92` tests A mask `$80` |
| `$57` | `$92` countdown, then `$10/$30` timing for state `$97` |
| `$0600+` | generated `$FF`-terminated text/password stream consumed by `$91` |

## Executable artifact

- `src/SaintSeiyaNesReborn.OriginalSpec/EngineState91To99Machine.cs`
- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/EngineState91To99MachineChecks.cs`

The model deliberately exposes mirror lag at the state writers that only increment `$00`; fixtures then compose those results with the existing global dispatcher rather than silently forcing `$01=$00`.

## Evidence status

- `$90->$91` bootstrap: **CONFIRMED** by PR #109 and this family trace.
- `$91->$92` through dynamic `$0600` `$FF`: **CONFIRMED**.
- `$92` countdown and A-button gate: **CONFIRMED**.
- `$93->$94->$95->$96->$97` one-NMI chain: **CONFIRMED**.
- `$97` timer/index gate and `$98` writer: **CONFIRMED**.
- `$98->$99`: **CONFIRMED**.
- `$99` absorbing under normal main/NMI execution: **CONFIRMED**.
