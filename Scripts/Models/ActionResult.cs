namespace AncientLife.Models;

public sealed record ActionResult(
  bool Success,
  string ActionId,
  string Message,
  int RewardAmount = 0,
  SkillType SkillType = SkillType.None,
  int SkillGain = 0)
{
  public static ActionResult Failed(string actionId, string message) =>
    new(false, actionId, message);
}
