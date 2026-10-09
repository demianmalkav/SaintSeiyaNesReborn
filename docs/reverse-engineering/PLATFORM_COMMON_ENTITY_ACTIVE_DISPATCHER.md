# Common entity active-state dispatcher — `$10/$30/$40/$50`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room dispatcher for common entity types `$00-$07` across the four closed active families needed for ordinary movement, jumping, surviving-hit reaction and falling.

## Why control-path continuation is separate from action state

The original branches on the action family near `$A55E`, but the value stored back into the action byte during an update does not by itself determine whether the current control path reaches `$9915/$98BA` later that same update.

Two important cases demonstrate this:

- terminal `$4F` reaction stores `$10`, then continues through `$A845`/renderer path and skips ordinary interaction;
- `$50` can land through `$C491` and store `$10`, but the `$A57E` route still jumps directly to renderer and skips ordinary interaction.

Therefore the clean-room dispatcher returns both:

- the resulting action/state;
- an explicit continuation: `ReadyForInteraction`, `SkipInteraction`, or `Removed`.

Inferring interaction solely from the final action byte would be incorrect.

## Family `$10` — ordinary mobile

Delegates to `PlatformCommonEntityPreparation.StepOrdinaryMobile`:

- common decision `$A970`;
- possible same-update jump start;
- movement/camera;
- removal gates;
- proximity `$50/Y+6` transition.

Survivors reach the ordinary interaction section.

## Family `$30` — active jump

An already-active `$3x` state skips `$A970` and enters `$C5E6` directly.

After vertical motion:

- exact `$31` moves right using `$A60B` cadence and then applies camera `$43`;
- exact `$32` moves left and then applies camera;
- other `$3x` values jump directly to the post-X gate and do not apply camera in that branch;
- a same-update `$C491` landing can return action to `$10`; the remainder of the current `$A5BE` path then performs ordinary facing movement before camera subtraction.

The route can still reach ordinary projectile/contact interaction after a landing or while jumping.

## Family `$40` — hit reaction

Delegates to `PlatformCommonEntityHitReaction40` and always returns `SkipInteraction` for the current update, even if `$4F` stores `$10`.

## Family `$50` — fall

Delegates to `PlatformCommonEntityFall50`.

- lower-band removal returns `Removed`;
- falling or same-update landing returns `SkipInteraction` because the original `$A57E` path jumps to renderer.

## Scope

`PlatformCommonEntityActiveDispatcher` intentionally rejects other action families. `$00/$70/$A0/$D0/$E0` and type `$08+` behavior remain separate reverse-engineering targets rather than being approximated through the nearest known path.

No ROM payload is embedded in the model.
