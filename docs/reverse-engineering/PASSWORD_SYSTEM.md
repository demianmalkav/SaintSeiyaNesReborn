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

The first 20 bytes are four five-byte Saint records. The active gameplay snapshot actually contains a fifth five-byte slot at `$05A0-$05A4`, but **that fifth slot is not serialized into the password**. This proves the game maintains five active Saint slots while only four are normal password-persistent records.

External documentation confirms five playable Bronze Saints, but the internal fifth slot remains deliberately unnamed until the ROM proves index-to-character mapping.

## Four persistent Saint records

Bank 0 `$AF19` consumes `$0110-$0123` in four groups of five bytes and repacks each five-byte record into four bytes.

For each record `[A,B,C,D,E]`:

- output 0 = `A`
- output 1 = `C`
- output 2 = `E`
- output 3 = `(B << 4) | D`

Four records therefore become 16 bytes in `$06CC-$06DB`.

## Global/progression fields

`$AF49+` appends six more packed bytes:

- `$0130` -> byte 16
- `$0131` -> byte 17
- `($0132 << 5) | $0133` -> byte 18
- `$0134` -> byte 19
- `($0135 << 4) | $0136` -> byte 20
- `($0137 << 4) | $0138` -> byte 21

The first two are the packed-decimal Seventh Sense bytes. `$0133` is the persistent progression value copied from `$067D`. Other field semantics remain under verification.

## 22-byte payload -> 30 six-bit data symbols

Bank 0 `$AE18` uses helper `$B03A` to split payload bytes into upper-two-bit groups and lower-six-bit values.

Three payload bytes contribute their upper two bits to one complete six-bit accumulator. The lower six bits are stored separately beginning eight bytes later. The final password contains 30 data symbols plus checksum.

## Checksum — symbol 31

At `$AE3E`, the game XORs the 30 data symbols and stores the result as symbol 31 at `$06CA`.

Decoder validation XORs all 31 symbols and requires zero:

`symbol[30] = symbol[0] XOR symbol[1] XOR ... XOR symbol[29]`

## Obfuscation window

After computing the checksum:

1. take `checksum & $0F` as start index;
2. for ten consecutive symbols beginning there;
3. XOR each with `$3F`.

The decoder applies the same XOR again to restore the data. This is lightweight obfuscation, not encryption.

## Glyph mapping table — `$AD81`

Password output uses each six-bit value as an index into `$AD81`. Several entries use bit 7 as a secondary-glyph marker; output code clears bit 7 and appends tile `$3B`, consistent with voiced kana/dakuten composition.

## Password entry UI

Bank 0 `$B271+` handles the grid:

- `$06EC` = column, 0..9;
- `$06ED` = row, 0..6;
- selected index = `row * 10 + column`;
- indices `>= $42` are rejected;
- `$06EE` = password position;
- accepted symbols are stored at `$06AC+position` and `$0140+position`;
- input buffer `$0140-$015E` is exactly 31 bytes.

## Decoder

Bank 0 `$AEB5+`:

1. validates input/checksum;
2. removes XOR-`$3F` window;
3. helper `$B04C` reconstructs payload bytes;
4. `$AF89+` restores persistent state;
5. `$B06F` validates packed-decimal fields.

Four Saint records restore into `$058C-$059F`; Seventh Sense restores into `$05AA/$05AB`; progression/global values restore into their staging fields and are transferred to live progression state during continue setup.

## Clean-room compatibility fixture

`tools/password/password_codec.py` implements the decoder without embedding ROM bytes.

A documented maximum-stat password uses:

- `し` × 6
- `が` × 2
- `ぜ` × 2
- `が` × 16
- `あ` × 5

Using the documented password-grid coordinates, the clean-room decoder reconstructs:

- all four password-persistent Saint records: Cosmo `999`;
- all four password-persistent Saint records: Life `999`;
- Seventh Sense: `9999`.

That fixture independently validates the central checksum, deobfuscation, six-bit unpacking, packed-decimal interpretation and four-record layout.

## Encoder caution

The original encoder operates on a 22-byte logical payload while the inverse bit-unpack helper naturally processes complete three-byte groups in a larger work area. The final partial upper-bit group therefore has an asymmetry that must be compatibility-tested before publishing a clean-room encoder.

For that reason the repository currently implements the decoder only. An encoder will be added after original-generated password fixtures prove the final partial-group behavior.

## REBORN implication

The password is a true serialization format, not a lookup table. REBORN can therefore support authentic 1988 password import into a modern save profile while preserving ORIGINAL SPEC compatibility.

## Remaining work

- assign exact semantic names to `$05AC`, `$0587-$058A`, `$06CD`;
- prove index-to-character mapping for all five active Saint slots;
- add more independent password fixtures;
- implement encoder after resolving final-group behavior;
- identify invalid-password UI feedback and end-of-entry control flow.
