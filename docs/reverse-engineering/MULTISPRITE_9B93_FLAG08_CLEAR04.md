# `$9B93` multisprite class — flag `$08`, flag `$04` clear

Status: **CONFIRMED by static ROM flow** for `$9F79-$A041`, excluding logical `$D0/$E0` and special substate `$0D`.

This is the second half of the flag-`$08` active dispatcher. It differs materially from the flag-`$04`-set route: there is no player-projectile check, movement happens before player contact, and the four visual pieces can receive different vertical/horizontal displacements during the same update.

## Phase and vertical source

The route first increments logical phase `+$03`:

```text
nextPhase = byte(phase + 1)
index = byte(nextPhase - 2)
```

If `index < $20`, the raw displacement byte is read from fixed `$C639+index`; otherwise it becomes `$FE`.

This branch can access all 32 bytes `$C639-$C658`:

```text
08 08 07 07 06 05 04 03
03 02 02 01 01 01 00 00
00 00 FF FF FF FF FE FE
FE FE FE FD FD FD FD FD
```

A phase starting at zero therefore increments to one, wraps `nextPhase-2` to `$FF`, and uses the `$FE` fallback.

## Sprite assignment and shared-curve mutation

Normal substates start the per-part sprite counter at `$B8`; substate `$10` starts it at `$DA`.

Only active visual parts execute the motion body. For an active part whose assigned sprite value is odd, `$9FB1-$9FBE` arithmetic-shifts the shared curve byte right by one. The mutated value remains in zero-page `$31` and is inherited by later parts.

For the canonical `$B8-$BB` sequence with initial curve 8:

```text
part0 $B8: curve 8
part1 $B9: curve 4
part2 $BA: curve 4
part3 $BB: curve 2
```

Empty parts skip this shift, so the curve evolution also depends on which parts are still alive.

Each active part then performs raw 8-bit:

```text
Y -= curveByte
```

If the result is `>= $A8`, that part is retired individually (`Y=$F0`, sprite=`$FE`) before its X update continues.

## Horizontal code/data indexing

Horizontal offset is obtained by:

```text
Xindex = assignedSprite - $B8
offset = ROM[$9F75 + Xindex]
```

For the normal `$B8-$BB` sprites this is the explicit four-byte table:

```text
$B8 -> $FF = -1
$B9 -> $01 = +1
$BA -> $FE = -2
$BB -> $02 = +2
```

The substate `$10` base sprite `$DA` gives index `$22`, landing at ROM address **`$9F97`**. That byte is `$F0`, even though `$9F97` is simultaneously executable branch code. Therefore the canonical `$DA` part receives horizontal offset **-16**. This is a real code-as-data overlap produced by the 6502 indexing and is preserved in OriginalSpec.

For completeness, if later `$10` parts were active, the next literal ROM bytes are `$04/$A9/$B8` for `$DB/$DC/$DD`. The canonical bootstrap creates only part0 in `$10`, so those continuation cases are defensive parity rather than evidence that the game normally activates them.

After the offset:

```text
X += signedOffset
X -= $43
```

If post-step X is `<5`, the individual part is retired.

## All-empty cleanup

After all four iterations, `$A00F` tests all sprite bytes. If all are `$FE`, it clears logical phase `+$03` and normalizes the visual records to the empty sentinel form.

## Contact occurs after movement

Control then jumps to `$A039`, which calls `$A042` exactly three times. `$A042` uses auxiliary-sized contact geometry `(4,4,3,3)` and synchronizes the current visual part into logical X/Y before `$98BA`.

Therefore this route's player contact sees **post-movement** coordinates, unlike the flag-`$04`-set route where contact precedes its visual motion.

### Pointer-stall quirk

`$A042` advances visual pointer `$12` by four bytes only after processing an active part. If the current part's sprite is `$FE`, it returns immediately without advancing the pointer.

Consequently, if the first part has retired but a later part is still active, the three `$A042` calls can all re-check the same empty first record and never reach the later active part. OriginalSpec preserves this rather than iterating naïvely over the first three array elements.

## No projectile route

There is no `$9915` call on this branch. Player attack objects are unaffected by this multisprite class while it is in flag-`$08` / clear-`$04` mode.

## Clean-room representation

`PlatformMultisprite9B93Flag08Clear04` models the full closed path: phase source, shared arithmetic curve mutation, code/data horizontal offsets, individual retirement, all-empty cleanup and the three post-movement contact calls with pointer-stall semantics.
