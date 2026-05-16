// <copyright file="T9BossInvasionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This plugin enables the T9 Boss Invasion feature (Selupan, Erohim, Dark Elf).
/// On a schedule, it spawns the three T9 bosses on their respective maps and
/// shows a golden announcement on screen. The bosses despawn when the event
/// duration ends (if not killed before).
/// </summary>
[PlugIn]
[Display(Name = "T9 Boss Invasion", Description = "Spawns the T9 bosses (Selupan, Erohim, Dark Elf) on a schedule with a screen announcement.")]
[Guid("B7E3A1C4-9F22-4D6E-A8B1-3C5D7E9F0A12")]
public class T9BossInvasionPlugIn : BaseInvasionPlugIn<PeriodicInvasionConfiguration>, ISupportDefaultCustomConfiguration
{
    // Monster numbers (verificados en la DB de BarnaMu).
    private const ushort SelupanId = 459;
    private const ushort ErohimId = 295;
    private const ushort DarkElfId = 412;

    // Map numbers (de VersionSeasonSix\Maps\*.cs).
    private const ushort RaklionBossMapId = 58;
    private const ushort LandOfTrialsMapId = 31;
    private const ushort BalgassRefugeMapId = 42;

    /// <summary>
    /// Initializes a new instance of the <see cref="T9BossInvasionPlugIn"/> class.
    /// </summary>
    public T9BossInvasionPlugIn()
        : base(
            null, // sin MapEventType (no usa el indicador de minimapa)
            [
                new(SelupanId, 1, MapId: RaklionBossMapId),
                new(ErohimId, 1, MapId: LandOfTrialsMapId),
                new(DarkElfId, 1, MapId: BalgassRefugeMapId),
            ],
            null)
    {
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PeriodicInvasionConfiguration
    {
        TaskDuration = TimeSpan.FromMinutes(30),
        PreStartMessageDelay = TimeSpan.FromSeconds(3),
        Message = "T9 Boss Invasion! Selupan, Erohim & Dark Elf have appeared!",
        Timetable = PeriodicTaskConfiguration.GenerateTimeSequence(TimeSpan.FromHours(8)).ToList(), // cada 8 horas
    };
}
