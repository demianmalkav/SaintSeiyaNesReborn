# Estado global de ingeniería inversa

> **Documento de orientación global, no operativo.**
>
> Resume madurez y localización de evidencia. **No decide el trabajo siguiente.** La única autoridad de continuación es `docs/PROJECT_STATE.md`, reconciliada con `main` y PR/CI.

## Target primario

**Saint Seiya: Ōgon Densetsu Kanketsu Hen** — Famicom, Japón, 1988.

ROM canónica:

- SHA-1 `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5 `3B0F17C2B6EFC928B3D3FE9B1A389680`
- SHA-256 `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`
- CRC32 iNES `F8D258A3`
- CRC32 sin header `9561798D`
- 128 KiB PRG + 128 KiB CHR
- mapper 1 / MMC1

Fuente: `docs/reverse-engineering/CANONICAL_ROM.md`.

## Convención de madurez

- `HIGH`: especificación semántica sustancial + documentación/fixtures; quedan bordes concretos.
- `MEDIUM`: partes importantes promovidas, superficie incompleta.
- `LOW`: anclas parciales/investigación temprana.

Los niveles `CONFIRMED / INFERRED / UNKNOWN / DISPROVEN` siguen aplicándose dentro de cada documento técnico.

## Mapa global vigente

| Subsistema | Madurez | Estado resumido | Entradas representativas |
|---|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, wrappers y mapa de bancos ampliamente establecidos. | `CANONICAL_ROM.md`, `BOOT_AND_MAPPER.md`, `BANK_MAP.md` |
| Índices de Saints | MEDIUM-HIGH | Mapping canonical/internal promovido y reutilizado. | `CHARACTER_INDEX_MAP.md`, `SaintId.cs` |
| Plataforma: frame / entidades / hazards | HIGH | Orden persistente, spawners, entidades, hazards y timing ampliamente promovidos. | `PERSISTENT_LATE_OBJECT_FRAME.md`, `ENTITY_TYPES_0A_0B.md` |
| Plataforma: movimiento / colisión / combate | HIGH | Movimiento, salto, ataques, probes, daño, drain y fatal resource transition modelados. | `PLATFORM_PLAYER.md`, `PLATFORM_JUMP.md`, `PLATFORM_ATTACKS.md`, `COLLISION_BEHAVIOR.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Plataforma: exits / reload / narrativa | HIGH | `$02=$00-$11`, selector warm reload, destinos `$00/$10/$90`, `$70-$89`, reload `$8F` y failure reload `$FF` promovidos. | `PLATFORM_EXIT_GATES.md`, `PLATFORM_WARM_RELOAD_INTERACTIVE_STATE.md`, `PLATFORM_NORMAL_WARM_RELOAD_DESTINATIONS.md`, `PLATFORM_SPECIAL_NORMAL_EXITS.md`, `PLATFORM_NARRATIVE_STATES_80_89.md`, `PLATFORM_RELOAD_MODE_8F.md` |
| Máquina global `$00/$01` | HIGH en dispatcher y principales familias / MEDIUM-HIGH global | Dispatcher superior, `$11-$14`, `$30-$4F`, `$60`, `$70-$89`, `$91-$99` cerrados. Frente actual: exact state `$50`. | `ENGINE_STATE_DISPATCHER.md`, `ENGINE_STATE_FAMILY_11_14.md`, `SCENE_BATTLE_ENGINE_STATE_30_4F.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md`, `ENGINE_STATE_FAMILY_91_99.md`, `PROJECT_STATE.md` |
| Escena/batalla outer graph | HIGH | Reachable set `$30,$31-$38,$40-$4D` promovido; `$39-$3F/$4E-$4F` descartados; entry/exit via `$50` demostrados. | `SCENE_BATTLE_ENGINE_STATE_30_4F.md`, `SceneBattleEngineStateGraph.cs` |
| Combate de jefes | MEDIUM-HIGH | Recursos, daño, dodge, técnicas, AI y dispatch de eventos promovidos; outer engine graph ahora cerrado, pero faltan contextos end-to-end de algunos bosses. | `BOSS_BATTLE_RESOURCES.md`, `BOSS_BATTLE_DAMAGE.md`, `BOSS_DODGE.md`, `BATTLE_TECHNIQUES.md`, `BATTLE_EVENT_DISPATCH.md` |
| Life/Cosmo/Seventh Sense | HIGH en plataforma / MEDIUM-HIGH global | Drain normal/fatal cerrados; quedan contextos globales/boss específicos. | `RESOURCE_ECONOMY.md`, `PLATFORM_PRE_PLAYER_RESOURCE_ORDER.md`, `PLATFORM_RESOURCE_FAILURE_STATE_60.md` |
| Texto / localización | MEDIUM | Corpus JP y borrador ES estructurado; falta integración/revisión final. | `TEXT_ENGINE.md`, `docs/LOCALIZATION.md`, corpus privado Drive |
| Password | HIGH en codec/runtime principal | Codec/fixture y ramas runtime `$11-$14` + `$91-$92` usan el mismo builder `$AE18`. | `PASSWORD_SYSTEM.md`, `ENGINE_STATE_FAMILY_11_14.md`, `ENGINE_STATE_FAMILY_91_99.md` |
| Modal state `$50` | MEDIUM-LOW | Entradas/salidas ya ancladas; falta cerrar mini-máquina `$0200/$0201/$0202` main/NMI. | `PROJECT_STATE.md`, `$DA13-$DB9C` |
| Renderer / metasprites / CHR | MEDIUM-LOW | Conocimiento parcial, sin render spec global. | `PLATFORM_CHR_MAP.md`, `BANK_MAP.md` |
| RNG | LOW-MEDIUM | Subsistema global abierto. | frontera futura |
| Audio | LOW | Sin especificación global equivalente a plataforma/combate. | frontera futura |
| REBORN moderno | EARLY | Se prioriza ORIGINAL SPEC integral antes de expansiones estructurales. | capa futura |

