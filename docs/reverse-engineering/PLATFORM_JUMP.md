# Platform jump state machine

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: A-button initiation, jump-profile selection, vertical phase progression, floor/ceiling resolution and the structure of airborne horizontal control are statically reconstructed from PRG bank 3 `$BB76` and `$BCD3-$BF48`.

## Initiation — `$BB76`

A jump can start only when:

- A is held (`$3D & $80`);
- jump phase `$49 == 0`;
- A latch `$4A == 0`;
- Down is not held in this standing path;
- jump lock/countdown `$038D == 0`.

If Down or `$038D` blocks the attempt, `$4A` becomes `$FF` and another start is impossible until A is released.

On a valid start:

- `$49 = 1`;
- `$4A = 1`;
- `$038A = 0` initially;
- current horizontal bits `input & $03` determine action state.

State mapping:

| input at takeoff | `$4D` |
|---|---:|
| no horizontal direction | `$30` |
| Right | `$31` |
| Left | `$32` |
| Right+Left | `$33` |

Only the no-horizontal case can become a high jump. If Up is also held, `$038A=$30` and `$4D` stays `$30`.

Thus `Up+A+Right` is a directional jump, not a high jump.

Releasing A at any time clears `$4A`. While already airborne, pressing A again cannot restart because `$49 != 0`, but it also does not relatch `$4A`; this means a release/repress during a jump can leave the latch clear for a possible immediate jump after landing.

## Profile selection — `$BCD3`

Every active jump frame chooses a duration before advancing phase.

### `$4D == $30`

- `$038A == 0`: ordinary vertical profile, duration 32;
- `$038A != 0`: per-Saint high-jump profile.

### `$4D != $30` inside family `$30-$33`

Per-Saint directional profile.

Durations:

| internal Saint | high | directional |
|---|---:|---:|
| Seiya 0 | 60 | 54 |
| Shun 1 | 50 | 40 |
| Hyoga 2 | 40 | 44 |
| Shiryu 3 | 40 | 44 |
| Ikki 4 | 50 | 54 |

The engine stores `duration / 2` in `$038B`. This midpoint is used by landing and bottom-side collision logic.

## Phase ordering

A new jump begins with `$49=1`.

On the same gameplay frame, `$BCD3` runs:

1. select duration and midpoint;
2. if the **current** phase is already at/after midpoint, run floor landing resolution before changing Y;
3. increment `$49`;
4. if the new phase is below duration, read table index `phase - 2`;
5. otherwise stop reading the table and apply fixed downward movement `+3 px/frame`;
6. test the pre-sampled head descriptor `$56` for ceiling interruption;
7. run airborne horizontal control.

Therefore the first jump update is:

`phase 1 -> phase 2 -> table index 0`.

This ordering is important for parity and is not equivalent to incrementing an animation timer after physics.

## Vertical delta convention

The table contains signed bytes:

- positive value: `player_y -= delta` -> rise;
- negative value: `player_y += abs(delta)` -> descend;
- zero: vertical pause.

The companion `$41` byte is decremented/incremented when the 8-bit Y arithmetic borrows/carries. During the post-table `+3` fall, a low-byte overflow sets `$41=0` through the original special path.

## Reconstructed profile metrics

### Ordinary vertical

- duration 32;
- table-controlled entries 30;
- maximum rise 58 px.

### High jump

| Saint | max rise |
|---|---:|
| Seiya | 103 px |
| Shun | 88 px |
| Hyoga | 71 px |
| Shiryu | 71 px |
| Ikki | 88 px |

### Directional jump

| Saint | max rise |
|---|---:|
| Seiya | 39 px |
| Shun | 33 px |
| Hyoga | 34 px |
| Shiryu | 34 px |
| Ikki | 39 px |

`PlatformJumpCore` encodes the exact frame-shaped displacement curves as run-length mechanical data, not an approximate gravity equation.

## Landing — `$B8CD`

Landing checks begin only when the current jump phase has reached the selected midpoint.

For ordinary upper-screen floor resolution (`player_y < $86`):

- descriptor `< $80`: no landing;
- `$80-$EF`: land only when low Y nibble `< 6`, snap to 16-pixel row;
- `$F0-$FE`: land only when low Y nibble `>= 8`, snap to row + 8 px;
- `$FF`: land at dynamic Y `$039B`.

A successful landing:

- sets `$49=0`;
- sets `$4D=0`;
- clears `$038D`.

For `Y >= $86`, floor descriptors `$F8/$F9` use the special Y=`$88` landing path and interact with `$76`.

If Y reaches `$A0`, `$41==0`, and no `$F8/$F9` rescue applies, the player enters action family `$80` with `$76=$80` and `$49=0`: the fall-out/damage path.

## Ceiling interruption

After the vertical displacement, head probe `$56` is tested.

Only descriptor family `$E0-$EF` interrupts the jump:

- `$49=0`;
- `$4D=$50`.

The probe was sampled before bank-3 player simulation for the current frame, so ORIGINAL SPEC deliberately uses the pre-move descriptor rather than inventing a post-move collision query.

## Airborne horizontal architecture

The horizontal behavior differs sharply between state `$30` and directional states `$31-$33`.

### Vertical family `$30`

The player can steer freely after takeoff:

- live Right moves right;
- live Left moves left;
- Right has priority if both are held;
- speed is `$3C & 1`, therefore alternates 0/1 px;
- direction is not locked by takeoff.

When neither direction is held in the second half of the jump, the wide lower probes can apply one-pixel side corrections away from blocking descriptor families.

### Directional state `$31` — right family

Horizontal sign remains right for the jump.

Live input changes only the magnitude:

- hold Right: `$0388` (accelerated/same-direction air control);
- hold Left: `$0389` (decelerated/opposite-input control);
- hold neither: `$0387` (base);
- if both are held, Left is checked first in this right-family branch, so `$0389` wins.

The player/camera handoff remains active in air: before screen X `$80`, move the player; afterward advance the camera until the terminal camera region; on the final screen, move local player X toward `$E0`.

### Directional states `$32/$33` — left family

Both states use the left-family path.

Magnitude selection:

- hold Left: `$0388`;
- hold Right: `$0389`;
- neither: `$0387`;
- if both are held, Right is tested first, so `$0389` wins.

The left family never scrolls the camera backward. Candidate X below `$10` cancels the horizontal directional family into `$30`.

### Collision cancellation

Directional horizontal collision does not cancel the vertical jump. Instead it changes `$4D` to `$30` and clears horizontal amount `$43`.

On the following frame, profile selection sees a vertical state. Because `$49` is preserved, the existing phase continues against the vertical profile/duration. This is an original quirk and must not be replaced with a generic velocity reflection.

## Clean-room implementation

`PlatformJumpCore` currently covers:

- A latch/start rules;
- state `$30-$33` selection;
- high-vs-directional precedence;
- exact per-Saint vertical curves and duration;
- midpoint landing-before-displacement ordering;
- post-curve +3 px fall;
- ordinary/dynamic/special floor landing;
- lower-screen fall-out path;
- ceiling interruption.

Airborne X/camera control is being kept as a separate component so it can be tested independently against the asymmetric original branches before composition into the frame-level platform session.
