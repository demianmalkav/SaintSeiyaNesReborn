# Scheduled special entities `$0D/$0E` — active runtime

Status: **clean-room semantic promotion from the canonical Japanese ROM**.

This document covers scheduled primary-entity types `$0D` and `$0E` after `$8927` has created them in one of the two primary logical/visual slots.

The crucial result is that `$0D/$0E` are **not** variants of the already-promoted `$08/$09/$0C` pre-dispatch. They join the later shared entity path at `$A55E` but bypass the earlier `+$04/$039A` control block.

## Dispatcher split

Bank-3 `$A47A+` reads logical type `+$09` and routes:

```text
$08 -> $A4A7
$09 -> $A4A7
$0C -> $A4A7
$0D -> $A4BF -> $A55E
$0E -> $A4BF -> $A55E
$0F -> $A74C
```

`$A4A7-$A55B` is the already-modeled `$08/$09/$0C` pre-dispatch: it examines/updates logical `+$04`, shares global `$039A`, and can call `$A908` before falling through to `$A55E`.

`$0D/$0E` never execute that block. Their dedicated runtime therefore receives no `$039A` state and the hybrid scheduler carries global `$039A` through unchanged.

## `$A55E` action-family entry

The shared family dispatch is:

```text
$50/$E0 -> $A57E fall/drop path -> $A886
$D0/$40 -> $A5BE
$30     -> $C5E6 jump vertical -> $A5BE
other   -> $A970 -> $A5BE
```

For type `>= $08`, `$A970` returns without the common `$00-$07` decision/chase logic. Thus `$0D/$0E` ordinary action `$00` receives no common AI decision.

After `$A5BE`:

- `$3x` uses the existing shared jump horizontal cadence for `$31/$32`;
- families `$00/$70/$D0/$40/$A0` take the camera-only horizontal path;
- other families, notably `$10` after `$70` completion, use the shared facing walk cadence;
- camera delta `$43` is subtracted;
- `X >= $F8` or `Y in $B0-$BF` reaches shared removal `$A647`.

## No phase-zero proximity fall for `$0D/$0E`

At `$A6AA+` the original excludes `$08`, `$09`, and every type `>= $0D` from the phase-zero proximity-fall branch:

```text
CMP #$08 / BEQ $A700
CMP #$09 / BEQ $A700
CMP #$0D / BCS $A700
```

Therefore both `$0D` and `$0E` go directly to `$A700`; they must not inherit the type-$0C` proximity-fall rule.

## Main interaction and attached hazard

At `$A700`, families `$E0/$D0/$A0/$40` skip ordinary interaction. Other families execute the shared sequence:

```text
$9915 projectile-vs-entity
$98BA entity-vs-player contact
$AA70 attached-record contact
post-interaction +$04 cadence
```

The same tall projectile geometry already used by the `$08/$09/$0C` shared path is reused here.

Although `$0D/$0E` bypass the *early* `+$04` pre-dispatch, `$A738-$A746` still applies after ordinary interaction:

```text
+$04 == 0 -> unchanged
+$04 != 0 -> increment; wrap to 0 at $0C
```

`$8927` initializes `+$04 = 0`, so a freshly spawned `$0D/$0E` normally leaves this cadence dormant unless another confirmed path makes the byte nonzero.

## `$40` reaction

After interaction, `$A79E+` advances family `$40` immediately. Completion differs from common `$00-$07` entities:

```text
type < $08  : completed $40 -> $10
type >= $08 : completed $40 -> $00
```

`$0D/$0E` therefore use the special `>= $08` rule and return to `$00`. The common `$40` helper cannot be reused because it validates type `< $08` and deliberately returns to `$10`.

## `$D0` death

`$0D/$0E` use the shared `$D0-$DF` death cadence:

- only frames with `($3C & 3) == 0` advance;
- some ground classes add `Y += 2`;
- action increments;
- reaching `$E0` enters `$A647` removal.

Type `$0F` has separate handling and is outside this runtime.

## Type `$0D` unique `$A0-$AF` terminal family

Bank-3 `$A86B-$A885` applies only to type `$0D`:

```text
if type == $0D and action family == $A0:
    action++
    if action >= $B0:
        $A647 removal
```

Type `$0E` does not execute this progression. A type `$0E` state in family `$A0` simply follows the preceding interaction-skip path and reaches `$A886` unchanged.

The hybrid scheduler composes `$A647` after the semantic runtime reports the `$0D` terminal condition. This automatically picks up the already-confirmed type-$0D` extra visual retirement at relative `+$2C/+2D`.

## Late `$70` family

`$A886-$A8DB` is shared and the existing `PlatformCommonEntityAttack70.AdvanceAfterInteraction(...)` is explicitly type-aware.

For `$0D/$0E`:

- odd `$3C` frames advance `$70`;
- reaching `$78` calls `$A908` and plays `$2A` because only `$08/$0C` are excluded from the midpoint call;
- the fixed secondary-object template for `$0D/$0E` is zero, so `$A908` creates no attached object;
- reaching `$80` completes to action `$10`;
- unlike `$08/$09/$0C`, terminal completion does not call `$A908` or play `$2D`.

This is why the shared type-aware late helper is reused rather than copying `$08/$09/$0C` terminal behavior.

## Persistent/hybrid integration

`PlatformHybridEntityCombatSlice` now routes admitted types as:

```text
$08/$09/$0C -> PlatformSpecialEntityActive08090C
$0D/$0E     -> PlatformSpecialEntityActive0D0E
common set  -> PlatformCommonEntitySlotRuntime
```

All routes remain behind the common visual/logical activity gate.

For `$0D/$0E`, the scheduler carries:

- attack-object state;
- `$76/$7F/$80` contact/drain state;
- Seventh Sense;
- logical entity state;
- `+$04`;
- attached hazard state;
- parent `+$08`;
- primary visual occupancy/removal state.

Global `$039A` is deliberately passed through unchanged because these types never enter `$A4A7-$A55B`.

## Regression boundary

Fixtures cover:

1. actual `$8927` production of both `$0D` and `$0E`, followed by same-frame routing through the dedicated hybrid runtime;
2. persistence of global `$039A` across those updates;
3. ordinary main contact plus post-interaction `+$04` cadence;
4. type `$0D` `$AF->$B0` terminal removal, including `$A647` type-$0D` extra visual retirement and next-frame free-slot gating;
5. type `$0E` `$7F->$10` terminal attack behavior from the shared type-aware `$70` helper.

## Open boundary

This promotion does not include type `$0F`, whose dispatcher jumps to `$A74C`, nor does it claim complete renderer-side animation ownership. Those remain separate boundaries.
