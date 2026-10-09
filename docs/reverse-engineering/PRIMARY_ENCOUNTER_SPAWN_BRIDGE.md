# Primary encounter -> common spawn bridge

Status: **clean-room integration of already confirmed data paths**.

The stage model already carries the per-page `PlatformPrimaryEncounter` reconstructed from bank 1 `$996C+/$9AE5+`. The two common-entity producers reconstructed later consume the same effective configuration through RAM `$58` and profile bytes `$03AE/$03AD/$03AC/$03AF`.

`PlatformPrimaryEncounterSpawnConfig` makes that relationship explicit instead of requiring callers to duplicate type/tier/profile data.

## Descriptor mapping

`PlatformPrimaryEncounter.Raw` is preserved verbatim as `Engine58`.

This matters because the raw byte carries more than the low-nibble type:

- bits 0-3: type;
- bits 4-5: tier;
- bit 6: still-preserved special/unknown flag;
- bit 7: enables the second common slot path.

The decoded `TypeId`, `Tier`, `SecondCommonSlotEnabled`, and `Bit6Unknown` remain available for semantic code, but producers receive the raw byte just as the original does.

## Stat profile mapping

`PlatformEncounterStats` is semantic/decoded:

```text
HP: integer
Cosmo drain: byte
Life drain: byte
Seventh Sense reward: decimal integer
```

The common logical record expects:

```text
+$0C HP byte
+$0D Life-drain ticks
+$0E Cosmo-drain ticks
+$0F packed-BCD Seventh-Sense reward
```

The bridge centralizes both important conversions:

1. Life and Cosmo are written in the original offset order (`Life -> +$0D`, `Cosmo -> +$0E`), even though the semantic record lists Cosmo first.
2. Seventh-Sense reward is encoded back to packed BCD with `PackedBcd.EncodeByte`.

## Producer wrappers

The bridge exposes thin wrappers for both confirmed producers:

- bank-0 generic edge producer `$B6D0-$B7FC` via `StepCommonEdgeSpawner`;
- bank-1 scheduled special producer `$8925-$89D1` via `TryScheduledSpecialSpawn`.

This means one `PlatformStagePage.PrimaryEncounter` can now be the single semantic source for `$58` and the per-entity stat profile, while producer-specific state such as cooldowns, camera triggers and slot occupancy remains explicit.

## Consequence

The clean-room stage representation now connects three layers that were previously separate:

```text
ROM page descriptor
    -> PlatformPrimaryEncounter
    -> PlatformPrimaryEncounterSpawnConfig
    -> generic or scheduled common-entity producer
    -> $03BA/$03CA runtime records
```

The remaining integration work is temporal: identify the exact frame phase at which the accepted page descriptor/profile is refreshed relative to player update, hazards and `$A442`, then compose the producer phase into the full platform-frame model.
