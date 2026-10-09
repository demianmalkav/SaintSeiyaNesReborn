# Common-slot activity gate `$A459-$A47A`

Status: **CONFIRMED by static ROM flow**.

Both common logical records enter the bank-3 `$A442` entity dispatcher through the same gate at `$A459`. The visual record's byte `+1` is the normal activity marker, but a free visual slot is not always equivalent to an inactive logical record.

## Exact rule

```text
visual +1 != $FE
    -> process slot

visual +1 == $FE
    -> inspect logical action family
       $40 -> process slot
       $D0 -> process slot
       else -> RTS / skip slot
```

The `$40` and `$D0` exceptions allow hit-reaction and death cleanup to continue after the visual slot has already been retired.

## Why this matters

A clean-room engine cannot use only one of these shortcuts:

- `visual == $FE => inactive`; or
- `logical action != 0 => active`.

Both are wrong for the original.

The correct slot state is a small product of visual occupancy and logical family. This also matches the primary encounter safe-acceptance latch: `$996C` requires visual `$FE` **and** explicitly rejects logical `$40/$D0` before allowing a new page encounter to replace active `$58`.

## Clean-room representation

`PlatformCommonSlotActivityGate` returns one of four outcomes:

- `VisualActive`;
- `VisualFreeButReactionContinues`;
- `VisualFreeButDeathContinues`;
- `VisualFreeInactive`.

This gate will be used when composing common spawners with the two-slot `$A442` runtime, allowing one slot to remain genuinely free while the other is newly spawned/active without inventing a dummy entity update.
