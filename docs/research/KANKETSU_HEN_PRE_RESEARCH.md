# Kanketsu Hen — pre-research and external anchors

This document records external material useful to reverse engineering the canonical Japanese ROM. External claims remain secondary to ROM evidence.

## Canonical target

The project targets the verified Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM documented in `docs/reverse-engineering/CANONICAL_ROM.md`.

## Community RAM / TAS work

TASVideos has material for this exact game/revision, including RAM watches and modern runs. These are useful as hypotheses and test vectors, particularly for:

- Life/Cosmo state;
- player position/camera;
- progression/event flags;
- boss-dialogue thresholds;
- timing-dependent parry/defense behavior.

All addresses/semantics are reproduced against our own static/dynamic analysis before promotion to `CONFIRMED`.

## Existing English translation hack

A public soft-patch repository contains the aishsha/Djinn English translation package for this game:

- `SS_ODKH_en_Djinn_Aishsha_v1.01.ips`
- IPS size: 19,773 bytes
- Git blob SHA: `3af03b06e9e3b8479d03f773972694c7c7ff0cde`
- companion readme: `SS_ODKH_en_Djinn_Aishsha_v1.01_readme.txt`

The readme states that the hacking work included:

- Huffman encoding to provide substantially more translated-text space;
- graphics editing;
- misc translation work;
- v1.01 fix to password output after game over.

### Why this matters

The IPS is not a replacement source ROM and is not our localization source. It is a highly valuable **binary differential**:

`canonical Japanese ROM + IPS records -> locations changed by translation hack`

Once its binary records are materialized locally, we can classify changed ranges by PRG/CHR bank and cross-reference them with our existing code map. That should sharply reduce the search space for:

- text renderer;
- original text storage/index tables;
- any inserted Huffman decoder;
- font/CHR modifications;
- password-output modifications.

Current connector access exposes the IPS metadata/path but rejects its binary body as non-UTF-8, so the patch has not yet been locally diffed. Do not infer its internal offsets until we possess the actual IPS records.

## Portuguese/French derivatives

Other translations were based on the English hack. They are secondary technical evidence only. They must not become the canonical source for Spanish text.

## Localization rule

Narrative/text authority remains:

`Japanese ROM -> semantic extraction/context -> direct Spanish localization`

English, Portuguese and French patches may help recover engineering details or resolve ambiguous context, but Spanish must not be produced by mechanically translating those patches.

## Public guides as behavioral evidence

Modern guides independently document:

- five playable Bronze Saints: Seiya, Shiryu, Hyoga, Shun and Ikki;
- platform controls including crouch, normal/high jump and attack;
- initial character-specific Life/Cosmo differences;
- story-specific character availability and forced-character sequences;
- known 31-kana passwords including maximum-stat fixtures.

These guides are useful test vectors, not substitutes for code evidence.

## Research priorities derived from external work

1. Use public password fixtures to validate the clean-room password decoder.
2. Map internal Saint indices to names from ROM behavior, not guide ordering assumptions.
3. Recover the English IPS bytes and classify its changed ranges.
4. Compare text-hack modifications against bank 6/fixed-bank rendering routines.
5. Reproduce TAS RAM-watch semantics dynamically when debugger execution becomes available.
