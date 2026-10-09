# Platform multi-slot hit ordering and repeated-death quirk

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: `$9915` three-slot ordering is statically confirmed. Combined with the already reconstructed HP death path, it implies and the clean-room model preserves a same-call repeated-death/reward quirk when multiple attack slots overlap one entity.

## Slot order

`$9915` checks attacks in this exact order:

1. helper `$9A3F` -> attack record `$0730` / slot 0 -> `$992A`;
2. helper `$9A4C` -> `$0738` / slot 1 -> `$992A`;
3. helper `$9A59` -> `$0740` / slot 2, then execution falls directly into `$992A`.

There is no entity-state check inserted between these calls inside `$9915`.

Each later slot therefore sees the entity fields as mutated by the previous slot.

## Important death-path property

The ordinary HP death path at `$99BA+`:

- detects zero/borrow;
- awards Seventh Sense;
- writes death state `$D0` (or `$A0` for type `$0D`);
- does **not** store zero back into the entity HP byte.

This is harmless for one projectile, but interacts with `$9915` multi-slot ordering.

## Repeated death/reward case

Example with three overlapping attacks, entity HP 15 and damage 10:

### Slot 0

`15 - 10 = 5`

Positive result:

- HP byte becomes 5;
- state becomes reaction `$40`.

### Slot 1

`5 - 10` borrows:

- death path runs;
- reward is added;
- state becomes death state;
- HP byte remains 5.

### Slot 2

`$9915` still invokes the hit routine for slot 2. Geometry can still overlap because death changed state but not coordinates.

The HP calculation again sees stored HP 5:

- death path runs again;
- reward can be added again.

Thus the original routine structurally permits **repeated death/reward entry in one `$9915` call**.

This is especially relevant to Saints able to maintain multiple simultaneous attack records:

- Shiryu can own slots 2/1/0;
- Ikki can own slots 1/0;
- Shun's `$54/$55` chain can supply two accepted hit records.

The Saint-specific `$9A27` helper may consume individual current slots, but it does not stop the later slot calls.

## Immediate-kill example

If an entity HP equals one attack's damage and two Ikki slots overlap:

1. slot 0 kills and awards reward;
2. HP byte remains its pre-kill value;
3. slot 1 enters the same kill path and can award the reward again.

Ikki's standard hit helper does not consume the attack, but consumption is not required for the repeated-death effect — only multiple accepted slots are.

## Mutation can also suppress later hits

Not every earlier hit makes later hits equivalent.

Some non-HP reactions modify position. For example, type `$01-$04` response `$99AA` adds 6 to entity Y and writes state `$E0`.

Because slot 1/2 geometry is evaluated afterward against the mutated coordinates, a first hit can move the entity out of a later slot's hit rectangle.

Therefore the clean-room sequence must be:

```text
slot0 overlap/route -> mutate entity
slot1 overlap/route -> mutate entity
slot2 overlap/route -> mutate entity
```

not “compute all overlaps first, then apply effects.”

## Clean-room implementation

`PlatformProjectileHitSequence.ResolveThreeSlots(...)`:

- iterates slot 0, 1, 2;
- calls the full hit router for each slot;
- writes back the possibly consumed attack record;
- feeds the mutated entity into the next slot;
- feeds updated Seventh Sense into the next slot;
- returns all three per-slot results for parity/debugging.

No attempt is made to sanitize repeated death rewards in ORIGINAL SPEC.

## REBORN policy boundary

This is a good example of why ORIGINAL SPEC and REBORN must remain separate.

Compatibility layer:

- preserve the observed order and quirk.

Modern REBORN design can later choose whether to:

- preserve repeated same-frame death rewards for exact compatibility;
- suppress rewards after the first death while still allowing visual multi-hit;
- redesign multi-projectile collision entirely.

That decision should be explicit and testable rather than accidentally introduced during porting.
