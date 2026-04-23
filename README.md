Las Granjas del Hastur

Documentación de cableado de escenas y alcance MVP de Zone 1: [docs/SceneWiringAndZone1Scope.md](docs/SceneWiringAndZone1Scope.md).

Contenido principal de `Assets/`: `00_Scenes`, `01_Scripts`, `02_Sprites`, `03_AUDIO`, `04_Prefabs`, `05_Fonts` (detalle en el doc enlazado). En `01_Scripts` hay carpetas por desarrollador: `Edwin`, `Jose`, `Lucas` (código compartido puede seguir en `UI`, `Zone1`, etc.).

**UPM / `DirectoryNotFoundException` en `com.unity.collections`…Unsafe.dll:** la ruta del proyecto es muy larga y Windows corta archivos en `Library\PackageCache`. Opciones: (1) activar rutas largas en el sistema; (2) mover el repo a una ruta corta (p. ej. `C:\Dev\LasGranjas`); (3) copiar `upmconfig.example.toml` a `%USERPROFILE%\.upmconfig.toml`, crear la carpeta `root` indicada ahí y reiniciar Unity para usar caché UPM fuera del proyecto. Tras cambiar dependencias, abre Unity para que regenere `packages-lock.json`.
