# Platform jump initiation — `$BB76-$BBCA`

Target: canonical Japanese `Saint Seiya: Ōgon Densetsu Kanketsu Hen` ROM.

Status: the A-button/jump-initiation portion of PRG bank 3 is reconstructed exactly. B-button attack handling continues from `$BBCA` and remains a separate subsystem.

## Entry behavior

The routine first tests A (`$3D & $80`).

If A is **not** held:

- `$4A` (jump-button latch) is cleared to zero;
- the routine continues to the B-button attack path.

This happens even while `$49` indicates an active jump. Releasing A therefore rearms the jump input without changing the current jump trajectory.

If A **is** held, a new jump can start only when:

- `$49 == 0` (no active jump);
- `$4A == 0` (A has been released since the previous accepted/blocked press).

## Rejected jump press

Two conditions reject a fresh A press:

- Down is held (`$3D & $04`);
- `$038D != 0`.

Either rejection writes:

`$4A = $FF`

and does not start a jump. The latch remains blocked until A is released.

This is distinct from the successful-jump latch value `$01`.

## Successful jump start

On success the original:

1. triggers sound/effect `$25`;
2. writes `$49 = 1`;
3. writes `$4A = 1`;
4. clears `$038A = 0`;
5. inspects Left/Right input bits `$3D & $03`.

Takeoff state:

| input at takeoff | `$4D` | meaning |
|---|---:|---|
| neither | `$30` | vertical-family jump |
| Right | `$31` | fixed right directional trajectory |
| Left | `$32` | fixed left directional trajectory |
| Left+Right | `$33` | recorded both-held quirk; later follows left trajectory |

For a purely vertical `$30` takeoff, Up is then tested:

- Up not held -> `$038A = 0`: ordinary standing-jump profile;
- Up held -> `$038A = $30`: Saint-specific high-jump profile.

If any horizontal bit was present, the directional state is selected first and Up does **not** enable the high-jump table.

## Same-frame vertical execution

A crucial caller detail from `$AAE4+`:

1. `$BB76` can create the new jump (`$49=1`, `$4D=$30-$33`);
2. the caller immediately tests the updated `$4D`;
3. for jump family `$30-$3F`, it copies the updated action into the frame latch and calls `$BCD3` **in the same frame**.

Therefore a native implementation must not insert a one-frame delay between pressing A and applying the first jump-table displacement.

`PlatformJumpInitiationResult.MustRunAirborneStepThisFrame` makes this contract explicit for the clean-room runtime.

## Attack interaction boundary

After the jump-input block, execution falls through to `$BBCA`, where B attack/latch/busy logic begins. That means A and B can participate in the same platform update.

`PlatformJumpInitiation` intentionally models only the A/jump side and does not claim to represent the later B path. The eventual unified player frame will compose both systems in ROM order.
