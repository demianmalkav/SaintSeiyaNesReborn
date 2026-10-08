#!/usr/bin/env python3
"""Validate a private JP->ES localization catalog without embedding its contents.

Expected CSV schema matches the project catalog kept outside the public repo.
The validator checks identity, coverage, status transitions and source-trace
metadata for the canonical 251-message dialogue corpus.
"""

from __future__ import annotations

import argparse
import csv
from dataclasses import dataclass
from pathlib import Path
import re

MESSAGE_COUNT = 251
ALLOWED_STATUS = {"RAW", "DRAFT", "REVIEWED", "FINAL", "NEEDS_CONTEXT"}
REQUIRED_COLUMNS = {
    "id",
    "stable_id",
    "jp_original",
    "es_draft",
    "status",
    "speaker",
    "scene",
    "semantic_alias",
    "source_text_offset",
}
HEX_OFFSET = re.compile(r"^0x[0-9A-Fa-f]{4}$")


@dataclass(frozen=True)
class CatalogIssue:
    row: int
    message: str


def validate_rows(rows: list[dict[str, str]], fieldnames: list[str] | None) -> list[CatalogIssue]:
    issues: list[CatalogIssue] = []
    fields = set(fieldnames or [])
    missing_columns = sorted(REQUIRED_COLUMNS - fields)
    if missing_columns:
        issues.append(CatalogIssue(1, f"missing required columns: {', '.join(missing_columns)}"))
        return issues

    if len(rows) != MESSAGE_COUNT:
        issues.append(CatalogIssue(1, f"expected {MESSAGE_COUNT} rows, found {len(rows)}"))

    seen_ids: set[int] = set()
    seen_stable: set[str] = set()

    for index, row in enumerate(rows, start=2):
        raw_id = (row.get("id") or "").strip()
        try:
            message_id = int(raw_id)
        except ValueError:
            issues.append(CatalogIssue(index, f"invalid numeric id {raw_id!r}"))
            continue

        if message_id in seen_ids:
            issues.append(CatalogIssue(index, f"duplicate id {message_id}"))
        seen_ids.add(message_id)

        expected_stable = f"MSG_{message_id:03d}"
        stable_id = (row.get("stable_id") or "").strip()
        if stable_id != expected_stable:
            issues.append(CatalogIssue(index, f"stable_id must be {expected_stable}, got {stable_id!r}"))
        if stable_id in seen_stable:
            issues.append(CatalogIssue(index, f"duplicate stable_id {stable_id!r}"))
        seen_stable.add(stable_id)

        status = (row.get("status") or "").strip()
        if status not in ALLOWED_STATUS:
            issues.append(CatalogIssue(index, f"invalid status {status!r}"))

        jp = row.get("jp_original") or ""
        es = row.get("es_draft") or ""
        if not jp.strip():
            issues.append(CatalogIssue(index, "jp_original is empty"))

        if status in {"DRAFT", "REVIEWED", "FINAL"} and not es.strip():
            issues.append(CatalogIssue(index, f"es_draft is empty for status {status}"))

        offset = (row.get("source_text_offset") or "").strip()
        if not HEX_OFFSET.match(offset):
            issues.append(CatalogIssue(index, f"invalid source_text_offset {offset!r}"))

        semantic_alias = (row.get("semantic_alias") or "").strip()
        if semantic_alias and not re.fullmatch(r"[A-Z0-9_]+", semantic_alias):
            issues.append(CatalogIssue(index, f"semantic_alias must use A-Z, 0-9 and underscore: {semantic_alias!r}"))

    expected_ids = set(range(MESSAGE_COUNT))
    missing_ids = sorted(expected_ids - seen_ids)
    extra_ids = sorted(seen_ids - expected_ids)
    if missing_ids:
        issues.append(CatalogIssue(1, f"missing ids: {missing_ids}"))
    if extra_ids:
        issues.append(CatalogIssue(1, f"ids outside 0..250: {extra_ids}"))

    return issues


def validate_catalog(path: Path) -> list[CatalogIssue]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        rows = list(reader)
        return validate_rows(rows, reader.fieldnames)


def main() -> None:
    parser = argparse.ArgumentParser(description="Validate the private Kanketsu Hen JP->ES catalog")
    parser.add_argument("catalog", type=Path)
    args = parser.parse_args()

    issues = validate_catalog(args.catalog)
    if issues:
        for issue in issues:
            print(f"row {issue.row}: {issue.message}")
        raise SystemExit(1)

    print(f"Localization catalog OK: {MESSAGE_COUNT}/{MESSAGE_COUNT} canonical message IDs validated.")


if __name__ == "__main__":
    main()
