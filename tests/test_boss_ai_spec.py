import unittest

from spec.original.boss_ai import select_boss_technique


class BossAiSpecTests(unittest.TestCase):
    def test_default_stages_follow_parity_bit(self):
        for stage in (1, 2, 3, 4, 7):
            self.assertEqual(select_boss_technique(stage, parity_bit=0), 0)
            self.assertEqual(select_boss_technique(stage, parity_bit=1), 1)

    def test_shaka_forces_slot_two_against_ikki(self):
        self.assertEqual(select_boss_technique(5, parity_bit=0, canonical_character_index=4), 2)
        self.assertEqual(select_boss_technique(5, parity_bit=1, canonical_character_index=0), 1)

    def test_milo_counter_threshold(self):
        self.assertEqual(select_boss_technique(6, event_counter_a=0, event_counter_b=0), 1)
        self.assertEqual(select_boss_technique(6, event_counter_a=1, event_counter_b=1), 0)

    def test_camus_special_flag_and_counter_threshold(self):
        self.assertEqual(select_boss_technique(8, event_counter_a=0, event_counter_b=0), 1)
        self.assertEqual(select_boss_technique(8, event_counter_a=2, event_counter_b=0), 0)
        self.assertEqual(select_boss_technique(8, camus_special_flag=True), 2)

    def test_aphrodite_escalates_by_phase(self):
        self.assertEqual(select_boss_technique(9, aphrodite_phase=0), 0)
        self.assertEqual(select_boss_technique(9, aphrodite_phase=2), 0)
        self.assertEqual(select_boss_technique(9, aphrodite_phase=3), 1)
        self.assertEqual(select_boss_technique(9, aphrodite_phase=5), 1)
        self.assertEqual(select_boss_technique(9, aphrodite_phase=6), 2)

    def test_saga_three_phase_dispatch(self):
        self.assertEqual(select_boss_technique(10, saga_phase=0), 0)
        self.assertEqual(select_boss_technique(10, saga_phase=0, saga_flag_06d0_ff=True), 1)
        self.assertEqual(select_boss_technique(10, saga_phase=1, player_technique_slot=0), 2)
        self.assertEqual(select_boss_technique(10, saga_phase=1, player_technique_slot=1), 0)
        self.assertEqual(select_boss_technique(10, saga_phase=1, saga_flag_06d0_ff=True), 3)
        self.assertEqual(select_boss_technique(10, saga_phase=2), 3)


if __name__ == "__main__":
    unittest.main()
