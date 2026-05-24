// <copyright file="InvasionMobsInitialization.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// The initialization of all monsters.
/// </summary>
internal class InvasionMobsInitialization : Version095d.InvasionMobsInitialization
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvasionMobsInitialization" /> class.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public InvasionMobsInitialization(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.InitializeGoldenInvasionMobs();
        this.InitializeRedDragonInvasionMobs();
    }

    /// <inheritdoc />
    protected override void InitializeGoldenInvasionMobs()
    {
        base.InitializeGoldenInvasionMobs();

        this.ApplySeasonSixGoldenInvasionBalance();

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 78;
            monster.Designation = "Golden Goblin";
            monster.MoveRange = 3;
            monster.AttackRange = 1;
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1400 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 45 },
                { Stats.MaximumHealth, 8000000 },
                { Stats.MinimumPhysBaseDmg, 1800 },
                { Stats.MaximumPhysBaseDmg, 2600 },
                { Stats.DefenseBase, 900 },
                { Stats.AttackRatePvm, 4500 },
                { Stats.DefenseRatePvm, 450 },
                { Stats.PoisonResistance, 0.25f },
                { Stats.IceResistance, 0.25f },
                { Stats.WaterResistance, 0.25f },
                { Stats.FireResistance, 0.25f },
                { Stats.LightningResistance, 0.25f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            this.AddBoxOfKundunToMonster(1, monster);
        }

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 79;
            monster.Designation = "Golden Dragon";
            monster.MoveRange = 3;
            monster.AttackRange = 2;
            monster.AttackSkill = this.GameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.MonsterSkill);
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1800 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 80 },
                { Stats.MaximumHealth, 40000000 },
                { Stats.MinimumPhysBaseDmg, 3500 },
                { Stats.MaximumPhysBaseDmg, 5000 },
                { Stats.DefenseBase, 2500 },
                { Stats.AttackRatePvm, 8000 },
                { Stats.DefenseRatePvm, 1200 },
                { Stats.PoisonResistance, 0.45f },
                { Stats.IceResistance, 0.45f },
                { Stats.WaterResistance, 0.45f },
                { Stats.FireResistance, 0.45f },
                { Stats.LightningResistance, 0.45f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            this.AddBoxOfKundunToMonster(3, monster);
        }

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 81;
            monster.Designation = "Golden Vepar";
            monster.MoveRange = 3;
            monster.AttackRange = 4;
            monster.AttackSkill = this.GameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.EnergyBall);
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1600 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 65 },
                { Stats.MaximumHealth, 22000000 },
                { Stats.MinimumPhysBaseDmg, 3000 },
                { Stats.MaximumPhysBaseDmg, 4200 },
                { Stats.DefenseBase, 1700 },
                { Stats.AttackRatePvm, 7000 },
                { Stats.DefenseRatePvm, 900 },
                { Stats.PoisonResistance, 0.35f },
                { Stats.IceResistance, 0.35f },
                { Stats.WaterResistance, 0.35f },
                { Stats.FireResistance, 0.35f },
                { Stats.LightningResistance, 0.35f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);
            monster.SetGuid(monster.Number);

            this.AddBoxOfKundunToMonster(2, monster);
        }

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 80;
            monster.Designation = "Golden Lizard King";
            monster.MoveRange = 3;
            monster.AttackRange = 3;
            monster.AttackSkill = this.GameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.Lightning);
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1600 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 83 },
                { Stats.MaximumHealth, 50000000 },
                { Stats.MinimumPhysBaseDmg, 4000 },
                { Stats.MaximumPhysBaseDmg, 5500 },
                { Stats.DefenseBase, 3000 },
                { Stats.AttackRatePvm, 9000 },
                { Stats.DefenseRatePvm, 1500 },
                { Stats.PoisonResistance, 0.50f },
                { Stats.IceResistance, 0.50f },
                { Stats.WaterResistance, 0.50f },
                { Stats.FireResistance, 0.50f },
                { Stats.LightningResistance, 0.50f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            this.AddBoxOfKundunToMonster(4, monster);
        }

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 83;
            monster.Designation = "Golden Wheel";
            monster.MoveRange = 3;
            monster.AttackRange = 4;
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1400 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 77 },
                { Stats.MaximumHealth, 35000000 },
                { Stats.MinimumPhysBaseDmg, 3200 },
                { Stats.MaximumPhysBaseDmg, 4600 },
                { Stats.DefenseBase, 2200 },
                { Stats.AttackRatePvm, 7500 },
                { Stats.DefenseRatePvm, 1000 },
                { Stats.PoisonResistance, 0.40f },
                { Stats.IceResistance, 0.40f },
                { Stats.WaterResistance, 0.40f },
                { Stats.FireResistance, 0.40f },
                { Stats.LightningResistance, 0.40f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);
            monster.SetGuid(monster.Number);
        }

        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 82;
            monster.Designation = "Golden Tantallos";
            monster.MoveRange = 3;
            monster.AttackRange = 2;
            monster.AttackSkill = this.GameConfiguration.Skills.FirstOrDefault(s => s.Number == (short)SkillNumber.MonsterSkill);
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1400 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(600 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 90 },
                { Stats.MaximumHealth, 70000000 },
                { Stats.MinimumPhysBaseDmg, 5500 },
                { Stats.MaximumPhysBaseDmg, 7500 },
                { Stats.DefenseBase, 3500 },
                { Stats.AttackRatePvm, 12000 },
                { Stats.DefenseRatePvm, 1800 },
                { Stats.PoisonResistance, 0.55f },
                { Stats.IceResistance, 0.55f },
                { Stats.WaterResistance, 0.55f },
                { Stats.FireResistance, 0.55f },
                { Stats.LightningResistance, 0.55f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            this.AddBoxOfKundunToMonster(5, monster);
        }
    }

    private void InitializeRedDragonInvasionMobs()
    {
        {
            var monster = this.Context.CreateNew<MonsterDefinition>();
            this.GameConfiguration.Monsters.Add(monster);
            monster.Number = 44;
            monster.Designation = "Red Dragon";
            monster.MoveRange = 3;
            monster.AttackRange = 2;
            monster.ViewRange = 7;
            monster.MoveDelay = new TimeSpan(400 * TimeSpan.TicksPerMillisecond);
            monster.AttackDelay = new TimeSpan(1800 * TimeSpan.TicksPerMillisecond);
            monster.RespawnDelay = new TimeSpan(100 * TimeSpan.TicksPerSecond);
            monster.Attribute = 2;
            monster.NumberOfMaximumItemDrops = 2;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 140 },
                { Stats.MaximumHealth, 250000000 },
                { Stats.MinimumPhysBaseDmg, 8000 },
                { Stats.MaximumPhysBaseDmg, 11000 },
                { Stats.DefenseBase, 6000 },
                { Stats.AttackRatePvm, 17000 },
                { Stats.DefenseRatePvm, 3200 },
                { Stats.PoisonResistance, 0.70f },
                { Stats.IceResistance, 0.65f },
                { Stats.WaterResistance, 0.65f },
                { Stats.FireResistance, 0.65f },
                { Stats.LightningResistance, 0.65f },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            var itemDrop = this.Context.CreateNew<DropItemGroup>();

            itemDrop.Chance = 1;
            itemDrop.Description = "Blue Chocolate Box - Ancient Sets";
            itemDrop.Monster = monster;
            itemDrop.PossibleItems.Add(this.GameConfiguration.Items.First(item => item.Group == 14 && item.Number == 34)); // Blue Chocolate Box
            monster.DropItemGroups.Add(itemDrop);
            this.GameConfiguration.DropItemGroups.Add(itemDrop);
        }
    }

    private void ApplySeasonSixGoldenInvasionBalance()
    {
        this.SetMonsterAttributes(
            43,
            new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 35 },
                { Stats.MaximumHealth, 5000000 },
                { Stats.MinimumPhysBaseDmg, 1400 },
                { Stats.MaximumPhysBaseDmg, 2000 },
                { Stats.DefenseBase, 600 },
                { Stats.AttackRatePvm, 3500 },
                { Stats.DefenseRatePvm, 300 },
                { Stats.PoisonResistance, 0.20f },
                { Stats.IceResistance, 0.20f },
                { Stats.WaterResistance, 0.20f },
                { Stats.FireResistance, 0.20f },
                { Stats.LightningResistance, 0.20f },
            });

        this.SetMonsterAttributes(
            54,
            new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 50 },
                { Stats.MaximumHealth, 10000000 },
                { Stats.MinimumPhysBaseDmg, 2000 },
                { Stats.MaximumPhysBaseDmg, 3000 },
                { Stats.DefenseBase, 1000 },
                { Stats.AttackRatePvm, 5000 },
                { Stats.DefenseRatePvm, 550 },
                { Stats.PoisonResistance, 0.28f },
                { Stats.IceResistance, 0.28f },
                { Stats.WaterResistance, 0.28f },
                { Stats.FireResistance, 0.28f },
                { Stats.LightningResistance, 0.28f },
            });

        this.SetMonsterAttributes(
            53,
            new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 60 },
                { Stats.MaximumHealth, 18000000 },
                { Stats.MinimumPhysBaseDmg, 2700 },
                { Stats.MaximumPhysBaseDmg, 3800 },
                { Stats.DefenseBase, 1500 },
                { Stats.AttackRatePvm, 6500 },
                { Stats.DefenseRatePvm, 800 },
                { Stats.PoisonResistance, 0.32f },
                { Stats.IceResistance, 0.32f },
                { Stats.WaterResistance, 0.32f },
                { Stats.FireResistance, 0.32f },
                { Stats.LightningResistance, 0.32f },
            });
    }

    private void SetMonsterAttributes(short monsterNumber, IDictionary<AttributeDefinition, float> attributesWithValues)
    {
        var monster = this.GameConfiguration.Monsters.First(m => m.Number == monsterNumber);
        foreach (var attributeWithValue in attributesWithValues)
        {
            var attributeDefinition = attributeWithValue.Key.GetPersistent(this.GameConfiguration);
            var attribute = monster.Attributes.FirstOrDefault(a => a.AttributeDefinition?.Id == attributeDefinition.Id);
            if (attribute is null)
            {
                attribute = this.Context.CreateNew<MonsterAttribute>();
                attribute.AttributeDefinition = attributeDefinition;
                monster.Attributes.Add(attribute);
                attribute.SetGuid(monster.Number, attributeDefinition.Id.ExtractFirstTwoBytes());
            }

            attribute.Value = attributeWithValue.Value;
        }
    }
}
