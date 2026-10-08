# Platform contact, drain and reward

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: static reconstruction. Ordinary entity contact, post-contact counters, Life/Cosmo drain arithmetic and enemy-death Seventh Sense reward are directly tied to code. A separate periodic environmental Life-drain path is documented separately because it shares the same subtraction helper but is not caused by `$7F`.

## Entity contact -> deferred drain

Bank 3 `$98BA+` tests a logical entity against the player's collision region. If overlap is valid and `$76 == 0`:

- entity record `+0E` -> `$7F`;
- entity record `+0D` -> `$80`;
- `$76 = $20`;
- sound/effect `$26` is requested.

Therefore contact damage is not represented solely as one immediate HP subtraction. The entity supplies two **tick counts**:

- `$7F` = remaining Life-drain ticks;
- `$80` = remaining Cosmo-drain ticks.

`$76` gates immediate repeat contact for the ordinary path. The same RAM byte is reused by at least one special floor/hazard path, so its generic engine meaning is a temporary contact/hazard latch/timer rather than a universally pure 'invulnerability' variable.

## Life drain — bank 1 `$927A+`

The ordinary contact-counter path is:

1. if `$7F == 0`, no contact Life drain is applied;
2. otherwise decrement `$7F` by one;
3. subtract **2 Life** from the current Saint's packed-decimal Life value.

The BCD subtraction helper is `$9299+` and operates on the current internal Saint selected by `$03`.

Thus an entity with `record[+0E] = N` schedules approximately `2 × N` Life points of drain, subject to the frame path continuing to call this routine and to the separate special-case branch described below.

If Life reaches zero, the helper enters the platform death transition: the current Life pair is zeroed, engine state changes, action state becomes `$D0`, player attack objects are hidden, and death sound/effect `$27` is requested.

## Cosmo drain — bank 1 `$930A+`

This path is simpler:

1. if `$80 == 0`, return;
2. decrement `$80` by one;
3. subtract **1 Cosmo** from the current Saint's packed-decimal Cosmo.

So an entity with `record[+0D] = N` schedules `N` Cosmo points of drain.

If Cosmo underflows to zero through this path, the same platform death transition is reached.

## Separate periodic Life drain when `$02 == $10`

`$927A` contains an additional path that is independent of `$7F`.

When engine/substage state `$02 == $10`, the routine periodically branches directly to the `subtract 2 Life` helper even if no contact-drain ticks are pending.

Cadence is based on frame-like byte `$3C`:

- internal Saint index `1` (Shun): subtract 2 Life when `($3C & 7) == 0` — once every 8 counts;
- all other Saints: subtract 2 Life when `($3C & 31) == 0` — once every 32 counts.

On intervening counts, the routine can still consume `$7F` contact ticks normally.

This is a distinct environmental/special-mode attrition mechanic. Its narrative/gameplay context is not yet named; do not describe it as enemy damage until `$02 == $10` is tied to a specific scene or environment.

The fourfold faster Life attrition for Shun is a notable character-specific rule and should be preserved in ORIGINAL SPEC.

## Contact timer `$76`

Ordinary entity contact writes `$76 = $20` (32). Bank 3 player-update code decrements non-zero `$76` once per update path.

However `$76` is also written by special floor/hazard logic — including a `$F8/$F9` floor-descriptor path that can seed it with `1`, and another hazard path that sets `$80`.

Therefore:

- `32` is a confirmed ordinary-contact cooldown/invulnerability duration;
- the RAM byte itself is a reused contact/hazard timer/latch, not a type-safe modern variable.

REBORN should split these overloaded meanings into explicit fields even if ORIGINAL SPEC keeps the aliasing documented.

## Enemy death -> Seventh Sense reward

Projectile damage at bank 3 `$99BA+` uses entity record `+0C` as HP. When damage reaches or crosses zero:

1. entity record `+0F` is loaded into A;
2. fixed routine `$D1E0` is called;
3. `$D1E0` adds that packed-decimal amount to `$05AA/$05AB`;
4. result is clamped to `$9999` if the four-digit BCD value would exceed the valid range.

`$05AA/$05AB` is independently confirmed as Seventh Sense. Therefore entity offset `+0F` is a **Seventh Sense reward amount** in this death path.

There is one mode gate: `$D1E0` immediately returns when `$02 == 0`. In other states, the reward is applied.

This promotes entity `+0F` from a generic death/reward field to `seventh_sense_reward_bcd` for the ordinary platform enemy-death path.

## Clean semantic model for REBORN

The 1988 implementation aliases several concepts into a handful of RAM bytes. A native model should separate:

- `contactCooldown`;
- `remainingLifeDrainTicks`;
- `remainingCosmoDrainTicks`;
- special environmental attrition;
- hazard state;
- enemy HP;
- Seventh Sense reward.

That preserves the original equations and timings without carrying forward accidental RAM aliasing as architecture.
