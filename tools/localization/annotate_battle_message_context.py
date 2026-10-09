#!/usr/bin/env python3
"""Annotate extracted dialogue callsites with statically reconstructed battle context.

Input is the private JSON emitted by tools/reverse/extract_japanese_script.py.
This tool does not contain or emit any built-in game text; it only adds semantic
metadata to callsites whose PRG-bank-5 addresses fall inside confirmed stage
handler ranges.
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class ContextRange:
    start: int
    end: int
    stage_index: int | None
    stage_key: str
    phase: str

    def contains(self, address: int) -> bool:
        return self.start <= address <= self.end


# Ranges are derived from the four stage-indexed dispatcher families documented
# in docs/reverse-engineering/BATTLE_EVENT_DISPATCH.md. Where two stage indices
# intentionally share one handler, stage_index remains None and stage_key states
# the ambiguity explicitly.
CONTEXT_RANGES: tuple[ContextRange, ...] = (
    # Battle/stage initialization handlers.
    ContextRange(0x97F8, 0x981E, 1, "TAURUS_ALDEBARAN", "battle_init"),
    ContextRange(0x981F, 0x9850, 2, "GEMINI_BRANCH", "battle_init"),
    ContextRange(0x9851, 0x989C, 3, "CANCER_DEATHMASK", "battle_init"),
    ContextRange(0x989D, 0x9A27, 4, "LEO_AIORIA", "battle_init"),
    ContextRange(0x9A28, 0x9ACD, 5, "VIRGO_SHAKA", "battle_init"),
    ContextRange(0x9ACF, 0x9B13, 7, "CAPRICORN_SHURA", "battle_init"),
    ContextRange(0x9B14, 0x9B5B, 8, "AQUARIUS_CAMUS", "battle_init"),
    ContextRange(0x9B5D, 0x9C94, 10, "POPE_SAGA", "battle_init"),

    # Talk / interaction dispatcher handlers.
    ContextRange(0x9CB7, 0x9D2B, 0, "ARIES_MU", "talk"),
    ContextRange(0x9D2C, 0x9D80, 1, "TAURUS_ALDEBARAN", "talk"),
    ContextRange(0x9D81, 0x9D95, 2, "GEMINI_BRANCH", "talk"),
    ContextRange(0x9D96, 0x9DD7, 3, "CANCER_DEATHMASK", "talk"),
    ContextRange(0x9DD8, 0x9E1A, 4, "LEO_AIORIA", "talk"),
    ContextRange(0x9E1B, 0x9E50, 5, "VIRGO_SHAKA", "talk"),
    ContextRange(0x9E51, 0x9ED5, 6, "SCORPIO_MILO", "talk"),
    ContextRange(0x9ED6, 0x9EFF, 7, "CAPRICORN_SHURA", "talk"),
    ContextRange(0x9F00, 0x9F98, 8, "AQUARIUS_CAMUS", "talk"),
    ContextRange(0x9F99, 0x9FF3, 9, "PISCES_APHRODITE", "talk"),
    ContextRange(0x9FF4, 0xA1AC, None, "POPE_SAGA_OR_FINAL_SHARED", "talk"),
    ContextRange(0xA1AD, 0xA360, 12, "FINAL_SPECIAL", "talk"),

    # Post-Bronze-action and post-Gold-response handlers alternate in ROM.
    ContextRange(0xA3A2, 0xA414, 1, "TAURUS_ALDEBARAN", "post_bronze_action"),
    ContextRange(0xA415, 0xA443, 1, "TAURUS_ALDEBARAN", "post_gold_response"),
    ContextRange(0xA444, 0xA4CB, 2, "GEMINI_BRANCH", "post_bronze_action"),
    ContextRange(0xA4CC, 0xA50E, 2, "GEMINI_BRANCH", "post_gold_response"),
    ContextRange(0xA50F, 0xA55F, 3, "CANCER_DEATHMASK", "post_bronze_action"),
    ContextRange(0xA560, 0xA5B2, 3, "CANCER_DEATHMASK", "post_gold_response"),
    ContextRange(0xA5B3, 0xA63D, 4, "LEO_AIORIA", "post_bronze_action"),
    ContextRange(0xA63E, 0xA660, 4, "LEO_AIORIA", "post_gold_response"),
    ContextRange(0xA661, 0xA7B2, 5, "VIRGO_SHAKA", "post_bronze_action"),
    ContextRange(0xA7B3, 0xA7FE, 5, "VIRGO_SHAKA", "post_gold_response"),
    ContextRange(0xA7FF, 0xA846, 6, "SCORPIO_MILO", "post_bronze_action"),
    ContextRange(0xA847, 0xA86A, 6, "SCORPIO_MILO", "post_gold_response"),
    ContextRange(0xA86B, 0xA8D7, 7, "CAPRICORN_SHURA", "post_bronze_action"),
    ContextRange(0xA8D8, 0xA8FB, 7, "CAPRICORN_SHURA", "post_gold_response"),
    ContextRange(0xA8FC, 0xA9D2, 8, "AQUARIUS_CAMUS", "post_bronze_action"),
    ContextRange(0xA9D3, 0xAA56, 8, "AQUARIUS_CAMUS", "post_gold_response"),
    ContextRange(0xAA57, 0xAAEF, 9, "PISCES_APHRODITE", "post_bronze_action"),
    ContextRange(0xAAF0, 0xAB17, 9, "PISCES_APHRODITE", "post_gold_response"),
    ContextRange(0xAB18, 0xAC04, 10, "POPE_SAGA", "post_bronze_action"),
    ContextRange(0xAC05, 0xACFF, 10, "POPE_SAGA", "post_gold_response"),
)


def classify_callsite(callsite: dict[str, object]) -> dict[str, object] | None:
    if int(callsite.get("prg_bank", -1)) != 5:
        return None

    raw = callsite.get("cpu_address")
    if not isinstance(raw, str):
        return None
    try:
        address = int(raw, 16)
    except ValueError:
        return None

    matches = [entry for entry in CONTEXT_RANGES if entry.contains(address)]
    if len(matches) > 1:
        raise RuntimeError(f"overlapping battle context ranges at ${address:04X}: {matches}")
    if not matches:
        return None

    entry = matches[0]
    return {
        "stage_index": entry.stage_index,
        "stage_key": entry.stage_key,
        "phase": entry.phase,
        "evidence": "bank5_static_dispatch_range",
    }


def annotate(payload: dict[str, object]) -> dict[str, object]:
    messages = payload.get("messages")
    if not isinstance(messages, list):
        raise ValueError("input JSON has no messages array")

    annotated_callsites = 0
    messages_with_context = 0
    for message in messages:
        if not isinstance(message, dict):
            continue
        callsites = message.get("immediate_callsites")
        if not isinstance(callsites, list):
            continue

        contexts: list[dict[str, object]] = []
        seen: set[tuple[object, object, object]] = set()
        for callsite in callsites:
            if not isinstance(callsite, dict):
                continue
            context = classify_callsite(callsite)
            if context is None:
                continue
            callsite["battle_context"] = context
            annotated_callsites += 1
            key = (context["stage_index"], context["stage_key"], context["phase"])
            if key not in seen:
                seen.add(key)
                contexts.append(context.copy())

        message["battle_contexts"] = contexts
        if contexts:
            messages_with_context += 1

    metadata = payload.setdefault("context_annotation", {})
    if isinstance(metadata, dict):
        metadata.update({
            "schema": "SaintSeiyaNesReborn.BattleMessageContext.v1",
            "annotated_callsites": annotated_callsites,
            "messages_with_battle_context": messages_with_context,
        })
    return payload


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("-o", "--output", type=Path, required=True)
    args = parser.parse_args()

    payload = json.loads(args.input.read_text(encoding="utf-8"))
    annotate(payload)
    args.output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
