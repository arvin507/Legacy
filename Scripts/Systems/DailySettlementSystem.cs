using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class DailySettlementSystem
{
    private const int DailyFoodCost = 1;
    private const int FoodPurchaseCost = 10;
    private const int StarvationHealthCost = 10;

    public DailySettlementResult Settle(CharacterState character)
    {
        if (character.Food >= DailyFoodCost)
        {
            character.Food -= DailyFoodCost;
            return new("本月消耗了 1 份粮食。", -DailyFoodCost, 0, 0);
        }

        if (character.Money >= FoodPurchaseCost)
        {
            character.Money -= FoodPurchaseCost;
            return new("家中无粮，花费 10 文解决了本月温饱。", 0, -FoodPurchaseCost, 0);
        }

        character.Health = Math.Max(0, character.Health - StarvationHealthCost);
        return new("无粮又无钱，健康下降了 10 点。", 0, 0, -StarvationHealthCost);
    }
}
