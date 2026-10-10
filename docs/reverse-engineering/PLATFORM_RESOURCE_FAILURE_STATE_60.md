# Platform resource failure — engine state `$60` and reload mode `$FF`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: fatal active-platform Life/Cosmo exhaustion is closed from bank-1 resource underflow through engine state `$60`, mode `$04=$FF`, and the already-promoted terminal-`$FF` reload destination machinery.

## Entry from active platform state `$20`

Fixed state `$20` calls bank 1 `$8000`. Its resource order is:

```text
$8012 JSR $927A   ; Life
$8015 JSR $930A   ; Cosmo
```

Life is therefore processed before Cosmo, and Cosmo still runs if Life has already committed failure.

### Life `$927A-$9309`

Normal pending Life drain consumes one `$7F` tick and subtracts 2 semantic Life points.

Substate `$02=$10` has an additional periodic route that reaches the same subtract-two body without consuming `$7F`:

- internal Shun index `1`: every frame where `$3C & $07 == 0`;
- every other Saint: every frame where `$3C & $1F == 0`.

The packed-BCD borrow chain proves the fatal boundary:

```text
Life >= 2 -> subtract 2 and return normally
Life 0 or 1 -> underflow -> clear active Life pair -> failure
```

Thus Life exactly `2` becomes `0` without entering `$60`; a later subtract-two attempt is what becomes fatal.

Fatal Life converges at:

```text
$92DD clear active Life pair
$92E3 LDA #$60
$92E5 STA $00
$92E7 STA $01
$92E9 LDA #$D0
$92EB STA $4D
```

The remainder of `$92ED-$9309` is presentation setup.

### Cosmo `$930A-$935F`

If `$80!=0`, one pending Cosmo tick is consumed and 1 semantic Cosmo point is subtracted.

The exact fatal boundary is:

```text
Cosmo >= 1 -> subtract 1 and return normally
Cosmo == 0 -> subtract-one underflow -> clear active Cosmo pair -> failure
```

Fatal Cosmo reaches `$935F JMP $92E3`, so both resources share the exact `$60/$D0` failure commit.

Because `$930A` follows `$927A` unconditionally, a frame that already killed Life can still consume/decrement Cosmo afterward.

## Reachable `$60-$6F` set

The top-level dispatcher routes any high-nibble `$60` state to `$C364`, but the fatal platform subgraph produces only literal `$60`.

No producer for `$61-$6F` exists in the bounded entry graph. More importantly, the reachable `$60` main/NMI bodies contain no engine-state increment/write that can advance `$60` to those values.

Therefore, for this boundary:

```text
reachable family set = { $60 }
```

`$61-$6F` remain structurally dispatchable values, not reachable failure states.

## Main state `$60`: `$C364-$C3AB`

The logical failure timer is `$4D`; `$4E` is synchronized to it on every non-terminal frame.

Entry seeds:

```text
$4D=$D0
```

On each state-$60 frame:

```text
if ($3C & $0F) == 0:
    X = $4D + 1
else:
    X = $4D
```

If `X < $E0`, `$C38E/$C390` store it to both `$4D/$4E` and the failure presentation continues.

If the increment reaches `$E0`:

```text
$C384 INX
$C385 CPX #$E0
$C387 BCC $C38E
$C389 LDA #$FF
$C38B JMP $C2BA
```

The incremented `$E0` is **not** stored; `$4D/$4E` retain the prior `$DF` value on the terminal frame.

`$C364-$C37A` can also increment `$40` on even frames while `$40<$A0` when `$4F` is outside `$80-$EF`. This is presentation/animation state and does not select the engine destination.

The later bank-1 helper `$95FB/$963F` cannot reset the failure timer: high-nibble `$D0` and `$E0` are explicit preserve cases in that helper. The other bounded state-$60 callees contain no `$00/$01/$04` writer.

## NMI state `$60`

NMI dispatch:

```text
$D2EC CMP #$60
$D2F0 LDA #$01
$D2F2 JSR $C0B4
$D2F5 JSR $9D69
```

Bank-1 `$9D69+` rotates `$73` and redraws active Cosmo/Life/Seventh-Sense values. Its bounded body writes PPU/display fields but does not write `$00`, `$01`, or `$04`.

Global-state progression is therefore main-owned for this family.

## Terminal handoff: `$60 -> $3D`, mode `$04=$FF`

At the `$E0` timer gate, A contains `$FF` and `$C2BA` performs the common reload handoff:

