# Entity drain byte semantics: `+$0D` Cosmos, `+$0E` Life

Status: **CONFIRMED by two independent ROM consumers plus the source data table layout**.

A naming inversion was found in the early C# spawn profile. The semantic mapping is:

```text
entity +$0C = HP
entity +$0D = Cosmo drain ticks
entity +$0E = Life drain ticks
entity +$0F = Seventh Sense reward (packed BCD)
```

The private/local stage extractor had already decoded the four-byte stat table in this order (`hp, cosmo, life, reward`). The error was introduced later when those semantic values were converted back into the clean-room entity-record profile.

## Direct runtime proof: ordinary contact `$98BA`

At `$9900-$990B`, after the contact geometry and `$76` latch checks succeed, the original copies:

```text
LDY #$0E
LDA ($16),Y
STA $7F
DEY              ; Y = $0D
LDA ($16),Y
STA $80
```

The previously reconstructed resource drain engine establishes:

```text
$7F = pending Life-drain ticks   ($927A, -2 Life per consumed tick)
$80 = pending Cosmo-drain ticks  ($930A, -1 Cosmo per consumed tick)
```

Therefore:

```text
+$0E -> $7F -> Life drain
+$0D -> $80 -> Cosmo drain
```

## Independent runtime proof: attached hazard `$AA70`

The attached subobject collision path repeats the same writes at `$AAC4-$AACD`:

```text
LDY #$0E
LDA ($16),Y
STA $7F
DEY
LDA ($16),Y
STA $80
```

This independently confirms that the mapping is not specific to one caller.

## Clean-room correction

`PlatformSpecialSpawnProfile` now names its raw record bytes explicitly as:

```text
CosmoDrainTicks0D
LifeDrainTicks0E
```

Both primary producers map these into the semantic runtime record as:

```text
PlatformCommonEntityRuntimeState.CosmoDrainTicks <- +$0D
PlatformCommonEntityRuntimeState.LifeDrainTicks  <- +$0E
```

`PlatformPrimaryEncounterSpawnConfig.FromEncounter(...)` likewise preserves the semantic `PlatformEncounterStats` order when rebuilding the raw profile.

## Regression containment

Tests use deliberately different values for Life and Cosmos drain. Equal values can hide this class of offset inversion, so parity fixtures should avoid equal sentinel values when validating aliased/raw record layouts.
