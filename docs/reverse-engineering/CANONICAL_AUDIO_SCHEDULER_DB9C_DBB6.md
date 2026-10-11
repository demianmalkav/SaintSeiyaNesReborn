# Canonical audio scheduler — `$DB9C/$DBB6/$0440+`

Status: `CONFIRMED` for scheduler/reset/load/arbitration/APU ownership; original note, envelope, instrument and SFX payloads remain outside this checkpoint.

Canonical ROM: SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`.

## Boundary

The audio architecture is split between fixed-bank control code and a bank-0 per-frame engine:

- `$DB9C`: reset/initialization.
- `$DBB6`: cue-descriptor loader.
- `$8B50+` in bank 0: eight-slot per-frame scheduler, stream interpreter and APU routing.
- RAM records: eight records at `$0440 + $15*N`, `N=0..7`.

This checkpoint models scheduler semantics and APU ownership only. It does not publish original descriptor payloads, note streams, envelope tables, instrument data, music or SFX assets.

## Reset — `$DB9C`

`$DB9C` performs three state resets before walking the records:

- writes `0` to `$4015`;
- writes `0` to `$04F0`;
- writes `0` to `$04EF`;
- writes `$FF` to record `+0` for offsets `$00,$15,$2A,$3F,$54,$69,$7E,$93`.

Therefore record `+0 == $FF` is the canonical inactive state.

## Cue load — `$DBB6`

Input `A` is multiplied by four and added to `$DC0E`. Each descriptor is four bytes:

1. byte 0: scheduler record offset, one of the `$15`-byte slot offsets;
2. byte 1: copied to record `+1`;
3. byte 2: copied to record `+2`;
4. byte 3: copied to record `+3`.

After the copy, record `+0` becomes `0`, marking the slot as newly loaded for the next `$8B50` update.

The low two bits of record `+1` select one of four APU channel classes:

| value | class | APU base |
| --- | --- | --- |
| `0` | pulse 1 | `$4000` |
| `1` | pulse 2 | `$4004` |
| `2` | triangle | `$4008` |
| `3` | noise | `$400C` |

The corresponding enable bits are `$01,$02,$04,$08`.

### Preemption

If the target record was already active (`+0 != $FF`), `$DBB6` reads the old record `+1 & 3`, selects mask `$0E,$0D,$0B,$07` from `$DC0A`, ANDs it with `$04F0`, and stores the result back to `$04F0`.

Thus cue loading preempts the *old* channel owner represented by that record by clearing its channel bit in the software `$4015` shadow. `$DBB6` itself does not write `$4015`.

## `$04F0` ownership

`$04F0` is the canonical software shadow for the low four `$4015` channel-enable bits.

Owners:

- `$DB9C`: reset to zero.
- `$DBB6`: clears the previous channel bit when overwriting an active record.
- `$8DB0-$8DBC`: ORs the active channel bit into `$04F0` and writes the new shadow to `$4015`.
- `$8DFD-$8E09`: ANDs the channel-clear mask into `$04F0` and writes the result to `$4015`.

The enable table at `$8F42` is `01,02,04,08`; the clear table at `$8F46` is `0E,0D,0B,07`. `$DC0A` uses the same clear masks.

## `$04EF` ownership

`$04EF` is not an APU enable shadow.

The bank-0 stream interpreter recognizes command `$A5` at `$8D33`; its following operand is stored in `$04EF`. The fixed dispatcher at `$DB40` tests `$04EF`; when nonzero it invokes `$8904` and `$8AF1`, clears `$04EF`, then continues.

Those bank-0 consumers operate on visual buffers rather than APU registers. Therefore `$04EF` is a scheduler-originated cross-subsystem visual-reset/request latch, not an audio-channel state byte.

## Per-frame scheduler — `$8B50+`

The fixed frame service calls bank-0 `$8B50` once per relevant update.

At entry `$8B50`:

- clears `$04E8` (per-frame claimed-channel mask);
- increments `$04EE` modulo 256;
- iterates the eight `$15`-byte records in ascending slot order;
- derives channel `record[+1] & 3`;
- derives APU register-base offset `0,4,8,12`.

Record states observed directly:

- `+0 == $FF`: inactive, skipped;
- `+0 == 0`: newly loaded; initialization reads through the pointer in `+2/+3`, then moves the lifecycle/cursor to the initialized state;
- other values: active stream/cursor state.

The stream interpreter begins at `$8C0C`; exact note/envelope payload reconstruction is intentionally out of scope.

## Channel arbitration

`$8E0D` loads the channel bit and tests it against `$04E8`. If an earlier slot already claimed that channel, it discards the caller's return address and suppresses the later slot's channel work. After a slot is processed, its channel bit is ORed into `$04E8`.

Therefore, for two simultaneously active records targeting the same channel, the first active record in ascending slot order owns that channel for the frame.

This is scheduler priority, distinct from `$DBB6` record replacement.

## APU write ownership

Relevant executable APU ownership for this boundary is:

- `$DB9E`: reset write `$4015 = 0`.
- `$8DBC`: enable-shadow write to `$4015`.
- `$8E06`: disable-shadow write to `$4015`.
- `$8E4D`, `$8E9E`: indexed writes to `$4000 + {0,4,8,12}`.
- `$8DCD`: indexed writes to `$4001 + {0,4,8,12}`.
- `$8DD1`, `$8F0E`: indexed writes to `$4002 + {0,4,8,12}`.
- `$8DEA`: indexed writes to `$4003 + {0,4,8,12}`.

This covers pulse 1, pulse 2, triangle and noise register blocks. The apparent fixed-bank byte sequence near `$D591` is data, not an executable `$4001,X` owner. Cold RESET `$C12C/$C12F` separately disables DMC and all channels as hardware initialization, outside the per-frame scheduler contract.

No executable scheduler owner of `$4010-$4013` was found; DMC is not part of this scheduler boundary.

## Clean-room contract

`CanonicalAudioScheduler` models:

- `$DB9C` reset state;
- descriptor-addressed slot loading;
- active-slot replacement/preemption and `$04F0` shadow effects;
- `$04EF` request/consume semantics;
- per-frame channel arbitration;
- semantic routing to the four APU register blocks and `$4015`.

The caller supplies decoded semantic voice-frame operations. This is deliberate: original stream bytes, note tables, envelopes and audio payloads remain external. The model therefore captures the scheduler/voice/APU ownership chain without turning this checkpoint into soundtrack reconstruction.

## Reproducibility

Run:

```text
python tools/reverse/audit_canonical_audio_scheduler.py <canonical-rom>
```

The audit verifies the canonical ROM identity, structural range hashes, reset and loader instruction anchors, channel-bit/clear-mask tables, `$04EF/$04F0` ownership anchors, APU writer addresses, and all executable `JSR $DBB6` occurrences without exporting original audio payloads.
