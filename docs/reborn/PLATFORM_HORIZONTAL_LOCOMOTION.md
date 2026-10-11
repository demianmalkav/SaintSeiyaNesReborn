# REBORN platform horizontal locomotion

Status: first bounded gameplay vertical slice.

This document owns the REBORN decision boundary for grounded horizontal locomotion and facing. The frozen original evidence remains in `docs/reverse-engineering/PLATFORM_PLAYER.md`, `PlatformHorizontalMotion.cs`, `PlatformGroundedSession.cs` and their ORIGINAL SPEC fixtures.

## Purpose

Preserve the original game's **semantic horizontal locomotion identity** while discarding NES-era storage and presentation coupling.

The original separates screen-local player X from horizontal scroll. At the camera handoff threshold, Right can stop changing local player X and advance scroll instead. In both cases:

```text
world_x = scroll_x + player_x
```

advances by the same grounded movement increment when unobstructed. REBORN therefore owns `WorldX`; camera policy is a presentation/game-camera concern for a later slice.

This is intentional modernization, not an oracle change. The bridge and parity fixtures prove that the modern world-space projection preserves the selected canonical behavior on both sides of the original camera handoff.

## Semantic contract

`Reborn.Core` owns:

- `RebornHorizontalInput`: `Neutral`, `Left`, `Right`;
- `RebornFacing`: `Left`, `Right`;
- `RebornMotionPhase`: deterministic even/odd logical-frame cadence;
- `RebornGroundedHorizontalProfile`: displacement for each cadence phase;
- `RebornPlatformPlayerHorizontalState`: `WorldX`, facing, phase and whether grounded locomotion is active;
- `RebornPlatformPlayerHorizontalLocomotion.Step`: pure deterministic state evolution.

For one free-space grounded logical tick:

- `Neutral`: world position and facing are preserved, locomotion becomes inactive;
- `Left`: subtract the phase-selected step, face left, locomotion active;
- `Right`: add the phase-selected step, face right, locomotion active;
- every logical tick advances the two-phase cadence, including neutral ticks.

No wall-clock time participates.

## Canonical bridge

`OriginalSpecPlatformHorizontalBridge` is the only layer that knows how to project the frozen original into these semantics.

It maps:

- canonical `scroll + player_x` -> REBORN `WorldX`;
- canonical facing value -> `RebornFacing`;
- canonical frame parity -> `RebornMotionPhase`;
- canonical grounded per-Saint increments -> `RebornGroundedHorizontalProfile`;
- canonical controller direction bits -> semantic horizontal input, preserving original Right priority when both directions are present.

The current frozen profiles are:

```text
Seiya / ordinary grounded profile: even 1, odd 1
Shun / distinct grounded profile: even 1, odd 2
```

The bridge derives these from `PlatformMovementIncrements.FromFrame`; REBORN Core does not encode original RAM fields or addresses.

## Parity coverage

The REBORN self-test compares Core output with `PlatformHorizontalMotion.StepGrounded` using synthetic open-space stage/probe fixtures. Coverage includes:

1. Right before the original camera handoff;
2. Right during camera handoff, proving equivalent world-space progression;
3. Left while canonical scroll is non-zero, proving backward movement changes world position without importing camera rules;
4. Neutral position/facing preservation;
5. the distinct even/odd grounded profile;
6. simultaneous canonical Left+Right projection preserving Right priority;
7. repeated equal state/input sequences producing identical results.

The oracle remains ORIGINAL SPEC. These fixtures do not make REBORN evidence for the 1988 game.

## Deliberately excluded

This checkpoint does **not** model:

- collision or terrain blocking;
- left/right stage boundaries;
- map/exits;
- camera behavior or screen anchoring as gameplay state;
- jump/vertical motion;
- attacks;
- hazards/damage;
- sprite animation or rendering;
- visual assets.

Those exclusions are required to keep the first gameplay migration finite. In particular, collision blocking remains a later semantic layer that decides whether a requested displacement may be committed; it must not force NES descriptor/probe storage into the locomotion core.

## Design consequence for the remake

REBORN is now free to use a modern camera, larger/redrawn sprites, richer animation and redesigned pixel-art environments without changing the proven locomotion cadence. Visual evolution therefore does not require preserving the original screen-local `$player_x + scroll` implementation split.

Any future deliberate change to locomotion feel—acceleration, different speed, animation-driven root motion, etc.—must be recorded as a REBORN design deviation rather than silently rewriting this parity baseline.
