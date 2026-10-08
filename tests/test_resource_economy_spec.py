import unittest

from spec.original.resources import (
    Resource,
    ResourceState,
    add_enemy_seventh_sense_reward,
    add_scripted_seventh_sense_hundreds,
    pack_cap_byte,
    resource_max_from_boundary,
    sacrifice_resource_for_seventh_sense,
    spend_seventh_sense_for_resource,
    unpack_cap_byte,
)


class ResourceEconomySpecTests(unittest.TestCase):
    def test_cap_byte_semantics(self) -> None:
        self.assertEqual(unpack_cap_byte(0x11), (1, 1))
        self.assertEqual(unpack_cap_byte(0x55), (5, 5))
        self.assertEqual(pack_cap_byte(5, 2), 0x52)
        self.assertEqual(resource_max_from_boundary(1), 99)
        self.assertEqual(resource_max_from_boundary(5), 499)

    def test_initial_normal_saint_caps(self) -> None:
        state = ResourceState(life=99, cosmo=99, seventh_sense=100, cap_byte=0x11)
        self.assertEqual(state.life_max, 99)
        self.assertEqual(state.cosmo_max, 99)
        self.assertEqual(spend_seventh_sense_for_resource(state, Resource.LIFE), state)
        self.assertEqual(spend_seventh_sense_for_resource(state, Resource.COSMO), state)

    def test_spend_seventh_sense_one_to_one(self) -> None:
        state = ResourceState(life=98, cosmo=97, seventh_sense=10, cap_byte=0x11)
        after_life = spend_seventh_sense_for_resource(state, Resource.LIFE)
        self.assertEqual((after_life.life, after_life.seventh_sense), (99, 9))

        after_cosmo = spend_seventh_sense_for_resource(state, Resource.COSMO)
        self.assertEqual((after_cosmo.cosmo, after_cosmo.seventh_sense), (98, 9))

    def test_reverse_exchange_one_to_one(self) -> None:
        state = ResourceState(life=20, cosmo=30, seventh_sense=100, cap_byte=0x11)
        after_life = sacrifice_resource_for_seventh_sense(state, Resource.LIFE)
        self.assertEqual((after_life.life, after_life.seventh_sense), (19, 101))

        after_cosmo = sacrifice_resource_for_seventh_sense(state, Resource.COSMO)
        self.assertEqual((after_cosmo.cosmo, after_cosmo.seventh_sense), (29, 101))

    def test_exchange_refuses_empty_source_and_full_seventh_sense(self) -> None:
        empty = ResourceState(life=0, cosmo=0, seventh_sense=10, cap_byte=0x11)
        self.assertEqual(sacrifice_resource_for_seventh_sense(empty, Resource.LIFE), empty)
        self.assertEqual(sacrifice_resource_for_seventh_sense(empty, Resource.COSMO), empty)

        full = ResourceState(life=10, cosmo=10, seventh_sense=9999, cap_byte=0x11)
        self.assertEqual(sacrifice_resource_for_seventh_sense(full, Resource.LIFE), full)

    def test_seventh_sense_reward_scales(self) -> None:
        self.assertEqual(add_enemy_seventh_sense_reward(100, 0x15), 115)
        self.assertEqual(add_enemy_seventh_sense_reward(9990, 0x15), 9999)
        self.assertEqual(add_scripted_seventh_sense_hundreds(100, 0x02), 300)
        self.assertEqual(add_scripted_seventh_sense_hundreds(100, 0x10), 1100)
        self.assertEqual(add_scripted_seventh_sense_hundreds(9800, 0x10), 9999)


if __name__ == "__main__":
    unittest.main()
