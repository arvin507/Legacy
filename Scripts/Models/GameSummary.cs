namespace AncientLife.Models;

public sealed record GameSummary(int MonthsSurvived, int FinalWealth, int Culture)
{
    public int DaysSurvived => MonthsSurvived;
}
