using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item")]
public class ItemInfo : ScriptableObject
{
    public enum AffectedStat
    {
        Movement = 0,
        Health = 1,
        Damage = 2,
        Crit = 3,
        Defence = 4,
    }

    [System.Serializable]
    public struct ModifyStat
    {
        public AffectedStat stat;
        public int value;
    }

    [Header("Stats modifiers")]
    [Tooltip("List with each stat modified by this item")]
    public List<ModifyStat> modifiedStats = new List<ModifyStat>();

    [Header("Abilities")]
    [Tooltip("List with each new ability obtained from this item")]
    public List<AbilityInfo> abilities = new List<AbilityInfo>();

    [Header("Other information")]
    [Tooltip("Specifies whether this item can be spawned on a hex")]
    public bool isSpawnable;

    [Tooltip("Game Object to be shown on top of the hex")]
    public GameObject prefab;

}
