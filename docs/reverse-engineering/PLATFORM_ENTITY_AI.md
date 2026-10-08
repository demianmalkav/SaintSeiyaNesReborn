# Platform entity AI and state machine

Target: canonical Japanese `Saint Seiya: Ougon Densetsu Kanketsu Hen` ROM.

Status: advanced static reconstruction. The common movable-entity record, jump AI, direction handling, terrain-turn rules, hit reaction and several special-state families are now tied directly to code. Numeric entity types remain neutral where visual identity is not proven.

## Entity record

The active entity pointer is `$16/$17`. Confirmed/provisional fields:

| Offset | Working name | Status | Evidence |
|---:|---|---|---|
| `$00` | `action_state` | CONFIRMED | high-nibble state dispatcher throughout bank 3 |
| `$01` | `x` | CONFIRMED | horizontal movement/collision/render input |
| `$02` | `y` | CONFIRMED | vertical movement/collision/render input |
| `$03` | `state_phase` | CONFIRMED | drives jump phase and several timed special states |
| `$04` | `animation_phase` | strong | cycles 0..11, passed to renderer and gates selected type logic |
| `$05` | `ground_descriptor` | CONFIRMED | fixed `$C491` uses it for landing/snap behavior; renderer also consumes it in falling/death states |
| `$06` | `decision_timer` | CONFIRMED for common mobile types | decremented by `$A98B`; reseeded 31..94 by `$AA64` |
| `$07` | `flags_facing` | CONFIRMED | bit `$40` controls direction and sprite orientation |
| `$08` | `stage_aux` | UNKNOWN semantics | receives `$03AB` in active platform mode, otherwise `$F0` |
| `$09` | `type` | CONFIRMED | explicit type/class dispatch and initialization |
| `$0A` | `terrain_probe_right` | CONFIRMED behavior | consumed when facing right to decide terrain turn |
| `$0B` | `terrain_probe_left` | CONFIRMED behavior | consumed when facing left to decide terrain turn |
| `$0C` | `hp` | CONFIRMED | player attack subtracts `$72` |
| `$0D` | `cosmo_drain_ticks` | CONFIRMED | copied to player `$80` on contact |
| `$0E` | `life_drain_ticks` | CONFIRMED | copied to player `$7F` on contact |
| `$0F` | `seventh_sense_reward_bcd` | CONFIRMED | kill path adds packed-BCD reward to Seventh Sense |

## Facing convention

`flags_facing & $40` is the horizontal direction bit.

- bit clear: movement subtracts from X -> **left**;
- bit set: movement adds to X -> **right**.

The same bit is passed into sprite construction and attack/helper-object construction, so movement and visual orientation share one authoritative direction flag.

## Movement step

Helper `$A60B` derives the horizontal step used by entity movement.

For ordinary types:

- when `frame_counter & 3 == 0`: step = 2 px;
- otherwise: step = 1 px.

For type family `(type & $FE) == $0A` (types `$0A/$0B`):

- step = `frame_counter & 1`, i.e. alternating 0/1 px.

Camera delta `$43` is then subtracted from entity X so screen-space position tracks scrolling.

## Common decision timer

For common mobile types `< $08`, except type `$07`:

1. decrement record offset `$06`;
2. if still non-zero, keep current behavior;
3. if it reaches zero, `$AA64` reseeds it with:

`decision_timer = ($48 & $3F) + $1F`

Range: **31..94**.

`$48` is used elsewhere as a changing pseudo-random/entropy-like engine byte; the exact RNG provenance remains separate from this confirmed timer formula.

Types `$07`, `$0A`, `$0B` bypass this common countdown and use their terrain-facing path directly. Types `$08+` other than the explicit `$0A/$0B` cases bypass the common chase/jump decision path.

## Jump family `$30-$3F`

The family is an airborne/jump state.

Fixed routine `$C5E6` applies vertical motion whenever the entity state is `$3x`.

Known exact horizontal variants:

- `$31`: move toward **+X/right** during jump;
- `$32`: move toward **-X/left** during jump.

The common AI writes `$31/$32` and initializes `state_phase = 1` when it decides to jump.

### Vertical profile

`state_phase` advances once per update. Before the final fall phase, the vertical displacement table at fixed `$C639` is consumed.

The used signed sequence begins:

`+8,+8,+7,+7,+6,+5,+4,+3,+3,+2,+2,+1,+1,+1,0,0,0,0,-1,-1,-1,-1,-2,-2,-2,-2,-2,-3,-3,-3`

The routine subtracts these signed values from Y. Therefore positive values move upward and negative values move downward.

Consequences:

