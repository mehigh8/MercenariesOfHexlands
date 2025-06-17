using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Component.Spawning;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Transporting;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance = null;

    private List<int> clientsTurnOrder = new List<int>();
    private List<int> clientsDead = new List<int>();
    private int turnOrderIndex = 0;
    [AllowMutableSyncType] public SyncVar<int> currentPlayerTurn = new SyncVar<int>();
    public List<ItemInfo> allExistingItems = new List<ItemInfo>();

    public event Action<int> OnBeginTurn;

    private int alivePlayers = 0;

    public bool IsMyTurn()
    {
        return currentPlayerTurn.Value == LocalConnection.ClientId;
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);
        currentPlayerTurn.OnChange += OnTurnChange;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;

        currentPlayerTurn.Value = -1;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
        currentPlayerTurn.OnChange -= OnTurnChange;
    }

    private void OnPlayerConnectionChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            alivePlayers++;
            clientsTurnOrder.Add(connection.ClientId);
            if (currentPlayerTurn.Value == -1)
            {
                currentPlayerTurn.Value = connection.ClientId;
                BeginTurnClient(connection.ClientId);
            }
        } else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            clientsDead.Remove(connection.ClientId);
            if (clientsTurnOrder.IndexOf((int)connection.ClientId) == turnOrderIndex)
            {
                currentPlayerTurn.Value = clientsTurnOrder[turnOrderIndex == clientsTurnOrder.Count - 1 ? 0 : turnOrderIndex + 1];
                BeginTurnClient(clientsTurnOrder[turnOrderIndex == clientsTurnOrder.Count - 1 ? 0 : turnOrderIndex + 1]);
            }
            if (clientsTurnOrder.IndexOf((int)connection.ClientId) < turnOrderIndex)
                    turnOrderIndex--;

            clientsTurnOrder.Remove(connection.ClientId);

            if (clientsTurnOrder.Count == 0)
            {
                turnOrderIndex = 0;
                currentPlayerTurn.Value = -1;
            }
        }
    }

    public void NextTurn()
    {
        print("NextTurn start");
        if (clientsTurnOrder.Count > 0 && alivePlayers <= 0)
            return;

        turnOrderIndex++;
        if (turnOrderIndex >= clientsTurnOrder.Count)
            turnOrderIndex = 0;

        currentPlayerTurn.Value = clientsTurnOrder[turnOrderIndex];

        if (clientsDead.Contains(clientsTurnOrder[turnOrderIndex]))
        {
            print("NextTurn again because player ded");
            NextTurn();
            return;
        }

        print("NextTurn event");
        BeginTurnClient(clientsTurnOrder[turnOrderIndex]);
    }

    private void OnTurnChange(int oldVal, int newVal, bool asServer)
    {
        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection} - Turn changed from {oldVal} to {newVal}");
    }

    [ObserversRpc]
    public void BeginTurnClient(int turn)
    {
        BeginTurn(turn);
    }

    private void BeginTurn(int turn)
    {
        OnBeginTurn?.Invoke(turn);
    }

    public void PlayerDied(int id)
    {
        print("A murit " + id);
        clientsDead.Add(id);
        alivePlayers--;
    }
}
