# Project state — authoritative continuation point

This file is the **single operational source of truth for `continúa` / `next`**.

## CURRENT

- Phase: `ORIGINAL SPEC / platform state-$20 NMI presentation boundary`
- State: `READY_FOR_NEXT`
- Last verified technical checkpoint: PR `#151` — complete platform visual-resource definition layer.
- Merge commit: `f0e36cf9b3f73988ac9b59b62aa93f9b269f4042`
- Exact final PR head: `fe2d0e16a3ace099be4d580c4878abf86c4ba5d6`
- Verification on that exact head:
  - `ORIGINAL SPEC tests` #388: `SUCCESS`
  - `Original Spec` #597: `SUCCESS`
  - build/self-test/password compatibility: `SUCCESS`
- Battle/event `$050E=$00-$0B` remains frozen with zero material stage-local gaps.
- Platform map kits, CHR routing and mechanical sprite-definition ownership are now closed. Do not reopen them without contradictory ROM evidence or a failing fixture.

## DONE

### Platform visual-resource definitions — PR #151

The bank-3 compositor rooted at `$B987` is now correctly modeled as a **shared player / primary-entity renderer**, not an entity-only table family.

Caller ownership:

```text
player $B94B+:
  $26 = $03 internal Saint index
  entries $00-$04 = Seiya / Shun / Hyoga / Shiryu / Ikki

primary entity $A8DC-$A907:
  $26 = logical +$09 numeric type
  entries $05-$0F = canonical primary entity types
```

Eleven 16-entry pointer-table families are mechanically closed:

```text
$B671 $B699 $B6C1 $B6E9 $B711 $B739
$B761 $B789 $B7B1 $B7D9 $B801
```

Direct hidden/flash definition `$B647` is separately owned.

`PlatformMetaspriteResourceSelector` reproduces `$B987-$BACF` for primary types `$05-$0F`, including:

- `$40/$70/$80/$A0/$D0/$E0` family routing;
- nonzero logical `+$04` routing;
- `$00/$20/$30/$50/$10` control-zero paths;
- type-specific `$08/$09/$0C/$0E/$0F` branches;
- dynamic `$B669` phase map `B699/B671/B699/B6C1`;
- renderer-owned mutation `action = (action + 1) & $13` when `$03B9==0`;
- unsupported original `$BA58` self-loop combinations remain unsupported rather than invented.

The ROM-fed audit covers:

```text
11 tables × 11 primary types = 121 pointer selections
51 distinct primary definitions on the canonical ROM
```

Every audited pointer/definition terminates inside bank 3 and every tile index resolves inside a selected 4 KiB CHR0 bank.

Maximum hardware-sprite counts:

```text
05=10 06=10 07=10 08=11 09=9  0A=6
0B=7  0C=7  0D=12 0E=4  0F=4
```

Type `$0D` is the sole twelve-record exception. Table `$B761` / definition `$B324` reaches 12 records, exactly matching the independently closed `$A647` rule that retires the extra visual record at `+$2C/+2D` only for `$0D`.

Separately selected direct resources are also frozen:

```text
$A908:
  $C0E3 -> tile table
  $C0EF -> signed vertical/Y offset table
  nonzero visuals only for $05/$06/$08/$09/$0C

$9B93 independent multisprite bootstrap, PRG bank 3:
  $9B65 sprite bases
  $9B6C $03A9 values
  $9B73 selector profiles
  $9B8F dedicated substate-$0D profile
  dedicated substate-$0D tile = $8C
```

The earlier provisional interpretation of `$C0EF` as horizontal was corrected: it is a **vertical/Y offset** added to logical Y.

Tools/artifacts:

- `PlatformMetaspriteResourceSelector.cs`
- `PlatformMetaspriteResourceSelectorChecks.cs`
- `tools/reverse/audit_platform_visual_resources.py`
- `tests/test_platform_visual_resources_spec.py`
- extended `tools/reverse/render_platform_entities.py`
- `PLATFORM_VISUAL_RESOURCE_DEFINITIONS.md`
- corrected `PLATFORM_CHR_MAP.md`

No ROM/CHR bytes or generated PNGs were committed.

## EVIDENCE FOR NEXT

The next contiguous global gap is no longer sprite-definition discovery. It is the **platform NMI presentation boundary**: how the already-modeled main-thread/OAM state reaches the NES presentation registers on the `$00=$20` platform branch.

Frozen NMI foundations:

```text
NMI vector $C000 -> $D269
```

`$D269+` already proves the common prologue:

