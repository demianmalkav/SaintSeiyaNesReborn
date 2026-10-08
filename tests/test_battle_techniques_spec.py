import unittest

from spec.original.battle_techniques import (
    Character,
    INITIAL_TECHNIQUE_COUNTS,
    MAX_TECHNIQUE_COUNTS,
    attack_id,
    available_attack_ids,
    initial_attack_ids,
    technique_for_attack_id,
    unlock_one,
)


class BattleTechniqueSpecTests(unittest.TestCase):
    def test_initial_counts_match_rom_table(self):
        self.assertEqual(INITIAL_TECHNIQUE_COUNTS, (2, 2, 2, 1, 2))
        self.assertEqual(MAX_TECHNIQUE_COUNTS, (3, 4, 4, 2, 2))

    def test_attack_id_layout_is_character_times_four(self):
        self.assertEqual(attack_id(Character.SEIYA, 2), 2)
        self.assertEqual(attack_id(Character.HYOGA, 3), 7)
        self.assertEqual(attack_id(Character.SHUN, 3), 11)
        self.assertEqual(attack_id(Character.SHIRYU, 1), 13)
        self.assertEqual(attack_id(Character.IKKI, 1), 17)

    def test_initial_selection_sets(self):
        self.assertEqual(initial_attack_ids(Character.SEIYA), (0, 1))
        self.assertEqual(initial_attack_ids(Character.HYOGA), (4, 5))
        self.assertEqual(initial_attack_ids(Character.SHUN), (8, 9))
        self.assertEqual(initial_attack_ids(Character.SHIRYU), (12,))
        self.assertEqual(initial_attack_ids(Character.IKKI), (16, 17))

    def test_full_selection_sets_skip_structural_unused_slots(self):
        self.assertEqual(available_attack_ids(Character.SEIYA, 3), (0, 1, 2))
        self.assertEqual(available_attack_ids(Character.HYOGA, 4), (4, 5, 6, 7))
        self.assertEqual(available_attack_ids(Character.SHUN, 4), (8, 9, 10, 11))
        self.assertEqual(available_attack_ids(Character.SHIRYU, 2), (12, 13))
        self.assertEqual(available_attack_ids(Character.IKKI, 2), (16, 17))

    def test_named_mappings_cover_all_accessible_slots(self):
        for character in Character:
            for value in available_attack_ids(character, MAX_TECHNIQUE_COUNTS[int(character)]):
                self.assertIsNotNone(technique_for_attack_id(value))

    def test_unlock_is_saturating(self):
        self.assertEqual(unlock_one(Character.SHUN, 2), 3)
        self.assertEqual(unlock_one(Character.SHUN, 3), 4)
        self.assertEqual(unlock_one(Character.SHUN, 4), 4)


if __name__ == "__main__":
    unittest.main()
