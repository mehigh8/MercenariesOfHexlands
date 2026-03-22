using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
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

    // The following variables are relevant only for the Server
    private NavMeshAgent navAgent; // NavMeshAgent component of this NPC
    private HexGridLayout.HexNode currentHexNode = null; // The actual hex this NPC is standing on (is updated from the SyncVar hex name)

    [HideInInspector] public NPCInfo npcInfo; // NPC Info reference

    #region Unity Functions
    private void Awake()
    {
        // Add callback to update current hex node
        currentHex.OnChange += OnUpdateHex;
    }


    private void Start()
    {
        // Get NavMeshAgent component
        navAgent = GetComponent<NavMeshAgent>();
    }

    private void OnDisable()
    {
        // Remove callbacks
        currentHex.OnChange -= OnUpdateHex;
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Callback that updates the current hex node when the hex node name SyncVar changes value
    /// </summary>
    /// <param name="oldVal">Old value of the SyncVar</param>
    /// <param name="newVal">New value of the SyncVar</param>
    /// <param name="asServer">Bool specifying if callback was called as Server of Client</param>
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

    #region Behaviour Functions
    /// <summary>
    /// Function that chooses the action for this NPC <br/>
    /// This should only be called from the Server as it uses variables that may not be set on clients
    /// </summary>
    public void ChooseAction()
    {
        // For now we only have the Move action so it will automatically be chosen
        Move();
    }
    #endregion

    #region Move Action Functions
    /// <summary>
    /// Function that moves the NPC to a random hex in its movement range
    /// </summary>
    private void Move()
    {
        if (npcInfo == null)
            Debug.LogWarning("npcInfo is null. Cannot move...");

        // Get all hexes in range that are not obstacles or occupied
        List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(currentHexNode) <= npcInfo.movement && !hex.hexRenderer.IsObstacle() && hex.hexRenderer.occupying.Value == null).ToList();

        // Randomly choose a hex in the list that can be reached with the NPC's movement value. Keep searching until one such hex is found or there are no more hexes to choose from
        List<HexGridLayout.HexNode> path = null;
        HexGridLayout.HexNode dest = null;
        while ((path == null || path.Count > npcInfo.movement) && hexesInRange.Count > 0)
        {
            dest = hexesInRange[Random.Range(0, hexesInRange.Count)];
            hexesInRange.Remove(dest);

            // Calculate path to the chosen hex
            path = Pathfinder.FindPath(currentHexNode, dest);
        }

        // If a path was found start the movement process
        if (path.Count <= npcInfo.movement)
        {
            // Free the hex that the NPC was standing on
            HexGridLayout.instance.UpdateHex(currentHex.Value, null);
            // Start moving towards the new hex
            StartCoroutine(MoveAnimation(path));
            // Occupy the new hex
            HexGridLayout.instance.UpdateHex(dest.hexObj.name, gameObject);
            // Update currentHex SyncVar value
            currentHex.Value = dest.hexObj.name;
        }
        else
        {
            // If a path could not be found, the movement action will be skipped for now and a warning will be sent
            Debug.LogWarning("Could not find hex to move to");
        }
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
    }
    #endregion
}
