# Battle message context metadata

The Japanese script extractor gives every original dialogue entry a stable identity (`MSG_000` through `MSG_250`) and records statically visible callsites. This document defines the next localization layer: contextual metadata derived from the reconstructed battle/event dispatchers.

The source Japanese text remains a private artifact generated from the user's ROM. The public repository stores only the rules needed to classify where a message is used.

## Why context is separate from message identity

One original message ID can be reused in multiple places or phases. Therefore a semantic alias must never replace the canonical `MSG_xxx` key.

Localization identity remains:

`MSG_xxx`

Context is additive metadata such as:

- stage / House / opponent;
- battle phase;
- message slot/presentation side;
- callsite address and evidence source.

This lets the Spanish translator distinguish, for example, a line used through the `Talk` command from the same line reused after a Bronze Saint action.

## Battle phases currently classified

The bank-5 stage dispatch architecture provides four useful phase labels:

- `battle_init`
- `talk`
- `post_bronze_action`
- `post_gold_response`

The annotator deliberately does not infer speaker identity from phase alone. Speaker attribution will be added only where portrait selection, message slot, event state or narrative evidence establishes it.

## Stage keys

Current stable contextual labels include:

- `ARIES_MU`
- `TAURUS_ALDEBARAN`
- `GEMINI_BRANCH`
- `CANCER_DEATHMASK`
- `LEO_AIORIA`
- `VIRGO_SHAKA`
- `SCORPIO_MILO`
- `CAPRICORN_SHURA`
- `AQUARIUS_CAMUS`
- `PISCES_APHRODITE`
- `POPE_SAGA`
- `FINAL_SPECIAL`

Where the original ROM intentionally shares a handler and static analysis cannot distinguish the caller, the metadata preserves the ambiguity explicitly, e.g. `POPE_SAGA_OR_FINAL_SHARED` with no forced numeric stage index.

## Tool

`tools/localization/annotate_battle_message_context.py` consumes the private JSON emitted by `tools/reverse/extract_japanese_script.py` and adds:

- per-callsite `battle_context`;
- per-message deduplicated `battle_contexts`;
- annotation coverage metadata.

The classifier operates only on statically reconstructed PRG-bank-5 address ranges. Unknown or non-bank-5 callsites remain unannotated rather than being guessed.

## Spanish localization consequence

The intended private translation record can now carry fields conceptually equivalent to:

```text
stable_id: MSG_061
source_jp: <private Japanese source>
contexts:
  - TAURUS_ALDEBARAN / talk
  - TAURUS_ALDEBARAN / post_bronze_action
translation_es: <Spanish translation>
status: draft/reviewed/final
```

This preserves direct JP -> ES translation while giving the translator enough narrative context to choose pronouns, tone and terminology correctly.
