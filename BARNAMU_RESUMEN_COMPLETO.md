# BarnaMu — Resumen Completo del Proyecto

> Documento de handoff para continuar en una nueva conversación.
> Fecha: 2026-05-13

---

## 1. Stack y entorno

- **Servidor**: OpenMU (C# / .NET 10) — fork local en `C:\MuDev\OpenMU`
- **Cliente**: MU Online Season 6 — `C:\MuDev\BarnaMu-Client`
- **DB**: PostgreSQL 16
- **OS**: Windows 10 sobre VMware
- **Dominio**: `barnamu.ddns.net`
- **Auto-start del server**: `C:\MuDev\AutoRestart.bat` (con `-resolveIP:barnamu.ddns.net`)
- **Repo de contexto**: `C:\MuDev\BARNAMU_CONTEXT.md`

---

## 2. Objetivo del proyecto

Lanzar un servidor MU S6 privado y publico, con economia balanceada estilo "slow rates" pero con beneficios reales para VIPs (sin ser OP). Toda la session estuvo enfocada en calibrar drops, arreglar bugs heredados de OpenMU y agregar contenido.

---

## 3. Cambios al codigo del servidor (compilados)

### 3.1 `DefaultDropGenerator.cs` — fix del multiplicador VIP
El `SelectRandomGroup` original solo aplicaba `dropMultiplier` cuando `totalChance > 1.0`. Resultado: el bonus VIP no se notaba en mapas low-tier.

Fix aplicado:
```csharp
private DropItemGroup? SelectRandomGroup(IEnumerable<DropItemGroup> groups, double totalChance, double dropMultiplier = 1.0)
{
    var effectiveTotalChance = totalChance * dropMultiplier;
    var remainingThreshold = this._randomizer.NextDouble();
    if (effectiveTotalChance > 1.0)
        remainingThreshold *= effectiveTotalChance;
    foreach (var group in groups)
    {
        var effectiveChance = group.Chance * dropMultiplier;
        if (remainingThreshold > effectiveChance) remainingThreshold -= effectiveChance;
        else return group;
    }
    return null;
}
```
Se cambio el call site para pasar `dropMultiplier` como argumento separado.

### 3.2 `AutoRestart.bat` — fix de conexion externa
Cambio: `dotnet run --configuration Release -- -resolveIP:barnamu.ddns.net`
Asi el Connect Server publica el dominio publico, no la IP de LAN. Soluciono el `SocketException 10060` que tenian los jugadores externos.

---

## 4. Bugs resueltos en este proceso

| Bug | Causa | Solucion |
|---|---|---|
| Bloody Golem solo soltaba zen | DropGroup vacio con Chance=1 en `MonsterDefinitionDropItemGroup` consumia slot | Borrado desde admin panel |
| Kundun Boxes casi siempre soltaban zen | `ItemDropItemGroup` items a 0.2, money fallback a 1.0 | Items a 1.0, money fallback a 0 |
| Devils Keys spammeaban | Chance=1 (mal subido desde 0.01) | Restaurado a 0.001 |
| Symbol of Kundun/Dark Horse/Raven Spirit a tasas altas | Mal configurado | Restaurado a 0.003 / 0.001 (vanilla) |
| `/setstats` no funcionaba | Comando real es `/set`, y `SetStatChatCommandPlugIn` es `IDisabledByDefault` | Habilitar en Admin Panel -> PlugIns |
| Skills no afectaban target | "Use target area filter" estaba en off en Admin Panel | Activado |
| Jaulas de Arena no se abrian | Archivo `.att` del cliente | El usuario lo arreglo client-side |
| Mapas nuevos no aparecian en menu M | Archivo BMD encriptado (encryption custom, no XOR estandar) | Usuario edito el BMD manualmente |

---

## 5. Estado de drops (rates "slow" balanceados)

- Lorencia / Devias / etc. (low-tier): drops moderados, sin spam de keys.
- Items azules tipo Sphinx solo donde corresponde por DropLevel.
- VIP recibe bonus real gracias al fix de `SelectRandomGroup`.
- Boxes de Kundun (+1..+5) sueltan items casi siempre (no zen).

---

## 6. Sistema de Box of Luck / Kundun decifrado

- Box of Luck = un solo `ItemDefinition` (Group 14, Number 11) con `MaximumItemLevel=15`.
- Cada level (0..15) es una "caja" visual distinta.
- **Kundun+1 a Kundun+5** = levels 8 a 12 del mismo item.
- `DropItemGroup`s asociados se filtran por `SourceItemLevel`.
- `AddMoneyDropFallback` crea un `ItemDropItemGroup` separado para zen.

---

## 7. Mapas agregados por el usuario (BMD client-side)

1. Land of Trials
2. Barracks of Balgass
3. Balgass Refuge
4. Crywolf Fortress

---

## 8. Inventario de super-bosses por mapa (server-side)

Datos extraidos de los archivos `Maps\*.cs` en `OpenMU\src\Persistence\Initialization\VersionSeasonSix\Maps\`:

| Mapa | Boss / Mob fuerte | NPC ID | Lvl | HP |
|---|---|---|---|---|
| Barracks of Balgass | Balram | 409 | 117 | 75.000 |
| Barracks of Balgass | Death Spirit | 410 | — | — |
| Barracks of Balgass | Soram | 411 | — | — |
| Balgass Refuge | **Dark Elf** (BOSS) | 412 | 128 | 1.500.000 |
| Balgass Refuge | Death Spirit | 410 | — | — |
| Balgass Refuge | Soram | 411 | — | — |
| Crywolf Fortress | Hammer Scout (regular) | 310 | — | — |
| Crywolf Fortress | *(pendiente leer resto del archivo para Balgass boss, Erohim, etc.)* | — | — | — |
| Land of Trials | Lizard Warrior (regular) | 290 | — | — |
| Land of Trials | *(pendiente leer resto del archivo para Selupan, etc.)* | — | — | — |

> NOTA: faltan terminar de leer `CrywolfFortress.cs` y `LandOfTrials.cs` para identificar todos los bosses (Erohim, Selupan, Balgass mismo, etc.) que el usuario menciono.

---

## 9. Tier list propuesta (confirmada parcialmente por usuario)

| Tier (caja/drop) | Mob asignado | Notas |
|---|---|---|
| Jewels (Star/Firecracker/Heart/Olive/Silver) | Pad/random by map | OK |
| Gold Medal | Pad (random) | OK |
| Box of Heaven | Bone, Golden Budge Dragon | OK |
| Kundun+1 | Sphinx, Golden Goblin | OK |
| Kundun+2 | Legendary, Golden Soldier + Titan + Vepar | OK |
| Kundun+3 | Grand Soul, Golden Wheel + Dragon | OK |
| Kundun+4 | **Dark Soul**, Golden Lizard King | Confirmado por test in-game |
| Kundun+5 | **Hades**, Golden Tantallos | Hades > Dark Soul (mas def + sockets) |
| Super-boss tier (Venom Mist + Ancients) | Dark Elf / Erohim / Selupan / Balgass (mapas nuevos) | **A definir** con SQL de sets |

---

## 10. SQL pendiente de correr (para completar la tier list)

```sql
SELECT
    "Number"           AS set_num,
    "Name"             AS set_name,
    "DropLevel"        AS drop_lvl,
    "MaximumItemLevel" AS max_item_lvl,
    "DropsFromMonsters"
FROM config."ItemDefinition"
WHERE "Group" = 7         -- Group 7 = Helms (cada Helm representa un set completo)
ORDER BY "DropLevel" ASC, "Number" ASC;
```

Esto deberia devolver la lista de TODOS los sets ordenados por DropLevel, para mapearlos uno a uno a los bosses/Golden mobs y completar la tier list.

Referencia codigo: `ArmorInitializerBase.cs` line 199 (signature `CreateArmor`) y line 208 (`armor.DropLevel = dropLevel;`).

Algunos sets ya identificados desde `VersionSeasonSix\Items\Armors.cs`:
- Set 22: **Dark Soul** Helm, lvl 110 (DW=2)
- Set 30: **Venom Mist** Helm, lvl 126 (DW=2)
- Set 52: **Hades** Helm, lvl 109 (DW=1)

---

## 11. Documentos generados en el proyecto

- `C:\MuDev\BARNAMU_CONTEXT.md` — contexto principal (mantenido)
- `C:\MuDev\COMANDOS_GM.txt` — lista completa de comandos GM/players con flags [DEFAULT OFF]
- `C:\MuDev\BARNAMU_RESUMEN_COMPLETO.md` — este documento
- `C:\MuDev\bmd_decrypt.ps1` / `bmd_decrypt_v2.ps1` — intentos fallidos de cracking del BMD (encryption custom, no XOR estandar)

---

## 12. Tareas pendientes para la proxima conversacion

**Alta prioridad (donde quedamos):**

1. **Tier list completa de sets**: correr el SQL de arriba, pegarlo en el nuevo chat para que arme la lista de tiers definitiva mapeada a bosses.
2. **Terminar lectura de mapas nuevos**: leer `CrywolfFortress.cs` y `LandOfTrials.cs` enteros para identificar **TODOS** los bosses (Erohim, Selupan, Balgass, etc.) — el usuario menciono que existen pero solo vimos los mobs base hasta ahora.
3. **Asignar drops de Venom Mist / Ancients** a los super-bosses una vez identificados todos.

**Media prioridad:**

4. Configurar drop de **items Ancient** (sets ancestrales) — task abierta.
5. Verificar que **todas las opciones de Excellent** esten habilitadas en Admin Panel.
6. Revisar y separar **horarios de eventos** (Blood Castle / Devil Square / Chaos Castle).
7. Implementar **VIP con timer** (expira en N dias automaticamente).

**Baja prioridad:**

8. VM Win10: desactivar suspension automatica para que el server no se duerma.
9. Re-confirmar niveles de walk-in + niveles del menu M + niveles de acceso VIP por mapa (sobretodo para los 4 mapas nuevos).
10. Task #12: Editor de BMD — el usuario ya lo resolvio client-side, pero la task sigue abierta por si en el futuro hay que tocar mas BMDs.

**Decidido NO hacer (en este momento):**

- Kundun+6 y Kundun+7 como items nuevos. Se descarto porque generaba issues de rendering visual con slots vacios en `item.bmd`. Se va a usar los 5 tiers existentes + drops directos de super-bosses.

---

## 13. Como retomar en una nueva conversacion

Pega en el nuevo chat:

> "Estoy retomando BarnaMu. Lee `C:\MuDev\BARNAMU_RESUMEN_COMPLETO.md` y `C:\MuDev\BARNAMU_CONTEXT.md` para el contexto. Lo siguiente que hay que hacer es: (1) correr el SQL del set tier list y mandarte el resultado, (2) terminar de leer `CrywolfFortress.cs` y `LandOfTrials.cs` para listar todos los super-bosses, (3) armar la tier list final con drops asignados."

Eso deberia darle al nuevo Claude todo lo necesario sin re-explicar 8 horas de session.
