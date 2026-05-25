// <copyright file="JewelBankAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// BarnaMu: action for the per-account item bank, accessed from the MU Helper menu.
/// Each of the 17 slots holds one pooled count (in single units); the client displays
/// it split into loose singles (count % 10) and packs (count / 10).
/// Slots 0-9 are jewels (keyed by <see cref="DataModel.Configuration.JewelMix.Number"/>);
/// slots 10-16 are boxes (Box of Kundun +1..+5, Blue/Pink Chocolate Box).
/// </summary>
public class JewelBankAction
{
    /// <summary>
    /// The total number of bank slots.
    /// </summary>
    public const int SlotCount = 17;

    /// <summary>
    /// Box slot definitions (item group, item number, item level) for slots 10-16.
    /// </summary>
    private static readonly (int Group, int Number, byte Level)[] BoxSlots =
    {
        (14, 11, 8),  // 10: Box of Kundun +1 (Box of Luck, level 8)
        (14, 11, 9),  // 11: Box of Kundun +2
        (14, 11, 10), // 12: Box of Kundun +3
        (14, 11, 11), // 13: Box of Kundun +4
        (14, 11, 12), // 14: Box of Kundun +5
        (14, 34, 0),  // 15: Blue Chocolate Box
        (14, 32, 0),  // 16: Pink Chocolate Box
    };

    /// <summary>
    /// Sends the current item bank balances to the player's client.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask ShowBalancesAsync(Player player)
    {
        if (player.Account is null)
        {
            return;
        }

        await player.InvokeViewPlugInAsync<IJewelBankBalancesPlugIn>(p => p.ShowBalancesAsync()).ConfigureAwait(false);
    }

    /// <summary>
    /// Deposits one item into the given bank slot, taken from the player's inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The bank slot (0-16).</param>
    /// <param name="pack">If set, deposits a packed item (jewel pack, or 10 boxes); otherwise a single.</param>
    public async ValueTask DepositAsync(Player player, byte slot, bool pack)
    {
        if (slot >= SlotCount || player.Account is not { } account || player.Inventory is null)
        {
            return;
        }

        if (slot < 10)
        {
            var mix = player.GameContext.Configuration.JewelMixes.FirstOrDefault(m => m.Number == slot);
            if (mix?.SingleJewel is null || mix.MixedJewel is null)
            {
                return;
            }

            if (pack)
            {
                var packed = player.Inventory.Items.FirstOrDefault(i => i.Definition == mix.MixedJewel);
                if (packed is null)
                {
                    return;
                }

                var amount = (packed.Level + 1) * 10;
                await RemoveAsync(player, packed).ConfigureAwait(false);
                AddCount(account, slot, amount);
            }
            else
            {
                var single = player.Inventory.Items.FirstOrDefault(i => i.Definition == mix.SingleJewel);
                if (single is null)
                {
                    return;
                }

                await RemoveAsync(player, single).ConfigureAwait(false);
                AddCount(account, slot, 1);
            }
        }
        else
        {
            var (group, number, level) = BoxSlots[slot - 10];
            var definition = player.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == group && i.Number == number);
            if (definition is null)
            {
                return;
            }

            if (pack)
            {
                var boxes = player.Inventory.Items.Where(i => i.Definition == definition && i.Level == level).Take(10).ToList();
                if (boxes.Count < 10)
                {
                    return;
                }

                foreach (var box in boxes)
                {
                    await RemoveAsync(player, box).ConfigureAwait(false);
                }

                AddCount(account, slot, 10);
            }
            else
            {
                var box = player.Inventory.Items.FirstOrDefault(i => i.Definition == definition && i.Level == level);
                if (box is null)
                {
                    return;
                }

                await RemoveAsync(player, box).ConfigureAwait(false);
                AddCount(account, slot, 1);
            }
        }

