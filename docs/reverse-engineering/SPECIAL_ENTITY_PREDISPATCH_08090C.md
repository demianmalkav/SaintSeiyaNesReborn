# Special entity pre-dispatch — types `$08/$09/$0C`

Status: **CONFIRMED by static ROM flow** for bank-3 `$A478-$A55E`.

The scheduled producer `$8927` creates these special types with logical action `$00`, visual marker `$FD`, `+$03=0`, `+$04=0`, and a type-specific combat profile. They do not immediately behave like ordinary `$10` enemies. Before the shared active-family dispatcher at `$A55E`, they pass through a dedicated control layer.

## Entry routing

At `$A478`, types `$08`, `$09`, and `$0C` branch to `$A4A7`.

If logical `+$04 != 0`, control jumps directly to `$A55E`; no facing correction or `$039A` update occurs.

When `+$04==0`, action families `$40`, `$D0`, and `$E0` also bypass the special trigger logic and go directly to `$A55E`.

## Facing correction

For the remaining path, entity X is compared with player `$3F`:

- entity left of player -> ensure facing bit `$40` is set;
- entity at/right of player -> ensure facing bit `$40` is clear.

Thus the special actor faces toward the player before its trigger counter advances.

## Global trigger counter `$039A`

The routine increments `$039A` once.

If the incremented value is `<$80`, it stores the value and continues to `$A55E`.

At `>=$80`, types `$08/$09/$0C` take the branch at `$A540`.

### `+$03 == 0`

The logical action is set to exactly `$70`.

No immediate `$A908` call occurs. `$039A` is reset to:

```text
$48 & $3F
```

The later `$70` machinery then controls midpoint/terminal secondary-object generation.

### `+$03 != 0`

The routine sets logical `+$04=1`, immediately calls `$A908`, plays sound `$29`, and then resets `$039A` to `$48&$3F`.

The already reconstructed `$A908` templates are:

| type | object | Y offset |
|---|---:|---:|
| `$08` | `$D2` | `+4` |
| `$09` | `$A1` | `-2` |
| `$0C` | `$8F` | `+5` |

The action is not forcibly changed to `$70` on this immediate-spawn route.

## Clean-room representation

`PlatformSpecialEntityPreDispatch08090C` keeps logical `+$04` in a dedicated `PlatformSpecialEntityControlState` because the generic common motion record does not otherwise need that field.

It exposes:

- bypass by `+$04`;
- bypass by `$40/$D0/$E0` family;
- ordinary `$039A` increment;
- `$70` start;
- immediate `$A908` spawn request and sound `$29`.

This is the first runtime piece that lets an entity freshly produced by the scheduled `$8927` path advance from action `$00` toward its actual special behavior instead of being treated as a generic common enemy.

## Remaining special-type work

The active-family behavior after `$A55E` is still being promoted for these types. Types `$0D/$0E` take a different pre-dispatch route: they jump directly to `$A55E` and do not use this facing/`$039A` trigger block.
