using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NPCManager : NetworkBehaviour
{
    public static NPCManager instance = null;
    [Header("NPC generation config")]
    [SerializeField] private int npcCount; // Number of NPCs that will be spawned at the start of the game. May be changed later to allow NPCs to spawn later
    [Space]
    public List<NPCInfo> allExistingNPCs = new List<NPCInfo>(); // List containing all NPC templates created

    public List<NPCBehaviour> npcs = new List<NPCBehaviour>(); // List containing all NPCs spawned

    private int npcIdCount = 0; // Internal counter used to assign IDs to NPCs

    #region Unity Functions
    private void Awake()
    {
        // Singleton logic
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);
    }
    #endregion

    #region Manager Flow Functions
    /// <summary>
    /// Function used to generate NPCs at the begenning of the game<br/>
    /// Spawns and configures all the SyncVars for the NPCs
    /// </summary>
    [Server]
    public void GenerateNPCs()
    {
        for (npcIdCount = 0; npcIdCount < npcCount; npcIdCount++)
        {
            // Pick a random transform from the HexGridLayout (the tranformList contains only hexes that are not obstacles)
            Transform parentHexTransform = HexGridLayout.instance.transformList[Random.Range(0, HexGridLayout.instance.transformList.Count)];
            // Pick a random NPC template
            NPCInfo npcInfo = allExistingNPCs[Random.Range(0, allExistingNPCs.Count)];

            // Instantiate the chosen NPC template on the chosen hex
            GameObject spawnedNPC = Instantiate(npcInfo.npcPrefab, parentHexTransform.position, Quaternion.identity);
            spawnedNPC.name = $"NPC{npcIdCount}";

            NPCBehaviour npcBehaviour = spawnedNPC.GetComponent<NPCBehaviour>();
            if (npcBehaviour == null)
                Debug.LogError("NPC Prefab does not have NPCBehaviour script!");

            // Set reference to npcInfo
            npcBehaviour.npcInfo = npcInfo;

            // Set current state and create behaviour tree
            npcBehaviour.currentState = CreateBehaviourTree(npcBehaviour, npcInfo);

            // Set SyncVar value for NPC's name
            npcBehaviour.npcName.Value = npcInfo.npcName;
            npcBehaviour.npcName.NetworkManager = NetworkManager;
            npcBehaviour.npcName.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's ID
            npcBehaviour.npcId.Value = npcIdCount;
            npcBehaviour.npcId.NetworkManager = NetworkManager;
            npcBehaviour.npcId.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's type
            npcBehaviour.npcType.Value = (int)npcInfo.npcType;
            npcBehaviour.npcType.NetworkManager = NetworkManager;
            npcBehaviour.npcType.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's maximum health
            npcBehaviour.maxHealth.Value = npcInfo.health;
            npcBehaviour.maxHealth.NetworkManager = NetworkManager;
            npcBehaviour.maxHealth.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's current health
            npcBehaviour.currentHealth.Value = npcInfo.health;
            npcBehaviour.currentHealth.NetworkManager = NetworkManager;
            npcBehaviour.currentHealth.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's damage stat
            npcBehaviour.damage.Value = npcInfo.damage;
            npcBehaviour.damage.NetworkManager = NetworkManager;
            npcBehaviour.damage.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's defence stat
            npcBehaviour.defence.Value = npcInfo.defence;
            npcBehaviour.defence.NetworkManager = NetworkManager;
            npcBehaviour.defence.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's movement stat
            npcBehaviour.movement.Value = npcInfo.movement;
            npcBehaviour.movement.NetworkManager = NetworkManager;
            npcBehaviour.movement.NetworkBehaviour = npcBehaviour;

            // Set SyncVar value for NPC's critical chance stat
            npcBehaviour.critChance.Value = npcInfo.critChance;
            npcBehaviour.critChance.NetworkManager = NetworkManager;
            npcBehaviour.critChance.NetworkBehaviour = npcBehaviour;

            // Append all ids of the NPC's abilities into one string
            StringBuilder stringBuilder = new StringBuilder();
            foreach (AbilityInfo ability in npcInfo.abilities)
            {
                stringBuilder.Append(GameManager.instance.allExistingAbilities.IndexOf(ability));
                stringBuilder.Append(',');
            }

            // Set SyncVar value for NPC's abilities
            npcBehaviour.npcAbilities.Value = stringBuilder.ToString();
            npcBehaviour.npcAbilities.NetworkManager = NetworkManager;
            npcBehaviour.npcAbilities.NetworkBehaviour = npcBehaviour;

            // Add NPC to the manager NPC list
            npcs.Add(npcBehaviour);

            // Set parent for the spawned NPC
            spawnedNPC.transform.SetParent(transform);

            // Spawn NPC on the network
            ServerManager.Spawn(spawnedNPC, null);

            // Remove the hex used from the list as it is now occupied
            HexGridLayout.instance.transformList.Remove(parentHexTransform);

            // Occupy the hex
            HexGridLayout.instance.UpdateHex(parentHexTransform.name, spawnedNPC);

            // Set SyncVar value for NPC's current hex name
            npcBehaviour.currentHex.Value = parentHexTransform.name;
            npcBehaviour.currentHex.NetworkManager = NetworkManager;
            npcBehaviour.currentHex.NetworkBehaviour = npcBehaviour;
        }
    }

    private BehaviourStateBase CreateBehaviourTree(NPCBehaviour npc, NPCInfo npcInfo)
    {
        Dictionary<NPCInfo.NPCState, BehaviourStateBase> references = new Dictionary<NPCInfo.NPCState, BehaviourStateBase>();

        // Create the states and store them for reference
        foreach (var state in npcInfo.npcBehaviour)
        {
            BehaviourStateBase createdState = null;
            switch (state.state)
            {
                case NPCInfo.NPCState.Wander:
                    createdState = new WanderState(npc);
                    break;
                case NPCInfo.NPCState.Run:
                    createdState = new RunState(npc);
                    break;
                case NPCInfo.NPCState.Attack:
                    createdState = new AttackState(npc);
                    break;
                default:
                    break;
            }

            references.Add(state.state, createdState);
        }

        // Create connections
        foreach (var state in npcInfo.npcBehaviour)
        {
            List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>> connections = new List<Pair<BehaviourSwitchConditionBase, BehaviourStateBase>>();

            foreach (var connection in state.connections)
            {
                BehaviourSwitchConditionBase switchCondition = null;
                switch (connection.item1)
                {
                    case NPCInfo.NPCSwitchCondition.Hit:
                        switchCondition = new HitCondition(npc);
                        break;
                    case NPCInfo.NPCSwitchCondition.HitOrSeen:
                        switchCondition = new HitOrSeenCondition(npc);
                        break;
                    case NPCInfo.NPCSwitchCondition.FarFromThreat:
                        switchCondition = new FarFromThreatCondition(npc);
                        break;
                    case NPCInfo.NPCSwitchCondition.LowOnHealth:
                        switchCondition = new LowOnHealthCondition(npc);
                        break;
                    case NPCInfo.NPCSwitchCondition.Healthy:
                        switchCondition = new HealthyCondition(npc);
                        break;
                    default:
                        break;
                }

                connections.Add(new Pair<BehaviourSwitchConditionBase, BehaviourStateBase>(switchCondition, references[state.state]));
            }

            references[state.state].SetConnections(connections);
        }

        return references[npcInfo.npcBehaviour[0].state];
    }

    /// <summary>
    /// Function used to process the NPC turn. For each NPC it will call the ChooseAction function of the NPCBehaviour script, then it will end the NPC turn<br/>
    /// This shouldd only be called from the Server
    /// </summary>
    public void DoNPCTurn()
    {
        foreach (NPCBehaviour npc in npcs)
        {
            npc.ChooseAction();
        }

        GameManager.instance.NextTurn();
    }
    #endregion
}
