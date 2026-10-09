# Common entity attack family `$70`

Status: **CONFIRMED by static ROM flow** for the control ordering described here. Runtime implementation currently integrates the common type `$00-$07` side; the late counter helper is generic enough to expose the observed `$08/$09/$0C` terminal behavior for later special-type work.

## Entry / trigger evidence

In PRG bank 3, the pre-dispatch block around `$A520-$A556` can write `$70` to entity record offset `+$00`. The directly observed type branch includes `$06`, `$08`, `$09`, and `$0C`. If record offset `+$03` is nonzero, the same trigger instead sets `+$04=1`, calls `$A908`, and plays sound `$29`.

`$70` is therefore not treated as an ordinary walk state. During the main entity pass it still reaches `$A970` decision logic, but while it remains `$70` the horizontal path at `$A5D9-$A5F3` preserves entity X apart from the common `$43` camera subtraction. A decision-timer expiry can replace `$70` with `$31/$32`; in that case the newly created jump is consumed immediately in the same update.

## Ordering relative to collision

The essential ordering is:

```text
$70 pre-state
  -> $A970 decision
  -> optional $31/$32 + $C5E6 jump step
  -> movement / $43 / removal / proximity fall
  -> $9915 projectile -> entity
  -> $98BA entity -> player contact
  -> ...
  -> $A886 late $70 progression
```

This means the late `$70` counter must observe mutations produced by `$9915`. If a projectile changes the entity to `$40` or `$D0`, `$A886` no longer sees family `$70` and the attack counter does not advance.

## Late counter `$A886-$A8DB`

Progression occurs only when `($3C & 1) != 0`.

- `$70..$77`: increment by one on each odd `$3C` update.
- Reaching `$78` calls `$A908` and plays sound `$2A`, except for types `$08` and `$0C`.
- `$79..$7F`: continue incrementing on odd updates.
- Attempting to advance beyond `$7F`:
  - types `$08/$09/$0C`: call `$A908`, play `$2D`, then action becomes `$00`;
  - other types: action becomes `$10` with no terminal `$A908` call.

Type `$09` is deliberately *not* excluded from the `$78` midpoint call and is also in the terminal special set.

## `$A908` secondary-object templates

`$A908` uses `type-5` to index fixed-bank tables `$C0E3` (secondary object type) and `$C0EF` (vertical offset). The entries relevant to observed attack-capable types are:

| entity type | object type | Y offset |
| --- | ---: | ---: |
| `$05` | `$B2` | `+5` |
| `$06` | `$B0` | `+2` |
| `$08` | `$D2` | `+4` |
| `$09` | `$A1` | `-2` |
| `$0C` | `$8F` | `+5` |

Zero object-type table entries cause `$A908` to return without creating a secondary object. The helper also derives secondary-object facing/control and X placement from the parent entity. Full secondary-slot mutation remains a separate runtime target because `$96B4/$9761` have not yet been promoted into the composed frame.

## Clean-room representation

`PlatformCommonEntityAttack70` is intentionally split into:

- `PrepareCommon(...)`: pre-interaction decision/movement/removal/proximity behavior for common types `$00-$07`;
- `AdvanceAfterInteraction(...)`: late `$A886` progression, spawn-call metadata, and sounds.

`PlatformTwoCommonEntityCombatSlice` invokes the second phase only after `$9915/$98BA`, preserving interruption semantics. Spawn attempts are surfaced as semantic events/templates; the secondary object buffer itself will be integrated when the `$96B4/$9761` pipelines are reconstructed.
