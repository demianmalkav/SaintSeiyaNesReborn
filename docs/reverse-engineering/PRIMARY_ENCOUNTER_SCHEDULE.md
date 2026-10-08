# Primary/common platform encounter schedule

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: per-page descriptor table, generic common-slot spawn protocol and stat-tier matrix are statically reconstructed. Special type-specific handling for several numeric types remains separate.

## Distinct from secondary hazards

This is the **primary/common encounter** schedule at bank 1 `$9AE5+`, not the secondary/hazard schedule at `$9C24+` documented in `PLATFORM_OBJECT_SLOTS.md`.

It controls the two common entity records:

- slot A `$03BA-$03C9` / OAM `$0748+`
- slot B `$03CA-$03D9` / OAM `$077C+`

The current page index is `$45`; platform substate is `$02`.

## Per-page lookup

Bank 1 `$996C+`:

1. indexes the 18-entry pointer table at `$9AE5` with `$02`;
2. reads one descriptor byte using `$45` as page index;
3. stages it at `$03B7`;
4. waits until previous common entities are in a safe/free condition before accepting a changed descriptor into zero-page `$58`;
5. derives sprite-palette pointers and combat configuration from the accepted descriptor;
6. the fixed platform frame later maps bank 0 and calls `$B6D0`, which attempts actual common-slot creation.

This separation is important: **page encounter configuration can change before/independently of the exact frame on which an entity is instantiated**.

## Descriptor byte

For an accepted nonzero byte:

- bits `0..3` = numeric entity type;
- bits `4..5` = tier `0..3`;
- bit `7` = enable use of common slot B in addition to slot A;
- bit `6` = not consumed by the generic edge-spawn path; preserve as an unknown/special flag until its special-type consumers are fully classified.

Bits 4–5 have two proven effects:

1. choose one of four stat/resource configurations;
2. select sprite-palette variants through pointer tables at `$9F46+` before `$9EEF` writes sprite palette entries.

## Generic edge-spawn protocol — bank 0 `$B6D0/$B70F`

When descriptor `$03B7` matches accepted `$58`, the generic spawner attempts slot A.

If bit 7 is set it also permits slot B to be attempted. This is **not** an immediate same-frame double spawn because successful creation sets global cooldown `$03B8=$30` (48 updates). Slot B becomes eligible after the cooldown expires while the descriptor remains active.

Thus bit 7 is best represented as:

`allow_second_common_entity_slot`

rather than `spawn_two_now`.

### Spawn cooldown

`$03B8`:

- when nonzero, decrements and blocks creation;
- successful creation resets it to `$30` = **48 platform updates**.

### Generic spawnable numeric types

The edge-spawn helper explicitly refuses types:

- `$08`
- `$09`
- `$0C`
- `$0D`
- `$0E`

Those IDs use other/specialized handling and must not be forced through the generic spawn rule.

Normal accepted types include `$05,$06,$07,$0A,$0B,$0F` when their descriptor/state conditions permit.

### Ordinary edge placement

For ordinary accepted types other than `$0F`, spawn side is chosen from current scroll state and an entropy bit:

- left edge: `X=$00`, facing bit `$40` set -> move right;
- right edge: `X=$F8`, facing bit `$40` clear -> move left.

Initial Y starts at `$20`; the initializer probes downward in 16-pixel steps until the entity's ground descriptor reaches an accepted support class or the candidate leaves the active vertical range.

On success the entity enters state `$10`, receives a 31–94 decision timer, type, HP/drain/reward data and an OAM activation marker.

### Type `$0F`

Type `$0F` has a distinct position path: one branch uses the left edge, while another positions relative to `player_x + $78`; it does not use the same downward ground scan before entering the active state.

Its final design identity remains open.

## Stat/resource tier table — bank 1 `$9A35-$9AE4`

Every numeric type `$05-$0F` owns 16 bytes:

`4 tiers × [HP, Cosmo-drain ticks, Life-drain ticks, Seventh-Sense reward BCD]`

### Type `$05`

| tier | HP | Cosmo drain | Life drain | SS reward |
|---:|---:|---:|---:|---:|
|0|10|1|1|2|
|1|20|2|2|4|
|2|40|4|3|5|
|3|80|8|4|6|

