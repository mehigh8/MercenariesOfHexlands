using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLobbyManager : MonoBehaviour
{
    public static GameLobbyManager Instance = null;

    [Header("References")]
    [SerializeField] private GameObject lobbyUI; // Reference to the object that contains the entirety of the lobby UI, to be disabled when the game starts
    [SerializeField] private GameObject playerRowPrefab; // Prefab for the gameobject that will represent the players in the lobby
    [SerializeField] private Button startMatchButton; // Reference to the button that will start the game
    [SerializeField] private Button backToMenuButton; // Reference to the button that will take the player back to the main menu
    [SerializeField] private TextMeshProUGUI lobbyNameText; // Reference to the text that will display the lobby's name
    [SerializeField] private Transform playerListContainer; // Reference to the parent of the player rows

    [HideInInspector] public bool gameStarted = false; // Used by other scripts to check if the game has started

    // Callbacks objects that prevent the garbage collector from collecting our listeners to the steam lobby events
    private Callback<LobbyDataUpdate_t> lobbyDataUpdate; 
    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;

#region Unity Functions
    private void Awake()
    {
        // Just singleton
        Instance = this;
    }

    private void OnEnable()
    {
        // We want to only enable the start match button for the host
        if (SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID) != NetworkManagerObject.Instance.mySteamID)
            startMatchButton.gameObject.SetActive(false);
        else
            startMatchButton.onClick.AddListener(StartMatch);

        // Add the listener for the back to menu button for all players
        backToMenuButton.onClick.AddListener(BackToMenu);

        // Creating the callback listeners for the lobby events
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyPlayerUpdate);
    }

    private void Start()
    {
        // We add the lobby name here and initialize the player list
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "lobbyName");
        UpdatePlayerList();
    }

    private void OnDisable()
    {
        // General cleanup for all listeners and callbacks to avoid duplicates when reloading the scene
        startMatchButton.onClick.RemoveAllListeners();
        backToMenuButton.onClick.RemoveAllListeners();

        lobbyDataUpdate.Dispose();
        lobbyChatUpdate.Dispose();
    }
#endregion

#region Steam Callbacks
    /// <summary>
    /// This event is fired whenever the lobby data is updated, we currently check if:<br/>
    /// - the games has started<br/>
    /// - we got kicked from the lobby<br/>
    /// </summary>
    /// <param name="result"></param>
    private void OnLobbyDataUpdate(LobbyDataUpdate_t result)
    {
        // If to make sure we didn't receive an event from another lobby (idk how this would happen to be honest)
        if (result.m_ulSteamIDLobby == NetworkManagerObject.Instance.currentLobbyID.m_SteamID)
        {
            // Since we can't tell which data has been updated, we check both
            CheckGameStarted();
            CheckIfKicked();
        }
    }

    /// <summary>
    /// This event is fired whenever the number of players inside a lobby changes (join, kick, leave)
    /// </summary>
    /// <param name="result"></param>
    private void OnLobbyPlayerUpdate(LobbyChatUpdate_t result)
    {
        // If to make sure we didn't receive an event from another lobby (idk how this would happen to be honest)
        if (result.m_ulSteamIDLobby == NetworkManagerObject.Instance.currentLobbyID.m_SteamID)
            // We update the player list since the number of players inside the lobby has changed
            UpdatePlayerList();
    }
#endregion

