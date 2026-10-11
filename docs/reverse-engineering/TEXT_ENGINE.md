# Japanese text engine and message corpus

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: message indexing, pointer table, physical text storage, terminator/newline controls, dakuten/handakuten overlay behavior, font mapping and the complete 251-message extraction path are statically reconstructed. Runtime request/localization integration is closed separately in `CANONICAL_RUNTIME_TEXT_CONTENT.md`.

The complete extracted Japanese script is **not committed** to the public repository. It is generated locally from a user-owned ROM by `tools/reverse/extract_japanese_script.py`.

## Message IDs

Dialogue/event code does not pass raw text pointers. Callers pass an 8-bit **message ID** to one of four fixed-bank entrypoints:

- `$E7B3`: store message ID in `$066A`, set `$0672=$FF`, call `$EC6D`;
- `$E7B7`: store message ID in `$066A`, set `$0672=$00`, call `$EC6D`;
- `$E7C3`: store message ID in `$066B`, set `$0672=$FF`, call `$ECB8`;
- `$E7C7`: store message ID in `$066B`, set `$0672=$00`, call `$ECB8`.

`$0672` is preserved as raw request/presentation variant metadata; the canonical evidence proves `$00/$FF` behavior but does not justify a narrower presentation name. `$066A/$066B` are the two canonical message request slots/channels.

Immediate `LDA #id ; JSR $E7Bx` patterns account for 184 statically obvious callsites. Other IDs can be reached through dynamic/event paths.

## Pointer table

The render setup maps PRG bank 6. Routine `$ED23+` reads a base pointer from bank 6 `$A479/$A47A`; that base is `$A47B`.

For a message ID:

`pointer = word[$A47B + id*2]`

The table contains exactly **251 valid entries**, message IDs `0..250`.

The pointer values are not CPU addresses. They are offsets/PPU addresses into the temporary CHR-backed message storage described below.

## Physical text storage

When fetching one character, bank 6 `$9030+` temporarily maps:

- CHR 4 KiB bank `$15` into PPU `$0000-$0FFF`;
- CHR 4 KiB bank `$17` into PPU `$1000-$1FFF`.

It then computes:

`message_pointer + character_index`

sets `$2006`, performs the PPU read-buffer dummy read, and reads the actual byte from `$2007`.

After reading the byte, `$9055/$9067` restore the normal CHR banks from `$0637/$0638`.

For clean-room extraction, the logical message blob is therefore:

`CHR4K[$15] || CHR4K[$17]`

or 8192 bytes total.

This is an unusual but clear ROM-space optimization: dialogue bytes are stored in CHR ROM and read through the PPU data port rather than being ordinary PRG strings.

## Message stream controls

The visible renderer at bank 6 `$8F42+` interprets:

- `$FF` — end of message;
- `$01` — space / advance one tile without drawing a glyph;
- `$A4` — line break / move to the second text line (`+ $40` nametable bytes);
- `$3B` — dakuten overlay on the preceding tile;
- `$3C` — handakuten overlay on the preceding tile;
- all other used values — write directly as the nametable tile ID.

For dakuten/handakuten, the renderer subtracts `$21` from the current nametable position and writes the mark over/adjacent to the preceding glyph, reproducing a voiced/semi-voiced kana without needing duplicate voiced glyph tiles.

## Font bank

The dialogue display restores a font-bearing CHR configuration in which CHR 4 KiB bank **1** contains the Japanese character set used by the message tile IDs.

The mapping is regular:

- `$04-$31` — base hiragana;
- `$32-$3A` — small hiragana;
- `$3B/$3C` — dakuten / handakuten;
- `$3E` — prolonged sound mark `ー`;
- `$3F` — `?`;
- `$40` — `!`;
- `$41` — middle dot `・`, used repeatedly to form ellipsis-like runs;
- `$44-$71` — base katakana;
- `$72-$7A` — small katakana;
- `$80-$89` — digits `0-9`;
- `$8A-$A3` — Latin `A-Z`;
- `$A4` — renderer control, not a visible glyph in message streams.

Several punctuation/font tiles exist but are unused by the 251-message corpus.

## Important correction: no hidden high-byte text encoding

A preliminary frequency scan over raw CHR storage appeared to show many values `$A5+`. That scan crossed message terminators into unrelated/gap data.

When each of the 251 pointer-selected streams is followed only through its first `$FF` terminator:

- every visible/control byte is `<= $A4`;
- no unknown high-byte glyph encoding is present;
- the original Japanese dialogue is **not compressed**.

This is now confirmed by decoding the corpus into coherent Japanese throughout.

The English fan-translation's documented Huffman work should therefore be treated as a modification introduced to fit expanded Latin text, not as evidence that the Japanese original uses Huffman compression.

## Stable localization identity

The canonical localization key is:

`MSG_000` through `MSG_250`.

The extractor also records static callsites with PRG bank, CPU address, message slot and `$0672` flag. Descriptive metadata such as speaker, scene and semantic alias may be improved as context is confirmed, but it never changes the stable numeric identity or canonical slot/variant request tuple.

The localization pipeline remains:

`ROM message ID -> Japanese decoded source -> semantic context -> Spanish localization -> external runtime catalog`

English/Portuguese fan translations are secondary technical/reference material only; Spanish is translated from the Japanese source.

## Reproducible extractor

`tools/reverse/extract_japanese_script.py`:

1. verifies canonical full-ROM SHA-1;
2. reads the 251 pointers at PRG bank 6 `$A47B`;
3. concatenates CHR4K `$15` + `$17` as the message-storage address space;
4. reads each stream through `$FF`;
5. maps font tile IDs to Unicode Japanese;
6. composes dakuten/handakuten into voiced Unicode kana;
7. records immediate message callsites;
8. outputs private JSON with `MSG_000..MSG_250`, source offset, raw bytes and decoded Japanese.

The generated JSON is derived copyrighted game text and is intentionally excluded from the public source tree.

## Runtime integration

`CanonicalRuntimeLocalization` and `CANONICAL_RUNTIME_TEXT_CONTENT.md` now freeze the content-independent runtime boundary:

- four `$E7Bx` entrypoints map deterministically to message ID + `$066A/$066B` slot + raw `$0672` variant;
- external catalogs must cover exactly `MSG_000..MSG_250`;
- JP and ES payloads remain private and are loaded externally;
- fallback policy is explicit and never changes message identity;
- public fixtures use synthetic text only.

Non-dialogue text such as menus, status UI, names and techniques remains a separate content class and is not silently folded into the 251-message dialogue corpus.
