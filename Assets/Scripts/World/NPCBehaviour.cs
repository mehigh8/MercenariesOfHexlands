using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NPCBehaviour : NetworkBehaviour
{
    // These SyncVars are necessary for the NPC's health bar and information panel
    [Header("NPC Stats and Info")]
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
    [Header("Additional information")]
    [AllowMutableSyncType] public SyncVar<string> currentHex; // Name of the hex this NPC is standing on

    private NavMeshAgent navAgent; // NavMeshAgent component of this NPC
    private HexGridLayout.HexNode currentHexNode = null; // The actual hex this NPc is standing on (is updated from the SyncVar hex name)

    [HideInInspector] public NPCInfo npcInfo; // NPC Info reference (This is only usable on the Server)

    private void Awake()
    {
        currentHex.OnChange += OnUpdateHex;
    }

    private void OnDisable()
    {
        currentHex.OnChange -= OnUpdateHex;
    }

    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
            Debug.LogError("NPC does not have NavMeshAgent!");
    }

    public void ChooseAction()
    {
        Move();
    }

    private void Move()
    {
        if (npcInfo == null)
            Debug.LogWarning("npcInfo is null. Cannot move...");

        List<HexGridLayout.HexNode> hexesInRange = HexGridLayout.instance.hexNodes.Where(hex => hex.Distance(currentHexNode) <= npcInfo.movement && !hex.hexRenderer.IsObstacle() && hex.hexRenderer.occupying.Value == null).ToList();

        List<HexGridLayout.HexNode> path = null;
        HexGridLayout.HexNode dest = null;
        while ((path == null || path.Count > npcInfo.movement) && hexesInRange.Count > 0)
        {
            dest = hexesInRange[Random.Range(0, hexesInRange.Count)];
            hexesInRange.Remove(dest);

            path = Pathfinder.FindPath(currentHexNode, dest);
        }

        if (path.Count <= npcInfo.movement)
        {
            HexGridLayout.instance.UpdateHex(currentHex.Value, null);
            StartCoroutine(MoveAnimation(path));
            HexGridLayout.instance.UpdateHex(dest.hexObj.name, gameObject);
            currentHex.Value = dest.hexObj.name;
        }
        else
        {
            Debug.LogWarning("Could not find hex to move to");
        }
    }

    private void OnUpdateHex(string oldVal, string newVal, bool asServer)
    {
        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection} - Hex changed from {oldVal} to {newVal}");

        currentHexNode = HexGridLayout.instance.hexNodes.Find(hex => hex.hexObj.name == newVal);
        if (currentHexNode == null)
            Debug.LogWarning("Invalid hex name: " + newVal);
    }

    IEnumerator MoveAnimation(List<HexGridLayout.HexNode> path)
    {
        while (path != null && path.Count > 0)
        {
            if (!navAgent.hasPath || (navAgent.hasPath && navAgent.remainingDistance < 0.1f))
            {
                Vector3 dest = new Vector3(path[0].hexObj.transform.position.x, transform.position.y, path[0].hexObj.transform.position.z);
                path.RemoveAt(0);
                navAgent.SetDestination(dest);
            }
            yield return null;
        }
    }
}
