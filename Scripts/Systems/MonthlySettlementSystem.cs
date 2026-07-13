using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class MonthlySettlementSystem
{
  private const int NormalFoodCost = 1;
  private const int WinterFoodCost = 2;
  private const int FoodPurchaseCost = 12;
  private const int NormalLivingCost = 5;
  private const int WinterLivingCost = 8;
  private const int HardshipHealthCost = 8;

  public MonthlySettlementResult Settle(CharacterState character, CalendarState calendar)
  {
    var startingFood = character.Food;
    var startingMoney = character.Money;
    var startingHealth = character.Health;
    var messages = new List<string>();
    var foodCost = calendar.Season == Season.Winter ? WinterFoodCost : NormalFoodCost;
    var livingCost = calendar.Season == Season.Winter ? WinterLivingCost : NormalLivingCost;

    var foodUsed = Math.Min(character.Food, foodCost);
    character.Food -= foodUsed;
    var missingFood = foodCost - foodUsed;
    if (missingFood == 0)
    {
      messages.Add($"消耗了 {foodCost} 份粮食");
    }
    else
    {
      var affordableFood = Math.Min(missingFood, character.Money / FoodPurchaseCost);
      if (affordableFood > 0)
      {
        var purchaseCost = affordableFood * FoodPurchaseCost;
        character.Money -= purchaseCost;
        missingFood -= affordableFood;
        messages.Add($"花费 {purchaseCost} 文补足口粮");
      }

      if (missingFood > 0)
      {
        var healthCost = missingFood * HardshipHealthCost;
        character.Health = Math.Max(0, character.Health - healthCost);
        messages.Add($"仍有 {missingFood} 份口粮无着，健康下降 {healthCost} 点");
      }
    }

    var paidLivingCost = Math.Min(character.Money, livingCost);
    character.Money -= paidLivingCost;
    var livingShortfall = livingCost - paidLivingCost;
    if (livingShortfall == 0)
    {
      messages.Add($"日常用度 {livingCost} 文");
    }
    else
    {
      var healthCost = Math.Max(2, livingShortfall);
      character.Health = Math.Max(0, character.Health - healthCost);
      messages.Add($"用度不足，健康下降 {healthCost} 点");
    }

    return new MonthlySettlementResult(
      $"本月结算：{string.Join("；", messages)}。",
      character.Food - startingFood,
      character.Money - startingMoney,
      character.Health - startingHealth);
  }
}
