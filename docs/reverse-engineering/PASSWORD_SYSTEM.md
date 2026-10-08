# Password system — static reverse engineering

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the encode/decode pipeline is now statically isolated end-to-end. Individual semantic fields are marked separately where their meaning still needs runtime confirmation.

## Overview

The password is a 31-symbol code selected from a 10-column × 7-row kana grid. The program does not save a raw memory dump. It:

1. stages durable game state into `$0110-$0138`;
2. packs that state into 22 bytes at `$06CC+`;
3. converts those 22 bytes into 30 six-bit symbols;
4. appends an XOR checksum as symbol 31;
5. obfuscates a 10-symbol window selected by the checksum low nibble;
6. maps the six-bit values to Japanese display glyphs;
7. performs the exact inverse path when a password is entered.

Publicly documented passwords contain 31 kana, independently matching the code path and the input buffer length.

## State staging — fixed bank `$C458`

`$C458` prepares the durable state used by the password encoder:

- `$058C-$059F` -> `$0110-$0123` (20 bytes)
- `$05AA` -> `$0130`
- `$05AB` -> `$0131`
- `$05AC` -> `$0132`
- `$067D` -> `$0133`
- `$06CD` -> `$0134`
- `$0587-$058A` -> `$0135-$0138`

The first 20 bytes are four five-byte Saint records. The active gameplay snapshot actually contains a fifth five-byte slot at `$05A0-$05A4`, but **that fifth slot is not serialized into the password**. This is important architectural evidence: the game maintains five active Saint slots, while only four are normal persistent/password characters. The fifth slot is therefore likely a special/transient character slot; exact identity remains to be proven from character-selection logic before assigning a final name.

## Four persistent Saint records

Bank 0 `$AF19` consumes `$0110-$0123` in four groups of five bytes and repacks each five-byte record into four bytes.

For each record `[A,B,C,D,E]`:

- output 0 = `A`
- output 1 = `C`
- output 2 = `E`
- output 3 = `(B << 4) | D`

This works because `B` and `D` are decimal/nibble-sized high digits associated with the packed-decimal Life/Cosmo values.

Four records therefore become 16 bytes in `$06CC-$06DB`.

## Global/progression fields

`$AF49+` appends six more packed bytes:

- `$0130` -> byte 16
- `$0131` -> byte 17
- `($0132 << 5) | $0133` -> byte 18
- `$0134` -> byte 19
- `($0135 << 4) | $0136` -> byte 20
- `($0137 << 4) | $0138` -> byte 21

The first two are the packed-decimal Seventh Sense bytes. `$0133` is the progression value copied from `$067D`. Other field semantics remain under verification.

The result is a 22-byte logical payload at `$06CC-$06E1`.

## 22 bytes -> 30 six-bit symbols

Bank 0 `$AE18` clears the work area and uses helper `$B03A` to transform each payload byte.

For every payload byte:

- the upper 2 bits are shifted into one of the first eight six-bit accumulators;
- the lower 6 bits are stored directly.

Three payload bytes contribute their upper two bits to one complete six-bit symbol. With 22 payload bytes, the work area ends up holding:

- 8 symbols formed from upper-bit groups;
- 22 symbols containing lower six bits;
- total: 30 six-bit data symbols.

The compact overlapping layout starts at `$06AC`; the lower-six-bit region starts at `$06B4`, exactly eight bytes later.

## Checksum — symbol 31

At `$AE3E`, the game XORs the 30 data symbols (the work buffer was pre-cleared, so the following empty slot contributes zero) and stores the result as the 31st symbol at `$06CA`.

Decoder validation later XORs all 31 symbols and requires the result to be zero.

Thus:

`symbol[30] = symbol[0] XOR symbol[1] XOR ... XOR symbol[29]`

## Obfuscation window

After computing the checksum:

1. take `checksum & $0F` as a start index;
2. for ten consecutive symbols beginning at that index;
3. XOR each with `$3F`.

Because `$3F` is binary `111111`, this complements all six bits of each selected symbol.

The decoder uses the checksum's low nibble to locate the same ten-symbol window and applies XOR `$3F` again, restoring the original data.

This is lightweight obfuscation, not encryption.

## Glyph mapping table — `$AD81`

For password output, each six-bit value is used as an index into the table at `$AD81`.

The first 64 entries map password values `0..63` to Japanese glyph/tile codes. Several mapped values have bit 7 set; output builder `$AE72+` treats these specially by clearing bit 7 and appending tile `$3B`, consistent with adding a dakuten/voicing mark as a second glyph.

The same table is also used by the manual-entry screen to draw the kana selected from the grid.

## Password entry UI

Bank 0 `$B271+` reads the standard held/newly-pressed controller state.

Grid coordinates:

- `$06EC` = column candidate, range effectively 0..9;
- `$06ED` = row candidate, range 0..6.

The selected grid index is calculated as:

`index = row * 10 + column`

The routine rejects indices `>= $42` (66 decimal), so the selectable character set contains 66 grid entries.

On A press, the selected index is written to:

- `$06AC + password_position`
- `$0140 + password_position`

`$06EE` tracks the password position. The input buffer is exactly 31 bytes (`$0140-$015E`).

The public 888/999 cheat passwords independently total 31 characters, matching the implementation.

## Decoder

Bank 0 `$AEB5+` validates and decodes an entered password.

High-level sequence:

1. reject obviously invalid/unfilled patterns;
2. XOR all 31 six-bit symbols; result must be zero;
3. derive obfuscation start from `symbol[30] & $0F`;
4. undo XOR `$3F` across ten symbols;
5. helpers `$B04C` reconstruct the original 22 payload bytes from the six-bit representation;
6. `$AF89+` unpacks the payload into game state;
7. `$B06F` validates packed-decimal bytes (each decimal nibble must be 0..9, values must remain below `$9A`);
8. valid data is written back into the persistent snapshot/progression structures.

### Saint restore

For each of the four persistent Saint records, `$AF89+` rebuilds the five original bytes and writes them into `$058C+`.

### Seventh Sense restore

Payload bytes 16/17 restore `$05AA/$05AB`.

### Progress/global restore

Following bytes restore values including `$05AC`, `$0100/$0101`, and `$0587-$058A`; `$C1ED` later transfers `$0100/$0101` to `$067D/$06CD` during continue-game setup.

## Encoder/decoder symmetry

The password is therefore not a lookup table of predetermined codes. It is a true serialization format for a subset of player state.

That is useful for REBORN in two ways:

1. ORIGINAL SPEC can reproduce old passwords exactly;
2. REBORN can import a real 1988 password into a modern save profile if we preserve this decoder as a compatibility module.

## Remaining work

- assign exact semantic names to `$05AC`, `$0587-$058A`, `$06CD`;
- identify the fifth active Saint slot and why it is intentionally excluded from password persistence;
- write a clean-room Python implementation of encode/decode and test it against known public passwords;
- use known 888/999 passwords as fixtures to confirm every bit-order detail;
- identify the UI/end-of-entry control flow and invalid-password feedback states.
