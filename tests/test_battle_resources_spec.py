import unittest

from spec.original.battle_resources import (
    ThreeDigitResource,
    load_battle_resources,
    seventh_sense_decrement,
    seventh_sense_increment,
    unpack_cap_byte,
)


class BattleResourceSpecTests(unittest.TestCase):
    def test_cap_byte_semantics(self):
        self.assertEqual(unpack_cap_byte(0x11), (1, 1))
        self.assertEqual(unpack_cap_byte(0x55), (5, 5))

    def test_three_digit_caps_are_exclusive_hundreds_boundaries(self):
        r = ThreeDigitResource.from_int(99, 1)
        self.assertEqual(r.maximum, 99)
        self.assertEqual(r.increment(), (r, True))

        r = ThreeDigitResource.from_int(499, 5)
        self.assertEqual(r.maximum, 499)
        self.assertEqual(r.increment(), (r, True))

    def test_decimal_carry_and_borrow(self):
        r = ThreeDigitResource.from_int(99, 5)
        n, blocked = r.increment()
        self.assertFalse(blocked)
        self.assertEqual(n.value, 100)
        self.assertEqual((n.low_bcd, n.hundreds), (0x00, 1))

        n2, blocked = n.decrement()
        self.assertFalse(blocked)
        self.assertEqual(n2.value, 99)
        self.assertEqual((n2.low_bcd, n2.hundreds), (0x99, 0))

    def test_zero_decrement_is_blocked(self):
        r = ThreeDigitResource.from_int(0, 1)
        self.assertEqual(r.decrement(), (r, True))

    def test_persistent_record_maps_life_then_cosmo(self):
        resources = load_battle_resources((0x99, 0, 0x49, 4, 0x15))
        self.assertEqual(resources.life.value, 99)
        self.assertEqual(resources.life.maximum, 99)
        self.assertEqual(resources.cosmo.value, 449)
        self.assertEqual(resources.cosmo.maximum, 499)

    def test_seventh_sense_edges(self):
        self.assertEqual(seventh_sense_increment(9998), (9999, False))
        self.assertEqual(seventh_sense_increment(9999), (9999, True))
        self.assertEqual(seventh_sense_decrement(1), (0, False))
        self.assertEqual(seventh_sense_decrement(0), (0, True))


if __name__ == "__main__":
    unittest.main()
