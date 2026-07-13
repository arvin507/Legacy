using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class ProfessionSystem
{
  private readonly IReadOnlyDictionary<string, ProfessionDefinition> _definitions;

  public ProfessionSystem(IReadOnlyList<ProfessionDefinition>? definitions = null)
  {
    definitions ??= CreateDefaultDefinitions();
    _definitions = definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
  }

  public ProfessionDefinition Current(CharacterState character) =>
    _definitions.GetValueOrDefault(character.ProfessionId) ?? _definitions["commoner"];

  public string DescribeGoal(CharacterState character)
  {
    var current = Current(character);
    if (current.Id == "commoner")
    {
      return "生涯目标：务农可入帮农，读书可入书生";
    }

    if (current.NextProfessionId is null ||
        !_definitions.TryGetValue(current.NextProfessionId, out var next))
    {
      return $"{current.Description} · 已达此路最高身份";
    }

    var requirements = new List<string>();
    AddProgress(requirements, "农事", character.FarmingSkill, next.RequiredFarmingSkill);
    AddProgress(requirements, "学识", character.ScholarshipSkill, next.RequiredScholarshipSkill);
    AddProgress(requirements, "文化", character.Culture, next.RequiredCulture);
    AddProgress(
      requirements,
      "金钱",
      character.Money,
      Math.Max(next.RequiredMoney, next.PromotionCost),
      "文");
    return $"晋升{next.Name}：{string.Join(" · ", requirements)}";
  }

  public void Initialize(CharacterState character)
  {
    SetProfession(character, _definitions["commoner"]);
  }

  public ProfessionResult HandleAction(CharacterState character, ActionDefinition action)
  {
    if (character.ProfessionId != "commoner")
    {
      return ProfessionResult.None;
    }

    var targetId = action.SkillType switch
    {
      SkillType.Farming => "farmhand",
      SkillType.Scholarship => "scholar",
      _ => string.Empty
    };
    if (string.IsNullOrEmpty(targetId) || !_definitions.TryGetValue(targetId, out var target))
    {
      return ProfessionResult.None;
    }

    SetProfession(character, target);
    return new ProfessionResult(true, $"你选择以{target.Name}为业，人生道路自此展开。", 0, 0);
  }

  public ProfessionResult ApplyMonthlyBenefits(CharacterState character)
  {
    var profession = Current(character);
    if (profession.MonthlyMoney == 0 && profession.MonthlyFood == 0)
    {
      return ProfessionResult.None;
    }

    character.Money += profession.MonthlyMoney;
    character.Food += profession.MonthlyFood;
    var rewards = new List<string>();
    if (profession.MonthlyMoney > 0)
    {
      rewards.Add($"{profession.MonthlyMoney} 文");
    }

    if (profession.MonthlyFood > 0)
    {
      rewards.Add($"{profession.MonthlyFood} 份粮食");
    }

    return new ProfessionResult(
      false,
      $"{profession.Name}本月带来 {string.Join("与", rewards)}。",
      profession.MonthlyMoney,
      profession.MonthlyFood);
  }

  public ProfessionResult TryPromote(CharacterState character)
  {
    var current = Current(character);
    if (current.NextProfessionId is null ||
        !_definitions.TryGetValue(current.NextProfessionId, out var next) ||
        !MeetsRequirements(character, next))
    {
      return ProfessionResult.None;
    }

    character.Money -= next.PromotionCost;
    SetProfession(character, next);
    character.Reputation += next.Tier;
    return new ProfessionResult(
      true,
      $"你已晋为{next.Name}，为此花费 {next.PromotionCost} 文。",
      -next.PromotionCost,
      0);
  }

  private static bool MeetsRequirements(CharacterState character, ProfessionDefinition definition) =>
    character.FarmingSkill >= definition.RequiredFarmingSkill &&
    character.ScholarshipSkill >= definition.RequiredScholarshipSkill &&
    character.Culture >= definition.RequiredCulture &&
    character.Money >= Math.Max(definition.RequiredMoney, definition.PromotionCost);

  private static void SetProfession(CharacterState character, ProfessionDefinition definition)
  {
    character.ProfessionId = definition.Id;
    character.ProfessionName = definition.Name;
  }

  private static void AddProgress(
    ICollection<string> requirements,
    string label,
    int current,
    int required,
    string suffix = "")
  {
    if (required > 0)
    {
      requirements.Add($"{label} {Math.Min(current, required)}/{required}{suffix}");
    }
  }

  private static IReadOnlyList<ProfessionDefinition> CreateDefaultDefinitions() =>
  [
    new ProfessionDefinition
    {
      Id = "commoner", Name = "平民", Description = "尚未择业", Track = "none", Tier = 0
    },
    new ProfessionDefinition
    {
      Id = "farmhand", Name = "帮农", Description = "以农事为业", Track = "farming", Tier = 1,
      MonthlyMoney = 3, MonthlyFood = 1, RequiredFarmingSkill = 1, NextProfessionId = "tenant_farmer"
    },
    new ProfessionDefinition
    {
      Id = "tenant_farmer", Name = "佃农", Description = "租种田地", Track = "farming", Tier = 2,
      MonthlyMoney = 8, MonthlyFood = 2, RequiredFarmingSkill = 12, RequiredMoney = 150,
      PromotionCost = 80, NextProfessionId = "freeholder"
    },
    new ProfessionDefinition
    {
      Id = "freeholder", Name = "自耕农", Description = "置办薄田", Track = "farming", Tier = 3,
      MonthlyMoney = 20, MonthlyFood = 3, RequiredFarmingSkill = 30, RequiredMoney = 500,
      PromotionCost = 300, NextProfessionId = "village_gentry"
    },
    new ProfessionDefinition
    {
      Id = "village_gentry", Name = "乡绅", Description = "田产丰足", Track = "farming", Tier = 4,
      MonthlyMoney = 60, MonthlyFood = 4, RequiredFarmingSkill = 60, RequiredCulture = 10,
      RequiredMoney = 1500, PromotionCost = 1000
    },
    new ProfessionDefinition
    {
      Id = "scholar", Name = "书生", Description = "立志读书", Track = "scholarship", Tier = 1,
      RequiredScholarshipSkill = 1, RequiredCulture = 1, NextProfessionId = "student"
    },
    new ProfessionDefinition
    {
      Id = "student", Name = "童生", Description = "准备科考", Track = "scholarship", Tier = 2,
      MonthlyMoney = 2, RequiredScholarshipSkill = 12, RequiredCulture = 12, RequiredMoney = 80,
      PromotionCost = 30, NextProfessionId = "xiucai"
    },
    new ProfessionDefinition
    {
      Id = "xiucai", Name = "秀才", Description = "得入士林", Track = "scholarship", Tier = 3,
      MonthlyMoney = 20, RequiredScholarshipSkill = 35, RequiredCulture = 35, RequiredMoney = 250,
      PromotionCost = 100, NextProfessionId = "juren"
    },
    new ProfessionDefinition
    {
      Id = "juren", Name = "举人", Description = "乡试得中", Track = "scholarship", Tier = 4,
      MonthlyMoney = 60, RequiredScholarshipSkill = 80, RequiredCulture = 80, RequiredMoney = 800,
      PromotionCost = 300
    }
  ];
}