#region Private Functions
    /// <summary>
    /// Function that checks if the current player was kicked <br/>
    /// This should only be called on callbacks since we don't know when the value updates
    /// </summary>
    private void CheckIfKicked()
    {
        // Get lobby data about current player kick status
        string kicked = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "kick_" + NetworkManagerObject.Instance.mySteamID);

        if (kicked == "1")
        {
            // If we are kicked we notify the lobby that we left, we clear connection data and load the main menu
            SteamMatchmaking.LeaveLobby(NetworkManagerObject.Instance.currentLobbyID);
            NetworkManagerObject.Instance.currentLobbyID = new CSteamID(0);
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Function that checks if the game has started <br/>
    /// This should only be called on callbacks since we don't know when the value updates
    /// </summary>
    private void CheckGameStarted()
    {
        // If the game has already started no point in checking again
        if (gameStarted)
            return;
        
        // Get lobby data about game started status
        gameStarted = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "gameStarted") == "true";

        if (gameStarted)
        {
            // If the game started we close the lobby UI and init the connection to the host
            lobbyUI.SetActive(false);
            NetworkManagerObject.Instance.networkManager.ClientManager.StartConnection();
        }
    }

    /// <summary>
    /// This function updates the player list, it clears the parent object then recreates the rows based on the client list
    /// </summary>
    private void UpdatePlayerList()
    {
        // Clear the parent object first before initializing the new list
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);
        
        // Getting player count
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID);

        for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(NetworkManagerObject.Instance.currentLobbyID); i++)
        {
            CSteamID playerId = SteamMatchmaking.GetLobbyMemberByIndex(NetworkManagerObject.Instance.currentLobbyID, i);

            // Setting the proper height for the player row
            GameObject playerRow = Instantiate(playerRowPrefab, playerListContainer);
            float playerRowHeight = playerRow.GetComponent<RectTransform>().rect.height;
            playerRow.transform.position = playerListContainer.position - Vector3.up * (i * playerRowHeight);

            // Updating the text to match the player name
            playerRow.GetComponentInChildren<TextMeshProUGUI>().text = SteamFriends.GetFriendPersonaName(playerId);

            // Make the kick button appear only for the host and not for the player themselves
            Button kickButton = playerRow.GetComponentInChildren<Button>();
            if (lobbyOwner != NetworkManagerObject.Instance.mySteamID || playerId == NetworkManagerObject.Instance.mySteamID)
                kickButton.gameObject.SetActive(false);
            else
                kickButton.onClick.AddListener(() => KickPlayer(playerId.ToString()));
        }
    }
#endregion

#region Button Listeners
    /// <summary>
    /// Function that sends kick data to the lobby, which will be checked inside the callback
    /// </summary>
    /// <param name="steamID">The Steam ID of the player to kick</param>
    private void KickPlayer(string steamID)
    {
        // One last failsafe to make sure only the host can kick players
        if (SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID) != NetworkManagerObject.Instance.mySteamID)
            return;
        
        // Setting lobby data to let the player know they got kicked
        SteamMatchmaking.SetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "kick_" + steamID, "1");
    }

    /// <summary>
    /// Function that sets the lobby data to let all the players know the game has started
    /// </summary>
    private void StartMatch()
    {
        SteamMatchmaking.SetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "gameStarted", "true");
    }

    /// <summary>
    /// Function that handles the back to menu logic
    /// </summary>
    private void BackToMenu()
    {
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID);
        // If we are the host we also want to kick all other players
        if (lobbyOwner == NetworkManagerObject.Instance.mySteamID)
        {
            // Get player count and kick all other players before leaving the lobby as host
            for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(NetworkManagerObject.Instance.currentLobbyID); i++)
            {
                CSteamID playerId = SteamMatchmaking.GetLobbyMemberByIndex(NetworkManagerObject.Instance.currentLobbyID, i);
                if (playerId != NetworkManagerObject.Instance.mySteamID)
                    KickPlayer(playerId.ToString());
            }
            // Terminate client connection (if connected) and server connection
            NetworkManagerObject.Instance.networkManager.ClientManager.StopConnection();
            NetworkManagerObject.Instance.networkManager.ServerManager.StopConnection(false);
        }
        else
        {
            // Disconnect from the server if connected
            NetworkManagerObject.Instance.networkManager.ClientManager.StopConnection();
        }
        // In both cases we want to leave the lobby, reset connection data and load the main menu
        SteamMatchmaking.LeaveLobby(NetworkManagerObject.Instance.currentLobbyID);
        NetworkManagerObject.Instance.currentLobbyID = new CSteamID(0);
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
#endregion