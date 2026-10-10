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
| Máquina global `$00/$01` | HIGH | PR #121 cerró el namespace: 59 valores producidos, 197 estructurales sin productor. |
| Front-end/title `$50` | HIGH | Modal, attract, start/password y overlays CHR->RAM cerrados. |
| Plataforma: frame / movimiento / colisión / hazards / exits | HIGH | Sistemas principales y handoffs ampliamente promovidos. |
| Password | HIGH | Codec, entrada/salida y puente front-end cerrados. |
| Combate de jefes — primitivas | HIGH | Recursos, clasificadores, damage, técnicas, dodge y dispatch tables promovidos. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo cerrado en PR #123. |
| Leo/Aioria `$04` | HIGH | Contexto completo cerrado en PR #125, incluida invulnerabilidad `$0690`, `$F1/$ED/$0681` y slots Gold 0/1. |
| Virgo/Shaka `$05` | ACTIVE | Próximo contexto: sustitución Ikki, releases especiales y slot Gold 2 forzado. |
| Otros boss contexts | MEDIUM | Aquarius, Pisces, Saga y ramas especiales aún deben componerse end-to-end. |
| Life/Cosmo/Seventh Sense | HIGH plataforma / MEDIUM-HIGH global | Primitivas cerradas; composición boss en progreso. |
| Texto / localización | MEDIUM | Corpus JP + borrador ES; integración runtime/revisión final pendiente. |
| Renderer / metasprites / CHR | MEDIUM | CHR/plataforma parcial; render spec global pendiente. |
| Executable CHR/RAM overlays | MEDIUM-HIGH | Dos overlays CHR31 confirmados; censo visual/general pendiente con renderer. |
| RNG | LOW-MEDIUM | Subsistema global abierto. |
| Audio | LOW | Subsistema global abierto. |
| REBORN moderno | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoints técnicos recientes

### PR #121 — namespace global `$00/$01`

Todos los valores `$00-$FF` quedaron clasificados: 59 producidos y 197 sin productor canónico.

### PR #123 — Taurus/Aldebaran `$01`

Primer boss context completo. Cerró Talk weakening, post-Bronze/post-Gold, retry/persistencia y releases terminales.

```text
merge 446b54395ce378119e59fcc12d7a7e8174fe5b3b
head  ff0e0064a5a3d194bf623e51d9b785a91cefe742
CI    #316 SUCCESS / #510 SUCCESS
```

### PR #125 — Leo/Aioria `$04`

Segundo boss context completo y primer contexto con invulnerabilidad stage-local + selección Gold materialmente distinta.

Resultados centrales:

- fixed stage-4 entry rearma `$0690=$FF`, que fuerza `$06BC=0` y bloquea daño Bronze;
- intro `$989D` sólo se despacha con Seiya y `$068E=0`; escribe `$ED=1` y hace dos `INC $0681` incondicionales;
- Talk 1 fuerza contraataque; Talk 2 es la rama única que puede limpiar `$0690` si `$F1==0`; Talk 3+ vuelve a forzar contraataques;
- `$F1` persiste sobre retry ordinario y low-condition Aioria + non-Seiya vuelve a armar `$0690` y hace `$F1++`;
- ambas ramas de victoria seleccionadas por `$ED` convergen en `$0670=$01`;
- post-Gold conserva el evento one-shot `$064D` y derrota `$0670=$FF`;
- selector stage4 produce sólo `$0680={0,1}` mediante `$065F&1`; slots 2/3 son inalcanzables para Leo;
- perfiles alcanzables: `48/32` y `32/48`, compuestos con weakening/dodge/damage genéricos.

```text
merge b77d863ae8e26769f4f4235eb2ca1771bf03643e
head  5744aa1d7673c0ff0777a85e71a2a56fa2370112
CI    #320 SUCCESS / #516 SUCCESS
```

Artifacts: `BOSS_CONTEXT_STAGE_04_LEO.md`, `LeoStage04Context.cs` y fixtures.

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente en:

```text
ORIGINAL SPEC / boss context stage $05 Virgo
```

Virgo/Shaka introduce una estructura distinta a Taurus/Leo:

```text
init        $9A28
Talk        $9E1B
post-Bronze $A661
post-Gold   $A7B3
```

La frontera debe cerrar la sustitución a Ikki y sus handoffs especiales. Evidencia preliminar:

- `$9A28` usa active Saint `4` (Ikki), `$0683` y `$0673=$3F` para rutas que emiten `$0670=$DD/$FE`, y una ruta non-Ikki puede reemplazar el Saint activo por Ikki;
- `$9E1B` separa non-Ikki de Ikki y, para Ikki, distingue `$067C==0` de `$067C!=0`; sólo algunas ramas levantan `$DC` y fuerzan respuesta Gold;
- `$A661` tiene una máquina Ikki independiente con `$067C/$0683/$064D/$06E0/$06BC/$0690` y puede emitir `$0670=$02` junto con platform substate `$02=$0D`, además de `$FE`;
- `$A7B3` tiene una regla especial: con Ikki y `$0683!=0`, player condition `$EA=$01` también puede terminar en defeat release `$FF`;
- bank-6 `$9074+` fuerza `$0680=$02` cuando `stage==5 && activeSaint==Ikki`, por lo que slot2 forma parte del grafo real de Virgo.

## Checkpoints que no deben reabrirse sin evidencia nueva

- global `$00/$01` — #121;
- Taurus `$01` — #123;
- Leo `$04` — #125;
- front-end/title `$50` — #119;
- attract `$30-$4D` — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma, exits, reloads y narrativa ya promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas globales abiertas

1. Virgo/Shaka `$05` end-to-end, incluido Ikki y releases especiales;
2. Aquarius/Camus, Pisces/Aphrodite, Saga y otros contexts no redundantes;
3. revisión de stages omitidos si aportan branches no cubiertas por los templates promovidos;
4. renderer/metasprites/CHR global;
5. RNG y consumidores;
6. audio / bank-track mapping;
7. texto runtime + integración española final;
8. campos persistentes/progresión todavía provisionales;
9. auditoría final de cobertura ORIGINAL SPEC;
10. REBORN después de ese cierre integral.

## Recuperación

Leer en orden:

1. `docs/PROJECT_STATE.md`;
2. `BOSS_CONTEXT_STAGE_01_TAURUS.md` y `BOSS_CONTEXT_STAGE_04_LEO.md`;
3. `BATTLE_EVENT_DISPATCH.md`;
4. `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`;
5. Virgo bank5 `$9A28/$9E1B/$A661/$A7B3` y bank6 `$9074+`;
6. platform/reload docs al reconciliar releases `$DD/$FE/$02`;
7. `docs/WORK_PROTOCOL.md`;
8. este documento sólo como mapa lateral.
