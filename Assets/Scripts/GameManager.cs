using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Spawning;

public class GameManager : NetworkBehaviour
{
    [Header("References")]
    public HexGridLayout hexGrid;
    public PlayerSpawner spawner;
    [Space]
    public readonly SyncVar<bool> terrainIsCreated = new SyncVar<bool>();
    public static GameManager instance = null;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);
    }

    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    if (terrainIsCreated.Value == false)
    //    {
    //        Debug.Log("Changing syncvar");
    //        terrainIsCreated.Value = true;
    //        Debug.Log(terrainIsCreated.Value);
    //        hexGrid.LayoutGrid(spawner);
    //    }
    //}
}
