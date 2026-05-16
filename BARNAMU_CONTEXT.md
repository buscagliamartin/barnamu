# BarnaMu — Contexto del proyecto

> **Archivo de handoff entre sesiones de Claude.** Al iniciar una conversación nueva, leer este archivo primero para recuperar todo el contexto. Mantener actualizado tras cada sesión con cambios relevantes.
>
> **Última actualización:** 2026-05-16 (sesión: refinamiento web + /pkclear con costo + fix chat VIP + tier items A-F drops)

---

## 1. Quién soy yo (Martin) y qué estamos haciendo

Dueño de **BarnaMu**, un servidor privado de MU Online basado en el proyecto open source **OpenMU** (C# / .NET 10). Estoy desarrollándolo desde cero junto con Claude. El objetivo es tener un servidor listo para abrir al público con rates custom, sistema VIP, resets, drops balanceados y mapas correctamente poblados.

---

## 2. Stack técnico e infraestructura

- **OpenMU** compilado desde source en `C:\MuDev\OpenMU\src\`
- **PostgreSQL 16** — DB `openmu`, usuario `postgres`, pass `W3l.c0m3`
- **Windows 10 en VMware**
- **IP pública:** `83.43.3.212` / `barnamu.ddns.net`
- **Cliente:** `C:\MuDev\BarnaMu-Client\` (también existe `C:\MuDev\Client\` que parece ser una copia/respaldo)
- **Puertos forwarded:** 44405 (Connect), 55901–55906 (Game), 80 (Admin Panel)
- **IP fija de la VM:** 192.168.1.52
- **Admin Panel:** puerto 80, accesible solo desde LAN
- **PostgreSQL:** bloqueado desde internet
- **Backup diario** a las 4am (Task Scheduler `BarnaMu-Backup`)
- **Auto-inicio con Windows** (Task Scheduler `BarnaMu-Server`)
- **No-IP DUC** instalado (renovar `barnamu.ddns.net` cada 30 días en noip.com)

### Scripts en `C:\MuDev\`

| Script | Función |
|---|---|
| `AutoRestart.bat` | Loop `dotnet run --configuration Release`, reinicia en 10s si cae |
| `StartServer.bat` | Llama a `AutoRestart.bat` |
| `Recompilar.bat` | Recompila el código C# (doble click) |
| `Backup.bat` | Backup de la DB |

### Reglas de oro

- **Cambios de código C# → recompilar** con `Recompilar.bat`
- **Cambios solo de DB → no hace falta recompilar**, basta reiniciar el servidor
- **Cliente:** ejecutar como Administrador

---

## 3. Cómo funciona OpenMU (contexto importante)

OpenMU es un reimplemento completo de MU Online Season 6.

- **Admin Panel web** en puerto 80 (LAN only)
- **Connect Server** en puerto 44405
- **Game Servers** en puertos 55901–55906
- **Configuración del juego** (monstruos, mapas, items, skills) → schema `config` en PostgreSQL
- **Datos de jugadores** → schema `data`
- **Código del servidor:** C# puro

### Sistema de drops (importante)

- Funciona con `DropItemGroup` en la DB
- Cada monstruo tiene `NumberOfMaximumItemDrops` slots
- Grupos con `Chance >= 1.0` son **garantizados** (se procesan primero, en orden de ID)
- Grupos con `Chance < 1.0` van a una lotería para los slots restantes
- Los mapas tienen grupos genéricos (zen, items random, joyas, excelentes) en `config.GameMapDefinitionDropItemGroup`

---

## 4. Rates del servidor

|                    | Normal | VIP   |
|--------------------|--------|-------|
| Exp                | 30x    | 35x   |
| Master Exp         | 15x    | 20x   |
| Zen solo           | 10x    | 13x   |
| Drop normal        | 5x     | 7x    |
| Drop excelente     | ~3x    | ~4x   |

---

## 5. Sistema VIP (implementado en código C#)

- Tag **[VIP]** en chat (NO en character list — el cliente trunca el nombre y rompe el login)
- Tag **[GM]** en character list y chat
- Almacén extendido automático al login para VIP/GM
- `/offlevel` exclusivo para VIP
- +10% Chaos Machine para VIP/GM
- Acceso anticipado a mapas:
  - **Kanturu Relics:** nivel 200 VIP vs 260 normal
  - **Swamp of Calmness:** 250 VIP vs 280
  - **Raklion (La Cleon):** 300 VIP vs 350
- Zen en party: distribuido automáticamente entre miembros con bonus VIP individual
- **VIP con timer (sesión 2026-05-15):** el VIP tiene una `VipExpirationDate` (DateTime?,
  UTC) en `data.Account`. Al expirar, el estado vuelve a Normal automáticamente:
  - Online: `VipExpirationCheckPlugIn` chequea cada 1 min y demote in-place + aviso.
  - Offline: `LoginAction.FinishLoginAsync` chequea al login y demote antes de aplicar beneficios.
  - Asignación: `/setvip <pj> [días]` (GM), default 30 días. Aplica VIP en memoria si el
    jugador está online (sin desconectar) y persiste a la DB.
  - Consulta del jugador: `/vipinfo` muestra días/hs restantes.

### Patrón de check VIP en el código

```csharp
player.Account?.State == AccountState.Vip
|| player.Account?.State == AccountState.GameMaster
|| player.Account?.State == AccountState.GameMasterInvisible
```

> Para chequear VIP *con timer activo*: además del check de estado, validar
> `player.Account?.VipExpirationDate is null || player.Account.VipExpirationDate > DateTime.UtcNow`.
> En la práctica esto no hace falta para el código del juego porque el plugin periódico
> revierte el State a Normal en cuanto vence, pero la web sí lo calcula así (defensive).

---

## 6. Sistema de resets (implementado)

- Nivel requerido: **400**
- Nivel tras reset: **1**
- Costo zen: **10.000.000 × número de reset**
- Puntos por reset: **500 × número de reset**
- Stats se resetean, logout automático
- Sin límite de resets
- Comandos: `/reset`, `/resetinfo`, `/getresets`

---

## 7. Party system

- **Zen:** distribuido automáticamente entre miembros del mismo mapa (no cae al piso en party). Cada miembro recibe su porción con su propio `MoneyAmountRate` + bonus VIP individual.
- **Exp:** distribuida con bonus VIP individual por jugador.

---

## 8. Path de leveleo (niveles de entrada en DB)

| Mapa | Normal | VIP |
|---|---|---|
| Lorencia | 10 | — |
| Devias | 20–40 | — |
| Dungeon | 50 | — |
| Lost Tower | 100 | — |
| Tarkan | 140 | — |
| Arena | 200 | — |
| Aida | 220 | — |
| Kanturu Ruins | 240 | 200 |
| Kanturu Relics | 260 | 200 |
| Swamp of Calmness | 280 | 250 |
| Raklion (La Cleon) | 350 | 300 |

---

## 9. Archivos C# modificados y recompilados

| Archivo | Cambio |
|---|---|
| `Character.cs` | `CharacterStatus.Vip` agregado |
| `Account.cs` | `AccountState.Vip` agregado |
| `PKClearChatCommandPlugIn.cs` | Acceso normal (todos) |
| `OfflineLevelingChatCommandPlugIn.cs` | Solo VIP/GM |
| `ShowCharacterListPlugIn.cs` (y 075/095/Extended) | Tag [GM] en nombre |
| `LoginAction.cs` | Almacén extendido automático VIP/GM |
| `ChatMessageNormalProcessor.cs` | Tag [VIP]/[GM] en chat |
| `SimpleItemCraftingHandler.cs` | +10% Chaos Machine VIP/GM |
| `ClientListener.cs` | Límite 2 conexiones por IP |
| `WarpAction.cs` + `WarpGateAction.cs` | Niveles diferenciados VIP |
| `Player.cs` | Exp VIP 35x/30x, master exp VIP 20x/15x, propiedad `PartyAutoMode` |
| `Party.cs` | Exp VIP party, zen VIP party +30%, zen distribuido sin caer al piso |
| `DefaultDropGenerator.cs` | Drop VIP +40% |
| `AttackableNpcBase.cs` | Zen VIP solo +30%, zen en party distribuido entre miembros, normalización de exp para cálculo de zen |
| `PartyAutoMode.cs` | **NUEVO**: enum `Normal` / `AutoAccept` / `AutoDecline` |
| `PartyRequestAction.cs` | Auto-respuesta a invitaciones de party |
| `PartyAutoCommandPlugIn.cs` | **NUEVO**: comando `/re` |
| `DataModel/Entities/Account.cs` | **NUEVO campo** `VipExpirationDate DateTime?` (BarnaMu VIP timer) |
| `LoginAction.cs` | Chequea VIP expirado al login y vuelve a Normal antes de aplicar perks |
| `SetVipChatCommandPlugIn.cs` | **NUEVO**: comando GM `/setvip <pj> [días]` (default 30) |
| `VipInfoChatCommandPlugIn.cs` | **NUEVO**: comando jugador `/vipinfo` (días restantes) |
| `VipExpirationCheckPlugIn.cs` | **NUEVO**: tarea periódica (1 min) que demote VIPs expirados online |
| Migración `20260515120000_AddAccountVipExpirationDate.cs` | Agrega la columna `VipExpirationDate` a `data.Account` |
| `ChatMessageNormalProcessor.cs` | **FIX BarnaMu (2026-05-16):** [VIP]/[GM] se mete adentro del MENSAJE, no del sender name. Antes el cliente no podía hacer match entre el sender name "[VIP]CharName" y ningún personaje en pantalla → no aparecía chat bubble (solo barra de chat). Ahora bubble + chat box muestran "[VIP] hola" correctamente. |
| `PKClearChatCommandPlugIn.cs` | **REWORK BarnaMu (2026-05-16):** sólo limpia el propio jugador (sin target), cobra **100.000 zen × PlayerKillCount**. Si no tenés zen suficiente o no tenés PKs, mensaje azul informativo. |

---

## 10. Comandos

### Jugadores

```
/addstr, /addagi, /addvit, /addene, /addcmd
/reset, /resetinfo, /getresets
/post, /pkclear, /ware, /clearinv
/move, /help, /changelanguage
/offlevel          ← solo VIP
/re auto           ← auto-aceptar party requests
/re off            ← auto-rechazar party requests
/re                ← volver a modo normal (popup)
/vipinfo           ← días/hs restantes de tu VIP
```

### GM

```
/item group:X number:Y lvl:15 ex:63 sk:1 lu:1 opt:4
/set str/agi/vit/ene/cmd [valor] [nombre_pj]
/setlevel, /setmoney, /getmoney, /setresets, /getstat
/ban, /banacc, /unban, /unacc
/chatban, /dc, /hide, /notice
/online, /trace, /track
/move nombre mapa
/startbc, /startds, /startcc
/setmasterlevel, /getlevel
/setvip <pj> [días]   ← BarnaMu: asigna VIP con timer (default 30 días)
```

### Admin Panel

- Asignar VIP: Accounts → State: `Vip` (sin timer; usar `/setvip` para con timer)
- Asignar GM: Accounts → State: `GameMaster`

---

## 11. Última sesión (cambios en DB — sin recompilar)

### Balance de drops — Opción B (PENDIENTE TEST)

**Problema:** con la sesión anterior caían demasiados items por kill (~3 cosas: zen + item garantizado + joya/exe ocasional). 10 dragones en Lorencia tiraban ~30 drops, sentido como excesivo para servidor slow.

**Análisis del código (`DefaultDropGenerator.cs`):**
- `NumberOfMaximumItemDrops` define cuántos slots tiene cada mob.
- Grupos con `Chance >= 1.0` se procesan primero como garantizados (uno por slot).
- Slots restantes van a lotería entre grupos con `Chance < 1.0`.
- Por slot de lotería: `P(grupo) = group.Chance` si `totalChance < 1.0`. Si `totalChance > 1.0`, se normaliza.
- VIP `dropMultiplier = 1.40` solo aplica cuando `totalChance > 1.0` (no-op para nuestros valores actuales).

**Cambios aplicados (transacción committeada):**

| Cambio | Antes | Después |
|---|---|---|
| `MonsterDefinition.NumberOfMaximumItemDrops` (330 mobs) | 3 | **2** |
| `RandomItem` Chance (`00000200-0002-...`) | 1.0 (garantizado) | **0.5** (lotería) |
| `Jewel` Chance (`00000200-0004-...`) | 0.125 | **0.03** |
| `Excellent` Chance (`00000200-0003-...`) | 0.0027 | sin cambios |
| `Money` Chance (`00000200-0001-...`) | 1.0 | sin cambios (sigue garantizado) |

**Resultado esperado por kill:**
- Slot 1: zen (siempre, 100%)
- Slot 2: lotería entre items (50%) + joyas (3%) + excelente (0.27%) + nada (46.73%)

**Estimación a 30 kills/min:** ~15 items/min, joya cada ~1 min, excelente cada ~12 min.

### Spots en Arena (sesión anterior — pendiente test)

Arena estaba casi vacía (10 Bloody Golems + NPCs). Se agregaron 9 spots de punto fijo (X1=X2, Y1=Y2):

| Spot | Monstruo | Coords | Cantidad |
|---|---|---|---|
| NW | Iron Wheel | (50, 50) | 12 |
| N | Tantallos | (120, 45) | 12 |
| NE | Zaikan | (190, 50) | 10 |
| W | Bloody Wolf | (48, 120) | 12 |
| Centro | Beam Knight | (120, 120) | 15 |
| E | Mutant | (192, 120) | 12 |
| SW | Death Beam Knight | (50, 190) | 10 |
| S | Alquamos | (120, 192) | 10 |
| SE | Queen Rainer | (192, 192) | 10 |

### Iteración final del balance de drops (estable)

Tras varias rondas de calibración, este es el estado final del sistema de drops. **Mobs comunes funcionan correctamente, eventos balanceados, items normales con cantidad apropiada de bonus.**

#### Valores actuales en DB

**Grupos globales (4 default + Symbol/Spirits + event tickets):**

| Grupo | ID | Chance | Notas |
|---|---|---|---|
| Money | `00000200-0001-...` | 1.0 | Garantizado, slot 1 = zen siempre |
| Random Item | `00000200-0002-...` | 0.5 | Lotería slot 2 |
| Excellent | `00000200-0003-...` | 0.0027 | Lotería slot 2 |
| Jewels | `00000200-0004-...` | 0.03 | Lotería slot 2 |
| Symbol of Kundun L1-7 | varios | 0.003 | Vanilla, raro |
| Dark Horse Spirit | varios | 0.001 | Vanilla, raro |
| Dark Raven Spirit | varios | 0.001 | Vanilla, raro |
| Event tickets (Devil's Key/Eye, Blood Bone, Scroll Archangel, Old Scroll, ISC) | 42 grupos | 0.001 | Bajado de 0.01 → ~11 keys/hora |

**`NumberOfMaximumItemDrops` por mob:**
- 330 mobs combatientes en Max=2
- 135 NPCs/destructibles/traps en Max=0 (correcto)

**ItemOptionDefinition AddChance (items azules):**

| Option | AddChance | Resultado |
|---|---|---|
| Luck | 0.10 | 10% items con +5% crit damage |
| Base Defense / Damage / Wizardry / Curse / Defense Rate / Damage Bonus | 0.15 c/u | ~15% items con +X options |
| Wings/Capes/Pets/Sockets/Excellent | sin cambios | Endgame, balance aparte |

#### Resultado por kill (mob común)

| | Normal | VIP |
|---|---|---|
| Zen (slot 1) | 100% | 100% (+30% cantidad) |
| Item random (slot 2 lottery) | 50% | 70% |
| Joya | 3% | 4.2% |
| Excelente | 0.27% | 0.378% |
| Item con Luck | 10% de los items | 10% |
| Item con Option +X | 15% de los items | 15% |
| Nada extra (solo zen) | ~46% | ~21% |

#### Fix C# para VIP drop multiplier (recompilado y funcionando)

`DefaultDropGenerator.cs` — método `SelectRandomGroup` modificado para que el `dropMultiplier` (VIP +40%) se aplique a cada `group.Chance` individualmente, no solo al `totalChance`. Antes solo funcionaba cuando `totalChance > 1.0`; ahora siempre aplica.

```csharp
private DropItemGroup? SelectRandomGroup(IEnumerable<DropItemGroup> groups, double totalChance, double dropMultiplier = 1.0)
{
    var effectiveTotalChance = totalChance * dropMultiplier;
    var remainingThreshold = this._randomizer.NextDouble();
    if (effectiveTotalChance > 1.0)
    {
        remainingThreshold *= effectiveTotalChance;
    }

    foreach (var group in groups)
    {
        var effectiveChance = group.Chance * dropMultiplier;
        if (remainingThreshold > effectiveChance)
        {
            remainingThreshold -= effectiveChance;
        }
        else
        {
            return group;
        }
    }

    return null;
}
```

#### Bug crítico encontrado y resuelto: Bloody Golem no dropeaba items

**Síntoma:** Bloody Golem (lvl 117) solo dropeaba zen, ningún item normal. Excelentes sí caían rara vez.

**Causa:** Bloody Golem tenía un grupo asignado via la **tabla join `MonsterDefinitionDropItemGroup`** (relación many-to-many de mob a grupo). El grupo tenía `Chance=1` (garantizado) pero `PossibleItems` vacío. Esto consumía el slot 2 silenciosamente, devolviendo null → no item.

**Fix:** desde Admin Panel → Monsters → Bloody Golem → borrar todos los drop groups asignados.

**Importante para futuras investigaciones:** hay DOS tipos de relación monstruo↔grupo:
1. `DropItemGroup.MonsterId` (one-to-many, filtro): el grupo solo aplica a este monstruo.
2. `MonsterDefinitionDropItemGroup` (many-to-many): tabla join, el monstruo "tiene" estos grupos extra.

Las queries de diagnóstico tienen que mirar AMBAS. El admin panel muestra la #2.

**Otros mobs con grupos en la tabla join (todos legítimos, NO tocar):**
- Golden Budge Dragon → Box of Luck
- Golden Goblin → Box of Kundun +1
- Golden Titan → Box of Kundun +2
- Golden Dragon → Box of Kundun +3
- Golden Lizard King → Box of Kundun +4
- Golden Tantallos → Box of Kundun +5
- Red Dragon → Items específicos
- Statue of Saint → Archangel Weapon (Blood Castle)

### Notas técnicas para próximas sesiones

- Los 4 grupos globales de drop se registran en `GameConfigurationInitializerBase.AddItemDropGroups()` y se asocian a cada mapa via `BaseMapInitializer.RegisterDefaultDropItemGroup()`.
- Event tickets se registran en `EventTicketItems.cs` con `Chance = 0.01` vanilla y filtros `MinimumMonsterLevel`/`MaximumMonsterLevel`.
- Symbol of Kundun se registra en `Misc.cs:80` con `Chance = 0.003` vanilla.
- Si se quiere ajustar el bonus VIP de drop: cambiar el `1.40` en `DefaultDropGenerator.cs` línea 95.
- Para investigar mobs que no dropean items: siempre chequear ambas tablas (`DropItemGroup.MonsterId` y `MonsterDefinitionDropItemGroup`).
- Si alguna vez se rompen los chances en DB (corrupción / restore parcial), correr los SQLs de esta sección para volver al estado conocido.

### Bug pendiente sin resolver

**Arena — no se puede entrar a las jaulas.** Reportado esta sesión. Probable causa: terreno hardcoded en `Terrain6.att` del cliente/server. Posible relación con `BattleZoneDefinition` tipo Soccer en Arena. Pendiente para próxima sesión.

---

## 12. Pendiente

PENDIENTE:
1. ~~Implementar **VIP con timer** (expira en N dias automaticamente).~~ ✅ Sesión 2026-05-15
2. configuracion de items de dropeo y definir en que mapa (mob lvl) caen y deben superponerse de mayor a menor, si el item A cae en mobs de lorencia noria y elvenland, luego en devias deberia caer item B + item A (no juntos, siempre posibilidades):
a: Star of sacred birth - random de item+4 con posibilidad de +luck y/o +option
b: firecracker - random de item+6 con posibilidad de +luck y/o +option
c: heart of love - random de item+7 con posibilidad de +luck y/o +option
d: olive of love - jewel
e: silver medal - jewel
f: gold medal - jewel
. cuando un usuario comun habla en el juego, aparece su comentario sobre el pj (normal), cuando un vip habla no aparece su comentario, solo aparece en la barra de chat izquierda.
. commando /pkclear requiere de usuario "/pkclear usuario", quisiera que solo sea "/pkclear" y cobre zen
. equipar y desequipar items con click derecho.
. Balance de clases (BK, DW, SM, Elf, MG, DL, Summoner, RF)
. ~~Página web del servidor~~ ✅ Sesión 2026-05-15 (v1: registro, status, rankings, download, news, bugs en `C:\MuDev\BarnaMuWeb`)
. Discord del servidor
. Cliente final para distribución pública
. ~~Sistema de ranking~~ ✅ Parcial: top 50 por exp/master/PK en la web. Pendiente ranking por resets (StatAttribute) y por clase.
. Castle Siege configuración
. Vulcanus - entender sistema Gens antes de activar
. Mapa exclusivo VIP - evaluar opciones

### Inmediatos post-sesión 2026-05-16

1. **Recompilar OpenMU** (`Recompilar.bat`) — pickup de los cambios en
   `PKClearChatCommandPlugIn.cs` y `ChatMessageNormalProcessor.cs`.
2. **Reiniciar el servidor** y probar in-game:
   - Chat: que un VIP escriba algo en el juego — debería aparecer el bubble sobre el
     personaje con el prefijo `[VIP]`.
   - PK clear: matá unos jugadores en algún test, después `/pkclear` solo (sin nombre).
     Debería cobrarte 100.000 × cant. de PKs y limpiar.
3. **Correr el SQL de tier items** una sola vez:
   ```cmd
   psql -U postgres -d openmu -f C:\MuDev\BARNAMU_TIER_ITEMS_DROPS.sql
   ```
   La password es `W3l.c0m3`. El script es idempotente (re-correrlo no duplica).
   Al final de la salida deberías ver 6 filas (una por tier) con el campo
   `maps_attached` en ~30+ mapas.
4. **Probar tier items in-game:** matar mobs lvl 1+ y eventualmente debería caer
   un Star of Sacred Birth (~0.5% por kill). Mobs lvl 30+: pueden caer Star o
   Firecracker. Etc. Mobs lvl 150+: cualquiera de los 6.
5. **Web — refrescar:** stop el `dotnet run` y arrancar de nuevo para tomar el
   nuevo `guide.json` (más contenido: items tier, mapas, quests) y los fixes
   menores (columnas removidas de rankings/status, bullets removidos de download).

### Pendiente — siguiente sesión

1. **Items tier A-F — comportamiento al consumir:**
   El drop está implementado, pero al hacer doble-click en los items A-F los
   premios siguen siendo los vanilla de OpenMU, no los que vos definiste:
   - A (Star) actualmente da items endgame (Dark Breaker, Great Dragon Set, etc.) —
     **MUY POTENTE** para un tier A. Querés que dé item random no-exe **+5**.
   - B (Firecracker) da items +7-9 — cerca, pero querés **+5**.
   - C (Heart) da items +7-9 — coincide bastante con tu spec **+7**.
   - D (Olive) **NO ESTÁ CONFIGURADO** en vanilla → no hace nada al consumirse.
   - E (Silver) da mix de items+jewels+armor — querés solo 1 jewel random.
   - F (Gold) da mix de items+jewels+armor — querés solo 1 jewel random.

   Trabajo pendiente: SQL que reconfigure los `ItemDropItemGroup` asociados al
   ItemDefinition del Box of Luck (Group 14, Number 11) — UPDATE de chance /
   MinimumLevel / MaximumLevel + DELETE/INSERT en
   `ItemDropItemGroupItemDefinition` (PossibleItems) para que cada tier dé lo
   que querés. Pool de items por tier hay que curarlo (qué armas/armaduras
   entran en "item +5" vs "+7").

2. **Estética de la web:** agregar `wwwroot/images/hero.jpg`, screenshots, logo.
   El framework CSS ya está listo para recibir assets.

3. **Levels exactos de Crywolf / Land of Trials / Balgass Refuge / Balgass
   Barracks** en `appsettings.json` → `Maps[]` y en el guide.json (sección
   "mapas"). Las puse como REVISAR.

---

## 13. Gotchas críticos descubiertos en sesiones

- **`SpecialItemType` enum** (`DataModel/Configuration/DropItemGroup.cs`):
  ```
  0 = None        3 = RandomItem
  1 = Ancient     4 = SocketItem
  2 = Excellent   5 = Money    6 = Jewel
  ```
  Sesión 2026-05-16: por error usé `ItemType = 1` pensando que era RandomItem y
  empezaron a caer ancients en Devias. **Para drops de items específicos
  (Box of Luck, event tickets, summoning orbs, etc.) usar `ItemType = 3`.**
  Fix aplicado en `BARNAMU_TIER_ITEMS_FIX.sql`.

## 14. Notas misceláneas

- **La Cleon = Raklion** (nombre cambiado en DB)
- **MU Helper** tiene restricción de nivel 80 en el cliente (`muonline.exe`), no es configurable server-side sin editar el ejecutable
- **BMD del menú M:** `C:\MuDev\BarnaMu-Client\Data\Local\movereq_eng.bmd` (editar con MagicHand)
- **Cliente requiere ejecutarse como Administrador**

---

## 15. Convenciones para mantener este archivo

- Al final de cada sesión, agregar bajo "Última sesión" un resumen de los cambios.
- Si un cambio se confirma estable, mover de "Última sesión" a la sección que corresponda (Sistema VIP, Rates, Archivos C# modificados, etc).
- Marcar tareas completadas en la lista de Pendiente con ~~tachado~~ + ✅, no borrar (queda como historial).
- Actualizar la fecha de "Última actualización" al inicio del archivo.
- Si se descubren detalles nuevos sobre cómo funciona OpenMU internamente, agregarlos a la sección 3 ("Cómo funciona OpenMU").
