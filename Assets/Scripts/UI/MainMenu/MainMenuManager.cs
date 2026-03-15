using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [HideInInspector] public SettingsManager settingsManager;
    [HideInInspector] public LobbyManager lobbyManager;
    [HideInInspector] public GameObject activeWindow;

    [Header("References")]
    [SerializeField] private Button lobbyButton;
    [SerializeField] private GameObject lobbyWindow;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        Instance = this;
        settingsManager = GetComponentInChildren<SettingsManager>();
        lobbyManager = GetComponentInChildren<LobbyManager>();

        lobbyWindow.SetActive(false);
        settingsWindow.SetActive(false);

        lobbyButton.onClick.AddListener(() => {
            OpenWindow(lobbyWindow);
            lobbyManager.FetchLobbies();
        });
        settingsButton.onClick.AddListener(() => OpenWindow(settingsWindow));
        quitButton.onClick.AddListener(() => Application.Quit());
    }


    private void OpenWindow(GameObject window)
    {
        if (activeWindow != null)
        {
            activeWindow.SetActive(false);
            if (activeWindow == window)
            {
                activeWindow = null;
                return;
            }
        }
        activeWindow = window;
        activeWindow.SetActive(true);
    }
}
