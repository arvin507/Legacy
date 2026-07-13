using AncientLife.Models;
using AncientLife.Models.Events;

namespace AncientLife.Systems.Events;

public sealed class ConditionEvaluator
{
  public bool AreMet(
    IReadOnlyList<ConditionData> conditions,
    CharacterState character,
    CalendarState calendar)
  {
    return conditions.All(condition => IsMet(condition, character, calendar));
  }

  public bool IsMet(ConditionData condition, CharacterState character, CalendarState calendar)
  {
    var field = condition.Field.Trim().ToLowerInvariant();
    var textValue = field switch
    {
      "season" => calendar.SeasonName,
      "profession" or "profession_id" => character.ProfessionId,
      _ => null
    };
    if (textValue is not null)
    {
      return CompareText(textValue, condition.Operator, condition.Value);
    }

    var actualValue = field switch
    {
      "money" => character.Money,
      "culture" => character.Culture,
      "age" => character.Age,
      "month" => calendar.Month,
      "health" => character.Health,
      "max_health" => character.MaxHealth,
      "stamina" or "energy" => character.Energy,
      "food" => character.Food,
      "year" => calendar.Year,
      "total_months" => calendar.TotalMonthsSurvived,
      "farming_skill" => character.FarmingSkill,
      "scholarship_skill" => character.ScholarshipSkill,
      "reputation" => character.Reputation,
      _ => (int?)null
    };

    return actualValue.HasValue &&
           int.TryParse(condition.Value, out var expectedValue) &&
           CompareNumber(actualValue.Value, condition.Operator, expectedValue);
  }

  private static bool CompareNumber(int actual, string comparisonOperator, int expected) =>
    comparisonOperator.Trim() switch
    {
      ">=" => actual >= expected,
      "<=" => actual <= expected,
      ">" => actual > expected,
      "<" => actual < expected,
      "==" or "=" => actual == expected,
      "!=" => actual != expected,
      _ => false
    };

  private static bool CompareText(string actual, string comparisonOperator, string expected) =>
    comparisonOperator.Trim() switch
    {
      "==" or "=" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
      "!=" => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
      _ => false
    };
}
