# Platform pre-player resource order

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition for the non-fatal active-platform path.

## Confirmed dependency

Existing contact-drain counters are consumed during the bank-1 frame work before the player-action dispatcher `$AAE4`.

The later active-frame attack inputs are derived from the resulting resource state:

- fixed `$C52F` maps bank 1 and calls `$8616` to calculate platform damage `$72` from current Cosmo;
- `$BBCA` creates the player attack and selects its range/lifetime seed from current Cosmo.

Therefore both calculations observe **post-drain Cosmo**, not the value at frame entry.

## Boundary example: Cosmo 200 -> 199

For Seiya:

- at Cosmo 200, platform damage is 38 and the projectile range seed is 6;
- if one pending `$80` Cosmo-drain tick is consumed first, Cosmo becomes 199;
- at Cosmo 199, platform damage is 36 and the projectile range seed falls to 3.

A single pre-player drain tick can therefore change both attack strength and projectile lifetime/range on the very same frame in which B is pressed.

## Executable model

`PlatformPrePlayerResourcePhases.StepNonFatal` composes:

1. the already-confirmed pre-player attack-busy advancement;
2. `$7F/$80` drain consumption;
3. semantic Life/Cosmo subtraction;
4. platform damage derivation from post-drain Cosmo;
5. the `$AAE4` player dispatcher using that same post-drain Cosmo for B/projectile range.

The helper is intentionally restricted to non-fatal frames. If Life or Cosmo would reach zero, the original enters its failure/death transition rather than continuing through the ordinary player path; that transition remains a separate composition boundary.

No ROM payload is embedded in the model.
