# BarnaMu — Sesión: VIP con timer + Web pública v1

> Checkpoint de sesión. Si se corta, retomar desde acá.
> Última actualización: 2026-05-15

## ✅ COMPLETADO

### VIP con timer (server-side C#)

**Campo nuevo en `data.Account`:** `VipExpirationDate DateTime?` (UTC).
Es `null` cuando la cuenta nunca tuvo VIP, cuando el VIP fue asignado sin timer
(Admin Panel → State: Vip), o cuando ya expiró y se revirtió.

**Asignación del VIP con timer:**
- Comando GM `/setvip <personaje> [días]` — default 30 días.
- Si el jugador está online: se actualiza `Account.State`, `VipExpirationDate` e
  `IsVaultExtended` en memoria + `SaveProgressAsync` del contexto del propio jugador.
  No se desconecta — los chequeos VIP del código son dinámicos contra
  `player.Account?.State` así que los perks (exp, drop, zen multipliers) aplican al toque.
- Si está offline: contexto fresco, mismo `account.State = Vip` + `VipExpirationDate`.

**Consulta del jugador:**
- Comando `/vipinfo` — muestra días/horas restantes y la fecha de vencimiento UTC.

**Expiración automática (incluso online):**
- `VipExpirationCheckPlugIn` (`IPeriodicTaskPlugIn`) corre cada 1 min, recorre los
  jugadores online y para cualquiera con `State == Vip && VipExpirationDate <= now`
  setea `State = Normal`, `VipExpirationDate = null`, mensaje azul al jugador y
  `SaveProgressAsync`. Sin desconectar.
- `LoginAction.FinishLoginAsync` chequea al login y revierte antes de aplicar
  cualquier beneficio VIP (asegura el caso "el VIP venció mientras estaba offline").

**Archivos tocados:**
- `OpenMU/src/DataModel/Entities/Account.cs` — agrega `VipExpirationDate`.
- `OpenMU/src/Persistence/EntityFramework/Migrations/20260515120000_AddAccountVipExpirationDate.cs` — migración.
- `OpenMU/src/Persistence/EntityFramework/Migrations/EntityDataContextModelSnapshot.cs` — agrega la property al snapshot.
- `OpenMU/src/GameLogic/PlugIns/ChatCommands/SetVipChatCommandPlugIn.cs` — comando GM.
- `OpenMU/src/GameLogic/PlugIns/ChatCommands/VipInfoChatCommandPlugIn.cs` — comando jugador.
- `OpenMU/src/GameLogic/PlugIns/VipExpirationCheckPlugIn.cs` — tarea periódica.
- `OpenMU/src/GameLogic/PlayerActions/LoginAction.cs` — chequeo al login.

**GUIDs de los plugins nuevos:**
- `SetVipChatCommandPlugIn`: `D4E8F1A2-3B6C-4D7E-9F0A-1B2C3D4E5F60`
- `VipInfoChatCommandPlugIn`: `C1A2B3D4-5E6F-4A7B-8C9D-0E1F2A3B4C5D`
- `VipExpirationCheckPlugIn`: `E5F6A7B8-9C0D-4E1F-A2B3-C4D5E6F7A8B9`

### Web pública v1

Proyecto nuevo standalone en `C:\MuDev\BarnaMuWeb\`. ASP.NET Core Razor Pages, .NET 10.
Conecta directo a Postgres con Npgsql + Dapper y usa `BCrypt.Net-Next 4.0.3` (misma
versión que OpenMU) para hashear passwords. **No referencia ningún assembly de OpenMU**
— el único "contrato" compartido es el esquema de `data."Account"` y BCrypt. Esto lo
hace fácil de mover a un VPS más adelante.

**Páginas:**
- `/` — home con novedades (`Content/news.json`), pill de estado y mini-stats.
- `/Register` — alta de cuenta. Hashea con BCrypt e inserta en `data.Account` con todos
  los NOT NULL completos. Rate-limit 5/h por IP.
- `/Status` — TCP probes al Connect (44405) + cada Game (55901–55906) + contadores DB.
- `/Rankings` — top 50 por experiencia, master experience, PK.
- `/Download` — link al cliente (configurable en `appsettings.json`).
- `/ReportBug` — form que escribe a `web."BugReport"` (schema propio que el sitio
  crea on-startup con `CREATE SCHEMA IF NOT EXISTS web` + `CREATE TABLE IF NOT EXISTS`).
  Rate-limit 10/h por IP.

**Configuración:** todo lo editable está en `appsettings.json` bajo `BarnaMu`:
ConnectionString, ServerName, DiscordInviteUrl, ClientDownloadUrl, ClientChecksum,
ConnectHost/Port, GameServerHost/Ports, Rates (la tabla que se muestra en home).

**Tech stack final:**
- ASP.NET Core Razor Pages (`Microsoft.NET.Sdk.Web`, net10.0)
- Npgsql 9.0.3 (raw client)
- Dapper 2.1.66
- BCrypt.Net-Next 4.0.3
- Rate limiting built-in de ASP.NET Core (sin paquetes externos)

## ⏳ PENDIENTE — PRÓXIMOS PASOS INMEDIATOS

1. **Recompilar OpenMU** (`Recompilar.bat`) y reiniciar el server. Al arrancar va a
   detectar la migración pendiente y aplicarla (auto si `AutoUpdateSchema = true` en
   `config.SystemConfiguration`, si no pregunta `Apply update? (y/n)` por consola).
2. **Habilitar los 3 plugins nuevos** en Admin Panel → PlugIns:
   - "Set VIP Chat Command"
   - "VIP Info Chat Command"
   - "BarnaMu VIP Expiration Check"
   (Verificar — pueden venir activos por default igual que `PeriodicSaveProgressPlugIn`.)
3. **Probar in-game:**
   - GM: `/setvip <tu_pj> 1` (1 día) → confirmar que sos VIP.
   - Jugador: `/vipinfo` → debería decir ~0 día(s) y ~23 hora(s).
   - Para forzar expiración: en DB
     `UPDATE data."Account" SET "VipExpirationDate" = now() - interval '1 minute' WHERE "LoginName" = '<tu_cuenta>';`
     Esperar hasta 1 min. Deberías recibir mensaje azul "Tu VIP expiró".
4. **Web — primera puesta en marcha:**
   - `cd C:\MuDev\BarnaMuWeb && dotnet run` para verificar local en `http://localhost:5050`.
   - Probar `/Register` → loguear con esa cuenta en el juego (debería andar).
   - Cuando esté listo para abrir: ver `BarnaMuWeb/README.md` → Opción A (publish + Task
     Scheduler + forward TCP 8081) o Opción B (pasar a 80/443 cuando muevas el Admin Panel).
