# Ordinary mobile entity combat slice — player first, entity AI second

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition for one common entity type `$00-$07` entering action family `$10`, now including its ordinary `$A442` preparation before projectile/contact interaction.

## Extended frame order

The covered slice executes:

1. pre-player busy/resource drain;
2. post-drain attack damage derivation;
3. `$AAE4` player simulation and B attack creation;
4. `$B94B` shared `$76` decrement;
5. ordinary common-entity preparation `$A55E-$A700`;
6. projectile-vs-entity `$9915/$992A`;
7. entity-vs-player contact `$98BA`;
8. player attack/projectile updater `$A22C`;
9. fixed `$C402` frame-counter increment.

The key new boundary is step 5: the entity decision receives the player's coordinates **after** the player's own same-frame movement.

## Entity AI observes same-frame player movement

A discriminating fixture starts with both player and entity at screen X `$40`.

The entity has decision timer `1` and faces right. Against the player's frame-entry coordinate `$40`, the exact `$A970/$A999` decision would expire the timer without starting a jump.

The player then presses Right. `$AAE4` moves the player to `$41` before `$A442` runs.

Now the entity is left of the player while already facing right, so `$A970` selects jump-right `$31`. The same entity update immediately reaches `$C5E6`, consumes the first vertical sample, advances phase `1 -> 2`, moves Y upward by 8 pixels, and applies the `$31` horizontal step.

Therefore the correct causal order is:

`player move -> entity decision -> entity jump sample`

not an engine-wide simultaneous snapshot of player/entity positions.

## Unified common-entity runtime state

Earlier clean-room components expose different semantic views of the same original 16-byte entity record:

- `PlatformCommonEntityMotionState` for AI/motion fields;
- `PlatformCombatEntity` for projectile-hit/combat fields.

`PlatformCommonEntityRuntimeState` now joins these views with HP, Life/Cosmo contact-drain counts and Seventh Sense reward. Conversion into combat view and back explicitly synchronizes the shared fields:

- action/state byte;
- X/Y;
- record offset `+$03` phase/motion byte;
- type;
- terrain probes;
- HP/reward.

Ground descriptor, decision timer and facing remain owned by the motion view unless a later reconstructed subsystem proves a write to them.

## Entity removal is local, not a frame abort

The ordinary preparation path can remove the current entity when post-move X enters `$F8-$FF` or Y enters `$B0-$BF`.

That ends the current entity update; it does **not** take the player's exceptional `$80` reload path and does not abort the platform frame.

The self-test therefore starts an entity at X=0 moving left. Its byte X wraps to `$FF` and it is removed before `$9915/$98BA`, but an existing player projectile still reaches later `$A22C`, moves +5, ages its range, and `$C402` still increments `$3C`.

## Explicit camera delta

The slice accepts `cameraDelta43` explicitly. `$43` is a transient frame delta consumed by entity movement; it is not yet safe to reconstruct it indirectly from end-of-player X/scroll values in every movement mode. Keeping the original semantic input explicit avoids introducing a false equivalence while the producer of `$43` is promoted separately.

## Scope boundary

This slice currently covers only ordinary mobile types `$00-$07` entering family `$10`. It does not generalize:

- hit reaction `$40`;
- fall/drop `$50`;
- timed `$70`;
- special `$A0/$D0/$E0` families;
- types `$08+`;
- secondary/special object classes that precede `$A442`.

No ROM payload is embedded in the model.
