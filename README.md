Las Granjas del Hastur

Documentación de cableado de escenas y alcance MVP de Zone 1: [docs/SceneWiringAndZone1Scope.md](docs/SceneWiringAndZone1Scope.md).

Contenido principal de `Assets/`: `00_Scenes`, `01_Scripts`, `02_Sprites`, `03_AUDIO`, `04_Prefabs`, `05_Fonts` (detalle en el doc enlazado). En `01_Scripts` hay carpetas por desarrollador: `Edwin`, `Jose`, `Lucas` (código compartido puede seguir en `UI`, `Zone1`, etc.).

**UPM / `DirectoryNotFoundException` (collections, visual scripting, etc.):** la ruta del proyecto es larga; Windows y el ApiUpdater fallan al leer DLLs bajo `Library\PackageCache`. **Con Unity cerrado:** (1) `Tools\Configure-UpmShortCache.ps1` — caché UPM en `C:\UnityPkgCache_LGH`; (2) `Tools\Clear-FullLibrary.ps1` — borra **toda** `Library` y obliga reimport limpio (mejor que solo `PackageCache` si entraste en modo seguro). Luego abre Unity. **Se quitó `com.unity.visualscripting`** del proyecto (no se usaba en `Assets/` y añadía rutas DLL muy profundas). Errores de Rider/VS con `TestRunner` suelen ser en cadena: se corrigen al compilar bien tras borrar `Library`. El `manifest` usa `resolutionStrategy: highestMinor` y `enableLockFile: false`.
