# Common entity death family `$D0-$DF`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room model for ordinary common entity types `$00-$07`, integrated into the active dispatcher and two-slot combat slice.

## Entry

The ordinary projectile kill path stores `$D0` for most common enemy types. On later frames `$A55E` routes family `$D0` through the non-ordinary path; it does not return to `$9915/$98BA` interaction while dying.

## Per-frame camera behavior

Before death-phase timing, the entity's screen X is corrected by global camera delta `$43` every update:

`entity_x = byte(entity_x - $43)`

Normal X/Y removal gates still run before the later death progression logic.

## Death-phase cadence

For common types `<$08`, `$A7FB-$A839` advances `$D0-$DF` only when:

`($3C & 3) == 0`

On other frames the action state and death-specific Y movement remain unchanged, though camera X correction has already happened.

On cadence frames:

1. selected ground-descriptor classes may add `+2` Y;
2. action state increments by one;
3. attempting to advance beyond `$DF` clears the action and removes the entity.

Thus the visual/state death sequence progresses at one quarter of the platform update cadence.

## Descriptor-dependent Y drift

The `+2 Y` death drift occurs when:

- ground descriptor `< $80`, or
- ground descriptor `>= $F0`.

Descriptors `$80-$EF` suppress that Y drift while the `$D0-$DF` phase still advances.

## Terminal removal

On a cadence tick with action `$DF`:

- descriptor-dependent Y drift happens first;
- action increment reaches `$E0`;
- the routine clears action to `$00`;
- removal helper is entered.

The clean-room result reports this as `CompletedRemoval`.

## Kill -> D0 -> next-frame continuity

The integrated self-test demonstrates a full cross-frame chain:

Frame N:

- Hyoga projectile deals lethal damage;
- kill path awards the entity's packed-BCD Seventh Sense reward once;
- slot A stores `$D0`;
- projectile is consumed.

Frame N+1:

- the same slot is dispatched through `DeathD0`;
- camera/death cadence is applied;
- `$9915/$98BA` are skipped;
- no additional Seventh Sense reward is generated.

This closes the ordinary kill-to-removal lifecycle far enough for multi-frame combat simulation.

No ROM payload is embedded in the model.
