# Ejecutar con Unity CERRADO. Crea caché UPM en ruta corta (evita MAX_PATH en Windows).
# Requiere PowerShell. Si prefieres otra unidad, cambia $CacheRoot.

$ErrorActionPreference = "Stop"
$CacheRoot = "C:\UnityPkgCache_LGH"
New-Item -ItemType Directory -Force -Path $CacheRoot | Out-Null

$upmPath = Join-Path $env:USERPROFILE ".upmconfig.toml"
$rootForward = $CacheRoot.Replace("\", "/")
$content = @"
[cache]
root = "$rootForward"
"@

if (Test-Path -LiteralPath $upmPath) {
    Write-Host "Ya existe $upmPath — no lo sobrescribo."
    Write-Host "Asegúrate de tener una sección [cache] con root en ruta CORTA, por ejemplo:"
    Write-Host $content
} else {
    Set-Content -LiteralPath $upmPath -Value $content -Encoding utf8
    Write-Host "Creado: $upmPath"
}

Write-Host "Caché UPM en: $CacheRoot"
Write-Host "Siguiente: ejecuta Clear-ProjectPackageCache.ps1 y abre el proyecto en Unity."
