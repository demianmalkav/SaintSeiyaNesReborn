# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper, PRG mapping y contratos de interrupción establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: main-thread frame / late objects | **HIGH / CLOSED** | Producers, player, auxiliares, primary A/B y late objects compuestos. |
| Boss primitives / stage contexts | **HIGH / CLOSED** | Recursos, daño, dodge, técnicas y contextos materiales `$00-$0A`; `$0B` estructural. |
| Platform maps / CHR / metasprites | **HIGH / CLOSED** | Kits `$00-$11`, routing CHR estático/dinámico y visual resources cerrados. |
| Platform / global presentation | **HIGH / CLOSED-SEMANTIC** | State-$20 NMI, palette/CHR, HUD y coverage NMI global con cero gaps materiales. |
| RNG / pseudo-random source `$065F/$0660` | **HIGH / CLOSED** | PR #163: `$E0AC`, bank-sensitive `$94F0,X`, seeds/resets y consumer masks/parity cerrados. |
| Audio scheduler `$DB9C/$DBB6/$0440+` | **ACTIVE** | Ocho slots de `$15` bytes y cue loader visibles; falta lifecycle/updater/APU ownership. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración runtime final pendiente. |
| Auditoría integral ORIGINAL SPEC | PENDING | Después de cerrar audio y runtime text/content integration necesaria. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #163

El source pseudo-random/phase queda cerrado.

```text
caller único       $E09C -> $E0AC
update gate        $9C!=0 && ($9D|$9E|$A0)==0
recurrence         065F' = 065F + visible_prg[94F0+0660]
                   0660' = 0660 + 1
source window      bank-sensitive, $94F0-$95EF
queue priority     0641 -> 0526 -> 0538 -> 057D
forced banks       0/6, 6, 5, 6 respectively
mapper busy        preserve incoming committed bank
```

Reset/seed ownership:

```text
C13D cold clear    (00,00)
AD4A clear+fill    (01,01)
959D clear         (00,00)
AF0D clear         (00,00)
B38A $0648,Y       Y=17 -> 065F; Y=18 -> 0660
```

Consumer masks/ranges are frozen at `$E33D`, `$EC18/$EC20/$EC2B`, `$F65D/$F665`, `$FAC9/$FAD7`, bank6 `$913E/$92E2`; `$F995` maps `$0660` parity to `$FF/$01` and closes the dodge-direction source.

Technical checkpoint:

```text
PR    #163
merge b89a5fb7d86e7b7b322cca1ea13a416ed9ca050f
head  cb003c01db8370d1195d1e8a527dfad4a40f68da
CI    #416 SUCCESS / #620 SUCCESS
```

Artifacts: `CanonicalRandomSourceE0AC.cs`, self-test fixtures, `audit_canonical_random_source.py`, and `CANONICAL_RANDOM_SOURCE_E0AC.md`. No original `$94F0` table payload is committed.

## Frontera operativa actual

La siguiente frontera global es **audio scheduler architecture**.

Evidence already frozen for entry:

```text
$DB9C:
  $4015=0
  $04EF/$04F0=0
  8 records: $0440 + $15*N, N=0..7
  record +0 = FF on reset

$DBB6:
  cue A -> descriptor $DC0E + 4*A
  descriptor[0] -> slot offset
  descriptor[1..3] -> slot +1..+3
  slot +0 = 0 (active)
  existing-slot replacement mutates $04F0 through $DC0A mask
```

The active work is to identify slot lifecycle/preemption, the per-frame updater and semantic ownership of APU writes `$4000-$4015`. Full music/SFX payload reconstruction remains a later boundary.

## No reabrir sin evidencia nueva

- global `$00/$01` reachability;
- global NMI presentation — #161;
- battle/event coverage — #149 and stage PRs;
- platform main-thread / exits / narrative;
- maps / CHR / visual definitions — #151/#156;
- state-$20 NMI — #154;
- HUD `$9D69-$9EED` — #159;
- canonical RNG `$E0AC/$065F/$0660` — #163;
- promoted boss damage/resources/dodge/technique boundaries.

## Áreas abiertas

1. audio scheduler/voice/APU ownership;
2. full audio content only after scheduler architecture;
3. texto runtime/localización final;
4. auditoría integral final de ORIGINAL SPEC;
5. REBORN sólo después del cierre integral.
