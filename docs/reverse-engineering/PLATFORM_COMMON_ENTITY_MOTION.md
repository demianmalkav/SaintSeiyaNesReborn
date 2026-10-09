# Common platform entity motion — executable spec

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the closed movement primitives used by the common movable-entity pipeline `$A442` are now represented in C# OriginalSpec. The higher-level stochastic chase/jump decision dispatcher remains separate until every branch of `$A999+` is promoted with the same precision.

## Horizontal cadence — `$A60B`

Ordinary entity types use:

```text
frame_counter & 3 == 0 -> 2 px
otherwise              -> 1 px
```

Type family `(type & $FE) == $0A` instead uses:

```text
step = frame_counter & 1
```

so types `$0A/$0B` alternate 0/1 px.

The directional jump states confirmed in the common path are:

- `$31`: `+step` X;
- `$32`: `-step` X.

Other `$3x` values do not receive a common horizontal jump delta from this primitive.

## Decision timer — `$AA64`

The common mobile decision timer reseeds as:

```text
decision_timer = ($48 & $3F) + $1F
```

Range: 31..94 inclusive.

## Terrain-facing turn — `$AA1F+`

Facing bit `$40` set means right.

Right-facing turn descriptors:

- `$80-$87`;
- `$E0-$EF`.

Left-facing turn descriptors:

- `$88-$8F`;
- `$E0-$EF`.

Descriptors `$F0+` do not use this turn response.

Type `$07` has an additional early-out for `$E4+`, so that type does not follow the ordinary response there.

Turning is exactly:

```text
flags_facing ^= $40
```

## Entity jump vertical phase — fixed `$C5E6`

`state_phase == 0` means no jump vertical update.

For nonzero phase:

1. if current phase `>= $10`, call landing helper `$C491` **before** incrementing phase;
2. if landing succeeds, return immediately;
3. increment phase;
4. if new phase `>= $20`, apply fixed `Y += 3`;
5. otherwise consume signed source table index `new_phase - 2` and subtract that byte from Y.

This is the same important phase/index pattern already found in the player jump system.

### Effective table entries

Fixed `$C639` physically contains 31 signed bytes, but `$C5E6` can consume only indices `0..29`. The final physical byte is unreachable because new phase `$20` takes the fixed-fall branch.

The 30 effective source values are:

```text
+8,+8,+7,+7,+6,+5,+4,+3,+3,+2,+2,+1,+1,+1,
 0, 0, 0, 0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-3,-3,-3
```

The ROM subtracts these bytes from screen Y, so positive values rise and negative values fall.

Metrics:

- effective table updates: 30;
- maximum ascent: 58 px;
- first maximum ascent: update 14;
- net position after the 30 table updates: 35 px above takeoff;
- unresolved post-table fall: +3 px/update.

## Landing helper — fixed `$C491`

The exact entity landing gate is narrower than the player's general floor rules.

Landing is ignored when:

```text
entity_y >= $86
```

or:

```text
ground_descriptor < $A8
```

For `$A8-$EF`:

- accept only when `(Y & $0F) < 6`;
- snap to `Y & $F0`.

For `$F0+`:

- accept only when `(Y & $0F) >= 8`;
- snap to `(Y & $F0) | 8`.

On accepted landing:

- `state_phase = 0`;
- types `$08`, `$09`, `$0C` -> action `$00`;
- all other types -> action `$10`.

## Clean-room implementation

`PlatformCommonEntityMotion` now provides:

- horizontal step cadence;
- decision-timer reseed;
- facing toggle and terrain-turn predicate;
- directional jump horizontal delta;
- exact effective jump table and metrics;
- exact `$C5E6` phase progression;
- exact `$C491` landing resolution.

The class deliberately does not yet claim the entire `$A442` state machine. In particular, the relative-position decision logic at `$A999+`, entity-record iteration and contact/projectile collision are still separate composition targets.

No ROM payload is embedded.
