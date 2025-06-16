using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;
using Unity.VisualScripting;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Transporting;
using System.Collections;
using System.Linq;

public class PlayerController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float cameraSpeed;

    [Header("Navigation Settings")]
    [SerializeField] private LayerMask mask;
    [SerializeField] public Pathfinder pathfinder;
    [SerializeField] private PlayerInfo playerInfo;

    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0, 9, -5);
    private Camera playerCamera;
    private NavMeshAgent navAgent;
    [HideInInspector] public HexRenderer currentlyOn;

    [HideInInspector] public List<HexGridLayout.HexNode> path = null;
    private List<HexGridLayout.HexNode> highlightedPath = null;
    private HexGridLayout.HexNode lastHighlightedTarget = null;
    [HideInInspector] public HexGridLayout.HexNode currentPosition = null;

    private AbilityHandler abilityHandler;

    public bool isMoving()
    {
        return (path != null && path.Count > 0) || (path != null && path.Count == 0 && navAgent.remainingDistance > 0.1f);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            playerCamera = Camera.main;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z) + cameraOffset;
            UIManager.instance.abilitiesUIManager.client = GetComponent<AbilityHandler>();
            UIManager.instance.abilitiesUIManager.ShowAbilities(true);
        }
    }

    private IEnumerator Start()
    {
        abilityHandler = GetComponent<AbilityHandler>();
        abilityHandler.playerController = this;
        navAgent = GetComponent<NavMeshAgent>();
        while (HexGridLayout.instance.transform.childCount != HexGridLayout.instance.gridSize.x * HexGridLayout.instance.gridSize.y)
            yield return 0;
    }

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

    private void PickMovement()
    {
        if (!base.IsOwner || GameManager.instance.currentPlayerTurn.Value != LocalConnection.ClientId || abilityHandler.currentAbility != null || isMoving())
            return;
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, mask))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.TryGetComponent<HexRenderer>(out HexRenderer hex) && hex.occupying.Value == null)
                    {
                        path = pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));
                        if (path.Count > playerInfo.movementPerTurn)
                            path.RemoveRange(playerInfo.movementPerTurn, path.Count - playerInfo.movementPerTurn);
                        
                        HexRenderer finalHex = path[path.Count - 1].hexRenderer;

                        UpdateHex(currentlyOn.name, null);
                        currentlyOn = finalHex;
                        currentPosition = path.Last();
                        UpdateHex(currentlyOn.name, gameObject);
                        
                        EndTurn();
                    }
                }
                else
                {
                    Debug.Log("no collision");
                }
            }
        }
    }

    private void HighlightMovement()
    {
        if (!base.IsOwner)
            return;

        if (GameManager.instance.currentPlayerTurn.Value == LocalConnection.ClientId && abilityHandler.currentAbility == null && !isMoving())
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, mask))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.TryGetComponent<HexRenderer>(out HexRenderer hex) && hex.occupying.Value == null)
                    {
                        if (lastHighlightedTarget == null || hex != lastHighlightedTarget.hexRenderer)
                        {
                            if (highlightedPath != null)
                                highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColorToOriginal());

                            highlightedPath = pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));
                            if (highlightedPath != null)
                            {
                                for (int i = 0; i < highlightedPath.Count && i < playerInfo.movementPerTurn; i++)
                                    highlightedPath[i].hexRenderer.ChangeColor(highlightedPath[i].hexRenderer.GetColor() + new Color(0.4f, 0.4f, 0.4f, 1f));
                            }

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

    private HexGridLayout.HexNode previousHex;
    private void AbilityInput()
    {
        if (!IsOwner || !GameManager.instance.IsMyTurn())
            return;

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

    private void InitOccupying()
    {
        if (currentlyOn || !base.IsOwner)
            return;
        currentPosition = HexGridLayout.instance.GetClosestHex(transform.position);
        currentlyOn = currentPosition?.hexRenderer;
        print(currentlyOn);
        if (currentlyOn)
            UpdateHex(currentlyOn.name, gameObject);
    }

    private void ApplyMovement()
    {
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

    private void PickupItem()
    {
        if (!IsOwner || GameManager.instance.currentPlayerTurn.Value != LocalConnection.ClientId)
            return;

        if (Input.GetKeyDown(KeyCode.F) && currentlyOn.hasItem.Value != -1)
        {
            if (UIManager.instance.inventoryUIManager.StoreItem(currentPosition.hexRenderer.GetItem()))
                PickupItemRPC(currentlyOn.name);
        }
    }

    [ServerRpc]
    public void DropItem(int item, string hex)
    {
        HexGridLayout.instance.PlaceItem(item, hex);
    }

    private void InventoryInteract()
    {
        if (!IsOwner || GameManager.instance.currentPlayerTurn.Value != LocalConnection.ClientId)
            return;

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (UIManager.instance.inventoryUIManager.isOpened)
                UIManager.instance.inventoryUIManager.CloseInventory();
            else
                UIManager.instance.inventoryUIManager.OpenInventory(playerInfo, this);
        }
    }

    private void Update()
    {
        InitOccupying();
        CameraMovement();
        PickMovement();
        ApplyMovement();
        HighlightMovement();
        InventoryInteract();
        PickupItem();
        AbilityInput();
    }

    [ServerRpc]
    public void EndTurn()
    {
        GameManager.instance.NextTurn();
    }

    [ServerRpc]
    public void UpdateHex(string hex, GameObject occupier)
    {
        HexGridLayout.instance.UpdateHex(hex, occupier);
    }

    [ServerRpc]
    public void PickupItemRPC(string hex)
    {
        HexGridLayout.instance.PickupItem(hex);
    }
}
