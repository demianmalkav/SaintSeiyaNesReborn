# SaintSeiyaNesReborn

Reingeniería, especificación y reconstrucción moderna para Windows de **Saint Seiya: Ōgon Densetsu (Famicom/NES, 1987)**.

## Objetivo

El proyecto parte de la versión japonesa como referencia canónica de comportamiento. La meta no es ampliar la ROM indefinidamente, sino comprender el juego original, documentarlo de forma verificable y reconstruir sus sistemas en una arquitectura moderna que permita expandir gráficos, animación, audio, narrativa y mecánicas sin perder el ADN del original.

## Dos capas obligatorias

- **ORIGINAL SPEC**: qué hace realmente el juego de 1987, demostrado mediante ROM, trazas, RAM, desensamblado y pruebas reproducibles.
- **REBORN**: decisiones modernas de diseño, expansión y presentación. Nunca debe usarse REBORN para inferir cómo funcionaba el original.

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

1. Identificar por hash la ROM japonesa aportada por el usuario.
2. Separar PRG/CHR y cartografiar bancos del mapper 152.
3. Crear mapa inicial de RAM y símbolos.
4. Instrumentar texto, combate, movimiento y eventos.
5. Producir una especificación ejecutable/reproducible del original.
6. Construir la implementación nativa para Windows y expandirla bajo la capa REBORN.
