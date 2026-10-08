# Platform exit gates and mode transition

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the coordinate predicates and immediate transition effects are statically confirmed. The high-level narrative destination after each `$3D` reload is stage-dependent and remains a separate concern.

## Discovery

PRG bank 1 `$969D-$9713` runs during active platform processing. It is the common gate that decides whether the current platform area has been completed.

The routine first requires:

- `$49 == 0` — no active jump phase;
- a substate-specific horizontal threshold;
- an exact player Y coordinate.

For substate `$10`, internal Saint index 1 (**Shun**) is explicitly rejected before the coordinate test.

## Main-sequence gates `$00-$0B`

All twelve principal approach stages use the same screen-local exit gate:

- `player_x >= $D0`
- `player_y == $40`
- `jump_phase == 0`

This is intentionally screen-local rather than an absolute world-X comparison. The camera is already clamped near the final page by the scroll-cap table at bank 3 `$AC51`, so the combination of final camera position plus `player_x >= $D0` forms the effective world endpoint.

## Special-substate gates

For `$02 >= $0C`, the routine indexes six coordinate pairs at bank 1 `$9714`:

| substate | min player X | required player Y | transition |
|---:|---:|---:|---|
| `$0C` | `$88` | `$20` | normal `$3D` reload |
| `$0D` | `$B4` | `$30` | normal `$3D` reload |
| `$0E` | `$B4` | `$80` | normal `$3D` reload |
| `$0F` | `$B4` | `$40` | normal `$3D` reload |
| `$10` | `$B4` | `$70` | normal `$3D` reload; Shun blocked |
| `$11` | `$D0` | `$50` | special `$70` transition |

The comparison is `player_x >= threshold`, not equality. Y must match exactly.

## Normal exit side effects

For every accepted gate except `$11`, `$96CB+` performs the following sequence:

1. clears `$04`;
2. calls bank 1 `$951F`, snapshotting all five active Saint records into persistent state;
3. sets `$00 = $3D` and `$01 = $3D`;
4. disables PPU rendering/NMI through `$2000/$2001`;
5. triggers engine transition/sound helper `$C00A` with `$37`;
6. resets stack to `$01FF`;
7. jumps to `$E100`.

Thus reaching the platform gate is a full engine-mode boundary, not merely a local `stage_complete=true` flag.

The snapshot before leaving is significant: platform Life/Cosmo/caps are committed before the next mode begins.

## Substate `$11`

When the `$11` gate is accepted, the routine does **not** use the `$3D` reload path. It instead:

- sets `$00 = $70`;
- clears `$26/$27`;
- sets `$57 = $C0`;
- invokes fixed helpers `$C00D` and `$C00A`;
- returns into the special `$70` engine state sequence.

This is a mechanically distinct final/special transition and should stay distinct in REBORN even if presentation is completely redesigned.

## Shun exception in `$10`

At `$96A1-$96AE`:

- if substate is `$10`;
- and internal platform Saint index is `1`;
- the routine returns immediately.

Internal index 1 is independently confirmed as Shun. Therefore Shun cannot activate the normal `$10` platform exit gate through this path.

Do not generalize this into a narrative reason in ORIGINAL SPEC; the ROM-level fact is simply a character-specific exit restriction.

## Relationship with stage width

Bank 3 `$AC51` stores the maximum scroll-high/page value per substate. For every reconstructed stage it equals `page_count - 2`, matching the two-page streaming window.

When that cap is reached, horizontal movement stops advancing the camera and lets the player move farther right on-screen, up to the final screen-space region. The exit-gate routine then tests the coordinates above.

So the original endpoint is a composition of:

`stage page count -> camera clamp -> player local X/Y gate -> mode transition`

not a single absolute world-coordinate constant.

## Clean-room model

`PlatformExitGate` in `SaintSeiyaNesReborn.OriginalSpec` implements:

- all `$00-$11` coordinate gates;
- `jump_phase == 0` requirement;
- the `$10` Shun exclusion;
- distinction between normal `$3D` reload and special `$70` transition.

This gives a future native stage loop a deterministic completion predicate without emulating the NES state-machine plumbing.
