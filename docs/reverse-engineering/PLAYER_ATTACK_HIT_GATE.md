# Bronze Saint attack hit gate — `$06BC`

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: confirmed functional role. The phase source that feeds the gate is deterministic but not yet reduced to a probability model.

## What `$06BC` means

`$06BC` is the **player attack hit token**.

Bank 1 `$AD52+` begins by reading `$06BC`:

- zero -> return without calculating or applying any opponent damage;
- non-zero -> call `$BC64`, calculate Life/Cosmo drain and consume the resulting counters.

Therefore `$06BC` is not the amount of damage. It is the gate that decides whether the selected Bronze Saint technique actually connects.

## Where it is generated

Fixed `$FAAA+` runs immediately before the player attack animation/damage path.

The routine first checks `$0690`.

### `$0690 != 0`

The hit token is forced to zero.

`$0690` is written by multiple stage/event scripts and acts as a story/script-level **block on player attack connection**. Several boss-event handlers set it to `$FF`; others clear it when the battle becomes hittable again.

### `$0690 == 0`

The routine reads `$065F`, masks to its low nibble and compares it to a threshold.

Normal phases:

`($065F & $0F) >= 5`

Special phase `$06CE == 2`:

`($065F & $0F) >= 6`

If the comparison fails, `$06BC = 0`.

If it succeeds, the nibble itself (`5..15` or `6..15`) is stored into `$06BC`. Observed consumers use its zero/non-zero state.

## Special no-damage context

Bank 1 `$ABF4+` explicitly clears `$06BC` when `$050E == $0C`, bypassing the normal hit calculation for that special/final context.

## Visible semantics

The hit token explains the documented battle behavior where a Bronze Saint technique can animate while the Gold Saint remains in a defensive pose and receives no damage. The numeric damage formula is only entered when the token is non-zero.

## `$065F/$0660` phase source

`$0660` is incremented continuously by fixed `$E0AC+`. No ordinary store/reset has been found beyond initial RAM clearing; it naturally wraps as an 8-bit index.

The same routine loads a byte from:

`$94F0 + $0660`

and adds it to `$065F`.

Important architectural detail: `$94F0` belongs to the currently mapped switchable PRG bank, so different battle/engine subsystems expose different source tables at the same CPU address. Thus `$065F` is best described currently as a **bank-dependent phase/variation accumulator**.

Its low bits are reused for:

- ordinary Gold-Saint technique slot selection;
- player attack hit gating;
- dodge danger-side parity (via the related `$0660` parity);
- several graphics/event selections.

This looks deliberately like a lightweight source of temporal variation, but we do **not** yet claim uniform randomness or a fixed percentage hit chance. Static code proves the threshold, not the distribution of accumulator values at player input times.

## Executable specification

`spec/original/player_attack_hit.py` implements:

- normal low-nibble threshold 5;
- phase-2 threshold 6;
- `$0690` scripted block;
- special forced-zero context;
- preservation of the successful nibble as the original hit token.

Tests live in `tests/test_player_attack_hit_spec.py`.

## Remaining work

1. recover the active `$94F0` phase tables for each battle state/bank;
2. determine the resulting long-period sequence of `$065F/$0660` under stable conditions;
3. test whether observed hit frequencies are uniform or state/timing-biased;
4. map every `$0690` write to a named story/boss event;
5. incorporate the hit gate into the complete executable battle-turn model.
