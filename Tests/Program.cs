using System.Text.Json;
using AncientLife.Managers;
using AncientLife.Models;
using AncientLife.Models.Calendar;
using AncientLife.Models.Events;
using AncientLife.Systems;
using AncientLife.Systems.Calendar;
using AncientLife.Systems.Events;

var checks = new (string Name, Action Run)[]
{
    ("初始状态", CheckInitialState),
    ("行动消耗与禁用", CheckActionCosts),
    ("休息每月限制", CheckRestLimit),
    ("捕鱼随机范围", CheckFishingRange),
    ("每月结算", CheckMonthlySettlement),
    ("事件前月结阶段", CheckDeferredMonthCompletion),
    ("历法与年龄", CheckCalendar),
    ("架空年号与改元", CheckEraManager),
    ("死亡与重新开始", CheckDeathAndRestart),
    ("职业路线与晋升", CheckProfessionProgression),
    ("完整职业成长", CheckCompleteProfessionTracks),
    ("自然寿命终点", CheckNaturalLifetime),
    ("衰老规则", CheckAging),
    ("人生履历与结局", CheckLifeRecordsAndEnding),
    ("多类人生结局", CheckEndingTypes),
    ("事件冷却与唯一性", CheckEventCooldown),
    ("动作配置完整", CheckActionConfig),
    ("职业配置完整", CheckProfessionConfig),
    ("事件配置完整", CheckEventConfig),
    ("历法配置完整", CheckCalendarConfig),
    ("事件基础条件", CheckEventConditions),
    ("事件加权随机", CheckEventWeights),
    ("事件奖励执行", CheckEventRewards),
    ("连续事件流程", CheckEventChain)
};

foreach (var check in checks)
{
    check.Run();
    Console.WriteLine($"PASS  {check.Name}");
}

Console.WriteLine($"All {checks.Length} core checks passed.");

static void CheckInitialState()
{
    var session = CreateSession();
    Equal("李二", session.Character.Name, "name");
    Equal(16, session.Character.Age, "age");
    Equal(10, session.Character.Energy, "energy");
    Equal(100, session.Character.Health, "health");
    Equal(100, session.Character.Money, "money");
    Equal(10, session.Character.Food, "food");
    Equal(0, session.Character.Culture, "culture");
    Equal(Season.Spring, session.Calendar.Season, "season");
    Equal(1, session.Calendar.Month, "month");
    Equal("正月", session.Calendar.MonthName, "month name");
    True(session.Calendar.EraYearName.EndsWith("元年", StringComparison.Ordinal), "first era year");
    True(!string.IsNullOrWhiteSpace(session.Calendar.MonthlyOrder), "monthly order");
}

static void CheckActionCosts()
{
    var session = CreateSession();
    var result = session.PerformAction("farm");
    True(result.Success, "farm should succeed");
    Equal(8, session.Character.Energy, "farm energy");
    Equal(115, session.Character.Money, "farm money");

    session.PerformAction("woodcut");
    session.PerformAction("woodcut");
    Equal(2, session.Character.Energy, "remaining energy");
    True(!session.GetActionAvailability("woodcut").CanPerform, "woodcut should disable");
    True(session.GetActionAvailability("study").CanPerform, "study should remain available");
}

static void CheckRestLimit()
{
    var session = CreateSession();
    True(!session.GetActionAvailability("rest").CanPerform, "rest is disabled at full energy");
    session.PerformAction("farm");
    True(session.PerformAction("rest").Success, "first rest should succeed");
    Equal(10, session.Character.Energy, "rest recovery");
    True(!session.PerformAction("rest").Success, "second rest should fail");
}

static void CheckFishingRange()
{
    var fish = Definitions().Single(action => action.Id == "fish");
    var system = new ActionSystem(new Random(2710));
    var character = CharacterState.CreateDefault();
    var usage = new Dictionary<string, int>();

    for (var index = 0; index < 200; index++)
    {
        character.Energy = character.MaxEnergy;
        character.FarmingSkill = 0;
        usage.Clear();
        var result = system.Perform(fish, character, usage, false);
        True(result.RewardAmount is >= 0 and <= 40, "fish reward out of range");
    }
}