## Frontera operativa actual — snapshot

`PROJECT_STATE.md` sitúa el frente activo en:

```text
ORIGINAL SPEC / engine state $50 modal subsystem
```

PR #117 cerró el outer scene/battle graph:

```text
$50 resume -> transient $30 -> $31->$32->$33->$34
 -> NMI $35 -> $36->$37->$38 -> direct $40
 -> $41->$42->$43->$44->$45->$46->$47->$48->$49->$4A->$4B->$4C->$4D
 -> $50
```

Reachable set exacto:

```text
$30, $31-$38, $40-$4D
```

No producers canónicos:

```text
$39-$3F, $4E-$4F
```

El siguiente frente es el estado modal exacto `$50`, no una familia numérica `$50-$5F`: main `$DA13+`, NMI `$DABC`, control interno `$0200/$0201/$0202`, y exits confirmados hacia `$30/$10`.

Este snapshot nunca sustituye el `NEXT` de `PROJECT_STATE.md`.

## Checkpoints que no deben reabrirse sin evidencia nueva

- plataforma normal y resource drain promovidos;
- exits `$02=$00-$11`;
- narrativa `$70-$89`;
- reload `$04=$8F`;
- selector `$F025<->$A275` y destinos `$00/$10/$90`;
- dispatcher global main/NMI de PR #109;
- familia `$11-$14` de PR #111;
- fatal state `$60` / reload `$04=$FF` de PR #113;
- familia `$91-$99` de PR #115;
- scene/battle outer graph `$30-$4F` de PR #117.

Reapertura requiere fixture fallido, ROM contradictoria o un efecto lateral nuevo demostrado.

## Áreas globales abiertas

1. exact state `$50` modal subsystem y `$0200/$0201/$0202`;
2. cobertura end-to-end de todos los contextos de boss battle/progresión;
3. renderer/metasprites/CHR global;
4. RNG y consumidores;
5. audio / bank-track mapping;
6. texto/runtime + integración final de localización española;
7. revisión final de boot/menu/terminal modes y otros huecos globales que surjan del estado `$50`;
8. capa REBORN moderna sólo después de cerrar ORIGINAL SPEC con el criterio integral acordado.

## Recuperación

Una sesión nueva debe leer, en orden:

1. `docs/PROJECT_STATE.md` — checkpoint y único `NEXT`;
2. `docs/reverse-engineering/SCENE_BATTLE_ENGINE_STATE_30_4F.md` — checkpoint técnico previo;
3. `docs/reverse-engineering/ENGINE_STATE_DISPATCHER.md`;
4. fixed state-$50 anchors `$C14B`, `$DA13-$DB9C` y sólo helpers que gobiernen `$0200/$0201/$0202`;
5. `docs/WORK_PROTOCOL.md`;
6. este archivo sólo como mapa lateral;
7. manifiesto privado Drive sólo para localizar ROM/corpus/traces/save states/builds.

Nunca reconstruir `NEXT` desde una lista global de incógnitas.
