# Platform attacks — projectile slots and Saint-specific behavior

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: attack input/latch, busy cadence, slot allocation, origin, range, generic `$64/$65` projectile update and Shun `$54/$55` extend/retract update are statically reconstructed and represented in clean-room executable code.

## Attack entry — bank 3 `$BBCA+`

`$3D & $40` is the live B input.

Two global bytes gate creation:

- `$4C` — B-button latch;
- `$4B` — attack busy/cooldown counter.

### B latch ordering

The exact ordering is important:

1. if B is not held, `$4C=0` and return;
2. if B is held and `$4C!=0`, return;
3. otherwise set `$4C=1` **immediately**;
4. only then test `$4B`, slot availability and player Y.

Consequences:

- pressing B while the busy counter is nonzero consumes the press;
- pressing B when all permitted slots are occupied consumes the press;
- pressing B at an invalid low-screen Y also consumes the press;
- in all those cases B must be released before another attempt.

This is an input-latch rule, not merely a projectile cooldown.

### Busy counter `$4B`

A successful creation sets:

`$4B = 1`.

Bank 1 `$926C` advances a nonzero value once per relevant update:

`1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 0`.

Zero remains zero.

Thus the original uses a compact seven-step busy cycle while the B latch separately prevents autorepeat from a held button.

## Slot selection

Attack objects use three 8-byte records:

- slot 0: `$0730-$0737`, lifetime/range `$038E`;
- slot 1: `$0738-$073F`, lifetime/range `$038F`;
- slot 2: `$0740-$0747`, lifetime/range `$0390`.

Record offset `+1` is the type/active marker; `$FE` is inactive.

Allocation order by internal platform Saint index `$03`:

| index | Saint | allocation order |
|---:|---|---|
| 0 | Seiya | slot 0 |
| 1 | Shun | slot 0 |
| 2 | Hyoga | slot 0 |
| 3 | Shiryu | slot 2 -> slot 1 -> slot 0 |
| 4 | Ikki | slot 1 -> slot 0 |

Therefore the original engine already gives Shiryu capacity for three ordinary concurrent attack objects and Ikki capacity for two. This is a gameplay distinction, not just an OAM implementation accident.

If no permitted slot has type `$FE`, creation stops before sound playback.

## Sound and height rejection

After a slot has been selected, the game requests an attack sound:

- ordinary Saints: `$24`;
- Shun/index 1: `$34`.

Only **after sound playback** does `$BC30` reject creation when:

`player_y >= $90`.

A height-rejected attack therefore:

- has already consumed B latch `$4C`;
- can play its attack sound;
- does not set busy `$4B`;
- does not initialize the selected object.

## Successful creation

A valid attack sets `$4B=1` and initializes the chosen record.

### Origin Y — `$BCC2`

Base origin:

`player_y + 7`.

If frame-start action snapshot `$4E` is **exactly** `$20`:

`player_y + 15`.

This is an exact equality test, not a generic `$2x` family test.

### Origin X and facing

Facing is copied from `$42`.

- facing bit `$40` set/right: `player_x + $12`;
- facing bit clear/left: `player_x - 9` (`+$F7` in 8-bit arithmetic).

### Object type

- Shun/index 1: `$54` and `$0391=5`;
- all other Saints: `$64`.

### Grounded movement-state reset

After creation, if frame-start action `$4E` is in family `$10-$1F` **and** jump phase `$49==0`, current action `$4D` is reset to zero.

This lets an ordinary grounded-moving attack return the player toward neutral without applying the same reset while airborne.

## Cosmo-dependent lifetime/range

For Saints 0–3 in ordinary platform substates, `$BC73+` reads the high packed-BCD Cosmo byte (`$64 + 2*index`), extracts the hundreds digit, groups it in pairs, and indexes `$BCAE`.

Equivalent clean-room rule:

`bracket = floor((Cosmo / 100) / 2)`.

Table:

| Cosmo hundreds | Seiya 0 | Shun 1 | Hyoga 2 | Shiryu 3 |
|---|---:|---:|---:|---:|
| 0–1 | 3 | 1 | 4 | 6 |
| 2–3 | 6 | 3 | 10 | 10 |
| 4–5 | 12 | 8 | 16 | 14 |
| 6–7 | 24 | 12 | 22 | 18 |
| 8–9 | 48 | 16 | 28 | 22 |

Two paths bypass the table and use fixed value `60`:

