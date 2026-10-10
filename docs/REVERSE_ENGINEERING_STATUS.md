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
| Aquarius/Camus `$08` | ACTIVE | Próximo contexto: unlocks Hyoga, `$06B8/$06E1`, dodge-history selector y redirecciones. |
| Otros boss contexts | MEDIUM | Pisces, Saga y stages omitidos aún requieren composición/auditoría. |
| Renderer / metasprites / CHR | MEDIUM | Spec global pendiente. |
| RNG | LOW-MEDIUM | Abierto. |
| Audio | LOW | Abierto. |
| Texto/localización | MEDIUM | Corpus JP + borrador ES; integración final pendiente. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Checkpoint técnico reciente — PR #127

Virgo/Shaka `$05` quedó cerrado end-to-end.

Resultados centrales:

- `$9A28` define el guard/substitution machine de Ikki y releases `$DD/$03/$FE`;
- non-Ikki con marker `$0673=$3F` sustituye a Ikki, arma `$0690=$FF` y entra por `$03`;
- Talk non-Ikki fuerza Gold response; Ikki pre-detour (`$067C=0`) no; Ikki post-detour sí;
- primer post-Bronze de Ikki fuerza `$02=$0D` + release `$02` hacia plataforma;
- plataforma `$0D` vuelve por `$3D`; fixed resume incrementa `$067C` sin el reset normal;
- post-detour se limpia `$0690`; `$06E0` es one-shot; primer `$EB!=0` se convierte en `$064D++/$0683++` en lugar de victoria normal;
- siguiente Ikki post-Bronze con `$0683!=0` sale por `$FE`, fuerza Seiya y avanza progreso;
- post-Gold Ikki trata `$EA=$01` como derrota una vez `$0683!=0`;
- selector stage5: non-Ikki slots 0/1; Ikki slot2 forzado; slot3 inalcanzable.

```text
merge b209b2106a6ea48b15fe69a71976d51e34d641be
head  2007ea765f28e245567fca238c9a2bf2d987816f
CI    #324 SUCCESS / #520 SUCCESS
```

Artifacts: `BOSS_CONTEXT_STAGE_05_VIRGO.md`, `VirgoStage05Context.cs` y fixtures.

## Frontera operativa actual

```text
ORIGINAL SPEC / boss context stage $08 Aquarius
```

Aquarius/Camus usa:

```text
init        $9B14
Talk        $9F00
post-Bronze $A8FC
post-Gold   $A9D3
selector    bank6 $90A8+
```

El próximo cierre debe resolver `$06B8/$06E1/$066F`, los dos unlocks de Hyoga (`$0588/$0696`), dodge history `$0677/$0678`, redirecciones de stage y slots Gold alcanzables.

## No reabrir sin evidencia nueva

- global `$00/$01` — #121;
- Taurus — #123;
- Leo — #125;
- Virgo — #127;
- front-end/title — #119;
- attract — #117/#119;
- `$11-$14`, `$60`, `$91-$99` — #111/#113/#115;
- plataforma/reload/narrativa promovidos;
- boss damage/resources/dodge/techniques genéricos.

## Áreas abiertas

1. Aquarius/Camus `$08` end-to-end;
2. Pisces/Aphrodite, Saga y revisión de stages omitidos;
3. renderer/metasprites/CHR global;
4. RNG;
5. audio;
6. texto runtime/localización final;
7. campos persistentes/progresión provisionales;
8. auditoría final ORIGINAL SPEC;
9. REBORN sólo después del cierre integral.
