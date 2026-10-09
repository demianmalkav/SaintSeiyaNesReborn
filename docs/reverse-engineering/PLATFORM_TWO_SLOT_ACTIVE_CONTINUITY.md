# Two common slots — active-family continuity across frames

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room two-slot frame composition now dispatches closed common families `$10/$30/$40/$50` independently for each entity.

## Why this matters

The prior two-slot slice correctly serialized shared attack/contact state but required both entities to begin every frame in ordinary family `$10`. That was sufficient for ordering tests, but not for continuous simulation: a surviving hit stores `$40`, a jump persists as `$31/$32`, and a nearby/falling transition can persist as `$50`.

The slice now routes each slot through `PlatformCommonEntityActiveDispatcher` before deciding whether to run `$9915/$98BA`.

## Multi-frame hit reaction

A new two-frame fixture uses Hyoga:

Frame N:

- slot A begins ordinary `$10`;
- Hyoga projectile hits and survives the HP subtraction;
- slot A stores action `$40` and recoil byte `$48`;
- Hyoga projectile is retired before slot B.

Frame N+1:

- slot A enters the `$40` dispatcher route;
- action advances `$40 -> $41`;
- recoil byte advances `$48 -> $47`;
- X moves +4;
- `$9915/$98BA` are skipped for A on that update;
- HP remains at the reduced value from frame N.

This proves the composed engine can now carry a post-hit entity state across frames without forcing it back through ordinary AI.

## Same-frame landing does not re-enter ordinary interaction

Another fixture begins slot A in `$50` at a landing-compatible Y/descriptor.

The `$50` route:

- applies fixed fall;
- lands through `$C491`;
- stores action `$10` and snapped Y;
- still reports `SkipInteraction` for the current update.

An overlapping player projectile therefore does not hit the entity until a later update that actually enters through the ordinary/jump interaction-capable path.

## Shared two-slot ordering remains intact

The change does not alter frame-global serialization:

- slot A still runs before B;
- attack state, `$76/$7F/$80`, and Seventh Sense still thread A -> B;
- `$A22C` still runs once after both slots;
- `$C402` still increments `$3C` once.

## Current active scope

Supported common type `$00-$07` families:

- `$10` ordinary mobile;
- `$30` active jump;
- `$40` hit reaction;
- `$50` fall.

Other families remain explicit unsupported work rather than being approximated.

No ROM payload is embedded in the model.
