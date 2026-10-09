# Platform projectile → entity hit resolution — `$9915/$992A`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: projectile signature filtering, unsigned-byte overlap, slot iteration, Saint-specific hit retirement, ordinary HP/reward handling, numeric type special cases and reaction-phase assignment are represented in C# OriginalSpec.

## Slot iteration — `$9915`

`$9915` prepares and checks attack records in this fixed order:

1. slot 0 — `$0730`, counter `$038E`;
2. slot 1 — `$0738`, counter `$038F`;
3. slot 2 — `$0740`, counter `$0390`.

It invokes the hit routine for each slot sequentially. It does not return after the first successful collision.

This matters because mutations from one hit are visible to the next slot test inside the same entity update.

## Accepted attack signatures

At `$992A` attack offset `+1` is masked with `$FE`.

Accepted families:

```text
$64/$65 -> generic projectile
$54/$55 -> Shun chain segments
```

Inactive `$FE` and unrelated object signatures return immediately.

## Parameterized overlap

The hit routine receives four bytes:

- `$79` vertical origin offset;
- `$7A` horizontal origin offset;
- `$7B` vertical extent;
- `$7C` horizontal extent.

For entity `(X,Y)` and attack point `(attackX,attackY)`:

```text
vertical_temp = byte(entityY + $79)
vertical_low  = byte(vertical_temp - $7B - 6)
vertical_high = byte(vertical_temp + $7B)

horizontal_temp = byte(entityX + $7A)
horizontal_low  = byte(horizontal_temp - $7C - 8)
horizontal_high = byte(horizontal_temp + $7C)
```

Both axes require unsigned:

```text
low < attack_coordinate <= high
```

The common `$A442` call site uses:

```text
$79=$10, $7A=$08, $7B=$0E, $7C=$04
```

so its effective windows are:

```text
Y: byte(entityY-4)  < attackY <= byte(entityY+30)
X: byte(entityX-4)  < attackX <= byte(entityX+12)
```

The byte arithmetic is intentionally preserved; compatibility mode must not silently replace it with unbounded integer AABBs.

## Initial collision effect

Any accepted overlap requests sound `$28` before numeric type-specific handling.

## Projectile retirement helper — `$9A27`

When the collision branch calls `$9A27`, retirement depends on current platform Saint index:

- Seiya `0`: keep current hit object active;
- Shun `1`: keep active;
- Hyoga `2`: retire current hit object;
- Shiryu `3`: retire current hit object;
- Ikki `4`: keep active.

Hit retirement writes only:

```text
attack Y    = $F0
attack type = $FE
```

It does not clear the slot's range counter.

Type `$0D` goes directly to HP damage and bypasses `$9A27`, so even Hyoga/Shiryu do not retire the current hit object through this helper on that type.

## Numeric entity type branches

After overlap:

### `$0A/$0B`

- call `$9A27`;
- no ordinary HP subtraction;
- apply directional reaction phase with magnitude 4;
- no type-reaction sound-table call.

### `$0D`

- bypass `$9A27`;
- go directly to ordinary HP subtraction;
- lethal action is `$A0`, not `$D0`.

### Other types

Except type `0`, a type-specific sound byte is read from fixed table `$C0D3` before the remaining type branches.

- type `0` -> HP path;
- types `1..4` -> `Y += 6`, action `$E0`, no HP subtraction;
- types `5..9` -> HP path;
- `$0C` -> HP path;
- `$0E` -> return after collision/retirement/sounds, no HP or action rewrite;
- `$0F` -> HP path;
- other higher values would follow the `$E0` reaction branch, though current known common type range is `0..$0F`.

## Ordinary HP path — `$99BA`

```text
remaining = entity_hp - $72
```

If positive:

- store remaining HP;
- set action `$40`;
- for types `<$08`, attempt directional reaction-phase assignment with magnitude 8;
- for types `$08+`, return after action `$40` without that phase write.

If zero/borrow:

- do **not** write zero back to entity HP;
- add packed-BCD entity offset `$0F` through fixed `$D1E0` when substate/mode allows;
- type `$0D` -> action `$A0`;
- all other HP-path types -> action `$D0`.

The fact that lethal HP is not stored is observable if another attack slot is checked later in the same `$9915` call.

## Directional reaction phase — `$99F9-$9A25`

Projectile facing comes from attack offset `+2`, bit `$40`.

The entity terrain probe on that side is consulted:

- right-facing attack -> entity `+$0A`;
- left-facing attack -> entity `+$0B`.

Only descriptor family `$E0-$EF` suppresses the phase write. `$F0+` does **not** suppress it.

When allowed:

```text
left  -> phase = magnitude
right -> phase = $40 + magnitude
```

Magnitude is:

- 4 for the `$0A/$0B` special branch;
- 8 for surviving ordinary HP hits on types `<$08`.

## Sequential-slot quirk

Because `$9915` checks all three attack records without re-running the higher-level entity-state gate, a lethal hit in slot 0 does not automatically prevent slot 1 from entering `$992A`.

The kill path also leaves entity HP unchanged in memory. Therefore multiple overlapping accepted attack records can run the lethal/reward path sequentially in the same call.

A concrete compatibility fixture uses Shun `$54/$55` segments: neither is retired by `$9A27`, so two overlapping segments can each execute the kill/reward branch.

This is recorded as original behavior. REBORN may intentionally normalize it in a non-compatibility ruleset, but the parity layer must not erase it.

## Clean-room implementation

`PlatformProjectileEntityHit` provides:

- attack-signature recognition;
- parameterized byte-exact overlap;
- single-slot hit resolution;
- fixed slot 0→1→2 batch resolution;
- Saint-specific current-object retirement;
- HP/reward/type-state reactions;
- directional reaction phase.

Tests cover strict/inclusive hitbox bounds, surviving hits, kills, reward gate, `$0D`, `$0A`, `$0E`, terrain phase suppression, Hyoga/Shiryu retirement and the multi-slot sequential-kill behavior.

No ROM payload is embedded.