        await this.ShowBalancesAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws one item from the given bank slot into the player's inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The bank slot (0-16).</param>
    /// <param name="pack">If set, withdraws a packed item (jewel pack, or 10 boxes); otherwise a single.</param>
    public async ValueTask WithdrawAsync(Player player, byte slot, bool pack)
    {
        if (slot >= SlotCount || player.Account is not { } account || player.Inventory is null)
        {
            return;
        }

        var needed = pack ? 10 : 1;
        if (GetCount(account, slot) < needed)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.YouLackOfJewels)).ConfigureAwait(false);
            return;
        }

        if (slot < 10)
        {
            var mix = player.GameContext.Configuration.JewelMixes.FirstOrDefault(m => m.Number == slot);
            if (mix?.SingleJewel is null || mix.MixedJewel is null)
            {
                return;
            }

            var freeSlots = player.Inventory.FreeSlots.Take(1).ToList();
            if (freeSlots.Count < 1)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryNotEnoughSpace)).ConfigureAwait(false);
                return;
            }

            AddCount(account, slot, -needed);
            await CreateAsync(player, freeSlots[0], pack ? mix.MixedJewel : mix.SingleJewel, 0).ConfigureAwait(false);
        }
        else
        {
            var (group, number, level) = BoxSlots[slot - 10];
            var definition = player.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == group && i.Number == number);
            if (definition is null)
            {
                return;
            }

            var itemsToCreate = pack ? 10 : 1;
            var freeSlots = player.Inventory.FreeSlots.Take(itemsToCreate).ToList();
            if (freeSlots.Count < itemsToCreate)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryNotEnoughSpace)).ConfigureAwait(false);
                return;
            }

            AddCount(account, slot, -needed);
            foreach (var freeSlot in freeSlots)
            {
                await CreateAsync(player, freeSlot, definition, level).ConfigureAwait(false);
            }
        }

        await this.ShowBalancesAsync(player).ConfigureAwait(false);
    }

    private static async ValueTask RemoveAsync(Player player, Item item)
    {
        var slot = item.ItemSlot;
        await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
    }

    private static async ValueTask CreateAsync(Player player, byte slot, ItemDefinition definition, byte level)
    {
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = 1;
        item.Level = level;
        item.ItemSlot = slot;
        await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
    }

    private static int GetCount(Account account, int slot) => slot switch
    {
        0 => account.JewelBankBless,
        1 => account.JewelBankSoul,
        2 => account.JewelBankLife,
        3 => account.JewelBankCreation,
        4 => account.JewelBankGuardian,
        5 => account.JewelBankGemstone,
        6 => account.JewelBankHarmony,
        7 => account.JewelBankChaos,
        8 => account.JewelBankLowerRefineStone,
        9 => account.JewelBankHigherRefineStone,
        10 => account.JewelBankKundun1,
        11 => account.JewelBankKundun2,
        12 => account.JewelBankKundun3,
        13 => account.JewelBankKundun4,
        14 => account.JewelBankKundun5,
        15 => account.JewelBankChocoBlue,
        16 => account.JewelBankChocoPink,
        _ => 0,
    };

    private static void AddCount(Account account, int slot, int delta)
    {
        switch (slot)
        {
            case 0: account.JewelBankBless += delta; break;
            case 1: account.JewelBankSoul += delta; break;
            case 2: account.JewelBankLife += delta; break;
            case 3: account.JewelBankCreation += delta; break;
            case 4: account.JewelBankGuardian += delta; break;
            case 5: account.JewelBankGemstone += delta; break;
            case 6: account.JewelBankHarmony += delta; break;
            case 7: account.JewelBankChaos += delta; break;
            case 8: account.JewelBankLowerRefineStone += delta; break;
            case 9: account.JewelBankHigherRefineStone += delta; break;
            case 10: account.JewelBankKundun1 += delta; break;
            case 11: account.JewelBankKundun2 += delta; break;
            case 12: account.JewelBankKundun3 += delta; break;
            case 13: account.JewelBankKundun4 += delta; break;
            case 14: account.JewelBankKundun5 += delta; break;
            case 15: account.JewelBankChocoBlue += delta; break;
            case 16: account.JewelBankChocoPink += delta; break;
        }
    }
}
