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
    protected override void InitializeGoldenInvasionMobs()
    {
        base.InitializeGoldenInvasionMobs();

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
                { Stats.Level, 20 },
                { Stats.MaximumHealth, 3200 },
                { Stats.MinimumPhysBaseDmg, 125 },
                { Stats.MaximumPhysBaseDmg, 130 },
                { Stats.DefenseBase, 50 },
                { Stats.AttackRatePvm, 100 },
                { Stats.DefenseRatePvm, 50 },
                { Stats.PoisonResistance, 2f / 255 },
                { Stats.IceResistance, 2f / 255 },
                { Stats.WaterResistance, 2f / 255 },
                { Stats.FireResistance, 2f / 255 },
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
                { Stats.MaximumHealth, 80000000 },
                { Stats.MinimumPhysBaseDmg, 650 },
                { Stats.MaximumPhysBaseDmg, 850 },
                { Stats.DefenseBase, 12000 },
                { Stats.AttackRatePvm, 3500 },
                { Stats.DefenseRatePvm, 4000 },
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
                { Stats.Level, 61 },
                { Stats.MaximumHealth, 10000 },
                { Stats.MinimumPhysBaseDmg, 190 },
                { Stats.MaximumPhysBaseDmg, 200 },
                { Stats.DefenseBase, 110 },
                { Stats.AttackRatePvm, 310 },
                { Stats.DefenseRatePvm, 90 },
                { Stats.WaterResistance, 3f / 255 },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);
            monster.SetGuid(monster.Number);
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
                { Stats.MaximumHealth, 110000000 },
                { Stats.MinimumPhysBaseDmg, 750 },
                { Stats.MaximumPhysBaseDmg, 950 },
                { Stats.DefenseBase, 15000 },
                { Stats.AttackRatePvm, 4000 },
                { Stats.DefenseRatePvm, 5000 },
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
                { Stats.MaximumHealth, 70000000 },
                { Stats.MinimumPhysBaseDmg, 600 },
                { Stats.MaximumPhysBaseDmg, 800 },
                { Stats.DefenseBase, 10000 },
                { Stats.AttackRatePvm, 3200 },
                { Stats.DefenseRatePvm, 3500 },
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
                { Stats.MaximumHealth, 160000000 },
                { Stats.MinimumPhysBaseDmg, 900 },
                { Stats.MaximumPhysBaseDmg, 1200 },
                { Stats.DefenseBase, 18000 },
                { Stats.AttackRatePvm, 4500 },
                { Stats.DefenseRatePvm, 6000 },
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
            monster.NumberOfMaximumItemDrops = 1;
            var attributes = new Dictionary<AttributeDefinition, float>
            {
                { Stats.Level, 47 },
                { Stats.MaximumHealth, 15000 },
                { Stats.MinimumPhysBaseDmg, 190 },
                { Stats.MaximumPhysBaseDmg, 210 },
                { Stats.DefenseBase, 120 },
                { Stats.AttackRatePvm, 400 },
                { Stats.DefenseRatePvm, 88 },
                { Stats.PoisonResistance, 9f / 255 },
                { Stats.IceResistance, 7f / 255 },
                { Stats.WaterResistance, 9f / 255 },
                { Stats.FireResistance, 9f / 255 },
            };
            monster.AddAttributes(attributes, this.Context, this.GameConfiguration);

            var itemDrop = this.Context.CreateNew<DropItemGroup>();

            itemDrop.Chance = 1;
            itemDrop.Description = "Items from red dragon";
            itemDrop.Monster = monster;
            itemDrop.PossibleItems.Add(this.GameConfiguration.Items.First(item => item.Group == 14 && item.Number == 13)); // Jewel of Bless
            itemDrop.PossibleItems.Add(this.GameConfiguration.Items.First(item => item.Group == 14 && item.Number == 14)); // Jewel of Soul
            itemDrop.PossibleItems.Add(this.GameConfiguration.Items.First(item => item.Group == 12 && item.Number == 15)); // Jewel of Chaos
            monster.DropItemGroups.Add(itemDrop);
            this.GameConfiguration.DropItemGroups.Add(itemDrop);
        }
    }
}
