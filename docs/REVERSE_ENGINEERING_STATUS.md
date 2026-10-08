# Estado de ingeniería inversa

Este documento registra el conocimiento vigente del target canónico.

## Target primario

**Saint Seiya: Ōgon Densetsu Kanketsu Hen** — Famicom, Japón, 1988.

ROM canónica verificada:

- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`
- CRC32 del archivo iNES: `F8D258A3`
- CRC32 sin header: `9561798D`
- 128 KiB PRG + 128 KiB CHR
- mapper 1 / MMC1

## CONFIRMED localmente

- La ROM es un iNES clásico de 262.160 bytes, sin trainer ni batería.
- Hay 8 bancos PRG de 16 KiB y 16 bancos CHR de 8 KiB físicos.
- RESET = `$C100`, NMI = `$C000`, IRQ/BRK = `$C003`.
- `$C000` salta al cuerpo NMI en `$D269`; `$C003` ejecuta `RTI`.
- RESET inicializa MMC1 con control `$1E`.
- `$1E` configura PRG de 16 KiB con último banco fijo en `$C000-$FFFF` y CHR en bancos de 4 KiB; el mirroring efectivo pasa a ser controlado por MMC1.
- Existen wrappers seriales MMC1 claramente identificados en el banco fijo:
  - `$C05A` -> control `$8000-$9FFF`;
  - `$C078` -> CHR bank 0 `$A000-$BFFF`;
  - `$C096` -> CHR bank 1 `$C000-$DFFF`;
  - `$C0B4` -> PRG bank `$E000-$FFFF`.
- RESET limpia RAM `$0000-$06FF`, inicializa `$00/$01` y transfiere control a `$DA13`.
- NMI realiza OAM DMA desde página `$07` y usa `$00/$01` como parte de un dispatcher de estados.

## CONFIRMED externamente / pendiente de reproducir dinámicamente

- TASVideos identifica esta misma revisión por SHA-1/MD5 y publica un `.wch` RAM map específico.
- Cheats comunitarios señalan múltiples ubicaciones relacionadas con Cosmo, Life/Energy y Seventh Sense, entre ellas `$0059/$005A`, `$0063/$0064`, `$05AA/$05AB`, `$05BC` y `$05CF`.
- Existe una traducción inglesa previa para este mismo juego. Su documentación declara hacking del motor de texto, edición gráfica y uso/introducción de codificación Huffman para ampliar el espacio disponible para texto.
- Existen romhacks chinos y portugueses, útiles como binarios diferenciales secundarios.

Nada de lo anterior debe convertirse en etiqueta semántica definitiva sin confirmar callers, lifetime y contexto de uso en nuestra ROM.

## INFERRED

- `$00/$01` forman parte de la máquina de estados global del engine.
- `$3A/$3B` participan en coordinación entre NMI y escrituras seriales MMC1.
- `$041B` funciona como señal auxiliar durante algunos cambios de banco, pero su papel exacto sigue abierto.
- El fixed bank concentra trampolines, manejo de interrupciones, bank switching y dispatch de alto nivel.
- La página `$07xx` es especialmente relevante: RESET no la borra y NMI la usa como fuente de OAM DMA.
- El juego distingue al menos tres grandes contextos con estructuras parcialmente diferentes: selección/configuración de Saint, plataforma y combate de jefe.

## DISPROVEN / retirado

Todo lo que en la investigación inicial pertenecía al primer **Ōgon Densetsu** de 1987 —mapper 152, direcciones `$0685/$0689`, tabla `$06CC-$06D0`, glitch de cursor del primer juego y dumps de texto de ese título— queda fuera del target canónico. Puede utilizarse sólo como comparación histórica del engine de TOSE, nunca como dato de Kanketsu Hen.

## UNKNOWN prioritarios

1. Mapa completo de bancos PRG y CHR por escena.
2. Mapa RAM verificado dinámicamente.
3. Máquina de estados completa de `$00/$01` y subestados asociados.
4. Física, colisiones y formato de los tramos de plataforma.
5. Formato de entidades/enemigos y spawners.
6. Estructura de animaciones/metasprites.
7. RNG y sus consumidores.
8. Fórmulas exactas de combate.
9. Estructura de técnicas, bloqueo/parry y daño.
10. Life, Cosmo y Seventh Sense: valores canónicos, copias y conversiones.
11. Selección y disponibilidad de los cinco Bronze Saints.
12. Flags de templos, muertes, diálogos y eventos favorables.
13. Formato de scripts/eventos.
14. Texto japonés: almacenamiento, índices, control codes y compresión original.
15. Diferencias exactas introducidas por el hack Huffman de traducción.
16. Password: codificación y decodificación.
17. Audio y bank/track mapping.
18. CHR bank usage y atlas visual por templo/personaje.

## Próximos experimentos

1. Enumerar callsites de `$C05A/$C078/$C096/$C0B4` y construir mapa de bancos.
2. Reproducir/obtener el RAM watch de TASVideos y verificar cada entrada con breakpoints.
3. Capturar boot, title, character select, Aries, plataforma y primer boss con un debugger NES.
4. Identificar cambios PRG/CHR por contexto.
5. Localizar las estructuras Life/Cosmo/Seventh Sense mediante escrituras controladas.
6. Trazar desde un texto visible hacia renderer, decoder y dispatcher de eventos.
7. Crear `symbols.map`, `ram-map.md` y `bank-map.md` versionables sin incluir la ROM.

## Comparadores secundarios

- primer Ōgon Densetsu (1987), sólo para genealogía del engine;
- traducción inglesa aishsha/Djinn y hacks derivados, como diferenciales técnicos;
- prototipo/beta si se consigue una copia legítimamente disponible para el usuario;
- WonderSwan Color Perfect Edition, como referencia de rediseño oficial.
