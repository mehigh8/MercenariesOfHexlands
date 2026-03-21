using Steamworks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLobbyManager : MonoBehaviour
{
    public static GameLobbyManager Instance = null;

    [Header("References")]
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private Transform playerListContainer;

    [HideInInspector] public bool gameStarted = false;

    private Callback<LobbyDataUpdate_t> lobbyDataUpdate;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;


    private void OnLobbyDataUpdate(LobbyDataUpdate_t result)
    {
        if (result.m_ulSteamIDLobby == NetworkManagerObject.Instance.currentLobbyID.m_SteamID)
        {
            CheckGameStarted();
            CheckIfKicked();
        }
    }

    private void OnLobbyPlayerUpdate(LobbyChatUpdate_t result)
    {
        if (result.m_ulSteamIDLobby == NetworkManagerObject.Instance.currentLobbyID.m_SteamID)
            UpdatePlayerList();
    }

    private void CheckIfKicked()
    {
        string kicked = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "kick_" + NetworkManagerObject.Instance.mySteamID);

        if (kicked == "1")
        {
            SteamMatchmaking.LeaveLobby(NetworkManagerObject.Instance.currentLobbyID);
            NetworkManagerObject.Instance.currentLobbyID = new CSteamID(0);
            Debug.Log("I was kicked from the lobby");
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }

    private void CheckGameStarted()
    {
        if (gameStarted)
            return;
        Debug.LogWarning("checking game started");
        gameStarted = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "gameStarted") == "true";
        if (gameStarted)
        {
            Debug.LogWarning("Game has started");
            lobbyUI.SetActive(false);
            NetworkManagerObject.Instance.networkManager.ClientManager.StartConnection();
        }
    }

    private void KickPlayer(string steamID)
    {
        // if player is not host return
        if (SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID) != NetworkManagerObject.Instance.mySteamID)
            return;
        SteamMatchmaking.SetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "kick_" + steamID, "1");
    }

    private void StartMatch()
    {
        Debug.LogWarning("Starting match");
        SteamMatchmaking.SetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "gameStarted", "true");
    }

    private void BackToMenu()
    {
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID);
        if (lobbyOwner == NetworkManagerObject.Instance.mySteamID)
        {
            for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(lobbyOwner); i++)
            {
                CSteamID playerId = SteamMatchmaking.GetLobbyMemberByIndex(NetworkManagerObject.Instance.currentLobbyID, i);
                if (playerId != NetworkManagerObject.Instance.mySteamID)
                    KickPlayer(playerId.ToString());
            }
            NetworkManagerObject.Instance.networkManager.ClientManager.StopConnection();
            NetworkManagerObject.Instance.networkManager.ServerManager.StopConnection(false);
        }
        else
        {
            NetworkManagerObject.Instance.networkManager.ClientManager.StopConnection();
        }
        SteamMatchmaking.LeaveLobby(NetworkManagerObject.Instance.currentLobbyID);
        NetworkManagerObject.Instance.currentLobbyID = new CSteamID(0);
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);
        
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID);
        for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(NetworkManagerObject.Instance.currentLobbyID); i++)
        {
            CSteamID playerId = SteamMatchmaking.GetLobbyMemberByIndex(NetworkManagerObject.Instance.currentLobbyID, i);
            GameObject playerRow = Instantiate(playerRowPrefab, playerListContainer);
            float playerRowHeight = playerRow.GetComponent<RectTransform>().rect.height;
            playerRow.transform.position = playerListContainer.position - Vector3.up * (i * playerRowHeight);

            playerRow.GetComponentInChildren<TextMeshProUGUI>().text = SteamFriends.GetFriendPersonaName(playerId);

            Button kickButton = playerRow.GetComponentInChildren<Button>();
            if (lobbyOwner != NetworkManagerObject.Instance.mySteamID || playerId == NetworkManagerObject.Instance.mySteamID)
                kickButton.gameObject.SetActive(false);
            else
                kickButton.onClick.AddListener(() => KickPlayer(playerId.ToString()));
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable() {
        if (SteamMatchmaking.GetLobbyOwner(NetworkManagerObject.Instance.currentLobbyID) != NetworkManagerObject.Instance.mySteamID)
            startMatchButton.gameObject.SetActive(false);
        else
        {
            Debug.LogWarning("Im the host");
            startMatchButton.onClick.AddListener(StartMatch);
        }
        backToMenuButton.onClick.AddListener(BackToMenu);

        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyPlayerUpdate);
    }

    private void OnDisable()
    {
        startMatchButton.onClick.RemoveAllListeners();
        backToMenuButton.onClick.RemoveAllListeners();

        lobbyDataUpdate.Dispose();
        lobbyChatUpdate.Dispose();
    }

    private void Start()
    {
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(NetworkManagerObject.Instance.currentLobbyID, "lobbyName");
        UpdatePlayerList();
    }
}
