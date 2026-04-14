namespace TornPages.Engine;

/// <summary>
/// Deterministic RNG seeded from run seed + page index + sub-operation index.
/// Pass the same three values and you get identical results every time.
/// </summary>
public sealed class DeterministicRng
{
    private readonly Random _rng;

    public DeterministicRng(int masterSeed, int pageIndex = 0, int subSeed = 0)
    {
        // Combine via prime multiplication to spread values
        var combined = masterSeed ^ (pageIndex * 1_000_003) ^ (subSeed * 999_983);
        _rng = new Random(combined);
    }

    public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
    public int Next(int maxExclusive) => _rng.Next(maxExclusive);
    public bool Chance(double probability) => _rng.NextDouble() < probability;
    public T Pick<T>(IReadOnlyList<T> list) => list[_rng.Next(list.Count)];
    public T Pick<T>(List<T> list) => list[_rng.Next(list.Count)];

    public List<T> Shuffle<T>(IEnumerable<T> source)
    {
        var list = source.ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    public List<T> Sample<T>(IReadOnlyList<T> source, int count)
    {
        var shuffled = Shuffle(source);
        return shuffled.Take(Math.Min(count, shuffled.Count)).ToList();
    }

    public T PickWeighted<T>(IReadOnlyList<(T item, int weight)> weighted)
    {
        var total = weighted.Sum(w => w.weight);
        var roll = _rng.Next(total);
        var cumulative = 0;
        foreach (var (item, weight) in weighted)
        {
            cumulative += weight;
            if (roll < cumulative) return item;
        }
        return weighted[^1].item;
    }

    // Distribute N points randomly across k buckets (min 0 per bucket)
    public int[] DistributePoints(int total, int buckets)
    {
        var result = new int[buckets];
        for (var i = 0; i < total; i++)
            result[_rng.Next(buckets)]++;
        return result;
    }
}
