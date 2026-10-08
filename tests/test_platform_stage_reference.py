import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "tools" / "reference" / "platform_stage_model.py"
SPEC = importlib.util.spec_from_file_location("platform_stage_model", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def synthetic_spec(substate: int) -> dict:
    rows = [[0 for _ in range(16)] for _ in range(11)]
    return {
        "substates": [
            {
                "substate": substate,
                "pages": [
                    {
                        "page_index": 0,
                        "metatile_rows_11x16": rows,
                        "primary_encounter": None,
                        "secondary_archetype": None,
                    }
                ],
            }
        ]
    }


class PlatformStageReferenceParityTests(unittest.TestCase):
    def test_grounded_upper_probe_is_special_substate_only(self):
        normal = MODULE.StageView(synthetic_spec(0x00), 0x00)
        special = MODULE.StageView(synthetic_spec(0x0C), 0x0C)

        right = MODULE.ProbeSet(
            upper_left=None,
            upper_center=None,
            upper_right=0x80,
            lower_left=None,
            lower_right=None,
            bottom_left=None,
            bottom_center=None,
            bottom_right=None,
        )
        left = MODULE.ProbeSet(
            upper_left=0x88,
            upper_center=None,
            upper_right=None,
            lower_left=None,
            lower_right=None,
            bottom_left=None,
            bottom_center=None,
            bottom_right=None,
        )

        self.assertTrue(normal.can_move_right(right))
        self.assertFalse(special.can_move_right(right))
        self.assertTrue(normal.can_move_left(left))
        self.assertFalse(special.can_move_left(left))

    def test_airborne_upper_solid_is_global(self):
        normal = MODULE.StageView(synthetic_spec(0x00), 0x00)
        probes = MODULE.ProbeSet(
            upper_left=None,
            upper_center=None,
            upper_right=0xE0,
            lower_left=None,
            lower_right=None,
            bottom_left=None,
            bottom_center=None,
            bottom_right=None,
        )
        self.assertFalse(normal.can_move_right(probes, airborne=True))


if __name__ == "__main__":
    unittest.main()
