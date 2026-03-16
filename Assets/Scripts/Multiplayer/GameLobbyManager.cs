using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLobbyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private Transform playerListContainer;

    private void KickPlayer(string steamID)
    {
        UpdatePlayerList();
    }

    private void StartMatch()
    {
        
    }

    private void BackToMenu()
    {
        
    }

    private void UpdatePlayerList()
    {
        
    }

    public static GameLobbyManager Instance = null;

    private void Awake()
    {
        Instance = this;

        startMatchButton.onClick.AddListener(StartMatch);
        backToMenuButton.onClick.AddListener(BackToMenu);
    }

    private void Start()
    {
        
    }
}
