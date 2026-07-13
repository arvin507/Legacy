namespace AncientLife.Models;

public sealed record MonthlySettlementResult(
  string Message,
  int FoodChange,
  int MoneyChange,
  int HealthChange,
  int MaxHealthChange = 0,
  int MaxEnergyChange = 0);
