# Primary encounter safe-acceptance latch `$996C-$9A2D`

Status: **CONFIRMED by static ROM flow**.

The per-page descriptor is not copied immediately into active `$58`. Bank 1 stages the current page byte in `$03B7`, compares it with `$58`, and only accepts a changed descriptor when both common slots are in a safe condition.

## Page staging

`$02 * 2` indexes the primary-encounter pointer table at `$9AE5`; `$45` selects the page entry. The selected byte is written to `$03B7`.

```text
staged = encounter_table[$02][$45]
$03B7 = staged
```

If `staged == $58`, no profile work is repeated. Control jumps through `$9A2E` to `$9BB9`.

## Safety gate for changed descriptors

When `$03B7 != $58`, the routine accepts the new descriptor only if all four checks pass:

```text
$0749 == $FE                 ; slot A visual +1 free
($03BA & $F0) != $40        ; slot A not hit reaction
($03BA & $F0) != $D0        ; slot A not death

$077D == $FE                 ; slot B visual +1 free
($03CA & $F0) != $40        ; slot B not hit reaction
($03CA & $F0) != $D0        ; slot B not death
```

Failure of any check jumps to `$9A2E -> $9BB9`. There is no internal wait loop: the changed page descriptor remains staged in `$03B7`, active `$58` and its profile remain unchanged, and acceptance can be retried on a later platform update.

This explains why page configuration and entity instantiation are temporally decoupled.

## Accepting a new descriptor

When safe:

```text
$58 = $03B7
```

### Nonzero

A nonzero accepted descriptor rebuilds:

- sprite/palette configuration;
- tier auxiliary `$03AB`;
- `$03AE` HP;
- `$03AD` Life-drain ticks;
- `$03AC` Cosmo-drain ticks;
- `$03AF` Seventh-Sense reward BCD.

The routine then returns at `$9A2D`.

### Zero

If the accepted byte is zero, `$58` is cleared but profile bytes are not recomputed. Control instead branches back to `$9986`, which jumps `$9A2E -> $9BB9`.

The clean-room state therefore treats zero as “no active primary encounter config” rather than inventing a zeroed stat profile.

## Control-flow summary

```text
stage current page -> $03B7
          |
          +-- equals active $58 --------------------> $9BB9
          |
          +-- changed but either slot unsafe ------> keep old $58/profile -> $9BB9
          |
          +-- changed + safe + zero ---------------> $58=0 -> $9BB9
          |
          `-- changed + safe + nonzero ------------> $58=new
                                                     rebuild profile
                                                     RTS
```

## Clean-room representation

`PlatformPrimaryEncounterAcceptance` stores active `$58` together with the semantic `PlatformPrimaryEncounterSpawnConfig` that corresponds to its profile. It distinguishes:

- `Unchanged`;
- `DeferredUnsafe`;
- `AcceptedZero`;
- `AcceptedNonzero`.

The result also exposes whether control continues to `$9BB9` and whether profile data was recomputed, because those are real control-flow differences rather than presentation details.

The next integration target is to put this latch before `PlatformPageEncounterSpawnPhase`, so spawners consume the **accepted** encounter rather than blindly consuming the newest page descriptor.
