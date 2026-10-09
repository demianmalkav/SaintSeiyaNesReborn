# Platform jump profiles — frame-exact vertical curves

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the vertical displacement tables selected by the platform jump code are reconstructed exactly and represented in ORIGINAL SPEC with a semantic positive-up convention.

## Why the original is not a gravity equation

The platform engine does not integrate a velocity/gravity pair. Each jump selects a signed displacement table and consumes one entry per update. Positive table entries move the Saint upward on screen; negative entries move downward. This produces deliberately hand-authored curves, including small asymmetries and plateaus near some apices.

REBORN should therefore preserve the exact curves in compatibility mode rather than approximate them with a generic parabola.

## Critical phase-counter detail

At bank 3 `$BCD3+`, `$49` is the jump-phase counter. The routine:

1. chooses a phase limit (`$32`);
2. increments `$49`;
3. while the incremented phase is below the limit, indexes the displacement table with `phase - 2`;
4. when the phase reaches the limit, stops reading the table and applies a fixed downward `+3 px` screen-Y step.

Therefore a ROM phase limit of `N` consumes exactly **`N - 2` table entries**. The last two physical bytes present in each authored table are never reached by this path and must not be counted as executed jump frames.

The older shorthand “duration” referred to the phase limit. ORIGINAL SPEC now exposes both concepts explicitly:

- `PhaseLimit`
- `TableFrames = PhaseLimit - 2`

## Jump families

The original exposes three distinct families:

1. standing/ordinary vertical jump;
2. `Up + A` high jump, with Saint-specific profile selection;
3. directional jump, also with Saint-specific profile selection.

The action family remains `$30-$33`; low bits encode directional state at jump start. The curve selection itself is separate from the horizontal/camera step.

## Standing jump

All Saints share one profile.

- phase limit: 32;
- consumed table frames: 30;
- maximum rise: 58 px;
- first maximum: table frame 14;
- net rise after consumed table: 35 px;
- after that, the routine uses the generic 3 px/frame downward path until landing/collision resolution.

## High jump (`Up + A`)

Internal platform indices:

| Saint | phase limit | table frames | max rise | first apex frame | table sharing |
|---|---:|---:|---:|---:|---|
| Seiya | 60 | 58 | 103 px | 28 | unique |
| Shun | 50 | 48 | 88 px | 23 | shared with Ikki |
| Hyoga | 40 | 38 | 71 px | 18 | shared with Shiryu |
| Shiryu | 40 | 38 | 71 px | 18 | shared with Hyoga |
| Ikki | 50 | 48 | 88 px | 23 | shared with Shun |

Net rise after the consumed table is 51 px for Seiya/Shun/Ikki and 41 px for Hyoga/Shiryu; subsequent frames use the fixed downward path.

## Directional jump

| Saint | phase limit | table frames | max rise | first apex frame | net after table | table sharing |
|---|---:|---:|---:|---:|---:|---|
| Seiya | 54 | 52 | 39 px | 25 | -8 px | shared with Ikki |
| Shun | 40 | 38 | 33 px | 18 | +5 px | unique |
| Hyoga | 44 | 42 | 34 px | 20 | 0 px | shared with Shiryu |
| Shiryu | 44 | 42 | 34 px | 20 | 0 px | shared with Hyoga |
| Ikki | 54 | 52 | 39 px | 25 | -8 px | shared with Seiya |

The Seiya/Ikki and Hyoga/Shiryu curves contain a one-frame positive bump around the near-apex plateau. This is preserved exactly rather than smoothed out.

## ORIGINAL SPEC representation

`PlatformJumpProfile` stores only the table entries that the original code can actually execute. It exposes:

- `RisePerFrame`;
- `TableFrames`;
- `PhaseLimit`;
- peak cumulative rise;
- first apex frame;
- net rise after the consumed table;
- fixed terminal-fall amount (`3 px/frame`);
- screen-Y and cumulative-rise queries that continue correctly into the terminal fall path.

The code does not contain ROM offsets or depend on the ROM at runtime. The ROM-derived extractor remains a reverse-engineering tool; the native compatibility model consumes only the reconstructed behavior.

## Evidence boundary

`CONFIRMED`:

- all per-frame displacement values that `$BCD3+` can actually consume;
- `phase-2` indexing and the `PhaseLimit - 2` frame count;
- table sharing among Saints;
- maximum rise and first apex frame;
- fixed `+3 px/frame` terminal fall after the phase limit;
- semantic sign convention (positive source value = upward movement);
- table-driven design rather than velocity/gravity integration.

Still to compose into the full executable platform session:

- exact jump-start frame ordering and A-latch mutation;
- directional airborne horizontal movement/camera handoff;
- ceiling interruption (`$E0-$EF` head probe);
- transition into landing logic;
- landing snap and action-state reset;
- crouch/drop-through path.

These are kept separate so the executable session only claims parity for behavior whose ordering has been reconstructed to the same standard.
