# Estado inicial de ingeniería inversa

Este documento registra el conocimiento previo a recibir la ROM de trabajo.

## Target primario

**Saint Seiya: Ōgon Densetsu** — Famicom/NES, Japón, 1987.

La ROM aportada se identificará por hash antes de cualquier análisis. No se asumirá que coincide con una revisión conocida hasta verificarlo.

## CONFIRMED externamente / pendiente de reproducir localmente

- El cartucho japonés usa mapper 152.
- Arquitectura reportada: 128 KiB PRG-ROM + 128 KiB CHR-ROM.
- Mapper 152: ventana PRG conmutada de 16 KiB en `$8000-$BFFF`, banco final fijo en `$C000-$FFFF`, banco CHR de 8 KiB y mirroring one-screen controlado por registro.
- El mapper presenta bus conflicts.
- Existen dumps comunitarios de tres bloques de texto extraídos mediante tablas de punteros.
- Uno de esos bloques contiene texto de interfaz de combate, desplazamiento, Cloth y password.
- Fuentes comunitarias identifican `$0685/$0686` y `$0689/$068A` con valores relacionados con Cosmo/Power y Damage/Life.
- Investigación TAS moderna muestra que `$06CC-$06D0` puede modificar potencia de técnicas en combate.
- El glitch de cursor puede alcanzar y corromper estado fuera del rango normal de atributos; una prueba identifica `$06D5` como una de las celdas afectadas.

Todo lo anterior debe reproducirse contra nuestra ROM antes de marcarse como `CONFIRMED` dentro de ORIGINAL SPEC.

## INFERRED

- El banco PRG fijo probablemente concentra vectores e infraestructura común de engine/bank switching.
- Existen varias representaciones o buffers de estado (persistente, menú, combate) y no una única estructura lineal de atributos.
- Las tablas de texto permiten localizar callers del motor de diálogo y, desde allí, el dispatcher de eventos.
- Las escrituras a `$06CC-$06D0` constituyen una entrada prometedora para localizar inicialización de combatiente, técnicas y resolución de daño.

## DISPROVEN / no adoptar

- No asumir `$06A3` como base de una estructura lineal sólo porque `index 50 -> $06D5`. La relación no explica otras direcciones documentadas y necesita observación del código real.

## UNKNOWN prioritarios

1. Hash y revisión exacta de la ROM de trabajo.
2. Distribución real de código/datos entre bancos PRG.
3. Rutinas de bank switching y manejo de bus conflicts.
4. Mapa completo de RAM.
5. Máquina de estados del jugador en action mode.
6. Física, colisiones y formato de mapas.
7. Formato de entidades/enemigos.
8. RNG.
9. Fórmula exacta de daño.
10. Inicialización y consumo de Cosmo.
11. Estructura de técnicas y thresholds de movimientos especiales.
12. Cloth y Gold Cloth: condiciones y modificadores.
13. Flags narrativos y tabla de progreso.
14. Formato de eventos/scripts.
15. Sistema de diálogo y control codes.
16. Password: codificación/decodificación.
17. Audio y asignación de bancos/temas.
18. Sprites, animaciones y metatiles.

## Primer experimento cuando llegue la ROM

1. Calcular SHA-1, MD5, CRC32 y tamaño.
2. Parsear header iNES/NES 2.0.
3. Extraer PRG/CHR a un workspace privado.
4. Verificar mapper y geometría de bancos.
5. Localizar vectores RESET/NMI/IRQ.
6. Desensamblar banco fijo y detectar escrituras al registro del mapper.
7. Ejecutar con debugger y capturar el boot.
8. Confirmar direcciones RAM conocidas mediante watches/breakpoints.
9. Correlacionar punteros de texto conocidos con offsets reales.
10. Crear el primer `symbols.map` y `ram-map.md` reproducibles.

## Comparadores secundarios

Como evidencia comparativa se considerarán, si están disponibles legalmente para el usuario:

- versión francesa oficial PAL;
- Saint Seiya: Ōgon Densetsu Kanketsu Hen;
- WonderSwan Color Perfect Edition.

No sustituyen a la ROM japonesa como referencia canónica.
