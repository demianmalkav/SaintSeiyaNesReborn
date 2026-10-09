# Ordinary platform player action — `$AAE4` default route

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the ordinary/default player route through `$AAE4`, `$BB76`, `$BBCA`, `$BCD3` and `$AB3F` is composed into one clean-room semantic step. This step intentionally stops before the later attack-object/entity pipeline and before the fixed-bank frame-counter increment.

## Dispatcher boundary

At entry, `$AAE4` uses the frame-start action snapshot `$4E` to dispatch special families before the default route.

The ordinary compositor therefore accepts neutral/locomotion/jump-family starts but explicitly rejects frame-start families handled by other branches:

- `$20` crouch -> `$B829`;
- `$40` special path;
- `$50` fall/drop -> `$B87D`;
- `$80` damage/hazard path.

Those routes already have separate clean-room primitives and will be composed at the higher full-player dispatcher layer.

## Exact default-route order

The reconstructed order is:

1. retain frame-start `$4E` (old `$4D`);
2. consume the already prepared collision probes;
3. run A/jump logic `$BB76`;
4. run B/attack creation `$BBCA`;
5. inspect the **updated** `$4D`;
6. if family `$30-$3F`, copy the new action into the current frame path and execute `$BCD3` immediately;
7. otherwise execute `$AB3F` grounded movement/action logic.

The clean-room class samples the stage at step 2 because collision preparation occurs earlier in the active platform frame; all later movement in this player step uses those pre-simulation descriptors.

## A+B ordering

A simultaneous fresh A+B press exposes an important original ordering.

A can first create:

- `$49=1`;
- `$4D=$30-$33`;
- `$4A=1`;
- optional high-jump selector `$038A`.

B attack creation then runs **before** the caller dispatches the newly created jump to `$BCD3`.

Therefore the attack uses:

- player X/Y from before the jump displacement;
- facing from before the later grounded/air-control direction work;
- the **old frame-start `$4E`** for attack-origin/action-reset semantics;
- the **new `$49=1`** from the just-created jump.

Only after attack creation does the first jump-table displacement execute in that same frame.

This is reproduced by `PlatformOrdinaryPlayerAction` and explicitly tested.

## Grounded `$AB3F` action side effects

When no jump-family action is active after A/B processing, grounded horizontal movement runs.

Directional priority:

1. Right;
2. otherwise Left.

Action-state behavior:

- ordinary movement or camera handoff -> family `$10`;
- terrain collision while a direction is held -> still family `$10`;
- hard local screen boundary (left edge or final right edge) -> `$4D=0`;
- Down in the common tail overrides the preceding result -> `$4D=$20`;
- if none of Right/Left/Down is held, the common tail clears `$4D=0`;
- setting family `$10` preserves the low nibble: `($4D & $0F) | $10`.

This explains why a blocked walking animation/state can persist even though X did not change.

## Moving attack interaction

`PlatformAttackSystem.ApplyBButton` already reproduces the original rule that a successful B attack can clear a frame-start locomotion-family action when `$49==0`.

The default route then continues into `$AB3F`:

- B only from frame-start `$10` -> attack clears action, grounded tail remains neutral;
- B+Right -> attack first clears the old locomotion state, then `$AB3F` moves right and re-enters `$10`.

The two effects are deliberately not collapsed into one rule.

## Frame ownership boundary

`PlatformOrdinaryPlayerAction` **does not**:

- advance attack projectiles/chains;
- advance attack busy `$4B`;
- update enemies/secondary/special objects;
- stream/render the map;
- increment global frame counter `$3C`.

Those operations occur later in the real active-platform frame. Advancing `$3C` here would make projectile parity and other downstream systems one frame early.

Accordingly `frameCounter3C` is input-only in this class.

## Clean-room result surface

The result exposes:

- frame-start action `$4E`;
- pre-simulation collision descriptors;
- jump-initiation result;
- attack-attempt result;
- selected route (`Grounded` or `Airborne`);
- airborne vertical/horizontal subresults when applicable;
- grounded horizontal subresult otherwise;
- final player/action/attack state at the end of the player-action portion of the frame.

This is the correct layer on which to build the future full `$AAE4` dispatcher without contaminating object-pipeline timing.