static void CheckMonthlySettlement()
{
    var session = CreateSession();
    session.PerformAction("woodcut");
    session.EndMonth();
    Equal(10, session.Character.Food, "profession food offsets consumption");
    Equal(123, session.Character.Money, "profession income and living cost");
    Equal(10, session.Character.Energy, "monthly energy restore");
    Equal(2, session.Calendar.Month, "next month");
    Equal(1, session.Calendar.TotalMonthsSurvived, "survived months");
}

static void CheckDeferredMonthCompletion()
{
    var session = CreateSession();
    session.PerformAction("farm");

    True(session.BeginEndMonth(), "month ending should begin");
    Equal(1, session.Calendar.Month, "month must wait for event resolution");
    Equal(10, session.Character.Food, "settlement occurs before event");
    True(session.IsEndingMonth, "month should remain pending");
    True(!session.GetActionAvailability("farm").CanPerform, "actions disabled while event is pending");
    True(!session.BeginEndMonth(), "month cannot begin twice");

    True(session.CompleteEndMonth(), "month should complete after event");
    Equal(2, session.Calendar.Month, "month advances after event");
    Equal(10, session.Character.Energy, "energy restores after event");
    True(!session.IsEndingMonth, "pending state should clear");
}

static void CheckCalendar()
{
    var character = CharacterState.CreateDefault();
    var time = CreateTimeSystem();
    var calendar = time.CreateCalendar();
    var firstMonthlyOrder = calendar.MonthlyOrder;

    time.CompleteCurrentMonth(calendar, character);

    Equal(2, calendar.Month, "second month transition");
    Equal("二月", calendar.MonthName, "second month name");
    Equal(Season.Spring, calendar.Season, "second month season");
    True(!string.Equals(firstMonthlyOrder, calendar.MonthlyOrder, StringComparison.Ordinal), "monthly order changes with month");

    for (var month = 1; month < 3; month++)
    {
        time.CompleteCurrentMonth(calendar, character);
    }

    Equal(4, calendar.Month, "summer month transition");
    Equal("四月", calendar.MonthName, "summer month name");
    Equal(Season.Summer, calendar.Season, "summer transition");

    for (var month = 3; month < 12; month++)
    {
        time.CompleteCurrentMonth(calendar, character);
    }

    Equal(2, calendar.Year, "year transition");
    Equal(1, calendar.Month, "new year month");
    Equal(Season.Spring, calendar.Season, "new year season");
    Equal(17, character.Age, "birthday");
    True(calendar.EraYearName.EndsWith("二年", StringComparison.Ordinal), "second era year display");
}

static void CheckEraManager()
{
    var manager = new EraManager(
        new EraConfig { Eras = ["承平", "永安", "弘德"] },
        new Random(626));
    var calendar = CalendarState.CreateDefault();
    manager.BeginNewEra(calendar, "承平");
    Equal("承平元年", calendar.EraYearName, "era first year");

    calendar.Year = 18;
    manager.RefreshDisplayYear(calendar);
    Equal("承平十八年", calendar.EraYearName, "era eighteenth year");

    manager.ChangeEra(calendar, "永安");
    Equal("永安元年", calendar.EraYearName, "era change");
    Equal("元", AncientYearFormatter.FormatEraYear(1), "first year numeral");
    Equal("二十一", AncientYearFormatter.FormatEraYear(21), "ancient numeral");
}

static void CheckDeathAndRestart()
{
    var session = CreateSession();
    var firstEra = session.Calendar.EraName;
    session.Character.Food = 0;
    session.Character.Money = 0;
    session.Character.Health = 10;
    session.EndMonth();

    True(session.IsGameOver, "game should be over");
    Equal(0, session.Character.Health, "health floor");
    Equal(1, session.Summary?.MonthsSurvived ?? -1, "death month count");
    True(!session.GetActionAvailability("farm").CanPerform, "actions disabled after death");

    session.Restart();
    True(!session.IsGameOver, "restart clears game over");
    Equal(100, session.Character.Health, "restart health");
    Equal(1, session.Calendar.Month, "restart month");
    True(!string.Equals(firstEra, session.Calendar.EraName, StringComparison.Ordinal), "restart chooses another configured era");
}


