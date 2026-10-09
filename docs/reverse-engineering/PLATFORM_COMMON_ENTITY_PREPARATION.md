# Common entity preparation — ordinary mobile path

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room composition of the ordinary mobile route through bank-3 `$A55E-$A700` for entity types `$00-$07` entering action family `$10`.

This path prepares a common movable entity immediately before the already-modeled projectile-hit/contact interaction section.

## Order

For the covered ordinary mobile route:

1. `$A970` runs the common decision logic;
2. if that decision starts `$31/$32`, `$C5E6` consumes jump vertical motion **in the same update**;
3. horizontal movement is selected from state/facing and `$A60B` cadence;
4. camera delta `$43` is subtracted from entity screen X;
5. wrapped/edge X `$F8-$FF` enters removal;
6. Y in `$B0-$BF` enters removal;
7. when state phase is zero, the proximity/drop test may transition the entity to `$50` and add `+6` Y;
8. surviving entities reach the interaction gate at `$A700`, followed by `$9915/$98BA` on the ordinary path.

## Same-update jump start

A timer expiry in `$A970` can write action `$31` or `$32` and seed phase `1`. Control then reaches `$A5BB` in the same call, which invokes `$C5E6` immediately.

Therefore the first jump table sample is not deferred to the next frame. A newly started jump advances phase `1 -> 2` and consumes table index `0` during that same entity update.

## Horizontal cadence and camera

The entity motion step from `$A60B` is applied before subtracting camera delta `$43`.

For ordinary types:

- `($3C & 3) == 0` -> 2 px movement;
- otherwise -> 1 px movement.

The stored screen X is then:

`byte(entity_x + signed_entity_step - camera_delta_43)`

This byte arithmetic is intentionally preserved. For example, a left-moving entity at X=0 can wrap to `$FF`, after which the `$F8-$FF` removal gate fires.

## Proximity transition to `$50`

When state phase is zero, the ordinary route can enter a nearby-player transition after movement.

The transition is bypassed for ground descriptors `$E0-$EF`. Otherwise, for covered types `$00-$07`, it requires:

- player Y `< $81`;
- player 16-pixel row strictly below the entity Y;
- entity X in `$21-$BF`;
- the original asymmetric horizontal proximity test.

On the entity-right side, exactly +32 px is rejected. On the opposite side, exactly -32 px can be accepted because the ROM uses different branch conditions (`BCS` vs `BCC`).

On success:

- action state becomes `$50`;
- entity Y gains `+6`.

The entity can then continue into the later interaction section during the same update.

## Executable model

`PlatformCommonEntityPreparation.StepOrdinaryMobile` composes the existing canonical components:

- `PlatformCommonEntityDecision`;
- `PlatformCommonEntityMotion.StepJumpVertical`;
- `PlatformCommonEntityMotion.HorizontalStep` / jump horizontal delta;
- camera-relative X arithmetic;
- removal gates;
- the proximity `$50/Y+6` transition.

Special-state families `$40/$50/$70/$A0/$D0/$E0` and entity types `$08+` remain separate paths and are not generalized through this helper.

No ROM payload is embedded in the model.