```text
$04=$FF
refresh Saint snapshot via $CA94 / bank-1 $951F
$00=$3D
$01=$3D
JMP $E100
```

This is distinct from the previously closed normal `$04=$00` and narrative `$04=$8F` reload modes.

## Mode `$FF` prelude in bank 5 `$970A`

`$E257` sees `$04=$FF` and jumps directly to `$E3ED`, which switches to bank 5 and calls `$970A`.

The `$04=$FF` branch first forces:

```text
$0670=$FF
$F0=$FF
```

and jumps to `$9780`.

### Mark defeated Saint

`$9780-$9789` converts canonical `$0533` into the bit table `$FFC0`:

```text
canonical 0 -> $01
canonical 1 -> $02
canonical 2 -> $04
canonical 3 -> $08
canonical 4 -> $10
```

and ORs that bit into `$0673`.

Only the low nibble participates in the later four-persistent-Saint completion gate. Ikki therefore contributes `$10` but does not by himself satisfy a missing low-nibble bit.

### Stage `$05` exception

If `$050E=$05`, `$97B8+` records:

```text
$067E=$050E
$0525=0
```

runs the stage-local failure presentation, then `$97D3-$97D8` restores `$0670=$FF` and clears `$04=0` before returning.

This does not create a different stable engine destination.

### Stage `$0A` / `$06B8` exception

If `$050E=$0A` and `$06B8!=0`:

```text
$067D=$02
FB9F(target canonical Seiya 0, source current Saint)
$0533=$00
```

The mode remains `$04=$FF`. This redirects the active working Saint/progression before common terminal-`$FF` destination logic.

All other `$04=$FF` paths save/reload the same current Saint through `$FB9F` and return with `$0670=$FF`.

## Stable destination composition

After `$970A`, control returns to the already-promoted terminal-`$FF` branch modeled by `PlatformNormalWarmReloadDestination`.

The death mark in `$0673` is therefore the new semantic input; destination rules are reused rather than duplicated.

### Story phase `$06CE!=0`

The fixed `$E168` gate checks story phase **before** the four-Saint completion mask. For the reachable nonzero Saga phase (`$050E=$0A`), failure reenters the already-promoted interactive selector instead of committing a stable engine state immediately.

This is a handoff to an existing closed boundary, not a new `$60` state.

### Story phase zero: four-Saint completion

With `$06CE=0`, `$E17B-$E185` tests:

```text
(($0673 | $06CC) & $0F) == $0F
```

After the `$970A` death-bit OR, a complete low nibble commits:

```text
stable engine state $90
$068F=$DD
```

This is the first stable destination for the all-four-persistent-Saints exhausted case.

### Story phase zero: incomplete mask

If the low nibble is not complete, terminal `$0670=$FF` follows the existing non-completion branch and commits stable engine state `$00`.

The existing resolver then:

- derives `$050E` from `$067D`;
- writes `$06CC` from the active canonical Saint mask;
- preserves `$06CD` because this is not a progression-advance release;
- applies the already-promoted `$0F->$0D` normalization where applicable;
- restores internal `$03` from canonical `$0533` at the common commit.

Thus the direct stable destination set of resource failure mode `$FF` is:

```text
{ $00, $90 }
```

with one already-known Saga-phase selector reentry boundary when `$06CE!=0`.

## Executable model

`PlatformResourceFailureTransition` exposes three bounded semantic layers:

1. `ApplyResourceFrame` — ordered Life/Cosmo drain and exact fatal thresholds;
2. `AdvanceFailureFrame` — state-$60 `$4D/$4E` timing and `$04=$FF/$3D` handoff;
3. `ResolveReloadFf` — bank-5 `$970A` defeat marking/stage exceptions composed into the existing terminal-`$FF` destination model.

The model deliberately does not emulate PPU, sound, metasprites or the already-promoted warm-reload selector.

## Closed boundary

```text
active platform $20
 -> fatal Life and/or Cosmo subtract
 -> $00/$01=$60, $4D=$D0
 -> only reachable failure state is $60
 -> $4D advances through $D0-$DF
 -> threshold -> $04=$FF, $00/$01=$3D
 -> $E100
 -> bank5 $970A marks defeated Saint / applies stage exception
 -> terminal-$FF common logic
 -> stable $00 or stable $90
    OR already-known Saga interactive-selector reentry when $06CE!=0
```

No renderer/audio internals are required to reproduce the logical transition graph.
