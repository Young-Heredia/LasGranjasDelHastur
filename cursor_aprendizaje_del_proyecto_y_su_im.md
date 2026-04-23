# Aprendizaje del proyecto y su implementación
_Exported on 21/4/2026 at 12:07:59 GMT-4 from Cursor (3.0.12)_

---

**User**

Empecemos por aprender de este proyecto, hasta el punto que esta implementado.

---

**Cursor**

Explorando el repositorio para mapear estructura, escenas y sistemas implementados.



Confirmando: Zone1 es plantilla URP 2D sin scripts de juego propios. Buscando referencias a `Game.unity` y la primera escena del build.



Teniendo una visión clara: el proyecto es un flujo de menús e intro cómic con audio persistente; la zona 1 está en escena pero sin lógica de gameplay en C#.



---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Implementando los todos del plan: no podemos abrir el editor de Unity aquí; extraeremos el cableado real desde los `.unity` y documentaremos el alcance mínimo de Zone1. Marcando el primer todo como en progreso y analizando las escenas.



Creando `docs/SceneWiringAndZone1Scope.md` con el cableado verificado desde los YAML de las escenas y el alcance acordado para Zone1.



Resumen de lo hecho:

## 1. Cableado escenas → scripts (todo “read-scene-wiring”)

No se puede abrir el Editor desde aquí, así que se **leyeron los YAML** de `MainMenu.unity`, `IntroComic.unity` y `ZoneSelection.unity` y se volcó la correspondencia con `MainMenuController`, `SimpleOptionsController`, `AudioManager`, `IntroComicController`, `ZoneSelectionController`, `ZoneCardUI`, UnityEvents y nombres de hijos del Canvas (`BackgroundSolid`, `Header`, `BackButton`, etc.).

Punto importante documentado: en **IntroComic**, Next/Skip pueden tener `onClick` vacío en el archivo de escena porque **`IntroComicController` registra los listeners en `Start()`**.

Archivo: [docs/SceneWiringAndZone1Scope.md](docs/SceneWiringAndZone1Scope.md).

## 2. Alcance MVP Zone 1 (todo “zone1-scope”)

En la misma guía quedó fijado el **MVP del jam** para `Zone1_Dungeons`: un espacio pequeño, bucle de “producción” con UI, coste/riesgo mínimo, salida a `ZoneSelection`/`MainMenu`, y desbloqueo de zona 2 con `ZoneProgressState.SetZoneUnlocked(2, true)` cuando se cumpla una condición simple; explícitamente **fuera** de ese hito: combate complejo, varios mapas, etc.

## 3. README

Se añadió una línea con enlace al doc anterior para que sea fácil de encontrar.

---

Los dos todos están **completados**. El archivo del plan en `.cursor/plans/` no se tocó.

