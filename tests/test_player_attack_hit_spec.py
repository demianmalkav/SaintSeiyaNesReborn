import unittest

from spec.original.player_attack_hit import resolve_player_attack_hit


class PlayerAttackHitSpecTests(unittest.TestCase):
    def test_normal_threshold_is_five(self):
        self.assertFalse(resolve_player_attack_hit(0x04).connects)
        self.assertTrue(resolve_player_attack_hit(0x05).connects)
        self.assertTrue(resolve_player_attack_hit(0x1F).connects)

    def test_success_token_preserves_low_nibble(self):
        result = resolve_player_attack_hit(0xAB)
        self.assertTrue(result.connects)
        self.assertEqual(result.hit_token, 0x0B)
        self.assertEqual(result.threshold, 5)

    def test_special_phase_two_raises_threshold_to_six(self):
        self.assertFalse(resolve_player_attack_hit(0x05, special_phase_two=True).connects)
        result = resolve_player_attack_hit(0x06, special_phase_two=True)
        self.assertTrue(result.connects)
        self.assertEqual(result.threshold, 6)

    def test_scripted_block_forces_zero_token(self):
        result = resolve_player_attack_hit(0x0F, scripted_block=True)
        self.assertFalse(result.connects)
        self.assertEqual(result.hit_token, 0)

    def test_special_no_damage_context_forces_zero(self):
        result = resolve_player_attack_hit(0x0F, forced_no_damage_context=True)
        self.assertFalse(result.connects)
        self.assertEqual(result.hit_token, 0)

    def test_low_nibble_only_controls_threshold(self):
        self.assertEqual(resolve_player_attack_hit(0x05), resolve_player_attack_hit(0xF5))
        self.assertEqual(resolve_player_attack_hit(0x0C), resolve_player_attack_hit(0xAC))


if __name__ == "__main__":
    unittest.main()
