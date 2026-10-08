# Parity corrections

This file records cases where an executable clean-room model was corrected after a stricter re-read of the canonical ROM.

## 2026-10-08 — grounded upper-side collision probes

Earlier C# and Python stage reference models checked the upper-left / upper-right grounded side probes in every platform substate.

PRG bank 3 `$AB60-$AB7A` and `$AC09-$AC23` show the exact rule:

- lower side probes remain part of ordinary grounded movement;
- the upper side probe branch is entered only when `$02` is `$0C`, `$0D`, or `$0E`;
- `$02 < $0C` and `$02 >= $0F` skip that grounded upper-side test;
- directional airborne movement has its own upper-probe checks and is not governed by this grounded special-substate gate.

The executable models and tests were updated accordingly.

Reason for keeping this note: it demonstrates why REBORN compatibility code should derive behavior from specific branch structure rather than flattening all collision probes into a generic body collider too early.
