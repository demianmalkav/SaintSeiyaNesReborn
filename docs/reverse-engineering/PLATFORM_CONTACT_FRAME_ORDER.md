# Platform contact frame ordering — `$927A/$930A` vs `$98BA`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: ordinary common-entity contact and its relative timing against Life/Cosmo drain counters are now represented in executable C#.

## Frame position

The active platform frame reaches bank-1 `$8000` before player simulation. That dispatcher calls the resource-drain routines:

- `$927A` — Life drain scheduling through `$7F`;
- `$930A` — Cosmo drain scheduling through `$80`.

Only later does the frame reach bank-3 `$A442`, where ordinary entity contact calls `$98BA`.

Therefore:

```text
existing $7F/$80 drain
    -> player $AAE4
    -> post-player $B94B
    -> entity pipeline $A442
    -> new contact $98BA may seed $7F/$80
```

A contact detected during frame N cannot be consumed by `$927A/$930A` until frame N+1.

## Ordinary contact gate `$98BA-$9914`

The common call site prepares:

```text
$79 = $10
$7A = $08
$7B = $0E
$7C = $04
```

`$98BA` then rejects contact when:

- frame-start action `$4E` is in family `$40`;
- player Y is `$90+`;
- the player is outside the vertical comparison window;
- the player is outside the horizontal comparison window;
- `$76 != 0`.

### Exact vertical window

Using the prepared constants, the original byte arithmetic becomes:

```text
temp  = byte(entity_y + $10)
lower = byte(temp - $0E - $1E)  = byte(entity_y - 28)
upper = byte(temp + $0E)         = byte(entity_y + 30)
```

Contact requires unsigned:

```text
lower < player_y <= upper
```

### Exact horizontal window

```text
temp  = byte(entity_x + $08)
lower = byte(temp - $04 - $0C)  = byte(entity_x - 8)
upper = byte(temp + $04)         = byte(entity_x + 12)
```

Contact requires unsigned:

```text
lower < player_x <= upper
```

These comparisons intentionally preserve 8-bit wrap. They are not replaced by an unconstrained modern AABB test in compatibility mode.

## Successful contact

When all gates pass and `$76==0`, `$98BA` writes:

```text
entity +$0E -> $7F   ; Life drain ticks
entity +$0D -> $80   ; Cosmo drain ticks
$20          -> $76  ; contact/hazard latch
sound $26
```

The drain counters are overwritten, not accumulated.

## One-frame latency

Example: an entity profile supplies Life ticks 2 and Cosmo ticks 4.

### Frame N

1. pre-player `$927A/$930A` sees old counters, e.g. `0/0` -> no loss;
2. later `$98BA` detects contact;
3. it writes `$7F=2`, `$80=4`, `$76=$20`;
4. frame N ends with no resource loss from that newly-created contact.

### Frame N+1

1. `$927A` consumes Life tick `2 -> 1` and removes 2 Life;
2. `$930A` consumes Cosmo tick `4 -> 3` and removes 1 Cosmo;
3. later phases continue with the reduced counters.

Thus a native implementation that applies contact damage immediately on collision would be one frame early relative to the original.

## Interaction with `$76`

Because `$B94B` decrements `$76` before `$A442`, an old latch can expire to zero and permit a fresh contact later **in the same frame**. If that happens, `$98BA` reseeds `$76=$20` and overwrites `$7F/$80` with the new entity's profile after the current frame's drain phase has already passed.

This establishes three separate compatibility phases:

1. pre-player drain of previously existing counters;
2. post-player `$76` countdown;
3. later entity contact/refresh.

## Clean-room implementation

- `PlatformEntityContact` reproduces the exact `$98BA` gate and write effects.
- `PlatformContactFramePhases` composes pre-player drain and later entity contact without conflating them.
- tests assert strict/inclusive collision bounds, byte wrap, latch suppression, overwrite behavior and the contact-N -> drain-N+1 delay.

No ROM payload is embedded.
