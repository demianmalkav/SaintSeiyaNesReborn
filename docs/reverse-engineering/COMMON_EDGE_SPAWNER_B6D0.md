# Common edge spawner `$B6D0-$B7FC`

Status: **CONFIRMED by static ROM flow** for the producer semantics described here.

This bank-0 producer complements the table-driven bank-1 `$8925` spawner. It targets the same logical records (`$03BA/$03CA`) and visual records (`$0748/$077C`) but explicitly excludes low-nibble types `$08/$09/$0C/$0D/$0E`.

## Pair gate and ordering

`$03B7` is checked before either slot:

```text
if $03B7 != 0 and $03B7 != $58: return
```

Slot A is always attempted first. Slot B is attempted only when bit 7 of `$58` is set.

Both attempts share cooldown `$03B8`. This creates observable same-frame ordering:

- cooldown `1`: A decrements `1->0`; B may then spawn in the same frame;
- A successful spawn seeds `$03B8=$30`; if B is enabled, B immediately sees it, decrements `$30->$2F`, and returns;
- a ground-search failure still seeds `$03B8=$30` before failing, so a following B attempt can also decrement it.

## Per-slot early gates

The current slot returns before spawn work if:

1. logical action family is `$D0` or `$40`;
2. visual sprite byte `+1` is not `$FE`;
3. low nibble of `$58` is `$08/$09/$0C/$0D/$0E`;
4. shared `$03B8` is nonzero (it is decremented once).

If all gates pass, `$03B8` is seeded to `$30` before positioning.

## Ordinary positioning

For every type except `$0F`:

- if `$43 != 0`, spawn from the right: `X=$F8`, `+$07=$01`;
- otherwise `$48 & $08` chooses side:
  - clear: `X=$00`, `+$07=$41`;
  - set: `X=$F8`, `+$07=$01`.
- initial Y is `$20`.

The routine samples the logical descriptor beneath the entity at `(worldX+8, Y+$20)`. If descriptor `<$A8`, Y advances by `$10` and sampling repeats. Candidate Y values continue through `$80`; if the next candidate would be `>=$81`, the spawn is rejected.

Therefore the search can sample Y `$20,$30,$40,$50,$60,$70,$80` (seven samples). A rejected search leaves the visual slot free but preserves the partial logical X/Y/ground writes and the already-seeded cooldown.

## Type `$0F`

Type `$0F` bypasses the ground search entirely:

- `$48 & $08 == 0`: `X=$00`, `Y=$00`, `+$07=$41`;
- `$48 & $08 != 0`: `X=playerX+$78` (8-bit wrap), `Y=$00`, `+$07=$01`.

Logical `+$05` is not overwritten on this route.

## Final initialization

On successful spawn:

```text
+$00 = $10
+$03 = $00
+$04 = $00
+$06 = ($48 & $3F) + $1F     ; 31..94
+$09 = ($58 & $0F)
+$0C = $03AE
+$0D = $03AD
+$0E = $03AC
+$0F = $03AF
visual +$01 = $FD
```

For non-`$0F` types, logical `+$05` contains the accepted ground descriptor from the vertical search.

## Clean-room implementation

`PlatformCommonEdgeSpawner` models:

- the `$03B7` pair gate;
- exact A->B shared-cooldown ordering;
- edge selection;
- type `$0F` special placement;
- descriptor search against `PlatformStageMap`;
- partial-state preservation on failure;
- final profile initialization.

This producer and `$8925` together explain two distinct ways the game populates the shared `$03BA/$03CA` records. Other scripted/event-specific writers still exist and remain separate reverse-engineering targets.
