using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityHandler))]
public class NPCBehaviour : NetworkBehaviour
{
    // These SyncVars are necessary for the NPC's health bar and information panel
    [Header("NPC Stats and Info")]
    [AllowMutableSyncType] public SyncVar<string> npcName; // NPC's name
    [AllowMutableSyncType] public SyncVar<int> npcId; // NPC's ID; Used for RPCs
    [Tooltip("0 - passive; 1 - neutral; 2 - agressive")]
    [AllowMutableSyncType] public SyncVar<int> npcType; // NPC's type
    [Tooltip("String contains all item ids of the npc's abilities, separated by ,")]
    [AllowMutableSyncType] public SyncVar<string> npcAbilities; // List of NPC's abilities
    [AllowMutableSyncType] public SyncVar<int> maxHealth; // NPC's maximum health
    [AllowMutableSyncType] public SyncVar<int> currentHealth; // NPC's current health
    [AllowMutableSyncType] public SyncVar<int> damage; // NPC's damage stat
    [AllowMutableSyncType] public SyncVar<int> defence; // NPC's defence stat
    [AllowMutableSyncType] public SyncVar<float> critChance; // NPC's critical chance stat
    [AllowMutableSyncType] public SyncVar<int> movement; // NPC's movement stat
    [Header("Additional information")]
    [AllowMutableSyncType] public SyncVar<string> currentHex; // Name of the hex this NPC is standing on
    [Header("References")]
    [SerializeField] private TMP_Text nameText; // Reference to the text object used for the name above the NPC
    [SerializeField] private Slider healthBar; // Reference to the health bar above the NPC
    [SerializeField] private TMP_Text healthText; // Reference to the health text above the NPC

    // The following variables are relevant only for the Server
    private NavMeshAgent navAgent; // NavMeshAgent component of this NPC
    private AbilityHandler abilityHandler; // AbilityHandler component of this NPC
    [HideInInspector] public HexGridLayout.HexNode currentHexNode = null; // The actual hex this NPC is standing on (is updated from the SyncVar hex name)

    [HideInInspector] public NPCInfo npcInfo; // NPC Info reference
    [HideInInspector] public PlayerController threat; // Reference to the PlayerController of the player this NPC considers as a threat
    [HideInInspector] public BehaviourStateBase currentState; // Reference to the current behaviour state of this NPC
    [HideInInspector] public bool isMoving = false; // Boolean specifying if this NPC is currently moving

    #region Unity Functions
    private void Awake()
    {
        // Add callbacks
        currentHex.OnChange += OnUpdateHex;
        maxHealth.OnChange += OnMaxHealthChange;
        currentHealth.OnChange += OnCurrentHealthChange;
        npcName.OnChange += OnNameChange;
    }


