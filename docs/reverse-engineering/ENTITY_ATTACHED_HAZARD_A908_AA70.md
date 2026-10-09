# Entity-attached hazard `$A908 / $AA70`

Status: **CONFIRMED by direct static disassembly of the canonical ROM**.

This subsystem is distinct from the independent `$07B0/$07B8` auxiliary hazards and from the `$9B93` multisprite class. It is rooted at raw visual offsets relative to the parent entity visual pointer `$18`.

## Spawn helper `$A908-$A96F`

The helper first checks visual byte `+$2D`:

```text
+$2D != $FE  -> return immediately
+$2D == $FE  -> slot is available
```

It indexes the already-identified fixed tables by `parentType - 5`:

| parent type | object byte `+$2D` | Y offset |
| --- | ---: | ---: |
| `$05` | `$B2` | `+5` |
| `$06` | `$B0` | `+2` |
| `$08` | `$D2` | `+4` |
| `$09` | `$A1` | `-2` |
| `$0C` | `$8F` | `+5` |

A zero object byte returns without creating anything.

For a real spawn:

```text
+$2D = object byte
+$2C = parentY + signed table offset
flags = (parent +$07 & $40) | $03
+$2E = flags
+$32 = flags
+$2F = parentX + (facing-right ? +9 : -9)
```

The routine also writes parent logical `+$08`:

```text
engine $00 == $20  -> parent +$08 = $03AB
otherwise          -> parent +$08 = $F0
```

The clean-room spawn result exposes that side effect separately because parent `+$08` is not yet part of `PlatformCommonEntityRuntimeState`.

## Contact helper `$AA70-$AAE5`

The collision routine uses attached coordinates `+$2C` (Y) and `+$2F` (X), with the reduced parameter set:

```text
$79 = 4
$7A = 4
$7B = 2
$7C = 2
```

It rejects contact when:

- player frame-start action family is `$40`;
- player Y is `>= $90`;
- the vertical or horizontal unsigned-byte windows do not overlap;
- `$76 != 0`.

### `$20` player-state exception

Unlike ordinary `$98BA`, `$AA70` has one additional rule. If frame-start `$4E` equals exactly `$20`, it subtracts `$10` from the vertical upper bound before comparing against player Y.

This rule is retained explicitly and is not approximated through the generic entity-contact helper.

## Successful contact

On contact:

```text
$76 = $20
sound = $26
parent +$0E -> $7F  (Life-drain ticks)
parent +$0D -> $80  (Cosmo-drain ticks)
```

Then the attached record is deactivated by writing:

```text
+$2C = $F0
+$2D = $FE
+$30 = $F0
+$31 = $FE
```

Other raw bytes are not written by `$AA70` and are preserved by the clean-room model.

## Why raw offset names are retained

`PlatformEntityAttachedHazardState` intentionally uses `Raw2C..Raw33` rather than assigning speculative render roles to every byte. `$A908` and `$AA70` prove which offsets are read/written, but the complete renderer semantics of the mirrored/second visual record have not yet been reconstructed.

This keeps the model exact without smuggling assumptions into ORIGINAL SPEC.

## Next integration boundary

This isolated subsystem is shared by:

- common `$70` attack-capable types such as `$05/$06`;
- scheduled special types `$08/$09/$0C` through their `$A908` trigger paths;
- later `$AA70` post-contact processing.

The next integration step is to thread this attached-hazard state through the special `08/09/0C` active runtime and through late `$70` spawn calls, while preserving the already-confirmed interaction ordering.
