# Platform player — state machine, movement and attack profiles

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: substantial static reconstruction. Input, horizontal movement, jump profile selection and Cosmo-dependent projectile range are directly tied to code/tables. Exact collision-sample geometry and full damage/hitbox semantics remain open.

## Gameplay controller reader

Core platform gameplay uses fixed-bank `$C4E4`, which reads controllers into:

- `$3D` = controller 1 held state
- `$3E` = controller 2 held state

`$3D` bit layout:

| Mask | Button |
|---:|---|
| `$80` | A |
| `$40` | B |
| `$20` | Select |
| `$10` | Start |
| `$08` | Up |
| `$04` | Down |
| `$02` | Left |
| `$01` | Right |

Menu/password code uses a separate reader at bank 0 `$8B1F`.

## Main platform state

Fixed-state dispatch recognizes `$00 == $20` as the active platform gameplay family. That path reads controller input, invokes support logic and maps bank 3 for the bulk of movement/collision/attack work.

## Core player state

### Position / camera

- `$3F` — horizontal player coordinate
- `$40` — principal vertical player coordinate
- `$41` — vertical high/page component candidate
- `$44/$45` — horizontal scroll/camera pair

Horizontal movement explicitly hands displacement between player X and camera scroll at screen/world thresholds rather than treating them as a single coordinate.

### Facing

`$42` is direction/facing state. Right movement writes `$40`; left movement writes zero. Sprite/collision setup copies this state into working records.

### Action state

`$4D` is the active/next action-animation state.

`$4E` is not simply another independent action variable: fixed gameplay code copies `$4D -> $4E` before bank-3 processing. It therefore behaves as a frame-start/latched state used to choose the current state handler while `$4D` can be changed for the next phase.

Observed families:

- `$00` idle/neutral
- `$10-$1F` locomotion/animation
- `$20` crouch
- `$30-$33` jump family; low bits encode horizontal direction at jump start
- `$50` fall/vertical transition family
- `$80` damage/fall/death-related family

Other high families appear in special/death animation paths and still need classification.

## Horizontal locomotion — bank 3 `$AB3F+`

### Right

- checks `$3D & $01`;
- sets facing;
- tests collision descriptors around `$50/$51`;
- normally adds `$0387` to `$3F`;
- near horizontal thresholds can advance `$44/$45` instead;
- moves `$4D` into locomotion family.

### Left

- checks `$3D & $02`;
- flips facing;
- tests corresponding collision descriptors around `$53/$54`;
- normally subtracts `$0387` from `$3F`;
- coordinates the same player/camera handoff.

### Crouch

Down (`$04`) enters `$4D = $20` under the ordinary locomotion path.

## Horizontal movement deltas

Bank 1 `$9211+` derives `$0387-$0389` from current Saint and frame parity.

For current Saint index other than 1:

- `$0387 = 1`
- `$0388 = 1/2` alternating with frame parity
- `$0389 = 0/1` alternating

For index 1:

- `$0387 = 1/2` alternating
- `$0388 = 2`
- `$0389 = 1`

During airborne horizontal control, the engine selects among these according to whether held direction matches/opposes/no direction relative to the jump.

Approximate average displacement implied by the alternating values:

- ordinary slots: neutral air drift 1 px/frame; with-direction ~1.5; opposite ~0.5
- slot 1: neutral ~1.5; with-direction 2; opposite 1

This is a strong behavioral signature for identifying the faster-air-control Saint.

## Jump initiation — `$BB76+`

A begins jump when busy/collision gates allow it.

At jump creation:

- base state family is `$30`;
- simultaneous Left/Right contributes the low two state bits, producing `$30-$33`;
- when no horizontal direction is held, Up is checked;
- A+Up sets `$038A = $30`, selecting the high-jump profile.

`$49` functions as a jump phase/table index and `$4A` participates as jump/airborne latch state.

## Jump physics are table-driven

The original does **not** use a conventional continuous gravity accumulator. Around bank 3 `$BCD3+`, it chooses a signed vertical-displacement table and a duration.

### Normal straight jump

A without Up and without horizontal direction:

- duration: 32 frames/phase units
- common curve pointer: bank 3 `$BF49`
- maximum ascent from extracted table: about 58 px

### A+Up high jump

Per-Saint duration table at `$BCF0`:

`[60, 50, 40, 40, 50]`

Curve pointer table at `$BFD7` resolves to:

