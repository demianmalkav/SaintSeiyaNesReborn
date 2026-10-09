# Common entity hit reaction `$40-$4F`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room model for common entity types `$00-$07`.

## Action-state duration

Bank-3 `$A79E+` handles the `$40` action family separately from ordinary movement/interactions.

For types `< $08`:

- current action is incremented once per update;
- `$40 -> $41 -> ... -> $4F`;
- the update after `$4F` returns action to `$10`.

Thus the reaction family lasts sixteen state values.

## Short knockback counter in record `+$03`

After updating the action state, types `< $08` jump to `$A845`.

The same entity-record byte used as `state_phase` by jump logic carries the projectile-hit recoil value written by `$99F9/$9A19`:

- rightward hit reaction commonly seeds `$48`;
- leftward hit reaction commonly seeds `$08`.

`$A845` performs:

1. if low nibble is zero, no knockback movement occurs;
2. otherwise decrement the full byte by one;
3. write it back to record `+$03`;
4. if the remaining byte is `>= $40`, add 4 to entity X;
5. otherwise subtract 4 from entity X.

The direction check is made **after** decrement.

For a normal seed of `$48` or `$08`, this yields up to eight 4-pixel knockback ticks. The action-family reaction continues after those motion ticks have finished.

## Terminal-frame detail

The `$4F` update first converts the action back to `$10`, then still falls through the same `$A845` knockback helper for types `<$08`.

If record `+$03` still has a nonzero low nibble on that terminal frame, one final knockback tick is applied even though the stored action has already returned to ordinary `$10`.

## Interaction and camera consequences

The `$40` path branches away from the ordinary `$9915/$98BA` call sequence. A common entity in this reaction family therefore does not take the ordinary projectile-hit/contact route during that update.

The `$A845` knockback write also does not subtract global camera delta `$43`; it directly modifies entity screen X by ±4.

These are parity properties of the original engine, not recommendations for REBORN's eventual modernized combat feel.

## Executable model

`PlatformCommonEntityHitReaction40.Step` preserves:

- sixteen-state `$40-$4F` timing;
- type `<$08` return to `$10`;
- low-nibble knockback exhaustion;
- decrement-before-direction semantics;
- ±4 byte X arithmetic;
- terminal-frame knockback behavior.

No ROM payload is embedded in the model.
