import unittest

from spec.original.boss_dodge import (
    DodgeDirection,
    blocked_direction_from_parity,
    simulate_dodge,
    update_dodge_counters,
)


class BossDodgeSpecTests(unittest.TestCase):
    def test_blocked_direction_alternates_from_parity(self):
        self.assertEqual(blocked_direction_from_parity(0), DodgeDirection.RIGHT)
        self.assertEqual(blocked_direction_from_parity(1), DodgeDirection.LEFT)

    def test_left_requires_eight_net_ticks_from_center(self):
        short = simulate_dodge([DodgeDirection.LEFT] * 7, blocked_direction=DodgeDirection.RIGHT)
        enough = simulate_dodge([DodgeDirection.LEFT] * 8, blocked_direction=DodgeDirection.RIGHT)
        self.assertFalse(short.valid_movement)
        self.assertTrue(enough.valid_movement)
        self.assertTrue(enough.evaded)
        self.assertEqual(enough.final_offset, 0x10)

    def test_right_requires_nine_net_ticks_from_center(self):
        short = simulate_dodge([DodgeDirection.RIGHT] * 8, blocked_direction=DodgeDirection.LEFT)
        enough = simulate_dodge([DodgeDirection.RIGHT] * 9, blocked_direction=DodgeDirection.LEFT)
        self.assertFalse(short.valid_movement)
        self.assertTrue(enough.valid_movement)
        self.assertTrue(enough.evaded)
        self.assertEqual(enough.final_offset, 0xEE)

    def test_moving_into_blocked_direction_still_takes_damage(self):
        result = simulate_dodge([DodgeDirection.LEFT] * 8, blocked_direction=DodgeDirection.LEFT)
        self.assertTrue(result.valid_movement)
        self.assertFalse(result.evaded)

    def test_opposite_valid_direction_skips_damage(self):
        result = simulate_dodge([DodgeDirection.LEFT] * 8, blocked_direction=DodgeDirection.RIGHT)
        self.assertTrue(result.evaded)

    def test_direction_changes_use_net_offset_and_last_direction(self):
        result = simulate_dodge(
            [DodgeDirection.LEFT] * 10 + [DodgeDirection.RIGHT] * 3,
            blocked_direction=DodgeDirection.RIGHT,
        )
        # Net offset = +14, but last direction is RIGHT and the right-side
        # validation requires the wrapped negative range below 0xF0.
        self.assertFalse(result.valid_movement)
        self.assertEqual(result.final_direction, DodgeDirection.NONE)

    def test_success_and_failure_feed_separate_rom_counters(self):
        success = simulate_dodge([DodgeDirection.LEFT] * 8, blocked_direction=DodgeDirection.RIGHT)
        fail = simulate_dodge([], blocked_direction=DodgeDirection.RIGHT)
        self.assertEqual(update_dodge_counters(2, 3, success), (3, 3))
        self.assertEqual(update_dodge_counters(2, 3, fail), (2, 4))


if __name__ == "__main__":
    unittest.main()
