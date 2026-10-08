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

Los detalles completos están en `docs/reverse-engineering/CANONICAL_ROM.md`.

## Idioma

La ROM japonesa es la fuente técnica y narrativa primaria. El juego final tendrá **español** como idioma principal. El japonés se preservará internamente como referencia y cada línea localizada tendrá un identificador estable.

## Estado de evidencia

Todo hallazgo técnico se etiqueta como:

- `CONFIRMED`: demostrado por código, traza, memoria o reproducción controlada.
- `INFERRED`: hipótesis consistente con evidencia disponible, todavía sin prueba concluyente.
- `UNKNOWN`: pendiente de investigar.
- `DISPROVEN`: hipótesis descartada por evidencia posterior.

## Regla de propiedad intelectual

No se versionarán ROMs, dumps binarios originales, manuales escaneados ni otros materiales protegidos dentro del repositorio público. Ese material de referencia queda fuera de Git y se gestiona en el espacio privado de trabajo.

## Próxima fase

1. Completar mapa de bancos PRG/CHR y MMC1.
2. Construir mapa RAM verificado para selección, plataforma y combates.
3. Instrumentar texto, combate, movimiento, eventos y password.
4. Producir una especificación reproducible del original.
5. Construir la implementación nativa para Windows y expandirla bajo la capa REBORN.
