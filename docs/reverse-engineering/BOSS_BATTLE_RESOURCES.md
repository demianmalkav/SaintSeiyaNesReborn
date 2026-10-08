# Gold Saint battle resources — static reconstruction

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: resource representation, active/persistent transfer and decimal arithmetic are confirmed by code. Full attack/defense formula, initiative/parry and boss-specific scripted victory conditions remain under reconstruction.

## Persistent record -> active battle state

Bank 1 `$AB10+` switches the currently active Saint in the battle/resource-allocation subsystem.

One persistent Saint record has five bytes:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, cap byte]`

When loaded:

- record byte 0 -> `$05CE` (active Life low BCD)
- record byte 1 -> `$05CF` (active Life hundreds)
- record byte 2 -> `$05BC` (active Cosmo low BCD)
- record byte 3 -> `$05BD` (active Cosmo hundreds)
- cap high nibble -> `$05D0` (Life exclusive hundreds boundary)
- cap low nibble -> `$05BE` (Cosmo exclusive hundreds boundary)

The same routine writes the previous active values back to its persistent record before loading the new Saint.

This proves that `$05BC-$05D0` are not unrelated cheat-only battle variables; they are a working mirror of the selected Saint's durable resource state.

## Cap semantics

The boundary values are exclusive hundreds limits:

- boundary `1` -> maximum 99
- boundary `2` -> maximum 199
- boundary `5` -> maximum 499

General rule:

`maximum = boundary * 100 - 1`

The fixed-bank increment routines first preserve the old resource, perform decimal carry, then compare the resulting hundreds digit against the boundary. If the new hundreds digit reaches the boundary, the old value is restored.

## Fixed-bank decimal routines

### Life

Active Life is `$05CE/$05CF` with boundary `$05D0`.

- `$FBCF+`: increment Life by one packed-decimal unit, refusing overflow past the boundary;
- `$FC2E+`: decrement Life by one unit, refusing to go below zero;
- `$FC81+`: refreshes Life display/state after a mutation.

### Cosmo

Active Cosmo is `$05BC/$05BD` with boundary `$05BE`.

- `$FCC7+`: increment Cosmo by one packed-decimal unit, refusing overflow past the boundary;
- `$FD26+`: decrement Cosmo by one unit, refusing to go below zero;
- `$FD79+`: refreshes Cosmo display/state after a mutation.

### Seventh Sense

`$05AA/$05AB` is the four-digit packed-decimal Seventh Sense value.

- `$FDE0+`: increment by one, saturating at 9999;
- `$FE26+`: decrement by one, saturating at zero;
- `$FE8B+`: refreshes the displayed digits and related state.

## UI representation

The fixed routines convert decimal nibbles into tile codes and maintain display buffers adjacent to the resource state.

For example, `$FDBA+` expands active Cosmo digits into `$05C0+`, while the analogous Life path writes around `$05D2+`. These are presentation mirrors, not the canonical numeric values.

## Battle threshold logic in PRG bank 5

Bank 5 contains stage/boss-specific threshold checks.

At `$AD55+`, the engine reduces active Cosmo to a two-nibble magnitude made from the hundreds and tens digits, then compares it against a table indexed by `$050E` (stage/house context). The result is converted to a three-way state in `$EA`:

- `$FF`
- `$00`
- `$01`

A structurally parallel routine at `$ACD6+` compares another pair of battle-side values against another stage table and writes `$EB`.

This is strong evidence that battle resolution/AI uses stage-specific resource thresholds rather than one universal formula. Exact semantic labels for `$EA/$EB` and the compared opponent fields remain open.

## Clean-room executable spec

`spec/original/battle_resources.py` models:

- packed BCD conversion;
- active Life/Cosmo record loading;
- exclusive hundreds caps;
- one-unit increment/decrement edge behavior;
- Seventh Sense saturation.

`tests/test_battle_resources_spec.py` locks these behaviors down as parity tests.

## Next targets

1. trace callers of the `$EA/$EB` threshold routines and assign their exact meaning;
2. identify attack/defense allocation values and where they consume active Cosmo;
3. isolate the damage formula for player and Gold Saint;
4. locate parry/initiative RNG and timing window;
5. map scripted dialogue/victory conditions per house;
6. build an executable battle-turn model analogous to the platform movement spec.
