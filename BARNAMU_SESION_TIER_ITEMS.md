# BarnaMu — Sesión: refinamiento web + /pkclear + chat VIP + tier items

> Checkpoint de sesión. Si se corta, retomar desde acá.
> Última actualización: 2026-05-16

## ✅ COMPLETADO

### Web fixes rápidos

- **Rankings:** removida la columna "Creado" de los 3 modos (resets, level, guilds).
- **Status:** removidas las columnas "Host" y "Puerto" — sólo se ve servicio + estado.
- **Download:** removidos los 2 bullets que mencionaban `muonline.exe` + IP (Martin
  modificó `main.exe` para conectar directo).
- **Guía de comandos:** removido `/changelanguage` (para evitar problemas de cliente).

### Server: chat bubble VIP — FIX

**Bug:** cuando un VIP escribía, el chat box NO aparecía sobre el personaje (sí en
la barra de chat izquierda).

**Causa:** `ChatMessageNormalProcessor.cs` armaba el sender name como
`"[VIP]CharName"`. El cliente de MU usa el sender name para buscar el personaje en
pantalla y pegarle el chat bubble. Con el prefijo, ningún personaje matcheaba → no
había bubble.

**Fix:** dejar el sender name como el nombre real del personaje y meter el tag
`[VIP]` / `[GM]` adentro del MENSAJE. Ahora el bubble aparece sobre el char y el
tag se ve en bubble y barra de chat.

Archivo: `OpenMU/src/GameLogic/PlayerActions/Chat/ChatMessageNormalProcessor.cs`.

### Server: /pkclear — REWORK

**Antes:** `/pkclear <character>` requería un target, no cobraba zen, podía limpiar PKs ajenos.

**Ahora:**
- Sólo limpia el propio jugador. Sin argumentos.
- Cobra **100.000 zen × PlayerKillCount**.
- Si no tenés zen suficiente: blue message con el costo exacto.
- Si no tenés PKs: blue message "no tenés PKs para limpiar".
- Cap defensivo de overflow (>21k PKs).

Archivo: `OpenMU/src/GameLogic/PlugIns/ChatCommands/PKClearChatCommandPlugIn.cs`.

### Server: items tier A-F — DROP SIDE

**Lo que hace ahora:** 6 DropItemGroups globales (attached a todos los mapas) que
dropean el Box of Luck (Group 14, Number 11) a niveles 1-6 según el nivel del mob,
con 0.5% de chance cada uno:

| Tier | Item                  | Drop level | Mob lvl mínimo |
|------|-----------------------|-----------:|---------------:|
| A    | Star of Sacred Birth  | +1         | 1+             |
| B    | Firecracker           | +2         | 30+            |
| C    | Heart of Love         | +3         | 60+            |
| D    | Olive of Love         | +4         | 90+            |
| E    | Silver Medal          | +5         | 120+           |
| F    | Gold Medal            | +6         | 150+           |

En late-game (mobs lvl 150+) los 6 tiers son eligibles → ~3% chance total por kill
de cualquier tier. Coincide con tu pedido "como las jewels".

Script: `C:\MuDev\BARNAMU_TIER_ITEMS_DROPS.sql` — **idempotente**, re-correrlo no
duplica filas. Incluye ROLLBACK al final por si querés desinstalar.

Aplicar con:
```cmd
psql -U postgres -d openmu -f C:\MuDev\BARNAMU_TIER_ITEMS_DROPS.sql
```

### Web: contenido de la guía expandido

`Content/guide.json` ahora tiene:

- **Items tier A-F:** sección nueva explicando qué tira cada uno al consumirlo, drop
  rate y niveles. Es la cara visible de los items para el jugador.
- **Goldens:** la columna "DropLevel 0-22" reemplazada por "sets que puede tirar"
  (Bronze/Pad/Leather/etc. → endgame Sunlight/Volcano/Great Dragon). Texto más
  útil para el jugador que el número.
- **Quests:** sección completada con Quest 1 (Sebina, lvl 150 — Scroll of Emperor +
  Broken Sword), Quest 2 (Marlon, lvl 220 — Tear of Elf + Soul Shard), Marlon's
  Hidden Garden, Apostle Devin (Master), Werewolf Quarrel.
- **Mapas:** sección nueva con tabla completa — mapa, nivel normal, nivel VIP, qué
  hay adentro (mobs y NPCs principales). Crywolf / Land of Trials / Balgass Refuge
  y Barracks quedan con "REVISAR" para que Martin pase los niveles correctos.

### Tasks completadas
- #15 Web fixes rápidos
- #16 Verificar max 2 cuentas por IP (confirmado, ya en 2)
- #17 /pkclear sin target + zen
- #18 Chat bubble VIP
- #19 Tier items A-F (drop side)
- #20 Guía: cajas con set names
- #21 Guía: quests
- #22 Guía: mapas

