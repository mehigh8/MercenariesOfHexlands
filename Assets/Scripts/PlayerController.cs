using UnityEngine;
using FishNet.Object;
using UnityEngine.AI;
using Unity.VisualScripting;

//This is made by Bobsi Unity - Youtube
public class PlayerController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float cameraSpeed;

    [Header("Navigation Settings")]
    [SerializeField] private LayerMask mask;

    [SerializeField]
    private Vector3 cameraOffset = new Vector3(0, 9, -5);
    private Camera playerCamera;
    private NavMeshAgent navAgent;


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
            if (Physics.Raycast(ray, out RaycastHit hit, mask)) {
                if (hit.collider != null)
                {
                    Debug.Log(hit.point);
                    Debug.Log(hit.collider.transform.position);
                    Debug.Log(hit.collider.gameObject.name);
                    navAgent.SetDestination(hit.collider.transform.position);
                }
                else
                {
                    Debug.Log("no collision");
                }
            }       
        }
    }

    private void Update()
    {
        CameraMovement();
        PlayerMovement();
    }
}
