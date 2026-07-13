using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class EndingSystem
{
  public string Evaluate(CharacterState character)
  {
    if (character.ProfessionId is "village_gentry" or "juren")
    {
      return "功成名就";
    }

    if (character.Age >= 60 && character.Money >= 300)
    {
      return "安稳终老";
    }

    if (character.Money == 0 && character.Food == 0)
    {
      return "穷困离世";
    }

    if (character.Culture >= 40)
    {
      return "耕读传家";
    }

    return "平凡一生";
  }
}
