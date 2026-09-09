using UnityEngine;

[CreateAssetMenu(
    fileName = "NewUnitData",
    menuName = "Turn Based/Unit Data"
)]
public class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string unitName;

    public Sprite unitSprite;

    [TextArea]
    public string description;

    [Header("Stats")]
    public int maxHP = 10;
    public int damage = 3;

    public int moveRange = 3;
    public int attackRange = 1;


    [Header("Team")]
    public UnitTeam defaultTeam;
}

