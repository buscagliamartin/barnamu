# BarnaMuWeb

Sitio web público de BarnaMu. ASP.NET Core Razor Pages, .NET 10, conecta directo a la DB
Postgres del server por Npgsql + Dapper. Hashea passwords con BCrypt.Net-Next (misma
versión que OpenMU), así las cuentas creadas desde la web loguean en el juego sin un
paso extra.

## Páginas

- `/` — home con novedades, hero y stats
- `/Register` — alta de cuenta (rate-limited a 5/h por IP)
- `/Vip` — beneficios, rates, mapas, cómo comprar (PayPal + WhatsApp)
- `/Guide` — guía del servidor (contenido editable en `Content/guide.json`)
- `/Rankings` — top 50 por experiencia / master / PK
- `/Status` — TCP probes a Connect + Game servers + contadores DB
- `/Download` — link al cliente
- `/ReportBug` — form que va a `web."BugReport"` (rate-limited a 10/h por IP)

## Lo que tenés que editar en `appsettings.json` antes de abrir

Sección `BarnaMu`:

| Clave                  | Qué poner                                                         |
|------------------------|-------------------------------------------------------------------|
| `DiscordInviteUrl`     | El invite real de Discord                                         |
| `ClientDownloadUrl`    | (ya está el de Mega)                                              |
| `ClientChecksum`       | SHA256 del ZIP del cliente — **opcional**, ver abajo              |
| `GameServerPorts`      | Sólo los puertos que realmente corren game server (default: `[55901]`) |
| `Vip.PriceText`        | Precio que mostrás (ej. `"USD 5 / ARS 5.000"`)                    |
| `Vip.PayPalUrl`        | Tu link de paypal.me                                              |
| `Vip.WhatsAppUrl`      | `https://wa.me/54911XXXXXXXX` (sin espacios ni `+`)               |
| `Vip.WhatsAppDisplay`  | Cómo se muestra el número (ej. `"+54 9 11 1234-5678"`)            |
| `Maps[]`               | Niveles de entrada normales/VIP. Corregí los que cambiaste y completá Crywolf/Balgass |

### ¿Qué es `ClientChecksum`?

Es el hash SHA256 del ZIP del cliente. Si lo configurás, la página de descarga lo muestra
y la gente puede verificar que el download no esté corrupto. **Es 100% opcional**, si lo
dejás vacío la línea no se muestra. Para calcularlo, en PowerShell:

```powershell
Get-FileHash "C:\ruta\al\cliente.zip" -Algorithm SHA256
```

Y pegás el `Hash` que te devuelve en `ClientChecksum`.

## Correr en desarrollo (local)

```cmd
cd C:\MuDev\BarnaMuWeb
dotnet run
```

Levanta en `http://localhost:5050`. Sólo accesible desde la VM.

## Hacer la web pública (paso a paso)

La idea: la web corre en el puerto **8081** de la VM, abrís ese puerto en el router y
listo. Mantengo el 80 reservado para el Admin Panel hasta que decidamos mover el panel
a un puerto LAN-only.

### 1. Publicar (build de release)

```cmd
cd C:\MuDev\BarnaMuWeb
dotnet publish -c Release -o C:\MuDev\BarnaMuWeb\publish
```

