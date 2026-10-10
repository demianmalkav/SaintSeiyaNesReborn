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
| Final-special `$0C` | ACTIVE | Sucesor probado de Pisces; requiere clasificar entry/commands/Talk y handoff a `$0D->$0A` Saga. |
| Saga `$0A` y finales posteriores | MEDIUM | Aún requieren composición/auditoría después de cerrar `$0C`. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #131

Pisces/Aphrodite `$09` quedó cerrado end-to-end sobre la ROM canónica verificada.

Resultados centrales:

- Aquarius `$FE` lleva `$067D $0A->$0B`; `$0B` selecciona stage `$09`;
- descriptor de historia produce `$0673=$3A`; el gate genérico demuestra que sólo Seiya y Shun son seleccionables en Pisces;
- `$9B5C` es `RTS`, sin init local de Aphrodite;
- Talk `$9F99` usa `$0677+$0678`: antes de dos intentos no fuerza respuesta; desde dos, el primer Talk de Shun con `$066F=0` da +1000 Seventh Sense e incrementa `$066F`, mientras el resto fuerza Gold response;
- cada Bronze action incrementa `$064D/$EF`;
- en el espacio alcanzable, `$0533!=0` equivale a Shun; `$EF==2` y `$EF==5` incrementan `$0589/$0696`, exponiendo Nebula Stream y Nebula Storm;
- los thresholds son igualdad exacta: una acción de Seiya en 2/5 pierde ese crecimiento;
- `$EB=$01` instala `$064D=$80`, latch de low-opponent que también fuerza de inmediato el tier Gold máximo;
- selector stage9: `$064D 0..2 -> slot0`, `3..5 -> slot1`, `>=6 -> slot2`; slot3 inalcanzable;
- victoria sobre Aphrodite da +1200 Seventh Sense y sale por `$FE`; `$EA=$FF` usa derrota genérica `$FF`;
- `$FE` genérico pone a cero Life/Cosmo activos antes del avance;
- el avance probado es `$067D $0B->$0C`, stage `$0C`, con dos variantes: Shun-Pisces -> Seiya (`$06CD=$0E/$0673=$3E`) y Seiya-Pisces -> Shun (`$06CD=$0B/$0673=$3B`).

```text
merge 002ecd86361ccc6acca028fbafda5e16df41b3a6
head  92d428cb30ced7f6c6a9f403571aafb4f8cd7378
CI    #333 SUCCESS / #529 SUCCESS
```

Artifacts: `BOSS_CONTEXT_STAGE_09_PISCES.md`, `PiscesStage09Context.cs`, fixtures y actualización de `BATTLE_TECHNIQUES.md`.

## Frontera operativa actual

```text
ORIGINAL SPEC / final-special stage $0C
```

La frontera no debe tratarse como un boss context ordinario. Evidencia inicial:

```text
story progress $0C -> stage $0C
Talk dispatcher -> $A1AD
post-Bronze      -> $A3A1 (RTS)
post-Gold        -> $A3A1 (RTS)
```

`$A1AD` tiene variantes Seiya/Shun y una rama one-shot `$066F==0` que llama `$A1FF` con `#$10`. Además existen ramas fijas específicas de stage `$0C` fuera de los dispatchers de boss: una alrededor de `$F08B+` termina escribiendo `$0670=$01`, y otra alrededor de `$F14A+` emite mensaje `$D4`. El init slot de stage `$0C` sigue siendo no estándar y debe clasificarse antes de modelar. El siguiente story index `$0D` mapea a stage `$0A` Saga, pero la transición `$0C->$0D` todavía debe probarse.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- Aquarius — #129;
- Pisces — #131;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. final-special stage `$0C` end-to-end;
2. Saga `$0A` y contextos finales posteriores;
3. revisión de stages de boss omitidos si la auditoría de cobertura los exige;
4. renderer/metasprites/CHR global;
5. RNG;
6. audio;
7. texto runtime/localización final;
8. campos persistentes/progresión provisionales;
9. auditoría final ORIGINAL SPEC;
10. REBORN sólo después del cierre integral.
