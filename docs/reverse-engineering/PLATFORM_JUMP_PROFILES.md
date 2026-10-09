# Platform jump profiles — frame-exact vertical curves

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the vertical displacement tables selected by the platform jump code are reconstructed exactly and represented in ORIGINAL SPEC with a semantic positive-up convention.

## Why the original is not a gravity equation

The platform engine does not integrate a velocity/gravity pair. Each jump selects a signed displacement table and consumes one entry per update. Positive table entries move the Saint upward on screen; negative entries move downward. This produces deliberately hand-authored curves, including small asymmetries and plateaus near some apices.

REBORN should therefore preserve the exact curves in compatibility mode rather than approximate them with a generic parabola.

## Jump families

The original exposes three distinct families:

1. standing/ordinary vertical jump;
2. `Up + A` high jump, with Saint-specific profile selection;
3. directional jump, also with Saint-specific profile selection.

The action family remains `$30-$33`; low bits encode directional state at jump start. The curve selection itself is separate from the horizontal/camera step.

## Standing jump

All Saints share one 32-frame table.

- duration: 32 updates;
- maximum rise: 58 px;
- first maximum: frame 14;
- net vertical position after the 32 table entries: still 29 px above takeoff;
- the later generic fall/landing path continues from there.

## High jump (`Up + A`)

Internal platform indices:

| Saint | duration | max rise | first apex frame | table sharing |
|---|---:|---:|---:|---|
| Seiya | 60 | 103 px | 28 | unique |
| Shun | 50 | 88 px | 23 | shared with Ikki |
| Hyoga | 40 | 71 px | 18 | shared with Shiryu |
| Shiryu | 40 | 71 px | 18 | shared with Hyoga |
| Ikki | 50 | 88 px | 23 | shared with Shun |

This confirms that character locomotion differences are encoded directly in authored motion data, not only in horizontal speed increments.

## Directional jump

| Saint | duration | max rise | first apex frame | table sharing |
|---|---:|---:|---:|---|
| Seiya | 54 | 39 px | 25 | shared with Ikki |
| Shun | 40 | 33 px | 18 | unique |
| Hyoga | 44 | 34 px | 20 | shared with Shiryu |
| Shiryu | 44 | 34 px | 20 | shared with Hyoga |
| Ikki | 54 | 39 px | 25 | shared with Seiya |

The Seiya/Ikki and Hyoga/Shiryu curves contain a one-frame positive bump around the near-apex plateau. This is preserved exactly in the clean-room profile rather than smoothed out.

## ORIGINAL SPEC representation

`PlatformJumpProfile` stores the reconstructed motion in run-length encoded semantic form. It exposes:

- `RisePerFrame`;
- duration;
- peak cumulative rise;
- first apex frame;
- net rise after the table;
- conversion to screen-Y delta for a given frame.

The code does not contain ROM offsets or depend on the ROM at runtime. The ROM-derived extractor remains a reverse-engineering tool; the native compatibility model consumes only the reconstructed behavior.

## Evidence boundary

`CONFIRMED`:

- all per-frame displacement values for the seven distinct physical tables;
- table sharing among Saints;
- duration, peak rise and first apex frame;
- semantic sign convention (positive source value = upward movement);
- table-driven design rather than velocity/gravity integration.

Still to compose into the full executable platform session:

- exact jump-start frame ordering and A-latch mutation;
- directional airborne horizontal movement/camera handoff;
- ceiling interruption (`$E0-$EF` head probe);
- transition from table-controlled motion into the generic fall family;
- landing snap and action-state reset;
- crouch/drop-through path.

These are kept separate so the executable session only claims parity for behavior whose ordering has been reconstructed to the same standard.
