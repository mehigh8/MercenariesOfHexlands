using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC", menuName = "NPC")]
public class NPCInfo : ScriptableObject
{
    public enum NPCType
    {
        Passive = 0,
        Neutral = 1,
        Aggressive = 2,
    }

    [Header("General Configuration")]
    [Tooltip("Name of this NPC")]
    public string npcName;
    [Tooltip("Type of this NPC: Passive - will not attack; Neutral - will attack if attacked; Agressive - will attack on sight")]
    public NPCType npcType;
    [Tooltip("Prefab used for this NPC")]
    public GameObject npcPrefab;

    [Header("Stats")]
    [Tooltip("Maximum health of this NPC")]
    public int health;
    [Tooltip("Base damage of this NPC")]
    public int damage;
    [Tooltip("Base defence of this NPC")]
    public int defence;
    [Tooltip("Base crit chance of this NPC")]
    public float critChance;
    [Tooltip("Base movement distance of this NPC")]
    public int movement;
    [Tooltip("Detection distance of this NPC")]
    public int detection;

    [Header("Abilities")]
    [Tooltip("List of abilities this NPC can use")]
    public List<AbilityInfo> abilities;
}
