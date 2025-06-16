using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    [Header("Player Stats")]
    public int maxHealth;
    [AllowMutableSyncType] public SyncVar<int> currentHealth = new SyncVar<int>();
    public int damage;
    [Range(0f, 1f)]
    public float critChance;
    public int movementPerTurn;
    public int defence;

    public void Die()
    {
        Debug.Log("💀");
    }

    public void TakeDamage(int amount)
    {
        currentHealth.Value = Math.Max(0, currentHealth.Value - amount);
        if (currentHealth.Value == 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth.Value = Math.Min(maxHealth, currentHealth.Value + amount);
    }

    public void EquipItem(ItemInfo item)
    {
        if (item == null)
            return;

        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn += stat.value;
                    break;
                case ItemInfo.AffectedStat.Damage:
                    damage += stat.value;
                    break;
                case ItemInfo.AffectedStat.Defence:
                    defence += stat.value;
                    break;
                case ItemInfo.AffectedStat.Crit:
                    critChance += stat.value / 100f;
                    break;
                case ItemInfo.AffectedStat.Health:
                    float healthPercentage = (float)currentHealth.Value / maxHealth;
                    maxHealth += stat.value;
                    currentHealth.Value = (int)(healthPercentage * maxHealth);
                    break;
            }
        }

        foreach (AbilityInfo ability in item.abilities)
        {
            if (!UIManager.instance.abilitiesUIManager.HasAbility(ability))
            {
                UIManager.instance.abilitiesUIManager.AddAbility(ability);
                UIManager.instance.abilitiesUIManager.GenerateAbilityUI();
            }
        }
    }

    public void UnequipItem(ItemInfo item)
    {
        if (item == null)
            return;

        foreach (ItemInfo.ModifyStat stat in item.modifiedStats)
        {
            switch (stat.stat)
            {
                case ItemInfo.AffectedStat.Movement:
                    movementPerTurn -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Damage:
                    damage -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Defence:
                    defence -= stat.value;
                    break;
                case ItemInfo.AffectedStat.Crit:
                    critChance -= stat.value / 100f;
                    break;
                case ItemInfo.AffectedStat.Health:
                    float healthPercentage = (float)currentHealth.Value / maxHealth;
                    maxHealth -= stat.value;
                    currentHealth.Value = (int)(healthPercentage * maxHealth);
                    break;
            }
        }

        foreach (AbilityInfo ability in item.abilities)
        {
            if (UIManager.instance.abilitiesUIManager.HasAbility(ability))
            {
                UIManager.instance.abilitiesUIManager.RemoveAbility(ability);
                UIManager.instance.abilitiesUIManager.GenerateAbilityUI();
            }
        }
    }

    private void Awake()
    {
        currentHealth.Value = maxHealth;
    }
}
