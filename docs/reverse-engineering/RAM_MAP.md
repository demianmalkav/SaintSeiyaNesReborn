# RAM map — current static pass

Target: verified Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM documented in `CANONICAL_ROM.md`.

This document separates structural facts proven by the ROM from semantic names that still need runtime confirmation.

## Five active Saint records

### `$0059-$0062` — Life, five packed-decimal two-byte values

The engine indexes this region as five two-byte values using `2 * current_saint_index`.

Semantic identification is confirmed by initialization/decrement code and independent Game Genie patch locations:

- the first four slots initialize to `099`, the fifth to `499`;
- `Start with 999 Health` patches the hundreds initialization at `$95B9`;
- `Infinite Health Except Battles` neutralizes the decrement store around `$92DA`.

Working symbol: `saint_life_bcd[5]`.

### `$0063-$006C` — Cosmo, five packed-decimal two-byte values

Same five-slot/two-byte packed-decimal structure.

- first four slots initialize to `099`, fifth to `499`;
- `Start with 999 Cosmo` patches the hundreds initialization at `$95C9`;
- `Infinite Cosmo Except Battles` neutralizes the decrement store around `$9354`;
- platform attack range/lifetime is derived from the selected Saint's Cosmo hundreds digit.

Working symbol: `saint_cosmo_bcd[5]`.

### `$006D-$0071` — one auxiliary byte per Saint

Snapshot/restore code treats these as the fifth byte of each Saint record. Exact meaning remains unknown.

Working symbol: `saint_aux_stat[5]`.

### `$03` — current Saint index

Internal platform order is now confirmed as:

`[Seiya, Shun, Hyoga, Shiryu, Ikki]`

A separate high-level selector `$0533` uses:

`[Seiya, Hyoga, Shun, Shiryu, Ikki]`

Fixed table `$E505 = [0,2,1,3,4]` converts between the two index spaces. Full proof is documented in `CHARACTER_INDEX_MAP.md`.

## Persistent Saint snapshot

### `$058C-$05A4` — five 5-byte records

Bank 1 `$951F/$9720` snapshot/restore order:

- `$058C-$0590` <- internal slot 0 Seiya
- `$0591-$0595` <- internal slot 2 Hyoga
- `$0596-$059A` <- internal slot 1 Shun
- `$059B-$059F` <- internal slot 3 Shiryu
- `$05A0-$05A4` <- internal slot 4 Ikki

Each record is:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, auxiliary]`

The order is therefore exactly the high-level/canonical selector order. Password serialization takes only the first four records and deliberately excludes Ikki.

## Seventh Sense / progression

### `$05AA-$05AB` — Seventh Sense

Four packed-decimal digits. The clean-room password decoder reconstructs `9999` from the known maximum-stat password.

### `$067D` — persistent story progression

Incremented by progression/event sequences, compared against `$0C`, serialized into the password and restored on continue.

Working symbol: `story_progress_index`.

### `$06CD` — persistent progression descriptor

Derived from progression tables and serialized/restored alongside `$067D`. Exact semantic label remains provisional.

## Input

### `$3D/$3E` — platform controller held state

`$3D` controller 1 bits:

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
- `$4E` — frame-start/latched action state
- `$4F-$56` — eight tile collision probes with now-reconstructed geometry
- `$76` — hit/invulnerability-like timer
- `$0387-$0389` — horizontal movement increments
- `$038A` — high-jump modifier

Action families observed include `$00` idle, `$10-$1F` locomotion, `$20` crouch, `$30-$33` jump, `$50` falling/vertical transition and `$80` damage/fall/death-related behavior.

## Collision probe geometry — CONFIRMED

Bank 0 `$B3E2+` samples the stage tilemap around the player. With `X = scroll_x + player_x`, `Y = $40`, and `T = (Y + 8) & $F0`:

| RAM | Sample position | Role |
|---:|---|---|
| `$54` | `X+0, T` | upper-left |
| `$56` | `X+8, T` | upper-center |
| `$51` | `X+16, T` | upper-right |
| `$53` | `X+0, T+16` normally | lower-left side |
| `$50` | `X+16, T+16` normally | lower-right side |
| `$55` | `X-8, Y+32` | bottom-left / ledge |
| `$4F` | `X+8, Y+32` | bottom-center / floor |
| `$52` | `X+24, Y+32` | bottom-right / ledge |

At `$40 == $88`, `$50/$53` use `T+24` instead of `T+16`.

See `COLLISION_PROBES.md` for the generator and consumption evidence.

## Table-driven jump physics

Jump physics use per-mode/per-Saint signed displacement tables rather than a velocity/gravity accumulator.

| Internal slot / Saint | High duration | High max ascent | Forward duration | Forward max ascent |
|---|---:|---:|---:|---:|
| 0 Seiya | 60 | 103 px | 54 | 39 px |
| 1 Shun | 50 | 88 px | 40 | 33 px |
| 2 Hyoga | 40 | 71 px | 44 | 34 px |
| 3 Shiryu | 40 | 71 px | 44 | 34 px |
| 4 Ikki | 50 | 88 px | 54 | 39 px |

Normal straight A jump uses a common 32-frame profile with about 58 px maximum ascent. After table completion, the ordinary fall path uses `+3 px/frame` until landing.

Reproducible extractor: `tools/physics/extract_platform_profiles.py`.

## Platform attack/projectile state

Attack records use OAM/projectile slots around `$0730/$0731`, `$0738/$0739`, `$0740/$0741`, with counters/metadata around `$038E-$0390`.

For internal Saint slots 0-3, initial projectile range/lifetime is indexed by:

`floor(Cosmo_hundreds / 2) * 4 + internal_saint_index`

Table at bank 3 `$BCAE`:

| Cosmo hundreds | Seiya | Shun | Hyoga | Shiryu |
|---|---:|---:|---:|---:|
| 0-1 | 3 | 1 | 4 | 6 |
| 2-3 | 6 | 3 | 10 | 10 |
| 4-5 | 12 | 8 | 16 | 14 |
| 6-7 | 24 | 12 | 22 | 18 |
| 8-9 | 48 | 16 | 28 | 22 |

Ikki bypasses this table and receives constant `60`.

Shun's internal slot 1 also has a distinct extend/retract attack path: attack type `$54`, extension seed `$0391=5`, then `$A311+` extends and later retracts the attack while using an additional sprite slot. This independently corroborates the character-index mapping.

## OAM / mapper coordination

- `$0700-$07FF` — OAM shadow page
- `$3A` — NMI-interrupted MMC1 serial-write flag
- `$3B` — persistent PRG bank restored by NMI
- `$0639/$063A/$063E/$063F` — synchronized PRG-bank path/critical-section state

## Password serialization

Fixed `$C458` stages:

- `$058C-$059F` -> `$0110-$0123` (Seiya, Hyoga, Shun, Shiryu)
- `$05AA-$05AC` -> `$0130-$0132`
- `$067D` -> `$0133`
- `$06CD` -> `$0134`
- `$0587-$058A` -> `$0135-$0138`

Ikki's `$05A0-$05A4` snapshot record is not serialized.

## External battle-mode candidates

Community cheat material points to `$05BC/$05BD` and `$05CE/$05CF` as battle-mode Cosmo/Life structures with nearby maxima. Their exact relationship to platform/persistent arrays still requires tracing.

## Next tests

1. Trace projectile hit detection and damage application.
2. Map tile-class meanings consumed by the eight collision probes.
3. Enumerate remaining `$4D/$4E` action states and transitions.
4. Map `$05BC-$05D0` boss-battle structures.
5. Add runtime breakpoints when a debugger-capable NES emulator becomes available.
