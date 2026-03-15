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
    [SerializeField] private int npcCount;
    [Space]
    public List<NPCInfo> allExistingNPCs = new List<NPCInfo>();

    public List<NPCBehaviour> npcs = new List<NPCBehaviour>();

    private int npcIdCount = 0;
    private bool hasGenerated = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);
    }

    [Server]
    public void GenerateNPCs()
    {
        if (hasGenerated)
            return;
        hasGenerated = true;

        for (npcIdCount = 0; npcIdCount < npcCount; npcIdCount++)
        {
            Transform parentHexTransform = HexGridLayout.instance.transformList[Random.Range(0, HexGridLayout.instance.transformList.Count)];
            NPCInfo npcInfo = allExistingNPCs[Random.Range(0, allExistingNPCs.Count)];

            GameObject spawnedNPC = Instantiate(npcInfo.npcPrefab, parentHexTransform.position, Quaternion.identity);
            spawnedNPC.name = $"NPC{npcIdCount}";

            NPCBehaviour npcBehaviour = spawnedNPC.GetComponent<NPCBehaviour>();
            if (npcBehaviour == null)
                Debug.LogError("NPC Prefab does not have NPCBehaviour script!");

            npcBehaviour.npcInfo = npcInfo;

            npcBehaviour.npcName.Value = npcInfo.npcName;
            npcBehaviour.npcName.NetworkManager = NetworkManager;
            npcBehaviour.npcName.NetworkBehaviour = npcBehaviour;

            npcBehaviour.npcId.Value = npcIdCount;
            npcBehaviour.npcId.NetworkManager = NetworkManager;
            npcBehaviour.npcId.NetworkBehaviour = npcBehaviour;

            npcBehaviour.npcType.Value = (int)npcInfo.npcType;
            npcBehaviour.npcType.NetworkManager = NetworkManager;
            npcBehaviour.npcType.NetworkBehaviour = npcBehaviour;

            npcBehaviour.maxHealth.Value = npcInfo.health;
            npcBehaviour.maxHealth.NetworkManager = NetworkManager;
            npcBehaviour.maxHealth.NetworkBehaviour = npcBehaviour;

            npcBehaviour.currentHealth.Value = npcInfo.health;
            npcBehaviour.currentHealth.NetworkManager = NetworkManager;
            npcBehaviour.currentHealth.NetworkBehaviour = npcBehaviour;

            npcBehaviour.damage.Value = npcInfo.damage;
            npcBehaviour.damage.NetworkManager = NetworkManager;
            npcBehaviour.damage.NetworkBehaviour = npcBehaviour;

            npcBehaviour.defence.Value = npcInfo.defence;
            npcBehaviour.defence.NetworkManager = NetworkManager;
            npcBehaviour.defence.NetworkBehaviour = npcBehaviour;

            npcBehaviour.movement.Value = npcInfo.movement;
            npcBehaviour.movement.NetworkManager = NetworkManager;
            npcBehaviour.movement.NetworkBehaviour = npcBehaviour;

            npcBehaviour.critChance.Value = npcInfo.critChance;
            npcBehaviour.critChance.NetworkManager = NetworkManager;
            npcBehaviour.critChance.NetworkBehaviour = npcBehaviour;

            StringBuilder stringBuilder = new StringBuilder();
            foreach (AbilityInfo ability in npcInfo.abilities)
            {
                stringBuilder.Append(GameManager.instance.allExistingAbilities.IndexOf(ability));
                stringBuilder.Append(',');
            }

            npcBehaviour.npcAbilities.Value = stringBuilder.ToString();
            npcBehaviour.npcAbilities.NetworkManager = NetworkManager;
            npcBehaviour.npcAbilities.NetworkBehaviour = npcBehaviour;

            npcs.Add(npcBehaviour);

            spawnedNPC.transform.SetParent(transform);

            ServerManager.Spawn(spawnedNPC, null);

            HexGridLayout.instance.transformList.Remove(parentHexTransform);
        }

        NetworkManagerObject.Instance.pSpawner.Spawns = HexGridLayout.instance.transformList.OrderBy(x => Random.value).ToArray();
    }
}
