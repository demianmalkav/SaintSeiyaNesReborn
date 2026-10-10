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
| Combate de jefes | MEDIUM-HIGH -> HIGH parcial por contexto | Primitivas fuertes y Taurus `$01` cerrado end-to-end en #123. Frente actual: Leo/Aioria `$04`. |
| Taurus/Aldebaran stage `$01` | HIGH | Init, Talk, post-Bronze, post-Gold, retry/persistencia y releases terminales cerrados. |
| Life/Cosmo/Seventh Sense | HIGH plataforma / MEDIUM-HIGH global | Recursos y clasificadores cerrados; siguen componiéndose en contexts boss. |
| Texto / localización | MEDIUM | Corpus JP + borrador ES; falta integración runtime/revisión final. |
| Renderer / metasprites / CHR | MEDIUM | CHR/plataforma parcial; no hay render spec global todavía. |
| Executable CHR/RAM overlays | MEDIUM-HIGH | Dos overlays CHR31 confirmados y auditados para reachability global; censo gráfico/general pertenece a renderer. |
| RNG | LOW-MEDIUM | Subsistema global abierto. |
| Audio | LOW | Subsistema global abierto. |
| REBORN moderno | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoints globales recientes

### PR #121 — namespace global

`ENGINE_STATE_REACHABILITY.md` clasifica todos los valores `$00-$FF` y deja cerrada la máquina global.

```text
PR #121
merge ab6859986b8d62b05943b2a011e6257a5933f308
head  ce56bb74663748edb71ff6fe15477575bd497114
CI    #312 SUCCESS / #506 SUCCESS
```

### PR #123 — Taurus/Aldebaran stage `$01`

Primer boss context completo promovido como composición end-to-end.

Handlers:

```text
init        $97F8
Talk        $9D2C
post-Bronze $A3A2
post-Gold   $A415
```

Resultados centrales:

- Talk `$066F:0->1->2`; segundo Talk activa `$0681=1` una sola vez;
- Talk #3+ fuerza contraataque Gold mediante `$DC` transitorio;
- `$EB=$FF` -> victoria `$0670=$01`;
- primera condición baja del boss latches `$DD=5`, feedback `$064E++`;
- miss/no-hit posterior puede hacer `$064E++`;
- `$EA=$FF` -> derrota `$0670=$FF`;
- primera condición baja del jugador latches `$064D=1`;
- `$0681` sobrevive derrota/retry pero se limpia al progresar tras victoria.

```text
PR #123
merge 446b54395ce378119e59fcc12d7a7e8174fe5b3b
head  ff0e0064a5a3d194bf623e51d9b785a91cefe742
CI    #316 SUCCESS / #510 SUCCESS
```

Artifact principal: `BOSS_CONTEXT_STAGE_01_TAURUS.md` + `TaurusStage01Context.cs` + fixtures.

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente en:

```text
ORIGINAL SPEC / boss context stage $04 Leo
```

Leo/Aioria es el siguiente context elegido porque introduce semántica nueva que Taurus no cubre:

```text
init        $989D
Talk        $9DD8
post-Bronze $A5B3
post-Gold   $A63E
```

La frontera debe resolver especialmente `$ED`, `$F1`, `$0690`, los dos writers de `$0681`, la forma distinta del Talk y la selección `$0680` de ataques Gold. Stage `$04` tiene coeficientes Gold alternantes `48/32` y `32/48`, por lo que el slot ya tiene consecuencia numérica y no puede abstraerse como en Taurus.

## Checkpoints que no deben reabrirse sin evidencia nueva

- máquina global `$00/$01` completa — #121;
- Taurus stage `$01` completo — #123;
- front-end/title `$50` y overlays — #119;
- attract `$30-$4D` estructura — #117, con semántica corregida por #119;
- `$11-$14` — #111;
- fatal `$60` — #113;
- `$91-$99` — #115;
- plataforma, exits, reloads y narrativa ya promovidos;
- damage/resources/dodge/technique primitives de bosses ya documentadas.

## Áreas globales abiertas

1. contextos boss/progresión restantes, con Leo `$04` como frente activo;
2. otros contexts prioritarios posteriores: Virgo, Aquarius, Pisces, Saga y ramas especiales;
3. renderer/metasprites/CHR global;
4. RNG y consumidores;
5. audio / bank-track mapping;
6. texto runtime + integración final española;
7. campos persistentes/progresión todavía provisionales;
8. revisión de cobertura final ORIGINAL SPEC;
9. REBORN sólo después de ese cierre.

## Recuperación

Leer en orden:

1. `docs/PROJECT_STATE.md`;
2. `BOSS_CONTEXT_STAGE_01_TAURUS.md` como plantilla de composición;
3. `BATTLE_EVENT_DISPATCH.md`;
4. `BOSS_BATTLE_RESOURCES.md`;
5. `BOSS_BATTLE_DAMAGE.md`;
6. `BOSS_DODGE.md`;
7. `BATTLE_TECHNIQUES.md`;
8. handlers Leo bank5 `$989D/$9DD8/$A5B3/$A63E`;
9. `docs/WORK_PROTOCOL.md`;
10. este documento sólo como mapa lateral.
