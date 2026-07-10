namespace AncientLife.Models;

public sealed record DailySettlementResult(
    string Message,
    int FoodChange,
    int MoneyChange,
    int HealthChange);
