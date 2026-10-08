# Platform camera handoff and horizontal limits

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: ordinary grounded horizontal handoff is statically reconstructed from PRG bank 3 `$AB3F-$AC50`. Directional-jump camera behavior is separately implemented at `$BEAD-$BF48` and follows the same central screen thresholds with jump-specific increments/collision gates.

## Screen-local player coordinate

`$3F` is the player's horizontal coordinate in the active screen window. `$44/$45` form the horizontal camera/scroll position used to derive world X.

The engine intentionally keeps the player near a central tracking band while the camera advances.

## Grounded movement to the right

After collision acceptance:

1. if the camera has not reached its substate limit, movement begins by updating the action family;
2. while `player_x < $80`, the player itself advances by `$0387`;
3. once `player_x >= $80`, the same increment is applied to scroll `$44/$45` instead;
4. at the final scroll page/limit, the camera stops and the player may advance again toward the right edge;
5. the right-edge hard player threshold is `$E0` in the terminal camera state.

This is the original side-scroller handoff:

`left screen region -> move player -> center tracking band -> move camera -> terminal region -> move player toward exit gate`.

## Grounded movement to the left

Left movement does not rewind the camera in the ordinary grounded path at `$ABE5-$AC37`.

After collision acceptance:

- if `player_x >= $10`, subtract `$0387` from player X;
- if the player is already left of that threshold, the action state returns toward neutral rather than moving farther left.

The camera-rewind behavior used by directional airborne movement is implemented separately in the jump path and should not be assumed for the grounded routine.

## Camera low-byte boundary

Rightward scrolling checks `$44` against `$F8` before deciding whether the current high-page value `$45` has reached the substate-specific limit.

When a scroll increment overflows `$44`, `$45` is incremented.

At the terminal boundary, one branch resets `$44` to zero before allowing terminal-screen player motion. The exact relationship is intentionally reproduced as state/limit logic rather than flattened into a single unconstrained world coordinate in ORIGINAL SPEC.

## Per-substate terminal page table

Table `$AC51` contains 18 bytes indexed by platform substate `$02`:

| substate | terminal `$45` value |
|---:|---:|
| `$00` | 4 |
| `$01` | 5 |
| `$02` | 6 |
| `$03` | 7 |
| `$04` | 8 |
| `$05` | 9 |
| `$06` | 10 |
| `$07` | 11 |
| `$08` | 12 |
| `$09` | 13 |
| `$0A` | 14 |
| `$0B` | 14 |
| `$0C` | 10 |
| `$0D` | 10 |
| `$0E` | 10 |
| `$0F` | 1 |
| `$10` | 10 |
| `$11` | 0 |

For the normal main sequence this tracks the increasing level lengths already reconstructed from page lists. `$0B` remains 14 because its page-list/camera termination semantics do not map 1:1 to raw page count.

## Grounded collision special-case correction

The same routine proves an important parity detail:

- lower side probes are consulted for ordinary grounded movement in every substate;
- upper side probes are additionally consulted **only when `$02` is `$0C`, `$0D`, or `$0E`**;
- directional airborne movement uses its upper-side probes independently of that grounded special-map gate.

The executable C# and Python stage models encode this rule explicitly.

## REBORN consequence

The native compatibility runtime should preserve three distinct coordinates/concepts:

- screen-local player X;
- horizontal camera/scroll state;
- derived world X.

Modern camera smoothing can later be introduced in REBORN presentation mode, but ORIGINAL SPEC compatibility must not collapse the original handoff into a generic `world_position += velocity`, because encounter-page activation, collision streaming and exit gates all depend on the original distinction.
