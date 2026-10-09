# Crouch, drop-through and fall state

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: action families `$20` and `$50` are statically reconstructed from PRG bank 3 `$B829-$B8CC` plus the shared landing routine `$B8CD`.

## Entering crouch

The ordinary grounded path sets `$4D=$20` when Down is held. Because the frame-start action is latched in `$4E`, `$B829` executes on the following frame while the crouch state remains active.

## Crouch behavior — `$B829`

While `$4E==$20`:

- Right updates facing `$42=$40`;
- otherwise Left updates facing `$42=$00`;
- Right therefore has priority when both are held;
- no horizontal movement occurs in this routine.

If Down is released:

`$4D = 0`

and crouch ends.

## Independent A latch `$038C`

Drop-through uses a latch separate from jump latch `$4A`.

If A is not held while crouched:

`$038C = 0`

If A is held and `$038C != 0`, no new drop attempt occurs.

On the first eligible A press, the game writes `$80` to `$038C` **before** checking terrain and player Y. Consequently even a rejected drop consumes the press; A must be released during a later crouch frame before another attempt can occur.

This latch is not cleared by the `$50` fall or by the shared landing routine.

## Down+A drop-through gate

After latching A, the center floor descriptor `$4F` is checked:

- `$E0-$EF`: reject drop;
- `< $E0`: allow;
- `>= $F0`: allow.

The player must also satisfy:

`player_y < $80`.

If accepted:

- `$4D = $50`;
- `player_y += 6` immediately.

The +6 displacement occurs on the initiation frame before the dedicated `$50` fall routine runs on later frames.

## Fall state `$50` — `$B87D`

Each fall frame first applies:

`player_y += 3`.

Unlike table-driven jump Y arithmetic, this routine simply updates the low Y byte and does not explicitly update `$41` on the +3 operation.

Then it performs optional horizontal correction from the wide floor-side probes.

### Left floor probe `$55` first

Blocking families:

- `$88-$8F`;
- `$E0-$EF`.

If blocked and local player X is below `$80`:

`player_x += 1`.

If player X is `$80` or greater, the routine calls the same camera-advance helper used by rightward movement; that helper adds current `$0387` to scroll. After this camera branch the routine writes literal:

`$43 = 1`.

This is deliberately not the actual `$0387` magnitude; Shun can advance camera by 2 on an odd frame while `$43` still receives 1.

Any successful left-side correction jumps directly to floor resolution. The right-side probe is not tested afterward.

### Right floor probe `$52`

Tested only if the left branch did not correct.

Blocking families:

- `$80-$87`;
- `$E0-$EF`.

If blocked:

`player_x -= 1`

and `$43=0`.

Thus fall side correction is **left-priority and mutually exclusive**, unlike the no-input second-half jump correction path where both sides can be visited sequentially.

## Shared landing

After +3 Y and optional side correction, `$B87D` calls `$B8CD`, the same floor resolver used by the second half of a jump.

Therefore `$50` inherits the already documented semantics:

- `$80-$EF` row-boundary floor snap;
- `$F0-$FE` +8 px floor snap;
- `$FF` dynamic `$039B` floor;
- `$F8/$F9` lower-screen special landing at Y `$88`;
- Y `$A0` / `$41==0` fall-out into action family `$80`;
- successful landing clears `$49`, `$4D`, and `$038D`.

It does **not** clear drop latch `$038C`.

## Interaction with attack path

After both crouch `$B829` and fall `$B87D`, the player dispatcher calls `$BBCA`, which contains the B-button attack path. This means attack handling remains reachable from these action families and should be composed separately rather than assuming crouch/fall monopolize input.

## Clean-room implementation

`PlatformCrouchDrop` implements:

- crouch facing and Down release;
- independent `$038C` latch;
- exact Down+A gate and +6 initiation displacement;
- +3 fall cadence;
- left-priority side correction;
- camera correction with `$0387` / literal `$43=1` quirk;
- shared floor, special landing and fall-out semantics.
