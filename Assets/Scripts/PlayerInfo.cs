using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHealth;
    [HideInInspector] public int currentHealth;
    public int damage;
    [Range(0f, 1f)]
    public float critChance;
    public int movementPerTurn;
    public int defence;

    public void EquipItem(ItemInfo item)
    {
        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn += stat.value;
                    break;
            }
        }

        // Here will go abilities
    }

    public void UnequipItem(ItemInfo item)
    {
        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn -= stat.value;
                    break;
            }
        }

        // Here will go abilities
    }

    private void Awake()
    {
        currentHealth = maxHealth;
    }
}
