using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.CodeGenerating;
using FishNet.Component.Spawning;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Component.Spawning;
using System.Linq;

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

        public HexNode(int x, int y, GameObject hexObj)
        {
            this.x = x;
            this.y = y;
            q = x;
            r = y - (x - (x & 1)) / 2;
            s = -q - r;
            this.hexObj = hexObj;
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

    [Header("References")]
    [SerializeField] private PlayerSpawner pSpawner;
    private List<Transform> transformList;

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
        LayoutGrid();
    }

    private void LayoutGrid()
    {
        transformList = new List<Transform>();
        print("Starting layout");
        List<Transform> hexes = new List<Transform>();
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = new GameObject($"Hex {x},{y}", typeof(HexRenderer));
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int((int)transform.position.x + x, (int)transform.position.y + y));
                transformList.Add(tile.transform);
                hexes.Add(tile.transform);

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.occupying.NetworkManager = seed.NetworkManager;
                hexRenderer.occupying.NetworkBehaviour = hexRenderer;
                hexRenderer.SetMaterial(material, new Color(0, Random.Range(0f, 1f), 0, 1));
                hexRenderer.DrawMesh();
                tile.layer = gridLayer;
                tile.transform.SetParent(transform);

                hexNodes.Add(new HexNode(x, y, tile));

                SpawnTile(tile);
            }
        }
        pSpawner.Spawns = transformList.OrderBy(x => Random.value).ToArray();
        // pSpawner.Spawns = transformList.ToArray();
        spawner.Spawns = hexes.ToArray();
    }

    [ServerRpc]
    public void SpawnTile(GameObject tile)
    {
        tile.SetActive(true);
        ServerManager.Spawn(tile);
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
