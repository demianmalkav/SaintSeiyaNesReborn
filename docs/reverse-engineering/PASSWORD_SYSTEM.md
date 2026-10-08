# Password system — static reverse engineering

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: encode/decode pipeline isolated end-to-end. Saint record semantics, including Life/Cosmo caps, are resolved; some global progression-field semantics remain provisional.

## Overview

The password is a 31-symbol code selected from a 10-column × 7-row kana grid. The game serializes selected durable state rather than storing a raw memory image:

1. stage state into `$0110-$0138`;
2. pack to a 22-byte logical payload at `$06CC+`;
3. transform to 30 six-bit data symbols;
4. append an XOR checksum as symbol 31;
5. XOR a checksum-selected 10-symbol window with `$3F`;
6. map six-bit values to kana/tile codes;
7. perform the exact inverse path on password entry.

## State staging — fixed bank `$C458`

- `$058C-$059F` -> `$0110-$0123` (four five-byte Saint records)
- `$05AA` -> `$0130`
- `$05AB` -> `$0131`
- `$05AC` -> `$0132`
- `$067D` -> `$0133`
- `$06CD` -> `$0134`
- `$0587-$058A` -> `$0135-$0138`

The active gameplay snapshot contains a fifth Saint record at `$05A0-$05A4` (Ikki), but it is deliberately excluded from password serialization.

## Saint record semantics — resolved

A five-byte persistent record is:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, cap byte]`

The cap byte packs two **exclusive hundreds boundaries**:

- high nibble = Life boundary;
- low nibble = Cosmo boundary.

For normal game values:

`maximum = boundary * 100 - 1`

Examples:

- cap byte `$11` -> Life max 99, Cosmo max 99;
- cap byte `$55` -> Life max 499, Cosmo max 499;
- cap byte `$99` -> Life max 899? No: the boundary is exclusive, so a boundary of 9 permits hundreds digits 0..8 and therefore maximum 899. A true 999 current value can still exist in cheat/test fixtures independent of ordinary cap progression.

The semantic record interpretation is supported by initialization/decrement code, Game Genie patch locations and the resource-allocation routines documented in `RESOURCE_ECONOMY.md`.

Bank 0 `$AF19` consumes each staged record `[A,B,C,D,E]` and emits:

- output 0 = `A` = Life low-two digits
- output 1 = `C` = Cosmo low-two digits
- output 2 = `E` = packed Life/Cosmo cap byte
- output 3 = `(B << 4) | D` = Life hundreds in high nibble, Cosmo hundreds in low nibble

Four five-byte records therefore become 16 payload bytes.

This means a password preserves not only current Life/Cosmo but also each persistent Saint's resource-growth boundaries.

## Persistent characters represented

The four serialized snapshot records are in canonical order:

1. Seiya
2. Hyoga
3. Shun
4. Shiryu

Ikki is the fifth active record and is not included in the password.

The internal platform order differs (`Seiya, Shun, Hyoga, Shiryu, Ikki`); fixed table `$E505=[0,2,1,3,4]` converts between the two domains. See `CHARACTER_INDEX_MAP.md`.

## Global/progression fields

`$AF49+` appends six bytes:

- `$0130` -> byte 16
- `$0131` -> byte 17
- `($0132 << 5) | $0133` -> byte 18
- `$0134` -> byte 19
- `($0135 << 4) | $0136` -> byte 20
- `($0137 << 4) | $0138` -> byte 21

Bytes 16/17 are the four packed-decimal digits of Seventh Sense. `$0133` carries persistent story progression copied from `$067D`. Other global fields remain under investigation.

## 22-byte payload -> 30 six-bit symbols

Bank 0 `$AE18` and helper `$B03A` split each payload byte into:

- upper two bits, accumulated three-at-a-time into six-bit symbols;
- lower six bits, stored directly.

This produces 30 data symbols.

## Checksum — symbol 31

The game XORs the 30 data symbols and stores the result as symbol 31:

`symbol[30] = symbol[0] XOR ... XOR symbol[29]`

Decoder validation XORs all 31 and requires zero.

## Obfuscation window

After checksum creation:

1. start index = `checksum & $0F`;
2. take 10 consecutive symbols;
3. XOR each with `$3F`.

The decoder applies the same operation to restore them. This is reversible obfuscation, not encryption.

## Glyph mapping / entry UI

Password output maps six-bit values through table `$AD81`. Entries with bit 7 set are rendered with a secondary tile consistent with voiced kana/dakuten handling.

Bank 0 `$B271+` handles manual entry:

- `$06EC` = column, 0..9
- `$06ED` = row, 0..6
- selected index = `row * 10 + column`
- indices `>= $42` rejected
- `$06EE` = password position
- accepted values written to `$06AC+position` and `$0140+position`
- `$0140-$015E` is exactly 31 bytes

## Decoder

Bank 0 `$AEB5+`:

1. validates input/checksum;
2. removes XOR-`$3F` window;
3. `$B04C` reconstructs the payload;
4. `$AF89+` restores four Saint records and globals;
5. `$B06F` validates decimal-bearing fields, including the cap-byte nibbles.

## Clean-room compatibility decoder

`tools/password/password_codec.py` implements the decoder without embedding ROM data.

`SaintRecord` now exposes:

- `life`
- `cosmo`
- `cap_byte`
- `life_cap_boundary`
- `cosmo_cap_boundary`
- `life_max`
- `cosmo_max`

The known maximum-stat password remains the primary independent fixture. Using its documented kana/grid coordinates, the decoder reconstructs for all four serialized Saints:

- Life = `999`
- Cosmo = `999`
- Seventh Sense = `9999`

This validates checksum, XOR window, six-bit unpacking, packed-decimal interpretation and record ordering. The fixture is a cheat/test state and should not be used to infer normal cap progression.

A second publicly transcribed 888 password was investigated but is **not accepted as a fixture**: its current transcription produces an inconsistent first record while remaining records decode plausibly. It stays outside automated tests until independently reconciled.

## REBORN implication

The password is a genuine state serialization format. REBORN can support authentic 1988 passwords as an import format into a modern save/profile layer and restore:

- four persistent Saints' current Life;
- current Cosmo;
- Life/Cosmo growth caps;
- Seventh Sense;
- story/global progression fields.

This provides a direct compatibility bridge between original-game progression and the modern remake.

## Remaining work

- resolve exact semantics of `$05AC`, `$0587-$058A`, `$06CD`;
- obtain additional independently verified password fixtures;
- implement an encoder only after resolving the final partial upper-bit group compatibility behavior;
- identify invalid-password UI feedback/end-of-entry states.
