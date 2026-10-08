"""Bronze-Saint attack hit gate reconstructed from fixed `$FAAA+`.

The player attack animation and technique selection are separate from whether
resource damage is actually applied. `$06BC` is a non-zero hit token consumed
by bank 1 `$AD52+`; zero skips damage entirely.

The token is derived from the low nibble of the continuously evolving battle
phase accumulator `$065F`, unless a story/script flag `$0690` blocks the hit.
No probability is asserted here because the accumulator's nibble distribution
has not yet been shown to be uniform.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class PlayerAttackHitResult:
    connects: bool
    hit_token: int
    threshold: int


def resolve_player_attack_hit(
    phase_accumulator: int,
    *,
    scripted_block: bool = False,
    special_phase_two: bool = False,
    forced_no_damage_context: bool = False,
) -> PlayerAttackHitResult:
    """Mirror fixed `$FAAA-$FAE5` plus bank-1 `$AC0F` special zeroing.

    Normal threshold: low nibble >= 5.
    When `$06CE == 2`: low nibble >= 6.
    `$0690 != 0` forces a miss/no-damage token.
    Special stage/context `$050E == $0C` also forces `$06BC = 0` in `$ABF4+`.

    On a successful normal hit, the ROM stores the nibble itself in `$06BC`,
    not merely boolean 1. Consumers observed so far treat zero/non-zero as the
    meaningful distinction.
    """
    if not 0 <= phase_accumulator <= 0xFF:
        raise ValueError("phase_accumulator must fit in one byte")

    threshold = 6 if special_phase_two else 5
    if scripted_block or forced_no_damage_context:
        return PlayerAttackHitResult(False, 0, threshold)

    token = phase_accumulator & 0x0F
    if token < threshold:
        return PlayerAttackHitResult(False, 0, threshold)
    return PlayerAttackHitResult(True, token, threshold)
