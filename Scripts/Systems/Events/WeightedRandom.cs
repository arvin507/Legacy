namespace AncientLife.Systems.Events;

public sealed class WeightedRandom
{
    private readonly Random _random;

    public WeightedRandom(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public T? Choose<T>(IReadOnlyList<T> items, Func<T, int> weightSelector)
        where T : class
    {
        if (items.Count == 0)
        {
            return null;
        }

        var totalWeight = 0;
        foreach (var item in items)
        {
            var weight = weightSelector(item);
            if (weight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weightSelector), "Weights cannot be negative.");
            }

            checked
            {
                totalWeight += weight;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        var roll = _random.Next(totalWeight);
        foreach (var item in items)
        {
            roll -= weightSelector(item);
            if (roll < 0)
            {
                return item;
            }
        }

        return items[^1];
    }
}
