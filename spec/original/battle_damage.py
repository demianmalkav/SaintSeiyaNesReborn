"""Clean-room damage scaling for Kanketsu Hen Gold Saint battles.

Both sides derive separate Cosmo and Life drain counts from the attacker's
current Cosmo. The ROM multiplies Cosmo by an 8-bit coefficient, converts the
product back through decimal work buffers, effectively takes floor(product/100),
and enforces a pre-mitigation minimum drain of 3.

Player attack ids are not arbitrary: `$ACA1+` receives
`canonical_character_index * 4 + technique_slot`, giving five characters ×
four possible slots = 20 ids. Some slots are intentionally unused and carry
zero coefficients.

Opponent attacks have an additional mitigation tier in `$0681`:
0 -> full drain, 1 -> half, any other non-zero value -> quarter.
The shifts happen *after* the minimum-3 clamp, so quarter mitigation can reduce
3 to zero.
"""

from __future__ import annotations

from dataclasses import dataclass


# `$BCD4`: two coefficients per player attack id.
# Canonical character order is [Seiya, Hyoga, Shun, Shiryu, Ikki].
# Tuple order is (opponent Cosmo drain coefficient, opponent Life drain coefficient).
PLAYER_ATTACK_COEFFICIENTS: tuple[tuple[int, int], ...] = (
    # Seiya: ids 0..3
    (26, 18), (18, 26), (25, 25), (25, 25),
    # Hyoga: ids 4..7
    (16, 24), (24, 16), (24, 24), (30, 30),
    # Shun: ids 8..11
    (19, 19), (25, 17), (39, 26), (35, 35),
    # Shiryu: ids 12..15; slots 2/3 are unused in the coefficient table
    (21, 21), (40, 40), (0, 0), (0, 0),
    # Ikki: ids 16..19; slots 2/3 are unused
    (30, 20), (20, 30), (0, 0), (0, 0),
)


# `$BD7A`: stages 1..10, four opponent techniques per stage, two coefficients
# per technique. Stage 0 is Mu/non-battle and is intentionally absent because
# the ROM indexes this table as `(stage - 1) * 8 + technique * 2`.
# Tuple order is (player Cosmo drain coefficient, player Life drain coefficient).
OPPONENT_ATTACK_COEFFICIENTS: tuple[tuple[tuple[int, int], ...], ...] = (
    ((19, 29), (19, 29), (19, 29), (19, 29)),
    ((26, 18), (26, 18), (26, 18), (26, 18)),
    ((28, 18), (28, 18), (28, 18), (28, 18)),
    ((48, 32), (32, 48), (48, 32), (32, 48)),
    ((42, 28), (24, 36), (37, 22), (37, 22)),
    ((29, 19), (20, 30), (29, 19), (20, 30)),
    ((26, 26), (26, 26), (26, 26), (26, 26)),
    ((30, 20), (21, 31), (30, 20), (21, 31)),
    ((22, 32), (34, 22), (29, 29), (29, 29)),
    ((35, 23), (30, 30), (60, 60), (60, 60)),
)


@dataclass(frozen=True)
class ResourceDrain:
    cosmo: int
    life: int


def scaled_drain(attacker_cosmo: int, coefficient: int) -> int:
    if not 0 <= attacker_cosmo <= 999:
        raise ValueError("attacker_cosmo must be 0..999")
    if not 0 <= coefficient <= 0xFF:
        raise ValueError("coefficient must fit in one byte")
    return max(3, (attacker_cosmo * coefficient) // 100)


def player_attack_id(canonical_character_index: int, technique_slot: int) -> int:
    if not 0 <= canonical_character_index <= 4:
        raise ValueError("canonical_character_index must be 0..4")
    if not 0 <= technique_slot <= 3:
        raise ValueError("technique_slot must be 0..3")
    return canonical_character_index * 4 + technique_slot


def player_attack_drain(attacker_cosmo: int, attack_id: int) -> ResourceDrain:
    if not 0 <= attack_id < len(PLAYER_ATTACK_COEFFICIENTS):
        raise ValueError("attack_id must be 0..19")
    cosmo_coeff, life_coeff = PLAYER_ATTACK_COEFFICIENTS[attack_id]
    return ResourceDrain(
        cosmo=scaled_drain(attacker_cosmo, cosmo_coeff),
        life=scaled_drain(attacker_cosmo, life_coeff),
    )


def _apply_mitigation(value: int, mitigation_tier: int) -> int:
    if mitigation_tier < 0:
        raise ValueError("mitigation_tier cannot be negative")
    if mitigation_tier == 0:
        return value
    value >>= 1
    if mitigation_tier == 1:
        return value
    return value >> 1


def opponent_attack_drain(
    attacker_cosmo: int,
    stage_index: int,
    technique_index: int,
    mitigation_tier: int = 0,
) -> ResourceDrain:
    """Mirror `$BCFC+`/`$BD57+` for Gold-Saint attack drain.

    `stage_index` is 1..10 because stage 0 is the non-battle Mu scene.
    `technique_index` is the engine value `$0680`, 0..3.
    """
    if not 1 <= stage_index <= 10:
        raise ValueError("stage_index must be 1..10")
    if not 0 <= technique_index <= 3:
        raise ValueError("technique_index must be 0..3")

    cosmo_coeff, life_coeff = OPPONENT_ATTACK_COEFFICIENTS[stage_index - 1][technique_index]
    cosmo = scaled_drain(attacker_cosmo, cosmo_coeff)
    life = scaled_drain(attacker_cosmo, life_coeff)
    return ResourceDrain(
        cosmo=_apply_mitigation(cosmo, mitigation_tier),
        life=_apply_mitigation(life, mitigation_tier),
    )
