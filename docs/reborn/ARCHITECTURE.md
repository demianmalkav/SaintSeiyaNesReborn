# REBORN architecture — initial boundary

Status: initial architecture checkpoint.

ORIGINAL SPEC is a frozen oracle for proven 1988 behavior. REBORN consumes that knowledge through semantic contracts; it must not turn NES implementation details into its own domain model.

## Projects

```text
SaintSeiyaNesReborn.OriginalSpec
    frozen semantic oracle / evidence-derived clean-room contracts

SaintSeiyaNesReborn.Reborn.Core
    modern deterministic domain/runtime
    references: none of OriginalSpec, no graphics/audio framework

SaintSeiyaNesReborn.Reborn.OriginalBridge
    anti-corruption layer
    references: OriginalSpec + Reborn.Core
    owns translation from canonical NES-facing contracts to REBORN semantic DTOs

future Reborn host/adapters
    references: Reborn.Core and selected bridge/content packages
    owns window/input/render/audio/filesystem/platform integration

SaintSeiyaNesReborn.Reborn.SelfTest
    architecture/parity boundary fixtures
    references all three only for verification
```

Dependency rule:

```text
OriginalSpec -----\
                  > Reborn.OriginalBridge -> Reborn.Core contracts
Reborn.Core ------/

host/adapters ---------------------------> Reborn.Core
```

`Reborn.Core` must never reference `OriginalSpec`. The bridge may reference both. ORIGINAL SPEC never references REBORN.

## What may cross the ORIGINAL SPEC -> REBORN boundary

Allowed:

- stable semantic identities such as `MSG_000..MSG_250`;
- state-transition meaning already promoted by frozen fixtures;
- formulas, resource values and bounded enums when they are gameplay semantics rather than hardware mechanics;
- semantic requests/results represented by REBORN-owned DTOs.

Evidence-only / bridge-only:

- CPU addresses and routine entrypoints;
- RAM addresses such as `$066A/$066B/$0672`;
- PRG/CHR bank numbers and MMC1 switching;
- PPU/APU register layout, OAM/tile encodings and nametable mechanics;
- source offsets used only for reverse-engineering traceability;
- copyrighted ROM/audio/dialogue payloads.

If a new REBORN feature appears to require one of those details in gameplay code, add or improve a semantic bridge contract instead of leaking the hardware concept downstream.

## Deterministic runtime model

`RebornRuntime.Step` is the sole domain-time primitive in the initial skeleton.

Rules:

1. one call advances exactly one monotonically increasing logical tick;
2. the same state plus the same input must produce the same semantic frame output;
3. wall-clock time is a host concern and is converted into zero or more fixed domain steps outside Core;
4. rendering and audio consume semantic outputs and cannot mutate domain state implicitly;
5. randomness, when introduced, must be injected/owned deterministically and parity-tested against the frozen canonical semantics selected for preservation.

The first skeleton intentionally has no real-time clock, thread scheduler, graphics API or audio device dependency.

## Content and localization boundary

Public REBORN code owns identities and schemas, not copyrighted payloads.

`IRebornLocalizationPort` resolves a `RebornTextRequest` into `RebornLocalizedText`. The current `OriginalSpecLocalizationBridge` adapts the already-closed external canonical catalog contract. Full Japanese/Spanish dialogue remains external/private.

REBORN Core sees:

```text
message id      RebornMessageId -> MSG_xxx
lane            Lane0 / Lane1
variant         Variant0 / Variant1
language        Japanese / Spanish
resolved text   injected by localization port
```

It does not see `$E7B3/$E7B7/$E7C3/$E7C7`, `$066A/$066B`, or raw `$0672` values. Those remain inside the bridge.

## Presentation and audio boundaries

Core emits semantic frame data. Platform-facing adapters implement `IRebornPresentationAdapter` and `IRebornAudioAdapter` outside the domain.

The domain must not depend on sprites, tiles, palettes, APU channels, DirectX/OpenGL/Vulkan/SDL/MonoGame types, window handles or device timing. Future adapters may use any rendering/audio technology as long as the semantic output contract remains deterministic and testable.

## Save/progression boundary

REBORN owns its save format; it does not serialize NES RAM or emulator state.

The initial `RebornSaveSnapshot` is versioned (`SchemaVersion = 1`) and currently stores only the deterministic runtime tick because no gameplay progression has been migrated yet. New progression fields must be semantic, versioned and migration-safe. Unknown schema versions are rejected explicitly.

Canonical password behavior remains an ORIGINAL SPEC compatibility oracle, not the native REBORN persistence format.

## First vertical slice

The architecture checkpoint uses runtime text presentation as the first bounded end-to-end slice because it is already closed canonically and can be verified without copyrighted payloads or broad gameplay migration.

Path:

```text
canonical synthetic request
  -> OriginalSpecLocalizationBridge
  -> RebornTextRequest (MSG_xxx + opaque lane/variant)
  -> RebornRuntime.Step
  -> injected localization resolution
  -> RebornFrameOutput
```

Acceptance criteria:

- all four canonical text entrypoint combinations map deterministically to two REBORN lanes and two opaque variants;
- stable message identity is preserved;
- JP/ES resolution and Japanese fallback match the frozen contract;
- lane/variant metadata survives resolution unchanged;
- two runtimes given identical initial state/input produce identical semantic output;
- Core has no assembly reference to OriginalSpec;
- CI rejects NES-address/entrypoint leakage into Core;
- save snapshot round-trip preserves deterministic tick and rejects unknown schema versions.

Synthetic `JP_SYNTH_xxx` / `ES_SYNTH_xxx` fixture strings are used exclusively in public tests.

## Deliberate non-goals of this checkpoint

- no broad platform gameplay port;
- no battle port;
- no renderer or audio backend selection;
- no original graphics/music/dialogue payloads;
- no emulator or cycle-accurate compatibility layer;
- no redesign of ORIGINAL SPEC.

## Next architecture-to-gameplay boundary

After this checkpoint is green and merged, the next bounded implementation should introduce the first actual gameplay slice in REBORN Core using frozen semantic fixtures. Prefer a small platform-player locomotion slice (input -> horizontal motion/facing -> semantic frame state) before collision, hazards, maps, rendering or narrative are layered on top. Its acceptance test must compare the selected canonical semantics against REBORN output without exposing NES addresses to Core.
