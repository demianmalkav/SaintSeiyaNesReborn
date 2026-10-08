import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "tools" / "reverse" / "extract_japanese_script.py"
SPEC = importlib.util.spec_from_file_location("extract_japanese_script", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class JapaneseTextCodecSpecTests(unittest.TestCase):
    def test_synthetic_kana_diacritics_and_controls(self):
        # Synthetic, non-game phrase: が パ\n0ー?!
        raw = bytes([
            0x09, 0x3B,  # か + dakuten -> が
            0x01,
            0x5D, 0x3C,  # ハ + handakuten -> パ
            0xA4,
            0x80,
            0x3E,
            0x3F,
            0x40,
            0xFF,
        ])
        self.assertEqual("が パ\n0ー?!", MODULE.decode_message(raw))

    def test_stable_font_ranges(self):
        self.assertEqual("あ", MODULE.HIRAGANA[0x04])
        self.assertEqual("ん", MODULE.HIRAGANA[0x31])
        self.assertEqual("ア", MODULE.KATAKANA[0x44])
        self.assertEqual("ン", MODULE.KATAKANA[0x71])
        self.assertEqual("0", MODULE.VISIBLE[0x80])
        self.assertEqual("Z", MODULE.VISIBLE[0xA3])

    def test_unclassified_tile_remains_explicit(self):
        self.assertEqual("<TILE_02>", MODULE.decode_message(bytes([0x02, 0xFF])))


if __name__ == "__main__":
    unittest.main()
