# Latched common producer phase

Status: **clean-room composition of confirmed producer ordering and latch semantics**.

The platform runtime has two separate sources of encounter state:

- `$03B7`: newest descriptor staged from the current page;
- `$58` plus `$03AE/$03AD/$03AC/$03AF`: encounter configuration already accepted by the NMI-side latch.

They can differ while a page transition is deferred.

## Main-thread producer order

Static fixed-bank flow shows:

```text
$C2CE  map bank 0 / platform preparation
$C30A  JSR $B6D0         generic common edge producer
$C30D  map bank 1
$C312  JSR $969D
$C319  JSR $8000
           ...
           JSR $8927     scheduled special producer
...
$C322  platform damage
$C32A  player $AAE4
```

Therefore the clean-room producer phase runs `$B6D0` first and `$8927` second, both before the player update.

## Why staged and active descriptors stay separate

### Generic `$B6D0`

The generic producer sees both values. Its top gate is effectively:

```text
if $03B7 != 0 and $03B7 != $58:
    return
```

So a changed page descriptor that has not yet been accepted blocks generic spawning from the previous common encounter.

### Scheduled `$8927`

The scheduled producer tests the low nibble of active `$58` and does not compare `$03B7` with `$58`.

Therefore an old special encounter that remains active during a deferred page transition can still evaluate its camera schedule. This asymmetry is preserved deliberately; the clean implementation must not invent a shared mismatch gate for both producers.

## `PlatformLatchedCommonProducerPhase`

Inputs include:

- active `PlatformPrimaryEncounterLatchState` (`$58` + accepted profile);
- separately staged `$03B7`;
- common slot logical/visual state;
- shared generic cooldown `$03B8`;
- scheduled trigger latch `$03A2`;
- current camera/player/entropy data;
- scheduled-special entries for the current substate.

Execution:

1. if there is no active semantic encounter (`$58=0`), return a contained no-op;
2. run generic `$B6D0` with staged `$03B7` and active `$58/profile`;
3. propagate its slot/cooldown mutations;
4. run scheduled `$8927` with the resulting slot state and the same active `$58/profile`;
5. propagate `$03A2` and slot mutations.

## Discriminating cases covered by tests

### Deferred common -> common page change

```text
active $58 = $85
staged $03B7 = $96
```

`$B6D0` returns at its descriptor mismatch gate; cooldown remains unchanged.

### Deferred special -> another page

```text
active $58 = $A8
staged $03B7 = $96
```

`$B6D0` is blocked by mismatch, but later `$8927` can still spawn type `$08` if its camera schedule matches because it consumes active `$58`, not staged `$03B7`.

### Accepted common descriptor

```text
active $58 = $86
staged $03B7 = $86
```

The mismatch gate is gone and generic spawning proceeds normally, including shared `$03B8` slot ordering.

## Thread boundary

This class is intentionally main-thread-only. NMI-side refresh/acceptance is modeled separately in `PlatformNmiPrimaryEncounterRefreshPhase`. A higher-level scheduler may pass the latch state produced by the appropriate NMI boundary into this producer phase, but should not collapse their execution contexts into an unordered method.
