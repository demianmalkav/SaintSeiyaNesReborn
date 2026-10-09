# Primary entity removal helper `$A647`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: **CONFIRMED by direct static ROM flow** and promoted into the hybrid slot runtime.

## Entry and slot pointers

Bank 3 `$A442` dispatches the two primary logical records with paired visual bases:

| slot | logical base | visual base |
|---|---:|---:|
| A | `$03BA` | `$0748` |
| B | `$03CA` | `$077C` |

`$A459` stores the current logical pointer in `$16/$17` and the current visual pointer in `$18/$19`. The visual activity test reads `(visual + 1)` and treats `$FE` as free, with the already-confirmed `$40/$D0` logical exceptions.

## Exact `$A647` behavior

The removal helper first checks engine state `$00`:

```text
if $00 < $30:
    logical +$00 = $00
```

At `$00 >= $30` that logical clear is skipped.

The helper then retires eleven 4-byte visual records rooted at the active slot visual base. For each record it writes:

```text
visual + 0 = $F0
visual + 1 = $FE
```

and advances by four bytes, covering relative bases:

```text
+$00, +$04, +$08, +$0C, +$10, +$14,
+$18, +$1C, +$20, +$24, +$28
```

For logical type `$0D`, `$A66E-$A679` additionally retires the visual record at `+$2C/+2D` with the same `$F0/$FE` pair.

The clean-room primary-slot runtime currently tracks only the visual `+1` occupancy byte, not the full renderer-owned visual array. `PlatformEntityRemovalA647` therefore mutates the tracked occupancy to `$FE` and exposes the eleven-record/type-`$0D` details as evidence metadata.

## Why `$00 >= $30` matters

The conditional logical clear explains why the `$A459` activity gate cannot simply equate visual `$FE` with an inactive logical record.

A removal can produce:

```text
visual +1 = $FE
logical family still $40 or $D0
```

when `$00 >= $30`. On a later frame `$A459` explicitly admits those two families so reaction/death cleanup may continue despite the visual slot already being free.

At `$00 < $30`, the same helper clears logical `+$00`, leaving a conventional free/inactive slot that the next `$A459` pass skips.

## Confirmed call sites in the promoted `$A442` region

Static bank-3 flow reaches `$A647` from the currently modeled entity runtime through:

- `$A5A3`: lower-band removal from the `$50/$E0` path;
- `$A643`: horizontal removal after shared camera correction;
- `$A686`: shared vertical-band `$B0-$BF` removal;
- `$A759/$A77D`: additional type-specific movement/removal branches outside the currently promoted common + `$08/$09/$0C` set;
- `$A831`: terminal `$D0-$DF` death completion;
- `$A882`: type `$0D` terminal branch.

The hybrid scheduler applies the helper only when its already-promoted common/special runtime reports a path corresponding to one of these confirmed removal entries.

## Cross-frame invariant now closed

The composed persistent-slot rule is now:

```text
frame N logical runtime
    -> confirmed removal outcome
    -> A647 retirement
    -> persistent {logical state, visual +1}

frame N+1
    -> A459 activity gate consumes that state directly
```

No external/manual repair of `VisualSpritePlus1` is required.

Regression fixtures cover:

- `$00=$2F` clearing logical action;
- `$00=$30` preserving logical action while retiring visual occupancy;
- visual-free `$40` continuation on a following frame;
- special `$08` `$50` removal through the same helper;
- direct reuse of a freed slot by the confirmed scheduled special spawner (`$FE -> $FD`).

## Scope boundary

This closes the **primary occupancy/removal lifecycle** required by the current hybrid scheduler. It does not claim that the runtime models every tile/Y/attribute/X write performed by the renderer or every entity class in the game.

The attached `$A908/$AA70` object already has its own independently confirmed deactivation writes and remains a separate visual record/state object.
