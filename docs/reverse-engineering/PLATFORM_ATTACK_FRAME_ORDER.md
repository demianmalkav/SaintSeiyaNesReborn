# Platform attack frame ordering — `$926C`, `$BBCA`, `$A22C`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the relative frame positions of attack busy update, attack creation and attack-object update are statically closed and represented in clean-room composition code.

## Fixed-bank active-frame skeleton

The relevant active-platform order is:

```text
map bank 1
JSR $8000
  ...
  JSR $926C       ; advance attack busy $4B
  ...

map bank 3
JSR $AAE4         ; player/action
  ...
  JSR $BBCA       ; B input / attack creation on applicable branches

JSR $B94B         ; post-player animation + $76 countdown
JSR $C2D7
  map bank 3
  JSR $9B93       ; special/event object path
  JSR $96B4       ; secondary spawn/allocation
  JSR $9761       ; secondary-object update
  JSR $A442       ; common movable entities
  JSR $A22C       ; player attack/projectile update

... later ...
INC $3C           ; fixed $C402
```

This proves that `$4B`, attack creation and projectile motion belong to three distinct moments in the frame.

## 1. Busy counter `$4B` — before player input

Bank-1 `$8000` calls `$926C` before the engine reaches `$AAE4`.

`$926C` advances:

```text
0 -> 0
1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 0
```

Consequences:

- a frame entering with `$4B=7` reaches `$AAE4` with `$4B=0`;
- if B is newly pressed and its latch is clear, attack creation is allowed on that same frame;
- successful `$BBCA` creation then sets `$4B=1` again;
- `$A22C` later in the frame does not advance `$4B`.

The busy cadence therefore must not be implemented as an end-of-frame cooldown tick.

## 2. Attack creation `$BBCA` — player phase

Applicable `$AAE4` branches call `$BBCA` before leaving player simulation.

Creation sets the original projectile/chain origin, range and busy state. The existing attack model already preserves B-latch ordering and the old frame-start `$4E` origin rule.

## 3. Attack-object update `$A22C` — later in the same frame

Fixed `$C2D7` calls `$A22C` only after `$B94B` and after the special/secondary/common entity update calls.

Crucially, `$C402` has not incremented `$3C` yet. `$A22C` therefore uses the **same current frame-counter value** that was visible to player simulation.

### Newly created generic projectile

Example: Seiya, right-facing, player X `$40`, Cosmo 100.

`$BBCA` creates:

- X `$52` (`player_x + $12`);
- range 3;
- type family `$64`.

Later on the same frame `$A22C`:

- decrements range `3 -> 2`;
- moves X `+5`: `$52 -> $57`;
- writes auxiliary X `$4F`;
- chooses visible type `$64 + ($3C & 1)`.

So the projectile's first visible/update state is already one motion step beyond its creation origin.

### Newly created Shun chain

At the lowest Cosmo bracket, Shun creation seeds:

- slot 0 type `$54`;
- range 1;
- extension `$0391=5`.

The same-frame `$A22C`/`$A311` update then:

- decrements range `1 -> 0`;
- increases extension `5 -> 10`;
- synthesizes near segment `$55`;
- rebuilds both segment coordinates around the player's current position.

Retraction begins on the following update because the range has reached zero.

## `$80` interaction

The two `$80` subpaths differ sharply.

### Waiting (`$76 != 0`)

`$AAE4` returns to the normal platform frame. It skips player B/movement, but the rest of the frame still runs:

- `$B94B` decrements `$76`;
- `$C2D7` is reached;
- existing attack objects continue through `$A22C`.

Thus player control can be frozen while an already-fired projectile continues moving.

### Reload (`$76 == 0`)

`$AAE4` resets the stack and jumps to `$C180`. It never returns to `$B94B` or `$C2D7`.

Therefore existing attack objects are **not** advanced by `$A22C` on the reload frame.

## Clean-room implementation

`PlatformAttackFramePhases` intentionally models only the two attack-related phase boundaries already closed:

- `AdvanceBusyBeforePlayer` — semantic `$926C` slice;
- `UpdateObjectsAfterPlayer` — semantic `$A22C` slice.

The special-event, secondary-object and common-entity calls between `$B94B` and `$A22C` remain separate systems. Their absence from this helper is deliberate; attack timing can be tested without pretending the complete object pipeline is already composed.

No ROM payload is embedded.
