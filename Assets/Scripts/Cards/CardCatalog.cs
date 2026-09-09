using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardCatalog", menuName = "Turn Based/Card Catalog")]
public sealed class CardCatalog : ScriptableObject
{
    public CardData[] cards;
    public CardData[] starterDeck;
    public UnitData[] characters;
    [Min(0)] public int energyPerTurn = 3;
    [Min(3)] public int handLimit = 10;

    public GameDatabase CreateDatabase()
    {
        var characterIds = new HashSet<string>(StringComparer.Ordinal);
        if (characters != null)
            foreach (var character in characters)
                if (character == null || string.IsNullOrWhiteSpace(character.id) || !characterIds.Add(character.id))
                    throw new ArgumentException("Missing or duplicate character ID.");
        var definitions = new List<CardDefinition>();
        if (cards == null) throw new ArgumentException("Catalog has no cards.");
        foreach (var card in cards)
        {
            if (card == null) throw new ArgumentException("Catalog contains a missing card.");
            definitions.Add(card.CreateDefinition());
        }
        return new GameDatabase(definitions);
    }

    public List<string> CreateStarterDeck(GameDatabase database)
    {
        var ids = new List<string>();
        if (starterDeck == null) throw new ArgumentException("Missing starter deck.");
        foreach (var card in starterDeck)
        {
            if (card == null || !database.Contains(card.id)) throw new ArgumentException("Unknown starter card.");
            ids.Add(card.id);
        }
        if (ids.Count == 0 || ids.Count > 200) throw new ArgumentException("Starter deck must contain 1 to 200 cards.");
        return ids;
    }

    public UnitData GetCharacter(string id)
    {
        if (characters == null) return null;
        foreach (var character in characters)
            if (character != null && character.id == id) return character;
        return null;
    }
}
