using System.Collections.Generic;
using Steamworks;
using Unity.VisualScripting;
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
    }
    
    private List<LobbyInfo> foundLobbies = new List<LobbyInfo>();
    private float lobbyHeight;
    private float currentScroll = 0;

    private Callback<LobbyMatchList_t> lobbyMatchList;
    private Callback<LobbyCreated_t> lobbyCreated;
    public void FetchLobbies()
    {
        foundLobbies = new List<LobbyInfo>();
        // after getting lobbies we should rebuild the lobby window


        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "game",
            "Mercenaries of Hexlands",
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
                steamID = SteamMatchmaking.GetLobbyData(lobbyId, "hostSteamID")
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
            lobby.GetComponent<LobbyReferenceHolder>().joinButton.onClick.AddListener(() => JoinMatch(foundLobbies[i].steamID));
        }
    }

    private void HostMatch()
    {
        // load the lobby scene and start the server
        string steamID = SteamUser.GetSteamID().m_SteamID.ToString(); // FIXME: dunno if this works yet
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(steamID);
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);

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

        Debug.Log($"Lobby created: {lobbyID} with name {SteamFriends.GetPersonaName()}'s Lobby and host {SteamUser.GetSteamID().m_SteamID}");

        NetworkManagerObject.Instance.networkManager.ServerManager.StartConnection();
        NetworkManagerObject.Instance.networkManager.ClientManager.StartConnection();
    }

    private void JoinMatch(string steamID)
    {
        // connect to the lobby with the given steamID
        NetworkManagerObject.Instance.fishySteamworks.SetClientAddress(steamID);
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        Debug.Log($"Joining lobby with host {steamID}");
        NetworkManagerObject.Instance.networkManager.ClientManager.StartConnection();
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