5. **Editar `appsettings.json`** del sitio:
   - `DiscordInviteUrl`
   - `ClientDownloadUrl` + `ClientChecksum` (SHA256 del ZIP del cliente final)
   - Considerar crear un user Postgres `barnamu_web` con permisos limitados (no más
     `postgres` superuser para la web) antes de exponer al público.

## 📋 PENDIENTE — FOLLOW-UPS DE ESTA SESIÓN

### VIP timer
- Si querés que el VIP asignado desde Admin Panel también tenga 30 días de default
  automáticamente, hace falta hookear `AccountService.cs` del Web/Shared del propio
  OpenMU. Por ahora, marcar Vip desde el Admin Panel = VIP sin timer (`VipExpirationDate
  = null`); usar `/setvip` para con timer.
- Endpoint web para que el jugador vea sus días de VIP desde el panel de cuenta (cuando
  agreguemos login web). Hoy se ve con `/vipinfo` in-game.

### Web v2
- **Panel de cuenta logueado** (ver tus pjs, resets, días de VIP, cambiar password).
  Requiere sesiones / cookies / login web.
- **Ranking por resets** (vive como `StatAttribute`, requiere join a `data.StatAttribute`
  filtrando por el AttributeDefinition Guid de "Resets").
- **Ranking con nombre de clase** (join a `config.CharacterClass`).
- **HTTPS** — Cloudflare proxy o win-acme con DNS plugin para noip.com.
- **Captcha** en /Register si empieza spam.
- **Rotar password de Postgres** y user `barnamu_web` con grants mínimos.
- **Página de eventos/agenda** — mostrar próximas invasiones (Golden cada 4h, Red Dragon
  6h, T9 8h) con countdowns en JS.

## 🔑 GOTCHAS / DATOS ÚTILES

- **Reusar BCrypt.Net-Next 4.0.3** es no-negociable: si la web hashea con otra librería
  o versión incompatible, las cuentas no van a poder loguear en el juego.
- **`data."Account"`** tiene varias columnas NOT NULL sin default (`EMail`,
  `SecurityCode`, `PasswordHash`, `VaultPassword`). El INSERT de la web las setea todas
  explícitamente — si OpenMU agrega columnas NOT NULL en el futuro, hay que actualizar
  `BarnaMuDb.CreateAccountAsync`.
- **Migraciones de Martin** (las tres últimas — `AddGlobalMasterExperienceRate`,
  `AddExcellentItemDropLevelDelta`, `UpdateDaybreakWeaponDimensions`) NO tienen
  `.Designer.cs` y traen `[DbContext]` + `[Migration]` inline. La nueva
  `20260515120000_AddAccountVipExpirationDate` sigue el mismo patrón.
- **Snapshot actualizado:** agregué la property `VipExpirationDate` al
  `EntityDataContextModelSnapshot.cs` para que un futuro `Add-Migration` no detecte
  un diff espurio.
- **El sitio web crea su propio schema `web`** la primera vez que arranca. Es
  `CREATE SCHEMA IF NOT EXISTS web; CREATE TABLE IF NOT EXISTS web."BugReport" (...)`.
  Si el user de la connection string no tiene permisos de CREATE SCHEMA, falla en
  silencio (con error logueado) y los bug reports no se guardan — el resto del sitio
  sigue funcionando.
- **Puerto 8081 sugerido para la web mientras el Admin Panel ocupe 80.** Cuando muevas
  el Admin Panel a un puerto LAN-only, podés pasar la web a 80/443.
- **Rate limits actuales:** 5 registros/h por IP, 10 bug reports/h por IP. Ajustar en
  `Program.cs` si pinta.
