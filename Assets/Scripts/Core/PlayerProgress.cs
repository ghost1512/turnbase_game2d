using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerProgress
{
    public int version = 1;
    public int battlesWon;
    public int battlesLost;
    public int stage = 1;
    public List<string> deck = new List<string>();

    public bool IsValid(GameDatabase database)
    {
        if (version != 1 || battlesWon < 0 || battlesLost < 0 || stage < 1 ||
            deck == null || deck.Count < 1 || deck.Count > 200) return false;
        foreach (string id in deck) if (!database.Contains(id)) return false;
        return true;
    }

    public PlayerProgress Copy() => new PlayerProgress
    {
        version = version, battlesWon = battlesWon, battlesLost = battlesLost,
        stage = stage, deck = new List<string>(deck)
    };
}
