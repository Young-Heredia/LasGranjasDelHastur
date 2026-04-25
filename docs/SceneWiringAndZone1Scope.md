# Referencia de escenas y alcance de Zone 1

Este documento sustituye la comprobación manual en el Editor: los datos salen de los archivos de escena en `Assets/00_Scenes/` y de los scripts en `Assets/01_Scripts/`.

## Convención de carpetas numeradas en `Assets/`

| Carpeta | Contenido |
|---------|-----------|
| `00_Scenes` | Escenas del build y prefabs embebidos de escena si aplica |
| `01_Scripts` | Código C# del juego: **dominio primero** (`Audio`, `Camera`, `Core`, `Intro`, `MainMenu`, `SceneManagement`, `UI`, zonas, `ZoneSelection`), **persona dentro** (`Edwin`, `Jose`, `Lucas`). |
| `02_Scripts` | Scripts o código auxiliar fuera de `01_Scripts` (antes bajo `03_Audio/Scripts`; misma convención de ensamblado Unity que el resto de `Assets/`). |
| `02_Sprites` | Texturas, sprites 2D y **Tilemaps** (`02_Sprites/Tilemaps/`; paletas y tiles de nivel) |
| `03_AUDIO` | Música y SFX |
| `04_Prefabs` | Prefabs reutilizables y datos serializados de juego (p. ej. `Zone1/Zone1Config.asset`; trabajo por autor bajo `Lucas/`, etc.) |
| `05_Fonts` | Fuentes TMP/TTF |

Los `.cs` bajo cualquier ruta que contenga una carpeta **`Editor/`** se compilan **solo en el Editor** (`UnityEditor`, `MenuItem`, etc.) y **no** entran en el build del jugador. Las herramientas del repo viven en **`01_Scripts/ZoneSelection/Editor/`** (constructores de escena ZoneSelection) y **`05_Fonts/Editor/`** (arreglo TMP New Rocker). **`Assets/Settings`** sigue en la raíz por convención Unity. **No** coloques código con `UnityEditor` dentro de `01_Scripts/...` sin una carpeta `Editor` en la ruta.

## Build order

Ver `ProjectSettings/EditorBuildSettings.asset`: `IntroComic` → `MainMenu` → `ZoneSelection` → `Zone1_Dungeons`.

## MainMenu (`MainMenu.unity`)

| Objeto / componente | Script | Inspector / eventos |
|---------------------|--------|---------------------|
| `AudioManager` | `AudioManager` | `musicSource` y `sfxSource` asignados; clips de intro, UI y música de menú; `altPanelIndices` 3 y 5; `autoPlayMusicByScene: 1`. |
| `MainMenuManager` | `MainMenuController` | `introSceneName`: IntroComic; `zoneSelectionSceneName`: ZoneSelection; `optionsPanel` enlazado al panel de opciones. |
| Mismo `MainMenuManager` | `SimpleOptionsController` | `musicSlider` → MusicSlider; `sfxSlider` → SFXSlider. |
| Botones (UnityEvent) | — | `PlayGame`, `OpenOptions`, `CloseOptions`, `QuitGame`, `ResetIntro` en `MainMenuController` (líneas de escena con esos `m_MethodName`). |

## IntroComic (`IntroComic.unity`)

| Objeto | Script | Inspector |
|--------|--------|-----------|
| `IntroManager` | `IntroComicController` | `comicImage`, `narrationText`, `nextButton`, `skipButton` referenciados; **6** entradas en `panels` y `narrations`; `delayPerCharacter` 0.035; `narrationBlipEveryNChars` 0; `mainMenuSceneName` MainMenu; `saveIntroAsSeen` 1. |
| `AudioManager` | `AudioManager` | Misma configuración de clips/música que en menú; si ya existe una instancia `DontDestroyOnLoad` desde otra escena, el duplicado se destruye en `Awake`. |

**Código vs Inspector:** `IntroComicController.Start()` hace `nextButton.onClick.AddListener(OnNextPressed)` y lo mismo para `SkipIntro`. En el YAML, `NextButton` / Skip pueden tener `m_OnClick.m_Calls` vacío; el flujo de clic depende del registro en tiempo de ejecución.

## ZoneSelection (`ZoneSelection.unity`)

| Objeto | Script | Inspector / jerarquía |
|--------|--------|------------------------|
| Raíz `Canvas` | `ZoneSelectionController` | `mainMenuSceneName` MainMenu; `zone1SceneName` Zone1_Dungeons; `zone2SceneName` Zone2_Cities; `zone3SceneName` Zone3_Celestial; `zoneCards` [Zona1, Zona2, Zona3] por fileID. |
| Jerarquía bajo Canvas (nombres usados por código) | — | `BackgroundSolid`, `Background`, `AmbientOverlay`, `PanelFrame`, `Header`, `CardsRow`, `BackButton` existen en la escena. |

### Tarjetas `ZoneCardUI`

Orden en `zoneCards`: coincide con **Zona 1**, **Zona 2**, **Zona 3** (objetos `ZoneCard_1` … `ZoneCard_3`).

| GameObject | `zoneNumber` | Título (resumen) |
|------------|----------------|------------------|
| ZoneCard_1 | 1 | Zona 1 – Calabozos |
| ZoneCard_2 | 2 | Zona 2 – Ciudades |
| ZoneCard_3 | 3 | Zona 3 – Cuerpos Celestes |

Cada tarjeta incluye `BasicUIAudio` con `useAudioManagerFirst: 1`; los clips efectivos se asignan en `ZoneCardUI.ApplyState()` y en `ZoneSelectionController.WireBackButtonAudio()` para `BackButton`.

### Botones de zona (UnityEvent → `ZoneSelectionController`)

- `EnterZone1`, `EnterZone2`, `EnterZone3`
- `BackButton` → `GoToMainMenu`

## Scripts en escena no usados en `00_Scenes`

- `SceneLoader.cs`: no aparece en `MainMenu`, `IntroComic` ni `ZoneSelection` (útil si enlazas `LoadSceneByName` desde UI genérica).

---

## Decisión de alcance: Zone1_Dungeons (jam)

**Objetivo del hito:** primera escena jugable coherente con la intro (granja / tributo / calabozos), sin bloquear el cierre del jam.

**Alcance mínimo acordado (MVP):**

1. **Un solo espacio** (una sala o un tilemap pequeño) explorable con cámara 2D ya plantilla URP.
2. **Bucle económico reducido:** una acción repetible de “producir” recurso (p. ej. interactuar con un altar / parcela) que incremente un contador visible en UI.
3. **Coste o riesgo leve** (tiempo de recarga, pérdida marginal, o texto de flavor) para que no sea solo un botón infinito sin contexto.
4. **Salida clara:** botón o zona que cargue `ZoneSelection` (o `MainMenu`) para cerrar el loop del build.
5. **Desbloqueo hacia Zona 2:** al cumplir una condición simple del MVP (umbral de recurso o una interacción final), llamar a `ZoneProgressState.SetZoneUnlocked(2, true)` una sola vez; opcional guardar progreso en `PlayerPrefs` adicional si hace falta.

**Fuera de este MVP (post-jam o si sobra tiempo):** combate complejo, múltiples mapas, IA, inventario grande, cinemáticas nuevas.

**Orden sugerido de implementación:** HUD + recurso → interacción en escena → condición de victoria / unlock → botón volver.
