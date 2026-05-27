// <copyright file="DuelSeasonEndChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: GM chat command which ends the current Duel Ladder season.
/// Snapshots the top 10 per reset bracket to a flat log file, bulk-resets every character's
/// DuelRating/Wins/Losses/Bracket to defaults, and writes the new season-start timestamp
/// (used later for distinct-opponent reward eligibility once that audit table lands).
/// Reward grants are deferred until the W Coin foundation is merged.
/// </summary>
[Guid("D5E20BF2-8C3F-4F2D-8E45-B2C3D4E5F6A7")]
[PlugIn]
[Display(Name = nameof(DuelSeasonEndChatCommandPlugIn), Description = "BarnaMu: GM command to end the current Duel Ladder season (/duelseasonend).")]
public class DuelSeasonEndChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/duelseasonend";
    private const byte BracketCount = 5;
    private const int TopN = 10;
    private const string SeasonFileName = "current_season.txt";

    /// <inheritdoc/>
    public string Key => Command;

    /// <inheritdoc/>
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc/>
    public async ValueTask HandleCommandAsync(Player gameMaster, string command)
    {
        try
        {
            await EndSeasonAsync(gameMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            gameMaster.Logger.LogError(ex, "Duel Ladder: error ending season.");
            await SendAsync(gameMaster, "Error ending Duel Ladder season - see server log.").ConfigureAwait(false);
        }
    }

    private static async ValueTask EndSeasonAsync(Player gameMaster)
    {
        // Player context is scoped to a single account's aggregate, and the generic
        // CreateNewContext eagerly tracks the entire GameConfiguration (saving it tries
        // to re-insert rows like DropItemGroupItemDefinition and conflicts on the PK).
        // The typed context is the right tool here: tracks only Character entities, so
        // a cross-account bulk read + write goes through cleanly.
        using var context = gameMaster.GameContext.PersistenceContextProvider.CreateNewTypedContext(typeof(Character), useCache: false, gameMaster.GameContext.Configuration);
        var allCharacters = (await context.GetAsync<Character>().ConfigureAwait(false)).ToList();
        var snapshotTime = DateTime.UtcNow;

        // 1) Snapshot top-N per reset bracket BEFORE the reset wipes the values.
        var (snapshotText, topWinners) = BuildSnapshot(allCharacters, snapshotTime);

        // 2) Reset online players' in-memory copies first so a concurrent save cannot
        //    overwrite the bulk DB reset that follows.
        var onlinePlayers = await gameMaster.GameContext.GetPlayersAsync().ConfigureAwait(false);
        foreach (var player in onlinePlayers)
        {
            if (player.SelectedCharacter is { } character)
            {
                ResetCharacter(character);
            }
        }

        // 3) Bulk reset every character row in the DB.
        foreach (var character in allCharacters)
        {
            ResetCharacter(character);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        // 4) Persist snapshot + new season-start timestamp to disk.
        var logDir = GetLogDir();
        WriteSnapshotToDisk(snapshotText, snapshotTime, logDir, gameMaster.Logger);
        WriteCurrentSeasonStart(snapshotTime, logDir, gameMaster.Logger);
        gameMaster.Logger.LogInformation(
            "Duel Ladder season ended at {Time}. {Count} characters reset.",
            snapshotTime,
            allCharacters.Count);

        // 5) Reply to the GM via chat.
        await SendAsync(gameMaster, $"=== Duel Ladder Season Ended === {snapshotTime:yyyy-MM-dd HH:mm} UTC ===").ConfigureAwait(false);
        await SendAsync(gameMaster, $"{allCharacters.Count} characters reset to {DuelLadderService.BaseRating}.").ConfigureAwait(false);
        foreach (var line in topWinners)
        {
            await SendAsync(gameMaster, line).ConfigureAwait(false);
        }

        await SendAsync(gameMaster, $"Snapshot logged to {logDir}.").ConfigureAwait(false);
        await SendAsync(gameMaster, "(Rewards: grant manually for now; auto-grant lands with W Coin foundation.)").ConfigureAwait(false);
    }

    private static (string SnapshotText, List<string> TopWinners) BuildSnapshot(List<Character> all, DateTime time)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Duel Ladder Season End === {time:u} ===");
        var topWinners = new List<string>();

        for (byte bracket = 1; bracket <= BracketCount; bracket++)
        {
            var top = all
                .Where(c => c.DuelResetBracket == bracket && (c.DuelWins + c.DuelLosses) > 0)
                .OrderByDescending(c => c.DuelRating)
                .Take(TopN)
                .ToList();

            sb.AppendLine($"--- T{bracket} top {TopN} ---");
            if (top.Count == 0)
            {
                sb.AppendLine("(no eligible players)");
                topWinners.Add($"T{bracket}: (no players)");
                continue;
            }

            int rank = 1;
            foreach (var c in top)
            {
                sb.AppendLine($"{rank,2}. {c.Name,-12} rating={c.DuelRating,5} W={c.DuelWins,4} L={c.DuelLosses,4}");
                rank++;
            }

            var champ = top[0];
            topWinners.Add($"T{bracket} champ: {champ.Name} ({champ.DuelRating}, {champ.DuelWins}W/{champ.DuelLosses}L)");
        }

        sb.AppendLine("=== End of Season Snapshot ===");
        return (sb.ToString(), topWinners);
    }

    private static void ResetCharacter(Character character)
    {
        character.DuelRating = DuelLadderService.BaseRating;
        character.DuelWins = 0;
        character.DuelLosses = 0;
        character.DuelResetBracket = 0;
    }

    private static string GetLogDir()
    {
        return Environment.GetEnvironmentVariable("BARNAMU_DUEL_LADDER_DIR") ?? @"C:\MuDev\Logs\DuelLadder";
    }

    private static void WriteSnapshotToDisk(string snapshot, DateTime time, string dir, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"season_{time:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(file, snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Duel Ladder: failed to write season snapshot to disk.");
        }
    }

    private static void WriteCurrentSeasonStart(DateTime time, string dir, ILogger logger)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, SeasonFileName);
            File.WriteAllText(file, time.ToString("O"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Duel Ladder: failed to update {File}.", SeasonFileName);
        }
    }

    private static async ValueTask SendAsync(Player player, string message)
    {
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(p => p.ShowMessageAsync(message, MessageType.BlueNormal)).ConfigureAwait(false);
    }
}
