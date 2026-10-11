import importlib.util
import unittest
from pathlib import Path


TOOL_PATH = Path(__file__).resolve().parents[1] / "tools" / "reverse" / "audit_platform_visual_resources.py"
_spec = importlib.util.spec_from_file_location("audit_platform_visual_resources", TOOL_PATH)
assert _spec is not None and _spec.loader is not None
visuals = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(visuals)


class PlatformVisualResourceToolTests(unittest.TestCase):
    def synthetic_banks(self):
        prg = [bytearray(visuals.PRG_BANK_SIZE) for _ in range(8)]
        chr4k = [bytes(visuals.CHR4K_SIZE) for _ in range(32)]

        def put_switched(bank, address, payload):
            start = address - 0x8000
            prg[bank][start : start + len(payload)] = bytes(payload)

        def put_fixed(address, payload):
            start = address - 0xC000
            prg[7][start : start + len(payload)] = bytes(payload)

        put_fixed(0xCACF, [25] * 18)

        definition_by_type = {}
        next_address = 0xA000
        for entity_type in visuals.PRIMARY_TYPES:
            count = 12 if entity_type == 0x0D else 1
            payload = [count]
            for index in range(count):
                if index == 0 and entity_type == 0x05:
                    payload += [0xFF, 0x40]
                payload += [(0x20 + index) & 0xFF, 0, 0]
            put_switched(3, next_address, payload)
            definition_by_type[entity_type] = next_address
            next_address += 0x40

        for table in visuals.POINTER_TABLES:
            for entity_type, definition in definition_by_type.items():
                entry = table + entity_type * 2
                put_switched(3, entry, [definition & 0xFF, definition >> 8])

        blank = [11]
        for _ in range(11):
            blank += [0xFE, 0, 0]
        put_switched(3, visuals.DIRECT_BLANK_DEFINITION, blank)

        put_fixed(0xC0E3, [0xB2] + [0] * 10)
        put_fixed(0xC0EF, [0xFE] + [0] * 10)

        # $9B93 and its direct bootstrap tables are also in switchable bank 3.
        put_switched(3, 0x9B65, [0, 0xB4, 0xB4, 0xB4, 0xB4, 0xF6, 0xDA])
        put_switched(3, 0x9B6C, [0, 1, 4, 1, 4, 1, 3])
        profiles = [
            0, 0, 0, 0,
            0x14, 0, 2, 2,
            0x28, 0, 6, 4,
            0x14, 0, 2, 2,
            0x28, 0, 6, 4,
            0x1E, 0x0A, 3, 1,
            0x28, 0x0A, 5, 4,
        ]
        put_switched(3, 0x9B73, profiles)
        put_switched(3, 0x9B8F, [0x1E, 5, 5, 1])

        return [bytes(bank) for bank in prg], chr4k

    def test_complete_synthetic_audit(self):
        prg, chr4k = self.synthetic_banks()
        result = visuals.audit(prg, chr4k)

        self.assertEqual([25], result["chr0"]["active_4k_banks"])
        self.assertEqual(12, result["shared_compositor"]["max_sprite_count_by_type"]["0D"])
        self.assertEqual(11, result["shared_compositor"]["visual_capacity_by_type"]["05"])
        self.assertEqual(12, result["shared_compositor"]["visual_capacity_by_type"]["0D"])
        self.assertEqual(1, result["shared_compositor"]["pointer_tables"]["B671"]["05"]["attribute_override_count"])
        self.assertTrue(result["attached_A908"]["entries"][0]["creates_visual"])
        self.assertEqual(-2, result["attached_A908"]["entries"][0]["y_offset"])
        self.assertEqual(3, result["multisprite_9B93"]["prg_bank"])
        self.assertEqual(0x8C, result["multisprite_9B93"]["dedicated_substate_0D_tile"])

    def test_only_type_0d_gets_twelve_record_capacity(self):
        for entity_type in visuals.PRIMARY_TYPES:
            expected = 12 if entity_type == 0x0D else 11
            self.assertEqual(expected, visuals.visual_capacity(entity_type))

    def test_parser_rejects_pointer_outside_bank3_window(self):
        prg, _ = self.synthetic_banks()
        with self.assertRaises(ValueError):
            visuals.parse_metasprite(prg, 0xC000)


if __name__ == "__main__":
    unittest.main()