static void CheckProfessionProgression()
{
    var session = CreateSession();
    var result = session.PerformAction("farm");
    True(result.Success, "farm action succeeds");
    Equal("farmhand", session.Character.ProfessionId, "farm action chooses farming route");
    Equal(2, session.Character.FarmingSkill, "farming skill gain");
    True(session.LifeRecords.Any(record => record.Category == "职业"), "profession record");

    session.Character.FarmingSkill = 12;
    session.Character.Money = 200;
    session.EndMonth();
    Equal("tenant_farmer", session.Character.ProfessionId, "farming promotion");
    Equal("佃农", session.Character.ProfessionName, "promoted profession name");
    True(session.LifeRecords.Any(record => record.Category == "晋升"), "promotion record");

    var scholarSession = CreateSession();
    scholarSession.PerformAction("study");
    Equal("scholar", scholarSession.Character.ProfessionId, "study chooses scholarship route");
    Equal(2, scholarSession.Character.ScholarshipSkill, "scholarship skill gain");
    True(scholarSession.ProfessionGoal.Contains("晋升童生", StringComparison.Ordinal), "scholar promotion goal");
}

static void CheckCompleteProfessionTracks()
{
    var farmer = CreateSession();
    farmer.PerformAction("farm");
    PromoteWithRequiredStats(farmer, 12, 0, 0, 200);
    PromoteWithRequiredStats(farmer, 30, 0, 0, 500);
    PromoteWithRequiredStats(farmer, 60, 0, 10, 1500);
    Equal("village_gentry", farmer.Character.ProfessionId, "complete farming track");
    True(farmer.ProfessionGoal.Contains("最高身份", StringComparison.Ordinal), "terminal farming goal");

    var scholar = CreateSession();
    scholar.PerformAction("study");
    PromoteWithRequiredStats(scholar, 0, 12, 12, 80);
    PromoteWithRequiredStats(scholar, 0, 35, 35, 250);
    PromoteWithRequiredStats(scholar, 0, 80, 80, 800);
    Equal("juren", scholar.Character.ProfessionId, "complete scholarship track");
    Equal(3, scholar.LifeRecords.Count(record => record.Category == "晋升"), "scholar promotion records");
}

static void CheckNaturalLifetime()
{
    var session = CreateSession();
    const int maximumExpectedMonths = 12 * 100;

    while (!session.IsGameOver && session.Calendar.TotalMonthsSurvived < maximumExpectedMonths)
    {
        while (session.GetActionAvailability("farm").CanPerform)
        {
            session.PerformAction("farm");
        }

        session.EndMonth();
    }

    True(session.IsGameOver, "repeating one action must still end naturally");
    True(session.Character.Age >= 60, "natural death should follow old age");
    True(session.Calendar.TotalMonthsSurvived < maximumExpectedMonths, "lifetime must be bounded");
    Equal("自耕农", session.Summary?.ProfessionName, "single-action route cannot reach best ending");
    Equal("安稳终老", session.Summary?.EndingTitle, "prosperous natural ending");
}

static void CheckAging()
{
    var character = CharacterState.CreateDefault();
    character.Age = 60;
    var system = new AgingSystem();
    var monthly = system.ApplyMonthlyWear(character);
    Equal(-1, monthly.HealthChange, "age sixty monthly wear");
    Equal(99, character.Health, "monthly aging health");

    character.Age = 70;
    var birthday = system.ApplyBirthday(character);
    Equal(-4, birthday.MaxHealthChange, "age seventy health cap loss");
    Equal(96, character.MaxHealth, "reduced health cap");
    Equal(-1, birthday.MaxEnergyChange, "age seventy energy cap loss");
    Equal(9, character.MaxEnergy, "reduced energy cap");
}

