# Primary encounter refresh invocation gate `$D7F2/$9915`

Status: **CONFIRMED by static ROM flow**.

`PlatformPrimaryEncounterAcceptance` models what happens once bank-1 `$996C` is reached. The engine does not reach `$996C` on every platform update, however. A fixed-bank wrapper and a bank-1 visual-state branch gate the acceptance attempt first.

## Fixed-bank wrapper `$D7F2-$D80B`

The wrapper begins with:

```text
LDA $44
AND #$06
BEQ $D80B
```

Therefore bank-1 `$9915` is called only when:

```text
($44 & $06) != 0
```

On the call path the wrapper clears `$03A3`, maps PRG bank 1, calls `$9915`, then restores bank 3.

When `($44 & $06)==0`, control takes the alternate `$D80B` path. That update does not attempt primary encounter acceptance through `$996C`.

## Bank-1 `$9915` entry gate

At `$9915`:

```text
LDA $07C0
CMP #$FE
BEQ $996C

LDA $03A4
CMP #$FF
BNE $996C

; otherwise special visual/CHR handling
; ...
RTS
```

So after the fixed wrapper calls bank 1, `$996C` is reached when either:

1. `$07C0 == $FE`; or
2. `$03A4 != $FF`.

The only suppressing combination on the called path is:

```text
$07C0 != $FE
AND
$03A4 == $FF
```

That combination enters a special visual/CHR branch and returns without running the primary encounter acceptance latch.

## Four semantic outcomes

`PlatformPrimaryEncounterRefreshGate` exposes:

- `AlignedScrollAlternatePath`: `($44&$06)==0`; bank 1 is not called;
- `AcceptanceEligibleVisualFree`: bank 1 called and `$07C0==$FE`;
- `AcceptanceEligibleStateA4`: visual marker occupied but `$03A4!=$FF`;
- `SpecialVisualBranchSuppressesAcceptance`: bank 1 called, but `$07C0!=$FE && $03A4==$FF`, so `$996C` is skipped.

This distinction matters in a clean-room frame model. “The page descriptor was not accepted this frame” can mean either:

- `$996C` ran and deliberately deferred it because common slots were unsafe; or
- `$996C` was never invoked at all.

Those states must not be collapsed if frame-exact encounter transitions are desired.

## Next integration

The next composed layer is:

```text
refresh gate
  -> if eligible: PlatformPrimaryEncounterAcceptance
  -> accepted/current active config
  -> common producer phase
```

The producer phase itself is known to occur before the player update: bank 0 `$B6D0` runs first, then bank 1 `$8000` includes scheduled producer `$8927`, then platform damage and bank-3 `$AAE4` player processing occur.
