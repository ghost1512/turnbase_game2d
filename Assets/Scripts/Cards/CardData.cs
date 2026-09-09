using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Turn Based/Card")]
public sealed class CardData : ScriptableObject
{
    public string id;
    public string displayName;
    [Min(0)] public int energyCost = 1;
    [Min(0)] public int range = 3;
    public CardTarget target;
    public bool exhaust;
    public CardEffect[] effects;
    public CardDefinition CreateDefinition() => new CardDefinition(id, displayName, energyCost, range, target, effects, exhaust);
}
