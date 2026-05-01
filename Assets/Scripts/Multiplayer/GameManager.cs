using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Transporting;
using System;
using TMPro;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance = null;
    
    [AllowMutableSyncType] public SyncVar<int> currentPlayerTurn = new SyncVar<int>(); // ID of the player whose turn it is
    public List<ItemInfo> allExistingItems = new List<ItemInfo>(); // List of all existing item templates
    public List<AbilityInfo> allExistingAbilities = new List<AbilityInfo>(); // List of all existing ability templates
    public List<GameObject> allExistingTiles = new List<GameObject>(); // List of all existing tile prefabs

    [HideInInspector] public event Action<int> OnBeginTurn; // Event triggered when a new player turn begins

    private List<int> clientsTurnOrder = new List<int>(); // List of all clients that are connected in order of their turns
    private List<int> clientsDead = new List<int>(); // List of clients that died
    private int turnOrderIndex = 0; // Internal turn counter

    private int alivePlayers = 0; // Number of players that are still alive
    private TMP_Text currentTurnText; // Reference to the current turn text object

    #region Unity + FishNet Functions
    private void Awake()
    {
        // Singleton logic
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);

        // Add callback for changing turn value
        currentPlayerTurn.OnChange += OnTurnChange;

        // Assign reference to current turn text object
        currentTurnText = GameObject.Find("CurrentTurnText").GetComponent<TMP_Text>();

        // Start coroutine to update current turn text
        StartCoroutine(UpdateTurnText(currentPlayerTurn.Value));
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        HexGridLayout.instance.seed = NetworkManagerObject.Instance.randomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : NetworkManagerObject.Instance.seed;
        UnityEngine.Random.InitState(HexGridLayout.instance.seed);

        // Add callback to handle players connecting or disconnecting
        ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;

        // Set default turn value to -1
        currentPlayerTurn.Value = -1;
        Debug.LogWarning("Current player turn set to -1");

        // Spawn hex tiles for terrain
        HexGridLayout.instance.LayoutGrid();

        // Spawn NPCs
        NPCManager.instance.GenerateNPCs();
    }

    private void Start()
    {
        // Set spawn positions for players
        NetworkManagerObject.Instance.pSpawner.Spawns = HexGridLayout.instance.transformList.OrderBy(x => UnityEngine.Random.value).ToArray();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        // Remove callbacks
        ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
        currentPlayerTurn.OnChange -= OnTurnChange;
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Callback used to handle player connecting or disconnecting from the server
    /// </summary>
    /// <param name="connection">NetworkConnection of the player</param>
    /// <param name="args">Additional arguments such as ConnectionState</param>
    private void OnPlayerConnectionChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        // Player connecting logic
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            alivePlayers++;
            // Add player to turn order list
            clientsTurnOrder.Add(connection.ClientId);
            // If this is the first player, set the current turn to him
            if (currentPlayerTurn.Value == -1)
            {
                currentPlayerTurn.Value = connection.ClientId;
                Debug.LogWarning("Current player turn set to " + connection.ClientId);
                BeginTurnClient(connection.ClientId);
            }
        }
        // Player disconnecting logic
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // If the player was not dead, decrement alive players counter
            if (!clientsDead.Contains(connection.ClientId))
                alivePlayers--;

            // Remove client from the list of dead clients
            clientsDead.Remove(connection.ClientId);

            // If it was this player's turn, switch to next turn
            if (clientsTurnOrder.IndexOf((int)connection.ClientId) == turnOrderIndex)
                NextTurn();

            // If this player's turn was already, just decrement turn order counter to reflect the updated list of players
            if (clientsTurnOrder.IndexOf((int)connection.ClientId) < turnOrderIndex)
                turnOrderIndex--;

            // Remove the player from the list
            clientsTurnOrder.Remove(connection.ClientId);

            // If there are no more players, change turn to default (-1)
            if (clientsTurnOrder.Count == 0)
            {
                turnOrderIndex = 0;
                currentPlayerTurn.Value = -1;
            }
        }
    }
    /// <summary>
    /// Callback used when the turn has changed to update the turn text
    /// </summary>
    /// <param name="oldVal">Previous turn</param>
    /// <param name="newVal">New turn</param>
    /// <param name="asServer">Bool to specify if the callback was called on the server</param>
    private void OnTurnChange(int oldVal, int newVal, bool asServer)
    {
        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection} - Turn changed from {oldVal} to {newVal}");

        StartCoroutine(UpdateTurnText(newVal));
    }
    #endregion

    #region RPCs
    /// <summary>
    /// Observers RPC used by the server to run the BeginTurn function on clients
    /// </summary>
    /// <param name="turn">ID of the player whose turn it is</param>
    [ObserversRpc]
    public void BeginTurnClient(int turn)
    {
        BeginTurn(turn);
    }
    #endregion

    #region Turn Handling Functions
    /// <summary>
    /// Function used to change to the next turn<br/>
    /// Shoul only be called on the Server
    /// </summary>
    public void NextTurn()
    {
        Debug.LogWarning("NextTurn start");
        // If there are no more players alive or in the game, turn should not change anymore
        if (clientsTurnOrder.Count > 0 && alivePlayers <= 0)
            return;

        // Increment internal counter
        turnOrderIndex++;

        // If the counter is equal to the count of the player list, the next turn should be the NPCs turn
        if (turnOrderIndex == clientsTurnOrder.Count)
        {
            Debug.LogWarning("Doing npc turn");
            currentPlayerTurn.Value = -2; // Set current turn value to -2 (NPC turn value)
            BeginTurnClient(-2);
            NPCManager.instance.DoNPCTurn();
            return;
        }
        // If the counter went past the NPC turn, it should be reset to 0, the first player
        if (turnOrderIndex > clientsTurnOrder.Count)
            turnOrderIndex = 0;

        Debug.LogWarning("NextTurn: " + turnOrderIndex + " " + clientsTurnOrder.Count + " alive players: " + alivePlayers);
        // Update current turn SyncVar
        currentPlayerTurn.Value = clientsTurnOrder[turnOrderIndex];

        // If the next player is dead his turn is skipped
        if (clientsDead.Contains(clientsTurnOrder[turnOrderIndex]))
        {
            Debug.LogWarning("NextTurn again because player ded");
            NextTurn();
            return;
        }

        Debug.LogWarning("NextTurn event");
        // Call ObserversRPC to begin turn on the clients
        BeginTurnClient(clientsTurnOrder[turnOrderIndex]);
    }
    /// <summary>
    /// IEnumerator used to update the current turn text
    /// </summary>
    /// <param name="turn">ID of the player whose turn it is</param>
    /// <returns>-</returns>
    private IEnumerator UpdateTurnText(int turn)
    {
        // If the turn is unassigned, place empty string
        if (turn == -1)
        {
            currentTurnText.text = "";
        }
        else if (turn == -2)
        {
            currentTurnText.text = "NPC turn";
        }
        else
        {
            // Search for player by iterating through all PlayerInfos from the scene
            bool foundPlayer = false;
            PlayerInfo[] playerInfos = FindObjectsOfType<PlayerInfo>();
            foreach (PlayerInfo playerInfo in playerInfos)
            {
                // If the owner of the PlayerInfo is the one whose turn it is, change the text using his name
                if (playerInfo.GetComponent<NetworkObject>().OwnerId == turn)
                {
                    // This wait is for when the player connects just as the game is starting and SyncVar is not updated.
                    // TODO: Maybe could be removed after the lobby changes
                    while (playerInfo.playerName.Value == "")
                        yield return null;

                    currentTurnText.text = playerInfo.playerName.Value + "'s turn";
                    foundPlayer = true;
                    break;
                }
            }

            // If player was not found leave text empty
            if (!foundPlayer)
                currentTurnText.text = "";
        }
    }

    
    /// <summary>
    /// Function called on the clients when the turn begins to trigger OnBeginTurn event
    /// </summary>
    /// <param name="turn">ID of the player whose turn it is</param>
    private void BeginTurn(int turn)
    {
        OnBeginTurn?.Invoke(turn);
    }
    #endregion

    #region Utils
    /// <summary>
    /// Function used to tell the GameManager that a player died<br/>
    /// Should only be called on the Server
    /// </summary>
    /// <param name="id">ID of the player who died</param>
    public void PlayerDied(int id)
    {
        print("A murit " + id);
        clientsDead.Add(id);
        alivePlayers--;
    }

    /// <summary>
    /// Function used to check if it is the turn of the client calling this<br/>
    /// Should only be called by the clients
    /// </summary>
    /// <returns>True if it is the players turn; False otherwise</returns>
    public bool IsMyTurn()
    {
        return currentPlayerTurn.Value == LocalConnection.ClientId;
    }

    /// <summary>
    /// Function used to check if it is the NPCs' turn
    /// </summary>
    /// <returns></returns>
    public bool IsNPCTurn()
    {
        return currentPlayerTurn.Value == -2;
    }
    #endregion
}
