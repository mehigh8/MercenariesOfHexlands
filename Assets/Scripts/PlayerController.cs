using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;
using Unity.VisualScripting;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Transporting;

//This is made by Bobsi Unity - Youtube
public class PlayerController : NetworkBehaviour
{
    // TODO: 
    [Header("Camera Settings")]
    [SerializeField] private float cameraSpeed;

    [Header("Navigation Settings")]
    [SerializeField] private LayerMask mask;
    [SerializeField] private Pathfinder pathfinder;

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

    private void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        float minDist = float.MaxValue;
        foreach (HexGridLayout.HexNode node in HexGridLayout.instance.hexNodes)
            if (Vector3.Distance(node.hexObj.transform.position, transform.position) < minDist)
            {
                minDist = Vector3.Distance(node.hexObj.transform.position, transform.position);
                currentPosition = node;
            }
        Debug.Log("My current hex is " + currentPosition.hexObj.name);
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

    private void PlayerMovement()
    {
        if (!base.IsOwner)
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
                        currentlyOn.ChangeOccupying(null);
                        currentlyOn = hex;
                        currentlyOn.ChangeOccupying(gameObject);
                        path = pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));
                    }
                }
                else
                {
                    Debug.Log("no collision");
                }
            }
        }

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

    private void HighlightMovement()
    {
        if (!base.IsOwner)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, mask))
        {
            if (hit.collider != null)
            {
                if (hit.collider.TryGetComponent<HexRenderer>(out HexRenderer hex) && hex.occupying.Value == null)
                {
                    if (lastHighlightedTarget == null || (lastHighlightedTarget != null && hex != lastHighlightedTarget.hexRenderer))
                    {
                        if (highlightedPath != null)
                            highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColorToOriginal());

                        highlightedPath = pathfinder.FindPath(currentPosition, HexGridLayout.instance.hexNodes.Find(h => h.hexObj == hit.collider.gameObject));

                        highlightedPath.ForEach(hex => hex.hexRenderer.ChangeColor(Color.red));

                        lastHighlightedTarget = highlightedPath[highlightedPath.Count - 1];
                    }

                    return;
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
        currentlyOn = HexGridLayout.instance.GetClosestHex(transform.position);
        currentlyOn.ChangeOccupying(gameObject);
    }

    private void Update()
    {
        InitOccupying();
        CameraMovement();
        PlayerMovement();
        HighlightMovement();
    }
}
