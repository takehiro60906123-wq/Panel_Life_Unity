using System;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleRandom
{
    int Range(int minInclusive, int maxExclusive);
}

public sealed class UnityBattleRandom : IBattleRandom
{
    public int Range(int minInclusive, int maxExclusive)
    {
        return UnityEngine.Random.Range(minInclusive, maxExclusive);
    }
}

/// <summary>
/// テスト用の決め打ち乱数。
/// 値が尽きたら minInclusive を返します。
/// </summary>
public sealed class SequenceBattleRandom : IBattleRandom
{
    private readonly Queue<int> values = new Queue<int>();

    public SequenceBattleRandom(IEnumerable<int> seedValues)
    {
        if (seedValues == null)
        {
            throw new ArgumentNullException(nameof(seedValues));
        }

        foreach (int value in seedValues)
        {
            values.Enqueue(value);
        }
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        if (values.Count == 0)
        {
            return minInclusive;
        }

        int raw = values.Dequeue();
        int width = maxExclusive - minInclusive;
        int normalized = ((raw % width) + width) % width;
        return minInclusive + normalized;
    }
}