- slot 0 -> `$BF69`
- slot 1 -> `$BFA5`
- slot 2 -> fixed `$D8D6`
- slot 3 -> fixed `$D8D6`
- slot 4 -> `$BFA5`

Extracted maximum ascents:

| Slot | Duration | Max ascent |
|---:|---:|---:|
| 0 | 60 | 103 px |
| 1 | 50 | 88 px |
| 2 | 40 | 71 px |
| 3 | 40 | 71 px |
| 4 | 50 | 88 px |

### Directional/forward jump

Duration table at `$BCF5`:

`[54, 40, 44, 44, 54]`

Pointer table at `$BFE1` resolves to per-slot curves in bank 3/fixed bank.

Extracted maximum ascents:

| Slot | Duration | Max ascent |
|---:|---:|---:|
| 0 | 54 | 39 px |
| 1 | 40 | 33 px |
| 2 | 44 | 34 px |
| 3 | 44 | 34 px |
| 4 | 54 | 39 px |

### Falling phase

After the selected table duration, the ordinary fall path uses a fixed downward increment of about `+3 px/frame` until landing/collision resolution.

All of these values can be re-extracted from a user-supplied canonical ROM with `tools/physics/extract_platform_profiles.py`.

## Collision sampling

`$4F-$56` are eight environment/collision descriptors sampled around the player. Movement/vertical paths compare them against tile classes including `$78`, `$80`, `$88`, `$90`, `$E0`, `$F0`, `$F8/$F9`.

The structure is proven; exact spatial assignment (head/feet/left/right probes) is not. Keep them as `collision_samples[8]` until sample-generation code is geometrically reconstructed.

## Attack input — B

Bank 3 `$BBCA+` handles B attack after busy/state gates. It enters character-specific attack logic and uses OAM/projectile slots around:

- `$0730/$0731`
- `$0738/$0739`
- `$0740/$0741`

Associated counters/metadata are stored around `$038E-$0390`.

At least one update path around `$A311+` decrements `$038E`, advances the projectile/attack and retires/hides its sprites when the range/lifetime expires.

## Cosmo controls platform attack reach

For Saint slots 0-3, bank 3 uses the selected Saint's **Cosmo hundreds digit** to choose a range/lifetime parameter from the table at `$BCAE`:

`index = floor(Cosmo_hundreds / 2) * 4 + saint_index`

Extracted table:

| Cosmo hundreds | slot 0 | slot 1 | slot 2 | slot 3 |
|---|---:|---:|---:|---:|
| 0-1 | 3 | 1 | 4 | 6 |
| 2-3 | 6 | 3 | 10 | 10 |
| 4-5 | 12 | 8 | 16 | 14 |
| 6-7 | 24 | 12 | 22 | 18 |
| 8-9 | 48 | 16 | 28 | 22 |

Slot 4 bypasses the table and receives `60`.

This is an important ORIGINAL SPEC rule: Cosmo is not merely a battle-screen resource; it changes the practical reach/lifetime of platform attacks.

## Current Saint-identity evidence from behavior

Not yet final, but increasingly constrained:

- slot 0: strong Seiya candidate — uniquely tallest high jump (103 px) and longest non-slot4 high-Cosmo attack reach;
- slot 4: strong Ikki candidate — unique initial Life/Cosmo 499/499, fixed attack reach 60 and high-jump profile shared with slot 1;
- slot 3: strong Shun candidate — attack path can employ multiple projectile/OAM slots, consistent with chain behavior;
- slots 1 and 2 remain to be separated rigorously between Shiryu and Hyoga.

These identities should not be promoted to `CONFIRMED` until the ROM's selection/name/graphics tables provide a direct mapping.

## REBORN design consequence

The original's feel can be preserved far more accurately than by approximating NES movement with generic modern physics. We can represent each original jump as a deterministic displacement curve and each Saint's movement/attack profile as data, then render/interpolate that behavior at modern resolution and frame rate.

A future REBORN compatibility mode could therefore reproduce original trajectories exactly while a redesigned mode can deliberately smooth or expand them.

## Next extraction targets

1. reconstruct geometric positions of collision samples `$4F-$56`;
2. trace projectile hit detection/damage, not only lifetime;
3. close Saint index -> identity mapping from ROM assets/tables;
4. enumerate all remaining action states and transition conditions;
5. convert jump/air-control behavior into clean-room parity test vectors;
6. dynamically validate table timing once a debugger-capable emulator is available.
