# Canonical pseudo-random / phase source — `$E0AC`, `$065F/$0660`

Status: **confirmed against the canonical Japanese ROM and promoted as a clean-room bank-context-sensitive source contract**.

This subsystem is smaller than the downstream battle/platform mechanics that consume it. The closure therefore models only the source recurrence, its invocation/bank context, reset/seed owners and consumer masks/ranges. Existing battle/dodge/stage behavior remains frozen.

## Update owner and invocation gate

There is one executable `JSR $E0AC` in the canonical PRG image:

```text
$E09C  JSR $E0AC
```

It lies inside mirror-state `$01=$3D -> $E000` NMI service. `$E0AC` is not called by ordinary state-$20` platform NMI or by the common `$D269` routes.

The full `$E000` service reaches `$E09C` only when the entry gate passes:

```text
$9C != 0
($9D | $9E | $A0) == 0
```

If `$9C==0`, or any of `$9D/$9E/$A0` is nonzero, `$E000` takes the short `$E01D` path and returns without updating `$065F/$0660`.

Therefore `$0660` counts successful `$E0AC` updates modulo 256. It is not a generic frame counter.

## Exact recurrence

Canonical fixed-bank code:

```text
E0AC  LDX $0660
E0AF  LDA $94F0,X
E0B2  CLC
E0B3  ADC $065F
E0B6  STA $065F
E0B9  INC $0660
E0BC  RTS
```

For the PRG bank visible at `$8000-$BFFF` when `$E0AC` runs:

```text
source = visible_prg[$94F0 + $0660]
$065F' = ($065F + source) & $FF
$0660' = ($0660 + 1) & $FF
```

`$94F0-$95EF` is exactly 256 bytes, so every byte value of `$0660` indexes one source byte and `INC` naturally wraps `$FF->$00`.

## `$94F0` is bank-sensitive

MMC1 is in 16 KiB PRG mode with `$8000-$BFFF` switchable. `$94F0` therefore does not identify one physical table by itself.

`$E0AC` runs before `$E0BD` restores persistent battle/reload bank `$0639`. Temporary queue services in `$E02C-$E08A` can alter the visible bank and deliberately leave it visible through `$E09C`.

If mapper service is available (`$063E!=$04` and `$063F!=$04`), bank ownership immediately before `$E0AC` is priority-ordered by the last serviced queue:

```text
entry visible bank
 -> if $0641!=0: bank 0 when $068F==$8F, otherwise bank 6
 -> if $0526!=0: bank 6
 -> if $0538!=0: bank 5
 -> if $057D!=0: bank 6
 -> $E0AC reads $94F0,X from the final resulting bank
```

Later service wins because there is no intervening PRG restoration.

If `$063E==$04` or `$063F==$04`, helper `$E0E4` suppresses these temporary services; `$E0AC` then consumes the PRG bank already committed/visible on NMI entry. The model therefore accepts that visible bank as explicit state rather than guessing from `$0639` during an interrupted MMC1 transaction.

After the update, `$E0BD+` performs its own work and, when mapper service is available, restores `$0639`. That restoration is subsequent to the random-source fetch and cannot retroactively define its table.

A ROM-fed audit hashes the seven physical `$94F0-$95EF` switchable-bank windows instead of exporting their bytes. The hashes differ, confirming that substituting one fixed source table for all contexts would be incorrect.

## Initialization and indirect seeds

The two source bytes have more than one canonical initialization owner.

### Cold RESET `$C13D+`

RESET clears `$0000-$06FF`, therefore:

```text
$065F=00
$0660=00
```

### Bank-0 `$AD4A+`

This initializer first clears all `$0500/$0600`, then writes `$01` across `$0648-$06AB`:

```text
AD4D/AD50  clear $0500,X / $0600,X for X=00..FF
AD56       LDA #$01
AD5A       STA $0648,X for X=00..63
```

Both source fields lie inside that fill range:

```text
$065F = $0648+$17 = 01
$0660 = $0648+$18 = 01
```

So this path seeds `(01,01)`, not `(00,00)`.

### Bank-1 `$959D+`

`$959D` clears `$0500/$0600` for all X and leaves these fields zero. It is reached from fixed `$C206` after mapping bank 1.

### Bank-0 `$AF0D`

The failure/reset branch inside the `$AEB5` family clears all `$0600,X`, also producing `(00,00)`.

### Bank-0 indexed `$0648,Y` seed

`$B38A` writes into `$0648,Y`. Canonical placement calculations produce `Y=$17` and `Y=$18` for two entries, which alias `$065F` and `$0660`. This is a real indirect seed/write path and is explicitly represented; it is not discarded merely because the instruction does not spell the absolute RNG addresses.

Other nearby `$0600,Y` builders are bounded below these offsets or use short fixed ranges and do not reach `$065F/$0660`. The raw bank-2 byte occurrence containing `5F 06` at `$8158` is data, not an executable absolute load/store.

## Executable consumers

Direct absolute-reference audit plus caller-context disassembly yields these real consumers.

### `$065F`

```text
$E33D  AND #$07 -> selector $00-$07 stored at $05AC

$EC18  AND #$01 -> $00-$01
$EC20  AND #$01; +2 -> $02-$03
$EC2B  AND #$03 -> $00-$03

$F65D  AND #$01 -> narrow two-way encounter selector
$F665  AND #$03 -> four-way encounter selector

$FAC9  AND #$0F; compare #$05
$FAD7  AND #$0F; compare #$06
        -> low-nibble threshold gate; values below threshold force result zero

bank6 $913E  AND #$01 -> binary stage/event selector
bank6 $92E2  AND #$03 -> four-entry pointer selector
```

### `$0660`

```text
$F995  AND #$01
        even -> $FF
        odd  -> $01
```

This supplies the previously unresolved parity source used by the closed dodge danger-direction path.

No other direct executable read of `$0660` exists outside the updater and `$F995`. No other direct absolute writer exists outside `$E0B6/$E0B9`; the additional seed/reset owners above are bulk/indexed RAM writes and are separately classified.

## Clean-room implementation

`CanonicalRandomSourceE0AC` exposes:

- full-service invocation gate;
- temporary-bank resolver through `$E09C`;
- exact 8-bit recurrence and index wrap;
- explicit reset/seed owners;
- bank-0 indexed alias seeding;
- consumer masks `$01/$03/$07/$0F`;
- low-nibble threshold helper;
- stage-five split selector;
- narrow/four-way encounter selectors;
- `$0660` parity-to-`$FF/$01` direction mapping.

The source table is supplied externally as a 256-byte window for the resolved visible bank. Original ROM source bytes are never embedded in source or fixtures.

## Scope boundary

Closed here:

```text
$01=$3D NMI full-service eligibility
 -> temporary PRG bank context
 -> $E0AC source byte
 -> $065F/$0660 recurrence
 -> canonical seed/reset ownership
 -> direct consumer masks/ranges/parity
```

Not reopened here: battle selection consequences, dodge mechanics, stage context logic, platform rendering, audio, or probability redesign. `$3C` and other deterministic counters remain separate from this source.
