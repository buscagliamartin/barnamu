// <copyright file="ItemAuditLogger.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.Globalization;
using System.IO;
using System.Text;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence;

/// <summary>
/// BarnaMu: central, file-based audit logger for item creation/transfer events.
/// It writes a forensic trail used to trace where in-game items came from and to
/// spot items that were created/injected by exploit (high-value items with no
/// matching audit entry). Writing never throws, so auditing can never disrupt gameplay.
/// Log files are written to %BARNAMU_AUDIT_DIR% (default C:\MuDev\Logs\ItemAudit),
/// one file per UTC day.
/// </summary>
public static class ItemAuditLogger
{
    /// <summary>
    /// Items below this level are only audited by high-volume sources (monster drops,
    /// pickups) when they carry excellent options, an ancient set, or sockets.
    /// </summary>
    public const byte MinNotableItemLevel = 7;

    private static readonly object Sync = new();

    private static readonly string LogDirectory =
        Environment.GetEnvironmentVariable("BARNAMU_AUDIT_DIR") is { Length: > 0 } dir
            ? dir
            : @"C:\MuDev\Logs\ItemAudit";

    /// <summary>
    /// Writes an audit line for an item attributed to a player, at an explicit map location.
    /// </summary>
    /// <param name="source">The source/event that produced or moved the item.</param>
    /// <param name="actor">The player involved, or <c>null</c> if unknown.</param>
    /// <param name="item">The audited item.</param>
    /// <param name="map">The map where the event happened.</param>
    /// <param name="position">The coordinates where the event happened.</param>
    /// <param name="extra">Optional extra context.</param>
    public static void Log(AuditSource source, Player? actor, Item item, GameMap? map, Point position, string? extra = null)
        => Log(source, DescribeActor(actor), item, DescribeLocation(map, position), extra);

    /// <summary>
    /// Writes an audit line for an item attributed to a player, using the player's current location.
    /// </summary>
    /// <param name="source">The source/event that produced or moved the item.</param>
    /// <param name="actor">The player involved.</param>
    /// <param name="item">The audited item.</param>
    /// <param name="extra">Optional extra context.</param>
    public static void Log(AuditSource source, Player actor, Item item, string? extra = null)
        => Log(source, DescribeActor(actor), item, DescribeLocation(actor.CurrentMap, actor.Position), extra);

    /// <summary>
    /// Writes a single audit line describing an item event. This method never throws.
    /// </summary>
    /// <param name="source">The source/event that produced or moved the item.</param>
    /// <param name="actor">The account/character involved, or a marker such as "&lt;world&gt;".</param>
    /// <param name="item">The audited item.</param>
    /// <param name="location">A human readable location (map and coordinates).</param>
    /// <param name="extra">Optional extra context.</param>
    public static void Log(AuditSource source, string actor, Item item, string location, string? extra = null)
    {
        try
        {
            var definition = item.Definition;
            var serial = TryGetSerial(item);
            var builder = new StringBuilder(320);
            builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.Append(" [").Append(source.ToString()).Append(']');
            builder.Append(" actor=").Append(Sanitize(actor));
            builder.Append(" item=\"").Append(Sanitize(Describe(item))).Append('"');
            builder.Append(" group=").Append(definition?.Group.ToString(CultureInfo.InvariantCulture) ?? "?");
            builder.Append(" number=").Append(definition?.Number.ToString(CultureInfo.InvariantCulture) ?? "?");
            builder.Append(" level=").Append(item.Level.ToString(CultureInfo.InvariantCulture));
            builder.Append(" exc=").Append(CountExcellentOptions(item).ToString(CultureInfo.InvariantCulture));
            builder.Append(" anc=").Append(HasAncientSet(item) ? '1' : '0');
            builder.Append(" skill=").Append(item.HasSkill ? '1' : '0');
            builder.Append(" sockets=").Append(item.SocketCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(" serial=").Append(serial == Guid.Empty ? "-" : serial.ToString());
            builder.Append(" location=\"").Append(Sanitize(location)).Append('"');
            if (!string.IsNullOrEmpty(extra))
            {
                builder.Append(" extra=\"").Append(Sanitize(extra)).Append('"');
            }

            builder.AppendLine();

            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);
                var path = Path.Combine(LogDirectory, $"item-audit-{DateTime.UtcNow:yyyy-MM-dd}.log");
                File.AppendAllText(path, builder.ToString());
            }
        }
        catch (Exception)
        {
            // Auditing must never interfere with gameplay.
        }
    }

    /// <summary>
    /// Determines whether an item is "notable" enough to be logged by high-volume
    /// sources such as monster drops and pickups (excellent, ancient, socketed, or
    /// at least <see cref="MinNotableItemLevel"/>).
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item should be logged; otherwise <c>false</c>.</returns>
    public static bool IsNotable(Item item)
    {
        if (item.Definition is null)
        {
            return false;
        }

        return item.Level >= MinNotableItemLevel
               || item.SocketCount > 0
               || HasAncientSet(item)
               || HasExcellentOption(item);
    }

    /// <summary>
    /// Determines whether the item carries at least one excellent option.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item has an excellent option; otherwise <c>false</c>.</returns>
    public static bool HasExcellentOption(Item item) => CountExcellentOptions(item) > 0;

    /// <summary>
    /// Determines whether the item belongs to an ancient set.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item is ancient; otherwise <c>false</c>.</returns>
    public static bool HasAncientSet(Item item)
        => item.ItemSetGroups?.Any(s => s.AncientSetDiscriminator != 0) == true;

    /// <summary>
    /// Builds a human readable actor label ("character/account") for a player.
    /// </summary>
    /// <param name="player">The player, or <c>null</c>.</param>
    /// <returns>The actor label.</returns>
    public static string DescribeActor(Player? player)
    {
        if (player?.SelectedCharacter is { } character)
        {
            return $"{character.Name}/{player.Account?.LoginName ?? "?"}";
        }

        return player?.Account?.LoginName ?? "<unknown>";
    }

    private static string DescribeLocation(GameMap? map, Point position)
        => $"{map?.Definition.Name}({position.X},{position.Y})";

    private static Guid TryGetSerial(Item item)
    {
        try
        {
            return item.GetId();
        }
        catch (Exception)
        {
            return Guid.Empty;
        }
    }

    private static int CountExcellentOptions(Item item)
        => item.ItemOptions?.Count(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent) ?? 0;

    private static string Describe(Item item)
    {
        var text = item.ToString();
        var separator = text.IndexOf(": ", StringComparison.Ordinal);
        return separator >= 0 ? text[(separator + 2)..] : text;
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    /// <summary>
    /// Identifies the source/event that produced or moved the audited item.
    /// </summary>
    public enum AuditSource
    {
        /// <summary>An item generated by a monster kill.</summary>
        MonsterDrop,

        /// <summary>An item created by the GM /item command.</summary>
        GmCommand,

        /// <summary>An item produced by opening a box (e.g. Box of Luck/Kundun).</summary>
        BoxReward,

        /// <summary>An item produced by the Chaos Machine / crafting.</summary>
        Crafting,

        /// <summary>An item handed over through a player trade.</summary>
        Trade,

        /// <summary>An item picked up from the ground into a player's inventory.</summary>
        Pickup,

        /// <summary>An item granted as a quest reward.</summary>
        Quest,
    }
}
