# Estado global de ingeniería inversa

> **Documento de orientación global, no operativo.**
>
> Resume madurez y ubicación de evidencia. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Target primario

**Saint Seiya: Ōgon Densetsu Kanketsu Hen** — Famicom, Japón, 1988.

ROM canónica: SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`, mapper 1/MMC1, 128 KiB PRG + 128 KiB CHR.

## Mapa global vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | PR #121 cerró el namespace completo: 59 valores producidos, 197 estructurales sin productor. |
| Front-end/title `$50` | HIGH | Modal `$0200=$00-$09`, attract, start/password y CHR->RAM overlays cerrados. |
| Attract `$30-$4D` | HIGH | Grafo cerrado; semántica corregida como presentación/front-end. |
| Plataforma: frame / entidades / hazards | HIGH | Orden, spawners, entidades, hazards y timing ampliamente promovidos. |
| Plataforma: movimiento / colisión / combate | HIGH | Movimiento, salto, ataques, colisión, damage/drain/failure modelados. |
| Plataforma: exits / reload / narrativa | HIGH | `$02=$00-$11`, selector, destinos `$00/$10/$90`, `$70-$89`, reload `$8F/$FF` cerrados. |
| Engine `$11-$14`, `$60`, `$91-$99` | HIGH | Familias cerradas con fixtures. |
| Password | HIGH | Codec, output, entrada, validación y puente front-end unidos. |
| Combate de jefes | MEDIUM-HIGH | Primitivas fuertes; falta composición end-to-end de contextos por stage. Frente actual: Taurus `$01`. |
| Life/Cosmo/Seventh Sense | HIGH plataforma / MEDIUM-HIGH global | Recursos y clasificadores cerrados; faltan composiciones boss/contexto. |
| Texto / localización | MEDIUM | Corpus JP + borrador ES; falta integración runtime/revisión final. |
| Renderer / metasprites / CHR | MEDIUM | CHR/plataforma parcial; no hay render spec global todavía. |
| Executable CHR/RAM overlays | MEDIUM-HIGH | Dos overlays CHR31 confirmados y auditados para reachability global; censo gráfico/general aún pertenece a renderer. |
| RNG | LOW-MEDIUM | Subsistema global abierto. |
| Audio | LOW | Subsistema global abierto. |
| REBORN moderno | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint global nuevo — PR #121

`ENGINE_STATE_REACHABILITY.md` cierra todos los valores `$00-$FF`.

Transitorios reales:

```text
$00, $10, $30, $3D, $90
```

Reachables ordinarios:

```text
$11-$14, $20, $31-$38, $40-$4D, $50, $60,
$70-$75, $80-$89, $91-$99
```

Todo lo demás carece de productor ejecutable canónico. El censo incluyó PRG, stores indirectos y los dos overlays CHR31 ejecutados desde RAM `$0440`.

Último checkpoint técnico:

```text
PR #121
merge ab6859986b8d62b05943b2a011e6257a5933f308
head  ce56bb74663748edb71ff6fe15477575bd497114
CI    #312 SUCCESS / #506 SUCCESS
```

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente en:

```text
ORIGINAL SPEC / boss context stage $01 Taurus
```

La meta ya no es descubrir otra máquina global, sino convertir las primitivas de combate de jefe en **contextos completos por stage**.

Taurus/Aldebarán (`$050E=$01`) es el primer patrón elegido porque sus cuatro handlers son acotados:

```text
init        $97F8
Talk        $9D2C
post-Bronze $A3A2
post-Gold   $A415
```

Con Taurus cerrado end-to-end se obtiene una plantilla verificable para Leo, Virgo, Aquarius, Pisces y Saga sin rehacer fórmulas genéricas.

## Checkpoints que no deben reabrirse sin evidencia nueva

- máquina global `$00/$01` completa — #121;
- front-end/title `$50` y overlays — #119;
- attract `$30-$4D` estructura — #117, con semántica corregida por #119;
- `$11-$14` — #111;
- fatal `$60` — #113;
- `$91-$99` — #115;
- plataforma, exits, reloads y narrativa ya promovidos;
- damage/resources/dodge/technique primitives de bosses ya documentadas.

## Áreas globales abiertas

1. contextos boss/progresión end-to-end, empezando por Taurus `$01`;
2. renderer/metasprites/CHR global;
3. RNG y consumidores;
4. audio / bank-track mapping;
5. texto runtime + integración final española;
6. campos persistentes/progresión todavía provisionales;
7. revisión de cobertura final ORIGINAL SPEC;
8. REBORN sólo después de ese cierre.

## Recuperación

Leer en orden:

1. `docs/PROJECT_STATE.md`;
2. `BATTLE_EVENT_DISPATCH.md`;
3. `BOSS_BATTLE_RESOURCES.md`;
4. `BOSS_BATTLE_DAMAGE.md`;
5. `BOSS_DODGE.md`;
6. `BATTLE_TECHNIQUES.md`;
7. handlers Taurus bank5 `$97F8/$9D2C/$A3A2/$A415`;
8. `docs/WORK_PROTOCOL.md`;
9. este documento sólo como mapa lateral.
