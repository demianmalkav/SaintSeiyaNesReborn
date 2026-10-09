# Platform entity types `$0A` / `$0B`

Status: **CONFIRMED by static ROM flow** for the complete normally reachable active action-family closure, ordinary `$10`, proximity/fall `$50`, projectile response, post-interaction `+$03` motion, and terminal retirement behavior described here.

## Reachable action-family closure

For normally produced `$0A/$0B` instances, the complete **active** action-family set is:

```text
{ $10, $50 }
```

A retired slot may additionally contain logical action `$00` because the shared removal helper `$A647` clears offset `+$00` when engine state `$00 < $30`. That `$00` is a terminal/free-slot state, not a third active family: `$A647` also retires the visual record (`sprite +1 = $FE`), so the ordinary activity gate no longer dispatches it as a live `$0A/$0B` entity.

The closure follows from the writer/control-flow audit below.

### Producer

The generic edge producer `$B6D0` is the normal producer for types `$0A/$0B`. On spawn it writes logical action `$10`.

The table-driven scheduled producer `$8925-$89D1` supports only `$08/$09/$0C/$0D/$0E`; it cannot produce `$0A/$0B`.

### Predispatch exclusion of `$70`

In bank 3, the primary slot predispatch checks type `$0A` and `$0B` at `$A4A0/$A4A4` and branches directly to `$A4C0 -> $A55E`. This bypasses the earlier generic predispatch path that can seed action `$70` for other entity types.

Therefore normally produced `$0A/$0B` cannot enter `$70` through that shared attack setup.

### Ordinary `$10` writers

At `$A970`, types `$0A/$0B` bypass the 31–94 frame decision timer and go directly to the terrain-facing check `$AA1F`. In particular they do not enter the decision path that can create the `$30` jump family for ordinary common types.

The ordinary path can:

- remain in `$10`;
- enter `$50` through the confirmed proximity/fall write at `$A6F3`;
- retire the entity through a removal condition.

It does not create `$30`, `$40`, `$70`, `$D0` or `$E0` for `$0A/$0B`.

### `$50` writers

The `$50` route can:

- remain `$50` while falling;
- land through fixed `$C491`, which writes `$10` for `$0A/$0B`;
- retire at the lower boundary.

It has no writer to another live action family.

### Projectile/contact/post-hit writers

`$9971+` special-cases `$0A/$0B` before the ordinary HP/reaction path. A projectile hit may be consumed and may write directional impulse to logical offset `+$03`, but it does **not** subtract HP or replace offset `+$00`. This prevents projectile handling from creating `$40`, `$D0` or `$E0` for these types.

The following `$98BA` player-contact path mutates player/contact state (`$76/$7F/$80`) rather than the entity action byte.

After interaction, `$A839 -> $A845` may decrement and consume the `+$03` directional impulse and move X. It does not write action offset `+$00`.

### Retirement

Shared helper `$A647` retires the visual slot. When engine state `$00 < $30`, it also clears logical action to `$00`; when `$00 >= $30`, the old logical action may remain behind a visual `$FE` marker. In either case the entity is no longer a normally active `$0A/$0B` slot.

So the full reachable lifecycle is:

```text
spawn -> $10
          | \
          |  -> retirement (visual $FE; optional logical $00)
          v
         $50
          | \
          |  -> retirement (visual $FE; optional logical $00)
          v
         $10
```

No normal writer reaches `$30/$40/$70/$D0/$E0` for `$0A/$0B`.

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

- `PlatformCommonEdgeSpawner` is the normal `$0A/$0B` producer and seeds `$10`.
- `PlatformScheduledSpecialEntitySpawner` explicitly excludes `$0A/$0B` from its supported set.
- `PlatformCommonEntityPreparation` admits `$0A/$0B` on `$10` and reuses the already-exact terrain/cadence helpers without the common jump-decision transition.
- `PlatformCommonEntityFall50` admits `$0A/$0B`; shared landing `$C491` returns them to `$10`.
- `PlatformProjectileHitRouter` keeps their action and HP intact while optionally writing `+$03` impulse.
- `PlatformEntityMotion3Knockback` is the shared `$A845` primitive.
- `PlatformCommonEntitySlotRuntime` admits `$0A/$0B` only for the confirmed live families `$10/$50` and deliberately rejects injected unreachable active families instead of generalizing corrupted states by analogy.
- `PlatformEntityRemovalA647` models the terminal visual retirement and optional logical `$00` clear.

The defensive rejection of injected `$30/$40/$70/$D0/$E0` states is therefore a clean-room boundary, not an assertion that the raw ROM dispatcher could never execute code if memory were externally corrupted into such a state.
