namespace AncientLife.Models;

public sealed record ProfessionResult(
  bool Changed,
  string Message,
  int MoneyChange = 0,
  int FoodChange = 0)
{
  public static ProfessionResult None => new(false, string.Empty);
}
