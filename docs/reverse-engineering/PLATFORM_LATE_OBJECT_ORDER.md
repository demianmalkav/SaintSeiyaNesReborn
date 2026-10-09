# Platform late-object ordering

Status: **CONFIRMED for the composed slice represented by `PlatformHazardAndCommonEntityFrameSlice`**.

The fixed-bank late platform dispatcher reaches multiple bank-3 object classes while `$3C` still contains the current frame value. The subset promoted so far is now composed with one shared mutable state instead of being tested in isolation.

## Closed order

For the currently implemented classes:

```text
bank-1 pre-player resource / attack-busy phase
    -> $AAE4 player action
    -> $B94B shared $76 decrement
    -> [$9B93 common-entity spawn/setup: NOT YET promoted]
    -> $96B4 auxiliary hazard spawn
    -> $9761 auxiliary slot A
    -> $9761 auxiliary slot B
    -> $A442 common entity A
    -> $A442 common entity B
    -> $A22C player attack-object update
    -> $C402 increment $3C
```

`PlatformHazardAndCommonEntityFrameSlice` starts from common entity records supplied by the caller; those records are assumed to represent the state after the still-unpromoted `$9B93` phase.

## One physical attack state

Player projectiles are not copied independently for each subsystem. The same `PlatformAttackState` is threaded through:

```text
player/B creation
 -> auxiliary A
 -> auxiliary B
 -> common A
 -> common B
 -> A22C movement/range update
```

This preserves observable ordering. A Hyoga/Shiryu projectile consumed by an auxiliary hazard is already inactive when the later common entity reaches `$9915`.

The converse control fixture is also preserved: when no auxiliary slot exists, that same projectile remains active and can hit common entity A.

## One physical `$76/$7F/$80` state

The same contact state is threaded through all late object classes.

An auxiliary contact can seed:

```text
$76 = $20
$7F = auxiliary Life drain count
$80 = auxiliary Cosmo drain count
```

before `$A442`. A later common entity overlap sees the nonzero `$76` and is rejected at the contact-latch gate; it cannot overwrite the already seeded drain counters.

The composed frame keeps the player-domain `Special76` view synchronized with `PlatformContactPhaseState.HazardLatch76` after each class boundary.

## Contact/hit ordering remains class-specific

Composition does not flatten internal class semantics:

- auxiliary ordinary hazards: `contact -> projectile hit`;
- common entities: `projectile hit -> contact`.

The two sequences coexist inside the larger frame in their original positions.

## Newly spawned auxiliary hazards

Because `$96B4` precedes `$9761`, a hazard created during the current frame is immediately eligible for its first update and interaction pass. No artificial one-frame spawn delay is introduced.

## Late player attack update

`$A22C` is executed only once after both auxiliary and common entity classes. Therefore:

- collision uses each projectile's pre-`$A22C` position;
- any projectile surviving all earlier interactions moves/consumes range only afterward;
- a projectile retired by any earlier class stays retired;
- `$3C` is still the old/current-frame value during that update.

`$C402` then increments `$3C` exactly once on the normal path.

## Exceptional `$80` player reload

When `$AAE4` takes the `$80/$76==0` exceptional reload path, it does not return into the normal late-object pipeline. The composed model therefore skips auxiliary hazards, common entities, `$A22C`, and the normal `$C402` increment.

## Remaining gap

The major earlier object-phase gap is now `$9B93`, which prepares/spawns common records before `$96B4`. Promoting that routine will allow the composed slice to own entity population as well as entity simulation, removing the current requirement that the caller supply post-`$9B93` records.
