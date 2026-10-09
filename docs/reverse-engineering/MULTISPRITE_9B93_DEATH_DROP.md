# `$9B93` multisprite class — logical `$D0/$E0` path

Status: **CONFIRMED by static ROM flow** for `$A06E-$A0DF`.

When the multisprite logical action at `$03FB+0` is in family `$D0` or `$E0`, both the normal and flag-`$08` dispatchers divert to `$A06E` before ordinary contact/projectile handling. This route is therefore collision-free.

Per update:

```text
action += 1
if action >= $F0: clear class and phase, return
choose sprite from action/substate
phase += 1
deltaY = phase >> 3
for all four visual parts:
    Y += deltaY
    sprite = selected sprite
    high flag bits = 00/40/80/C0 by part index
    X -= 2
    X -= $43
if part0 Y >= $A0: clear class and phase
```

Sprite selection is:

- normal substates: `$D6` while incremented action `<$E8`, then `$D7`;
- substate `$0C`: `$6B` while incremented action `<$E8`, then `$6A`.

The orientation table at `$A0E0` supplies high flag bits `00,40,80,C0`; existing low six bits are preserved.

Cleanup writes all four visual records to `Y=$F0, sprite=$FE` and clears logical phase `+$03`. The already incremented logical action remains intact.

`PlatformMultisprite9B93DeathDrop` is the clean-room representation of this terminal route. It deliberately contains no contact or projectile API because the original reaches neither `$98BA` nor `$9915` here.
