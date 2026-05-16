// <copyright file="PartyAutoCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Chat command to toggle automatic party request response.
/// /re auto  → auto-accept all party requests.
/// /re off   → auto-decline all party requests.
/// /re       → return to normal (show popup).
/// </summary>
[Guid("A3F2C1D4-8B6E-4F9A-B2D7-5C3E1A8F0D62")]
[PlugIn]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class PartyAutoCommandPlugIn : ChatCommandPlugInBase<PartyAutoCommandPlugIn.Arguments>
{
    private const string Command = "/re";
    private const CharacterStatus MinimumStatus = CharacterStatus.Normal;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        switch (arguments.Mode?.ToLowerInvariant())
        {
            case "auto":
                player.PartyAutoMode = PartyAutoMode.AutoAccept;
                await player.ShowBlueMessageAsync("Party auto-accept: ON").ConfigureAwait(false);
                break;
            case "off":
                player.PartyAutoMode = PartyAutoMode.AutoDecline;
                await player.ShowBlueMessageAsync("Party auto-decline: ON").ConfigureAwait(false);
                break;
            default:
                player.PartyAutoMode = PartyAutoMode.Normal;
                await player.ShowBlueMessageAsync("Party auto mode: OFF (normal)").ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Arguments for the /re command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the mode: "auto", "off", or empty to reset.
        /// </summary>
        public string? Mode { get; set; }
    }
}
