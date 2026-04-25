# Unity 2D Medieval-Lovecraftian SFX Pack

Generado para un juego 2D pixel-art de gestión medieval-lovecraftiana en Unity.

## Especificaciones
- Formato: WAV PCM 48 kHz / 24-bit.
- One-shots: mono, inicio limpio, cola corta, pico aprox. -3 dB.
- Ambientes/loops: estéreo, diseñados para looping suave, pico aprox. -6 dB para headroom.
- Estética: oscura, mística, legible, no cartoon, sin beeps sci-fi modernos, sin reverb excesiva.

## Integración Unity sugerida
- One-shots UI/gameplay: `Load Type = Decompress On Load`, `Loop = Off`.
- Ambientes/event loops: `Load Type = Streaming` o `Compressed In Memory`, `Loop = On`.
- Mixer recomendado: buses `SFX_UI`, `SFX_Gameplay`, `SFX_Ambience`, `SFX_Events`.
- Para acciones frecuentes, reproducir variaciones aleatorias (`_01`, `_02`, `_03`) con pitch random leve ±3% y volumen random ±1 dB.

## Carpetas
1. `01_global_ux` — guardado/carga, warnings, transiciones, pausa.
2. `02_intro_comic` — impactos, sello, sting final y ambiente oscuro.
3. `03_ui_complete` — paneles, tabs, sliders, toggles, confirm/cancel, notificaciones.
4. `04_zone_selection_minigames` — selección, desbloqueos y minijuegos.
5. `05_zone1_dungeon` — producción, upgrades, limpieza, assistants, game over y loop de producción.
6. `06_zone2_ruined_city` — supplies, blueprints, tier up, economía, storage full.
7. `07_zone3_celestial` — residue, ink, tier up, endless, objectives, cosmic warning.
8. `08_random_events` — eventos aleatorios y loops.
9. `09_ambiences` — ambientes por zona y capas reutilizables.

## Cantidad
- Archivos WAV: 127
- Total assets incluyendo manifiestos: 129
