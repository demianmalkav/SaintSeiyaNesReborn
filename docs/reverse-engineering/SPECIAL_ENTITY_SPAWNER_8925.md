# Table-driven special common-entity producer `$8925-$89D1`

Status: **CONFIRMED by static ROM flow** for the producer semantics described here.

This routine is distinct from the ordinary `$A442` updater. It is one of the producers that populates the two 16-byte logical records rooted at `$03BA` and `$03CA`.

## Gating

The low nibble of `$58` must be one of:

- `$08`
- `$09`
- `$0C`
- `$0D`
- `$0E`

Other values return without attempting a spawn.

The routine then attempts slot A first (`$03BA` logical / `$0748` visual) and slot B second (`$03CA` logical / `$077C` visual).

A slot is considered free only when visual record byte `+1` is `$FE`.

## Schedule lookup

`$02 * 2` indexes the pointer table at bank-1 `$89D2`. There are 18 substate pointers (`$00-$11`). Each pointed schedule is a sequence of four-byte entries terminated when entry byte 0 is `$FF`.

Per entry:

```text
+0 camera low trigger
+1 camera high trigger
+2 spawn Y
+3 reserved/unused by this routine
```

Matching is exact against:

```text
entry.high == $45
entry.low  == ($44 & $F8)
```

The canonical ROM contains 109 schedule entries across the 18 substates. The raw table contents are intentionally not committed; `tools/rom/extract_special_spawn_schedule.py` derives them from a user-supplied ROM.

## Duplicate latch `$03A2`

After a camera match, the routine compares only entry byte `+0` with `$03A2`.

If equal, the spawn is rejected even if the matching entry's high byte differs from the previous trigger. On successful spawn, `$03A2` becomes the current low trigger byte.

Because slot A is processed before slot B, a successful A spawn changes `$03A2` before B scans the same schedule. Therefore B normally rejects the same trigger in that frame. If A is occupied, B can consume the trigger.

## Record initialization

For a successful spawn the producer writes:

```text
logical +$00 = $00
logical +$01 = $F8
visual  +$01 = $FD
logical +$02 = entry spawn Y
logical +$03 = $00
logical +$04 = $00
logical +$07 = $01
logical +$09 = ($58 & $0F)
logical +$0C = $03AE
logical +$0D = $03AD
logical +$0E = $03AC
logical +$0F = $03AF
```

Notably, this path does **not** overwrite logical offsets `+$05`, `+$06`, `+$08`, `+$0A`, or `+$0B`. The clean-room model therefore preserves the corresponding state it already represents instead of zeroing those fields by assumption.

## Clean-room representation

`PlatformScheduledSpecialEntitySpawner` models both an individual attempt and the exact ordered A->B pair. Schedule entries are injected as derived data rather than embedded in source.

`tools/rom/extract_special_spawn_schedule.py` reads the pointer table and emits JSON from the canonical/user-supplied ROM. No ROM payload or raw schedule table is stored in the public repository.

## Remaining producer work

This closes the table-driven producer for the special `$08/$09/$0C/$0D/$0E` families. It does **not** yet identify every producer that creates ordinary `$00-$07` common enemies; those remain a separate reverse-engineering target.