- Ikki/index 4;
- engine substate `$01 >= $30`.

The second condition was missing from an earlier provisional description and is now explicitly preserved.

## Generic projectile `$64/$65` — `$A250+`

For non-Shun Saints, attack slots are sent through the common object updater. Type `$64/$65` is recognized by masking bit 0.

### Correction: movement is 5 px/update

The exact branch is:

`$A266: LDX #$05`.

Therefore the generic attack projectile moves **5 pixels per update**, not 3. The earlier 3-pixel note was incorrect; `3` belongs to other object families handled by the same updater.

### Update ordering

For `$64/$65`:

1. decrement the slot's lifetime/range byte;
2. if the new value is zero, retire immediately — no movement that update;
3. set visible type to `$64 + ($3C & 1)`, toggling `$64/$65` with frame parity;
4. move X by 5 according to facing;
5. write an auxiliary X 8 px behind/ahead of the moved coordinate;
6. if moved X enters `$F8-$FF`, retire.

Thus a starting range of 3 produces two 5-pixel movement updates before retirement on the third update, barring edge removal.

Facing semantics:

- facing clear/left: `X -= 5`, auxiliary `X + 8`;
- facing `$40`/right: `X += 5`, auxiliary `X - 8`.

Generic retirement writes:

- Y/offset 0 = `$F0`;
- type/offset 1 = `$FE`;
- offset 4 = `$F0`;
- offset 5 = `$FE`.

## Shun chain `$54/$55` — `$A311+`

Shun does not update attack slots through the generic `$64` path. If slot 0 is type `$54`, `$A311` maintains a two-part extend/retract chain.

Creation initializes:

- slot 0 type `$54`;
- slot 0 range counter `$038E` from the Cosmo table;
- extension `$0391 = 5`.

### Extension

While `$038E != 0`:

1. decrement `$038E`;
2. add 5 to `$0391`;
3. rebuild both chain segment positions around the **current** player position.

Note that when range changes `1 -> 0`, that same update still performs one final `extension += 5`. Retraction starts on the following update.

### Retraction

Once `$038E == 0`:

`$0391 -= 5` per update.

Value zero remains active for one rebuilt frame. On the next subtraction, the 8-bit result has the sign bit set and both chain segments are retired.

Shun retirement writes Y `$F0` and type `$FE` to slots 0 and 1.

### Dynamic two-part positions

`$BCC2` is called every chain update, so both segments track the player's current attack-origin Y rather than keeping creation Y.

Slot 1 becomes type `$55` and copies slot 0 facing.

For right facing:

- near segment (slot 1): `player_x + $10 + extension`;
- far segment (slot 0): `near + 8`.

For left facing:

- near segment: `player_x - 8 - extension`;
- far segment: `near - 8`.

If the **far** segment enters `$F8-$FF`, the engine forces `$038E=0`, causing retraction to begin on the next update.

This is a genuinely different weapon model, not a reskinned generic projectile, and is a ROM-internal mechanical anchor for Shun/index 1.

## Character mechanical identity

With the character-index map now independently reconstructed, the attack subsystem can be named directly:

- Seiya: one generic slot, range grows most strongly with Cosmo;
- Shun: one initiating slot plus synthesized returning two-part chain;
- Hyoga: one generic slot with comparatively long range table;
- Shiryu: three-slot allocation chain;
- Ikki: two-slot allocation and fixed range 60.

These differences should survive REBORN even after NES slot limits and coarse sprite movement are replaced by modern animation/hitboxes.

## Clean-room executable implementation

`PlatformAttackSystem` models:

- B latch and rejection ordering;
- busy cadence;
- per-Saint slot selection;
- sound timing versus height rejection;
- attack origins;
- Cosmo range table and fixed-60 bypasses;
- generic `$64/$65` update at 5 px;
- retirement markers;
- Shun extension/retraction and dynamic two-segment reconstruction;
- grounded moving-family reset after attack.

No ROM data blob is embedded; only reconstructed semantic constants/tables needed for parity are represented.

## Remaining targets

1. compose attack processing into the frame-level platform session at the exact `$AAE4 -> $BBCA -> later $A22C` ordering;
2. isolate projectile/enemy hitbox tests and hit consumption;
3. connect `PlatformDamage` to the exact object-hit branch;
4. identify whether any special platform modes consume Cosmo differently when firing;
5. preserve the original compatibility behavior while designing richer REBORN attack animation, collision shapes and effects.
