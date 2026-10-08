# RAM map — current static pass

Target: verified Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM documented in `CANONICAL_ROM.md`.

This document separates structural facts proven by the ROM from semantic names that still need runtime confirmation.

## Five active Saint records

### `$0059-$0062` — Life, five packed-decimal two-byte values

The engine indexes this region as five two-byte values using `2 * current_saint_index`.

Semantic identification is now strong enough to treat this as Life/Health rather than Cosmo:

- initialization around bank 1 `$95B2+` gives the first four slots `099` and the fifth slot `499`;
- the published `Start with 999 Health` Game Genie code patches the Life hundreds initialization immediate at `$95B9`;
- the published `Infinite Health Except Battles` code neutralizes the store in the decrement path around `$92DA`;
- bank 1 performs manual packed-decimal correction when subtracting from this array.

Working symbol: `saint_life_bcd[5]`.

### `$0063-$006C` — Cosmo, five packed-decimal two-byte values

Same five-slot/two-byte packed-decimal structure.

Semantic identification:

- first four slots initialize to `099`, fifth to `499`;
- the published `Start with 999 Cosmo` code patches the hundreds initialization immediate at `$95C9`;
- the published `Infinite Cosmo Except Battles` code neutralizes the store in the decrement path around `$9354`;
- platform attack range/lifetime is derived from the selected Saint's Cosmo hundreds digit.

Working symbol: `saint_cosmo_bcd[5]`.

### `$006D-$0071` — one auxiliary byte per Saint

Snapshot/restore code treats these as the fifth byte of each Saint record. Display/gameplay routines consume the nibbles, so they are meaningful state rather than padding.

Exact meaning remains unknown.

Working symbol: `saint_aux_stat[5]`.

### `$03` — current Saint index

Repeatedly doubled to index the five Life/Cosmo arrays. Valid structure is `0..4`.

Working symbol: `current_saint_index`.

Full identity mapping is still being proven. Current behavioral evidence strongly suggests slot 0 = Seiya and slot 4 = Ikki; slot 3 is a strong Shun candidate because its attack path uses multiple projectile/OAM slots. Keep those identities `INFERRED` until ROM-side mapping is closed.

## Persistent Saint snapshot

### `$058C-$05A4` — five 5-byte records

Bank 1 `$951F/$9720` snapshot and restore active Saint state using this order:

- slot 0 `$058C-$0590` <- `$59,$5A,$63,$64,$6D`
- slot 1 `$0591-$0595` <- `$5D,$5E,$67,$68,$6F`
- slot 2 `$0596-$059A` <- `$5B,$5C,$65,$66,$6E`
- slot 3 `$059B-$059F` <- `$5F,$60,$69,$6A,$70`
- slot 4 `$05A0-$05A4` <- `$61,$62,$6B,$6C,$71`