static void CheckLifeRecordsAndEnding()
{
    var session = CreateSession();
    session.PerformAction("farm");
    session.Character.Food = 0;
    session.Character.Money = 0;
    session.Character.Health = 2;
    session.EndMonth();

    True(session.IsGameOver, "hardship should end the game");
    Equal("穷困离世", session.Summary?.EndingTitle, "poverty ending");
    Equal("帮农", session.Summary?.ProfessionName, "summary profession");
    True(session.Summary?.LifeRecords.Any(record => record.Category == "结局") == true, "ending life record");
}

static void CheckEndingTypes()
{
    var system = new EndingSystem();
    var successful = CharacterState.CreateDefault();
    successful.ProfessionId = "juren";
    Equal("功成名就", system.Evaluate(successful), "successful ending");

    var peaceful = CharacterState.CreateDefault();
    peaceful.Age = 65;
    peaceful.Money = 300;
    Equal("安稳终老", system.Evaluate(peaceful), "peaceful ending");

    var impoverished = CharacterState.CreateDefault();
    impoverished.Money = 0;
    impoverished.Food = 0;
    Equal("穷困离世", system.Evaluate(impoverished), "impoverished ending");
}

static void CheckEventCooldown()
{
    var uniqueEvent = new EventData
    {
        Id = "unique",
        Title = "unique",
        Description = "unique",
        Weight = 1,
        Unique = true,
        Choices = [Choice("one"), Choice("two")]
    };
    var manager = new EventManager(
        new EventConfig { TriggerChance = 1, Events = [uniqueEvent] },
        new Random(1),
        new WeightedRandom(new Random(1)));
    var character = CharacterState.CreateDefault();
    var calendar = CalendarState.CreateDefault();

    True(manager.TryStartRandomEvent(character, calendar, out _), "unique event first trigger");
    manager.ResolveChoice("one", character);
    True(!manager.TryStartRandomEvent(character, calendar, out _), "unique event cannot repeat");
}

static void CheckProfessionConfig()
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "Professions.json");
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    var professions = document.RootElement.EnumerateArray().ToArray();
    Equal(9, professions.Length, "configured profession count");
    True(professions.Any(profession => profession.GetProperty("id").GetString() == "village_gentry"), "farming ending profession");
    True(professions.Any(profession => profession.GetProperty("id").GetString() == "juren"), "scholar ending profession");
    True(professions.All(profession => profession.GetProperty("monthly_money").GetInt32() >= 0), "profession income values");
}

static void CheckActionConfig()
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "actions.json");
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    var ids = document.RootElement
        .EnumerateArray()
        .Select(element => element.GetProperty("id").GetString())
        .ToHashSet(StringComparer.Ordinal);

    Equal(5, ids.Count, "configured action count");
    True(document.RootElement.EnumerateArray().All(action => action.GetProperty("monthly_limit").GetInt32() > 0), "monthly action limits");
    True(document.RootElement.EnumerateArray().Any(action => action.GetProperty("skill_type").GetString() == "farming"), "farming action skill");
    True(document.RootElement.EnumerateArray().Any(action => action.GetProperty("skill_type").GetString() == "scholarship"), "scholarship action skill");
    foreach (var id in new[] { "farm", "woodcut", "study", "fish", "rest" })
    {
        True(ids.Contains(id), $"missing action config: {id}");
    }
}

static void CheckEventConfig()
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "Events.json");
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    var root = document.RootElement;
    Equal(0.3, root.GetProperty("trigger_chance").GetDouble(), "event trigger chance");

    var events = root.GetProperty("events").EnumerateArray().ToArray();
    Equal(10, events.Length, "configured event count");
    Equal(10, events.Select(gameEvent => gameEvent.GetProperty("id").GetString()).Distinct().Count(), "unique event ids");
    True(events.All(gameEvent => gameEvent.GetProperty("weight").GetInt32() > 0), "event weights must be positive");
    True(events.All(gameEvent => gameEvent.GetProperty("choices").GetArrayLength() is >= 2 and <= 4), "event choice counts");
    True(events.All(gameEvent => gameEvent.GetProperty("cooldown_months").GetInt32() >= 0), "event cooldown values");
    True(events.Any(gameEvent => gameEvent.GetProperty("unique").GetBoolean()), "unique event required");
    True(events.Any(gameEvent => gameEvent.GetProperty("choices").GetArrayLength() == 4), "four-choice event required");
}

