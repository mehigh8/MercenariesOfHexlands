using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    [Header("Player Stats")]
    public int movementPerTurn;

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
}
