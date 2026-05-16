// <copyright file="ChatMessageNormalProcessor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace MUnique.OpenMU.GameLogic.PlayerActions.Chat;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
/// <summary>
/// A chat message processor for normal chat.
/// </summary>
public class ChatMessageNormalProcessor : BannableChatMessageBaseProcessor
{
    /// <inheritdoc/>
    public override async ValueTask SubclassProcessMessageAsync(Player sender, (string Message, string PlayerName) content)
    {
        sender.Logger.LogDebug("Sending Chat Message to Observers, Count: {0}", sender.Observers.Count);

        // BarnaMu: para que el chat bubble aparezca sobre el personaje, el sender name TIENE
        // que matchear con el Name real del Character en pantalla (el cliente busca el char
        // con ese nombre exacto). Si prepenmos "[VIP]" o "[GM]" al sender name, el lookup
        // del cliente falla y no se muestra el bubble (solo aparece en la barra de chat
        // izquierda, donde no le importa el matching).
        //
        // Fix: dejar el sender name como el nombre real del personaje, y meter el tag
        // [VIP]/[GM] adentro del texto del mensaje. Asi:
        //   - El bubble sale sobre el personaje ✓
        //   - El tag se ve tanto en el bubble como en la barra de chat ✓
        var characterName = sender.SelectedCharacter!.Name;
        var prefix = sender.Account?.State switch
        {
            AccountState.Vip => "[VIP] ",
            AccountState.GameMaster => "[GM] ",
            AccountState.GameMasterInvisible => "[GM] ",
            _ => string.Empty,
        };

        var messageToSend = prefix + content.Message;
        await sender.ForEachWorldObserverAsync<IChatViewPlugIn>(p => p.ChatMessageAsync(messageToSend, characterName, ChatMessageType.Normal), true).ConfigureAwait(false);
    }
}