static void CheckCalendarConfig()
{
    var eraPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "Eras.json");
    using var eraDocument = JsonDocument.Parse(File.ReadAllText(eraPath));
    var eras = eraDocument.RootElement.GetProperty("eras").EnumerateArray().ToArray();
    Equal(20, eras.Length, "configured era count");
    Equal(20, eras.Select(era => era.GetString()).Distinct().Count(), "unique eras");

    var calendarPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "Calendar.json");
    using var calendarDocument = JsonDocument.Parse(File.ReadAllText(calendarPath));
    var root = calendarDocument.RootElement;
    var months = root.GetProperty("months").EnumerateArray().ToArray();
    var expectedNames = new[] { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
    Equal(12, months.Length, "configured month count");
    True(months.Select(month => month.GetProperty("name").GetString()).SequenceEqual(expectedNames), "ancient month names");
    True(months.All(month => month.GetProperty("monthly_orders").GetArrayLength() > 0), "monthly orders required");
    var seasons = months.Select(month => month.GetProperty("season").GetString()).ToArray();
    True(seasons.Take(3).All(season => season == "春"), "spring months");
    True(seasons.Skip(3).Take(3).All(season => season == "夏"), "summer months");
    True(seasons.Skip(6).Take(3).All(season => season == "秋"), "autumn months");
    True(seasons.Skip(9).Take(3).All(season => season == "冬"), "winter months");
}

static void CheckEventConditions()
{
    var evaluator = new ConditionEvaluator();
    var character = CharacterState.CreateDefault();
    var calendar = CalendarState.CreateDefault();

    True(evaluator.IsMet(Condition("money", ">=", "100"), character, calendar), "money condition");
    True(!evaluator.IsMet(Condition("culture", ">=", "1"), character, calendar), "culture condition");
    True(evaluator.IsMet(Condition("age", "==", "16"), character, calendar), "age condition");
    True(evaluator.IsMet(Condition("month", "==", "1"), character, calendar), "month condition");
    True(evaluator.IsMet(Condition("profession", "==", "commoner"), character, calendar), "profession condition");
    character.FarmingSkill = 8;
    True(evaluator.IsMet(Condition("farming_skill", ">=", "8"), character, calendar), "farming skill condition");
}

static void CheckEventWeights()
{
    var common = Event("common", 100, Choice("close"));
    var rare = Event("rare", 1, Choice("close"));
    var random = new WeightedRandom(new Random(9182));
    var commonCount = 0;
    var rareCount = 0;

    for (var index = 0; index < 2000; index++)
    {
        var selected = random.Choose(new[] { common, rare }, gameEvent => gameEvent.Weight);
        if (selected?.Id == "common")
        {
            commonCount++;
        }
        else
        {
            rareCount++;
        }
    }

    True(commonCount > 1900, "weighted common event should dominate");
    True(rareCount > 0, "rare event should remain selectable");
}

static void CheckEventRewards()
{
    var character = CharacterState.CreateDefault();
    character.Health = 95;
    character.Energy = 1;
    var executor = new RewardExecutor();
    executor.Apply(
    [
        Reward("Money", 25),
        Reward("Health", 20),
        Reward("Culture", 3),
        Reward("Stamina", -5)
    ], character);

    Equal(125, character.Money, "event money reward");
    Equal(100, character.Health, "event health clamp");
    Equal(3, character.Culture, "event culture reward");
    Equal(0, character.Energy, "event stamina clamp");
}