    private void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        abilityHandler = GetComponent<AbilityHandler>();
        abilityHandler.npcBehaviour = this;
        GameManager.instance.OnBeginTurn += abilityHandler.ReduceCooldowns;
    }

    private void OnDisable()
    {
        // Remove callbacks
        currentHex.OnChange -= OnUpdateHex;
        maxHealth.OnChange -= OnMaxHealthChange;
        currentHealth.OnChange -= OnCurrentHealthChange;
        npcName.OnChange -= OnNameChange;
        GameManager.instance.OnBeginTurn -= abilityHandler.ReduceCooldowns;
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Callback that updates the current hex node when the hex node name SyncVar changes value
    /// </summary>
    /// <param name="oldVal">Old value of the SyncVar</param>
    /// <param name="newVal">New value of the SyncVar</param>
    /// <param name="asServer">Bool specifying if callback was called as Server or Client</param>
    private void OnUpdateHex(string oldVal, string newVal, bool asServer)
    {
        // If this callback is called on a client we should return as the clients don't need this and they could get the wrong hex
        if (!asServer)
            return;

        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection} - Hex changed from {oldVal} to {newVal}");

        // Search for the hex's name in the hex nodes list from HexGridLayout
        currentHexNode = HexGridLayout.instance.hexNodes.Find(hex => hex.hexObj.name == newVal);
        // Send a warning if hex node was not found
        if (currentHexNode == null)
            Debug.LogWarning("Invalid hex name: " + newVal);
    }
    #endregion

    /// <summary>
    /// Callback that updates the health bar when the current health of the NPC changes
    /// </summary>
    /// <param name="oldVal">Old health value</param>
    /// <param name="newVal">New health value</param>
    /// <param name="asServer">Bool specifying if callback was called as Server or Client</param>
    private void OnCurrentHealthChange(int oldVal, int newVal, bool asServer)
    {
        healthBar.value = (float)newVal / maxHealth.Value;
        healthText.text = newVal + " / " + maxHealth.Value;
    }

    /// <summary>
    /// Callback that updates the health bar when the max health of the NPC changes
    /// </summary>
    /// <param name="oldVal">Old health value</param>
    /// <param name="newVal">New health value</param>
    /// <param name="asServer">Bool specifying if callback was called as Server or Client</param>
    private void OnMaxHealthChange(int oldVal, int newVal, bool asServer)
    {
        healthBar.value = (float)currentHealth.Value / newVal;
        healthText.text = currentHealth.Value + " / " + newVal;
    }

    /// <summary>
    /// Callback that changes the name text of the NPC
    /// </summary>
    /// <param name="oldVal">Old name</param>
    /// <param name="newVal">New name</param>
    /// <param name="asServer">Bool specifying if callback was called as Server or Client</param>
    private void OnNameChange(string oldVal, string newVal, bool asServer)
    {
        nameText.text = newVal;
    }

    #region Turn Functions
    /// <summary>
    /// Function that chooses the action for this NPC <br/>
    /// This should only be called from the Server as it uses variables that may not be set on clients
    /// </summary>
    public void ChooseAction()
    {
        // Update current state
        currentState = currentState.UpdateState();

        // Apply state actions
        StartCoroutine(currentState.DoStateActions());
    }

    /// <summary>
    /// Function used by the NPC to end its turn
    /// </summary>
    private void EndTurn()
    {
        NPCManager.instance.DoNPCTurn();
    }
    #endregion

    #region Actions Related Functions
    /// <summary>
    /// Function that moves the NPC on a specified path
    /// </summary>
    public void Move(List<HexGridLayout.HexNode> path)
    {
        if (path == null || path.Count == 0)
            return;

        isMoving = true;
        // Free the hex that the NPC was standing on
        HexGridLayout.instance.UpdateHex(currentHex.Value, null);
        HexGridLayout.HexNode dest = path[path.Count - 1];
        // Start moving towards the new hex
        StartCoroutine(MoveAnimation(path));
        // Occupy the new hex
        HexGridLayout.instance.UpdateHex(dest.hexObj.name, gameObject);
        // Update currentHex SyncVar value
        currentHex.Value = dest.hexObj.name;
    }
    /// <summary>
    /// IEnumerator used to animate the NPC moving towards its destination<br/>
    /// Uses the NavMeshAgent component to move from hex to hex
    /// </summary>
    /// <param name="path">List of hex nodes representing the path towards the destination</param>
    private IEnumerator MoveAnimation(List<HexGridLayout.HexNode> path)
    {
        // As long as the path has nodes, pop one hex at a time and move towards it
        while (path != null && path.Count > 0)
        {
            if (!navAgent.hasPath || (navAgent.hasPath && navAgent.remainingDistance < 0.1f))
            {
                Vector3 dest = new Vector3(path[0].hexObj.transform.position.x, transform.position.y, path[0].hexObj.transform.position.z);
                path.RemoveAt(0);
                navAgent.SetDestination(dest);
            }
            // Use yield return null to go to the next frame
            yield return null;
        }

        isMoving = false;
    }

    /// <summary>
    /// Placeholder function used to indicate that the NPC wants to heal
    /// </summary>
    public void Heal()
    {
        Debug.LogWarning(npcName.Value + ": Heal action");

        if (currentHealth.Value == maxHealth.Value)
        {
            Debug.LogWarning("Not healing since full HP");
            NPCManager.instance.DoNPCTurn();
            return;
        }

        List<AbilityInfo> healAbilities = npcInfo.abilities.Where(a => a.isHeal).ToList();

        AbilityInfo chosenAbility = null;
        foreach (AbilityInfo heal in healAbilities)
        {
            bool isOnCooldown = false;
            foreach (AbilityHandler.AbilityCooldown cooldown in abilityHandler.cooldowns)
            {
                if (cooldown.ability == heal)
                {
                    isOnCooldown = true;
                    break;
                }
            }

            if (isOnCooldown)
                continue;

            if (chosenAbility == null)
            {
                chosenAbility = heal;
                continue;
            }

            int healAmount = heal.useWeaponDamage ? heal.GetDamage(1) /* TODO: replace with actual weapon damage */ : heal.GetDamage();
            if (healAmount > (chosenAbility.useWeaponDamage ? chosenAbility.GetDamage(1) /* TODO: replace with actual weapon damage */ : chosenAbility.GetDamage()))
                chosenAbility = heal;
        }

        if (chosenAbility != null)
        {
            Debug.Log("Casting " + chosenAbility.name);
            abilityHandler.currentAbility = chosenAbility;
            abilityHandler.ConfirmCasting(new List<HexGridLayout.HexNode>() { currentHexNode }, currentHexNode);
        }

        EndTurn();
    }

    /// <summary>
    /// Placeholder function used to indicate that the NPC wants to attack
    /// </summary>
    public void Attack()
    {
        Debug.LogWarning(npcName.Value + ": Attack action");

        List<AbilityInfo> attackAbilities = npcInfo.abilities.Where(a => !a.isHeal).ToList();

        AbilityInfo chosenAbility = null;
        foreach (AbilityInfo attack in attackAbilities)
        {
            List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(h => h.Distance(currentHexNode) <= attack.range).ToList();
            if (!hexesInRange.Contains(threat.currentPosition))
                continue;

            bool isOnCooldown = false;
            foreach (AbilityHandler.AbilityCooldown cooldown in abilityHandler.cooldowns)
            {
                if (cooldown.ability == attack)
                {
                    isOnCooldown = true;
                    break;
                }
            }

            if (isOnCooldown)
                continue;

            if (chosenAbility == null)
            {
                chosenAbility = attack;
                continue;
            }

            int attackAmount = attack.useWeaponDamage ? attack.GetDamage(1) /* TODO: replace with actual weapon damage */ : attack.GetDamage();
            if (attackAmount > (chosenAbility.useWeaponDamage ? chosenAbility.GetDamage(1) /* TODO: replace with actual weapon damage */ : chosenAbility.GetDamage()))
                chosenAbility = attack;
        }

        if (chosenAbility != null)
        {
            Debug.Log("Casting " + chosenAbility.name);
            abilityHandler.currentAbility = chosenAbility;
            List<HexGridLayout.HexNode> affectedNodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(threat.currentPosition) <= chosenAbility.areOfEffect).ToList();
            abilityHandler.ConfirmCasting(affectedNodes, threat.currentPosition);
        }

        EndTurn();
    }
    #endregion

    public void TakeDamage(int damage, PlayerController threat = null)
    {
        TakeDamageRPC(damage, threat ? threat.gameObject.name : "");
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageRPC(int damage, string threat)
    {
        if (this.threat == null)
            this.threat = GameObject.Find(threat)?.GetComponent<PlayerController>();
        
        int updatedHealth = currentHealth.Value - damage;
        currentHealth.Value = updatedHealth < 0 ? 0 : updatedHealth;
        if (updatedHealth <= 0)
            Die();
    }

    [Server]
    private void Die()
    {
        Debug.Log(npcName.Value + " has died");
        NPCManager.instance.NPCDied(this);
    }

    public void HealHP(int value)
    {
        HealRPC(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealRPC(int value)
    {
        int updatedHealth = currentHealth.Value + value;
        if (updatedHealth > maxHealth.Value)
            updatedHealth = maxHealth.Value;

        currentHealth.Value = updatedHealth;
    }
}