## ⏳ PENDIENTE — PRÓXIMOS PASOS INMEDIATOS

1. **Recompilar OpenMU** (`Recompilar.bat`) y reiniciar el server. Pickup de:
   - `PKClearChatCommandPlugIn.cs` (nuevo comportamiento de /pkclear)
   - `ChatMessageNormalProcessor.cs` (fix del chat bubble VIP)

2. **Correr el SQL** una vez:
   ```cmd
   psql -U postgres -d openmu -f C:\MuDev\BARNAMU_TIER_ITEMS_DROPS.sql
   ```

3. **Probar in-game:**
   - VIP en chat: que un personaje VIP escriba — chat bubble debería aparecer.
   - `/pkclear`: matá unos pjs en test, después `/pkclear` solo. Cobra zen escalado.
   - Tier items: matar mobs un rato. Eventualmente caen Star/Firecracker/etc.

4. **Refrescar la web:** stop `dotnet run` y volver a arrancarlo para que tome el
   nuevo `guide.json`.

5. **Levels de mapas:** Crywolf / Land of Trials / Balgass Refuge / Balgass Barracks
   están como "REVISAR". Pasame los niveles que querés y los pongo en `appsettings.json`
   y `guide.json`.

## 📋 PENDIENTE — SIGUIENTE SESIÓN (item tier — CONSUME SIDE)

Lo que NO se hizo en esta sesión, intencional, es la reconfiguración del COMPORTAMIENTO
al consumir A-F. Hoy, al hacer doble-click en cada item, te tira los premios vanilla
de OpenMU, NO los que vos definiste:

| Tier | Vanilla actual                                | Tu spec                                |
|------|-----------------------------------------------|----------------------------------------|
| A    | Items endgame (Dark Breaker, Great Dragon)    | Item random no-exe **+5**              |
| B    | 30% items +7-9 + 20% jewels                   | Item random no-exe **+5**              |
| C    | 30% items +7-9 + 20% jewels                   | Item random no-exe **+7**              |
| D    | **NO CONFIGURADO** — no hace nada             | 1 jewel random                         |
| E    | Items +6 + jewels + armor                     | 1 jewel random                         |
| F    | Items +7 + jewels + armor                     | 1 jewel random                         |

Trabajo para la próxima sesión:
1. Definir el "pool" de items que pueden caer en tier A/B (+5) y tier C (+7) —
   probablemente todas las armas y armaduras Season 6 que NO sean ancient ni
   excellent, filtradas por las que soporten esos niveles.
2. SQL que UPDATE / DELETE / INSERT en `config."ItemDropItemGroup"` y
   `config."ItemDropItemGroupItemDefinition"` para reconfigurar las DropItems
   asociadas al Box of Luck (Group 14, Number 11) — los ItemDropItemGroups
   identificables por Description = "Star of the Sacred Birth", "Firecracker
   (...)", etc.
3. Agregar el ItemDropItemGroup faltante para Olive of Love (level 4).
4. Para D/E/F: PossibleItems = `[Bless, Soul, Life, Chaos, Creation, Guardian]`.

## 🔑 GOTCHAS / DATOS ÚTILES

- **Box of Luck es UN solo ItemDefinition** (Group 14, Number 11). Sus niveles +1 a
  +6 son los tier A-F. El nombre que muestra el cliente depende del nivel:
  +1 = Star of the Sacred Birth, +2 = Firecracker, etc. Esto está documentado en
  `OpenMU/src/Persistence/Initialization/VersionSeasonSix/Items/BoxOfLuck.cs`
  arriba (líneas 47-62).
- **Olive of Love (+4) no tiene config en vanilla.** Si Martin doble-clickea un
  Olive antes de hacer el reconfigure del consume, no le pasa nada (consume sin
  reward). El drop sí funciona (cae al piso, lo levanta).
- **MaxConnectionsPerIp = 2** vive en `ConnectServer/ClientListener.cs:84` como
  `const`. Si en algún momento querés cambiarlo, ahí está.
- **El fix del chat VIP** sólo afecta a `ChatMessageNormalProcessor`. Si en algún
  momento agregás otros tipos de chat (party, guild, whisper) y querés el tag VIP
  también ahí, hay que aplicar el mismo patrón en sus respectivos procesadores.
- **Los DropItemGroups de tier items** tienen `Description LIKE 'BarnaMu Tier %'`,
  identificables y fáciles de borrar via ROLLBACK del SQL.
- **Configuración de drop rate / niveles de mob:** todo está en el SQL —
  `Chance = 0.005` y los `MinimumMonsterLevel = 1/30/60/90/120/150`. Para tunear,
  editar el SQL o hacer UPDATEs directos.