Esto genera `C:\MuDev\BarnaMuWeb\publish\BarnaMuWeb.exe` y todos los archivos que necesita
(incluye `appsettings.json`, `Content\news.json`, `Content\guide.json`, `wwwroot\...`).
Si cambiás `appsettings.json` o el contenido más adelante, podés editarlo directamente
adentro de `publish\` y reiniciar el proceso — no hace falta re-publicar.

### 2. Abrir Windows Firewall para el puerto 8081

PowerShell **como administrador**:

```powershell
New-NetFirewallRule -DisplayName "BarnaMu Web 8081" -Direction Inbound -LocalPort 8081 -Protocol TCP -Action Allow
```

### 3. Forwardear el puerto 8081 en el router

Igual que ya forwardeaste el 44405 y los 55901-06: hacé port forwarding de TCP **8081**
externo → **192.168.1.52:8081** (la IP fija de la VM).

### 4. Crear un `WebStart.bat`

En `C:\MuDev\` creá un archivo `WebStart.bat` con:

```bat
@echo off
set ASPNETCORE_URLS=http://*:8081
set ASPNETCORE_ENVIRONMENT=Production
:loop
"C:\MuDev\BarnaMuWeb\publish\BarnaMuWeb.exe"
echo Web cayó, reiniciando en 5 segundos...
timeout /t 5 /nobreak
goto loop
```

(Mismo patrón que tu `AutoRestart.bat` para el server.)

### 5. Task Scheduler para autostart

Creá una task igual que `BarnaMu-Server`:

- **Name:** `BarnaMu-Web`
- **Trigger:** At log on (o At system startup si la VM auto-loguea un user)
- **Action:** Start a program → `C:\MuDev\WebStart.bat`
- **Conditions:** desactivar "Start the task only if the computer is on AC power"
- **Settings:** activar "If the task fails, restart every 1 minute, up to 3 times"

Ejecutá la task una vez para arrancarla. Mirá Task Manager para verificar que
`BarnaMuWeb.exe` está corriendo.

### 6. Probar desde afuera

Desde tu celular (con datos, no WiFi) o desde otra red, andá a:

```
http://barnamu.ddns.net:8081/
```

Si responde y podés crear una cuenta y luego loguear con esa cuenta en el juego —
listo, está en línea.

### 7. Pasar a 80/443 (después)

Cuando quieras dejar la URL sin `:8081`:

1. Cambiá el puerto del Admin Panel de OpenMU a uno LAN-only (ej. 8088).
2. Cambiá `ASPNETCORE_URLS=http://*:80` en `WebStart.bat`.
3. Para HTTPS, lo más fácil es Cloudflare en modo proxy. Crear cuenta gratis, agregar
   `barnamu.ddns.net` no se puede (no es dominio propio), pero podés comprar un dominio
   tipo `barnamu.com.ar` y apuntar el A record a `83.43.3.212`. Cloudflare te da TLS
   gratis y te oculta la IP.

## Migrar al VPS más adelante

Como el proyecto no referencia assemblies internos de OpenMU, lo único que cambia al
mudarte a un VPS dedicado es:

- `ConnectionString` (apuntá al Postgres del VPS).
- `ConnectHost` y `GameServerHost` (IP/dominio nuevo del game server).
- Levantar el proceso (systemd / Windows service / docker).

## Seguridad antes de abrir al público de verdad

1. **Rotar la password de `postgres`** y crear un user `barnamu_web` con permisos
   limitados:
   ```sql
   CREATE ROLE barnamu_web LOGIN PASSWORD 'pass-fuerte';
   GRANT USAGE ON SCHEMA data TO barnamu_web;
   GRANT SELECT ON data."Account", data."Character" TO barnamu_web;
   GRANT INSERT ON data."Account" TO barnamu_web;
   GRANT USAGE, CREATE ON SCHEMA web TO barnamu_web;
   GRANT ALL ON ALL TABLES IN SCHEMA web TO barnamu_web;
   ```
   Y actualizá la `ConnectionString` con ese user.

2. **HTTPS** (Cloudflare o win-acme). HTTP plano es ok para test cerrado, no para
   producción.

3. **Captcha** en `/Register` si empieza a llegar spam (hoy hay sólo rate limiting).

## Esquema de la DB que toca la web

- **Lectura:** `data."Account"`, `data."Character"`.
- **Escritura:** `INSERT INTO data."Account"` (en registro).
- **Schema propio** que crea on-startup: `web."BugReport"`. Aislado, no interfiere con
  las migraciones de OpenMU.
