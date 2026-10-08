# Platform camera, horizontal handoff and screen bounds

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: grounded horizontal movement/camera handoff and per-substate camera caps are statically reconstructed from PRG bank 3 `$AB3F-$AC50` plus movement increment setup in bank 1 `$9211-$922F`.

## Core design

The original does **not** store the player only in world coordinates. Horizontal progression is split into:

- `$3F` — player X in screen-local coordinates;
- `$44` — scroll X low byte;
- `$45` — scroll X high/page byte.

Effective horizontal world position is therefore:

`world_x = ($45 << 8) + $44 + $3F`

This is the same world-coordinate composition consumed by the collision sampler.

## Grounded Right

PRG bank 3 `$AB3F+` tests Right before Left, so **Right wins if both directions are held**.

On Right:

1. `$42` is set to `$40` (facing right);
2. collision probes are checked;
3. movement state enters the `$1x` locomotion family;
4. movement is split between player-local X and camera scroll.

### Before center handoff

If `player_x < $80`:

`player_x += $0387`

The camera does not move.

### Camera handoff

Once `player_x >= $80`, ordinary forward movement changes scroll instead:

`scroll_low += $0387`

with carry incrementing `$45`.

The player therefore remains roughly centered while the world streams past.

### Final-screen handback

When `$44 >= $F8` and `$45` has reached the substate's camera-high cap, camera advance stops. Right movement returns to screen-local player X:

`player_x += $0387`

until the pre-move boundary check sees:

`player_x >= $E0`

At that point no further grounded Right displacement occurs through this path.

The pre-addition nature of the comparison means a multi-pixel step may cross `$E0` on the final successful update; the next frame is the one that blocks.

This final handback is what makes the coordinate-based exit gate (`player_x >= $D0`) reachable after the camera itself stops.

## Grounded Left

Left sets `$42=0` and applies the asymmetric left collision rules.

If `player_x >= $10`:

`player_x -= $0387`

Crucially, **grounded Left never decrements `$44/$45`**. The camera is forward-only in this ordinary path. After the level has scrolled, the player can retreat only within the current visible screen region; earlier level pages are not recovered by walking left.

If `player_x < $10`, no further leftward displacement occurs.

As with the right edge, a multi-pixel step can cross below `$10` because the threshold is checked before subtraction.

## Collision correction: upper side probes are not universal

A previous clean-room model applied grounded upper-side probes everywhere. Bank 3 proves that this was too broad.

For ordinary grounded movement:

- lower-right `$50` always controls the normal Right side test;
- lower-left `$53` always controls the normal Left side test;
- upper-right `$51` and upper-left `$54` are additionally consulted **only when platform substate is `$0C-$0E`**.

Airborne movement has its own universal upper/lower probe logic and is not subject to this `$0C-$0E` restriction.

This distinction is now encoded in `PlatformStageMap.CanMoveRight/CanMoveLeft` through an explicit substate parameter.

## Per-substate camera-high cap

Table at PRG bank 3 `$AC51`:

| substate | `$45` cap |
|---:|---:|
| `$00` | `$04` |
| `$01` | `$05` |
| `$02` | `$06` |
| `$03` | `$07` |
| `$04` | `$08` |
| `$05` | `$09` |
| `$06` | `$0A` |
| `$07` | `$0B` |
| `$08` | `$0C` |
| `$09` | `$0D` |
| `$0A` | `$0E` |
| `$0B` | `$0E` |
| `$0C` | `$0A` |
| `$0D` | `$0A` |
| `$0E` | `$0A` |
| `$0F` | `$01` |
| `$10` | `$0A` |
| `$11` | `$00` |

For every reconstructed stage this equals:

`page_count - 2`

which matches the engine's two-page streaming/collision window.

The low scroll byte is not hard-clamped exactly to `$F8`; `$F8` is the threshold tested on the following update. A 2-pixel movement increment may therefore leave `$44` at `$F9` before the final-camera branch engages.

## Movement increments `$0387-$0389`

Fixed `$C411-$C418` computes:

`$30 = ($3C & 1) + 1`

so `$30` alternates between 1 and 2 with frame parity.

Bank 1 `$9211+` then sets:

### Every internal Saint index except 1

- `$0387 = 1`;
- `$0388 = $30` -> alternates 1/2;
- `$0389 = $3C & 1` -> alternates 0/1.

### Internal index 1 = Shun

- `$0387 = $30` -> alternates 1/2;
- `$0388 = 2`;
- `$0389 = 1`.

Thus Shun has a distinct grounded speed cadence in addition to his separate attack behavior.

`$0388/$0389` are selected by directional-jump/air-control branches. Their exact semantic names remain intentionally raw in ORIGINAL SPEC until the whole airborne controller is promoted as one unit.

## Relationship with stage completion

The final horizontal loop is now mechanically complete:

1. page count determines camera cap;
2. player moves locally to `$80`;
3. camera advances while the player remains near center;
4. final camera cap freezes scroll around low-byte threshold `$F8`;
5. player resumes local Right movement;
6. the substate-specific `PlatformExitGate` becomes reachable;
7. the platform mode exits into the story router/reload state.

This is the core horizontal progression architecture to reproduce in REBORN even if the final game uses higher resolution and modern rendering.

## Clean-room implementation

`PlatformHorizontalMotion` implements:

- original camera-high cap table;
- grounded Right/Left priority;
- player-to-camera handoff at `$80`;
- final-screen camera-to-player handback;
- right `$E0` and left `$10` local boundaries;
- forward-only grounded camera behavior;
- per-frame `$0387-$0389` movement increments.

Executable C# self-tests cover the handoff, scroll carry, final screen, no-rewind Left behavior, direction priority, stage-specific upper probes and Shun's distinct movement cadence.
