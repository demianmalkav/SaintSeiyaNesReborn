# Common entity fall family `$50`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: executable clean-room model for common entity types `$00-$07`.

## Exact path

At bank-3 `$A55E`, action family `$50` branches to `$A57E`.

For this path the engine:

1. subtracts global camera delta `$43` from entity X;
2. adds exactly `+3` to entity Y;
3. if resulting Y is `>= $B0`, removes the entity and returns;
4. otherwise calls fixed `$C491` landing resolution;
5. jumps to the renderer path rather than the ordinary `$9915/$98BA` interaction sequence.

Thus `$50` is a self-contained fall/landing family for the common entity route, not ordinary walking with a different animation.

## Shared `$C491` landing primitive

`PlatformCommonEntityLanding.Resolve` is now the single promoted semantic implementation of `$C491` used by both:

- jump vertical motion after its landing-check phase begins;
- the `$50` fall path after its fixed `+3` Y step.

This prevents the jump and fall clean-room models from drifting on descriptor thresholds or snap rules.

For normal common types `<$08`, an accepted landing:

- snaps Y according to descriptor class;
- clears record `+$03` phase;
- returns action to `$10`.

## Order matters at the lower boundary

The `$B0` removal test happens after `Y += 3` and before `$C491`.

An entity beginning at `Y=$AD` reaches exactly `$B0` and is removed even if its floor descriptor would otherwise be landing-compatible. The clean-room model preserves that ordering.

## Net vertical delta can differ from +3

Because landing is checked after the fixed fall delta, the visible net change can be smaller than +3.

Example: starting at `Y=$5F` with a normal landing descriptor:

- fall step: `$5F -> $62`;
- `$C491` snap: `$62 -> $60`;
- net screen delta: `+1`.

## Interaction consequence

Like the common `$40` reaction path, `$50` branches away from ordinary projectile-hit/player-contact calls for that update. It does not process `$9915/$98BA` while following this path.

No ROM payload is embedded in the model.
