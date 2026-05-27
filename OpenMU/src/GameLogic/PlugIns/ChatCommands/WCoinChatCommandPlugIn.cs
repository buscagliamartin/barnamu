// <copyright file="WCoinChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.CashShop;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu GM command which grants or removes W Coin from an account by character name.
/// </summary>
[Guid("B91F0F9A-6E7A-4D1B-A38F-61E6E5D9F5C2")]
[PlugIn]
[Display(Name = "W Coin Chat Command", Description = "BarnaMu: GM command to grant or remove W Coin. Usage: /wcoin <character> <amount> [note].")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class WCoinChatCommandPlugIn : ChatCommandPlugInBase<WCoinChatCommandPlugIn.Arguments>
{
    private const string Command = "/wcoin";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, Arguments arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.CharacterName) || arguments.Amount == 0)
        {
            await gameMaster.ShowBlueMessageAsync("Uso: /wcoin <personaje> <cantidad> [nota]. Usa cantidad negativa para debitar.").ConfigureAwait(false);
            return;
        }

        var actor = gameMaster.SelectedCharacter?.Name ?? gameMaster.Name;
        var reason = arguments.Amount > 0 ? "GmGrant" : "GmDebit";
        var targetPlayer = gameMaster.GameContext.GetPlayerByCharacterName(arguments.CharacterName);
        if (targetPlayer?.Account is { } onlineAccount
            && targetPlayer.SelectedCharacter?.Name is { } selectedName
            && selectedName.Equals(arguments.CharacterName, StringComparison.OrdinalIgnoreCase))
        {
            if (!WCoinService.TryApply(targetPlayer.PersistenceContext, onlineAccount, arguments.Amount, reason, "GameServerCommand", actor, arguments.Note, out var error))
            {
                await gameMaster.ShowBlueMessageAsync(error!).ConfigureAwait(false);
                return;
            }

            await targetPlayer.SaveProgressAsync().ConfigureAwait(false);
            await targetPlayer.ShowBlueMessageAsync($"W Coin actualizado. Balance: {onlineAccount.WCoin}.").ConfigureAwait(false);
            await gameMaster.ShowBlueMessageAsync($"W Coin de '{arguments.CharacterName}' actualizado a {onlineAccount.WCoin} (online).").ConfigureAwait(false);
            return;
        }

        using var context = gameMaster.GameContext.PersistenceContextProvider.CreateNewPlayerContext(gameMaster.GameContext.Configuration);
        var account = await context.GetAccountByCharacterNameAsync(arguments.CharacterName).ConfigureAwait(false);
        if (account is null)
        {
            await gameMaster.ShowBlueMessageAsync($"No se encontro la cuenta del personaje '{arguments.CharacterName}'.").ConfigureAwait(false);
            return;
        }

        if (!WCoinService.TryApply(context, account, arguments.Amount, reason, "GameServerCommand", actor, arguments.Note, out var offlineError))
        {
            await gameMaster.ShowBlueMessageAsync(offlineError!).ConfigureAwait(false);
            return;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        await gameMaster.ShowBlueMessageAsync($"W Coin de '{arguments.CharacterName}' actualizado a {account.WCoin}.").ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for the <c>/wcoin</c> command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets a character name of the target account.
        /// </summary>
        public string? CharacterName { get; set; }

        /// <summary>
        /// Gets or sets the signed W Coin amount to apply.
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Gets or sets an optional one-word audit note.
        /// </summary>
        public string? Note { get; set; }
    }
}
