# Cierra Unity antes. Borra Library\PackageCache del proyecto usando rutas extendidas (\\?\)
# para que Windows no falle por longitud de ruta.

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $here
$packageCache = Join-Path $projectRoot "Library\PackageCache"

if (-not (Test-Path -LiteralPath $packageCache)) {
    Write-Host "No existe: $packageCache (nada que borrar)."
    exit 0
}

$extended = if ($packageCache -match '^\\\\\?\\') { $packageCache } else { "\\?\$packageCache" }
Write-Host "Borrando: $packageCache"
cmd /c "if exist `"$extended`" rmdir /s /q `"$extended`""
if (Test-Path -LiteralPath $packageCache) {
    Write-Warning "Quedaron restos; prueba cerrar Unity y volver a ejecutar, o borra Library entera."
    exit 1
}
Write-Host "PackageCache eliminado. Abre Unity y deja terminar la reimportación."
