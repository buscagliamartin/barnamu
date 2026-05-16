// <copyright file="SetVipChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// BarnaMu: GM chat command which grants the <see cref="AccountState.Vip"/> state with an
/// expiration timer.
/// Usage: <c>/setvip &lt;character&gt; [days]</c>. When the days argument is omitted (or 0),
/// a default of 30 days is used.
/// When the timer expires the account is automatically reverted to <see cref="AccountState.Normal"/>
/// (online players via the VipExpirationCheckPlugIn, offline accounts on their next login).
/// </summary>
[Guid("D4E8F1A2-3B6C-4D7E-9F0A-1B2C3D4E5F60")]
[PlugIn]
[Display(Name = "Set VIP Chat Command", Description = "BarnaMu: GM command to grant VIP status with an expiration timer. Usage: /setvip <character> [days] (default 30).")]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class SetVipChatCommandPlugIn : ChatCommandPlugInBase<SetVipChatCommandPlugIn.Arguments>
{
    private const string Command = "/setvip";
    private const int DefaultVipDays = 30;
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player gameMaster, Arguments arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.CharacterName))
        {
            await gameMaster.ShowBlueMessageAsync("Uso: /setvip <personaje> [dias]  (por defecto 30 dias)").ConfigureAwait(false);
            return;
        }

        var days = arguments.Days > 0 ? arguments.Days : DefaultVipDays;
        var expiration = DateTime.UtcNow.AddDays(days);

        // Si el jugador esta online, actualizamos su cuenta en memoria para que el VIP
        // tenga efecto inmediato sin necesidad de reconectar.
        var targetPlayer = gameMaster.GameContext.GetPlayerByCharacterName(arguments.CharacterName);
        if (targetPlayer?.Account is { } onlineAccount
            && targetPlayer.SelectedCharacter?.Name is { } selectedName
            && selectedName.Equals(arguments.CharacterName, StringComparison.OrdinalIgnoreCase))
        {
            onlineAccount.State = AccountState.Vip;
            onlineAccount.VipExpirationDate = expiration;
            onlineAccount.IsVaultExtended = true;
            await targetPlayer.SaveProgressAsync().ConfigureAwait(false);
            await targetPlayer.ShowBlueMessageAsync($"Ahora sos VIP por {days} dia(s). Vence: {expiration:yyyy-MM-dd HH:mm} UTC.").ConfigureAwait(false);
            await gameMaster.ShowBlueMessageAsync($"VIP asignado a '{arguments.CharacterName}' por {days} dia(s) (jugador online).").ConfigureAwait(false);
            return;
        }

        // Jugador offline: actualizamos directamente en la base de datos.
        using var context = gameMaster.GameContext.PersistenceContextProvider.CreateNewPlayerContext(gameMaster.GameContext.Configuration);
        var account = await context.GetAccountByCharacterNameAsync(arguments.CharacterName).ConfigureAwait(false);
        if (account is null)
        {
            await gameMaster.ShowBlueMessageAsync($"No se encontro la cuenta del personaje '{arguments.CharacterName}'.").ConfigureAwait(false);
            return;
        }

        account.State = AccountState.Vip;
        account.VipExpirationDate = expiration;
        account.IsVaultExtended = true;
        await context.SaveChangesAsync().ConfigureAwait(false);
        await gameMaster.ShowBlueMessageAsync($"VIP asignado a '{arguments.CharacterName}' por {days} dia(s). Vence: {expiration:yyyy-MM-dd HH:mm} UTC.").ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for the <c>/setvip</c> command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the name of a character of the account to grant VIP to.
        /// </summary>
        public string? CharacterName { get; set; }

        /// <summary>
        /// Gets or sets the amount of VIP days. When 0 or not provided, a default of 30 days is used.
        /// </summary>
        public int Days { get; set; }
    }
}
