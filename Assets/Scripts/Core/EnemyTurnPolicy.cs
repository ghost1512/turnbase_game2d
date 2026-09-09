using System;
using System.Collections.Generic;

/// <summary>Select from currently living/active players, using paths computed on the current board.</summary>
public static class EnemyTurnPolicy
{

    public static T ChooseTarget<T>(IEnumerable<T> candidates, T preferred,
        Func<T, bool> inRange, Func<T, int?> pathLength, Func<T, int> health) where T : class
    {
        T nearby = null, reachable = null;
        int shortest = int.MaxValue;
        bool preferredReachable = false;
        foreach (T candidate in candidates)
        {
            if (candidate == null) continue;
            if (inRange(candidate))
            {
                if (ReferenceEquals(candidate, preferred)) return candidate;
                if (nearby == null || health(candidate) < health(nearby)) nearby = candidate;
                continue;
            }
            int? length = pathLength(candidate);
            if (!length.HasValue || length.Value <= 0) continue;
            if (ReferenceEquals(candidate, preferred)) preferredReachable = true;
            if (reachable == null || length.Value < shortest || (length.Value == shortest && health(candidate) < health(reachable)))
            { reachable = candidate; shortest = length.Value; }
        }
        // Never walk past an attackable opponent solely because an old target was locked.
        return nearby ?? (preferredReachable ? preferred : reachable);
    }
}

