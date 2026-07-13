using AncientLife.Models;
using AncientLife.Models.Events;
using AncientLife.Systems;

namespace AncientLife.Managers;

public sealed class GameSession
{
  private readonly ActionSystem _actionSystem;
  private readonly MonthlySettlementSystem _monthlySettlementSystem;
  private readonly TimeSystem _timeSystem;
  private readonly AgingSystem _agingSystem;
  private readonly ProfessionSystem _professionSystem;
  private readonly EndingSystem _endingSystem;
  private readonly Dictionary<string, int> _monthlyUsage = new(StringComparer.Ordinal);
  private readonly List<LifeRecord> _lifeRecords = [];

  public GameSession(
    IReadOnlyList<ActionDefinition> actions,
    ActionSystem? actionSystem = null,
    MonthlySettlementSystem? monthlySettlementSystem = null,
    TimeSystem? timeSystem = null,
    AgingSystem? agingSystem = null,
    ProfessionSystem? professionSystem = null,
    EndingSystem? endingSystem = null)
  {
    Actions = actions;
    _actionSystem = actionSystem ?? new ActionSystem();
    _monthlySettlementSystem = monthlySettlementSystem ?? new MonthlySettlementSystem();
    _timeSystem = timeSystem ?? throw new ArgumentNullException(nameof(timeSystem));
    _agingSystem = agingSystem ?? new AgingSystem();
    _professionSystem = professionSystem ?? new ProfessionSystem();
    _endingSystem = endingSystem ?? new EndingSystem();
    Character = CharacterState.CreateDefault();
    _professionSystem.Initialize(Character);
    Calendar = _timeSystem.CreateCalendar();
    Record("人生", "十六岁这年，你开始独自谋划今后的生活。");
  }

  public event System.Action? StateChanged;
  public event System.Action<ActionResult>? ActionResolved;
  public event System.Action<MonthlySettlementResult>? MonthSettled;
  public event System.Action<GameSummary>? GameEnded;

  public IReadOnlyList<ActionDefinition> Actions { get; }
  public IReadOnlyList<LifeRecord> LifeRecords => _lifeRecords;
  public CharacterState Character { get; private set; }
  public CalendarState Calendar { get; private set; }
  public string ProfessionGoal => _professionSystem.DescribeGoal(Character);
  public bool IsGameOver { get; private set; }
  public bool IsEndingMonth { get; private set; }
  public GameSummary? Summary { get; private set; }

  public ActionAvailability GetActionAvailability(string actionId)
  {
    var definition = FindAction(actionId);
    return definition is null
      ? new(false, "行动不存在")
      : _actionSystem.GetAvailability(definition, Character, _monthlyUsage, IsGameOver || IsEndingMonth);
  }

  public ActionResult PerformAction(string actionId)
  {
    var definition = FindAction(actionId);
    var result = definition is null
      ? ActionResult.Failed(actionId, "行动不存在")
      : _actionSystem.Perform(definition, Character, _monthlyUsage, IsGameOver || IsEndingMonth);

    if (result.Success && definition is not null)
    {
      var professionResult = _professionSystem.HandleAction(Character, definition);
      if (professionResult.Changed)
      {
        result = result with { Message = $"{result.Message}\n{professionResult.Message}" };
        Record("职业", professionResult.Message);
      }
    }

    ActionResolved?.Invoke(result);
    if (result.Success)
    {
      StateChanged?.Invoke();
    }

    return result;
  }

  public void EndMonth()
  {
    if (BeginEndMonth())
    {
      CompleteEndMonth();
    }
  }

  public bool BeginEndMonth()
  {
    if (IsGameOver || IsEndingMonth)
    {
      return false;
    }

    IsEndingMonth = true;
    var startingFood = Character.Food;
    var startingMoney = Character.Money;
    var startingHealth = Character.Health;
    var messages = new List<string>();

    var benefit = _professionSystem.ApplyMonthlyBenefits(Character);
    if (!string.IsNullOrWhiteSpace(benefit.Message))
    {
      messages.Add(benefit.Message);
    }

    var promotion = _professionSystem.TryPromote(Character);
    if (promotion.Changed)
    {
      messages.Add(promotion.Message);
      Record("晋升", promotion.Message);
    }

    var settlement = _monthlySettlementSystem.Settle(Character, Calendar);
    messages.Add(settlement.Message);

    var aging = _agingSystem.ApplyMonthlyWear(Character);
    if (!string.IsNullOrWhiteSpace(aging.Message))
    {
      messages.Add(aging.Message);
    }

    MonthSettled?.Invoke(new MonthlySettlementResult(
      string.Join("\n", messages),
      Character.Food - startingFood,
      Character.Money - startingMoney,
      Character.Health - startingHealth));

    if (Character.Health <= 0)
    {
      CompleteEndMonth();
      return false;
    }

    StateChanged?.Invoke();
    return true;
  }

  public bool CompleteEndMonth()
  {
    if (!IsEndingMonth || IsGameOver)
    {
      return false;
    }

    var previousAge = Character.Age;
    _timeSystem.CompleteCurrentMonth(Calendar, Character);
    _monthlyUsage.Clear();
    IsEndingMonth = false;

    if (Character.Age > previousAge)
    {
      var birthday = _agingSystem.ApplyBirthday(Character);
      if (!string.IsNullOrWhiteSpace(birthday.Message))
      {
        MonthSettled?.Invoke(new MonthlySettlementResult(
          birthday.Message,
          0,
          0,
          birthday.HealthChange,
          birthday.MaxHealthChange,
          birthday.MaxEnergyChange));
      }

      if (Character.Age is 40 or 50 or 60 or 70 or 80)
      {
        Record("年岁", $"你已年满 {Character.Age} 岁，身体开始显出岁月的痕迹。");
      }
    }

    if (Character.Health <= 0)
    {
      EndGame();
      return false;
    }

    Character.RestoreMonthlyEnergy();
    StateChanged?.Invoke();
    return true;
  }

  public void RecordEvent(EventResolution resolution)
  {
    Record(
      "经历",
      $"{resolution.ResolvedEvent.Title}：你选择了“{resolution.SelectedChoice.Text}”。");
  }

  public void Restart()
  {
    Character = CharacterState.CreateDefault();
    _professionSystem.Initialize(Character);
    Calendar = _timeSystem.CreateCalendar(Calendar.EraName);
    _monthlyUsage.Clear();
    _lifeRecords.Clear();
    IsGameOver = false;
    IsEndingMonth = false;
    Summary = null;
    Record("人生", "十六岁这年，你开始独自谋划今后的生活。");
    StateChanged?.Invoke();
  }

  private void EndGame()
  {
    IsGameOver = true;
    var endingTitle = _endingSystem.Evaluate(Character);
    Record("结局", $"你在 {Character.Age} 岁时离世，此生被后人称作“{endingTitle}”。");
    Summary = new GameSummary(
      Calendar.TotalMonthsSurvived,
      Character.Money,
      Character.Culture,
      Character.Age,
      Character.ProfessionName,
      endingTitle,
      _lifeRecords.ToArray());
    StateChanged?.Invoke();
    GameEnded?.Invoke(Summary);
  }

  private void Record(string category, string text)
  {
    _lifeRecords.Add(new LifeRecord(
      Character.Age,
      Calendar.EraYearName,
      Calendar.MonthName,
      category,
      text));
  }

  private ActionDefinition? FindAction(string actionId) =>
    Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.Ordinal));
}
