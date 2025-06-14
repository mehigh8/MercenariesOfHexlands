using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;
using Unity.VisualScripting;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Transporting;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    // TODO: 
    [Header("Camera Settings")]
    [SerializeField] private float cameraSpeed;

    [Header("Navigation Settings")]
    [SerializeField] private LayerMask mask;
    [SerializeField] private Pathfinder pathfinder;
    [SerializeField] private PlayerInfo playerInfo;

    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0, 9, -5);
    private Camera playerCamera;
    private NavMeshAgent navAgent;
    private HexRenderer currentlyOn;

    private List<HexGridLayout.HexNode> path = null;
    private List<HexGridLayout.HexNode> highlightedPath = null;
    private HexGridLayout.HexNode lastHighlightedTarget = null;
    private HexGridLayout.HexNode currentPosition = null;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            playerCamera = Camera.main;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z) + cameraOffset;
        }
    }

    private IEnumerator Start()
    {
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
        if (!base.IsOwner || GameManager.instance.currentPlayerTurn.Value != LocalConnection.ClientId)
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

        if (GameManager.instance.currentPlayerTurn.Value == LocalConnection.ClientId)
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
            highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColorToOriginal());
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
                currentPosition = path[0];
                path.RemoveAt(0);

                Vector3 dest = new Vector3(currentPosition.hexObj.transform.position.x, transform.position.y, currentPosition.hexObj.transform.position.z);
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
            playerInfo.EquipItem(currentPosition.hexRenderer.GetItem());
            PickupItemRPC(currentlyOn.name);
        }

    }

    private void Update()
    {
        InitOccupying();
        CameraMovement();
        PickMovement();
        ApplyMovement();
        HighlightMovement();
        PickupItem();

        //if (Input.GetKeyDown(KeyCode.E))
        //    EndTurn();
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
