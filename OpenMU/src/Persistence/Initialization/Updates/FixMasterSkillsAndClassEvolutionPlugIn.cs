// <copyright file="FixMasterSkillsAndClassEvolutionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Skills;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fixes master skill replacement chains, buff master scaling, and class evolution data.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("7998BCA9-6E65-4B4B-96DE-32D6E5AA9E36")]
public class FixMasterSkillsAndClassEvolutionPlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Fix Master Skills And Class Evolution";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Fixes master skill replacement chains, buff master scaling, wizardry enhance, drain life mastery, and class evolution data.";

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.FixMasterSkillsAndClassEvolution;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 05, 24, 15, 30, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.FixClassEvolution(context, gameConfiguration);
        this.FixWizardryEnhance(gameConfiguration);
        this.FixMasterSkillChains(context, gameConfiguration);
        this.FixSkillBehaviorCopies(context, gameConfiguration);
        await ValueTask.CompletedTask;
    }

    private void FixClassEvolution(IContext context, GameConfiguration gameConfiguration)
    {
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.DarkWizard, CharacterClassNumber.SoulMaster);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.SoulMaster, CharacterClassNumber.GrandMaster);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.DarkKnight, CharacterClassNumber.BladeKnight);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.BladeKnight, CharacterClassNumber.BladeMaster);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.FairyElf, CharacterClassNumber.MuseElf);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.MuseElf, CharacterClassNumber.HighElf);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.Summoner, CharacterClassNumber.BloodySummoner);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.BloodySummoner, CharacterClassNumber.DimensionMaster);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.MagicGladiator, CharacterClassNumber.DuelMaster);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.DarkLord, CharacterClassNumber.LordEmperor);
        this.SetNextGeneration(gameConfiguration, CharacterClassNumber.RageFighter, CharacterClassNumber.FistMaster);

        foreach (var classNumber in new[]
                 {
                     CharacterClassNumber.GrandMaster,
                     CharacterClassNumber.BladeMaster,
                     CharacterClassNumber.HighElf,
                     CharacterClassNumber.DimensionMaster,
                     CharacterClassNumber.DuelMaster,
                     CharacterClassNumber.LordEmperor,
                     CharacterClassNumber.FistMaster,
                 })
        {
            this.EnsureMasterClass(context, gameConfiguration, classNumber);
        }
    }

    private void SetNextGeneration(GameConfiguration gameConfiguration, CharacterClassNumber source, CharacterClassNumber target)
    {
        var sourceClass = gameConfiguration.CharacterClasses.FirstOrDefault(c => c.Number == (byte)source);
        var targetClass = gameConfiguration.CharacterClasses.FirstOrDefault(c => c.Number == (byte)target);
        if (sourceClass is not null && targetClass is not null)
        {
            sourceClass.NextGenerationClass = targetClass;
        }
    }

    private void EnsureMasterClass(IContext context, GameConfiguration gameConfiguration, CharacterClassNumber classNumber)
    {
        if (gameConfiguration.CharacterClasses.FirstOrDefault(c => c.Number == (byte)classNumber) is not { } characterClass)
        {
            return;
        }

        characterClass.IsMasterClass = true;
        this.EnsureStatAttribute(context, gameConfiguration, characterClass, Stats.MasterLevel, 0, false);
        this.EnsureBaseAttribute(context, gameConfiguration, characterClass, Stats.MasterPointsPerLevelUp, 1);
        this.EnsureBaseAttribute(context, gameConfiguration, characterClass, Stats.MasterExperienceRate, 1);
    }

    private void EnsureStatAttribute(IContext context, GameConfiguration gameConfiguration, CharacterClass characterClass, AttributeDefinition attribute, float value, bool increasableByPlayer)
    {
        var persistentAttribute = attribute.GetPersistent(gameConfiguration);
        if (characterClass.StatAttributes.Any(a => a.Attribute == persistentAttribute))
        {
            return;
        }

        characterClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(persistentAttribute, value, increasableByPlayer));
    }

    private void EnsureBaseAttribute(IContext context, GameConfiguration gameConfiguration, CharacterClass characterClass, AttributeDefinition attribute, float value)
    {
        var persistentAttribute = attribute.GetPersistent(gameConfiguration);
        if (characterClass.BaseAttributeValues.Any(a => a.Definition == persistentAttribute))
        {
            return;
        }

        characterClass.BaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(value, persistentAttribute));
    }

    private void FixWizardryEnhance(GameConfiguration gameConfiguration)
    {
        var wizEnhanceEffect = gameConfiguration.MagicEffects.FirstOrDefault(e => e.Number == (short)MagicEffectNumber.WizEnhance);
        if (wizEnhanceEffect is not null)
        {
            var critChancePowerUp = wizEnhanceEffect.PowerUpDefinitions.FirstOrDefault(p => p.TargetAttribute == Stats.CriticalDamageChance.GetPersistent(gameConfiguration));
            if (critChancePowerUp is not null)
            {
                critChancePowerUp.Boost!.ConstantValue.Value = 0f;
                critChancePowerUp.Boost.ConstantValue.AggregateType = AggregateType.AddRaw;
            }
        }

        if (this.GetSkill(gameConfiguration, SkillNumber.ExpansionofWizMas)?.MasterDefinition is { } wizardryMastery)
        {
            wizardryMastery.ValueFormula = SkillsInitializer.Formula120Value;
            wizardryMastery.DisplayValueFormula = SkillsInitializer.Formula120;
            wizardryMastery.TargetAttribute = Stats.CriticalDamageChance.GetPersistent(gameConfiguration);
            wizardryMastery.Aggregation = AggregateType.AddRaw;
            wizardryMastery.ReplacedSkill = this.GetSkill(gameConfiguration, SkillNumber.ExpansionofWizStreng);
        }
    }

    private void FixMasterSkillChains(IContext context, GameConfiguration gameConfiguration)
    {
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.TwistingSlashMastery, SkillNumber.TwistingSlashStreng);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.RagefulBlowMastery, SkillNumber.RagefulBlowStreng);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.SwellLifeProficiency, SkillNumber.SwellLifeStrengt);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.ExpansionofWizMas, SkillNumber.ExpansionofWizStreng);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.TripleShotMastery, SkillNumber.TripleShotStrengthener);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.AttackIncreaseMastery, SkillNumber.AttackIncreaseStr);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.DefenseIncreaseMastery, SkillNumber.DefenseIncreaseStr);
        this.SetReplacedSkill(context, gameConfiguration, SkillNumber.DefSuccessRateIncMastery, SkillNumber.DefSuccessRateIncPowUp);
        this.EnsureDrainLifeMastery(context, gameConfiguration);
    }

    private void SetReplacedSkill(IContext context, GameConfiguration gameConfiguration, SkillNumber skillNumber, SkillNumber replacedSkillNumber)
    {
        var skill = this.GetSkill(gameConfiguration, skillNumber);
        var replacedSkill = this.GetSkill(gameConfiguration, replacedSkillNumber);
        if (skill?.MasterDefinition is null || replacedSkill is null)
        {
            return;
        }

        skill.MasterDefinition.ReplacedSkill = replacedSkill;
        this.CopySkillBehavior(gameConfiguration, context, skillNumber, replacedSkillNumber);
    }

    private void EnsureDrainLifeMastery(IContext context, GameConfiguration gameConfiguration)
    {
        var skill = this.GetSkill(gameConfiguration, SkillNumber.DrainLifeMastery);
        var requiredSkill = this.GetSkill(gameConfiguration, SkillNumber.DrainLifeStrengthener);
        if (skill is null || requiredSkill is null)
        {
            return;
        }

        skill.MasterDefinition ??= context.CreateNew<MasterSkillDefinition>();
        skill.MasterDefinition.Rank = 6;
        skill.MasterDefinition.Root = requiredSkill.MasterDefinition?.Root;
        skill.MasterDefinition.MaximumLevel = 20;
        skill.MasterDefinition.MinimumLevel = 1;
        skill.MasterDefinition.ValueFormula = SkillsInitializer.Formula502;
        skill.MasterDefinition.DisplayValueFormula = SkillsInitializer.Formula502;
        skill.MasterDefinition.TargetAttribute = null;
        skill.MasterDefinition.Aggregation = AggregateType.AddRaw;
        skill.MasterDefinition.ReplacedSkill = requiredSkill;
        skill.MasterDefinition.ExtendsDuration = false;
        skill.MasterDefinition.RequiredMasterSkills.Clear();
        skill.MasterDefinition.RequiredMasterSkills.Add(requiredSkill);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.DrainLifeMastery, SkillNumber.DrainLifeStrengthener);
    }

    private void FixSkillBehaviorCopies(IContext context, GameConfiguration gameConfiguration)
    {
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.InfernoStrengthener, SkillNumber.Inferno);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.InfernoStrengthenerDuelMaster, SkillNumber.Inferno);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.ExpansionofWizStreng, SkillNumber.ExpansionofWizardry);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.ExpansionofWizMas, SkillNumber.ExpansionofWizStreng);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.AttackIncreaseStr, SkillNumber.GreaterDamage);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.AttackIncreaseMastery, SkillNumber.AttackIncreaseStr);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.DefenseIncreaseStr, SkillNumber.GreaterDefense);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.DefenseIncreaseMastery, SkillNumber.DefenseIncreaseStr);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.SwellLifeStrengt, SkillNumber.SwellLife);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.SwellLifeProficiency, SkillNumber.SwellLifeStrengt);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.DefSuccessRateIncPowUp, SkillNumber.IncreaseBlock);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.DefSuccessRateIncMastery, SkillNumber.DefSuccessRateIncPowUp);
        this.CopySkillBehavior(gameConfiguration, context, SkillNumber.StaminaIncreaseStrengthener, SkillNumber.IncreaseHealth);
    }

    private Skill? GetSkill(GameConfiguration gameConfiguration, SkillNumber skillNumber)
    {
        return gameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)skillNumber);
    }

    private void CopySkillBehavior(GameConfiguration gameConfiguration, IContext context, SkillNumber skillNumber, SkillNumber sourceSkillNumber)
    {
        var skill = this.GetSkill(gameConfiguration, skillNumber);
        var sourceSkill = this.GetSkill(gameConfiguration, sourceSkillNumber);
        if (skill is null || sourceSkill is null)
        {
            return;
        }

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
