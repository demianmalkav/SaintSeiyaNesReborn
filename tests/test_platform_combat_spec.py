import unittest

from spec.original.platform_combat import (
    ContactDrainResult,
    ORDINARY_HIT_INVULNERABILITY_FRAMES,
    Saint,
    add_seventh_sense_reward,
    platform_attack_damage,
    projectile_range_parameter,
    resolve_contact_drain,
)


class PlatformCombatSpecTests(unittest.TestCase):
    def test_known_damage_values(self) -> None:
        self.assertEqual(platform_attack_damage(Saint.SEIYA, 99), 18)
        self.assertEqual(platform_attack_damage(Saint.SEIYA, 999), 188)
        self.assertEqual(platform_attack_damage(Saint.SHUN, 999), 247)
        self.assertEqual(platform_attack_damage(Saint.IKKI, 500), 75)

    def test_ones_digit_is_ignored_above_99(self) -> None:
        self.assertEqual(
            platform_attack_damage(Saint.SEIYA, 990),
            platform_attack_damage(Saint.SEIYA, 999),
        )

    def test_projectile_range(self) -> None:
        self.assertEqual(projectile_range_parameter(Saint.SEIYA, 99), 3)
        self.assertEqual(projectile_range_parameter(Saint.SEIYA, 999), 48)
        self.assertEqual(projectile_range_parameter(Saint.SHUN, 500), 8)
        self.assertEqual(projectile_range_parameter(Saint.IKKI, 0), 60)
        self.assertEqual(projectile_range_parameter(Saint.IKKI, 999), 60)

    def test_seventh_sense_reward_and_clamp(self) -> None:
        self.assertEqual(add_seventh_sense_reward(100, 0x15), 115)
        self.assertEqual(add_seventh_sense_reward(9988, 0x15), 9999)
        self.assertEqual(
            add_seventh_sense_reward(100, 0x15, mode_active=False), 100
        )

    def test_contact_drain(self) -> None:
        self.assertEqual(
            resolve_contact_drain(99, 99, 3, 5),
            ContactDrainResult(life=93, cosmo=94, frames=5, failed=False),
        )
        self.assertEqual(
            resolve_contact_drain(3, 99, 2, 0),
            ContactDrainResult(life=0, cosmo=99, frames=2, failed=True),
        )
        self.assertEqual(
            resolve_contact_drain(99, 1, 0, 1),
            ContactDrainResult(life=99, cosmo=0, frames=1, failed=True),
        )

    def test_ordinary_hit_invulnerability(self) -> None:
        self.assertEqual(ORDINARY_HIT_INVULNERABILITY_FRAMES, 32)


if __name__ == "__main__":
    unittest.main()
