using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHandler : NetworkBehaviour
{
    [AllowMutableSyncType] public SyncVar<string> itemName;
    void Start()
    {
        gameObject.name = itemName.Value;
        if (IsServerInitialized)
            GameManager.instance.EntityMovedClient(gameObject.name, "Hex " + gameObject.name.Split(' ')[1], LayerMask.NameToLayer("Default"));
    }
}
