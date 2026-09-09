using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum CardTarget { Self, Ally, Enemy }
public enum CardEffectKind { Damage, Block, Heal, Status, Draw }

[Serializable]
public struct CardEffect
{
    public CardEffectKind kind;
    public int amount;
    public StatusKind status;
    public int duration;
    public CardEffect(CardEffectKind kind, int amount, StatusKind status = StatusKind.Poison, int duration = 1)
    { this.kind = kind; this.amount = amount; this.status = status; this.duration = duration; }
}

public sealed class CardDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int Cost { get; }
    public int Range { get; }
    public CardTarget Target { get; }
    public bool Exhausts { get; }
    public IReadOnlyList<CardEffect> Effects { get; }

    public CardDefinition(string id, string name, int cost, int range, CardTarget target,
        IEnumerable<CardEffect> effects, bool exhausts = false)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || cost < 0 || range < 0 ||
            !Enum.IsDefined(typeof(CardTarget), target)) throw new ArgumentException("Invalid card definition.");
        var copy = new List<CardEffect>(effects ?? throw new ArgumentNullException(nameof(effects)));
        if (copy.Count == 0) throw new ArgumentException("A card needs an effect.");
        foreach (var effect in copy)
            if (!Enum.IsDefined(typeof(CardEffectKind), effect.kind) || effect.amount <= 0 ||
                (effect.kind == CardEffectKind.Status &&
                (effect.duration <= 0 || !Enum.IsDefined(typeof(StatusKind), effect.status))))
                throw new ArgumentException("Invalid card effect.");
        Id = id; Name = name; Cost = cost; Range = range; Target = target; Exhausts = exhausts;
        Effects = new ReadOnlyCollection<CardEffect>(copy);
    }
}

/// <summary>Each copy has distinct identity, even when several copies use the same definition.</summary>
public sealed class CardInstance
{
    public CardDefinition Definition { get; }
    public CardInstance(CardDefinition definition) => Definition = definition ?? throw new ArgumentNullException(nameof(definition));
}
