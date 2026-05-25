// <copyright file="FixBuffSkillSafezoneAndMasterEffectsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes active buff master definitions so their values can be applied to the matching magic-effect attributes.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7C31229E-8EF2-4A68-96F5-011C9B0E4E1F")]
public class FixBuffSkillSafezoneAndMasterEffectsPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Fix Buff Skill Safezone And Master Effects";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Fixes active buff master skill formulas, target attributes, aggregate types, and copied buff behavior.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixBuffSkillSafezoneAndMasterEffects;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 05, 24, 16, 45, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.FixMagicEffectAggregates(gameConfiguration);
        this.FixActiveBuffMasterDefinitions(context, gameConfiguration);
        await ValueTask.CompletedTask;
    }

    private void FixMagicEffectAggregates(GameConfiguration gameConfiguration)
    {
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.GreaterFortitude, Stats.MaximumHealth, AggregateType.Multiplicate);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.GreaterDamage, Stats.GreaterDamageBonus, AggregateType.AddRaw);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.GreaterDefense, Stats.DefenseFinal, AggregateType.AddFinal);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.IncreaseHealth, Stats.TotalVitality, AggregateType.AddFinal);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.IncreaseBlock, Stats.DefenseRatePvm, AggregateType.AddFinal);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.IncreaseBlock, Stats.DefenseRatePvp, AggregateType.AddFinal);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.WizEnhance, Stats.WizardryAttackDamageIncrease, AggregateType.Multiplicate, 1.2f);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.SoulBarrier, Stats.SoulBarrierReceiveDecrement, AggregateType.AddRaw);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.SoulBarrier, Stats.SoulBarrierManaTollPerHit, AggregateType.AddRaw);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.Berserker, Stats.BerserkerCurseMultiplier, AggregateType.AddRaw);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.Berserker, Stats.BerserkerProficiencyMultiplier, AggregateType.AddRaw);
        this.SetPowerUpAggregate(gameConfiguration, MagicEffectNumber.CriticalDamageIncrease, Stats.CriticalDamageBonus, AggregateType.AddRaw);
    }

    private void FixActiveBuffMasterDefinitions(IContext context, GameConfiguration gameConfiguration)
    {
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.SwellLifeStrengt, SkillNumber.SwellLife, $"{SkillsInitializer.Formula181} / 100", SkillsInitializer.Formula181, Stats.MaximumHealth, AggregateType.Multiplicate);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.SwellLifeProficiency, SkillNumber.SwellLifeStrengt, $"{SkillsInitializer.Formula181} / 100", SkillsInitializer.Formula181, Stats.MaximumHealth, AggregateType.Multiplicate);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.ExpansionofWizStreng, SkillNumber.ExpansionofWizardry, SkillsInitializer.Formula120Value, SkillsInitializer.Formula120, Stats.WizardryAttackDamageIncrease, AggregateType.Multiplicate);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.ExpansionofWizMas, SkillNumber.ExpansionofWizStreng, SkillsInitializer.Formula120Value, SkillsInitializer.Formula120, Stats.WizardryAttackDamageIncrease, AggregateType.Multiplicate);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.AttackIncreaseStr, SkillNumber.GreaterDamage, SkillsInitializer.Formula502, SkillsInitializer.Formula502, Stats.GreaterDamageBonus, AggregateType.AddRaw);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.AttackIncreaseMastery, SkillNumber.AttackIncreaseStr, SkillsInitializer.Formula502, SkillsInitializer.Formula502, Stats.GreaterDamageBonus, AggregateType.AddRaw);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.DefenseIncreaseStr, SkillNumber.GreaterDefense, SkillsInitializer.Formula502, SkillsInitializer.Formula502, Stats.DefenseFinal, AggregateType.AddFinal);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.DefenseIncreaseMastery, SkillNumber.DefenseIncreaseStr, SkillsInitializer.Formula502, SkillsInitializer.Formula502, Stats.DefenseFinal, AggregateType.AddFinal);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.DefSuccessRateIncPowUp, SkillNumber.IncreaseBlock, SkillsInitializer.Formula502, SkillsInitializer.Formula502, null, AggregateType.AddFinal);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.DefSuccessRateIncMastery, SkillNumber.DefSuccessRateIncPowUp, SkillsInitializer.Formula502, SkillsInitializer.Formula502, null, AggregateType.AddFinal);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.StaminaIncreaseStrengthener, SkillNumber.IncreaseHealth, SkillsInitializer.Formula1154, SkillsInitializer.Formula1154, Stats.TotalVitality, AggregateType.AddFinal);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.SoulBarrierStrength, SkillNumber.SoulBarrier, $"{SkillsInitializer.Formula181} / 100", SkillsInitializer.Formula181, Stats.SoulBarrierReceiveDecrement, AggregateType.AddRaw);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.SoulBarrierProficie, SkillNumber.SoulBarrierStrength, SkillsInitializer.Formula803, SkillsInitializer.Formula803, null, AggregateType.AddRaw, true);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.BerserkerStrengthener, SkillNumber.Berserker, $"{SkillsInitializer.Formula181} / 100", SkillsInitializer.Formula181, Stats.BerserkerCurseMultiplier, AggregateType.AddRaw);
        this.SetMasterDefinition(context, gameConfiguration, SkillNumber.BerserkerProficiency, SkillNumber.BerserkerStrengthener, $"{SkillsInitializer.Formula181} / 100", SkillsInitializer.Formula181, Stats.BerserkerProficiencyMultiplier, AggregateType.AddRaw);
    }

    private void SetMasterDefinition(
        IContext context,
        GameConfiguration gameConfiguration,
        SkillNumber skillNumber,
        SkillNumber replacedSkillNumber,
        string valueFormula,
        string displayValueFormula,
        AttributeDefinition? targetAttribute,
        AggregateType aggregateType,
        bool extendsDuration = false)
    {
        var skill = this.GetSkill(gameConfiguration, skillNumber);
        var replacedSkill = this.GetSkill(gameConfiguration, replacedSkillNumber);
        if (skill is null || replacedSkill is null)
        {
            return;
        }

        skill.MasterDefinition ??= context.CreateNew<MasterSkillDefinition>();
        skill.MasterDefinition.ValueFormula = valueFormula;
        skill.MasterDefinition.DisplayValueFormula = displayValueFormula;
        skill.MasterDefinition.TargetAttribute = targetAttribute?.GetPersistent(gameConfiguration);
        skill.MasterDefinition.Aggregation = aggregateType;
        skill.MasterDefinition.ReplacedSkill = replacedSkill;
        skill.MasterDefinition.ExtendsDuration = extendsDuration;
        this.CopySkillBehavior(gameConfiguration, context, skill, replacedSkill);
    }

    private void SetPowerUpAggregate(
        GameConfiguration gameConfiguration,
        MagicEffectNumber effectNumber,
        AttributeDefinition targetAttribute,
        AggregateType aggregateType,
        float? constantValue = null)
    {
        var persistentTarget = targetAttribute.GetPersistent(gameConfiguration);
        var powerUp = gameConfiguration.MagicEffects
            .FirstOrDefault(e => e.Number == (short)effectNumber)?
            .PowerUpDefinitions
            .FirstOrDefault(p => p.TargetAttribute == persistentTarget);
        if (powerUp?.Boost?.ConstantValue is null)
        {
            return;
        }

        if (constantValue is { } value)
        {
            powerUp.Boost.ConstantValue.Value = value;
        }

        powerUp.Boost.ConstantValue.AggregateType = aggregateType;
    }

    private Skill? GetSkill(GameConfiguration gameConfiguration, SkillNumber skillNumber)
    {
        return gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)skillNumber);
    }

    private void CopySkillBehavior(GameConfiguration gameConfiguration, IContext context, Skill skill, Skill sourceSkill)
    {
        skill.AttackDamage = sourceSkill.AttackDamage;
        skill.DamageType = sourceSkill.DamageType;
        skill.ElementalModifierTarget = sourceSkill.ElementalModifierTarget;
        skill.SkipElementalModifier = sourceSkill.SkipElementalModifier;
        skill.ImplicitTargetRange = sourceSkill.ImplicitTargetRange;
        skill.MovesTarget = sourceSkill.MovesTarget;
        skill.MovesToTarget = sourceSkill.MovesToTarget;
        skill.SkillType = sourceSkill.SkillType;
        skill.Target = sourceSkill.Target;
        skill.TargetRestriction = sourceSkill.TargetRestriction;
        skill.MagicEffectDef = sourceSkill.MagicEffectDef;

        if (sourceSkill.AreaSkillSettings is { } sourceAreaSkillSettings)
        {
            skill.AreaSkillSettings = context.CreateNew<AreaSkillSettings>();
            var id = skill.AreaSkillSettings.GetId();
            skill.AreaSkillSettings.AssignValuesOf(sourceAreaSkillSettings, gameConfiguration);
            skill.AreaSkillSettings.SetGuid(id);
        }
        else
        {
            skill.AreaSkillSettings = null;
        }
    }
}