### Type `$06`

| tier | HP | Cosmo drain | Life drain | SS reward |
|---:|---:|---:|---:|---:|
|0|15|1|1|4|
|1|30|2|2|6|
|2|60|4|3|9|
|3|120|8|4|13|

### Type `$07`

All four tiers: `HP100, Cosmo10, Life16, SS2`.

### Type `$08`

| tier | HP | Cosmo drain | Life drain | SS reward |
|---:|---:|---:|---:|---:|
|0|20|2|2|6|
|1|40|4|4|10|
|2|80|8|8|16|
|3|160|16|16|24|

### Type `$09`

All tiers: `HP100, Cosmo8, Life5, SS4`.

### Type `$0A`

All tiers: `HP0, Cosmo5, Life2, SS0`.

### Type `$0B`

All tiers: `HP0, Cosmo5, Life1, SS0`.

### Type `$0C`

All tiers: `HP30, Cosmo4, Life3, SS16`.

### Type `$0D`

All tiers: `HP100, Cosmo8, Life4, SS20`.

### Type `$0E`

All tiers: `HP0, Cosmo2, Life3, SS0`.

### Type `$0F`

All tiers: `HP20, Cosmo0, Life3, SS0`.

The zero-HP entries must not be labeled harmless: their type-specific handlers can still represent damaging hazards/objects.

## Tier auxiliary value `$03AB`

Tier also indexes table `$9A31`:

`[6, 10, 21, 43]`

The selected value is staged at `$03AB` and later copied into active entity record offset `$08` in platform mode. Its exact semantic meaning remains unresolved; preserve it as `tier_aux` rather than inventing a gameplay label.

## Main per-page descriptor sequences

For normal substates `$00-$0B`:

```text
00: 85 85 85 95 95 95
01: 95 95 95 86 86 86 86
02: 95 95 95 95 86 86 86 86
03: 95 95 95 95 88 88 96 96 96
04: 95 95 95 95 95 98 98 A5 A5 A5
05: 96 96 96 96 96 96 96 A5 A5 A5 A5
06: A5 A5 A5 A5 A5 A8 A8 A6 A6 A6 A6 A6
07: A5 A5 A5 A5 A5 A5 A8 A8 A8 A6 A6 A6 A6
08: B5 B5 B5 B5 B8 B8 B8 A6 A6 A6 A6 A6 A6 A6
09: B5 A8 A8 A8 B5 B5 B5 B5 A6 A6 A6 B5 B5 B5 B5
0A: B5 B5 B5 B8 B8 B5 B5 B5 B8 B8 B8 B6 B6 B6 B6 B6
0B: B5 B5 B8 B8 B8 B8 B8 B6 B6 B6 B6 B8 B8 B6 B6 B6
```

The progression is visibly data-driven: earlier approaches emphasize types `$05/$06` at lower tiers, while later stages introduce `$08` and tier-3 configurations.

## Special-substate sequences

```text
0C: 8B 8B 8B CC CC CC 8B 8B 8B 8B 8B 8B
0D: 8A 8A 8A 8A 00 87 87 87 89 89 89 89
0E: 00 8F 8F 8F 8F 8E 8D 8D 8D 8D 8D 8F
0F: 0D 0D 0D
10: 00 00 00 00 00 00 00 00 00 00 00 00
11: 00 00
```

These reinforce that several numeric types are special-script/object families rather than generic edge-spawn enemies.

## Reproducible extractor

`tools/reverse/extract_primary_encounters.py` reads a user-owned canonical ROM and emits JSON with:

- per-substate/page descriptors;
- decoded type/tier/bit flags;
- tier auxiliary value;
- HP/drain/reward configuration;
- whether the generic edge-spawner accepts the type.

No ROM bytes or extracted graphics are embedded in the tool.

## REBORN consequence

An original platform stage can now be represented as three independent but synchronized data streams:

1. `map chunks / collision descriptors`;
2. `primary encounter descriptor per page`;
3. `secondary hazard/object schedule per page`.

Together with the already reconstructed entity AI and player physics, this is enough to begin an executable clean-room stage simulation before graphics are modernized.
