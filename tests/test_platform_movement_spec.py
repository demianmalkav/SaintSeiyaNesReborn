import unittest

from spec.original.platform_movement import (
    JumpKind,
    collision_probe_points,
    maximum_rise,
    movement_increments,
    select_jump_curve,
)


class PlatformMovementSpecTests(unittest.TestCase):
    def test_standing_jump_is_common_32_frame_curve(self):
        for slot in range(5):
            self.assertEqual(len(select_jump_curve(slot, JumpKind.STANDING)), 32)
            self.assertEqual(maximum_rise(slot, JumpKind.STANDING), 58)

    def test_high_jump_profiles_match_reconstructed_rom_tables(self):
        self.assertEqual([len(select_jump_curve(i, JumpKind.HIGH)) for i in range(5)], [60, 50, 40, 40, 50])
        self.assertEqual([maximum_rise(i, JumpKind.HIGH) for i in range(5)], [103, 88, 71, 71, 88])

    def test_directional_jump_profiles_match_reconstructed_rom_tables(self):
        self.assertEqual([len(select_jump_curve(i, JumpKind.DIRECTIONAL)) for i in range(5)], [54, 40, 44, 44, 54])
        self.assertEqual([maximum_rise(i, JumpKind.DIRECTIONAL) for i in range(5)], [39, 33, 34, 34, 39])

    def test_movement_increment_asymmetry_for_engine_slot_1(self):
        self.assertEqual(movement_increments(0, 0), (1, 1, 0))
        self.assertEqual(movement_increments(0, 1), (1, 2, 1))
        self.assertEqual(movement_increments(1, 0), (1, 2, 1))
        self.assertEqual(movement_increments(1, 1), (2, 2, 1))

    def test_collision_probe_geometry(self):
        p = collision_probe_points(player_x=0x40, player_y=0x70, scroll_x=0x120)
        self.assertEqual((p.head_center.x, p.head_center.y), (0x168, 0x70))
        self.assertEqual((p.upper_left.x, p.upper_left.y), (0x160, 0x70))
        self.assertEqual((p.upper_right.x, p.upper_right.y), (0x170, 0x70))
        self.assertEqual((p.lower_left.x, p.lower_left.y), (0x160, 0x80))
        self.assertEqual((p.lower_right.x, p.lower_right.y), (0x170, 0x80))
        self.assertEqual((p.ground_left.x, p.ground_left.y), (0x158, 0x90))
        self.assertEqual((p.ground_center.x, p.ground_center.y), (0x168, 0x90))
        self.assertEqual((p.ground_right.x, p.ground_right.y), (0x178, 0x90))

    def test_y_88_uses_special_lower_probe_offset(self):
        p = collision_probe_points(player_x=0, player_y=0x88, scroll_x=0)
        self.assertEqual(p.lower_left.y, 0xA8)
        self.assertEqual(p.lower_right.y, 0xA8)


if __name__ == "__main__":
    unittest.main()
