namespace AncientLife.Models;

public readonly record struct ActionAvailability(bool CanPerform, string Reason)
{
    public static ActionAvailability Available => new(true, string.Empty);
}
