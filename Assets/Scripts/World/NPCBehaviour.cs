using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCBehaviour : NetworkBehaviour
{
    [Header("NPC Stats")]
    [AllowMutableSyncType] public SyncVar<string> npcName;
    [AllowMutableSyncType] public SyncVar<int> npcId;
    [Tooltip("0 - passive; 1 - neutral; 2 - agressive")]
    [AllowMutableSyncType] public SyncVar<int> npcType;
    [Tooltip("String contains all item ids of the npc's abilities, separated by ,")]
    [AllowMutableSyncType] public SyncVar<string> npcAbilities;
    [AllowMutableSyncType] public SyncVar<int> maxHealth;
    [AllowMutableSyncType] public SyncVar<int> currentHealth;
    [AllowMutableSyncType] public SyncVar<int> damage;
    [AllowMutableSyncType] public SyncVar<int> defence;
    [AllowMutableSyncType] public SyncVar<float> critChance;
    [AllowMutableSyncType] public SyncVar<int> movement;

    public NPCInfo npcInfo;
}
