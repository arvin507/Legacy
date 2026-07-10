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
    ("每月结算", CheckDailySettlement),
    ("事件前月结阶段", CheckDeferredDayCompletion),
    ("历法与年龄", CheckCalendar),
    ("架空年号与改元", CheckEraManager),
    ("死亡与重新开始", CheckDeathAndRestart),
    ("动作配置完整", CheckActionConfig),
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
        var result = system.Perform(fish, character, usage, false);
        True(result.RewardAmount is >= 0 and <= 40, "fish reward out of range");
    }
}

static void CheckDailySettlement()
{
    var session = CreateSession();
    session.PerformAction("woodcut");
    session.EndDay();
    Equal(9, session.Character.Food, "food settlement");
    Equal(10, session.Character.Energy, "monthly energy restore");
    Equal(2, session.Calendar.Month, "next month");
    Equal(1, session.Calendar.Day, "month starts at first day");
    Equal(1, session.Calendar.TotalMonthsSurvived, "survived months");
}

static void CheckDeferredDayCompletion()
{
    var session = CreateSession();
    session.PerformAction("farm");

    True(session.BeginEndDay(), "day ending should begin");
    Equal(1, session.Calendar.Month, "month must wait for event resolution");
    Equal(9, session.Character.Food, "settlement occurs before event");
    True(session.IsEndingDay, "day should remain pending");
    True(!session.GetActionAvailability("farm").CanPerform, "actions disabled while event is pending");
    True(!session.BeginEndDay(), "day cannot begin twice");

    True(session.CompleteEndDay(), "day should complete after event");
    Equal(2, session.Calendar.Month, "month advances after event");
    Equal(10, session.Character.Energy, "energy restores after event");
    True(!session.IsEndingDay, "pending state should clear");
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
    Equal(1, calendar.Day, "second month first day");
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
    session.EndDay();

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

static void CheckActionConfig()
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configs", "actions.json");
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    var ids = document.RootElement
        .EnumerateArray()
        .Select(element => element.GetProperty("id").GetString())
        .ToHashSet(StringComparer.Ordinal);

    Equal(5, ids.Count, "configured action count");
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
    Equal(30, root.GetProperty("days_per_month").GetInt32(), "days per month");
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
    True(!evaluator.IsMet(Condition("profession", "==", "farmer"), character, calendar), "unknown field");
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
    return new CalendarConfig { DaysPerMonth = 30, Months = months };
}

static IReadOnlyList<ActionDefinition> Definitions() =>
[
    Define("farm", 2, RewardType.Money, 15, 15),
    Define("woodcut", 3, RewardType.Money, 25, 25),
    Define("study", 2, RewardType.Culture, 1, 1),
    Define("fish", 3, RewardType.Money, 0, 40),
    Define("rest", 0, RewardType.Energy, 2, 2, 1)
];

static ActionDefinition Define(
    string id,
    int energyCost,
    RewardType rewardType,
    int rewardMin,
    int rewardMax,
    int dailyLimit = 0) =>
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
        DailyLimit = dailyLimit,
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