- maximum ascent: **58 px**;
- apex occurs around phase 15;
- from the second half of the jump, fixed `$C491` begins checking the entity ground descriptor for landing;
- after the table phase reaches its end, unresolved airborne motion falls at **+3 px/update** until landing.

This is table-driven, not gravity/velocity integration.

## Landing — fixed `$C491`

The common jump/fall landing helper consumes record offset `$05` (`ground_descriptor`) plus entity Y.

It:

- ignores landing in the lower inactive/out-of-range vertical region;
- uses descriptor-dependent low-nibble Y thresholds;
- snaps Y to either a 16-pixel row or an 8-pixel offset row for upper descriptor classes;
- clears `state_phase` on accepted landing;
- returns most normal types to state `$10`;
- types `$08/$09/$0C` return to state `$00` instead.

This proves offset `$05` is a floor/environment descriptor rather than animation metadata.

## Terrain-facing probes

`$AA1F+` prevents mobile entities from continuing blindly into certain descriptor families.

When facing left (bit `$40` clear), offset `$0B` is inspected.

Turn-around families for ordinary types:

- `$88-$8F`
- `$E0-$EF`

When facing right (bit `$40` set), offset `$0A` is inspected.

Turn-around families for ordinary types:

- `$80-$87`
- `$E0-$EF`

These mirror the player's left/right collision families and strongly support the semantic names `terrain_probe_left/right`.

Type `$07` has an additional cutoff: descriptors `$E4+` bypass the normal turn path, so only the lower part of the `$E0` family can trigger the same response for that type.

Turn-around itself is simply:

`flags_facing ^= $40`

## AI jump decision

When the common decision timer expires, `$A999+` compares entity X/Y and facing against player position.

The exact branches include:

- whether the player is currently in a jump phase;
- relative Y checks;
- a horizontal proximity window around the player;
- current facing consistency.

The outcome is one of:

1. keep current movement;
2. toggle facing;
3. enter jump-right `$31` with phase 1;
4. enter jump-left `$32` with phase 1.

Thus the standard moving enemy AI is not frame-by-frame homing. It performs **periodic stochastic decisions**, with terrain avoidance operating separately.

## Hit reaction family `$40-$4F`

Entity damage handling enters `$40` family on a surviving hit.

Update `$A79E+` increments the state byte each update. When the state reaches `$50`:

- types `< $08` transition to `$10`;
- types `$08+` transition to `$00`.

Therefore `$40-$4F` is a 16-step timed reaction/transition family rather than sixteen independent behaviors.

Rendering deliberately blinks/changes presentation for `$40` family based on frame timing.

## Falling / removal families

Observed roles:

- `$50` and `$E0`: use the common downward path (`Y += 3`) and landing/removal checks;
- `$D0`: death/fall/removal family, including descriptor-aware rendering and timed state progression;
- `$A0`: special state family used notably by type `$0D`; progresses toward `$B0` and removal;
- `$70`: timed transition/animation family; progresses toward `$80`/reset with type-specific handling.

Exact design names for `$70/$A0/$D0/$E0` remain intentionally neutral until their originating type/event paths are fully classified.

## Animation phase `$04`

For the normal active path, `$A738+` increments offset `$04`, wrapping at 12:

`0 -> 1 -> ... -> 11 -> 0`

The renderer copies it to `$27`. Selected entity types also gate logic on it being zero. This is strong evidence that it is an animation/cycle phase, though some types may reuse it as a cadence gate.

## Stage auxiliary `$08`

Helper `$A908+` writes:

- `$03AB` when global platform mode `$00 == $20`;
- `$F0` otherwise.

`$03AB` is selected from fixed small values `{6,10,21,43}` by bank-1 stage/setup logic. No sufficiently direct consumer has yet been tied back to offset `$08`, so its final semantic name remains UNKNOWN.

## Architecture consequence

The platform entity model is already data-oriented:

`type + state + phase + animation + direction + terrain probes + stats`

The common AI layer provides:

- camera-relative movement;
- periodic decision timing;
- left/right terrain avoidance;
- table-driven jump arcs;
- common hit reaction;
- common rendering inputs;
- shared player-contact and projectile-contact interfaces.

REBORN can represent this directly as an `EntityArchetype` plus reusable state-machine components rather than reproducing pointer/OAM constraints.

## Next targets

1. classify visual identities of the five base archetypes extracted from bank 1;
2. prove how offsets `$05/$0A/$0B` are generated from the stage descriptor grid;
3. resolve `$08` and the remaining record bytes/state families;
4. reconstruct type-specific behaviors `$08-$0F`;
5. convert the common jump/turn/reaction rules into clean-room parity tests.
