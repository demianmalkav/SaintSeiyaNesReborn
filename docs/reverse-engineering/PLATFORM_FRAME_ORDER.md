# Active platform frame ordering

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the fixed-bank ordering around platform exit, player simulation and frame-counter update is statically reconstructed.

## Active platform entry

Fixed bank `$C2F5+` recognizes `$00 == $20` as active platform gameplay.

At frame entry it first copies:

`$4D -> $4E`

so later routines can distinguish the action state that existed at frame start from mutations made during the frame.

Controller input is then sampled through `$C4E4`.

## Relevant order

The active path around `$C307-$C336` is:

1. fixed/platform support work (`$C2CE`);
2. common entity spawn attempt (`$B6D0`);
3. map PRG bank 1;
4. **evaluate platform exit at `$969D`**;
5. if the exit changed global mode, leave the normal platform path;
6. otherwise perform additional fixed support work;
7. map PRG bank 3;
8. call `$AAE4` — player/action simulation, including grounded movement/jump/attack paths;
9. run `$B94B` and the platform object pipeline;
10. rendering/streaming support;
11. return through `$C3FC`.

At `$C402` the engine increments frame counter `$3C`, then prepares per-frame support values including the movement cadence used on the **next** active frame.

## Exit-before-movement consequence

The exit gate is checked before the current frame's player movement.

Example for a normal House approach:

- frame N begins at `player_x=$CF`, `player_y=$40`;
- the exit gate rejects because X is below `$D0`;
- bank-3 movement advances X to `$D0`;
- frame N ends normally;
- frame N+1 evaluates `$969D` first and now accepts the exit;
- bank-3 movement for frame N+1 is never reached.

The native compatibility runtime must preserve this one-frame ordering. Evaluating completion after movement would make stage transitions one frame early.

## Frame-counter consequence

`$3C` increments after simulation at `$C402`.

Movement increments `$0387-$0389` are refreshed after that increment, so the values already present at the start of a platform frame correspond to that frame's current `$3C` parity.

The executable grounded session therefore uses:

`current frameCounter -> current movement increment -> movement -> frameCounter + 1`

and does **not** advance the counter if the exit gate diverts the engine before the normal end-of-frame path.

## First executable composition

`PlatformGroundedSession` intentionally implements only the fully reconstructed grounded horizontal slice:

`exit gate -> collision probes -> grounded horizontal/camera step -> frame-counter advance`

It does not pretend that jump, crouch/drop, attack, damage and entity processing are complete. Those subsystems will be composed only after their frame-order and state transitions are promoted to the same evidence level.
