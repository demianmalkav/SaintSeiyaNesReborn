# Platform player-action dispatcher — modeled `$AAE4` routes

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the ordinary/default route plus frame-start `$20` crouch and `$50` fall/drop branches are composed in executable clean-room code. Frame-start `$40` and `$80` remain intentionally unsupported until their routines are isolated to the same evidence level.

## Dispatcher boundary

This model corresponds only to the player/action portion reached through PRG bank 3 `$AAE4`.

It **does not** yet own:

- the later platform object/projectile update pipeline;
- entity AI/contact processing;
- fixed-bank frame-counter increment `$3C` at `$C402`;
- mode/exit work that happens before `$AAE4`.

Keeping this boundary explicit is necessary because attack objects created during the player step are updated later in the same platform frame while `$3C` still has its old value.

## Frame-start snapshot

The fixed-bank platform loop copies current action `$4D` to `$4E` before player simulation.

The dispatcher therefore chooses its branch from **frame-start** `$4E`, not from action mutations that happen during the frame.

This matters for attack origin logic: `$BBCA/$BCC2` tests old `$4E` exactly against `$20` to select the lower crouching attack origin.

## Modeled branches

### Ordinary/default

All families except the explicit `$20/$40/$50/$80` branches use the ordinary composition already represented by `PlatformOrdinaryPlayerAction`.

That path preserves:

1. pre-simulation collision probes;
2. A/jump logic at `$BB76`;
3. B/attack creation at `$BBCA`;
4. same-frame airborne dispatch when current action becomes/remains `$30-$3F`;
5. otherwise grounded `$AB3F` movement/action update.

A newly created jump therefore consumes its first vertical table entry in the same update. Simultaneous A+B creates the attack first from the pre-jump coordinates, then moves the player vertically.

### Frame-start `$20` — crouch/drop

The confirmed order is:

1. sample collision descriptors before `$AAE4`;
2. run crouch/drop routine `$B829`;
3. run B attack routine `$BBCA`.

Consequences:

- Down+A may transition `$20 -> $50` and add `+6` Y before the attack is created;
- facing changes made by crouch processing are visible to the attack origin;
- attack creation sees the **new** player coordinates/current action;
- attack-origin height logic still sees the **old** `$4E==$20`, adding the crouch-specific extra 8 pixels (`player_y + 15` total instead of `+7`).

Thus a Down+A+B frame can create an attack from the post-drop player Y while retaining the frame-start crouch offset.

### Frame-start `$50` — fall/drop

The confirmed order is:

1. sample collision descriptors before `$AAE4`;
2. run fall routine `$B87D`;
3. run B attack routine `$BBCA`.

Consequences:

- the fixed `+3` fall delta happens before attack creation;
- side correction, landing snap, or lower-screen hazard transition can happen before B is evaluated;
- B height rejection at `player_y >= $90` uses the **post-fall** Y;
- a frame that begins below `$90` can cross the threshold during `$B87D`, then have B consume its latch/play sound but create no object;
- if the fall lands first, B creates from the snapped landing Y and current neutral action.

## Explicitly unsupported branches

Frame-start families `$40` and `$80` are returned as `UnsupportedSpecial40` and `UnsupportedDamage80`.

The dispatcher deliberately performs no speculative mutation for them. This prevents later code from silently treating unknown damage/special behavior as ordinary locomotion.

They are the next reverse-engineering targets for complete `$AAE4` coverage.

## Executable implementation

`PlatformPlayerActionDispatcher` composes the three confirmed dispatcher families into a common state/result type while preserving the existing component models:

- `PlatformOrdinaryPlayerAction`;
- `PlatformCrouchDrop.StepCrouched`;
- `PlatformCrouchDrop.StepFall`;
- `PlatformAttackSystem.ApplyBButton`.

The self-test suite asserts ordering-sensitive cases, including:

- crouch drop before B;
- old `$4E==$20` attack-origin offset after current action has changed;
- crouch facing update before attack origin selection;
- fall delta before B;
- fall crossing the `$90` B-height threshold;
- landing before B;
- no invented behavior for `$40/$80`.

No ROM payload is embedded in the executable model.
