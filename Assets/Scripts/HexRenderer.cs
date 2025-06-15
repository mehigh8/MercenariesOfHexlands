using FishNet.CodeGenerating;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
// [RequireComponent(typeof(NetworkObject))]
public class HexRenderer : NetworkBehaviour
{
    public struct Face
    {
        public List<Vector3> vertices;
        public List<int> triangles;
        public List<Vector2> uvs;

        public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
        }
    }

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private List<Face> faces;

    public Material material;
    [AllowMutableSyncType] public SyncVar<float> innerSize;
    [AllowMutableSyncType] public SyncVar<float> outerSize;
    [AllowMutableSyncType] public SyncVar<float> height;
    [AllowMutableSyncType] public SyncVar<bool> isFlatTopped;
    [AllowMutableSyncType] public SyncVar<Vector2Int> coords;
    [AllowMutableSyncType] public SyncVar<GameObject> occupying = new SyncVar<GameObject>();
    [AllowMutableSyncType] public SyncVar<UnityEngine.Color> originalColor;
    [AllowMutableSyncType] public SyncVar<int> hasItem; // This is the index of the item from all existing items (list found in GameManager) because FishNet doesn't support Sprites :(

    private void OnOccupyingChange(GameObject oldVal, GameObject newVal, bool asServer)
    {
        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection.ClientId} - {gameObject.name} changed from {oldVal} to {newVal}");
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        occupying.OnChange += OnOccupyingChange;
    }

    public override void OnStopNetwork()
    {
        base.OnStartNetwork();

        occupying.OnChange -= OnOccupyingChange;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeOccupying(GameObject occupier)
    {
        print(gameObject);
        occupying.Value = occupier;
    }

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer.material = material;

        gameObject.name = $"Hex {coords.Value.x},{coords.Value.y}";
        HexGridLayout.instance.transformList.Add(transform);
        HexGridLayout.instance.hexNodes.Add(new HexGridLayout.HexNode(coords.Value.x, coords.Value.y, gameObject, this));

        transform.parent = HexGridLayout.instance.transform;
        if (HexGridLayout.instance.transform.childCount == HexGridLayout.instance.gridSize.x * HexGridLayout.instance.gridSize.y)
            HexGridLayout.instance.pSpawner.Spawns = HexGridLayout.instance.transformList.OrderBy(x => Random.value).ToArray();

        DrawMesh();
    }

    public void DrawMesh()
    {
        mesh = new Mesh();
        mesh.name = "Hex";

        meshFilter.mesh = mesh;
        meshRenderer.material.color = originalColor.Value;
        DrawFaces();
        CombineFaces();
    }

    private void DrawFaces()
    {
        faces = new List<Face>();

        // Top faces
        for (int point = 0; point < 6; point++)
            faces.Add(CreateFace(innerSize.Value, outerSize.Value, height.Value / 2f, height.Value / 2f, point));
    }

    private void CombineFaces()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < faces.Count; i++)
        {
            // Add vertices
            vertices.AddRange(faces[i].vertices);
            uvs.AddRange(faces[i].uvs);

            // Offset the triangles
            int offset = 4 * i;
            foreach (int triangle in faces[i].triangles)
                triangles.Add(triangle + offset);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
    }

    private Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        List<Vector3> vertices = new List<Vector3>() { pointA, pointB, pointC, pointD };
        List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0 };
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        if (reverse)
            vertices.Reverse();

        return new Face(vertices, triangles, uvs);
    }

    private Vector3 GetPoint(float size, float height, int index)
    {
        float angle_deg = isFlatTopped.Value ? 60 * index : 60 * index - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3((size * Mathf.Cos(angle_rad)), height, size * Mathf.Sin(angle_rad));
    }

    public void ChangeColorToOriginal()
    {
        meshRenderer.material.color = originalColor.Value;
    }

    public void ChangeColor(UnityEngine.Color color)
    {
        meshRenderer.material.color = color;
    }

    public UnityEngine.Color GetColor()
    {
        return meshRenderer.material.color;
    }

    public ItemInfo GetItem()
    {
        if (hasItem.Value == -1)
            return null;

        return GameManager.instance.allExistingItems[hasItem.Value];
    }

    public bool IsObstacle()
    {
        return originalColor.Value.g <= HexGridLayout.instance.obstacleThreshold;
    }
}
