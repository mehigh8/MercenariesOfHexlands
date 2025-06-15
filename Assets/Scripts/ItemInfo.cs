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

    public enum EquipmentSlot
    {
        Helmet = 0,
        Chestplate = 1,
        Leggings = 2,
        Boots = 3,
        Gloves = 4,
        Ring = 5,
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
    [Tooltip("Name of the item")]
    public string itemName;

    [Tooltip("Image that should be displayed for this item")]
    public Sprite itemImage;

    [Tooltip("Which equipment slot does the item use")]
    public EquipmentSlot equipmentSlot;

    [Tooltip("Specifies whether this item can be spawned on a hex")]
    public bool isSpawnable;

    [Tooltip("Game Object to be shown on top of the hex")]
    public GameObject prefab;

}
