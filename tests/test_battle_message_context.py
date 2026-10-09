import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "tools" / "localization" / "annotate_battle_message_context.py"
SPEC = importlib.util.spec_from_file_location("annotate_battle_message_context", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class BattleMessageContextTests(unittest.TestCase):
    def classify(self, address: int, bank: int = 5):
        return MODULE.classify_callsite({"prg_bank": bank, "cpu_address": f"0x{address:04X}"})

    def test_taurus_talk_and_post_action_boundaries(self):
        self.assertEqual("TAURUS_ALDEBARAN", self.classify(0x9D2C)["stage_key"])
        self.assertEqual("talk", self.classify(0x9D80)["phase"])
        self.assertEqual("post_bronze_action", self.classify(0xA3A2)["phase"])
        self.assertEqual("post_gold_response", self.classify(0xA415)["phase"])

    def test_major_battle_contexts(self):
        self.assertEqual("LEO_AIORIA", self.classify(0x9DDF)["stage_key"])
        self.assertEqual("VIRGO_SHAKA", self.classify(0xA72C)["stage_key"])
        self.assertEqual("AQUARIUS_CAMUS", self.classify(0xA990)["stage_key"])
        self.assertEqual("PISCES_APHRODITE", self.classify(0xAAA6)["stage_key"])
        self.assertEqual("POPE_SAGA", self.classify(0xAC64)["stage_key"])

    def test_shared_final_talk_handler_remains_explicitly_ambiguous(self):
        context = self.classify(0xA02C)
        self.assertIsNone(context["stage_index"])
        self.assertEqual("POPE_SAGA_OR_FINAL_SHARED", context["stage_key"])
        self.assertEqual("talk", context["phase"])

    def test_non_bank5_and_unclassified_addresses_remain_unannotated(self):
        self.assertIsNone(self.classify(0x9D2C, bank=4))
        self.assertIsNone(self.classify(0x9742))

    def test_payload_annotation_deduplicates_message_contexts(self):
        payload = {
            "messages": [
                {
                    "id": 61,
                    "stable_id": "MSG_061",
                    "immediate_callsites": [
                        {"prg_bank": 5, "cpu_address": "0x9D37"},
                        {"prg_bank": 5, "cpu_address": "0xA402"},
                    ],
                }
            ]
        }
        result = MODULE.annotate(payload)
        contexts = result["messages"][0]["battle_contexts"]
        self.assertEqual(1, len(contexts))
        self.assertEqual("TAURUS_ALDEBARAN", contexts[0]["stage_key"])
        self.assertEqual(2, result["context_annotation"]["annotated_callsites"])


if __name__ == "__main__":
    unittest.main()
