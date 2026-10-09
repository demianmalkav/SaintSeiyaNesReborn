# Common entity drop-reaction family `$E0`

Status: **CONFIRMED by static ROM flow** and promoted to executable OriginalSpec for entity types `$00-$07`.

## Entry

The projectile hit router can write `$E0` to the entity action byte for special non-HP reactions. The same hit path also applies an immediate `Y += 6`. On the next entity update the `$A442` dispatcher sees family `$E0`.

## Exact path

At bank 3 `$A55E` family `$E0` shares the initial fall block with `$50`:

1. `X = X - $43` (camera-relative screen correction).
2. `Y = Y + 3`.
3. If post-step `Y >= $B0`, the entity record is removed through `$A647`.
4. Otherwise `$E0` is detected at `$A5A7-$A5AB` and control jumps directly to `$A5B8/$A886`.

The critical difference from `$50` is step 4: `$E0` **does not call fixed `$C491`**, so it cannot land or snap back to a floor. It continues falling until removed.

The path also never reaches `$A700-$A735`, therefore current-frame `$9915` projectile collision and `$98BA` player contact are skipped.

## Clean-room representation

`PlatformCommonEntityDropE0.Step(...)` exposes only the confirmed semantic effects:

- camera delta on X;
- fixed +3 Y fall;
- lower-band removal at post-step `$B0+`;
- no landing resolver.

`PlatformCommonEntityActiveDispatcher` maps `$E0` to `DropE0` with `SkipInteraction` while active and `Removed` at the lower boundary. `PlatformTwoCommonEntityCombatSlice` accepts persisted `$E0` entities across frame boundaries.

A cross-frame fixture covers the natural transition:

```text
ordinary type01
  -> projectile hit
  -> immediate Y+6, action=$E0
  -> next frame: X-=$43, Y+=3
  -> no projectile/contact interaction
  -> repeat until Y >= $B0
```
