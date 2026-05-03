# Guía visual — Las Granjas del Hastur

Documento corto para alinear arte, prompts y Unity. La convención de carpetas numéricas sigue en [SceneWiringAndZone1Scope.md](SceneWiringAndZone1Scope.md).

## Estilo general

- **Lectura:** siluetas claras, pocos focos de detalle; horror cósmico **sin** gore explícito.
- **Tono:** granja ritual + ruina; frío en sombras, acentos tétricos o “enfermizos” (ocre, ámbar apagado) para ritual e iconografía.
- **UI:** convive el pack pixel `hastur_pixel_art_pack` con paneles propios `zone1_ui_panel_*` (ver `UIManager` / `ZonePrototypeUiChrome`).

## Pixel art

- **Regla:** píxeles legibles, sin blur; paletas acotadas; sombras suaves en pocos tonos.
- **Prompts por zona:**  
  - Z1: `Assets/02_Sprites/Lucas/Zone1/AssetPrompts_Zone1_Dungeons.txt`  
  - Z2: `AssetPrompts_Zone2_Cities.txt`  
  - Z3: `AssetPrompts_Zone3_Celestial.txt`

## Paleta de referencia (extraída del juego)

Valores aproximados en hex; el tono final puede variar por iluminación URP y tints en `SpriteRenderer` / UI.

| Uso | Hex | Notas |
|-----|-----|--------|
| Cielo / fondo cámara Z1 | `#080D1F` | `Zone1ArtTuner.cameraBackground` |
| Piedra muro (bloque fallback) | `#302E38` | Muros traseros `CreateBrokenWalls` |
| Piedra muro lateral | `#2B2933` | Variante ligeramente más fría |
| Suelo ritual / acento oscuro | `#4A1A2E` al ~18% alpha | Marca de ritual sobre celda |
| Niebla / suelo teñido (frío) | `#D1DCF5` | Base tint suelo `floorBaseTint` |
| Niebla alterna A / B | `#B8C1DB` / `#A3AEC7` | Variación de baldosas |
| Anillo celda seleccionada | `#F2D375` | `selectedRingTint` |
| Pulso “listo” | `#FAEE9E` | `readyPulseTint` |
| Título HUD | `#F2D699` | `titleTextTint` |
| Texto cuerpo HUD | `#E8EBF2` | `bodyTextTint` |
| Runas ambiente (capa fría) | `#EBF5FF` × ~18% alpha | `AmbientRunes` |
| Runas amarillas (capa cálida) | `#F2C748` × ~16% alpha | `AmbientRunesYellow` si no hay PNG dedicado |
| Impuesto / peligro UI | Rojo apagado sobre gris azulado | Botones danger del pack + paneles |

Zona 2 y 3 introducen hormigón azul-gris y violetas cósmicos; mantener acentos **apagados** para no competir con la lectura de celdas.

## Tamaños y rejillas (Unity 2D)

No hay un solo “pixel por mundo” global; se mezcla por tipo de asset (coherente con el código actual):

| Tipo | Tamaño típico / PPU | Dónde |
|------|---------------------|--------|
| Celdas UI / props HUD | Pack + `32` en `Zone1ArtProvider` al crear sprites desde PNG | Iconos, muchos props |
| Llama antorcha | **32×32** por frame, spritesheet | `zone1_torch_sheet.png` |
| Niebla baja | **96×48** por frame | `zone1_lowfog_sheet` |
| Fuente decorativa / gacha | **128×128** por frame | `zone1_fountain_spritesheet_4x1_128` |
| Baldosas suelo Z1 | Variantes en `NWzone1_floor_tile_*`; **PPU 32** al cargar vía `Zone1ArtProvider` | `CreateFloorTiled` |

**Recomendación:** nuevos tiles de suelo/muro en **32 PPU** salvo elementos “hero” (jefe, FX grande) donde **64** puede servir si se documenta en el nombre (`_64`).

## Botones, paneles, íconos

- **Botones:** `UI_Button_Primary` / `.png` peligro del pack; fallback híbrido Zone1.
- **Paneles:** `UI_Panel_*` del pack + `zone1_ui_panel_cell.png`, `_sales`, etc.
- **Íconos Z1:** `Assets/02_Sprites/Lucas/Zone1/Icons/Nzone1_icon_*.png` — prefijo `N` para evitar colisiones.

## Zona 1 — calabozo (implementación)

- **Suelo:** baldosas `NWzone1_floor_tile_01` … `06` (obligatorias en repo).
- **Muros:** fallback = rectángulos coloreados; si añades los PNG listados en `Zone1DungeonArtPaths`, el runtime los usa automáticamente.
- **Borde ornamental:** `zone1_overlay_dungeon_frame.png` (opcional).
- **Runas amarillas:** capa `AmbientRunesYellow`; PNG opcional o tinte sobre `zone1_overlay_runes.png`.

## Enlaces

- Prompts Z1: `Assets/02_Sprites/Lucas/Zone1/AssetPrompts_Zone1_Dungeons.txt`
- Rutas opcionales en código: `Zone1DungeonArtPaths.cs`