static void CheckEventChain()
{
    var firstChoice = new EventChoice
    {
        Id = "continue",
        Text = "continue",
        Rewards = [Reward("Money", 10)],
        NextEventId = "second",
        CloseEvent = false
    };
    var secondChoice = Choice("finish", Reward("Culture", 2));
    var first = Event("first", 10, firstChoice);
    var second = Event("second", 1, secondChoice);
    var config = new EventConfig { TriggerChance = 1, Events = [first, second] };
    var manager = new EventManager(
        config,
        new Random(1),
        new WeightedRandom(new Random(1)));
    var character = CharacterState.CreateDefault();
    var calendar = CalendarState.CreateDefault();

    True(manager.TryStartRandomEvent(character, calendar, out var selected), "event should trigger");
    Equal("first", selected?.Id, "weighted first event");
    var firstResult = manager.ResolveChoice("continue", character);
    Equal("second", firstResult.NextEvent?.Id, "next event id");
    True(!firstResult.IsComplete, "chain should remain open");
    Equal(110, character.Money, "chain first reward");

    var secondResult = manager.ResolveChoice("finish", character);
    True(secondResult.IsComplete, "chain should complete");
    True(!manager.HasActiveEvent, "active event should clear");
    Equal(2, character.Culture, "chain second reward");
}

static GameSession CreateSession() =>
    new(
        Definitions(),
        new ActionSystem(new Random(42)),
        timeSystem: CreateTimeSystem());

static void PromoteWithRequiredStats(
    GameSession session,
    int farmingSkill,
    int scholarshipSkill,
    int culture,
    int money)
{
    session.Character.FarmingSkill = farmingSkill;
    session.Character.ScholarshipSkill = scholarshipSkill;
    session.Character.Culture = culture;
    session.Character.Money = money;
    session.EndMonth();
}

static TimeSystem CreateTimeSystem()
{
    var eraManager = new EraManager(
        new EraConfig { Eras = ["承平", "永安", "弘德"] },
        new Random(42));
    return new TimeSystem(CreateCalendarConfigData(), eraManager, new Random(42));
}

static CalendarConfig CreateCalendarConfigData()
{
    var names = new[] { "正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月" };
    var months = names.Select((name, index) => new MonthData
    {
        Name = name,
        Season = index switch
        {
            < 3 => Season.Spring,
            < 6 => Season.Summer,
            < 9 => Season.Autumn,
            _ => Season.Winter
        },
        MonthlyOrders = [$"{name}月令"]
    }).ToArray();
    return new CalendarConfig { Months = months };
}

static IReadOnlyList<ActionDefinition> Definitions() =>
[
    Define("farm", 2, RewardType.Money, 15, 15, 4, SkillType.Farming, 2),
    Define("woodcut", 3, RewardType.Money, 25, 25, 3, SkillType.Farming, 1),
    Define("study", 2, RewardType.Culture, 1, 1, 4, SkillType.Scholarship, 2),
    Define("fish", 3, RewardType.Money, 0, 40, 2, SkillType.Farming, 1),
    Define("rest", 0, RewardType.Energy, 2, 2, 1)
];

static ActionDefinition Define(
    string id,
    int energyCost,
    RewardType rewardType,
    int rewardMin,
    int rewardMax,
    int monthlyLimit = 0,
    SkillType skillType = SkillType.None,
    int skillGain = 0) =>
    new()
    {
        Id = id,
        Name = id,
        Description = id,
        EnergyCost = energyCost,
        RewardType = rewardType,
        RewardMin = rewardMin,
        RewardMax = rewardMax,
        RewardLabel = id,
        MonthlyLimit = monthlyLimit,
        SkillType = skillType,
        SkillGain = skillGain,
        IconPath = string.Empty,
        AccentColor = "#000000"
    };

static ConditionData Condition(string field, string comparisonOperator, string value) =>
    new() { Field = field, Operator = comparisonOperator, Value = value };

static RewardData Reward(string type, int amount) =>
    new() { Type = type, Amount = amount };

static EventChoice Choice(string id, params RewardData[] rewards) =>
    new()
    {
        Id = id,
        Text = id,
        Rewards = rewards,
        CloseEvent = true
    };

static EventData Event(string id, int weight, params EventChoice[] choices) =>
    new()
    {
        Id = id,
        Title = id,
        Description = id,
        Weight = weight,
        Choices = choices
    };

static void True(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Equal<T>(T expected, T actual, string field)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{field}: expected {expected}, got {actual}");
    }
}
