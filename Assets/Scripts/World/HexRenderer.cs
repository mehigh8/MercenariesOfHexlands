using FishNet.CodeGenerating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class HexRenderer : NetworkBehaviour
{
    /// <summary>
    /// Class used to store information about ligering effects
    /// </summary>
    [System.Serializable]
    public class LingeringEffect
    {
        public int source;
        public AbilityInfo.Element element;
        public int remainingDuration;

        public LingeringEffect()
        {
            this.source = -1;
            this.element = AbilityInfo.Element.None;
            this.remainingDuration = 0;
        }

        public LingeringEffect(int source, AbilityInfo.Element element, int remainingDuration)
        {
            this.source = source;
            this.element = element;
            this.remainingDuration = remainingDuration;
        }
    }

    /// <summary>
    /// Struct used to store vertices, indices and uvs for a face. Used for the hex geometry
    /// </summary>
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

    [SerializeField] private List<Texture> elementMapper; // List used to map effects to their corresponding textures
    [SerializeField] private MeshRenderer elementObject; // Mesh renderer of the effect child object
    [SerializeField] private Material material; // Material of the hex
    [SerializeField] private List<GameObject> fowPrefabs; // Prefabs of the Fog of War hexes
    [AllowMutableSyncType] public SyncVar<int> hexVisualPrefab; // Hex's visual prefab index
    [AllowMutableSyncType] public SyncVar<float> innerSize; // Hex's inner size (If bigger than 0 it will create a hole in the middle of the hex; Most likely will not be used)
    [AllowMutableSyncType] public SyncVar<float> outerSize; // Hex's outer size (You can consider this as the pure size of the hex)
    [AllowMutableSyncType] public SyncVar<float> height; // Hex's height (Most likely will not be used since we now use prefabs for visuals)
    [AllowMutableSyncType] public SyncVar<bool> isFlatTopped; // Specifies if the top of the hex is flat or pointy
    [AllowMutableSyncType] public SyncVar<Vector2Int> coords; // Hex's coordinates in world space
    [AllowMutableSyncType] public SyncVar<GameObject> occupying = new SyncVar<GameObject>(); // Specifies which entity is occupying this hex (TODO: Maybe change to a bool to only specify if occupied since it most likely doesn't work to specify the exact occupier)
    [AllowMutableSyncType] public SyncVar<Color> originalColor; // Hex's base color
    [AllowMutableSyncType] public SyncVar<int> hasItem; // Index of the item that is on this hex
    [AllowMutableSyncType] public SyncVar<LingeringEffect> lingeringEffect = new SyncVar<LingeringEffect>(); // Hex's lingering effect that is active on it

    private Mesh mesh; // Mesh that will be created
    private MeshFilter meshFilter; // Reference to the mesh filter
    private MeshRenderer meshRenderer; // Reference to the mesh renderer
    private MeshCollider meshCollider; // Reference to the mesh collider

    private List<Face> faces; // List of this hex's faces

    private bool isRevealed = false; // Specifies if this hex has been revealed by the player
    private GameObject visualHex; // Reference to the spawned visuals of the hex
    private GameObject fowHex; // Reference to the spawned Fog of War hex

    #region Unity + FishNet Functions
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        // Add callback to change the occupier of the hex
        occupying.OnChange += OnOccupyingChange;
        // Add callback for the start of a new turn
        GameManager.instance.OnBeginTurn += ReduceLingering;
        // Add callback to change the lingering effect
        lingeringEffect.OnChange += OnLingeringChange;
    }

    public override void OnStopNetwork()
    {
        base.OnStartNetwork();
        // Remove callbacks
        occupying.OnChange -= OnOccupyingChange;
        GameManager.instance.OnBeginTurn -= ReduceLingering;
        lingeringEffect.OnChange -= OnLingeringChange;
    }

    private void Start()
    {
        // Get references
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        // Set material
        meshRenderer.material = material;
        SetMaterialTransparent(meshRenderer.material);

        // Set object's name to uniquely identify this hex
        gameObject.name = $"Hex {coords.Value.x},{coords.Value.y}";

        // If specified, add itself to the list of hex nodes in HexGridLayout (needed for clients)
        if (HexGridLayout.instance.populateHexNodeList)
            HexGridLayout.instance.hexNodes.Add(new HexGridLayout.HexNode(coords.Value.x, coords.Value.y, gameObject, this));

        // Set parent of this hex
        transform.parent = HexGridLayout.instance.transform;

        // Instantiate visual prefab (TODO: Maybe move in a better place?)
        MeshRenderer visuals = Instantiate(GameManager.instance.allExistingTiles[hexVisualPrefab.Value], transform.position - Vector3.up * 0.51f, Quaternion.Euler(-90, 0, 0), transform).GetComponent<MeshRenderer>();
        visuals.material = new Material(visuals.material);
        visuals.material.color -= Color.white * originalColor.Value.g / 2; // This is just cosmetic

        // Instantiate fog of war hex and switch layer for actual hex
        fowHex = Instantiate(fowPrefabs[Random.Range(0, fowPrefabs.Count)], new Vector3(transform.position.x, 0f, transform.position.z), Quaternion.Euler(-90, 0, Random.Range(0, 6) * 60f));
        visualHex = visuals.gameObject;

        Helpers.SetLayerRecursively(visualHex, LayerMask.NameToLayer("Hidden"));

        // Draw the mesh
        DrawMesh();
        
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Callback to log the change of the hex's occupier
    /// </summary>
    /// <param name="oldVal">Previous occupier</param>
    /// <param name="newVal">New occupier</param>
    /// <param name="asServer">Bool to specify if the callback is called on the Server</param>
    private void OnOccupyingChange(GameObject oldVal, GameObject newVal, bool asServer)
    {
        Debug.Log($"{(asServer ? "Server" : "Client")}{LocalConnection.ClientId} - {gameObject.name} changed from {oldVal} to {newVal}");
    }

    /// <summary>
    /// Callback to update the lingering effect texture
    /// </summary>
    /// <param name="oldVal">Previous lingering effect</param>
    /// <param name="newVal">New lingering effect</param>
    /// <param name="asServer">Bool to specify if the callback is called on the Server</param>
    private void OnLingeringChange(LingeringEffect oldVal, LingeringEffect newVal, bool asServer)
    {
        if (lingeringEffect.Value == null)
        {
            elementObject.material.mainTexture = null;
            elementObject.enabled = false;
        }
        else
        {
            elementObject.enabled = true;
            elementObject.material.mainTexture = elementMapper[(int)newVal.element];
        }
    }
    // TODO: Remove the turn
    /// <summary>
    /// Callback to update the duration of the lingering effect
    /// </summary>
    /// <param name="turn">ID of the player whose turn it is</param>
    private void ReduceLingering(int turn)
    {
        if (lingeringEffect.Value == null || !GameManager.instance.IsMyTurn() || lingeringEffect.Value.source != GameManager.instance.LocalConnection.ClientId)
            return;
        lingeringEffect.Value.remainingDuration -= 1;
        if (lingeringEffect.Value.remainingDuration == 0)
            lingeringEffect.Value = null;
    }
    #endregion

    #region Mesh Generation

    /// <summary>
    /// This basically tells unity that the material should be transparent
    /// </summary>
    /// <param name="mat">The material we wish to make transparent</param>
    public static void SetMaterialTransparent(Material mat)
    {
        // Tell URP this is a transparent surface
        mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent

        // Set the blend mode
        mat.SetFloat("_Blend", 0f); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply

        // Disable depth writing (required for transparency)
        mat.SetFloat("_ZWrite", 0f);

        // Set the actual GPU blend state
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

        // Enable the transparency keyword
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        // Set render queue to transparent range
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    /// <summary>
    /// Function used to create the mesh, then create and combine the necessary faces
    /// </summary>
    public void DrawMesh()
    {
        // Create mesh
        mesh = new Mesh();
        mesh.name = "Hex";

        // Assign the mesh and color
        meshFilter.mesh = mesh;
        meshRenderer.material.color = Color.clear; // TODO: marking this in case I break stuff

        // Create and combine the faces
        DrawFaces();
        CombineFaces();
    }

    /// <summary>
    /// Function used to create the faces and add them to the list
    /// </summary>
    private void DrawFaces()
    {
        faces = new List<Face>();

        // Top faces
        for (int point = 0; point < 6; point++)
            faces.Add(CreateFace(innerSize.Value, outerSize.Value, height.Value / 2f, height.Value / 2f, point));
    }

    /// <summary>
    /// Function used to combine the vertices, indices and uvs from all the faces and assign them to the mesh
    /// </summary>
    private void CombineFaces()
    {
        // Create common lists
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // For each face add its vertices, indices and uvs to the common lists
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

        // Assign the combined vertices, indices and uvs to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        // Recalculate mesh normals
        mesh.RecalculateNormals();
        // Assign mesh to the mesh collider
        meshCollider.sharedMesh = mesh;
    }

    /// <summary>
    /// Function used to create a face
    /// </summary>
    /// <param name="innerRad">Inner circle radius</param>
    /// <param name="outerRad">Outer circle radius</param>
    /// <param name="heightA">Outer circle height</param>
    /// <param name="heightB">Inner circle height</param>
    /// <param name="point">Face index, specifies which sixth of the circles to use</param>
    /// <param name="reverse">Specifies if the vertices order should be reversed</param>
    /// <returns></returns>
    private Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
    {
        // Calculate trapeze points for the face
        Vector3 pointA = GetPoint(innerRad, heightB, point);
        Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
        Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
        Vector3 pointD = GetPoint(outerRad, heightA, point);

        // Create lists for vertices, indices and uvs
        List<Vector3> vertices = new List<Vector3>() { pointA, pointB, pointC, pointD };
        List<int> triangles = new List<int>() { 0, 1, 2, 2, 3, 0 };
        List<Vector2> uvs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        if (reverse)
            vertices.Reverse();

        return new Face(vertices, triangles, uvs);
    }

    /// <summary>
    /// Function used to calculate the positions on a circle
    /// </summary>
    /// <param name="size">Radius of the circle</param>
    /// <param name="height">Height of the point</param>
    /// <param name="index">Index used to specify a sixth of the circle</param>
    /// <returns></returns>
    private Vector3 GetPoint(float size, float height, int index)
    {
        float angle_deg = isFlatTopped.Value ? 60 * index : 60 * index - 30;
        float angle_rad = Mathf.PI / 180f * angle_deg;
        return new Vector3((size * Mathf.Cos(angle_rad)), height, size * Mathf.Sin(angle_rad));
    }
    #endregion

    #region Utils
    /// <summary>
    /// Function used to change the color of the hex to the base color
    /// </summary>
    public void ChangeColorToOriginal()
    {
        meshRenderer.material.color = Color.clear; // TODO: marking this in case I break stuff
    }

    /// <summary>
    /// Function used to change the color of the hex to the specified color
    /// </summary>
    /// <param name="color">Color to which to change the hex</param>
    public void ChangeColor(Color color)
    {
        color.a = 0.5f;
        meshRenderer.material.color = color;
    }

    /// <summary>
    /// Function used to get the current color of the hex
    /// </summary>
    /// <returns>The current color of the hex</returns>
    public Color GetColor()
    {
        return meshRenderer.material.color;
    }

    /// <summary>
    /// Function used to get the item that is on the hex
    /// </summary>
    /// <returns>ItemInfo that is on the hex</returns>
    public ItemInfo GetItem()
    {
        if (hasItem.Value == -1)
            return null;

        return GameManager.instance.allExistingItems[hasItem.Value];
    }

    /// <summary>
    /// Function used to check if the hex is an obstacle
    /// </summary>
    /// <returns>True - hex is an obstacle; False - hex is not an obstacle</returns>
    public bool IsObstacle()
    {
        return originalColor.Value.g <= HexGridLayout.instance.obstacleThreshold;
    }

    /// <summary>
    /// Function used to reveal this hex
    /// </summary>
    public void RevealHex()
    {
        if (isRevealed) return;

        Helpers.SetLayerRecursively(visualHex, LayerMask.NameToLayer("Default"));
        isRevealed = true;

        StartCoroutine(RevealHexAnimation());
    }

    /// <summary>
    /// Function used to apply a new lingering effect to the hex
    /// </summary>
    /// <param name="clientId">ID of the client that is applying the effect</param>
    /// <param name="element">Effect element</param>
    /// <param name="lingeringDuration">Duration of the lingering effect</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyLingering(int clientId, AbilityInfo.Element element, int lingeringDuration)
    {
        lingeringEffect.Value = new LingeringEffect(clientId, element, lingeringDuration);
    }
    #endregion

    #region Others
    /// <summary>
    /// IEnumerator used to animate the reveal of the hex
    /// </summary>
    /// <returns>-</returns>
    private System.Collections.IEnumerator RevealHexAnimation()
    {
        Vector3 velocity = Vector3.zero;
        Vector3 destination = fowHex.transform.position + new Vector3(0, 2, 0);
        while (Mathf.Abs(fowHex.transform.position.y - destination.y) > 0.01f)
        {
            fowHex.transform.position = Vector3.SmoothDamp(fowHex.transform.position, destination, ref velocity, 0.1f);
            yield return null;
        }

        Destroy(fowHex);
    }
    #endregion
}
