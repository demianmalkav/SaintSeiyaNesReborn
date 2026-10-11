# Canonical runtime text-content integration

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Scope: the runtime request/localization boundary only. Message extraction, CHR-backed text storage and glyph decoding were already closed in `TEXT_ENGINE.md`. This checkpoint does not version Japanese or Spanish dialogue payloads and does not implement REBORN presentation UI.

## Canonical request entrypoints

The four fixed-bank entrypoints define the complete public request mapping needed by the modern runtime contract:

```text
$E7B3  message id -> $066A ; $0672=$FF ; dispatch through $EC6D
$E7B7  message id -> $066A ; $0672=$00 ; dispatch through $EC6D
$E7C3  message id -> $066B ; $0672=$FF ; dispatch through $ECB8
$E7C7  message id -> $066B ; $0672=$00 ; dispatch through $ECB8
```

Therefore the canonical request identity is the tuple:

```text
(message id, message slot, $0672 variant)
```

where:

- message id is exactly `0..250` and maps to stable public key `MSG_000..MSG_250`;
- message slot is either `$066A` or `$066B`;
- the observed `$0672` values emitted by these entrypoints are `$00` and `$FF`.

No stronger semantic name is assigned to `$0672`. Existing evidence proves that it is request/presentation metadata shared with the text path, but not enough to call it left/right, speaker/player, portrait side, or any other narrower interpretation. The clean-room model therefore preserves the raw byte.

## Runtime metadata boundary

The public runtime needs only metadata that can affect deterministic request resolution or traceability:

```text
stable_id          immutable MSG_000..MSG_250 identity
message slot       canonical $066A or $066B request channel
variant0672        raw canonical $00/$FF request metadata
language           requested JP or ES runtime language
speaker            optional private-catalog metadata
scene              optional private-catalog metadata
semantic_alias     optional descriptive alias; never primary identity
source_text_offset traceability back to the extracted canonical source
```

`speaker`, `scene` and `semantic_alias` are descriptive catalog metadata. They do not alter canonical message identity or dispatch semantics. Unknown/unconfirmed fields may remain empty rather than being inferred.

## External catalog contract

`CanonicalRuntimeLocalization` accepts a complete external catalog with exactly 251 entries. The public assembly contains no dialogue corpus.

Required canonical identity rules:

```text
entry count        exactly 251
numeric ids        exactly 0..250, no duplicates
stable ids         MSG_000..MSG_250 matching numeric id
Japanese source    present for every entry
Spanish payload    may be absent while translation is incomplete
```

The existing private CSV schema remains compatible:

```text
id
stable_id
jp_original
es_draft
status
speaker
scene
semantic_alias
source_text_offset
```

The runtime loader parses that CSV externally. The private catalog remains in Drive/workspace storage and is not compiled or copied into the repository.

## Language resolution and fallback

Resolution is deterministic:

1. find the entry by canonical message id;
2. preserve slot and `$0672` metadata unchanged;
3. select the requested language payload;
4. if ES is missing and fallback policy is `Japanese`, return JP and mark the resolution as fallback;
5. if fallback policy is `None`, missing requested content is an error.

Japanese is never synthesized from Spanish. Stable identity never falls back to a different message id.

## Clean-room executable contract

Public artifact:

- `src/SaintSeiyaNesReborn.OriginalSpec/CanonicalRuntimeLocalization.cs`

It models:

- canonical entrypoint -> `(id, slot, variant)` mapping;
- exact 251-entry coverage enforcement;
- stable ID validation;
- external CSV loading;
- JP/ES selection;
- explicit JP fallback for incomplete Spanish entries;
- propagation of descriptive/traceability metadata.

Synthetic fixtures:

- `tests/SaintSeiyaNesReborn.OriginalSpec.SelfTest/CanonicalRuntimeLocalizationChecks.cs`

Fixtures use generated `JP_SYNTH_xxx` / `ES_SYNTH_xxx` strings only. They verify:

- all four `$E7Bx` entrypoint mappings;
- slot and variant propagation;
- deterministic language selection;
- explicit fallback behavior;
- exact `0..250` coverage and duplicate rejection;
- stable ID mismatch rejection;
- external CSV quoting behavior;
- absence of embedded dialogue/localization resources.

## Content separation

The following remain private/non-versioned:

- the complete Japanese dialogue corpus;
- Spanish translations/drafts;
- any future reviewed/final localization payload;
- screenshots or captures used only to establish speaker/scene context.

The public repository may contain IDs, schemas, engine semantics, validators, synthetic fixtures and non-copyrighted metadata classifications.

## Closure

This checkpoint closes runtime text/content integration as a clean-room engine boundary. It does not change the already-closed NES text extraction/codec model, and it does not begin REBORN UI implementation.

Do not reopen this boundary unless new canonical-ROM evidence contradicts the request mapping, a catalog invariant changes, or a regression fixture fails.
