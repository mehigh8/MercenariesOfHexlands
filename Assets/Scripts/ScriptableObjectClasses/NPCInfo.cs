using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC", menuName = "NPC")]
public class NPCInfo : ScriptableObject
{
    /// <summary>
    /// Class storing a node of the NPC's behaviour tree
    /// </summary>
    [System.Serializable]
    public class BehaviourNode
    {
        public NPCState state; // Enum state stored in this node
        public List<Pair<NPCSwitchCondition, NPCState>> connections; // Connections to other states
    }

    public enum NPCState
    {
        Wander = 0,
        Run = 1,
        Attack = 2,
    }

    public enum NPCSwitchCondition
    { 
        Hit = 0,
        HitOrSeen = 1,
        FarFromThreat = 2,
        LowOnHealth = 3,
        Healthy = 4,
    }

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
    [Tooltip("Distance this npc has to run from a threat to lose it")]
    public int runDistance;
    [Tooltip("Health threshold to cause NPC to panic and run")]
    public int healthLowThreshold;
    [Tooltip("Health threshold for NPC to be considered healthy and be able to attack")]
    public int healthyThreshold;

    [Header("Behaviour")]
    public List<BehaviourNode> npcBehaviour;

    [Header("Abilities")]
    [Tooltip("List of abilities this NPC can use")]
    public List<AbilityInfo> abilities;

}
