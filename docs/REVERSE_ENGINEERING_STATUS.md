# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper y mapa PRG establecidos. |
| Máquina global `$00/$01` | HIGH | PR #121: namespace completo cerrado. |
| Front-end/title/password | HIGH | Modal, attract, codec y overlays ejecutables CHR->RAM cerrados. |
| Plataforma / exits / reload / narrativa | HIGH | Sistemas principales y handoffs ampliamente promovidos. |
| Boss primitives | HIGH | Recursos, clasificadores, daño, técnicas, dodge y dispatchers. |
| Taurus/Aldebaran `$01` | HIGH | Contexto completo, PR #123. |
| Leo/Aioria `$04` | HIGH | Contexto completo, PR #125. |
| Virgo/Shaka `$05` | HIGH | Contexto completo con Ikki + plataforma `$0D` + releases especiales, PR #127. |
| Aquarius/Camus `$08` | HIGH | Dos encuentros Camus, unlocks Hyoga, `$06B8/$06E1`, selector y releases cerrados, PR #129. |
| Pisces/Aphrodite `$09` | HIGH | Roster Seiya/Shun, Talk/dodge, Shun growth, selector 0→1→2 y `$FE->$0C` cerrados, PR #131. |
| Final-special `$0C` | HIGH | Init-overrun inalcanzable, Talk compartido, rosas `$12->$0C`, release `$01` y handoff a Saga cerrados, PR #133. |
| Saga `$0A` | ACTIVE | Máquina final multiphase `$06CE`; próximo cierre end-to-end. |
| Finales posteriores / ending | MEDIUM | Deben emerger del cierre de Saga; no asumir terminal antes de probar releases. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #133

Final-special `$0C` quedó cerrado end-to-end sobre la ROM canónica verificada.

Resultados centrales:

- Pisces deja dos entradas exactas: Seiya `$06CD=$0E/$0673=$3E` o Shun `$06CD=$0B/$0673=$3B` en `$067D/$050E=$0C`;
- el supuesto init de stage 12 es un overrun de tabla que decodifica `$8D00`, pero el flujo canónico lo evita porque `$F36F[$0C]=$FF` no puede coincidir con Seiya/Shun;
- Talk `$A1AD` no distingue personajes: siempre `$D3/$D5`; primera vez da +1000 Seventh Sense e incrementa `$066F`, luego sólo repite diálogo;
- Escape está bloqueado por `$D4`; resource allocation está suprimido;
- Attack conserva selección/presentación de técnica, pero no puede cancelarse y no aplica daño normal al oponente;
- `$FF9F -> bank0 $B900` usa temporalmente `$050E=$12`, ejecuta el efecto de rosas hasta `$06C1=$40`, termina `$0632=$1A` y restaura `$050E=$0C`;
- post-Bronze, Gold response/dodge/damage y post-Gold son inalcanzables por gates fijos para stage `$0C`;
- Attack emite release `$01`;
- release `$01` hace `$067D $0C->$0D`, `$06CD=$0E`, `$0673=$3E`, y `$F016[$0D]=$0A` selecciona Saga;
- la identidad activa Seiya/Shun se conserva hasta el boundary de Saga.

```text
merge 140f7eadf62e3a05d66fbe5448aa542005ed4d76
head  d08f8a3a59c945409d113978d6bccce96bffa987
CI    #337 SUCCESS / #533 SUCCESS
```

Artifacts: `FINAL_SPECIAL_STAGE_0C.md`, `FinalSpecialStage0CContext.cs`, fixtures y corrección de `BATTLE_EVENT_DISPATCH.md`.

## Frontera operativa actual

```text
ORIGINAL SPEC / final boss stage $0A Saga
```

La frontera entra desde story progress `$0D` con descriptor `$06CD=$0E`, roster `$0673=$3E` y dos variantes de Santo activo todavía vivas: Seiya o Shun.

Saga usa una máquina de fases explícita en `$06CE`:

```text
init        $9B5D -> $9B69 / $9B9E / $9C2C
Talk        $9FF4 -> $A000 / $A0E0 / $A115
post-Bronze $AB18 -> $AB24 / $AB62 / $AB6F
post-Gold   $AC05 -> $AC11 / $AC3E / $AC76
selector    bank6 $90EC+ -> $9104 / $911D / $9135
```

Anchors iniciales muestran sustitución/retorno de Santo, `$06CF/$06D0/$066F`, `$064D/$0649`, bloqueo `$0690`, weakening `$0681`, releases especiales y selector Gold distinto por fase. El writer tardío `$F497` fija la tercera técnica de Seiya y +1000 Seventh Sense, pero todavía debe unirse a su fase Saga exacta.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- Aquarius — #129;
- Pisces — #131;
- final-special `$0C` — #133;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. Saga `$0A` multiphase end-to-end y su terminal real;
2. revisión de stages de boss omitidos si la auditoría de cobertura los exige;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. campos persistentes/progresión provisionales;
8. auditoría final ORIGINAL SPEC;
9. REBORN sólo después del cierre integral.
