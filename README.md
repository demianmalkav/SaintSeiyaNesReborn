# SaintSeiyaNesReborn

Reingeniería, especificación y reconstrucción moderna para Windows de **Saint Seiya: Ōgon Densetsu Kanketsu Hen (Famicom, 1988)**.

## Objetivo

El proyecto parte de la versión japonesa como referencia canónica de comportamiento. La meta no es ampliar la ROM indefinidamente, sino comprender el juego original, documentarlo de forma verificable y reconstruir sus sistemas en una arquitectura moderna que permita expandir gráficos, animación, audio, narrativa y mecánicas sin perder el ADN del original.

## Dos capas obligatorias

- **ORIGINAL SPEC**: qué hace realmente el juego de 1988, demostrado mediante ROM, trazas, RAM, desensamblado y pruebas reproducibles.
- **REBORN**: decisiones modernas de diseño, expansión y presentación. Nunca debe usarse REBORN para inferir cómo funcionaba el original.

## Target canónico

`Saint Seiya - Ougon Densetsu Kanketsu Hen (Japan).nes`

- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`
- mapper 1 / MMC1
- 128 KiB PRG + 128 KiB CHR

Los hashes y la geometría completos están en `docs/reverse-engineering/CANONICAL_ROM.md`. La ROM no vive en este repositorio público.

## Idioma

La ROM japonesa es la fuente técnica y narrativa primaria. El juego final tendrá **español** como idioma principal. El japonés se preserva como referencia y la localización usa identificadores estables.

## Estado de evidencia

Todo hallazgo técnico se etiqueta como:

- `CONFIRMED`: demostrado por código, traza, memoria o reproducción controlada.
- `INFERRED`: hipótesis consistente con evidencia disponible, todavía sin prueba concluyente.
- `UNKNOWN`: pendiente de investigar.
- `DISPROVEN`: hipótesis descartada por evidencia posterior.

## Fuentes de verdad y reanudación

Este README es **orientativo** y no decide qué se investiga a continuación.

Para retomar el proyecto sin contexto previo:

1. leer `docs/PROJECT_STATE.md`; es la **única fuente operativa** para el checkpoint aceptado, límites abiertos y `NEXT`;
2. consultar los documentos técnicos, código y tests que ese estado cite para la frontera activa;
3. usar `docs/REVERSE_ENGINEERING_STATUS.md` como mapa global de subsistemas, nunca como cola de trabajo;
4. aplicar `docs/WORK_PROTOCOL.md` para `FAST -> VERIFY -> CHECKPOINT -> NEXT`;
5. cuando hagan falta activos privados, consultar el `PRIVATE_WORKSPACE_MANIFEST — Saint Seiya Reborn` en la raíz privada de Drive y el `EVIDENCE_INDEX` de `04_REVERSE_ENGINEERING`.

Si README, documentos históricos, Drive o contexto de chat contradicen `docs/PROJECT_STATE.md` reconciliado con `main`, prevalecen `PROJECT_STATE.md` y el historial Git verificado.

## Estado técnico general

La auditoría integral de ORIGINAL SPEC clasifica los subsistemas materiales del juego original como cerrados o deliberadamente fuera de alcance y no identifica gaps materiales pendientes. El detalle y la matriz de ownership viven en `docs/reverse-engineering/ORIGINAL_SPEC_CLOSURE_AUDIT.md`.

ORIGINAL SPEC funciona como baseline semántico congelado. REBORN ya posee una arquitectura inicial separada: `Reborn.Core` contiene runtime/dominio determinista sin dependencia de NES u OriginalSpec, mientras `Reborn.OriginalBridge` actúa como anti-corruption layer para traducir contratos canónicos a DTOs modernos. La frontera completa está documentada en `docs/reborn/ARCHITECTURE.md`.

El primer slice arquitectónico de texto/localización ya prueba end-to-end el bridge con fixtures sintéticos. El siguiente frente es el primer slice de gameplay: locomoción horizontal y facing del jugador de plataforma, todavía sin colisiones, hazards, mapas, ataques, movimiento vertical ni rendering.

El detalle global por subsistema vive en `docs/REVERSE_ENGINEERING_STATUS.md`; el punto exacto de continuación vive exclusivamente en `docs/PROJECT_STATE.md`.

## Propiedad intelectual y almacenamiento

GitHub contiene código propio, documentación, tests y tablas reconstruidas. No se versionan ROMs, dumps completos de PRG/CHR, manuales escaneados protegidos, música ni gráficos originales extraídos.

Drive es el almacén privado para ROM, localización de trabajo, save states, trazas extensas, capturas, dumps temporales, manuales aportados y builds grandes. Su manifiesto privado localiza esos activos, pero nunca mantiene un `NEXT` independiente.
