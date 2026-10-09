# Platform post-player step — `$B94B` timer boundary

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the `$76` side effect at the entry of bank-3 `$B94B` is statically reconstructed and composed as a separate clean-room timer step. The remainder of `$B94B` is predominantly player animation/sprite-selection work and remains outside this semantic helper until its effects are classified.

## Position in the active frame

The fixed-bank active platform path executes:

```text
... exit/support work ...
map PRG bank 3
JSR $AAE4   ; player/action dispatcher
JSR $B94B   ; post-player animation/timer work
JSR $C2D7
JSR $CECC
JSR $C2EC
...
INC $3C     ; only later at $C402
```

Therefore `$B94B` observes player state **after** the current `$AAE4` action step but while frame counter `$3C` still contains the current frame's old value.

## `$76` countdown at `$B94B`

At `$B94B` the routine first copies several player/action values into working bytes for sprite/action processing. The relevant timer fragment is:

```text
LDX $76
BEQ timer_done
DEX
STX $76
...
```

Thus:

- `$76 == 0` remains zero;
- every nonzero `$76` that reaches `$B94B` is decremented exactly once;
- this decrement happens **after** `$AAE4` has already made its decisions for the frame.

The remainder of the nonzero branch also participates in flashing/animation selection using `$3C`, but that render-side behavior is deliberately not folded into the timer helper.

## Critical `$80` two-frame consequence

The `$80-$8F` branch in `$AAE4` checks `$76` before `$B94B` runs:

```text
if (($4E & $F0) == $80) {
    if ($76 != 0)
        return;
    ... exceptional reload ...
}
```

Therefore a frame entering with:

```text
$4E = $80
$76 = 1
```

behaves as follows:

### Frame N

1. `$AAE4` sees `$76=1` and takes `Damage80Waiting`;
2. input, movement and B attack logic are skipped;
3. control returns to the normal platform path;
4. `$B94B` runs and decrements `$76: 1 -> 0`;
5. the remainder of frame N continues normally.

### Frame N+1

1. frame-start action remains in family `$80`;
2. `$AAE4` now sees `$76=0`;
3. it enters the exceptional resource-snapshot/reinitialization path;
4. execution resets the stack and jumps to `$C180`;
5. `$B94B` is **not reached** on this frame.

This is a one-frame ordering rule. Decrementing `$76` before `$AAE4` would trigger reload one frame too early.

## Contact/hazard consequence

`$76` is shared by ordinary contact and special hazard paths. Ordinary entity contact can set `$76=$20`; lower-screen hazard/fall-out can seed other values such as `$80`.

The clean-room architecture should therefore keep the concepts separate:

- the semantic event that creates/refreshes a contact or hazard latch;
- the post-player once-per-frame countdown;
- the `$80` dispatcher gate that observes the value before countdown.

This reproduces original timing without perpetuating the 1988 RAM aliasing as a modern domain model.

## Executable model

`PlatformPostPlayerLatch` models only the confirmed `$76` side effect:

- `Step(value, playerLoopExited)` returns the exact countdown result;
- `Apply(dispatchResult)` composes it after `PlatformPlayerActionDispatcher`;
- if the dispatcher already took `Damage80Reload`, the helper explicitly skips the countdown because the original never reaches `$B94B`.

Self-tests include the exact `$80/$76=1` two-frame transition.

No ROM payload is embedded.