- save A/X/Y;
- set `$3A=1` to mark MMC1-write interruption;
- reset MMC1 serial state;
- read `$2002`;
- set OAM address `$2003=0`;
- perform `$4014=$07` DMA from shadow page `$0700-$07FF`;
- dispatch by global `$00/$01` state.

For platform state `$00=$20`, direct ROM flow reaches:

```text
$D2BA JSR $D7F2
$D2BD JSR $D988
$D2C0 JMP $D367
```

The exact semantics and write ownership of `$D7F2/$D988` must be re-audited from canonical ROM rather than inherited from stale labels.

The shared NMI epilogue at `$D367+` is mechanically visible:

- derives nametable/control bit 0 in mirror `$77` from camera/page state;
- writes `$77 -> $2000`;
- writes `$78 -> $2001`;
- writes scroll components `$44/$46 -> $2005/$2005`;
- waits on the relevant `$2002` status condition;
- restores persistent PRG bank `$3B` through the mapper helper;
- restores Y/X/A and `RTI`.

Main-thread platform ordering and late-object state are already composed separately in `PlatformPersistentLateObjectFrame`; do not duplicate that simulation inside NMI.

## OPEN

1. Freeze exact NMI prologue `$D269+` through the `$00=$20` dispatch, including OAM DMA ordering and the mapper-interruption contract `$3A/$3B`.
2. Re-disassemble `$D7F2` and `$D988` from the canonical ROM and classify every platform-state-20 RAM/PPU write, bank switch and early return. Reconcile any stale documentation instead of preserving an old label by assumption.
3. Freeze the exact `$D367+` presentation commit: `$77/$78`, `$44/$46`, `$2000/$2001/$2005`, status wait and persistent-bank restore.
4. Model the **semantic ordering boundary** between main-thread `$0700` OAM mutations and NMI DMA. Do not emulate cycle-level PPU timing unless ROM evidence makes it gameplay-relevant.
5. Add a clean-room `PlatformState20NmiPhase` (or equivalently scoped model) plus discriminating fixtures for OAM-DMA source, state-20 branch effects, scroll/control commit and bank restoration.
6. Update focused NMI/renderer documentation, explicitly correcting any contradicted earlier `D7F2` interpretation.
7. Stop at platform state `$20`. Do not absorb every non-platform NMI state, complete palette semantics, audio or unrelated mapper code in this checkpoint.

## NEXT

**Close the platform `$00=$20` NMI presentation boundary from `$D269` through the state-20 calls `$D7F2/$D988` and common `$D367+` epilogue: prove OAM DMA ownership/order, reclassify the two platform NMI subroutines from canonical ROM, model the PPU-control/mask/scroll commit and persistent-bank restore, and promote an executable clean-room phase with fixtures without duplicating the already-closed main-thread platform simulation.**

Completion criterion:

> Given the already-composed platform main-thread state and its `$0700-$07FF` OAM shadow, the model must deterministically describe what a platform-state-`$20` NMI snapshots/commits and in what semantic order: DMA source, relevant `$D7F2/$D988` side effects, `$77/$78` control/mask commit, `$44/$46` scroll commit, mapper interruption/restoration and return. Any existing document that conflicts with direct ROM flow must be corrected in the same checkpoint. Other NMI global states and full palette/audio systems remain outside scope.

## BLOCKERS

- None. Canonical ROM, boot/MMC1/NMI entry, OAM shadow ownership, platform main-thread frame composition, CHR routing and visual-resource definitions are already available.

## RECOVERY CONTRACT

1. Read this file from `main` and reconcile it with newer merged history before executing `NEXT`.
2. Freeze PR #151 visual-resource closure and PR #149 battle-stage closure.
3. Reuse `BOOT_AND_MAPPER.md`, `PERSISTENT_LATE_OBJECT_FRAME.md`, `PLATFORM_VISUAL_RESOURCE_DEFINITIONS.md` and existing NMI documents, but treat direct canonical-ROM bytes as authority when older prose disagrees.
4. Keep main-thread simulation and NMI presentation as separate ownership phases.
5. Do not commit ROM/OAM dumps or extracted art. Semantic code/tests/docs only.
6. Drive remains private ROM/evidence storage and never owns an independent `NEXT`.

## ANTI-LOOP

- `last_next_signature`: `platform-state20-nmi-oam-ppu-commit`
- `same_result_count`: `0`
- `retry_budget_per_strategy`: `2`

A cycle must produce code, tests, evidence, a discarded hypothesis or an evidence-backed decision. Two identical no-progress cycles forbid a third identical attempt.
