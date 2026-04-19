using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    /// <summary>
    /// Struct that stores only information relevant to us for displaying and connecting to lobbies
    /// </summary>
    struct LobbyInfo
    {
        public string name; // Name of the lobby
        public int playerCount; // Current number of players in the lobby
        public string steamID; // SteamID of the host, used for connecting via fishy steamworks
        public CSteamID lobbyID; // LobbyID of lobby, used for connecting to the lobby via steamworks
    }

    [Header("References")]
    [SerializeField] private GameObject lobbyPrefab; // Prefab for lobby entries in the lobby list
    [SerializeField] private Transform lobbyContainer; // Reference to the parent that will hold lobby entries
    [SerializeField] private RectTransform lobbyMask; // Reference to the mask of the lobby list, used for calculating scroll limits based on its height
    [SerializeField] private Button hostMatchButton; // Reference to the host match button
    [SerializeField] private Button refreshButton; // Reference to the refresh button, it fetches the lobbies again when clicked

    [Header("Settings")]
    [SerializeField] private float scrollSpeed = 50000f; // FIXME: this is really jank, we should probably use a proper scroll rect at some point

    private List<LobbyInfo> foundLobbies = new List<LobbyInfo>(); // Object used to store the lobbies we find
    private float lobbyHeight; // Height of a single lobby entry, used for calculating scroll limits and positioning lobby entries
    private float currentScroll = 0; // Stores the current amount of scroll in the lobby list, used for scrolling and calculating scroll limits

    // Callbacks objects that prevent the garbage collector from collecting our listeners to the steam lobby events
    private Callback<LobbyMatchList_t> lobbyMatchList;
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyJoined;

#region Unity Functions
    void Start()
    {
        refreshButton.onClick.RemoveAllListeners();
        hostMatchButton.onClick.RemoveAllListeners();

        // Here we add the necessary listeners and bind our callback objects
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnFetchLobbyList);
        lobbyJoined = Callback<LobbyEnter_t>.Create(OnLobbyJoined);

        refreshButton.onClick.AddListener(FetchLobbies);
        hostMatchButton.onClick.AddListener(HostMatch);
    }

    void Update()
    {
        ScrollLobbyWindow();
    }

    void OnDisable()
    {
        lobbyCreated.Dispose();
        lobbyMatchList.Dispose();
        lobbyJoined.Dispose();
    }
    #endregion

    #region Steam Callbacks
    /// <summary>
    /// This function is called when we successfully join a lobby, either by hosting or by joining an existing one
    /// </summary>
    /// <param name="result"></param>
    private void OnLobbyJoined(LobbyEnter_t result)
    {
        Debug.Log("Received lobby join callback");
        CSteamID lobbyID = new CSteamID(result.m_ulSteamIDLobby);
        // Check that the lobby is still joinable (either if it is full or it has already started). Edge case if our fetched lobby list is outdated
        if (SteamMatchmaking.GetNumLobbyMembers(lobbyID) >= 4 || SteamMatchmaking.GetLobbyData(lobbyID, "gameStarted") == "true")
        {        
            SteamMatchmaking.LeaveLobby(lobbyID);
            Debug.LogWarning("Lobby is full or game already started");
            FetchLobbies();
            return;
        }

        Debug.Log("Trying to load scene");
        // FIXME: currently we load the scene that is most up to date, but we should load the game scene at some point
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        // If we are the host of the match we also want to start the server to allow players to join
        if (NetworkManagerObject.Instance.mySteamID == SteamMatchmaking.GetLobbyOwner((CSteamID)result.m_ulSteamIDLobby))
            NetworkManagerObject.Instance.networkManager.ServerManager.StartConnection();
        
        // We store the current lobby id since we have successfully joined a lobby
        NetworkManagerObject.Instance.currentLobbyID = (CSteamID)result.m_ulSteamIDLobby;
        Debug.Log("Successfully joined lobby " + NetworkManagerObject.Instance.currentLobbyID);
    }

    /// <summary>
    /// This function is called when we receive the lobby list
    /// </summary>
    /// <param name="result"></param>
    void OnFetchLobbyList(LobbyMatchList_t result)
    {
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

            // Here we filter out lobbies we have been kicked from, since we can't add this as a filter for the initial query
            string value = SteamMatchmaking.GetLobbyData(lobbyId, "kick_" + NetworkManagerObject.Instance.mySteamID);

            if (value == "1")
                continue;

            // Here we hand pick the information that we want to display and use for connection
            foundLobbies.Add(new LobbyInfo()
            {
                name = SteamMatchmaking.GetLobbyData(lobbyId, "lobbyName"),
                playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
                steamID = SteamMatchmaking.GetLobbyData(lobbyId, "hostSteamID"),
                lobbyID = lobbyId
            });
        }
        Debug.LogWarning("Updating lobby list");
        // After fetching a new list we also want to update the lobby window to display the new list
        UpdateLobbyWindow();
    }

    /// <summary>
    /// This function is called when the lobby we are trying to host has been successfully created
    /// </summary>
    /// <param name="callback"></param>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        // Check if the lobby was created successfully
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby creation failed");
            return;
        }

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Here we set the lobby data so other clients can find and join the lobby properly
        SteamMatchmaking.SetLobbyData(lobbyID, "game", "Mercenaries of Hexlands");
        SteamMatchmaking.SetLobbyData(lobbyID, "lobbyName", SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(lobbyID, "hostSteamID", SteamUser.GetSteamID().m_SteamID.ToString());
        SteamMatchmaking.SetLobbyData(lobbyID, "gameStarted", "false");

        Debug.Log($"Lobby created: {lobbyID} with name {SteamFriends.GetPersonaName()}'s Lobby and host {SteamUser.GetSteamID().m_SteamID}");
    }
