namespace AncientLife.Models;

public sealed record AgingResult(
  string Message,
  int HealthChange,
  int MaxHealthChange = 0,
  int MaxEnergyChange = 0)
{
  public static AgingResult None => new(string.Empty, 0);
}
