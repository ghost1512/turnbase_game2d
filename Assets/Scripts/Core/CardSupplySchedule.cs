using System;

/// <summary>Round 1 starts with three cards; rounds 3, 5, 7... receive one more.</summary>
public sealed class CardSupplySchedule
{
    private int lastRound;
    public int GetDrawCount(int round)
    {
        if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
        if (round <= lastRound) return 0;
        lastRound = round;
        return round == 1 ? 3 : (round - 1) % 2 == 0 ? 1 : 0;
    }
}
