using System.Collections.Generic;
using System.Linq;
using FishNet.CodeGenerating;
using FishNet.Component.Spawning;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Observing;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexGridLayout : NetworkBehaviour
{
    /// <summary>
    /// Class used to store information for the pathfinder
    /// </summary>
    public class HexNode
    {
        // Offset coordinates (Usual coordinates; used in name)
        public int x;
        public int y;

        // Cube coordinates (Used for distance calculation)
        public int q;
        public int r;
        public int s;

        // Used for pathfinding
        public float G;
        public float H;
        public float F => G + H;
        public HexNode connection;

        public GameObject hexObj;
        public HexRenderer hexRenderer;

        public HexNode(){}

        public HexNode(int x, int y, GameObject hexObj, HexRenderer hexRenderer)
        {
            this.x = x;
            this.y = y;
            q = x;
            r = y - (x - (x & 1)) / 2;
            s = -q - r;
            this.hexObj = hexObj;
            this.hexRenderer = hexRenderer;
        }

        /// <summary>
        /// Function used to calculate the distance between this HexNode and another
        /// </summary>
        /// <param name="other">Another HexNode</param>
        /// <returns>Distance between the 2 HexNodes</returns>
        public int Distance(HexNode other)
        {
            if (other == null)
            {
                Debug.LogError("Hex Node is null");
                return int.MaxValue;
            }

            int vecQ = q - other.q;
            int vecR = r - other.r;
            int vecS = s - other.s;

            return (Mathf.Abs(vecQ) + Mathf.Abs(vecR) + Mathf.Abs(vecS)) / 2;
        }

        /// <summary>
        /// Function used to get the neighbours of a HexNode
        /// </summary>
        /// <param name="hexes">List of all the hexes</param>
        /// <returns>List of hexes that are at a distance of 1 from the current hex</returns>
        public List<HexNode> GetNeighbours(List<HexNode> hexes)
        {
            return hexes.Where(h => h.Distance(this) == 1).ToList();
        }
    }
    public static HexGridLayout instance;

    [Header("Grid Config")]
    public Vector2Int gridSize; // Size of the grid
    [SerializeField] private float chanceToSpawnItem; // Chance to spawn an item on a hex

    [Header("Tile Config")]
    [SerializeField] private float innerSize; // Hex's inner size (If bigger than 0 it will create a hole in the middle of the hex; Most likely will not be used)
    [SerializeField] private float outerSize; // Hex's outer size (You can consider this as the pure size of the hex)
    [SerializeField] private float height; // Hex's height (Most likely will not be used since we now use prefabs for visuals)
    [SerializeField] private bool isFlatTopped; // Specifies if the top of the hex is flat or pointy
    [SerializeField] private int gridLayer; // Layer of the hex (TODO: May not be necessary as we also put the layer on the prefab)
    public int seed; // Seed used for the Random system
    [Range(0f, 0.4f)]
    public float obstacleThreshold; // Threshold from which a hex start to be considered an obstacle

    [Header("References")]
    [HideInInspector] public List<Transform> transformList; // List with the transforms of the hexes that are not obstacles or have items or have NPCs
    [SerializeField] private GameObject hexPrefab; // Prefab of the hex

    [Header("Visuals")]
    [SerializeField] private float visualHeightVariance; // Variance of the hexes' height

    [HideInInspector] public bool populateHexNodeList = true; // Bool used to specify if the HexRenderer should add the hex to the hex nodes list

    public List<HexNode> hexNodes = new List<HexNode>(); // List of all hex nodes
    private List<GameObject> spawnedItems = new List<GameObject>(); // List of all spawned items

    #region Unity Functions
    private void Awake()
    {
        // Singleton logic
        if (instance == null)
            instance = this;
        else
            Despawn(gameObject);
    }
    #endregion

    #region Grid Generation
    /// <summary>
    /// Function used to generate the grid by spawning hexes<br/>
    /// Shoul only be called on Server
    /// </summary>
    [Server]
    public void LayoutGrid()
    {
        // On the Server the Hex Node list is populated at the end of this function
        populateHexNodeList = false;

        transformList = new List<Transform>();
        print("Starting layout");
        // Get all spawnable items
        List<ItemInfo> spawnableItems = GameManager.instance.allExistingItems?.Where(item => item.isSpawnable).ToList();
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                // Calculate hex position
                Vector3 hexPosition = GetPositionForHexFromCoordinate(new Vector2Int((int)transform.position.x + x, (int)transform.position.y + y));
                // Generate the hex's color
                Color visualColor = GenerateColor();
                // Instantiate the prefab
                GameObject tile = Instantiate(hexPrefab);
                // Set the hex's name and position
                tile.name = $"Hex {x},{y}";
                tile.transform.position = hexPosition + Vector3.up * (-visualColor.g * visualHeightVariance + 0.51f);

                // Get HexRenderer component and set SyncVars
                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped.Value = isFlatTopped;
                hexRenderer.isFlatTopped.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.isFlatTopped.NetworkBehaviour = hexRenderer;

                hexRenderer.outerSize.Value = outerSize;
                hexRenderer.outerSize.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.outerSize.NetworkBehaviour = hexRenderer;

                hexRenderer.innerSize.Value = innerSize;
                hexRenderer.innerSize.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.innerSize.NetworkBehaviour = hexRenderer;

                hexRenderer.height.Value = height;
                hexRenderer.height.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.height.NetworkBehaviour = hexRenderer;

                hexRenderer.coords.Value = new Vector2Int(x, y);
                hexRenderer.coords.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.coords.NetworkBehaviour = hexRenderer;

                hexRenderer.occupying.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.occupying.NetworkBehaviour = hexRenderer;

                hexRenderer.originalColor.Value = visualColor;
                hexRenderer.originalColor.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.originalColor.NetworkBehaviour = hexRenderer;

                hexRenderer.hexVisualPrefab.Value = GetTileIndexBasedOnHeight(visualColor.g);
                hexRenderer.hexVisualPrefab.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.hexVisualPrefab.NetworkBehaviour = hexRenderer;

                // Randomly pick if and what item to spawn on the tile (only if the tile is not an obstacle)
                ItemInfo spawnItem = null;
                if (spawnableItems != null && spawnableItems.Count > 0)
                    spawnItem = Random.value <= chanceToSpawnItem && !hexRenderer.IsObstacle() ? spawnableItems[Random.Range(0, spawnableItems.Count)] : null;

                hexRenderer.hasItem.Value = spawnItem ? GameManager.instance.allExistingItems.IndexOf(spawnItem) : -1;
                hexRenderer.hasItem.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.hasItem.NetworkBehaviour = hexRenderer;
                
                hexRenderer.lingeringEffect.Value = null;
                hexRenderer.lingeringEffect.NetworkManager = NetworkManagerObject.Instance.networkManager;
                hexRenderer.lingeringEffect.NetworkBehaviour = hexRenderer;

                // Set hex's layer
                tile.layer = gridLayer;
                // Set hex's parent
                tile.transform.SetParent(transform);

                // Spawn hex on the network
                ServerManager.Spawn(tile, null);

                // If this hex has an item, instantiate it and spawn it on the network
                if (spawnItem)
                {
                    GameObject spawnedItem = Instantiate(spawnItem.prefab, tile.transform.position, Quaternion.identity);
                    spawnedItem.name = $"Item {x},{y}";
                    spawnedItem.transform.SetParent(transform);
                    spawnedItems.Add(spawnedItem);
                    ServerManager.Spawn(spawnedItem, null);
                }

                // If the hex is not an obstacle and doesn't have an item, add it to the tranform list
                if (spawnItem == null && !hexRenderer.IsObstacle())
                    transformList.Add(tile.transform);

                // Add the corresponding hex node to the list
                hexNodes.Add(new HexNode(x, y, tile, hexRenderer));
            }
        }
    }
    #endregion

    #region Hex Interaction
    /// <summary>
    /// Update the occupier of a hex
    /// </summary>
    /// <param name="hex">Name of the hex</param>
    /// <param name="occupier">New occupier</param>
    public void UpdateHex(string hex, GameObject occupier)
    {
        HexNode hexNode = hexNodes.Find(h => h.hexObj.name == hex);
        hexNode.hexRenderer.occupying.Value = occupier;
    }

    /// <summary>
    /// Function used to pick an item from a hex
    /// </summary>
    /// <param name="hex">Name of the hex</param>
    public void PickupItem(string hex)
    {
        // Determine the item name and search for it
        string item = "Item " + hex.Split(' ')[1];
        GameObject spawnedItem = spawnedItems.Find(i => i.name == item);
        if (spawnedItem != null)
        {
            // Despawn the item and remove from the list
            spawnedItem.GetComponent<NetworkObject>().Despawn();
            spawnedItems.Remove(spawnedItem);
        }

        // Update hasItem SyncVar
        HexNode hexNode = hexNodes.Find(h => h.hexObj.name == hex);
        hexNode.hexRenderer.hasItem.Value = -1;
    }

    /// <summary>
    /// Function used to place an item on a hex<br/>
    /// Should always be called on Server
    /// </summary>
    /// <param name="item">Index of the item to be placed</param>
    /// <param name="hex">Name of the hex</param>
    [Server]
    public void PlaceItem(int item, string hex)
    {
        // Search for the hex and update its hasItem SyncVar
        HexNode hexNode = hexNodes.Find(h => h.hexObj.name == hex);
        hexNode.hexRenderer.hasItem.Value = item;

        // Get corresponding item, instantiate it and spawn it across the network
        ItemInfo spawnItem = GameManager.instance.allExistingItems[item];
        GameObject spawnedItem = Instantiate(spawnItem.prefab, hexNode.hexObj.transform.position, Quaternion.identity);
        spawnedItem.name = "Item " + hex.Split(' ')[1];
        spawnedItem.transform.SetParent(transform);
        spawnedItems.Add(spawnedItem);
        ServerManager.Spawn(spawnedItem, null);

        GameManager.instance.EntityMovedClient(spawnedItem.name, hex, LayerMask.NameToLayer("Default"));
    }
    #endregion

    #region Utils
    /// <summary>
    /// Function used to calculate the correct position of the hex based on the coordinates received
    /// </summary>
    /// <param name="coordinate">Coordinates from layout generation</param>
    /// <returns>Correct position</returns>
    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float size = outerSize;

        if (!isFlatTopped)
        {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2f * size;

            horizontalDistance = width;
            verticalDistance = height * (3f / 4f);

            offset = shouldOffset ? width / 2 : 0;

            xPosition = column * horizontalDistance + offset;
            yPosition = row * verticalDistance;
        }
        else
        {
            shouldOffset = (column % 2) == 0;
            height = Mathf.Sqrt(3) * size;
            width = 2f * size;

            horizontalDistance = width * (3f / 4f);
            verticalDistance = height;

            offset = shouldOffset ? height / 2 : 0;

            xPosition = column * horizontalDistance;
            yPosition = row * verticalDistance - offset;
        }

        return new Vector3(xPosition, 0, -yPosition);
    }

    /// <summary>
    /// Used to generate color for tiles so that obstacles are easily distinguished
    /// </summary>
    /// <returns>New color for the hex</returns>
    private Color GenerateColor()
    {
        float g = Random.value < obstacleThreshold ? Random.Range(0, obstacleThreshold) : Random.Range(0.5f, 1f);
        return new Color(0, g, 0, 1);
    }

    /// <summary>
    /// TEMP: Used to pick the tiles based on height for now, but we will want to do this properly later<br/>
    /// TODO: Implement a proper system
    /// </summary>
    /// <returns>Returns index based on height</returns>
    private int GetTileIndexBasedOnHeight(float height)
    {
        if (height < obstacleThreshold / 2)
            return 3; // Grass tile
        if (height < obstacleThreshold)
            return 2; // Forest tile
        if (height < 0.75f)
            return 1; // Small Mountain
        return 0; // Tall Mountain tile
    }

    /// <summary>
    /// Function used to find the closest hex to the given position
    /// </summary>
    /// <param name="origin">Given position</param>
    /// <returns>Closest HexNode</returns>
    public HexNode GetClosestHex(Vector3 origin)
    {
        if (hexNodes == null || hexNodes.Count == 0)
            return null;

        float minDistance = float.MaxValue;
        HexNode closestHex = null;
        foreach (HexNode hex in hexNodes)
        {
            if (minDistance > Vector3.Distance(origin, hex.hexObj.transform.position))
            {
                minDistance = Vector3.Distance(origin, hex.hexObj.transform.position);
                closestHex = hex;
            }
        }
        return closestHex;
    }
    #endregion
}

