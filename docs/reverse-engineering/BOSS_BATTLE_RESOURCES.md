# Gold Saint battle resources — static reconstruction

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: resource representation, active/persistent transfer, opponent resource pairs, decimal arithmetic and the coarse battle-condition classifier are confirmed by code. Full attack/defense formula, initiative/parry and boss-specific scripted victory conditions remain under reconstruction.

## Persistent record -> active player battle state

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

## Opponent battle resources

Bank 4 `$8020+` initializes a stage-specific opponent block from two 16-byte-per-stage tables.

The resource pairs are:

- `$05E0/$05E1` — opponent Cosmo, low packed-BCD byte / hundreds digit;
- `$05F1/$05F2` — opponent Life, low packed-BCD byte / hundreds digit.

The Life identity is proven by the bank-5 classifier: `$05F1|$05F2 == 0` yields the defeated state `$EB = $FF`. `$05E0/$05E1` is then the structurally parallel Cosmo pair.

For stage indices `0..10`, the initialized opponent resources are:

| Stage index | Life | Cosmo |
|---:|---:|---:|
| 0 | 119 | 119 |
| 1 | 199 | 299 |
| 2 | 299 | 299 |
| 3 | 299 | 399 |
| 4 | 399 | 399 |
| 5 | 599 | 599 |
| 6 | 499 | 499 |
| 7 | 799 | 799 |
| 8 | 599 | 599 |
| 9 | 699 | 699 |
| 10 | 999 | 999 |

Exact Gold-Saint/house names for these numeric stage indices should be attached only after the `$050E` stage map is internally cross-referenced to event/text/graphics data.

## Cap semantics

The player's boundary values are exclusive hundreds limits:

- boundary `1` -> maximum 99
- boundary `2` -> maximum 199
- boundary `5` -> maximum 499

General rule:

`maximum = boundary * 100 - 1`

The fixed-bank increment routines first preserve the old resource, perform decimal carry, then compare the resulting hundreds digit against the boundary. If the new hundreds digit reaches the boundary, the old value is restored.

## Fixed-bank decimal routines

### Player Life

Active Life is `$05CE/$05CF` with boundary `$05D0`.

- `$FBCF+`: increment Life by one packed-decimal unit, refusing overflow past the boundary;
- `$FC2E+`: decrement Life by one unit, refusing to go below zero;
- `$FC81+`: refreshes Life display/state after a mutation.

### Player Cosmo

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

## Three-state battle-condition classifier

Bank 5 contains two structurally parallel classifiers:

- `$AD55+` -> player condition `$EA`;
- `$ACD6+` -> opponent condition `$EB`.

Both reduce Life and Cosmo to their **hundreds+tens** BCD magnitude, ignoring the ones digit for this comparison.

For example:

- resource `299` -> comparison magnitude `$29`;
- resource `290` -> also `$29`;
- resource `99` -> `$09`.

The threshold comparison is strict: the magnitude must be **greater than**, not equal to, the stage threshold.

### State meaning

Both `$EA` and `$EB` use the same three values:

- `$FF` — Life is zero: combatant is defeated;
- `$00` — Life **and** Cosmo are both strictly above the relevant stage threshold;
- `$01` — combatant is alive, but Life and/or Cosmo do not clear the threshold.

This conclusion follows directly from both classifier control flows and their many downstream branches. The values are therefore condition/readiness states, not an initiative flag.

### Player threshold table — `$ADB9`

For stage indices `0..10`:

`00, 05, 05, 08, 08, 08, 10, 10, 10, 10, 10`

### Opponent threshold table — `$AD42`

For stage indices `0..10`:

`00, 05, 05, 08, 20, 20, 00, 30, 00, 20, 20`

These are packed hundreds+tens magnitudes: e.g. `$20` corresponds to a 200 threshold for the coarse comparison.

The asymmetric tables are important: the same raw Life/Cosmo values can place the player and opponent in different condition states during the same stage.

## Clean-room executable spec

`spec/original/battle_resources.py` models:

- packed BCD conversion;
- active Life/Cosmo record loading;
- exclusive hundreds caps;
- one-unit increment/decrement edge behavior;
- Seventh Sense saturation.

`spec/original/battle_condition.py` models:

- the hundreds+tens comparison magnitude;
- both 11-entry stage threshold tables;
- the `$FF/$00/$01` condition classifier.

Parity tests live in:

- `tests/test_battle_resources_spec.py`
- `tests/test_battle_condition_spec.py`

## Next targets

1. map `$050E` stage indices to exact houses/Gold Saints using an internal ROM anchor;
2. identify attack/defense allocation values and where they consume active Cosmo;
3. isolate the damage formula for player and Gold Saint;
4. locate parry/initiative RNG and timing window;
5. map scripted dialogue/victory conditions per house;
6. build an executable battle-turn model analogous to the platform movement spec.
