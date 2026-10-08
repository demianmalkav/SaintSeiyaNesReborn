import unittest

from spec.original.battle_damage import (
    ResourceDrain,
    opponent_attack_drain,
    player_attack_drain,
    scaled_drain,
)


class BattleDamageSpecTests(unittest.TestCase):
    def test_scaled_drain_is_floor_percent_with_minimum_three(self):
        self.assertEqual(scaled_drain(999, 26), 259)
        self.assertEqual(scaled_drain(100, 26), 26)
        self.assertEqual(scaled_drain(1, 26), 3)
        self.assertEqual(scaled_drain(0, 0), 3)

    def test_player_attack_uses_two_independent_coefficients(self):
        self.assertEqual(player_attack_drain(100, 0), ResourceDrain(cosmo=26, life=18))
        self.assertEqual(player_attack_drain(100, 1), ResourceDrain(cosmo=18, life=26))
        self.assertEqual(player_attack_drain(999, 13), ResourceDrain(cosmo=399, life=399))

    def test_opponent_stage_and_technique_select_coefficient_pair(self):
        # Stage 4 row alternates (48,32) and (32,48).
        self.assertEqual(opponent_attack_drain(100, 4, 0), ResourceDrain(cosmo=48, life=32))
        self.assertEqual(opponent_attack_drain(100, 4, 1), ResourceDrain(cosmo=32, life=48))

    def test_mitigation_is_applied_after_minimum_clamp(self):
        full = opponent_attack_drain(1, 1, 0, mitigation_tier=0)
        half = opponent_attack_drain(1, 1, 0, mitigation_tier=1)
        quarter = opponent_attack_drain(1, 1, 0, mitigation_tier=2)
        self.assertEqual(full, ResourceDrain(3, 3))
        self.assertEqual(half, ResourceDrain(1, 1))
        self.assertEqual(quarter, ResourceDrain(0, 0))

    def test_any_mitigation_tier_above_one_is_only_quarter(self):
        self.assertEqual(
            opponent_attack_drain(999, 10, 2),
            opponent_attack_drain(999, 10, 99),
        )


if __name__ == "__main__":
    unittest.main()
