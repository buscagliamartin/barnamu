# Definí qué palabra querés buscar
$busqueda = "CheckAttack" 
# Definí la extensión de archivos donde buscar
$extensiones = "*.cpp", "*.h", "*.c" 

# Busca en todos los subdirectorios
$archivos = Get-ChildItem -Recurse -Include $extensiones

foreach ($archivo in $archivos) {
    $contenido = Get-Content $archivo.FullName
    if ($contenido -match $busqueda) {
        Write-Host "--- ENCONTRADO EN: $($archivo.FullName) ---" -ForegroundColor Green
        # Te muestra la línea donde aparece
        $contenido | Select-String -Pattern $busqueda | ForEach-Object { Write-Host $_ }
    }
}