# Cierra Unity. Borra toda la carpeta Library (reimportación completa).
# Usa ruta extendida \\?\ para evitar fallos de MAX_PATH al borrar.

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $here
$library = Join-Path $projectRoot "Library"

if (-not (Test-Path -LiteralPath $library)) {
    Write-Host "No existe Library."
    exit 0
}

$extended = if ($library -match '^\\\\\?\\') { $library } else { "\\?\$library" }
Write-Host "Borrando Library completa (puede tardar)..."
cmd /c "if exist `"$extended`" rmdir /s /q `"$extended`""
if (Test-Path -LiteralPath $library) {
    Write-Warning "No se pudo borrar del todo (¿Unity abierto?). Cierra Unity y vuelve a ejecutar."
    exit 1
}
Write-Host "Listo. Abre el proyecto en Unity (primera vez tardará)."