Each record is therefore:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, auxiliary]`

Only snapshot slots 0-3 are serialized into the password. Slot 4 is active gameplay state but intentionally excluded from the 31-symbol password.

## Seventh Sense / progression

### `$05AA-$05AB` — Seventh Sense

Four packed-decimal digits. The clean-room password decoder reconstructs `9999` from the known maximum-stat password.

Working symbol: `seventh_sense_bcd`.

### `$067D` — persistent story progression

Incremented by progression/event sequences, compared against `$0C`, serialized into the password and restored on continue.

Working symbol: `story_progress_index`.

### `$06CD` — persistent progression descriptor

Derived from progression tables and serialized/restored alongside `$067D`.

Exact semantic label remains provisional.

## Input

### `$3D/$3E` — platform controller held state

Fixed `$C4E4` reads controller 1/2. `$3D` bit layout:

- `$80` A
- `$40` B
- `$20` Select
- `$10` Start
- `$08` Up
- `$04` Down
- `$02` Left
- `$01` Right

### `$020A/$020B` — menu/password input

Bank 0 `$8B1F` stores held input in `$020A` and newly pressed input in `$020B`.

## Platform player core

- `$3F` — player horizontal coordinate
- `$40` — principal vertical coordinate
- `$41` — vertical high/page component candidate
- `$42` — facing/direction
- `$44/$45` — horizontal camera/scroll pair
- `$49` — jump phase/index counter
- `$4A` — jump/airborne latch
- `$4B/$4C` — attack/busy timing state
- `$4D` — current/next player action/animation state
- `$4E` — latched frame-start action state; fixed gameplay code copies `$4D -> $4E` before bank-3 processing
- `$4F-$56` — collision/environment sample block; exact spatial positions still pending
- `$76` — hit/invulnerability-like timer; set to `$20` by interaction paths
- `$0387-$0389` — horizontal movement increments used in ground/air control
- `$038A` — high-jump modifier; A+Up straight jump stores `$30` here

Important action families currently observed:

- `$00` idle/neutral
- `$10-$1F` locomotion/animation
- `$20` crouch
- `$30-$33` jump family
- `$50` fall/vertical transition family
- `$80` damage/fall/death-related family

## Table-driven jump physics

Jump physics are not a conventional velocity-plus-gravity accumulator. Bank 3 selects signed displacement tables and a duration by current Saint and jump mode.

Static extraction currently yields:

| Slot | High jump duration | High max ascent | Forward jump duration | Forward max ascent |
|---:|---:|---:|---:|---:|
| 0 | 60 | 103 px | 54 | 39 px |
| 1 | 50 | 88 px | 40 | 33 px |
| 2 | 40 | 71 px | 44 | 34 px |
| 3 | 40 | 71 px | 44 | 34 px |
| 4 | 50 | 88 px | 54 | 39 px |

A normal straight A jump without Up uses a common 32-frame table with about 58 px maximum ascent. After the table phase, the fall path uses `+3 px/frame` until collision/landing resolution.

The extraction is reproducible with `tools/physics/extract_platform_profiles.py`.

## Platform attack/projectile state

Attack creation uses sprite/projectile records around:

- `$0730/$0731`
- `$0738/$0739`
- `$0740/$0741`

and counters/metadata around `$038E-$0390`.

`$038E` is consumed as a projectile lifetime/range counter by bank-3 attack update logic.

For Saint slots 0-3, the initial range/lifetime parameter is indexed by:

`floor(Cosmo_hundreds / 2) * 4 + saint_index`

The 20-byte table at bank 3 `$BCAE` gives:

| Cosmo hundreds | slot 0 | slot 1 | slot 2 | slot 3 |
|---|---:|---:|---:|---:|
| 0-1 | 3 | 1 | 4 | 6 |
| 2-3 | 6 | 3 | 10 | 10 |
| 4-5 | 12 | 8 | 16 | 14 |
| 6-7 | 24 | 12 | 22 | 18 |
| 8-9 | 48 | 16 | 28 | 22 |

Slot 4 bypasses this table and receives a constant `60`.

This proves an important gameplay rule: **Cosmo affects platform attack reach/lifetime**, not only battle statistics.

## OAM / mapper coordination

### `$0700-$07FF`

OAM shadow page. NMI writes `$07` to `$4014` for sprite DMA.

### `$3A/$3B`

- `$3A` — NMI-interrupted MMC1 serial-write flag
- `$3B` — persistent PRG bank restored by NMI

### `$0639/$063A/$063E/$063F`

Second synchronized PRG-bank path around fixed `$E589/$E5B7`; working roles are requested/transient bank plus critical-section flags.

## Password serialization

Fixed `$C458` stages:

- `$058C-$059F` -> `$0110-$0123` (four Saint records)
- `$05AA-$05AC` -> `$0130-$0132`
- `$067D` -> `$0133`
- `$06CD` -> `$0134`
- `$0587-$058A` -> `$0135-$0138`

Detailed bit packing/checksum/obfuscation is documented in `PASSWORD_SYSTEM.md`.

## External battle-mode candidates

Community cheat material points to `$05BC/$05BD` and `$05CE/$05CF` as battle-mode Cosmo/Life structures with nearby maxima. These are not yet promoted because their exact relationship to the platform/persistent arrays still requires tracing.

## Next tests

1. Close the internal Saint index -> character identity mapping.
2. Assign spatial meaning to collision samples `$4F-$56`.
3. Trace projectile hit detection and damage application, not only lifetime/range.
4. Enumerate the remaining `$4D/$4E` action states and transitions.
5. Map `$05BC-$05D0` boss-battle structures.
6. Add runtime breakpoints when a debugger-capable NES emulator becomes available in the execution environment.
