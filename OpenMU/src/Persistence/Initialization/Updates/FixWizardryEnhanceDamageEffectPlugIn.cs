// <copyright file="FixWizardryEnhanceDamageEffectPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes Wizardry Enhance so base, strengthener, and mastery all increase real wizardry damage.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("1B7C9624-A6D0-4677-AFC2-5AD033D03BBE")]
public class FixWizardryEnhanceDamageEffectPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Fix Wizardry Enhance Damage Effect";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Moves Wizardry Enhance from min/max-only/critical chance placeholders to the real wizardry damage multiplier.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixWizardryEnhanceDamageEffect;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 05, 24, 18, 15, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.FixWizardryEnhanceEffect(context, gameConfiguration);
        this.SetWizardryMasterDefinition(gameConfiguration, SkillNumber.ExpansionofWizStreng, SkillNumber.ExpansionofWizardry);
        this.SetWizardryMasterDefinition(gameConfiguration, SkillNumber.ExpansionofWizMas, SkillNumber.ExpansionofWizStreng);
        await ValueTask.CompletedTask;
    }

    private void FixWizardryEnhanceEffect(IContext context, GameConfiguration gameConfiguration)
    {
        var effect = gameConfiguration.MagicEffects.FirstOrDefault(e => e.Number == (short)MagicEffectNumber.WizEnhance);
        if (effect is null)
        {
            return;
        }

        var oldTargets = new[]
        {
            Stats.MinimumWizBaseDmg.GetPersistent(gameConfiguration),
            Stats.MaximumWizBaseDmg.GetPersistent(gameConfiguration),
            Stats.CriticalDamageChance.GetPersistent(gameConfiguration),
        };

        foreach (var oldPowerUp in effect.PowerUpDefinitions.Where(p => oldTargets.Contains(p.TargetAttribute)).ToList())
        {
            effect.PowerUpDefinitions.Remove(oldPowerUp);
        }

        var target = Stats.WizardryAttackDamageIncrease.GetPersistent(gameConfiguration);
        var powerUp = effect.PowerUpDefinitions.FirstOrDefault(p => p.TargetAttribute == target);
        if (powerUp is null)
        {
            powerUp = context.CreateNew<PowerUpDefinition>();
            powerUp.TargetAttribute = target;
            effect.PowerUpDefinitions.Add(powerUp);
        }

        powerUp.Boost ??= context.CreateNew<PowerUpDefinitionValue>();
        powerUp.Boost.ConstantValue.Value = 1.2f;
        powerUp.Boost.ConstantValue.AggregateType = AggregateType.Multiplicate;

        effect.InformObservers = true;
        effect.SendDuration = false;
        effect.StopByDeath = true;
        effect.SubType = 33;
        effect.Duration ??= context.CreateNew<PowerUpDefinitionValue>();
        effect.Duration.ConstantValue.Value = 1200f;
    }

    private void SetWizardryMasterDefinition(GameConfiguration gameConfiguration, SkillNumber skillNumber, SkillNumber replacedSkillNumber)
    {
        var skill = gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)skillNumber);
        var replacedSkill = gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)replacedSkillNumber);
        var effect = gameConfiguration.MagicEffects.FirstOrDefault(e => e.Number == (short)MagicEffectNumber.WizEnhance);
        if (skill?.MasterDefinition is null)
        {
            return;
        }

        skill.SkillType = SkillType.Buff;
        skill.Target = SkillTarget.ImplicitPlayer;
        skill.MagicEffectDef = effect;
        skill.MasterDefinition.ValueFormula = SkillsInitializer.Formula120Value;
        skill.MasterDefinition.DisplayValueFormula = SkillsInitializer.Formula120;
        skill.MasterDefinition.TargetAttribute = Stats.WizardryAttackDamageIncrease.GetPersistent(gameConfiguration);
        skill.MasterDefinition.Aggregation = AggregateType.Multiplicate;
        skill.MasterDefinition.ReplacedSkill = replacedSkill;
    }
}
