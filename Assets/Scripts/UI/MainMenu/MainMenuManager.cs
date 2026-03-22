using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("References")]
    [SerializeField] private Button lobbyButton; // Reference to the lobbies button
    [SerializeField] private GameObject lobbyWindow; // Reference to the window associated with the lobbies
    [SerializeField] private Button settingsButton; // Reference to the settings button
    [SerializeField] private GameObject settingsWindow; // Reference to the window associated with the settings
    [SerializeField] private Button quitButton; // Reference to the quit button

    [HideInInspector] public SettingsManager settingsManager; // Reference to the settings manager script
    [HideInInspector] public LobbyManager lobbyManager; // Reference to the lobby manager script
    
    [HideInInspector] public GameObject activeWindow; // The window inside the main menu that is currently active

#region Unity Functions
    private void Awake()
    {
        // Just singleton
        Instance = this;

        // Get the references for both managers
        settingsManager = GetComponentInChildren<SettingsManager>();
        lobbyManager = GetComponentInChildren<LobbyManager>();

        // Make sure all of the windows are closed
        lobbyWindow.SetActive(false);
        settingsWindow.SetActive(false);
        
        // Make sure buttons have no listeners to avoid duplicates
        lobbyButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        // Add relevant listeners to all buttons
        lobbyButton.onClick.AddListener(() => {
            OpenWindow(lobbyWindow);
            lobbyManager.FetchLobbies();
        });
        settingsButton.onClick.AddListener(() => OpenWindow(settingsWindow));
        quitButton.onClick.AddListener(() => Application.Quit());
    }
#endregion

#region Private Functions
    /// <summary>
    /// Function that handles the opening and closing of all windows present inside the main menu
    /// </summary>
    /// <param name="window">Reference to the window object we want to open</param>
    private void OpenWindow(GameObject window)
    {
        // If another window is already we want to close it
        if (activeWindow != null)
        {
            activeWindow.SetActive(false);

            // If the window we just closed coresponds to the button we just want to close it
            if (activeWindow == window)
            {
                activeWindow = null;
                return;
            }
        }

        // We enable the window we want to open
        activeWindow = window;
        activeWindow.SetActive(true);
    }
#endregion
}
