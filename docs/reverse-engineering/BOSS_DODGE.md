# Gold Saint counterattack dodge — exact static reconstruction

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the input window, direction encoding, displacement checks, success/failure counters and complete-damage bypass are confirmed by code.

## Separation from attack weakening

This subsystem is independent from `$0681`.

- `$0681` weakens a Gold Saint's raw attack to 1/2 or 1/4 through story scripts.
- the dodge subsystem decides whether the already-selected Gold Saint attack reaches the Bronze Saint at all.

A successful dodge skips the player-damage routine completely.

## Window

Fixed `$F936+` prepares a Gold Saint counterattack and calls PRG bank 6 `$9183`.

`$9183+` runs an input loop with a counter at `$064C` and accepts at most `$1C` (28) polling iterations.

During the window:

- controller right -> `$9219`, all relevant OAM X coordinates move +2 pixels;
- controller left -> `$91F8`, all relevant OAM X coordinates move -2 pixels.

The same helpers update a two-byte displacement accumulator `$AF/$B0`.

## Direction values

`$0661` stores the player's last dodge direction:

- `$00` = none / invalid movement
- `$01` = left
- `$FF` = right

Right input sets `$0661 = $FF`; left input sets `$0661 = $01`.

## Danger direction

At fixed `$F995+`, the game takes `$0660 & 1` and stores the attack's blocked/danger direction in `$0662`:

- even parity -> `$FF` (right)
- odd parity -> `$01` (left)

A valid dodge only succeeds if `$0661` is **different** from `$0662`.

## Displacement validation

After the input loop, bank 6 `$91C0+` validates the net 8-bit displacement according to the **last direction**.

### Last direction left (`$01`)

The movement remains valid only when:

`$AF >= $10`

From a centered offset of zero and +2 per left tick, this requires at least eight net left ticks.

### Last direction right (`$FF`)

The movement remains valid only when:

`$AF < $F0`

When moving only right from zero, `$AF` wraps downward by 2 each tick:

- 8 ticks -> `$F0` -> invalid
- 9 ticks -> `$EE` -> valid

### Original asymmetry / quirk

The right-side test is a raw unsigned comparison; it does **not** prove that the offset entered the wrapped negative-right range.

Therefore mixed-direction sequences can produce counter-intuitive but valid results. Example:

- ten left ticks -> `$AF = $14`
- three right ticks -> `$AF = $0E`
- last direction = right
- `$0E < $F0`, so the ROM accepts it as a valid right-ending dodge

This behavior is preserved in ORIGINAL SPEC even though REBORN may later choose to normalize it deliberately.

## Success / failure accounting

Fixed `$F9C6+` evaluates:

`success = ($0661 != 0) AND ($0661 != $0662)`

On success:

- increment `$0678`

On failure:

- increment `$0677`

These counters are not cosmetic. Milo and Camus scripts later use their sum to choose opponent techniques and battle progression branches.

## Damage bypass

Immediately before the Gold Saint's damage calculation, `$F9F9+` repeats the same direction test.

If the dodge is valid and opposite the danger direction, execution jumps directly to `$FA64`, bypassing:

- `$BCFC` opponent damage calculation;
- pending Cosmo/Life drain counters;
- all player resource-decrement loops.

Thus the successful state is a true **zero-damage evade**, not merely a reduction multiplier.

## External behavioral corroboration

Published walkthroughs describe the same visible behavior: the player must move left/right during a narrow defensive window, insufficient displacement can fail, choosing the wrong side can still result in damage, and some story states alter whether an attack is avoidable. This external description is consistent with the reconstructed input and script layers but is not used as the source of numeric rules.

## Executable specification

`spec/original/boss_dodge.py` models:

- direction values;
- parity-derived danger direction;
- 28-iteration input window;
- +2/-2 offset arithmetic with byte wrap;
- asymmetric final displacement validation;
- complete-damage evade condition;
- `$0677/$0678` success/failure counters.

Tests live in `tests/test_boss_dodge_spec.py`.

## Remaining work

1. determine what drives `$0660` beyond its confirmed role as a continuously incrementing phase/parity index;
2. relate dodge availability to boss-specific scripts and story flags;
3. isolate cases described externally as unavoidable/partially unavoidable and determine whether they arise from the dodge gate, scripted weakening, or both;
4. reproduce the visual timing window in a future native battle simulator.
