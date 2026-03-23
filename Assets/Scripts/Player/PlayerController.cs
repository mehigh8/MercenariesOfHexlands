using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using FishNet.CodeGenerating;
using FishNet.Object.Synchronizing;
using System.Globalization;

public class PlayerController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float cameraSpeed; // Speed value used for camera movement

    [Header("Navigation Settings")]
    [SerializeField] private LayerMask mask; // LayerMask of the ground
    [SerializeField] private PlayerInfo playerInfo; // Reference to the player info script

    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 9, -5); // Camera offset relative to player
    private Camera playerCamera; // Reference to the Camera
    private NavMeshAgent navAgent; // Reference to the NavMeshAgent script
    [AllowMutableSyncType] public SyncVar<string> currentlyOn; // Name of the hex the player is currently standing on

    [HideInInspector] public List<HexGridLayout.HexNode> path = null; // Hex path used for movement
    private List<HexGridLayout.HexNode> highlightedPath = null; // Hex path used to preview movement
    private HexGridLayout.HexNode lastHighlightedTarget = null; // Last hex that was a destination for highlight. Used for optimization (TODO: Optimize more)
    [HideInInspector] public HexGridLayout.HexNode currentPosition = null; // Reference to the HexNode the player is currently standing on (should be the hex node with the name held by the SyncVar above)

    private AbilityHandler abilityHandler; // Reference to AbilityHandler script
    private HexGridLayout.HexNode previousHex; // Reference to previous hex used for ability input

    #region Unity + FishNet Functions
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            // Set references and callbacks
            playerCamera = Camera.main;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z) + cameraOffset;
            UIManager.instance.abilitiesUIManager.client = GetComponent<AbilityHandler>();
            UIManager.instance.abilitiesUIManager.ShowAbilities(true);
            GameManager.instance.OnBeginTurn += GetComponent<AbilityHandler>().ReduceCooldowns;
            GameManager.instance.OnBeginTurn += ResetMovementThisTurn;
        }
    }

    private IEnumerator Start()
    {
        // Set more references (TODO: Why are some here and some in OnStartClient??)
        abilityHandler = GetComponent<AbilityHandler>();
        abilityHandler.playerController = this;
        navAgent = GetComponent<NavMeshAgent>();
        // Wait for all hexes to be generated before Update
        while (HexGridLayout.instance.transform.childCount != HexGridLayout.instance.gridSize.x * HexGridLayout.instance.gridSize.y)
            yield return 0;
    }

    private void Update()
    {
        if (!GameLobbyManager.Instance.gameStarted)
            return;

        // Update is split into more functions, each having its specific purpose, that are called 1 by 1
        CameraMovement();

        if (IsOwner)
        {
            InitOccupying();
            if (GameManager.instance.IsMyTurn())
            {
                PickMovement();
                HighlightMovement();
                InventoryInteract();
                PickupItem();
                AbilityInput();
                EndTurn();
            }
        }

        // Apply movemement is outside the ifs since a player can end turn while it is still moving
        ApplyMovement();
    }
    #endregion

    #region Callbacks
    /// <summary>
    /// Callback used to reset movement amount when a new turn begins
    /// </summary>
    /// <param name="turn">ID of the player whose turn it is</param>
    public void ResetMovementThisTurn(int turn)
    {
        if (LocalConnection.ClientId != turn)
            return;

        playerInfo.canMoveThisTurn = playerInfo.movementPerTurn;
    }
    #endregion

    #region Update Steps
    /// <summary>
    /// Function used to update internal reference to the hex the player is standing on
    /// </summary>
    private void InitOccupying()
    {
        if (currentlyOn.Value != "")
            return;
        currentPosition = HexGridLayout.instance.GetClosestHex(transform.position);
        UpdateCurrentlyOn(currentPosition.hexRenderer.name);
        if (currentPosition != null)
            UpdateHex(currentPosition.hexObj.name, gameObject);
    }

    /// <summary>
    /// Function used to handle camera movement input and logic
    /// </summary>
    private void CameraMovement()
    {
        if (!playerCamera)
            return;
        if (Input.GetKeyDown(KeyCode.Space))
                playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z) + cameraOffset;

        Vector3 toMove = Vector3.zero;

        if (Input.GetKey(KeyCode.D))
            toMove += Vector3.right;
        if (Input.GetKey(KeyCode.A))
            toMove -= Vector3.right;
        if (Input.GetKey(KeyCode.W))
            toMove += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            toMove -= Vector3.forward;

        playerCamera.transform.position += toMove.normalized * cameraSpeed;
    }

    /// <summary>
    /// Function used to pick a hex for movement by checking for a mouse click on top of a hex
    /// </summary>
    private void PickMovement()
    {
        if (abilityHandler.currentAbility != null || isMoving())
            return;

        // Mouse input check
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            // Ray used to check if the cursor was on top of a hex
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, mask))
            {
                if (hit.collider != null)
                {
                    // Check if a hex was found and if it is not an obstacle
                    if (hit.collider.TryGetComponent<HexRenderer>(out HexRenderer hex) && hex.occupying.Value == null && !hex.IsObstacle())
                    {

                        // Use Pathfinder to determine a path
                        path = Pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));
                        // If the determined path is too long, shorten it to the amount of movement the player still has this turn
                        if (path.Count > playerInfo.canMoveThisTurn)
                            path.RemoveRange(playerInfo.canMoveThisTurn, path.Count - playerInfo.canMoveThisTurn);
                        

                        HexRenderer finalHex = path[path.Count - 1].hexRenderer;
                        // Update hexes
                        UpdateHex(currentPosition.hexObj.name, null);
                        UpdateCurrentlyOn(finalHex.name);
                        currentPosition = path.Last();
                        UpdateHex(currentPosition.hexObj.name, gameObject);

                        // Update remaining movement
                        playerInfo.canMoveThisTurn -= path.Count;
                    }
                }
                else
                {
                    Debug.Log("no collision");
                }
            }
        }
    }

    /// <summary>
    /// Function used to move the player using the NaxMeshAgent
    /// </summary>
    private void ApplyMovement()
    {
        // As long as the path still has nodes, use NavMeshAgent to walk to the next node
        if (path != null && path.Count > 0)
        {
            if (!navAgent.hasPath || (navAgent.hasPath && navAgent.remainingDistance < 0.1f))
            {
                Vector3 dest = new Vector3(path[0].hexObj.transform.position.x, transform.position.y, path[0].hexObj.transform.position.z);
                path.RemoveAt(0);
                navAgent.SetDestination(dest);
            }
        }
    }

    /// <summary>
    /// Function used to preview a possible path towards the cursor by highlighting the hexes on that path
    /// </summary>
    private void HighlightMovement()
    {
        if (abilityHandler.currentAbility == null && !isMoving())
        {
            // Use ray to check if the cursor is on top of a hex
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, mask))
            {
                if (hit.collider != null)
                {
                    // Check if a hex was found
                    if (hit.collider.TryGetComponent<HexRenderer>(out HexRenderer hex) && hex.occupying.Value == null)
                    {
                        // Only update the highlighted path if a new hex was found. This is for optimization
                        if (lastHighlightedTarget == null || hex != lastHighlightedTarget.hexRenderer)
                        {
                            // Stop highlighting the old path
                            if (highlightedPath != null)
                                highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColorToOriginal());

                            // Use Pathfinder to find a path to the new hex
                            highlightedPath = Pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));
                            if (highlightedPath != null)
                            {
                                // Highlight the hexes on the new path
                                for (int i = 0; i < highlightedPath.Count && i < playerInfo.canMoveThisTurn; i++)
                                    highlightedPath[i].hexRenderer.ChangeColor(highlightedPath[i].hexRenderer.GetColor() + new Color(0.4f, 0.4f, 0.4f, 1f));
                            }

                            // Store the last hex
                            lastHighlightedTarget = highlightedPath != null && highlightedPath.Count > 0 ? highlightedPath[highlightedPath.Count - 1] : null;
                        }

                        return;
                    }
                }
            }
        }

        if (highlightedPath != null)
        {
            highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColorToOriginal());
            highlightedPath = null;
        }
        
    }

    /// <summary>
    /// Function used to open and close the inventory
    /// </summary>
    private void InventoryInteract()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (UIManager.instance.inventoryUIManager.isOpened)
                UIManager.instance.inventoryUIManager.CloseInventory();
            else
                UIManager.instance.inventoryUIManager.OpenInventory(playerInfo, this);
        }
    }

    /// <summary>
    /// Function used to pick up an item from the hex underneath the player
    /// </summary>
    private void PickupItem()
    {
        if (Input.GetKeyDown(KeyCode.F) && currentPosition.hexRenderer.hasItem.Value != -1)
        {
            // Check if the player has space in its inventory
            if (UIManager.instance.inventoryUIManager.StoreItem(currentPosition.hexRenderer.GetItem()))
                PickupItemRPC(currentPosition.hexObj.name);
        }
    }

    /// <summary>
    /// Function used to start casting or cancel casting abilities
    /// </summary>
    private void AbilityInput()
    {
        if (abilityHandler.currentAbility != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (previousHex != null)
                {
                    List<HexGridLayout.HexNode> AOENodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(previousHex) <= abilityHandler.currentAbility.areOfEffect).ToList();
                        foreach (HexGridLayout.HexNode hex in AOENodes)
                            hex.hexRenderer.ChangeColorToOriginal();
                }
                abilityHandler.CancelCasting();
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, mask))
            {
                HexGridLayout.HexNode centerHex = HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject);
                if (previousHex != null && previousHex != centerHex)
                {
                    List<HexGridLayout.HexNode> AOENodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(previousHex) <= abilityHandler.currentAbility.areOfEffect).ToList();
                    foreach (HexGridLayout.HexNode hex in AOENodes)
                    {
                        if (abilityHandler.IsWithinRange(hex))
                            hex.hexRenderer.ChangeColor(hex.hexRenderer.originalColor.Value + new Color(0.4f, 0.4f, 0.4f, 1f));
                        else
                            hex.hexRenderer.ChangeColorToOriginal();
                    }

                    AOENodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(centerHex) <= abilityHandler.currentAbility.areOfEffect).ToList();
                    bool isValid = abilityHandler.IsHexValid(centerHex);
                    foreach (HexGridLayout.HexNode hex in AOENodes)
                    {
                        if (isValid)
                            hex.hexRenderer.ChangeColor(Color.green);
                        else
                            hex.hexRenderer.ChangeColor(Color.red);
                    }
                }
                previousHex = centerHex;
                if (Input.GetKeyDown(KeyCode.Mouse0) && abilityHandler.IsHexValid(previousHex))
                {
                    List<HexGridLayout.HexNode> AOENodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(previousHex) <= abilityHandler.currentAbility.areOfEffect).ToList();
                    foreach (HexGridLayout.HexNode hex in AOENodes)
                        hex.hexRenderer.ChangeColorToOriginal();
                    abilityHandler.ConfirmCasting(AOENodes, previousHex);
                }
            }
            else if (previousHex != null)
            {
                List<HexGridLayout.HexNode> AOENodes = HexGridLayout.instance.hexNodes.Where(h => h.Distance(previousHex) <= abilityHandler.currentAbility.areOfEffect).ToList();
                foreach (HexGridLayout.HexNode hex in AOENodes)
                {
                    if (abilityHandler.IsWithinRange(hex))
                        hex.hexRenderer.ChangeColor(hex.hexRenderer.originalColor.Value + new Color(0.4f, 0.4f, 0.4f, 1f));
                    else
                        hex.hexRenderer.ChangeColorToOriginal();
                }
            }
        }
    }

    /// <summary>
    /// Function used to end turn
    /// </summary>
    public void EndTurn()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (abilityHandler.currentAbility != null)
                abilityHandler.CancelCasting();
            UIManager.instance.inventoryUIManager.CloseInventory();
            EndTurnRPC();
        }
    }
    #endregion

    #region RPCs and Utils
    /// <summary>
    /// Server RPC used to place an item on the ground
    /// </summary>
    /// <param name="item">ID of the item to be placed</param>
    /// <param name="hex">Name of the hex on top of which to place the item</param>
    [ServerRpc]
    public void DropItem(int item, string hex)
    {
        HexGridLayout.instance.PlaceItem(item, hex);
    }
    
    /// <summary>
    /// Server RPC used to end turn by calling NextTurn on GameManager
    /// </summary>
    [ServerRpc]
    public void EndTurnRPC()
    {
        GameManager.instance.NextTurn();
    }

    /// <summary>
    /// Server RPC used to update the occupier reference of a hex
    /// </summary>
    /// <param name="hex">Name of the hex</param>
    /// <param name="occupier">Occupier reference</param>
    [ServerRpc]
    public void UpdateHex(string hex, GameObject occupier)
    {
        HexGridLayout.instance.UpdateHex(hex, occupier);
    }

    /// <summary>
    /// Server RPC used to pick up an item from a hex
    /// </summary>
    /// <param name="hex">Name of the hex</param>
    [ServerRpc]
    public void PickupItemRPC(string hex)
    {
        HexGridLayout.instance.PickupItem(hex);
    }

    /// <summary>
    /// Server RPC to update the hex reference on which the player is standing on
    /// </summary>
    /// <param name="hex">Name of the hex</param>
    [ServerRpc]
    public void UpdateCurrentlyOn(string hex)
    {
        currentlyOn.Value = hex;
    }

    /// <summary>
    /// Function used to check if the player is moving
    /// </summary>
    /// <returns>True - Player is moving; False - Player is not moving</returns>
    public bool isMoving()
    {
        return (path != null && path.Count > 0) || (path != null && path.Count == 0 && navAgent.remainingDistance > 0.1f);
    }
    #endregion
}
