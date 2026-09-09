using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Stable IDs are the persistence contract; display names can change freely.</summary>
public sealed class GameDatabase
{
    private readonly Dictionary<string, CardDefinition> cards = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
    public IEnumerable<CardDefinition> Cards => cards.Values;
    public GameDatabase(IEnumerable<CardDefinition> definitions)
    {
        foreach (var card in definitions)
        {
            if (card == null || cards.ContainsKey(card.Id)) throw new ArgumentException("Null or duplicate card ID.");
            cards.Add(card.Id, card);
        }
        if (cards.Count == 0) throw new ArgumentException("Empty database.");
    }
    public bool Contains(string id) => id != null && cards.ContainsKey(id);
    public CardDefinition Get(string id) => cards[id];
    public List<CardDefinition> Resolve(IEnumerable<string> ids) => ids.Select(Get).ToList();

    public static GameDatabase CreateDefault() => new GameDatabase(new[]
    {
        new CardDefinition("strike", "Strike", 1, 3, CardTarget.Enemy, new[] { new CardEffect(CardEffectKind.Damage, 6) }),
        new CardDefinition("guard", "Guard", 1, 0, CardTarget.Self, new[] { new CardEffect(CardEffectKind.Block, 5) }),
        new CardDefinition("mend", "Mend", 1, 3, CardTarget.Ally, new[] { new CardEffect(CardEffectKind.Heal, 4) }, true),
        new CardDefinition("venom", "Venom", 1, 3, CardTarget.Enemy, new[] { new CardEffect(CardEffectKind.Status, 2, StatusKind.Poison, 3) }),
        new CardDefinition("weaken", "Weaken", 1, 3, CardTarget.Enemy, new[] { new CardEffect(CardEffectKind.Status, 1, StatusKind.Weak, 2) }),
        new CardDefinition("expose", "Expose", 1, 3, CardTarget.Enemy, new[] { new CardEffect(CardEffectKind.Status, 1, StatusKind.Vulnerable, 2) }),
        new CardDefinition("focus", "Focus", 1, 0, CardTarget.Self, new[] { new CardEffect(CardEffectKind.Draw, 2) }, true),
        new CardDefinition("rage", "Rage", 1, 0, CardTarget.Self, new[] { new CardEffect(CardEffectKind.Status, 2, StatusKind.Strength, 2) })
    });
    public static List<string> DefaultDeck() => new List<string>
        { "strike", "strike", "strike", "guard", "guard", "mend", "venom", "weaken", "expose", "focus", "rage" };
}
