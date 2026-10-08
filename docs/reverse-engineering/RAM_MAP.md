# RAM map — first static pass

Target: verified Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM documented in `CANONICAL_ROM.md`.

This file distinguishes what the original code itself proves from semantic names still awaiting runtime confirmation.

## CONFIRMED structures

### `$0059-$0062` — five 2-byte numeric slots

The code indexes this range as `2 * $03`, so it is structurally an array of five two-byte values. Arithmetic routines treat the two bytes as decimal digits packed into nibbles and perform explicit decimal correction after subtraction.

External cheat documentation identifies these values as the five Saints' Cosmo values. Static code strongly agrees with that interpretation.

Provisional semantic name: `saint_cosmo_bcd[5]`.

Evidence anchors:

- bank 1 `$9299+` selects `2 * $03` and subtracts two units from the `$59/$5A` pair with decimal-nibble correction;
- bank 1 `$951F` snapshots all five pairs into `$058C-$05A4`;
- bank 1 `$9720` restores them;
- UI code in bank 1 reads the same array for display.

### `$0063-$006C` — five 2-byte numeric slots

Same five-character / two-byte layout as `$0059-$0062`. The game performs packed-decimal arithmetic and converts the current character's value for gameplay use.

External cheat documentation identifies these values as Life/Energy. Static behavior supports that interpretation.

Provisional semantic name: `saint_life_bcd[5]`.

Evidence anchors:

- bank 1 `$8616` uses `2 * $03` and reads `$63/$64` for the selected Saint;
- `$9311+` decrements the pair with manual BCD correction;
- `$951F` snapshots all five values;
- `$9720` restores them.

### `$006D-$0071` — five one-byte per-Saint values

`$951F/$9720` snapshot and restore one byte per Saint alongside Cosmo and Life. The semantic meaning is not yet established.

Name remains `saint_unknown_6d[5]`.

### `$0076` — timer-like gameplay state

Initialized to zero by bank 1 `$98BA`. Collision/gameplay paths set it to `$20` and other code tests it while processing interactions.

Community cheats call this invulnerability/flashing state. Static behavior proves that it is a countdown/cooldown-like state associated with interaction handling; exact visible semantics still need runtime confirmation.

Provisional semantic name: `invulnerability_timer`.

### `$020A` — controller held-state byte

Bank 0 `$8B1F` strobes `$4016`, shifts eight controller reads into `$020A`, and retains the resulting current-button bitfield.

Provisional semantic name: `input_held`.

### `$020B` — controller newly-pressed byte

The same routine computes `(current XOR previous) AND current` and stores it in `$020B`.

Provisional semantic name: `input_pressed`.

### `$0203-$0204` — monotonically incremented 16-bit counter

Incremented once at the end of the controller polling routine, with carry from `$0203` to `$0204`.

Likely a frame/update counter, but runtime cadence should be confirmed before assigning the final name.

### `$03` — selected/current Saint index candidate

Multiple routines use `$03`, double it, and index the five-entry Cosmo/Life arrays. The observed valid structure strongly implies values `0..4` select one of the five playable Saints.

Provisional semantic name: `current_saint_index`.

### `$058C-$05A4` — 25-byte Saint-stat snapshot

Bank 1 `$951F` copies all five Saints' Cosmo pairs, Life pairs, and one additional byte per Saint into this region. `$9720` performs the inverse restore.

This is a persistent/snapshot representation distinct from the active zero-page arrays.

### `$05AA-$05AB` — packed-decimal four-digit stat

The fixed bank explicitly extracts four nibbles from these two bytes and converts them into four display tiles. `$F31E` adds a packed-decimal amount to `$05AB` with decimal correction and a `$99` clamp before refreshing the display.

Community documentation identifies the value as Seventh Sense. Combined evidence is strong enough to use the provisional name `seventh_sense_bcd`, pending runtime observation of acquisition/use.

### `$0700-$07FF` — OAM shadow page

NMI writes `$07` to `$4014`, causing sprite DMA from page `$0700`. Multiple routines also write sprite-related values throughout this page.

## Mapper/NMI coordination

### `$3A`

NMI writes `1` here before resetting the MMC1 shift register. Interrupt-safe MMC1 writers clear/test it and retry a five-write serial transaction if an NMI occurred during the transaction.

Provisional name: `mmc1_write_interrupted`.

### `$3B`

Used by the protected PRG-bank writer and reloaded by NMI before returning. This tracks the PRG bank that should be restored after NMI performs temporary bank switches.

Provisional name: `persistent_prg_bank`.

## High-level state candidates

### `$00/$01`

RESET initializes both to `$50`. NMI dispatches large branches of engine behavior from these values, including exact states and state families (`$40`, `$60`, `$70`, `$80`, `$90` ranges).

They are confirmed as high-level engine/state-machine bytes; their division of responsibility remains unknown.

### `$067D`

Incremented when a progression/event sequence completes. Used as an index into fixed-bank tables and compared against `$0C`. Strong candidate for temple/story progression index.

Status: `INFERRED`.

### `$06CD`

Derived from a table indexed by `$067D`, then used to construct another state value at `$0673`. Likely a stage/event descriptor linked to progression.

Status: `INFERRED`.

## Serialization/password staging candidate

Fixed-bank routine `$C458` copies:

- `$058C-$059F` -> `$0110-$0123`;
- `$05AA-$05AC` -> `$0130-$0132`;
- `$067D` -> `$0133`;
- `$06CD` -> `$0134`;
- `$0587-$058A` -> `$0135+`.

The collection consists largely of durable player/progression state and therefore is a strong candidate for a password/save serialization staging buffer. This remains `INFERRED` until callers and downstream encoder are fully traced.

## External battle-mode candidates not yet promoted

Community cheats additionally identify `$05BC/$05BD` as battle Cosmo and `$05CE/$05CF` as battle Life, with `$05BE` / `$05D0` related maxima. These have not yet been statically tied to sufficient callers in this pass and remain `INFERRED` pending tracing.

## Next tests

1. Runtime-watch `$03`, `$59-$71` while changing playable Saint.
2. Deliberately lose Life/Cosmo and verify the BCD decrement paths.
3. Observe `$76` through a received hit and the flashing period.
4. Trace `$C458` forward to prove or reject password serialization.
5. Trace `$067D/$06CD` across temple completion.
6. Map `$05BC-$05D0` during Gold Saint battles and reconcile them with the active per-Saint arrays.
