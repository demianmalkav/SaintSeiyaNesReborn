# Platform player-action dispatcher — modeled `$AAE4` routes

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: **all observed `$AAE4` frame-start action branches are now structurally modeled**: ordinary/default, `$20` crouch, `$40` special cycle, `$50` fall/drop and `$80` hazard/reinitialization.

## Dispatcher boundary

This model corresponds only to the player/action portion reached through PRG bank 3 `$AAE4`.

It does **not** yet own:

- the later platform object/projectile update pipeline;
- entity AI/contact processing;
- fixed-bank frame-counter increment `$3C` at `$C402`;
- mode/exit work that happens before `$AAE4`.

Keeping this boundary explicit is necessary because attack objects created during the player step are updated later in the same platform frame while `$3C` still has its old value.

The `$80` branch is exceptional: when its latch has expired, `$AAE4` never returns to this normal continuation. The engine snapshots resources, resets global mode/render/stack state and jumps to `$C180`.

## Frame-start snapshot

The fixed-bank platform loop copies current action `$4D` to `$4E` before player simulation.

The dispatcher therefore chooses its branch from **frame-start** `$4E`, not from action mutations that happen during the frame.

This matters for attack origin logic: `$BBCA/$BCC2` tests old `$4E` exactly against `$20` to select the lower crouching attack origin.

## Modeled branches

### Ordinary/default

All families except the explicit `$20/$40/$50/$80` branches use the ordinary composition represented by `PlatformOrdinaryPlayerAction`.

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

### Frame-start `$40-$4F` — fixed-bank special cycle

`$AAE4` performs exactly one call to fixed-bank `$C5CC` and then returns. It does **not** run B/attack creation, grounded movement or airborne jump logic.

`$C5CC` is a compact 16-state cycle driven entirely by frame-start `$4E`:

- for `$40-$4E`: current action becomes `frame_start_action + 1` and player Y increments by one;
- for `$4F`: current action resets to `$00` and player Y subtracts `$0F`.

Therefore a complete `$40 -> ... -> $4F` sequence accumulates fifteen downward pixels, then restores those fifteen pixels in one final upward snap. `$C5CC` touches only `$4D` and the low player-Y byte `$40`; it does not update the Y page/high byte `$41`.

Input is ignored by this branch. In particular, B does not update its latch or busy state here because `$BBCA` is never reached.

The clean-room model is `PlatformSpecial40Motion`.

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

### Frame-start `$80-$8F` — hazard latch and reinitialization

The exact `$AAE4` branch is:

```text
if (($4E & $F0) == $80) {
    if ($76 != 0)
        return;

    JSR $CA94;
    $00 = 0;
    $01 = 0;
    JSR $C154;
    SP = $FF;
    JMP $C180;
}
```

No A/B/movement logic is run in either subpath.

#### `$76 != 0`: waiting/frozen player branch

The routine returns immediately. `$AAE4` itself does not decrement `$76` and does not mutate player position/action or B latch state.

This is important because `$76` is an overloaded contact/hazard latch/timer maintained elsewhere. A lower-screen fall-out path already reconstructed in `PlatformCrouchDrop` sets:

- `player_y = $A0`;
- `$4D = $80`;
- `$76 = $80`.

The `$80` dispatcher then waits until external timer processing has reduced `$76` to zero.

#### `$76 == 0`: exceptional reinitialization branch

This path does not return to the rest of the active platform frame.

1. `$CA94` maps PRG bank 1 and calls `$951F`;
2. `$951F` snapshots live Saint resources `$0059-$0071` to persistent `$058C-$05A4`;
3. resource order changes from internal platform `[Seiya, Shun, Hyoga, Shiryu, Ikki]` to canonical/persistent `[Seiya, Hyoga, Shun, Shiryu, Ikki]`;
4. `$00` and `$01` are cleared;
5. `$C154` writes zero to PPU control/mask (`$2000/$2001`), disabling rendering;
6. `LDX #$FF / TXS` resets the CPU stack pointer;
7. execution jumps directly to fixed `$C180`.

Because `$02/$03` are not cleared in this branch, this is best modeled as a **platform/main-loop reinitialization preserving stage/Saint selection context**, not as the ordinary player-death state. The true resource-depletion death path elsewhere uses action family `$D0`.

`PlatformDamage80Transition` represents the gate and exceptional exit. `PlatformPersistentResourceSnapshot` models the exact `$951F` resource-copy semantics separately so REBORN does not have to preserve the original RAM aliasing.

## Persistent resource snapshot `$951F`

Live platform resources are five records in internal order:

`[Seiya, Shun, Hyoga, Shiryu, Ikki]`

Each record is:

`[Life low-two BCD digits, Life hundreds, Cosmo low-two BCD digits, Cosmo hundreds, cap byte]`

`$951F` writes the 25 persistent bytes at `$058C-$05A4` in canonical/high-level order:

`[Seiya, Hyoga, Shun, Shiryu, Ikki]`

This is the same canonical order used by the higher-level selector and persistent save/password structures (with Ikki later excluded from password serialization).

The clean-room helper `PlatformResourceSnapshot.Capture` performs only this semantic reorder/copy; no original resource payload is embedded.

## Executable implementation

`PlatformPlayerActionDispatcher` now composes every observed `$AAE4` branch through:

- `PlatformOrdinaryPlayerAction`;
- `PlatformCrouchDrop.StepCrouched`;
- `PlatformSpecial40Motion`;
- `PlatformCrouchDrop.StepFall`;
- `PlatformDamage80Transition`;
- `PlatformAttackSystem.ApplyBButton` where the original actually reaches `$BBCA`.

The self-test suite asserts ordering-sensitive cases, including:

- crouch drop before B;
- old `$4E==$20` attack-origin offset after current action has changed;
- crouch facing update before attack origin selection;
- exact `$40->$41` progression and `$4F->$00`/`Y-15` completion;
- B/input suppression during the `$40` branch;
- fall delta before B;
- fall crossing the `$90` B-height threshold;
- landing before B;
- `$80` waiting with nonzero `$76` and zero player/input mutation;
- `$80` exceptional reload request once `$76==0`;
- exact `$951F` persistent resource record order.

No ROM payload is embedded in the executable model.
