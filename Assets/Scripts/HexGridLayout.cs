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

        public int Distance(HexNode other)
        {
            int vecQ = q - other.q;
            int vecR = r - other.r;
            int vecS = s - other.s;

            return (Mathf.Abs(vecQ) + Mathf.Abs(vecR) + Mathf.Abs(vecS)) / 2;
        }

        public List<HexNode> GetNeighbours(List<HexNode> hexes)
        {
            return hexes.Where(h => h.Distance(this) == 1).ToList();
        }
    }
    public static HexGridLayout instance;

    [Header("Grid Config")]
    public Vector2Int gridSize;

    [Header("Tile Config")]
    public float innerSize;
    public float outerSize;
    public float height;
    public bool isFlatTopped;
    public Material material;
    public int gridLayer;
    [AllowMutableSyncType] public SyncVar<int> seed;
    public List<HexNode> hexNodes = new List<HexNode>();

    [Header("References")]
    [SerializeField] public PlayerSpawner pSpawner;
    public List<Transform> transformList;
    [SerializeField] private GameObject hexPrefab;

    public HexRenderer GetClosestHex(Vector3 origin)
    {
        if (transformList == null || transformList.Count == 0)
            return null;
        float minDistance = 0;
        Transform minTransform = null;
        foreach (Transform t in transformList)
        {
            if (minTransform == null)
            {
                minDistance = Vector3.Distance(origin, t.position);
                minTransform = t;
            }
            else if (minDistance > Vector3.Distance(origin, t.position))
            {
                minDistance = Vector3.Distance(origin, t.position);
                minTransform = t;
            }
        }
        return minTransform.GetComponent<HexRenderer>();
    }

    private void Awake()
    {
        Random.InitState(seed.Value);
        instance = this;
    }

    private void Start()
    {
        LayoutGrid();
    }

    private bool isGenerated;
    [Server]
    private void LayoutGrid()
    {
        if (isGenerated)
            return;
        isGenerated = true;
        transformList = new List<Transform>();
        print("Starting layout");
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = Instantiate(hexPrefab);
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int((int)transform.position.x + x, (int)transform.position.y + y));

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped.Value = isFlatTopped;
                hexRenderer.isFlatTopped.NetworkManager = seed.NetworkManager;
                hexRenderer.isFlatTopped.NetworkBehaviour = hexRenderer;

                hexRenderer.outerSize.Value = outerSize;
                hexRenderer.outerSize.NetworkManager = seed.NetworkManager;
                hexRenderer.outerSize.NetworkBehaviour = hexRenderer;

                hexRenderer.innerSize.Value = innerSize;
                hexRenderer.innerSize.NetworkManager = seed.NetworkManager;
                hexRenderer.innerSize.NetworkBehaviour = hexRenderer;
                
                hexRenderer.height.Value = height;
                hexRenderer.height.NetworkManager = seed.NetworkManager;
                hexRenderer.height.NetworkBehaviour = hexRenderer;

                hexRenderer.coords.Value = new Vector2Int(x, y);
                hexRenderer.coords.NetworkManager = seed.NetworkManager;
                hexRenderer.coords.NetworkBehaviour = hexRenderer;

                hexRenderer.occupying.NetworkManager = seed.NetworkManager;
                hexRenderer.occupying.NetworkBehaviour = hexRenderer;

                hexRenderer.originalColor.Value = new Color(0, Random.Range(0f, 1f), 0, 1);
                hexRenderer.originalColor.NetworkManager = seed.NetworkManager;
                hexRenderer.originalColor.NetworkBehaviour = hexRenderer;

                tile.layer = gridLayer;
                tile.transform.SetParent(transform);


                ServerManager.Spawn(tile, null);
                // SpawnTile(tile);
            }
        }
        // pSpawner.Spawns = transformList.ToArray();
    }

    public void UpdateHex(string hex, GameObject occupier)
    {
        HexNode hexNode = hexNodes.Find(h => h.hexObj.name == hex);
        hexNode.hexRenderer.occupying.Value = occupier;
    }

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
}

