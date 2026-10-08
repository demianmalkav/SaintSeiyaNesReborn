import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "tools" / "localization" / "validate_catalog.py"
SPEC = importlib.util.spec_from_file_location("validate_catalog", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


def synthetic_rows():
    rows = []
    for message_id in range(MODULE.MESSAGE_COUNT):
        rows.append({
            "id": str(message_id),
            "stable_id": f"MSG_{message_id:03d}",
            "jp_original": "てすと",
            "es_draft": "Prueba.",
            "status": "DRAFT",
            "speaker": "",
            "scene": "",
            "semantic_alias": "",
            "source_text_offset": f"0x{message_id:04X}",
        })
    return rows


class LocalizationCatalogValidatorTests(unittest.TestCase):
    def setUp(self):
        self.fieldnames = sorted(MODULE.REQUIRED_COLUMNS)

    def test_complete_synthetic_catalog_passes(self):
        self.assertEqual([], MODULE.validate_rows(synthetic_rows(), self.fieldnames))

    def test_missing_id_is_detected(self):
        rows = synthetic_rows()
        del rows[17]
        issues = MODULE.validate_rows(rows, self.fieldnames)
        messages = [issue.message for issue in issues]
        self.assertTrue(any("expected 251 rows" in message for message in messages))
        self.assertTrue(any("missing ids: [17]" in message for message in messages))

    def test_stable_id_and_translation_state_are_checked(self):
        rows = synthetic_rows()
        rows[3]["stable_id"] = "MSG_WRONG"
        rows[4]["status"] = "FINAL"
        rows[4]["es_draft"] = ""
        issues = MODULE.validate_rows(rows, self.fieldnames)
        messages = [issue.message for issue in issues]
        self.assertTrue(any("stable_id must be MSG_003" in message for message in messages))
        self.assertTrue(any("es_draft is empty for status FINAL" in message for message in messages))

    def test_semantic_alias_format_is_strict(self):
        rows = synthetic_rows()
        rows[0]["semantic_alias"] = "aries.intro"
        issues = MODULE.validate_rows(rows, self.fieldnames)
        self.assertTrue(any("semantic_alias must use A-Z" in issue.message for issue in issues))


if __name__ == "__main__":
    unittest.main()
