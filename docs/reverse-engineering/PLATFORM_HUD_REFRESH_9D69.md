# Platform HUD/status writer — `$9D69-$9EED`

Status: **confirmed against the canonical Japanese ROM and modeled as a clean-room four-phase PPU writer**.

This is the downstream presentation owner reached from bank-1 `$9915` when no palette mutation takes the current refresh slot. It is presentation/HUD work only; it does not own platform gameplay simulation, palette selection, audio or RNG.

## Entry and cadence

Canonical entry begins:

```text
$9D69  LDA #$00
$9D6B  STA $2000
$9D6E  LDX $73
$9D70  INX
$9D71  TXA
$9D72  AND #$03
$9D74  STA $73
```

Therefore every invocation:

1. temporarily writes `PPUCTRL=0`;
2. advances `$73 = ($73 + 1) & 3`;
3. dispatches the **new** phase value.

The cadence advances even if the chosen phase later performs no visible data write. In particular phase 3 with platform substate `$02=0` still consumes its cadence slot.

Phase mapping:

```text
$73=0 -> numeric Cosmo/Life + optional Seventh Sense digits
$73=1 -> Cosmo cap/current gauge
$73=2 -> Life cap/current gauge
$73=3 -> optional Seventh Sense gauge
```

The enclosing state-`$20` NMI later restores ordinary `$77/$78/$44/$46` control/mask/scroll state. `$9D69` does not replace that ownership.

## Phase 0 — resource digits

### Cosmo — `$22F0`

The active internal Saint index `$03` is doubled and used against the five two-byte Cosmo records `$63-$6C`.

At `$22F0` the writer emits exactly three digit tiles:

```text
Cosmo hundreds = low nibble of $64 + 2*$03
Cosmo tens/ones = packed BCD byte $63 + 2*$03
```

Digit tile mapping is:

```text
tile = $80 + nibble
```

### Life — `$2330`

Same structure for Life records `$59-$62`:

```text
Life hundreds = low nibble of $5A + 2*$03
Life tens/ones = packed BCD byte $59 + 2*$03
```

### Seventh Sense — `$236F`

The writer always sets PPU address `$236F`, then checks platform substate `$02`.

```text
$02 == 0 -> no Seventh Sense digit data
$02 != 0 -> write $05AB then $05AA as packed-BCD digit pairs
```

Because `$05AA` is the low two digits and `$05AB` the high two digits, visible order is thousands, hundreds, tens, ones.

Helpers:

```text
$9EB8 -> high nibble digit, then falls through to $9EC4
$9EC4 -> low nibble digit
```

## Phase 1 — Cosmo gauge at `$22F4`

The low nibble of current Saint cap byte `$6D+$03` is the exclusive Cosmo-hundreds boundary already established by the resource model.

The routine performs two passes at the same PPU address:

1. clear the full cap width with tile `$A7`;
2. overwrite each completed Cosmo hundred with tile `$BF`, then append one partial tile selected from current packed BCD `$63 + 2*$03`.

Thus the cap width and current fill are mechanically distinct.

## Phase 2 — Life gauge at `$2334`

Identical structure using:

```text
cap segments  = high nibble of $6D+$03
full segments = low nibble of Life-hundreds byte $5A + 2*$03
partial value = packed BCD Life low-two byte $59 + 2*$03
```

Empty/fill tiles remain `$A7/$BF`.

## Resource partial-tile classifier — `$9E84-$9EB7`

The comparison is raw-byte ordered; canonical callers supply packed BCD `00-99`.

| packed BCD range | tile |
|---|---:|
| `$00-$04` | `$A7` |
| `$05-$24` | `$B1` |
| `$25-$36` | `$B2` |
| `$37-$49` | `$B3` |
| `$50-$61` | `$B4` |
| `$62-$74` | `$B5` |
| `$75-$86` | `$B6` |
| `$87-$99` | `$BF` |

The exact threshold bytes are `$05,$25,$37,$50,$62,$75,$87`.

## Phase 3 — Seventh Sense gauge at `$2374`

The entire phase is suppressed when `$02=0`; no PPU address/data write occurs after the initial `PPUCTRL=0`, but `$73` has already advanced.

For `$02!=0` the gauge has a fixed ten-segment background:

1. write ten `$A7` empty tiles at `$2374`;
2. reset address to `$2374`;
3. write one `$BE` full tile for each thousands digit (`high nibble of $05AB`);
4. write one partial tile from the remaining hundreds+tens fraction.

The ones digit is deliberately ignored for the gauge.

Fraction construction exactly matches `$9E49-$9E59`:

```text
fraction = ((low nibble of $05AB) << 4)
         | (high nibble of $05AA)
```

The high nibble of `$05AA` is also stored in scratch `$39` before the combination.

The fraction first uses the same `$9E84` classifier. Seventh Sense then transforms the partial tile family:

```text
$A7 -> $A7
$B1 -> $B8
$B2 -> $B9
$B3 -> $BA
$B4 -> $BB
$B5 -> $BC
$B6 -> $BD
$BF -> $BE
```

So `$BE` is both a completed thousand segment and the clamped full partial segment.

## Repeated-tile helpers

Three adjacent helpers share the same loop body:

```text
$9E73 -> repeat `$A7` Y times
$9E77 -> repeat `$BF` Y times
$9E80 -> repeat `$BE` Y times
```

A zero count bypasses the helper at each canonical callsite, avoiding the 8-bit `DEY` wrap behavior that would occur if entered with zero.

## Fixed PPU-address helpers

```text
$9ECD -> $22F4
$9ED8 -> $2334
$9EE3 -> $2374
```

Phase-0 digit addresses are written inline:

```text
Cosmo          $22F0
Life           $2330
Seventh Sense  $236F
```

## Clean-room implementation

`PlatformHudRefresh9D69` models:

- unconditional `PPUCTRL=0`;
- modulo-four post-increment cadence;
- phase-0 packed-BCD digit streams;
- Life/Cosmo cap-width and fill passes;
- exact `$9E84` threshold classifier;
- phase-3 fixed ten-segment Seventh Sense gauge;
- `$39` scratch write used by the fraction builder;
- exact semantic PPU address/data operation order.

The self-test fixtures discriminate threshold boundaries, address order, phase advancement, resource-cap semantics, Seventh Sense digit gating and the phase-3 no-write branch.

## Scope boundary

Closed here:

```text
$9915 no palette work
 -> $9D69 cadence advance
 -> one HUD/status phase
 -> bounded helpers through $9EED
 -> return to enclosing $9915/NMI presentation flow
```

Outside this checkpoint: `$9915` palette/CHR ownership, main-thread gameplay simulation, text rendering outside this HUD slice, full audio, RNG, non-platform NMI states and original nametable/graphics payloads.
