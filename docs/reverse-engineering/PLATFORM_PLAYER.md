# Platform player — input and movement skeleton

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: first static pass. Input bit mapping and several player-state transitions are directly proven by code. Collision field semantics are still being named conservatively.

## Gameplay controller reader

The engine has more than one input subsystem. The title/password code uses the bank-0 reader at `$8B1F`, while core gameplay also uses fixed-bank reader `$C4E4`.

`$C4E4` strobes `$4016/$4017` and shifts eight serial button states into:

- `$3D` = controller 1 held-state bitfield
- `$3E` = controller 2 held-state bitfield

Given NES serial order and the routine's `ROL` accumulation, `$3D` maps as:

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

This mapping is independently consistent with the known platform controls and the behavior of the routines below.

A second newer-style reader exists at `$E814`, storing current/previous states in `$A5-$A8` and producing filtered/edge information in X/Y. This appears to serve another engine mode/subsystem and should not be conflated with `$3D/$3E`.

## Main gameplay state `$20`

Fixed bank state dispatcher `$C2F5+` recognizes `$00 == $20` as a major gameplay state.

In that path the engine:

1. calls `$C4E4` to read controller(s);
2. runs bank-0 and bank-1 support logic;
3. executes bank-3 gameplay routines including `$AAE4`, `$B94B`, etc.;
4. updates common engine/render state.

This is strong evidence that state family `$20` is associated with active platform gameplay.

## Core player variables

### `$3F` — horizontal player coordinate

Bank 3 horizontal-motion code reads and changes `$3F` directly.

Right movement path `$AB3F+` adds movement amount `$0387` to `$3F`, subject to collision/tile tests and camera logic.

Left movement path `$ABE5+` subtracts `$0387` from `$3F`, subject to corresponding collision/tile tests.

Status: `CONFIRMED` as the player's horizontal position component within the active platform coordinate system. Exact world-vs-screen interpretation at scrolling boundaries still needs dynamic tracing.

### `$40` — vertical player coordinate / vertical-motion component

Bank 3 copies `$40` alongside `$3F` and facing/state into collision/render working values. Jump/fall routines repeatedly adjust `$40`, including `$B87D+` and other vertical collision paths.

Status: `CONFIRMED` as the principal vertical position/motion coordinate used by platform physics; exact coordinate convention and subpixel interpretation remain to be resolved.

### `$41`

Used with `$40` in vertical-limit/overflow checks. Likely high byte / vertical-page state, but not yet promoted.

### `$42` — horizontal facing/direction

Right-input path writes `$40` to `$42`; left-input path writes `$00`.

This value is copied into sprite/collision working state by `$B94B`.

Provisional semantic name: `player_facing`.

### `$44/$45` — horizontal camera/world-scroll pair

When the player approaches horizontal thresholds, right movement can advance `$44`, carrying into `$45`; left movement can reduce `$44/$45` depending on the player's screen position and stage limits.

The fixed NMI also uses `$44` in PPU scrolling.

Status: strong `CONFIRMED/INFERRED` split:

- `$44` definitely participates in PPU scroll and horizontal gameplay movement;
- `$45` is the associated coarse/high scroll component.

Provisional names: `scroll_x_low`, `scroll_x_high`.

### `$4D/$4E` — player action/animation state

Bank 3 writes high-nibble action families into `$4D` and compares previous/current `$4E` families.

Observed state families include:

- `$00`: neutral/idle family;
- `$10`: walking/moving family;
- `$20`: crouch/down family;
- `$30+`: jump family with low bits affected by horizontal input;
- `$50`: vertical/jump-like action path;
- `$80`: hit/death/damage-related family appears elsewhere.

The exact animation/state ontology is not yet complete, so these names remain provisional except where behavior is directly tied to input.

## Horizontal movement

Bank 3 `$AB3F+` handles ordinary right/left locomotion.

### Right

