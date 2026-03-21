using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject lobbyPrefab;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private RectTransform lobbyMask;
    [SerializeField] private Button hostMatchButton;
    [SerializeField] private Button RefreshButton;

    [Header("Settings")]
    [SerializeField] private float scrollSpeed = 10f;


    struct LobbyInfo
    {
        public string name;
        public int playerCount;
        public string steamID;
        public CSteamID lobbyID;
    }
    
    private List<LobbyInfo> foundLobbies = new List<LobbyInfo>();
    private float lobbyHeight;
    private float currentScroll = 0;

    private Callback<LobbyMatchList_t> lobbyMatchList;
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyJoined;

    private void OnLobbyJoined(LobbyEnter_t result)
    {
        // if we are the host 
        if (NetworkManagerObject.Instance.mySteamID == SteamMatchmaking.GetLobbyOwner((CSteamID)result.m_ulSteamIDLobby))
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            NetworkManagerObject.Instance.networkManager.ServerManager.StartConnection();
        }
        else
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
        NetworkManagerObject.Instance.currentLobbyID = (CSteamID)result.m_ulSteamIDLobby;
        Debug.LogWarning("Successfully joined lobby " + NetworkManagerObject.Instance.currentLobbyID);
    }
    public void FetchLobbies()
    {
        foundLobbies = new List<LobbyInfo>();
        // after getting lobbies we should rebuild the lobby window


        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "game",
            "Mercenaries of Hexlands",
            ELobbyComparison.k_ELobbyComparisonEqual
        );
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "gameStarted",
            "false",
            ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.RequestLobbyList();
    }

    void OnLobbyMatchList(LobbyMatchList_t result)
    {
        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

            foundLobbies.Add(new LobbyInfo()
            {
                name = SteamMatchmaking.GetLobbyData(lobbyId, "lobbyName"),
                playerCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId),
                steamID = SteamMatchmaking.GetLobbyData(lobbyId, "hostSteamID"),
                lobbyID = lobbyId
            });
        }
        UpdateLobbyWindow();
    }

    private void UpdateLobbyWindow()
    {
        foreach (Transform child in lobbyContainer)
        {
            Destroy(child.gameObject);
        }

        currentScroll = 0;
        lobbyContainer.transform.localPosition = new Vector3(lobbyContainer.transform.localPosition.x, 0, lobbyContainer.transform.localPosition.z);

        if (foundLobbies.Count == 0)
            return;

        for (int i = 0; i < foundLobbies.Count; i++)
        {
            GameObject lobby = Instantiate(lobbyPrefab, lobbyContainer);
            lobbyHeight = lobby.GetComponent<RectTransform>().rect.height;
            lobby.transform.position = lobbyContainer.transform.position - Vector3.up * (i * lobbyHeight);
            lobby.GetComponent<LobbyReferenceHolder>().lobbyName.text = foundLobbies[i].name;
            lobby.GetComponent<LobbyReferenceHolder>().playerCount.text = $"{foundLobbies[i].playerCount}/4";
            LobbyInfo lobbyInfo = foundLobbies[i];
            lobby.GetComponent<LobbyReferenceHolder>().joinButton.onClick.AddListener(() => JoinMatch(lobbyInfo));
        }
    }

    private void HostMatch()
    {
        // load the lobby scene and start the server
        string steamID = SteamUser.GetSteamID().m_SteamID.ToString();
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(steamID);

        SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypePublic,
            4 // max players
        );
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.Log("Lobby creation failed");
            return;
        }

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(lobbyID, "game", "Mercenaries of Hexlands");
        SteamMatchmaking.SetLobbyData(lobbyID, "lobbyName", SteamFriends.GetPersonaName() + "'s Lobby");
        SteamMatchmaking.SetLobbyData(lobbyID, "hostSteamID", SteamUser.GetSteamID().m_SteamID.ToString());
        SteamMatchmaking.SetLobbyData(lobbyID, "gameStarted", "false");

        Debug.Log($"Lobby created: {lobbyID} with name {SteamFriends.GetPersonaName()}'s Lobby and host {SteamUser.GetSteamID().m_SteamID}");
    }

    private void JoinMatch(LobbyInfo lobbyInfo)
    {
        // check one last time if the lobby is still joinable
        if (SteamMatchmaking.GetNumLobbyMembers(lobbyInfo.lobbyID) >= 4 || SteamMatchmaking.GetLobbyData(lobbyInfo.lobbyID, "gameStarted") == "true")
        {
            Debug.LogWarning("Lobby is full or game already started");
            FetchLobbies();
            return;
        }

        // connect to the lobby with the given steamID
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(lobbyInfo.steamID);
        SteamMatchmaking.JoinLobby(lobbyInfo.lobbyID);
        Debug.Log($"Joining lobby with host {lobbyInfo.steamID}");
    }

    private void ScrollLobbyWindow()
    {
        if (MainMenuManager.Instance.activeWindow != gameObject)
            return;
        
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentScroll -= scrollInput * scrollSpeed * Time.deltaTime;
            currentScroll = Mathf.Clamp(currentScroll, 0, Mathf.Max(0, foundLobbies.Count * lobbyHeight - lobbyMask.rect.height));
            lobbyContainer.transform.localPosition = new Vector3(lobbyContainer.transform.localPosition.x, currentScroll, lobbyContainer.transform.localPosition.z);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        RefreshButton.onClick.AddListener(FetchLobbies);
        hostMatchButton.onClick.AddListener(HostMatch);

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyJoined = Callback<LobbyEnter_t>.Create(OnLobbyJoined);
    }

    private void OnDisable()
    {   
        // lobbyCreated.Dispose(); 
        // lobbyMatchList.Dispose();
        // lobbyJoined.Dispose();

        RefreshButton.onClick.RemoveAllListeners();
        hostMatchButton.onClick.RemoveAllListeners();
    }

    // Update is called once per frame
    void Update()
    {
        ScrollLobbyWindow();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            foundLobbies.Add(new LobbyInfo() { name = "Lobby " + (foundLobbies.Count + 1), playerCount = Random.Range(1, 5), steamID = Random.Range(1000000000, 9999999999).ToString() });
            UpdateLobbyWindow();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (foundLobbies.Count > 0)
                foundLobbies.RemoveAt(foundLobbies.Count - 1);
            UpdateLobbyWindow();
        }
#endif
    }
}
