// <copyright file="QuestMonsterKillCountPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This plugin increases the monster kill count of the quest state of active quests.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.QuestMonsterKillCountPlugIn_Name), Description = nameof(PlugInResources.QuestMonsterKillCountPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("416C2231-D7FE-414A-9321-26622E262EF5")]
public class QuestMonsterKillCountPlugIn : IAttackableGotKilledPlugIn, ISupportCustomConfiguration<QuestMonsterKillCountPlugInConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc/>
    public QuestMonsterKillCountPlugInConfiguration? Configuration { get; set; }

    /// <summary>
    /// Is called when an <see cref="IAttackable" /> object got killed by another.
    /// </summary>
    /// <param name="killed">The killed <see cref="IAttackable" />.</param>
    /// <param name="killer">The killer.</param>
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        var configuration = this.Configuration ??= CreateDefaultConfiguration();

        // BarnaMu fix: vanilla required `killer is Player`, which silently dropped
        // every kill done by a Summon (BK mascot, Summoner pet) or any IPlayerSurrogate
        // (skill projectiles, area-skill emitters, etc.). The result was that quests
        // like "Into the Darkness" (kill Dark Elf #412) never registered kills when
        // the player used skills. Unwrap the player the same way AttackableNpcBase
        // does in GetHitNotificationTarget.
        var player = killer as Player ?? (killer as IPlayerSurrogate)?.Owner;
        if (player is null
            || killed is not Monster monster
            || player.SelectedCharacter?.QuestStates is null)
        {
            return;
        }

        foreach (var questState in player.SelectedCharacter.QuestStates)
        {
            if (questState.ActiveQuest is null)
            {
                continue;
            }

            foreach (var killRequirement in questState.ActiveQuest.RequiredMonsterKills.Where(r => object.Equals(r.Monster, monster.Definition)))
            {
                if (questState.RequirementStates.FirstOrDefault(s => object.Equals(s.Requirement, killRequirement))
                    is not { } requirementState)
                {
                    requirementState = player.PersistenceContext.CreateNew<QuestMonsterKillRequirementState>();
                    requirementState.Requirement = killRequirement;
                    questState.RequirementStates.Add(requirementState);
                }

                requirementState!.KillCount++;

                if (killRequirement.MinimumNumber >= requirementState!.KillCount
                    && configuration.Message.GetTranslation(player.Culture) is { Length: > 0 } translation)
                {
                    var message = string.Format(
                        translation,
                        questState.ActiveQuest.Name.GetTranslation(player.Culture),
                        monster.Definition.Designation.GetTranslation(player.Culture),
                        requirementState.KillCount,
                        killRequirement.MinimumNumber);

                    await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
                }
            }
        }
    }

    /// <inheritdoc/>
    public object CreateDefaultConfig()
    {
        return CreateDefaultConfiguration();
    }

    private static QuestMonsterKillCountPlugInConfiguration CreateDefaultConfiguration()
    {
        return new QuestMonsterKillCountPlugInConfiguration();
    }
}