- tests `$3D & $01`;
- sets facing `$42 = $40`;
- checks collision/tile fields around `$50/$51`;
- normally adds `$0387` to player X `$3F`;
- once the player crosses screen/world thresholds, may advance camera scroll `$44/$45` instead;
- changes `$4D` into movement family `$1x`.

### Left

- tests `$3D & $02`;
- sets facing `$42 = 0`;
- checks corresponding collision fields `$53/$54`;
- normally subtracts `$0387` from `$3F`;
- coordinates with the scroll boundary so player and camera do not move independently through blocked tiles.

### Down/crouch

- tests `$3D & $04`;
- writes action state `$4D = $20`.

This matches the documented platform control `Down = crouch`.

## Jump input

Routine `$BB76+` handles A-button initiation in the ordinary locomotion path.

- tests `$3D & $80` (A);
- rejects the action under several busy/collision conditions;
- sets jump/action state;
- uses `$3D & $03` so simultaneous left/right modifies the low bits of the jump state;
- tests `$3D & $08` (Up), providing a separate high-jump behavior path.

This aligns with the documented controls:

- A = jump;
- A + Up = higher jump;
- A + Down = drop from a platform.

Exact initial velocity values and high-jump delta remain to be extracted from the later state handlers.

## Attack input

The same general routine reaches `$BBCA+`, which tests `$3D & $40` (B).

This is the platform attack button. The branch checks busy/action flags and character-specific conditions before transitioning to an attack action family and dispatching character-specific routines.

This aligns with the documented control `B = punch/attack`.

## Vertical physics/collision

`$B87D+` is one vertical-motion path:

- changes `$40` by a fixed amount (`+3` in this phase);
- inspects collision/tile samples around `$52/$55`;
- can nudge `$3F` horizontally when resolving edge interactions;
- snaps `$40` to collision-aligned values under certain tile classes;
- resets action/airborne flags on landing-like conditions;
- handles bottom/fall hazards when vertical thresholds are exceeded.

There are complementary vertical paths later in bank 3 (`$BDxx-$BExx`) that vary movement amount based on `$0387-$0389` and test `$3D` during airborne states.

We should not reduce this to a single `y += velocity` formula yet: the original engine mixes tile collision, scroll-relative positioning and action-state-specific movement.

## Movement parameter candidates

`$0387`, `$0388`, `$0389` appear repeatedly as movement increments selected according to action/input state.

- `$0387` is used by ordinary horizontal displacement and as a default airborne movement amount;
- `$0388/$0389` are selected in alternate directional/airborne branches.

Strong candidates for per-stage/per-character speed deltas, but exact units must be mapped.

## Collision samples

The movement code repeatedly examines `$4F-$56`, with tile/class values compared to ranges such as `$78`, `$80`, `$88`, `$90`, `$E0`, `$F0`, `$F8/$F9`.

These are clearly collision/environment samples or descriptors around the player, but exact spatial mapping (left foot/right foot/head/etc.) is not yet proven.

Do not assign anatomical names until their sample positions are reconstructed.

## Important consequence for REBORN

The platform engine is not a single simplistic velocity loop. It has:

- explicit facing;
- camera/player horizontal handoff;
- directional jump states;
- high jump modifier;
- crouch/drop behavior;
- character/state-dependent movement increments;
- multiple collision samples around the player;
- state-family-driven animation/movement.

That means REBORN can preserve the original control semantics while replacing the coarse NES implementation with continuous/high-resolution physics and richer animation.

## Next extraction targets

1. identify the exact player coordinate origin and camera handoff thresholds;
2. assign spatial meaning to `$4F-$56` collision samples;
3. map `$0387-$0389` to speed/jump parameters;
4. enumerate `$4D/$4E` action-state families completely;
5. trace B attack through character-specific attack routines;
6. isolate hitboxes/hurtboxes and ordinary enemy collision;
7. reconstruct jump arc numerically for parity tests.
