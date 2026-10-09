# Entity post-interaction timing correction

Status: **CONFIRMED by direct static ROM flow**.

This document records a timing correction to the clean-room runtime. Earlier composition treated projectile-created `$40` and `$D0` states as states that began processing on the following frame. Re-reading the actual fall-through from `$9915/$98BA` proves that this was one frame late.

## Relevant control flow

For an ordinary entity path the ROM executes:

```text
$A723  JSR $9915   ; player projectile -> entity
$A726  JSR $98BA   ; entity -> player contact
...
$A749  JMP $A79E
```

`$A79E` immediately examines the entity action byte. If the projectile hit just wrote family `$40`, the reaction is processed now:

```text
$40 created by $9915
  -> same frame $A79E
  -> $40 becomes $41
  -> type < $08 falls into $A845
  -> freshly seeded +$03 recoil is decremented and applied
```

The flow then reaches the `$D0` test at `$A7FB`. A kill created by `$9915` therefore also reaches death cadence logic on the kill frame:

```text
$D0 created by $9915
  -> same frame $A7FB
  -> if ($3C & 3) != 0: remains $D0
  -> if ($3C & 3) == 0: advances immediately to $D1
     and may apply descriptor-dependent +2 Y
```

Later logic such as type `$0A/$0B` `$A845` handling and `$70` `$A886` progression sees the state left by these earlier post-interaction phases.

## Frame-start `$40` is different from hit-created `$40`

An entity that already enters the frame in family `$40` passes through the shared screen-position path before `$A79E`:

```text
frame-start $40
  -> $A5F1 / $A636
  -> X -= $43
  -> horizontal/vertical removal gates
  -> $A79E reaction increment
  -> $A845 recoil for type < $08
```

Therefore camera correction occurs once for a frame-start reaction. It must **not** be repeated for a `$40` created after the entity already crossed that earlier path in the current frame.

The clean-room implementation exposes this distinction as:

- `PlatformCommonEntityHitReaction40.Step(...)`: frame-start route, including camera/removal gates;
- `PlatformCommonEntityHitReaction40.AdvanceAfterPath(...)`: late `$A79E/$A845` only.

## Frame-start `$D0` vs kill-created `$D0`

The same principle applies to death:

- `PlatformCommonEntityDeathD0.Step(...)` handles a frame that starts in `$D0`, including `X -= $43` and the earlier removal gates;
- `PlatformCommonEntityDeathD0.AdvanceAfterPath(...)` handles only `$A7FB-$A839`, for a `$D0` written during the current hit sequence.

This prevents accidental double application of `$43` on a kill frame.

## Consequences for parity fixtures

A normal surviving right-facing hit on type `<$08` no longer ends the hit frame at `$40/$48`. The observable end-of-frame state is:

```text
$9915: action=$40, +$03=$48
$A79E: action=$41
$A845: +$03=$47, X += 4
```

On the following frame, if `$43=1`, a persisted `$41/$47` reaction performs:

```text
X -= 1
$41 -> $42
$47 -> $46
X += 4
```

Similarly, a lethal hit on a `$3C` value divisible by four can end the kill frame already in `$D1`, rather than `$D0`.

## Epistemic note

The former one-frame-late model was a reasonable decomposition from isolated routines but was contradicted by the direct branch/fall-through structure of `$A700-$A839`. The corrected ordering is now the source of truth and is guarded by executable fixtures.
