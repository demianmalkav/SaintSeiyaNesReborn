# Platform air control — horizontal behavior during jump

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: bank 3 `$BDA2-$BF48` is reconstructed into clean-room horizontal air-control rules. Vertical displacement/landing remains a separate step and is composed later in frame order.

## Two different control models

The original distinguishes the jump state captured at takeoff:

- `$30`: vertical/standing or Up+A high jump;
- `$31`: directional jump started with Right;
- `$32`: directional jump started with Left;
- `$33`: both horizontal directions were held at takeoff; this follows the **left-trajectory** branch.

This state does more than select an animation. It changes the horizontal-control model for the whole airborne phase unless a collision collapses the directional trajectory back to `$30`.

## Vertical-jump drift (`$30`)

A vertical/high jump can be nudged after takeoff.

- Right is tested before Left, so Right wins if both are held.
- drift amount is `$3C & 1`: alternating 0/1 px according to frame parity;
- movement still passes through airborne side/floor collision gates;
- during the descending half, if no usable input motion occurs, lower-side geometry can apply a one-pixel landing correction.

This means standing/high jumps have deliberately weak air steering rather than sharing the full directional-jump speed.

## Directional trajectory (`$31-$33`)

The horizontal direction is fixed by the takeoff state; current input changes speed but does not reverse the trajectory.

Right-started `$31`:

- hold Left: use `$0389` (counter-steer / reduced step);
- otherwise hold Right: use `$0388` (strong step);
- neither: use `$0387` (baseline step).

Left-started `$32/$33` mirrors this:

- hold Right: `$0389`;
- otherwise hold Left: `$0388`;
- neither: `$0387`.

For the internal Saint index 1, the already reconstructed cadence makes these values systematically larger than for the other indices.

## Collision gates

Airborne right blocks on:

- lower/right or late floor probe: `$80-$87` and `$E0-$EF`;
- upper/right probe: `$E0-$EF` only.

Airborne left blocks on:

- lower/left or late floor probe: `$88-$8F` and `$E0-$EF`;
- upper/left probe: `$E0-$EF` only.

The late wide floor probe is included only once the incremented jump phase has reached the half-phase marker stored in `$038B`.

On a directional collision or left-edge rejection, the original writes `$4D=$30`: horizontal trajectory control collapses to the vertical-jump family while the jump phase itself continues. REBORN compatibility mode preserves this instead of cancelling the whole jump.

## Camera handoff

Directional/right airborne movement uses the same basic forward-only camera concept as grounded movement:

- while `player_x + step < $80`, move the player on screen;
- otherwise advance horizontal scroll;
- in the final camera region, stop scrolling and allow local player X to advance until the `$E0` boundary.

The same per-substate scroll-high cap table is reused.

## Descending side correction

For `$30` during the second half of the jump:

- an obstructing left-lower family (`$88-$8F` / `$E0-$EF`) pushes right by one pixel, or advances the camera using `$0387` once player X is in the camera region;
- otherwise an obstructing right-lower family (`$80-$87` / `$E0-$EF`) pushes left by one pixel.

The left correction is evaluated first.

## Clean-room implementation

`PlatformAirborneHorizontalMotion` reproduces:

- takeoff-family directional semantics;
- current-input step selection;
- weak 0/1 px vertical-jump drift;
- airborne lower/upper/late-floor collision gates;
- directional collapse to `$30`;
- camera handoff and final-screen boundary;
- descending landing-side correction.

The implementation deliberately does not perform vertical movement. That separation mirrors the reverse-engineering evidence and makes the final frame composition testable.
