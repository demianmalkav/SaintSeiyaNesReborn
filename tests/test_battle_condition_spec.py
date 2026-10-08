import unittest

from spec.original.battle_condition import (
    CombatCondition,
    OPPONENT_THRESHOLDS,
    PLAYER_THRESHOLDS,
    classify,
    classify_opponent,
    classify_player,
    resource_magnitude,
)
from spec.original.battle_resources import ThreeDigitResource


class BattleConditionSpecTests(unittest.TestCase):
    def r(self, value: int, boundary: int = 10) -> ThreeDigitResource:
        return ThreeDigitResource.from_int(value, boundary)

    def test_tables_have_eleven_stage_entries(self):
        self.assertEqual(len(PLAYER_THRESHOLDS), 11)
        self.assertEqual(len(OPPONENT_THRESHOLDS), 11)

    def test_resource_magnitude_ignores_ones_digit(self):
        self.assertEqual(resource_magnitude(self.r(299)), 0x29)
        self.assertEqual(resource_magnitude(self.r(290)), 0x29)
        self.assertEqual(resource_magnitude(self.r(99)), 0x09)

    def test_zero_life_is_defeated_regardless_of_cosmo(self):
        self.assertEqual(classify(self.r(0), self.r(999), 0x00), CombatCondition.DEFEATED)

    def test_comparison_is_strictly_greater_than_threshold(self):
        # 200 -> magnitude 0x20, so equality does not clear a 0x20 threshold.
        self.assertEqual(classify(self.r(200), self.r(999), 0x20), CombatCondition.BELOW_THRESHOLD)
        self.assertEqual(classify(self.r(210), self.r(999), 0x20), CombatCondition.ABOVE_THRESHOLD)

    def test_both_life_and_cosmo_must_clear_threshold(self):
        self.assertEqual(classify(self.r(999), self.r(99), 0x10), CombatCondition.BELOW_THRESHOLD)
        self.assertEqual(classify(self.r(99), self.r(999), 0x10), CombatCondition.BELOW_THRESHOLD)
        self.assertEqual(classify(self.r(199), self.r(199), 0x10), CombatCondition.ABOVE_THRESHOLD)

    def test_stage_tables_can_treat_player_and_opponent_differently(self):
        # Stage 4: player threshold 0x08, opponent threshold 0x20.
        life = self.r(199)
        cosmo = self.r(199)
        self.assertEqual(classify_player(4, life, cosmo), CombatCondition.ABOVE_THRESHOLD)
        self.assertEqual(classify_opponent(4, life, cosmo), CombatCondition.BELOW_THRESHOLD)


if __name__ == "__main__":
    unittest.main()