#endregion

#region Private Functions
    /// <summary>
    /// Function that creates the query for fetching the lobbies and adds filters for the search
    /// </summary>
    public void FetchLobbies()
    {
        Debug.LogWarning("Fetching lobbies");
        foundLobbies = new List<LobbyInfo>();

        // This is a temp filter for using the steam test app
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "game",
            "Mercenaries of Hexlands",
            ELobbyComparison.k_ELobbyComparisonEqual
        );
        // We also want to filter out lobbies that have already started their game
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "gameStarted",
            "false",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        // Finally we send the request to fetch the lobbies with the applied filters
        SteamMatchmaking.RequestLobbyList();
    }

    /// <summary>
    /// Function that clears the current lobby list and creates a new one based on the query
    /// </summary>
    private void UpdateLobbyWindow()
    {
        // Clear old lobby list
        foreach (Transform child in lobbyContainer)
            Destroy(child.gameObject);

        // Reset the scroll amount
        currentScroll = 0;
        lobbyContainer.transform.localPosition = new Vector3(lobbyContainer.transform.localPosition.x, 0, lobbyContainer.transform.localPosition.z);

        if (foundLobbies.Count == 0)
            return;

        for (int i = 0; i < foundLobbies.Count; i++)
        {
            GameObject lobby = Instantiate(lobbyPrefab, lobbyContainer);

            // We set the position of the lobby object based on its index and height
            lobbyHeight = lobby.GetComponent<RectTransform>().rect.height;
            lobby.transform.position = lobbyContainer.transform.position - Vector3.up * (i * lobbyHeight);

            LobbyReferenceHolder holder = lobby.GetComponent<LobbyReferenceHolder>();

            // Setting display information about the lobby
            holder.lobbyName.text = foundLobbies[i].name;
            holder.playerCount.text = $"{foundLobbies[i].playerCount}/4";

            // We need this to create a copy, otherwise the reference to i will be lost after the loop finished
            LobbyInfo lobbyInfo = foundLobbies[i];
            holder.joinButton.onClick.AddListener(() => JoinMatch(lobbyInfo));
        }
    }

    /// <summary>
    /// Handles the scroll wheel input of the lobbies [THIS WILL BE REMOVED WHEN WE ADD SCROLL RECT]
    /// </summary>
    private void ScrollLobbyWindow()
    {
        // We only want to scroll when the lobby window is open
        if (MainMenuManager.Instance.activeWindow != gameObject)
            return;
        
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentScroll -= scrollInput * scrollSpeed * Time.deltaTime;
            
            // We want to clamp the value of the scroll between the min and max value of the scroll
            currentScroll = Mathf.Clamp(currentScroll, 0, Mathf.Max(0, foundLobbies.Count * lobbyHeight - lobbyMask.rect.height));
            lobbyContainer.transform.localPosition = new Vector3(lobbyContainer.transform.localPosition.x, currentScroll, lobbyContainer.transform.localPosition.z);
        }
    }
#endregion

#region Button Functions
    /// <summary>
    /// Function that hosts a match and calls steamworks events associated with it
    /// </summary>
    private void HostMatch()
    {
        // Bind the server address to the client's steamID
        Debug.Log($"Hosting lobby with steamID {NetworkManagerObject.Instance.mySteamID}");
        string steamID = NetworkManagerObject.Instance.mySteamID.ToString();
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(steamID);
        Debug.Log("Bound client address to steamID " + steamID);

        // Send request to steamworks that we want to create this lobby
        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypePublic,
            4 // TODO: maybe we want this to be more idk, we'll see after we have a proper server hosting window
        );
        Debug.Log("Sent create lobby request");
    }

    /// <summary>
    /// Function associated with each lobby entry that is called when the join button is pressed
    /// </summary>
    /// <param name="lobbyInfo">The lobby associated with the button call</param>
    private void JoinMatch(LobbyInfo lobbyInfo)
    {
        // Send request to steamworks that we want to join the lobby
        SteamMatchmaking.JoinLobby(lobbyInfo.lobbyID);

        // Bind the steamID of the host to our server connection
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(lobbyInfo.steamID);
        Debug.Log($"Joining lobby with host {lobbyInfo.steamID}");
    }
#endregion
}