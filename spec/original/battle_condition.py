"""Battle condition classifier reconstructed from PRG bank 5.

The original derives a coarse three-state condition for each combatant from
Life, Cosmo and a stage-specific threshold. It ignores the ones digit for the
threshold comparison: resources are reduced to hundreds+tens BCD magnitude.

Return states mirror zero-page `$EA/$EB`:
- -1 / 0xFF: Life is zero (defeated)
-  0 / 0x00: Life and Cosmo are both strictly above threshold
-  1 / 0x01: alive, but one or both resources do not clear threshold
"""

from __future__ import annotations

from enum import IntEnum

from .battle_resources import ThreeDigitResource


class CombatCondition(IntEnum):
    DEFEATED = -1
    ABOVE_THRESHOLD = 0
    BELOW_THRESHOLD = 1


# PRG bank 5 tables at $ADB9 and $AD42. Entries are packed two-digit
# hundreds+tens magnitudes, e.g. 0x20 represents 200 for comparison purposes.
# There are 11 valid stage entries before executable code resumes.
PLAYER_THRESHOLDS = (
    0x00, 0x05, 0x05, 0x08, 0x08, 0x08, 0x10, 0x10, 0x10, 0x10, 0x10,
)

OPPONENT_THRESHOLDS = (
    0x00, 0x05, 0x05, 0x08, 0x20, 0x20, 0x00, 0x30, 0x00, 0x20, 0x20,
)


def resource_magnitude(resource: ThreeDigitResource) -> int:
    """Return the BCD hundreds+tens magnitude used by the ROM comparison."""
    tens = (resource.low_bcd >> 4) & 0x0F
    return ((resource.hundreds & 0x0F) << 4) | tens


def classify(life: ThreeDigitResource, cosmo: ThreeDigitResource, threshold: int) -> CombatCondition:
    if not 0 <= threshold <= 0x99:
        raise ValueError("threshold must be a packed two-digit BCD magnitude")

    if life.value == 0:
        return CombatCondition.DEFEATED

    if resource_magnitude(cosmo) > threshold and resource_magnitude(life) > threshold:
        return CombatCondition.ABOVE_THRESHOLD

    return CombatCondition.BELOW_THRESHOLD


def classify_player(stage_index: int, life: ThreeDigitResource, cosmo: ThreeDigitResource) -> CombatCondition:
    try:
        threshold = PLAYER_THRESHOLDS[stage_index]
    except IndexError as exc:
        raise ValueError("stage_index must be 0..10") from exc
    return classify(life, cosmo, threshold)


def classify_opponent(stage_index: int, life: ThreeDigitResource, cosmo: ThreeDigitResource) -> CombatCondition:
    try:
        threshold = OPPONENT_THRESHOLDS[stage_index]
    except IndexError as exc:
        raise ValueError("stage_index must be 0..10") from exc
    return classify(life, cosmo, threshold)
