using System;
using System.Collections.Generic;

public sealed class DeckState
{
    private readonly List<CardInstance> draw = new List<CardInstance>();
    private readonly List<CardInstance> hand = new List<CardInstance>();
    private readonly List<CardInstance> discard = new List<CardInstance>();
    private readonly List<CardInstance> exhaust = new List<CardInstance>();
    private readonly Random random;
    public IReadOnlyList<CardInstance> DrawPile => draw.AsReadOnly();
    public IReadOnlyList<CardInstance> Hand => hand.AsReadOnly();
    public IReadOnlyList<CardInstance> DiscardPile => discard.AsReadOnly();
    public IReadOnlyList<CardInstance> ExhaustPile => exhaust.AsReadOnly();
    public int Energy { get; private set; }
    public int HandLimit { get; }
    public event Action Changed;

    public DeckState(IEnumerable<CardDefinition> cards, int seed, int handLimit = 10)
    {
        if (handLimit < 1 || cards == null) throw new ArgumentException("Invalid deck.");
        HandLimit = handLimit;
        random = new Random(seed);
        foreach (var card in cards) draw.Add(new CardInstance(card));
        if (draw.Count == 0) throw new ArgumentException("Deck cannot be empty.");
        Shuffle(draw);
    }

    private void Shuffle(List<CardInstance> pile)
    {
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            CardInstance swap = pile[i]; pile[i] = pile[j]; pile[j] = swap;
        }
    }

    public void BeginTurn(int energy, int drawCount)
    {
        if (energy < 0 || drawCount < 0) throw new ArgumentOutOfRangeException();
        Energy = energy;
        Draw(drawCount);
    }

    public int Draw(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        int drawn = 0;
        while (drawn < count && hand.Count < HandLimit)
        {
            if (draw.Count == 0)
            {
                break; // All remaining cards are already in hand.
            }
            int last = draw.Count - 1;
            hand.Add(draw[last]); draw.RemoveAt(last); drawn++;
        }
        Changed?.Invoke();
        return drawn;
    }

    public bool CanPlay(CardInstance card) => card != null && hand.Contains(card) && Energy >= card.Definition.Cost;

    // Call only after target validation and acquiring the turn lock.
    public bool TryConsume(CardInstance card)
    {
        if (!CanPlay(card)) return false;
        Energy -= card.Definition.Cost;
        hand.Remove(card);
        draw.Add(card);
        Shuffle(draw); // Recycle immediately; the next draw can include this copy.
        Changed?.Invoke();
        return true;
    }

    public void EndTurn()
    {
        Energy = 0; // Unused cards stay in hand between turns.
        Changed?.Invoke();
    }
}
