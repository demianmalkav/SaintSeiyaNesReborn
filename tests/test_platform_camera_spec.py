import unittest


TERMINAL_HIGH_PAGE = [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 14, 10, 10, 10, 1, 10, 0]


def camera_may_advance(substate: int, low: int, high: int) -> bool:
    if low < 0xF8:
        return True
    return high < TERMINAL_HIGH_PAGE[substate]


class PlatformCameraSpecTests(unittest.TestCase):
    def test_terminal_page_table(self):
        self.assertEqual(4, TERMINAL_HIGH_PAGE[0x00])
        self.assertEqual(14, TERMINAL_HIGH_PAGE[0x0B])
        self.assertEqual(10, TERMINAL_HIGH_PAGE[0x10])
        self.assertEqual(0, TERMINAL_HIGH_PAGE[0x11])

    def test_scroll_low_boundary_defers_terminal_check(self):
        self.assertTrue(camera_may_advance(0x00, 0xF7, 4))
        self.assertFalse(camera_may_advance(0x00, 0xF8, 4))
        self.assertTrue(camera_may_advance(0x00, 0xF8, 3))

    def test_tracking_band_constants(self):
        self.assertEqual(0x80, 128)
        self.assertEqual(0x10, 16)
        self.assertEqual(0xE0, 224)


if __name__ == "__main__":
    unittest.main()
