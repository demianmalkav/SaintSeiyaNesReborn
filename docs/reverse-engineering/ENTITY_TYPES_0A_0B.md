# Platform entity types `$0A` / `$0B`

Status: **CONFIRMED by static ROM flow** for the ordinary `$10`, proximity/fall `$50`, projectile-response, and post-interaction `+$03` motion described here.

## Ordinary route

At `$A970`, types `$0A/$0B` bypass the 31–94 frame decision timer and go directly to the terrain-facing check `$AA1F`. Their stored decision-timer byte is therefore not decremented by this route.

At `$A60B` they use a special horizontal cadence:

```text
step = $3C & 1
```

So they move 0 px on even `$3C` and 1 px on odd `$3C`, in the current facing direction. The shared `$43` camera subtraction then applies normally.

They are not part of the `$08/$09` or `$0D+` proximity exclusions at `$A68A-$A6FE`, so with phase `+$03 == 0` they can enter `$50` / `Y += 6` when the usual player-below/proximity conditions are met. The current update still continues through `$9915/$98BA`; `$50` takes control only on the next entity dispatch.

## `$50` fall

The `$50` path is the same structural path already promoted for common entities:

1. `X -= $43`.
2. `Y += 3`.
3. remove at post-step `Y >= $B0`.
4. otherwise call fixed `$C491` landing.

For `$0A/$0B`, `$C491` returns to action `$10` on landing.

## Projectile response

`$9971+` special-cases both types before the ordinary HP route:

- standard projectile-consumption helper still runs (Hyoga/Shiryu retire their projectile);
- HP is not subtracted;
- action state is not replaced;
- `$99F9/$9A19` attempts to write a 4-tick directional impulse into record offset `+$03`:
  - right-facing attack -> `$44`;
  - left-facing attack -> `$04`;
  - active-side terrain `$E0-$EF` blocks this write.

## Same-frame post-hit knockback `$A845`

After `$9915` and `$98BA`, `$A839` detects type `$0A` or `$0B` and falls into `$A845`.

`$A845` uses the shared `+$03` byte:

1. if low nibble is zero, no motion;
2. otherwise decrement the byte first;
3. if remaining byte `>= $40`, `X += 4`;
4. otherwise `X -= 4`.

Consequently a new right hit can perform this in one entity update:

```text
$9915: +$03 = $44
$98BA: player-contact test at pre-knockback X
$A845: $44 -> $43, X += 4
```

The impulse persists across later ordinary frames until its low nibble reaches zero. If the entity entered `$50` earlier in the same ordinary update, `$A845` still runs once that frame; subsequent `$50` frames bypass `$A845`.

## Clean-room representation

- `PlatformCommonEntityPreparation` admits `$0A/$0B` on `$10` and reuses the already-exact decision/terrain and cadence helpers.
- `PlatformCommonEntityFall50` admits `$0A/$0B`.
- `PlatformEntityMotion3Knockback` is the shared `$A845` primitive, also used by the type `$00-$07` `$40` reaction path.
- `PlatformTwoCommonEntityCombatSlice` applies that primitive **after** interaction for `$0A/$0B` ordinary routes, preserving same-frame hit -> contact -> knockback ordering.

No behavior for other action families is implied by this document; the active dispatcher deliberately rejects unsupported `$0A/$0B` families instead of generalizing them by analogy.
