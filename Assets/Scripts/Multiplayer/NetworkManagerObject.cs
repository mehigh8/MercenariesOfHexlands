using FishNet.Component.Spawning;
using FishNet.Managing;
using UnityEngine;

public class NetworkManagerObject : MonoBehaviour
{
    public static NetworkManagerObject Instance;
    [HideInInspector] public PlayerSpawner pSpawner;
    [HideInInspector] public FishySteamworks.FishySteamworks fishySteamworks;
    [HideInInspector] public SteamManager steamManager;
    [HideInInspector] public NetworkManager networkManager;

    private void Awake()
    {
        if (FindObjectOfType<NetworkManagerObject>() != null && gameObject != FindObjectOfType<NetworkManagerObject>().gameObject)
        {
            Destroy(gameObject);
            return;
        }
        pSpawner = GetComponent<PlayerSpawner>();
        fishySteamworks = GetComponent<FishySteamworks.FishySteamworks>();
        steamManager = GetComponent<SteamManager>();
        networkManager = GetComponent<NetworkManager>();

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
