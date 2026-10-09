# Type `$0F` direct platform entity runtime

Status: **CONFIRMED by direct static flow in the canonical Japanese ROM and composed clean-room fixtures**.

Type `$0F` is produced by the generic bank-0 `$B6D0` edge spawner, but it is not a common-runtime entity once bank-3 `$A442` processes the slot. The dispatcher recognizes the type explicitly and jumps directly:

```text
$A491  CMP #$0F
$A493  BNE $A498
$A495  JMP $A74C
```

It therefore bypasses common `$A55E` preparation and both scheduled-special pre-dispatch routes.

## Direct movement at `$A74C`

Every admitted update starts with:

```text
Y += 2
if Y >= $A0:
    $A647 removal
```

Horizontal motion then depends only on facing bit `$40` in logical `+$07`:

```text
facing bit clear -> X -= 2
facing bit set   -> X += 2
X -= camera $43

if X < 4:
    $A647 removal
```

All arithmetic is byte arithmetic before the unsigned `<4` comparison.

This happens before the action-family interaction gate, so `$40` and `$D0` records also receive the direct `$0F` movement before their post phases.

## Interaction geometry

At `$A780`, action families `$D0` and `$40` skip projectile/contact interaction and go directly to `$A79E`.

All other families install:

```text
$79 = 8
$7A = 8
$7B = 6
$7C = 6
```

and execute:

```text
$9915 projectile hit
$98BA player contact
```

in that order.

The clean-room runtime therefore uses `PlatformHitboxParameters.Square` for projectile collision and the same `8/8/6/6` parameters for `$98BA` contact.

### No `$AA70` on this route

Unlike `$08/$09/$0C` and `$0D/$0E`, the direct `$0F` path does **not** call `$AA70` after `$98BA` and does not pass through the `$A738` logical `+$04` cadence.

Any attached-hazard record and logical `+$04` value therefore remain untouched by an ordinary `$0F` update.

## `$40` reaction

After interaction, `$A79E` is shared. Type `$0F` is `>= $08`, so a reaction advances:

```text
$40 -> $41 -> ... -> $4F -> $00
```

A surviving projectile hit that writes `$40` during `$9915` can therefore finish that same update at `$41`.

## `$D0` death cadence is unique

The important type-specific branch is at `$A7FB`:

```text
LDY #$09
CMP #$0F
BEQ $A807
LDA $3C
AND #$03
BNE $A839
```

The type byte loaded at `$A7D0` remains in `A`. For type `$0F`, the branch goes directly to `$A807`, bypassing the ordinary `($3C & 3)==0` cadence gate.

Therefore `$D0-$DF` advances **every admitted update** for type `$0F`.

The shared death body still applies descriptor-dependent `+2 Y` when the ground descriptor is `<$80` or `>=$F0`, increments the action byte, and on terminal completion clears action then jumps to `$A647`.

This is intentionally not routed through `PlatformCommonEntityDeathD0`, whose cadence and type validation are different.

## Late `$70`

Surviving control eventually reaches shared `$A886`.

The existing type-aware late helper is valid for `$0F`:

- progression occurs only on odd `$3C`;
- `$77 -> $78` requests `$A908` and sound `$2A` because only `$08/$0C` are excluded at the midpoint;
- the fixed `$A908` table entry for `$0F` is zero, so the request creates no attached object;
- terminal `$7F` completes to `$10`, not to the `$00`/terminal-spawn behavior reserved for `$08/$09/$0C`.

## `$A647` retirement and persistence

Movement removal and terminal `$DF` death both feed the already-promoted exact `$A647` lifecycle. At engine state `<$30`, the resulting persistent slot has action `$00` and visual occupancy `$FE`, so the next frame's activity gate skips it and the generic producer can later reuse it without manual repair.

## Clean-room integration

`PlatformSpecialEntityActive0F` owns only the direct `$0F` semantic body. `PlatformHybridEntityCombatSlice` routes type `$0F` into that runtime and applies `$A647` when it reports a removal outcome.

Fixtures discriminate:

1. actual `$B6D0` type-`$0F` output entering the dedicated route in the same frame;
2. projectile coordinates accepted by `8/8/6/6` but rejected by the common `8/8/5/5` box;
3. absence of `$AA70` and `+$04` cadence;
4. `$DF` terminal death on a frame where `($3C&3)!=0`;
5. midpoint and terminal `$70` behavior through the shared type-aware helper.

No ROM payload is committed.
