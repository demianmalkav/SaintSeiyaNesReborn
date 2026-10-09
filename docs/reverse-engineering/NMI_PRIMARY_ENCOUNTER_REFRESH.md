# NMI primary encounter refresh phase

Status: **CONFIRMED by static ROM flow** for the execution boundary described here.

The primary encounter refresh is not part of the same bank-0/bank-1 producer block that creates common entities. It is reached from the fixed NMI state dispatcher.

## NMI context

Fixed handler `$D269` saves registers and dispatches global state. In the `$00 == $20` branch:

```text
$D2BA  JSR $D7F2
$D2BD  JSR $D988
$D2C0  JMP $D367
```

`$D7F2` is therefore an NMI-side operation associated with platform global state `$20`.

`$D7F2` contains the refresh invocation gate documented in `PRIMARY_ENCOUNTER_REFRESH_GATE.md`; eligible calls reach bank-1 `$996C`, whose safe-acceptance semantics are documented in `PRIMARY_ENCOUNTER_ACCEPTANCE.md`.

## Clean-room phase

`PlatformNmiPrimaryEncounterRefreshPhase.StepPlatformState20` composes only this NMI-side chain:

```text
$44/$07C0/$03A4 refresh gate
       |
       +-- suppressed ----------------> preserve active latch
       |
       `-- acceptance eligible
              |
              +-- select stage page with $45
              `-- PlatformPrimaryEncounterAcceptance
                     -> unchanged / deferred / accepted-zero / accepted-nonzero
```

The result keeps the staged/current page encounter separate from the active `$58`/profile. This preserves the original possibility that the camera has entered a new page while the previous encounter remains active because the common slots are not yet safe to replace.

## Separation from entity production

Static fixed-bank flow for the active platform main-thread update shows a separate producer chain:

```text
$C2CE -> map bank 0 -> $B3E2
$C30A -> $B6D0            generic common edge producer
$C30D -> map bank 1
$C312 -> $969D
$C319 -> $8000            bank-1 frame dispatcher
             ...
             JSR $8927    scheduled special producer
...
$C322 -> $C52F            platform damage
$C327 -> map bank 3
$C32A -> $AAE4            player update
```

Therefore the clean-room implementation deliberately keeps:

- NMI-side encounter refresh/acceptance; and
- main-thread common-entity production

as distinct phases.

The exact synchronization boundary between one NMI refresh result and the following/main-thread producer invocation should be represented explicitly rather than collapsing both into a single unordered "frame" method.

## Consequence

The project now has all semantic pieces needed for a latched encounter pipeline:

1. stage page exposes the newest descriptor;
2. NMI gate decides whether acceptance is attempted;
3. `$996C` may keep the previous `$58`/profile active;
4. main-thread producers consume the active configuration;
5. only after producer work do damage/player/entity interaction phases continue.

This distinction prevents REBORN from spawning the next page's enemies prematurely at a camera-page boundary.
