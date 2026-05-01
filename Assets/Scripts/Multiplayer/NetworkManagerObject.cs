using FishNet.Component.Spawning;
using FishNet.Managing;
using Steamworks;
using UnityEngine;

public class NetworkManagerObject : MonoBehaviour
{
    public static NetworkManagerObject Instance; // Singleton

    [HideInInspector] public PlayerSpawner pSpawner; // Reference to the player spawner
    [HideInInspector] public FishySteamworks.FishySteamworks fishySteamworks; // Reference to fishy steamworks, handles the interactions between steamworks and fishnet
    [HideInInspector] public SteamManager steamManager; // Reference to steam manager, handles steam related stuff
    [HideInInspector] public NetworkManager networkManager; // Reference to network manager, handles fishnet networking

    [HideInInspector] public CSteamID mySteamID; // This is the steamID of the local player, useful for various steam related functions
    [HideInInspector] public CSteamID currentLobbyID; // This is the lobbyID of the lobby we are currently in, useful for various steam related functions

    public bool randomSeed; // Whether or not maps should be generated randomly
    public int seed; // This is the seed that will be passed on to the terrain generation

    private void Awake()
    {
        // We only want one instance of this object
        if (FindObjectOfType<NetworkManagerObject>() != null && gameObject != FindObjectOfType<NetworkManagerObject>().gameObject)
        {
            Destroy(gameObject);
            return;
        }
        
        // Set all of the references we need
        pSpawner = GetComponent<PlayerSpawner>();
        fishySteamworks = GetComponent<FishySteamworks.FishySteamworks>();
        steamManager = GetComponent<SteamManager>();
        networkManager = GetComponent<NetworkManager>();

        // Just singleton 
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // We do this here since the Steamworks system initializes in Awake
        mySteamID = SteamUser.GetSteamID();
    }
}
