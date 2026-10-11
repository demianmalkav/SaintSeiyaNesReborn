import importlib.util
import unittest
from pathlib import Path


TOOL_PATH = Path(__file__).resolve().parents[1] / "tools" / "reverse" / "audit_platform_visual_refresh.py"
_spec = importlib.util.spec_from_file_location("audit_platform_visual_refresh", TOOL_PATH)
assert _spec is not None and _spec.loader is not None
refresh = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(refresh)


class PlatformVisualRefreshAuditTests(unittest.TestCase):
    def synthetic_prg(self):
        prg = [bytearray(refresh.PRG_BANK_SIZE) for _ in range(8)]

        def put(address, payload):
            start = address - 0x8000
            prg[1][start : start + len(payload)] = bytes(payload)

        def put_u16(address, value):
            put(address, [value & 0xFF, value >> 8])

        put(refresh.DYNAMIC_CHR0_TABLE, [0x1D, 0x1D, 0x1B, 0x00, 0x19, 0x00])
        put(refresh.SPECIAL_DESCRIPTOR_DEFAULT, [1, 2, 3])
        put(refresh.SPECIAL_DESCRIPTOR_SUBSTATE10, [4, 5, 6])
        put(refresh.BACKGROUND_A022, list(range(9)))
        put(refresh.BACKGROUND_A02B, list(range(9, 18)))

        next_descriptor = 0xA100

        for saint in range(5):
            put_u16(refresh.OUTER_TABLE_9F46 + saint * 2, next_descriptor)
            put(next_descriptor, [1, 2, 3])
            next_descriptor += 3

        next_list = 0xA200
        for entity_type in range(0x05, 0x10):
            put_u16(refresh.OUTER_TABLE_9F46 + entity_type * 2, next_list)
            for variant in range(4):
                put_u16(next_list + variant * 2, next_descriptor)
                put(next_descriptor, [1, 2, 3])
                next_descriptor += 3
            put_u16(refresh.SECONDARY_TABLE_9F66 + entity_type * 2, next_descriptor)
            put(next_descriptor, [1, 2, 3])
            next_descriptor += 3
            next_list += 8

        for profile in range(1, 5):
            put_u16(refresh.AUX_TABLE_9C7F + profile * 2, next_descriptor)
            put(next_descriptor, [1, 2, 3])
            next_descriptor += 3

        for selector in range(1, 6):
            put_u16(refresh.PENDING_TABLE_9CC6 + (selector - 1) * 2, next_descriptor)
            put(next_descriptor, [1, 2, 3])
            next_descriptor += 3

        return [bytes(bank) for bank in prg]

    def test_audit_reports_structure_without_palette_payloads(self):
        result = refresh.audit(self.synthetic_prg())
        self.assertEqual(
            [0x1D, 0x1D, 0x1B, 0x00, 0x19, 0x00],
            result["special_one_shot"]["dynamic_chr0_by_substate_0C_11"],
        )
        self.assertEqual(5, len(result["player_descriptors"]))
        self.assertEqual(11, len(result["primary_profile_descriptors"]))
        self.assertEqual(4, len(result["auxiliary_profile_descriptors"]))
        self.assertEqual(5, len(result["pending_03A9_descriptors"]))
        self.assertEqual(3, result["transfer_format"]["descriptor_byte_count"])
        self.assertNotIn("bytes", result["player_descriptors"]["00"])

    def test_descriptor_rejects_pointer_outside_bank1(self):
        with self.assertRaises(ValueError):
            refresh.descriptor(self.synthetic_prg(), 0xC000)


if __name__ == "__main__":
    unittest.main()
