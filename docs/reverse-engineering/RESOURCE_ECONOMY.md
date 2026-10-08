# Resource economy — Life, Cosmo, Seventh Sense and caps

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: the persistent Saint record, temporary configuration/battle values, 1:1 exchange routines and Seventh Sense gain paths are statically reconstructed.

## Persistent five-byte Saint record

Canonical snapshot records `$058C-$05A4` are five bytes each:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, cap byte]`

The fifth byte is now semantically resolved.

## Cap byte

For a selected Saint, bank 1 `$AB4E+` loads the fifth snapshot byte and splits it into:

- low nibble -> `$05BE`
- high nibble -> `$05D0`

The increment routines prove their roles:

- `$05BE` is the **exclusive Cosmo hundreds boundary**;
- `$05D0` is the **exclusive Life hundreds boundary**.

When increasing a stat by one, the game performs packed-decimal increment and rejects/reverts the increment as soon as the stat's hundreds digit is greater than or equal to its boundary.

For the normal values used by the game:

`maximum = boundary * 100 - 1`

Examples:

- boundary `1` -> maximum `99`
- boundary `2` -> maximum `199`
- boundary `5` -> maximum `499`

Thus the persistent cap byte is:

`(life_boundary << 4) | cosmo_boundary`

## Initial values

Initialization around bank 1 `$95B1+` sets:

### Seiya, Shun, Hyoga, Shiryu

- Life `99`
- Cosmo `99`
- cap byte `$11`

Therefore both resources initially cap at 99.

### Ikki

- Life `499`
- Cosmo `499`
- cap byte `$55`

This independently explains the special fifth-slot initialization and confirms the cap-byte interpretation.

## Active configuration/battle resource mirrors

When a Saint is selected for the resource-allocation screen, bank 1 `$AB10+` maps the persistent record into:

- `$05CE/$05CF` — active Life BCD
- `$05BC/$05BD` — active Cosmo BCD
- `$05D0` — Life hundreds boundary (exclusive)
- `$05BE` — Cosmo hundreds boundary (exclusive)

When leaving/switching, the same routine writes those values back into the selected persistent five-byte record.

This resolves earlier community labels for `$05BC` and `$05CE`.

## Spend Seventh Sense -> gain Life/Cosmo

Bank 1 menu paths around `$A2A3+` / `$A90B+` first require a nonzero Seventh Sense pool.

Depending on selected resource:

- fixed `$FBCF` increments active Life by exactly 1 BCD unit, unless at cap;
- fixed `$FCC7` increments active Cosmo by exactly 1 BCD unit, unless at cap;
- on successful increment, fixed `$FE26` decrements Seventh Sense by exactly 1.

Therefore:

`1 Seventh Sense -> 1 Life`

or

`1 Seventh Sense -> 1 Cosmo`

subject to the selected Saint's cap.

## Sacrifice Life/Cosmo -> gain Seventh Sense

The reverse menu branch uses:

- fixed `$FC2E` to decrement active Life by exactly 1, refusing at zero;
- fixed `$FD26` to decrement active Cosmo by exactly 1, refusing at zero;
- fixed `$FDE0` to increment Seventh Sense by exactly 1 after a successful sacrifice.

Therefore:

`1 Life -> 1 Seventh Sense`

or

`1 Cosmo -> 1 Seventh Sense`

The game prevents the selected source stat from going below zero and caps Seventh Sense at 9999.

## Direct Seventh Sense rewards

Two distinct reward granularities exist.

### Enemy/platform reward — fixed `$D1E0`

Takes an 8-bit packed-BCD amount (00..99) and adds it to the four-digit pool, saturating at 9999. Ordinary enemy kill records supply this amount from entity offset `$0F`.

### Large scripted rewards — fixed `$F31E`

This routine adds its packed-BCD input to the **high two decimal digits** of Seventh Sense (`$05AB`). Semantically this grants amounts in hundreds.

For example an input of `$02` corresponds to +200; `$10` corresponds to +1000, subject to the routine's 9999 clamp.

Its numerous callers in event/progression code match the game's large scripted Seventh Sense awards.

## Display representation

`$05AA/$05AB` are four packed-decimal digits:

- `$05AA` = tens/ones pair (lower two decimal digits)
- `$05AB` = thousands/hundreds pair (upper two decimal digits)

Fixed `$FEC6` expands the four nibbles into display tiles at `$05AD-$05B0`.

## ORIGINAL SPEC model

The core resource relationships are now:

```text
enemy kills / scripted events
          -> Seventh Sense (0..9999)

Seventh Sense 1:1 <-> Life
Seventh Sense 1:1 <-> Cosmo

Life/Cosmo upper limits
          <- persistent per-Saint cap byte
```

In platform mode both Life and Cosmo are survival resources; reaching zero can cause failure. Meanwhile current Cosmo also changes platform attack damage and attack range.

This means resource allocation is not cosmetic RPG bookkeeping: moving points between Seventh Sense, Life and Cosmo directly changes both survivability and offensive performance.

## Remaining work

1. map every story event that increases the cap byte;
2. determine whether any exceptional exchange rate exists in special boss/revival scenes;
3. connect battle-mode attack formulas to active `$05BC-$05D0` values;
4. catalog `$F31E` callers to recover scripted Seventh Sense reward values by event.
