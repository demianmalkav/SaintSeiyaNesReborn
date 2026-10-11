# Platform `$00=$20` NMI presentation boundary

Status: **confirmed against the canonical Japanese ROM and modeled as a clean-room semantic phase**.

Scope is deliberately limited to the platform NMI branch:

```text
$C000 -> $D269
$D2BA JSR $D7F2
$D2BD JSR $D988
$D2C0 JMP $D367
```

Main-thread player/entity simulation remains owned by the existing platform frame model. This document describes the presentation boundary only.

## Common NMI prologue — `$D269+`

The interrupt first:

1. pushes A/X/Y;
2. writes `$3A=1`, marking any protected MMC1 transaction as interrupted;
3. resets the MMC1 serial latch through `INC $FFFF`;
4. reads `$2002`;
5. writes `$2003=0`;
6. writes `$4014=$07`, performing DMA from the already-built OAM shadow `$0700-$07FF`;
7. only then dispatches the high-level engine state.

Therefore OAM DMA is a snapshot boundary: platform main-thread writes that occur after this interrupt are not visible to the current DMA and become eligible for a later NMI.

For `$00=$20`, the dispatcher executes `$D7F2`, then `$D988`, then the common `$D367` epilogue.

## `$D7F2` is the platform background streamer

The previous broad label “visual refresh” was incomplete. Direct ROM flow separates two mutually exclusive paths using horizontal scroll `$44` and latch `$03A3`.

### Refresh path

If:

```text
($44 & $06) != 0
```

then `$03A3=0` and the NMI temporarily maps PRG bank 1, calls `$9915`, then temporarily maps bank 3 again.

If `$44` is aligned such that bits 1-2 are clear but `$03A3!=0`, it also performs the same bank-1 `$9915` call without clearing the latch.

The bank switches use raw `$C0B4`; they do not replace persistent `$3B` ownership.

`$9915` belongs to the platform visual/palette/CHR refresh path. It is not entity simulation and is not duplicated by `PlatformState20NmiPhase`.

### New tile-column path

When:

```text
($44 & $06) == 0
$03A3 == 0
```

`$D7F2` sets `$03A3=$FF` and uploads one vertical nametable column.

It writes:

```text
$2000 = $77 | $04
```

which temporarily enables PPU increment-by-32 without modifying RAM mirror `$77`.

The target nametable address is:

```text
high = $20 + (((($45 + 1) & 1)) << 2)
low  = $44 >> 3
```

Then exactly 22 bytes are copied:

```text
$0362-$0377 -> $2007
```

This is the visible 22-tile vertical column buffer.

## `$D844` attribute-column extension

After a new tile column, `$D844` runs. It returns immediately unless:

```text
($44 & $1E) == 0
```

On the 32-pixel boundary it restores ordinary PPUCTRL from `$77` and uploads eight attribute bytes `$0378-$037F` at explicit addresses.

Address family:

```text
high = $23 + (((($45 + 1) & 1)) << 2)
low0 = $C0 + ($44 >> 5)
lowN = low0 + 8*N, N=0..7
```

Thus `$D7F2/$D844` is a deterministic background-streaming boundary: tile column first, attribute column only on the coarser boundary.

## `$D988` is the platform pause toggle

Canonical flow `$D988-$D9B9` uses Start bit `$10` in controller state `$3D` with edge/debounce byte `$05`.

```text
Start released:
    $05=0

Start held and $05!=0:
    no second toggle

fresh Start edge ($05==0):
    $05=$FF
    JSR $CB6A
    toggle $0386
```

`$0386` is the real pause gate. Main-thread platform code at `$C302-$C305` checks it before the normal platform simulation:

```text
$0386 != 0 -> skip normal platform frame work
$0386 == 0 -> continue normal player/entity/platform work
```

When entering pause from `$0386=0`, `$D988` additionally runs `$DB9C` and queues audio cue `$63` through `$DBB6`, then stores `$0386=$FF`.

When leaving pause, `$CB6A` still runs but the additional pause-entry reset/cue path is skipped and `$0386=0` is stored.

### Dormant debug tail is not canonical gameplay

After the pause logic `$D988` reads fixed ROM byte `$FFDE`.

Canonical value:

```text
$FFDE = $00
```

so the routine returns immediately at `$D9B9`.

The later `$D9BA+` code that can mutate Saint resources and force state `$3D` is therefore unreachable in the canonical ROM. It must not be promoted as normal gameplay behavior.

## Common epilogue — `$D367-$D39D`

After `$D7F2/$D988`, the NMI commits presentation state.

### PPUCTRL nametable bit

Bit 0 of mirror `$77` is replaced by camera page bit 0:

```text
$77 = ($77 & $FE) | ($45 & 1)
$2000 = $77
```

### PPUMASK and scroll

```text
$2001 = $78
$2005 = $44
$2005 = $46
```

The two `$2005` writes are the horizontal and vertical scroll commit.

### Status wait

The loop uses `BIT $2002` with A=`$40` and repeats while bit 6 remains set. Mechanically, the NMI waits for the PPUSTATUS sprite-zero-hit bit to clear.

### Persistent PRG restoration

Finally:

```text
A = $3B
JSR $C0B4
```

restores the persistent PRG bank after temporary NMI bank changes. `$C0B4` is intentionally raw here: `$3B` is already the persistent owner.

Y/X/A are restored and the handler returns with `RTI`.

## Clean-room model

`PlatformState20NmiPhase` models:

- mapper-interruption mark and MMC1 reset ordering;
- `$2003=0` and `$4014=$07` DMA before state-specific work;
- immutable snapshot of the supplied 256-byte OAM shadow;
- `$D7F2` refresh-vs-column arbitration and `$03A3` ownership;
- 22-byte nametable column and optional 8-byte attribute column;
- temporary PRG bank 1 -> `$9915` -> bank 3 route;
- Start-edge debounce and `$0386` pause toggle;
- canonical `$FFDE=0` dead debug tail;
- final `$77/$78/$44/$46` PPU commit;
- sprite-zero-hit-clear wait;
- persistent `$3B` restore and interrupt return boundary.

The model deliberately emits `$9915`, `$CB6A`, and audio reset/cue activity as semantic operations rather than re-simulating their already-separate subsystems.

## Scope boundary

Closed here:

```text
platform main-thread OAM shadow
 -> NMI DMA
 -> state20 background/pause work
 -> final PPU control/mask/scroll commit
 -> persistent mapper restore
 -> RTI
```

Not closed here:

- every other `$00/$01` NMI state;
- complete palette semantics inside bank-1 `$9915`;
- cycle-accurate PPU timing;
- full audio engine semantics;
- unrelated mapper users.

No ROM, OAM dump or extracted graphics are committed.
